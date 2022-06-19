global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using Newtonsoft.Json;
global using Smartstore.PriceBuilder.Domain;
global using Smartstore.Web.Modelling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.PriceBuilder.Migrations;
using Smartstore.Core;
using Smartstore.Core.Messaging;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.PriceBuilder
{
    internal class Module : ModuleBase, IConfigurable
    {
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public RouteInfo GetConfigurationRoute()
            => new("List", "BlogAdmin", new { area = "Admin" });

        public override async Task InstallAsync(ModuleInstallationContext context)
        {
            await TrySaveSettingsAsync<PriceBuilderSettings>();
            await ImportLanguageResourcesAsync();
            await TrySeedData(context);
            //writeVersionPendingStart();


            await base.InstallAsync(context);
        }

        private async Task TrySeedData(ModuleInstallationContext context)
        {
            try
            {
                var seeder = new BlogInstallationDataSeeder(context, Services.Resolve<IMessageTemplateService>());
                await seeder.SeedAsync(Services.DbContext);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "BlogSampleDataSeeder failed.");
            }
        }

        public override async Task UninstallAsync()
        {
            await DeleteSettingsAsync<PriceBuilderSettings>();
            await DeleteLanguageResourcesAsync();
 
            await getMigrator().MigrateAsync(-1);
 

            await base.UninstallAsync();

        }
        private DbMigrator getMigrator()
        {
            return Engine.EngineContext.Current.Scope.Resolve(typeof(DbMigrator<>).MakeGenericType(typeof(SmartDbContext))) as DbMigrator;
        }
        //private void writeVersionPendingStart()
        //{
        //    //RawDataSettings
        //    var migrator = getMigrator();
        //    var allMigrator = migrator.MigrationTable.GetAppliedMigrations();
        //    var pendingStartInstall = allMigrator.LastOrDefault();
        //    var settings = DataSettings.Instance.RawDataSettings;
        //    if (!settings.TryAdd("PendingStart", pendingStartInstall.ToString(), true))
        //    {
        //        // lỗi cmnr
        //    }
        //}
        //private long getVersionPendingStart()
        //{
        //    //RawDataSettings
        //    var settings = DataSettings.Instance.RawDataSettings;
        //    if (settings.TryGetValue("PendingStart", out var pendingStartInstall))
        //    {
        //        return pendingStartInstall.Convert<long>(-1);
        //    }
        //    return -1;
        //}
    }
}
