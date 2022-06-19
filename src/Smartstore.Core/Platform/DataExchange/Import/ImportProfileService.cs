﻿using System.Diagnostics;
using Dasync.Collections;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.IO;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportProfileService : IImportProfileService
    {
        private const string IMPORT_FILE_ROOT = "ImportProfiles";
        private static readonly object _lock = new();
        private static Dictionary<ImportEntityType, Dictionary<string, string>> _entityProperties = null;

        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly DataExchangeSettings _dataExchangeSettings;
        private readonly ITaskStore _taskStore;

        public ImportProfileService(
            SmartDbContext db,
            IApplicationContext appContext,
            ILocalizationService localizationService,
            ILanguageService languageService,
            DataExchangeSettings dataExchangeSettings,
            ITaskStore taskStore)
        {
            _db = db;
            _appContext = appContext;
            _localizationService = localizationService;
            _languageService = languageService;
            _dataExchangeSettings = dataExchangeSettings;
            _taskStore = taskStore;
        }

        public virtual async Task<IDirectory> GetImportDirectoryAsync(ImportProfile profile, string subpath = null, bool createIfNotExists = false)
        {
            Guard.NotNull(profile, nameof(profile));

            var root = _appContext.TenantRoot;
            var path = root.PathCombine(IMPORT_FILE_ROOT, profile.FolderName, subpath.EmptyNull());
            var dir = await root.GetDirectoryAsync(path);

            if (createIfNotExists)
            {
                await dir.CreateAsync();
            }

            return dir;
        }

        public virtual async Task<IList<ImportFile>> GetImportFilesAsync(ImportProfile profile, bool includeRelatedFiles = true)
        {
            var result = new List<ImportFile>();
            var directory = await GetImportDirectoryAsync(profile, "Content");

            if (directory.Exists)
            {
                var files = directory.EnumerateFiles();
                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        var importFile = new ImportFile(file);
                        if (!includeRelatedFiles && importFile.RelatedType.HasValue)
                        {
                            continue;
                        }

                        result.Add(importFile);
                    }

                    // Always main data files first.
                    result = result
                        .OrderBy(x => x.RelatedType)
                        .ThenBy(x => x.File.Name)
                        .ToList();
                }
            }

            return result;
        }

        public virtual async Task<string> GetNewProfileNameAsync(ImportEntityType entityType)
        {
            var defaultNamesStr = await _localizationService.GetResourceAsync("Admin.DataExchange.Import.DefaultProfileNames");
            var defaultNames = defaultNamesStr.SplitSafe(';').ToArray();
            var profileCount = 1 + await _db.ImportProfiles.CountAsync(x => x.EntityTypeId == (int)entityType);

            var result = defaultNames.ElementAtOrDefault((int)entityType).NullEmpty() ?? entityType.ToString();
            return result + " " + profileCount.ToString();
        }

        public virtual async Task<ImportProfile> InsertImportProfileAsync(string fileName, string name, ImportEntityType entityType)
        {
            Guard.NotEmpty(fileName, nameof(fileName));

            if (name.IsEmpty())
            {
                name = await GetNewProfileNameAsync(entityType);
            }

            var task = _taskStore.CreateDescriptor(name + " Task", typeof(DataImportTask));
            task.Enabled = false;
            task.CronExpression = "0 */24 * * *"; // Every 24 hours
            task.StopOnError = false;
            task.IsHidden = true;

            await _taskStore.InsertTaskAsync(task);

            var profile = new ImportProfile
            {
                Name = name,
                EntityType = entityType,
                Enabled = true,
                TaskId = task.Id,
                FileType = Path.GetExtension(fileName).EqualsNoCase(".xlsx")
                    ? ImportFileType.Xlsx
                    : ImportFileType.Csv
            };

            switch (entityType)
            {
                case ImportEntityType.Product:
                    profile.KeyFieldNames = string.Join(",", ProductImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.Category:
                    profile.KeyFieldNames = string.Join(",", CategoryImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.Customer:
                    profile.KeyFieldNames = string.Join(",", CustomerImporter.DefaultKeyFields);
                    break;
                case ImportEntityType.NewsletterSubscription:
                    profile.KeyFieldNames = string.Join(",", NewsletterSubscriptionImporter.DefaultKeyFields);
                    break;
            }

            var folderName = SeoHelper.BuildSlug(name, true, false, false)
                .ToValidPath()
                .Truncate(_dataExchangeSettings.MaxFileNameLength);

            profile.FolderName = _appContext.TenantRoot.CreateUniqueDirectoryName(IMPORT_FILE_ROOT, folderName);

            _db.ImportProfiles.Add(profile);

            // Get the import profile ID.
            await _db.SaveChangesAsync();

            task.Alias = profile.Id.ToString();

            // INFO: (mc) (core) Isolation --> always use ITaskStore to interact with task storage (which is DB in our case by default, but can be memory in future).
            await _taskStore.UpdateTaskAsync(task);

            // Finally update export profile.
            await _db.SaveChangesAsync();

            return profile;
        }

        public virtual async Task DeleteImportProfileAsync(ImportProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            await _db.LoadReferenceAsync(profile, x => x.Task);

            var directory = await GetImportDirectoryAsync(profile);

            _db.ImportProfiles.Remove(profile);

            await _db.SaveChangesAsync();

            if (profile.Task != null)
            {
                await _taskStore.DeleteTaskAsync(profile.Task);
            }

            if (directory.Exists)
            {
                directory.FileSystem.ClearDirectory(directory, true, TimeSpan.Zero);
            }
        }

        public virtual async Task<int> DeleteUnusedImportDirectoriesAsync()
        {
            var numFolders = 0;
            var tenantRoot = _appContext.TenantRoot;

            var importProfileFolders = await _db.ImportProfiles
                .ApplyStandardFilter()
                .Select(x => x.FolderName)
                .ToListAsync();

            var dir = await tenantRoot.GetDirectoryAsync(tenantRoot.PathCombine(IMPORT_FILE_ROOT));
            if (dir.Exists)
            {
                foreach (var subdir in dir.EnumerateDirectories())
                {
                    if (!importProfileFolders.Contains(subdir.Name))
                    {
                        dir.FileSystem.ClearDirectory(subdir, true, TimeSpan.Zero);
                        numFolders++;
                    }
                }
            }

            return numFolders;
        }

        #region Localized entity properties labels

        public virtual IDictionary<string, string> GetEntityPropertiesLabels(ImportEntityType entityType)
        {
            if (_entityProperties == null)
            {
                lock (_lock)
                {
                    if (_entityProperties == null)
                    {
                        _entityProperties = new Dictionary<ImportEntityType, Dictionary<string, string>>();

                        var allLanguages = _languageService.GetAllLanguages(true);
                        var allLanguageNames = allLanguages.ToDictionarySafe(x => x.UniqueSeoCode, x => CultureHelper.GetLanguageNativeName(x.LanguageCulture) ?? x.Name);

                        var localizableProperties = new Dictionary<ImportEntityType, string[]>
                        {
                            { ImportEntityType.Product, new[] { "Name", "ShortDescription", "FullDescription", "MetaKeywords", "MetaDescription", "MetaTitle", "SeName" } },
                            { ImportEntityType.Category, new[] { "Name", "FullName", "Description", "BottomDescription", "MetaKeywords", "MetaDescription", "MetaTitle", "SeName" } },
                            { ImportEntityType.Customer, Array.Empty<string>() },
                            { ImportEntityType.NewsletterSubscription, Array.Empty<string>() }
                        };

                        // There is no 'FindEntityTypeByDisplayName'.
                        var allEntityTypes = _db.Model.GetEntityTypes().ToDictionarySafe(x => x.DisplayName(), x => x, StringComparer.OrdinalIgnoreCase);

                        var addressProperties = allEntityTypes.Get(nameof(Address))?.GetProperties()
                            .Where(x => x.Name != "Id")
                            .Select(x => x.Name)
                            .ToList();

                        foreach (ImportEntityType importType in Enum.GetValues(typeof(ImportEntityType)))
                        {
                            if (!allEntityTypes.TryGetValue(importType.ToString(), out var efType) || efType == null)
                            {
                                throw new SmartException($"There is no entity set for ImportEntityType {importType}. Note, the enum value must equal the entity name.");
                            }

                            var names = efType.GetProperties()
                                .Where(x => x.Name != "Id")
                                .Select(x => x.Name)
                                .ToDictionarySafe(x => x, x => string.Empty, StringComparer.OrdinalIgnoreCase);

                            // Add property names missing for column mapping.
                            switch (importType)
                            {
                                case ImportEntityType.Product:
                                    if (!names.ContainsKey("SeName"))
                                        names["SeName"] = string.Empty;
                                    names["Specification"] = string.Empty;
                                    break;
                                case ImportEntityType.Category:
                                    if (!names.ContainsKey("SeName"))
                                        names["SeName"] = string.Empty;
                                    break;
                                case ImportEntityType.Customer:
                                    foreach (var property in addressProperties)
                                    {
                                        names["BillingAddress." + property] = string.Empty;
                                        names["ShippingAddress." + property] = string.Empty;
                                    }
                                    break;
                            }

                            // Add localized property names.
                            foreach (var key in names.Keys.ToList())
                            {
                                var localizedValue = GetLocalizedPropertyLabel(importType, key).NaIfEmpty();
                                names[key] = localizedValue;

                                if (localizableProperties[importType].Contains(key))
                                {
                                    foreach (var language in allLanguages)
                                    {
                                        names[$"{key}[{language.UniqueSeoCode.EmptyNull().ToLower()}]"] =
                                            $"{localizedValue} {allLanguageNames[language.UniqueSeoCode]}";
                                    }
                                }
                            }

                            _entityProperties[importType] = names;
                        }
                    }
                }
            }

            return _entityProperties.ContainsKey(entityType) ? _entityProperties[entityType] : null;
        }

        private string GetLocalizedPropertyLabel(ImportEntityType entityType, string property)
        {
            if (property.IsEmpty())
            {
                return string.Empty;
            }

            string key = null;
            string prefixKey = null;

            if (property.StartsWith("BillingAddress."))
            {
                prefixKey = "Admin.Orders.Fields.BillingAddress";
            }
            else if (property.StartsWith("ShippingAddress."))
            {
                prefixKey = "Admin.Orders.Fields.ShippingAddress";
            }

            switch (entityType)
            {
                case ImportEntityType.Product:
                    key = "Admin.Catalog.Products.Fields." + property;
                    break;
                case ImportEntityType.Category:
                    key = "Admin.Catalog.Categories.Fields." + property;
                    break;
                case ImportEntityType.Customer:
                    key = (property.StartsWith("BillingAddress.") || property.StartsWith("ShippingAddress."))
                        ? "Address.Fields." + property.Substring(property.IndexOf('.') + 1)
                        : "Admin.Customers.Customers.Fields." + property;
                    break;
                case ImportEntityType.NewsletterSubscription:
                    key = "Admin.Promotions.NewsletterSubscriptions.Fields." + property;
                    break;
            }

            if (key.IsEmpty())
            {
                return string.Empty;
            }

            var result = _localizationService.GetResource(key, 0, false, string.Empty, true);

            if (result.IsEmpty() && _otherResourceKeys.TryGetValue(property, out var otherKey))
            {
                result = _localizationService.GetResource(otherKey, 0, false, string.Empty, true);
            }

            if (result.IsEmpty())
            {
                if (key.EndsWith("Id"))
                {
                    result = _localizationService.GetResource(key.Substring(0, key.Length - 2), 0, false, string.Empty, true);
                }
                else if (key.EndsWith("Utc"))
                {
                    result = _localizationService.GetResource(key.Substring(0, key.Length - 3), 0, false, string.Empty, true);
                }
            }

            if (result.IsEmpty())
            {
                Debug.WriteLine($"Missing string resource mapping for {entityType} - {property}");
                result = property.SplitPascalCase();
            }

            if (prefixKey.HasValue())
            {
                result = _localizationService.GetResource(prefixKey, 0, false, string.Empty, true) + " - " + result;
            }

            return result;
        }

        private static readonly Dictionary<string, string> _otherResourceKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "Admin.Common.Entity.Fields.Id" },
            { "DisplayOrder", "Common.DisplayOrder" },
            { "HomePageDisplayOrder", "Common.DisplayOrder" },
            { "Deleted", "Admin.Common.Deleted" },
            { "WorkingLanguageId", "Common.Language" },
            { "CreatedOnUtc", "Common.CreatedOn" },
            { "BillingAddress.CreatedOnUtc", "Common.CreatedOn" },
            { "ShippingAddress.CreatedOnUtc", "Common.CreatedOn" },
            { "UpdatedOnUtc", "Common.UpdatedOn" },
            { "HasDiscountsApplied", "Admin.Catalog.Products.Fields.HasDiscountsApplied" },
            { "DefaultViewMode", "Admin.Configuration.Settings.Catalog.DefaultViewMode" },
            { "StoreId", "Admin.Common.Store" },
            { "LimitedToStores", "Admin.Common.Store.LimitedTo" },
            { "SubjectToAcl", "Admin.Common.CustomerRole.LimitedTo" },
            { "ParentGroupedProductId", "Admin.Catalog.Products.Fields.AssociatedToProductName" },
            { "PasswordFormatId", "Admin.Configuration.Settings.CustomerUser.DefaultPasswordFormat" },
            { "LastIpAddress", "Admin.Customers.Customers.Fields.IPAddress" },
            { "CustomerNumber", "Account.Fields.CustomerNumber" },
            { "BirthDate", "Admin.Customers.Customers.Fields.DateOfBirth" },
            { "Salutation", "Address.Fields.Salutation" },
            { "VisibleIndividually", "Admin.Catalog.Products.Fields.Visibility" },
            { "IsShippingEnabled", "Admin.Catalog.Products.Fields.IsShipEnabled" },
            { "MetaDescription", "Admin.Configuration.Seo.MetaDescription" },
            { "MetaKeywords", "Admin.Configuration.Seo.MetaKeywords" },
            { "MetaTitle", "Admin.Configuration.Seo.MetaTitle" },
            { "SeName", "Admin.Configuration.Seo.SeName" },
            { "TaxDisplayTypeId", "Admin.Customers.CustomerRoles.Fields.TaxDisplayType" }
        };

        #endregion
    }
}
