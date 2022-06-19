﻿using System.Data.Common;
using System.Text.RegularExpressions;
using System.Xml;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Providers;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Installation
{
    public partial class InstallationService : IInstallationService
    {
        const string LanguageCookieName = ".Smart.Installation.Lang";

        private IList<InstallationLanguage> _installLanguages;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationContext _appContext;
        private readonly IFilePermissionChecker _filePermissionChecker;
        private readonly IAsyncState _asyncState;
        private readonly IUrlHelper _urlHelper;
        private readonly IEnumerable<Lazy<InvariantSeedData, InstallationAppLanguageMetadata>> _seedDatas;

        public InstallationService(
            IHttpContextAccessor httpContextAccessor,
            IApplicationContext appContext,
            IFilePermissionChecker filePermissionChecker,
            IAsyncState asyncState,
            IUrlHelper urlHelper,
            IEnumerable<Lazy<InvariantSeedData, InstallationAppLanguageMetadata>> seedDatas)
        {
            _httpContextAccessor = httpContextAccessor;
            _appContext = appContext;
            _filePermissionChecker = filePermissionChecker;
            _asyncState = asyncState;
            _urlHelper = urlHelper;
            _seedDatas = seedDatas;

            Logger = _appContext.Logger;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Installation

        public virtual async Task<InstallationResult> InstallAsync(InstallationModel model, ILifetimeScope scope, CancellationToken cancelToken = default)
        {
            Guard.NotNull(model, nameof(model));

            UpdateResult(x =>
            {
                x.ProgressMessage = GetResource("Progress.CheckingRequirements");
                x.Completed = false;
                Logger.Info(x.ProgressMessage);
            });

            if (DataSettings.DatabaseIsInstalled())
            {
                return UpdateResult(x =>
                {
                    x.Success = true;
                    x.RedirectUrl = _urlHelper.Action("Index", "Home");
                    Logger.Info("Application already installed");
                });
            }

            DbFactory dbFactory = null;

            try
            {
                dbFactory = DbFactory.Load(model.DataProvider, _appContext.TypeScanner);
            }
            catch (Exception ex)
            {
                return UpdateResult(x =>
                {
                    x.Errors.Add(ex.Message);
                    Logger.Error(ex);
                });
            }

            model.DbRawConnectionString = model.DbRawConnectionString?.Trim();

            DbConnectionStringBuilder conStringBuilder = null;

            try
            {
                // Try to create connection string
                if (model.UseRawConnectionString)
                {
                    conStringBuilder = dbFactory.CreateConnectionStringBuilder(model.DbRawConnectionString);
                }
                else
                {
                    // Structural connection string
                    var userId = model.DbUserId;
                    var password = model.DbPassword;
                    if (model.DataProvider == "sqlserver" && model.DbAuthType == "windows")
                    {
                        userId = null;
                        password = null;
                    }
                    conStringBuilder = dbFactory.CreateConnectionStringBuilder(model.DbServer, model.DbName, userId, password);
                }
            }
            catch (Exception ex)
            {
                return UpdateResult(x =>
                {
                    x.Errors.Add(GetResource("ConnectionStringWrongFormat"));
                    Logger.Error(ex, x.Errors.Last());
                });
            }

            //// Check FS access rights
            CheckFileSystemAccessRights(GetInstallResult().Errors);

            if (GetInstallResult().HasErrors)
            {
                return UpdateResult(x =>
                {
                    x.Completed = true;
                    x.Success = false;
                    x.RedirectUrl = null;
                    Logger.Error("Aborting installation.");
                });
            }

            SmartDbContext db = null;
            var shouldDeleteDbOnFailure = false;

            try
            {
                cancelToken.ThrowIfCancellationRequested();

                var conString = conStringBuilder.ConnectionString;
                var settings = DataSettings.Instance;

                settings.AppVersion = SmartstoreVersion.Version;
                settings.DbFactory = dbFactory;
                settings.ConnectionString = conString;

                // So that DataSettings.DatabaseIsInstalled() returns false during installation.
                DataSettings.SetTestMode(true);

                // Resolve SeedData instance from primary language
                var lazyLanguage = GetAppLanguage(model.PrimaryLanguage);
                if (lazyLanguage == null)
                {
                    return UpdateResult(x =>
                    {
                        x.Errors.Add(GetResource("Install.LanguageNotRegistered").FormatInvariant(model.PrimaryLanguage));
                        x.Completed = true;
                        x.Success = false;
                        x.RedirectUrl = null;
                        Logger.Error(x.Errors.Last());
                    });
                }

                // Now it is safe to create the DbContext instance
                db = scope.Resolve<SmartDbContext>();

                // Delete only on failure if WE created the database.
                var canConnectDatabase = await db.Database.CanConnectAsync(cancelToken);
                shouldDeleteDbOnFailure = !canConnectDatabase;

                cancelToken.ThrowIfCancellationRequested();

                // Create Language domain object from lazyLanguage
                var languages = db.Languages;
                var primaryLanguage = new Language 
                {
                    Name = lazyLanguage.Metadata.Name,
                    LanguageCulture = lazyLanguage.Metadata.Culture,
                    UniqueSeoCode = lazyLanguage.Metadata.UniqueSeoCode,
                    FlagImageFileName = lazyLanguage.Metadata.FlagImageFileName
                };

                // Build the seed configuration model
                var seedConfiguration = new SeedDataConfiguration
                {
                    DefaultUserName = model.AdminEmail,
                    DefaultUserPassword = model.AdminPassword,
                    SeedSampleData = model.InstallSampleData,
                    Data = lazyLanguage.Value,
                    Language = primaryLanguage,
                    StoreMediaInDB = model.MediaStorage == "db",
                    ProgressMessageCallback = msg => UpdateResult(x => x.ProgressMessage = GetResource(msg))
                };

                UpdateResult(x =>
                {
                    x.ProgressMessage = GetResource("Progress.BuildingDatabase");
                    Logger.Info(x.ProgressMessage);
                });

                //// TEST
                //return UpdateResult(x =>
                //{
                //    x.Completed = true;
                //    x.Success = true;
                //    //x.RedirectUrl = _urlHelper.Action("Index", "Home");
                //    Logger.Info("Installation completed successfully");
                //});

                // ===>>> Creates database
                await db.Database.EnsureCreatedSchemalessAsync(cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                // ===>>> Populates latest schema
                var migrator = scope.Resolve<DbMigrator<SmartDbContext>>(TypedParameter.From(db));
                await migrator.EnsureSchemaPopulatedAsync(cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                // ===>>> Seeds data.
                var templateService = scope.Resolve<IMessageTemplateService>(TypedParameter.From(db));
                var seeder = new InstallationDataSeeder(_appContext, migrator, templateService, seedConfiguration, Logger);
                await seeder.SeedAsync(db, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                // ===>>> Install modules
                await InstallModules(model, db, scope, cancelToken);

                // Move media and detect media file tracks (must come after module installation)
                UpdateResult(x =>
                {
                    x.ProgressMessage = GetResource("Progress.ProcessingMedia");
                    Logger.Info(x.ProgressMessage);
                });

                var mediaTracker = scope.Resolve<IMediaTracker>();
                foreach (var album in scope.Resolve<IAlbumRegistry>().GetAlbumNames(true))
                {
                    await mediaTracker.DetectAllTracksAsync(album, cancelToken);
                }

                cancelToken.ThrowIfCancellationRequested();

                UpdateResult(x =>
                {
                    x.ProgressMessage = GetResource("Progress.Finalizing");
                    Logger.Info(x.ProgressMessage);
                });

                // Now persist settings
                settings.Save();

                // SUCCESS: Redirect to home page
                return UpdateResult(x =>
                {
                    x.Completed = true;
                    x.Success = true;
                    x.RedirectUrl = _urlHelper.Action("Index", "Home");
                    Logger.Info("Installation completed successfully");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

                // Delete Db if it was auto generated
                if (db != null && shouldDeleteDbOnFailure)
                {
                    try
                    {
                        Logger.Debug("Deleting database");
                        await db.Database.EnsureDeletedAsync(cancelToken);
                    }
                    catch 
                    { 
                    }
                }

                // Clear provider settings if something got wrong
                DataSettings.Delete();

                var msg = ex.Message;
                var realException = ex;
                while (realException.InnerException != null)
                {
                    realException = realException.InnerException;
                }

                if (!object.Equals(ex, realException))
                {
                    msg += " (" + realException.Message + ")";
                }

                return UpdateResult(x =>
                {
                    x.Errors.Add(string.Format(GetResource("SetupFailed"), msg));
                    x.Success = false;
                    x.Completed = true;
                    x.RedirectUrl = null;
                });
            }
        }

        private async Task InstallModules(InstallationModel model, SmartDbContext db, ILifetimeScope scope, CancellationToken cancelToken = default)
        {
            var idx = 0;
            var modularState = ModularState.Instance;
            var modules = modularState.IgnoredModules.Length == 0 
                ? _appContext.ModuleCatalog.Modules.ToArray()
                : _appContext.ModuleCatalog.Modules.Where(x => !modularState.IgnoredModules.Contains(x.SystemName, StringComparer.OrdinalIgnoreCase)).ToArray();

            modularState.InstalledModules.Clear();

            using var dbScope = new DbContextScope(db, minHookImportance: HookImportance.Essential, retainConnection: true);

            var installContext = new ModuleInstallationContext
            {
                ApplicationContext = _appContext,
                SeedSampleData = model.InstallSampleData,
                Culture = model.PrimaryLanguage,
                Stage = ModuleInstallationStage.AppInstallation,
                Logger = Logger
            };

            foreach (var module in modules)
            {
                try
                {
                    idx++;
                    UpdateResult(x =>
                    {
                        x.ProgressMessage = GetResource("Progress.InstallingModules").FormatInvariant(idx, modules.Length);
                        Logger.Info(x.ProgressMessage);
                    });

                    var moduleInstance = scope.Resolve<Func<IModuleDescriptor, IModule>>().Invoke(module);
                    installContext.ModuleDescriptor = module;
                    await moduleInstance.InstallAsync(installContext);
                    await dbScope.CommitAsync(cancelToken);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    modularState.InstalledModules.Remove(module.SystemName);
                }
            }
        }

        private void CheckFileSystemAccessRights(List<string> errors)
        {
            foreach (var subpath in FilePermissionChecker.WrittenDirectories)
            {
                var entry = _appContext.ContentRoot.GetDirectory(subpath);
                if (entry.Exists && !_filePermissionChecker.CanAccess(entry, FileEntryRights.Write | FileEntryRights.Modify))
                {
                    errors.Add(string.Format(GetResource("ConfigureDirectoryPermissions"), _appContext.OSIdentity.Name, entry.PhysicalPath));
                }
            }

            foreach (var subpath in FilePermissionChecker.WrittenFiles)
            {
                var entry = _appContext.ContentRoot.GetFile(subpath);
                if (entry.Exists && !_filePermissionChecker.CanAccess(entry, FileEntryRights.Write | FileEntryRights.Modify | FileEntryRights.Delete))
                {
                    errors.Add(string.Format(GetResource("ConfigureFilePermissions"), _appContext.OSIdentity.Name, entry.PhysicalPath));
                }
            }
        }

        private InstallationResult GetInstallResult()
        {
            var result = _asyncState.Get<InstallationResult>();
            if (result == null)
            {
                result = new InstallationResult();
                _asyncState.Create<InstallationResult>(result);
            }
            return result;
        }

        private InstallationResult UpdateResult(Action<InstallationResult> fn)
        {
            var result = GetInstallResult();
            fn(result);
            _asyncState.Create<InstallationResult>(result);

            return result;
        }

        #endregion

        #region Localization

        public virtual string GetResource(string resourceName)
        {
            Guard.NotEmpty(resourceName, nameof(resourceName));
            
            var language = GetCurrentLanguage();
            if (language == null)
                return resourceName;

            if (language.Resources.TryGetValue(resourceName, out var entry) && entry.Value.HasValue())
            {
                return entry.Value;
            }

            return resourceName;
        }

        public virtual InstallationLanguage GetCurrentLanguage()
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            var cookieLanguageCode = request?.Cookies[LanguageCookieName].EmptyNull();

            // Ensure it's available (it could be deleted since the previous installation)
            var installLanguages = GetInstallationLanguages();

            var language = installLanguages
                .Where(x => x.Code.EqualsNoCase(cookieLanguageCode))
                .FirstOrDefault();

            if (language != null)
                return language;

            // Try to resolve install language from CurrentCulture
            language = installLanguages.Where(MatchLanguageByCurrentCulture).FirstOrDefault();
            if (language != null)
                return language;

            // If we got here, the language code is not found. Let's return the default one
            language = installLanguages.Where(x => x.IsDefault).FirstOrDefault();
            if (language != null)
                return language;

            // Return any available language
            return installLanguages.FirstOrDefault();
        }

        private bool MatchLanguageByCurrentCulture(InstallationLanguage language)
        {
            var curCulture = Thread.CurrentThread.CurrentUICulture;

            if (language.Code.EqualsNoCase(curCulture.IetfLanguageTag))
                return true;

            curCulture = curCulture.Parent;
            if (curCulture != null)
            {
                return language.Code.EqualsNoCase(curCulture.IetfLanguageTag);
            }

            return false;
        }

        public virtual void SaveCurrentLanguage(string languageCode)
        {
            Guard.NotEmpty(languageCode, nameof(languageCode));
            
            var cookies = _httpContextAccessor.HttpContext?.Response?.Cookies;
            if (cookies == null)
            {
                return;
            }

            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(24),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };

            cookies.Delete(LanguageCookieName, options);
            cookies.Append(LanguageCookieName, languageCode, options);
        }

        public virtual IEnumerable<InstallationAppLanguageMetadata> GetAppLanguages()
        {
            foreach (var data in _seedDatas)
            {
                yield return data.Metadata;
            }
        }

        public Lazy<InvariantSeedData, InstallationAppLanguageMetadata> GetAppLanguage(string culture)
        {
            Guard.NotEmpty(culture, nameof(culture));
            return _seedDatas.FirstOrDefault(x => x.Metadata.Culture.EqualsNoCase(culture));
        }

        public IList<InstallationLanguage> GetInstallationLanguages()
        {
            if (_installLanguages == null)
            {
                _installLanguages = new List<InstallationLanguage>();

                foreach (var file in _appContext.AppDataRoot.EnumerateFiles("Localization/Installation", "*.xml"))
                {
                    var filePath = file.PhysicalPath;
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(filePath);

                    // Get language code
                    var languageCode = string.Empty;

                    // File name format: installation.{languagecode}.xml
                    var r = new Regex(Regex.Escape("installation.") + "(.*?)" + Regex.Escape(".xml"));
                    var matches = r.Matches(file.Name);
                    foreach (Match match in matches)
                    {
                        languageCode = match.Groups[1].Value;
                    } 

                    // Get language friendly name
                    var languageName = xmlDocument.SelectSingleNode(@"//Language").Attributes["Name"].InnerText.Trim();

                    // Is default
                    var isDefaultAttribute = xmlDocument.SelectSingleNode(@"//Language").Attributes["IsDefault"];
                    var isDefault = isDefaultAttribute != null && Convert.ToBoolean(isDefaultAttribute.InnerText.Trim());

                    // Is RTL
                    var isRightToLeftAttribute = xmlDocument.SelectSingleNode(@"//Language").Attributes["IsRightToLeft"];
                    var isRightToLeft = isRightToLeftAttribute != null && Convert.ToBoolean(isRightToLeftAttribute.InnerText.Trim());

                    // Create language
                    var language = new InstallationLanguage
                    {
                        Code = languageCode,
                        Name = languageName,
                        IsDefault = isDefault,
                        IsRightToLeft = isRightToLeft
                    };

                    // Load resources
                    foreach (XmlNode resNode in xmlDocument.SelectNodes(@"//Language/LocaleResource"))
                    {
                        var resNameAttribute = resNode.Attributes["Name"];
                        var resValueNode = resNode.SelectSingleNode("Value");

                        if (resNameAttribute == null)
                            throw new SmartException("All installation resources must have an attribute Name=\"Value\".");

                        var resourceName = resNameAttribute.Value.Trim();
                        if (string.IsNullOrEmpty(resourceName))
                            throw new SmartException("All installation resource attributes 'Name' must have a value.'");

                        if (resValueNode == null)
                            throw new SmartException("All installation resources must have an element \"Value\".");

                        var resourceValue = resValueNode.InnerText.Trim();
                        language.Resources[resourceName] = new InstallationLocaleResource
                        {
                            Name = resourceName,
                            Value = resourceValue
                        };
                    }

                    _installLanguages.Add(language);
                }

                _installLanguages = _installLanguages.OrderBy(x => x.Name).ToList();
            }

            return _installLanguages;
        }

        #endregion
    }
}
