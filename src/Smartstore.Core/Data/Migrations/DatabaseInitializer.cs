﻿using System.Reflection;
using Autofac;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Data.Migrations
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private static readonly SyncedCollection<Type> _initializedContextTypes = new List<Type>().AsSynchronized();
        private readonly ILifetimeScope _scope;
        private readonly SmartConfiguration _appConfig;
        private readonly ITypeScanner _typeScanner;

        public DatabaseInitializer(
            ILifetimeScope scope, 
            ITypeScanner typeScanner, 
            SmartConfiguration appConfig)
        {
            _scope = scope;
            _appConfig = appConfig;
            _typeScanner = typeScanner;
        }

        public virtual async Task InitializeDatabasesAsync(CancellationToken cancelToken = default)
        {
            if (!ModularState.Instance.HasChanged)
            {
                // (perf) ignore modules, they did not change since last migration.
                await InitializeDatabaseAsync(typeof(SmartDbContext), cancelToken);
            }
            else
            {
                var contextTypes = _typeScanner.FindTypes<DbContext>().ToArray();
                foreach (var contextType in contextTypes)
                {
                    await InitializeDatabaseAsync(contextType, cancelToken);
                }
            }
        }

        public Task InitializeDatabaseAsync(Type dbContextType, CancellationToken cancelToken = default)
        {
            Guard.NotNull(dbContextType, nameof(dbContextType));
            Guard.IsAssignableFrom<DbContext>(dbContextType);

            var migrator = _scope.Resolve(typeof(DbMigrator<>).MakeGenericType(dbContextType)) as DbMigrator;
            return InitializeDatabaseAsync(migrator, cancelToken);
        }

        protected virtual async Task InitializeDatabaseAsync(DbMigrator migrator, CancellationToken cancelToken = default)
        {
            Guard.NotNull(migrator, nameof(migrator));

            var context = migrator.Context;
            var type = context.GetInvariantType();

            if (_initializedContextTypes.Contains(type))
            {
                return;
            }

            if (!await context.Database.CanConnectAsync(cancelToken))
            {
                throw new InvalidOperationException($"Database migration failed because the target database does not exist. Ensure the database was initialized and properly seeded with data.");
            }

            using (new DbContextScope(context, minHookImportance: HookImportance.Essential))
            {
                // Set (usually longer) command timeout for migrations
                var prevCommandTimeout = context.Database.GetCommandTimeout();
                if (_appConfig.DbMigrationCommandTimeout.HasValue && _appConfig.DbMigrationCommandTimeout.Value > 15)
                {
                    context.Database.SetCommandTimeout(_appConfig.DbMigrationCommandTimeout.Value);
                }
                
                // Run all pending migrations
                await migrator.RunPendingMigrationsAsync(null, cancelToken);

                // Execute the global seeders anyway (on every startup),
                // we could have locale resources or settings to add/update.
                await RunGlobalSeeders(context, cancelToken);

                // Restore standard command timeout
                context.Database.SetCommandTimeout(prevCommandTimeout);

                _initializedContextTypes.Add(type);
            }
        }

        private static async Task RunGlobalSeeders(HookingDbContext dbContext, CancellationToken cancelToken = default)
        {
            var seederTypes = dbContext.Options.FindExtension<DbFactoryOptionsExtension>()?.DataSeederTypes;
            
            if (seederTypes != null)
            {
                foreach (var seederType in seederTypes)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    var seeder = Activator.CreateInstance(seederType);
                    if (seeder != null)
                    {
                        var seedMethod = seederType.GetMethod(nameof(IDataSeeder<HookingDbContext>.SeedAsync), BindingFlags.Public | BindingFlags.Instance);
                        if (seedMethod != null)
                        {
                            await (Task)seedMethod.Invoke(seeder, new object[] { dbContext, cancelToken });
                            await dbContext.SaveChangesAsync(cancelToken);
                        }
                    }
                }
            }
        }
    }
}
