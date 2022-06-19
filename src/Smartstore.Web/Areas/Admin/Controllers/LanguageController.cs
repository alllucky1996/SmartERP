﻿using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Xml;
using Autofac;
using Dasync.Collections;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Localization;
using Smartstore.ComponentModel;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Threading;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class LanguageController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IXmlResourceManager _xmlResourceManager;
        private readonly IAsyncState _asyncState;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AsyncRunner _asyncRunner;

        public LanguageController(
            SmartDbContext db,
            ILanguageService languageService,
            IStoreMappingService storeMappingService,
            IModuleCatalog moduleCatalog,
            IXmlResourceManager xmlResourceManager,
            IAsyncState asyncState,
            IHttpClientFactory httpClientFactory,
            AsyncRunner asyncRunner)
        {
            _db = db;
            _languageService = languageService;
            _storeMappingService = storeMappingService;
            _moduleCatalog = moduleCatalog;
            _xmlResourceManager = xmlResourceManager;
            _asyncState = asyncState;
            _httpClientFactory = httpClientFactory;
            _asyncRunner = asyncRunner;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> List()
        {
            var lastImportInfos = await GetLastResourcesImportInfos();
            var languages = _languageService.GetAllLanguages(true);
            var masterLanguageId = _languageService.GetMasterLanguageId();
            var mapper = MapperFactory.GetMapper<Language, LanguageModel>();

            var models = await languages.SelectAsync(async x =>
            {
                var m = await mapper.MapAsync(x);
                m.Name = GetCultureDisplayName(x.LanguageCulture) ?? x.Name;

                if (lastImportInfos.TryGetValue(x.Id, out LastResourcesImportInfo info))
                {
                    m.LastResourcesImportOn = info.ImportedOn;
                    m.LastResourcesImportOnString = info.ImportedOn.Humanize(true);
                }

                if (x.Id == masterLanguageId)
                {
                    ViewBag.DefaultLanguageNote = T("Admin.Configuration.Languages.DefaultLanguage.Note", m.Name);
                }

                return m;
            })
            .AsyncToList();

            return View(models);
        }

        public async Task<IActionResult> LanguageSelected(int customerlanguage)
        {
            var language = await _db.Languages.FindByIdAsync(customerlanguage, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            return Content(T("Admin.Common.DataEditSuccess"));
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> AvailableLanguages(bool enforce = false)
        {
            var languages = _languageService.GetAllLanguages(true);
            var languageDic = languages.ToDictionarySafe(x => x.LanguageCulture, StringComparer.OrdinalIgnoreCase);

            var downloadState = await _asyncState.GetAsync<LanguageDownloadState>();
            var lastImportInfos = await GetLastResourcesImportInfos();
            var checkResult = await CheckAvailableResources(enforce);

            var model = new AvailableLanguageListModel
            {
                Version = checkResult.Version,
                ResourceCount = checkResult.ResourceCount
            };

            foreach (var resources in checkResult.Resources)
            {
                if (resources.Language.Culture.HasValue())
                {
                    languageDic.TryGetValue(resources.Language.Culture, out Language language);

                    var alModel = new AvailableLanguageModel();
                    PrepareAvailableLanguageModel(alModel, resources, lastImportInfos, language, downloadState);

                    model.Languages.Add(alModel);
                }
            }

            return PartialView(model);
        }

        [Permission(Permissions.Configuration.Language.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new LanguageModel();
            await PrepareLanguageModel(model, null, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Language.Create)]
        public async Task<IActionResult> Create(LanguageModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var language = await MapperFactory.MapAsync<LanguageModel, Language>(model);

                _db.Languages.Add(language);
                await _db.SaveChangesAsync();

                await SaveStoreMappingsAsync(language, model.SelectedStoreIds);

                var filterLanguages = new List<Language> { language };
                var modules = _moduleCatalog.GetInstalledModules();

                foreach (var module in modules)
                {
                    await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(module, null, false, filterLanguages);
                }

                NotifySuccess(T("Admin.Configuration.Languages.Added"));

                return continueEditing 
                    ? RedirectToAction(nameof(Edit), new { id = language.Id }) 
                    : RedirectToAction(nameof(List));
            }

            await PrepareLanguageModel(model, null, true);

            return View(model);
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Language, LanguageModel>(language);

            await PrepareLanguageModel(model, language, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Language.Update)]
        public async Task<IActionResult> Edit(LanguageModel model, bool continueEditing)
        {
            var language = await _db.Languages.FindByIdAsync(model.Id);
            if (language == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Ensure we have at least one published language.
                var allLanguages = _languageService.GetAllLanguages();
                if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id && !model.Published)
                {
                    NotifyError(T("Admin.Configuration.Languages.OnePublishedLanguageRequired"));
                    
                    return RedirectToAction(nameof(Edit), new { id = language.Id });
                }

                await MapperFactory.MapAsync(model, language);
                await _storeMappingService.ApplyStoreMappingsAsync(language, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Languages.Updated"));

                return continueEditing 
                    ? RedirectToAction(nameof(Edit), new { id = language.Id }) 
                    : RedirectToAction(nameof(List));
            }

            await PrepareLanguageModel(model, language, true);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }

            // Ensure we have at least one published language
            var allLanguages = _languageService.GetAllLanguages();
            if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id)
            {
                NotifyError(T("Admin.Configuration.Languages.OnePublishedLanguageRequired"));

                return RedirectToAction(nameof(Edit), new { id = language.Id });
            }

            _db.Languages.Remove(language);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Configuration.Languages.Deleted"));

            return RedirectToAction(nameof(List));
        }

        #region Resources

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> Resources(int languageId)
        {
            var language = await _db.Languages.FindByIdAsync(languageId);
            if (language == null)
            {
                return NotFound();
            }

            ViewBag.AllLanguages = _languageService.GetAllLanguages(true)
                .Select(x => new SelectListItem
                {
                    Selected = x.Id.Equals(languageId),
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();

            var model = new LanguageResourceListModel
            {
                LanguageId = language.Id,
                LanguageName = language.Name
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> LocaleStringResourceList(GridCommand command, LanguageResourceListModel model)
        {
            var language = await _db.Languages.FindByIdAsync(model.LanguageId);
            if (language == null)
            {
                return NotFound();
            }

            var query = _db.LocaleStringResources
                .AsNoTracking()
                .Where(x => x.LanguageId == language.Id);

            if (model.ResourceName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.ResourceName, model.ResourceName);
            }

            if (model.ResourceValue.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.ResourceValue, model.ResourceValue);
            }

            var resources = await query
                .OrderBy(x => x.ResourceName)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = resources
                .AsQueryable()
                .Select(x => new LanguageResourceModel
                {
                    Id = x.Id,
                    LanguageId = language.Id,
                    LanguageName = language.Name,
                    ResourceName = x.ResourceName,
                    ResourceValue = x.ResourceValue.EmptyNull(),
                })
                .ToList();

            return Json(new GridModel<LanguageResourceModel>
            {
                Rows = rows,
                Total = resources.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceUpdate(LanguageResourceModel model)
        {
            var success = true;

            model.ResourceName = model.ResourceName.TrimSafe();
            model.ResourceValue = model.ResourceValue.TrimSafe();

            if (ModelState.IsValid)
            {
                var resource = await _db.LocaleStringResources.FindByIdAsync(model.Id);

                // If resourceName changed, ensure it is not being used by another resource.
                if (!resource.ResourceName.EqualsNoCase(model.ResourceName))
                {
                    var resource2 = await _db.LocaleStringResources
                        .AsNoTracking()
                        .Where(x => x.LanguageId == model.LanguageId && x.ResourceName == model.ResourceName)
                        .FirstOrDefaultAsync();

                    if (resource2 != null && resource2.Id != resource.Id)
                    {
                        success = false;
                        NotifyError(T("Admin.Configuration.Languages.Resources.NameAlreadyExists", resource2.ResourceName));
                    }
                }

                if (success)
                {
                    resource.ResourceName = model.ResourceName;
                    resource.ResourceValue = model.ResourceValue;
                    resource.IsTouched = true;

                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                success = false;
                ModelState.Values.SelectMany(x => x.Errors).Each(x => NotifyError(x.ErrorMessage));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceInsert(LanguageResourceModel model, int languageId)
        {
            var success = true;

            model.ResourceName = model.ResourceName.TrimSafe();
            model.ResourceValue = model.ResourceValue.TrimSafe();

            if (ModelState.IsValid)
            {
                if (!await _db.LocaleStringResources.AnyAsync(x => x.LanguageId == languageId && x.ResourceName == model.ResourceName))
                {
                    _db.LocaleStringResources.Add(new LocaleStringResource
                    {
                        LanguageId = languageId,
                        ResourceName = model.ResourceName,
                        ResourceValue = model.ResourceValue,
                        IsTouched = true
                    });

                    await _db.SaveChangesAsync();
                }
                else
                {
                    success = false;
                    NotifyError(T("Admin.Configuration.Languages.Resources.NameAlreadyExists", model.ResourceName));
                }
            }
            else
            {
                success = false;
                ModelState.Values.SelectMany(x => x.Errors).Each(x => NotifyError(x.ErrorMessage));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceDelete(GridSelection selection)
        {
            var success = false;
            var num = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var resources = await _db.LocaleStringResources.GetManyAsync(ids, true);

                _db.LocaleStringResources.RemoveRange(resources);

                num = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = num });
        }

        #endregion

        #region Export / Import / Download

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> ExportXml(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id, false);
            if (language == null)
            {
                return NotFound();
            }

            try
            {
                var xml = await _xmlResourceManager.ExportResourcesToXmlAsync(language);

                return new XmlDownloadResult(xml, $"language-pack-{language.UniqueSeoCode}.xml");
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<ActionResult> ImportXml(int id, ImportModeFlags mode, bool updateTouched, int? availableLanguageSetId)
        {
            var language = await _db.Languages.FindByIdAsync(id, false);
            if (language == null)
            {
                return NotFound();
            }

            try
            {
                var file = Request.Form.Files["importxmlfile"];

                if (file != null && file.Length > 0)
                {
                    using var stream = file.OpenReadStream();
                    var xml = await stream.AsStringAsync();

                    await _xmlResourceManager.ImportResourcesFromXmlAsync(language, xml, null, false, mode, updateTouched);

                    NotifySuccess(T("Admin.Configuration.Languages.Imported"));
                }
                else if (availableLanguageSetId > 0)
                {
                    var checkResult = await CheckAvailableResources();
                    var availableResources = checkResult.Resources.First(x => x.Id == availableLanguageSetId.Value);

                    var client = _httpClientFactory.CreateClient();
                    var xmlDoc = await DownloadAvailableResources(client, availableResources.DownloadUrl, Services.StoreContext.CurrentStore.Url);

                    await _xmlResourceManager.ImportResourcesFromXmlAsync(language, xmlDoc, null, false, mode, updateTouched);

                    var serializedImportInfo = JsonConvert.SerializeObject(new LastResourcesImportInfo
                    {
                        TranslatedPercentage = availableResources.TranslatedPercentage,
                        ImportedOn = DateTime.UtcNow
                    });

                    language.GenericAttributes.Set("LastResourcesImportInfo", serializedImportInfo);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Languages.Imported"));
                }
                else
                {
                    NotifyError(T("Admin.Configuration.Languages.UploadFileOrSelectLanguage"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                Logger.ErrorsAll(ex);
            }

            return RedirectToAction("Edit", new { id = language.Id });
        }

        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> Download(int setId)
        {
            var ctx = new LanguageDownloadContext(setId)
            {
                AvailableResources = await CheckAvailableResources(),
                AppShutdownCancellationToken = _asyncRunner.AppShutdownCancellationToken
            };

            if (ctx.AvailableResources.Resources.Any())
            {
                _ = _asyncRunner.Run((scope, ct, state) => DownloadCore(scope, state as LanguageDownloadContext, ct), ctx);
            }
            
            return RedirectToAction(nameof(List));
        }

        private static void DownloadCore(ILifetimeScope scope, LanguageDownloadContext context, CancellationToken cancelToken)
        {
            var asyncState = scope.Resolve<IAsyncState>();
            var httpClientFactory = scope.Resolve<IHttpClientFactory>();
            var services = scope.Resolve<ICommonServices>();
            var xmlResourceManager = scope.Resolve<IXmlResourceManager>();
            var logger = scope.Resolve<ILogger>();
            var db = services.DbContext;
            var loc = services.Localization;

            try
            {
                // 1. Download resources.
                var importResourcesString = loc.GetResource("Admin.Configuration.Languages.ImportResources");
                var cts = CancellationTokenSource.CreateLinkedTokenSource(context.AppShutdownCancellationToken, cancelToken);

                var state = new LanguageDownloadState
                {
                    Id = context.SetId,
                    ProgressMessage = loc.GetResource("Admin.Configuration.Languages.DownloadingResources")
                };

                asyncState.Create(state, null, false, cts);

                var client = httpClientFactory.CreateClient();
                var resources = context.AvailableResources.Resources.First(x => x.Id == context.SetId);
                var xmlDoc = DownloadAvailableResources(client, resources.DownloadUrl, services.StoreContext.CurrentStore.Url, cancelToken).Await();

                if (!cts.Token.IsCancellationRequested)
                {
                    asyncState.Update<LanguageDownloadState>(state => state.ProgressMessage = importResourcesString);

                    // 2. Create language entity (if required).
                    var language = db.Languages
                        .Where(x => x.LanguageCulture == resources.Language.Culture)
                        .FirstOrDefault();

                    if (language == null)
                    {
                        var maxDisplayOrder = db.Languages
                            .Where(x => x.Published)
                            .Max(x => (int?)x.DisplayOrder);

                        language = new Language
                        {
                            LanguageCulture = resources.Language.Culture,
                            UniqueSeoCode = resources.Language.TwoLetterIsoCode,
                            Name = GetCultureDisplayName(resources.Language.Culture) ?? resources.Name,
                            FlagImageFileName = GetFlagFileName(resources.Language.Culture, services.ApplicationContext),
                            Rtl = resources.Language.Rtl,
                            Published = false,
                            DisplayOrder = maxDisplayOrder.HasValue ? maxDisplayOrder.Value + 1 : 0
                        };

                        db.Languages.Add(language);
                        db.SaveChanges();
                    }

                    // 3. Import resources.
                    xmlResourceManager.ImportResourcesFromXmlAsync(language, xmlDoc).Await();

                    var serializedImportInfo = JsonConvert.SerializeObject(new LastResourcesImportInfo
                    {
                        TranslatedPercentage = resources.TranslatedPercentage,
                        ImportedOn = DateTime.UtcNow
                    });

                    language.GenericAttributes.Set("LastResourcesImportInfo", serializedImportInfo);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logger.ErrorsAll(ex);
            }
            finally
            {
                if (asyncState.Contains<LanguageDownloadState>())
                {
                    asyncState.Remove<LanguageDownloadState>();
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadProgress()
        {
            var state = await _asyncState.GetAsync<LanguageDownloadState>();
            if (state != null)
            {
                var progressInfo = new
                {
                    id = state.Id,
                    percent = state.ProgressPercent,
                    message = state.ProgressMessage
                };

                return Json(new object[] { progressInfo });
            }

            return Json(new EmptyResult());
        }

        #endregion

        private async Task<CheckAvailableResourcesResult> CheckAvailableResources(bool enforce = false)
        {
            var cacheKey = "admin:language:checkavailableresourcesresult";
            var currentVersion = SmartstoreVersion.CurrentFullVersion;
            CheckAvailableResourcesResult result = null;
            string jsonString = null;

            if (!enforce)
            {
                jsonString = HttpContext.Session.GetString(cacheKey);
            }

            if (jsonString == null)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    
                    client.Timeout = TimeSpan.FromMilliseconds(10000);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                    client.DefaultRequestHeaders.Add("Authorization-Key", Services.StoreContext.CurrentStore.Url.EmptyNull().TrimEnd('/'));

                    var url = Services.ApplicationContext.AppConfiguration.TranslateCheckUrl.FormatInvariant(currentVersion);
                    var response = await client.GetAsync(url);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        jsonString = await response.Content.ReadAsStringAsync();
                        HttpContext.Session.SetString(cacheKey, jsonString);
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
                    Logger.ErrorsAll(ex);
                }
            }

            if (jsonString.HasValue())
            {
                result = JsonConvert.DeserializeObject<CheckAvailableResourcesResult>(jsonString);
            }

            return result ?? new CheckAvailableResourcesResult();
        }

        private static async Task<XmlDocument> DownloadAvailableResources(
            HttpClient client, 
            string downloadUrl, 
            string storeUrl,
            CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(downloadUrl, nameof(downloadUrl));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.Xml));
            client.DefaultRequestHeaders.Add("Authorization-Key", storeUrl.EmptyNull().TrimEnd('/'));

            using var inStream = await client.GetStreamAsync(downloadUrl, cancelToken);
            var document = new XmlDocument();
            document.Load(inStream);

            return document;
        }

        private async Task<Dictionary<int, LastResourcesImportInfo>> GetLastResourcesImportInfos()
        {
            Dictionary<int, LastResourcesImportInfo> result = null;

            try
            {
                var attributes = await _db.GenericAttributes
                    .AsNoTracking()
                    .Where(x => x.Key == "LastResourcesImportInfo" && x.KeyGroup == "Language")
                    .ToListAsync();

                result = attributes.ToDictionarySafe(x => x.EntityId, x => JsonConvert.DeserializeObject<LastResourcesImportInfo>(x.Value));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result ?? new Dictionary<int, LastResourcesImportInfo>();
        }

        private async Task PrepareLanguageModel(LanguageModel model, Language language, bool excludeProperties)
        {
            var twoLetterLanguageCodes = new List<SelectListItem>();
            var countryFlags = new List<SelectListItem>();
            var lastImportInfos = await GetLastResourcesImportInfos();

            var allCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .OrderBy(x => x.DisplayName)
                .ToList();

            ViewBag.Cultures = allCultures
                .Select(x => new SelectListItem { Text = $"{x.DisplayName} [{x.IetfLanguageTag}]", Value = x.IetfLanguageTag })
                .ToList();

            // Get two-letter language codes.
            foreach (var culture in allCultures)
            {
                if (!twoLetterLanguageCodes.Any(x => x.Value.EqualsNoCase(culture.TwoLetterISOLanguageName)))
                {
                    // Display language name is not provided by net framework.
                    var index = culture.DisplayName.EmptyNull().IndexOf(" (");

                    if (index == -1)
                    {
                        index = culture.DisplayName.EmptyNull().IndexOf(" [");
                    }

                    var displayName = "{0} [{1}]".FormatInvariant(
                        index == -1 ? culture.DisplayName : culture.DisplayName[..index],
                        culture.TwoLetterISOLanguageName);

                    if (culture.TwoLetterISOLanguageName.Length == 2)
                    {
                        twoLetterLanguageCodes.Add(new SelectListItem { Text = displayName, Value = culture.TwoLetterISOLanguageName });
                    }
                }
            }

            ViewBag.TwoLetterLanguageCodes = twoLetterLanguageCodes;

            // Get country flags.
            var allCountries = await _db.Countries
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var allCountryNames = allCountries.ToDictionarySafe(x =>
                x.TwoLetterIsoCode.EmptyNull().ToLower(),
                x => x.GetLocalized(y => y.Name, Services.WorkContext.WorkingLanguage, true, false));

            var flagsDir = await Services.ApplicationContext.WebRoot.GetDirectoryAsync("images/flags");
            var flags = await flagsDir.EnumerateFilesAsync().ToListAsync();

            foreach (var flag in flags)
            {
                var name = flag.NameWithoutExtension.EmptyNull().ToLower();
                string countryDescription = null;

                if (allCountryNames.ContainsKey(name))
                {
                    countryDescription = $"{allCountryNames[name]} [{name}]";
                }

                if (countryDescription.IsEmpty())
                {
                    countryDescription = name;
                }

                countryFlags.Add(new SelectListItem { Text = countryDescription, Value = flag.Name });
            }

            ViewBag.CountryFlags = countryFlags.OrderBy(x => x.Text).ToList();

            if (language != null)
            {
                if (!excludeProperties)
                {
                    model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(language);
                }

                if (lastImportInfos.TryGetValue(language.Id, out LastResourcesImportInfo info))
                {
                    model.LastResourcesImportOn = info.ImportedOn;
                    model.LastResourcesImportOnString = info.ImportedOn.Humanize(true);
                }

                // Provide downloadable resources.
                var checkResult = await CheckAvailableResources();
                string cultureParentName = null;

                try
                {
                    var ci = CultureInfo.GetCultureInfo(language.LanguageCulture);
                    if (!ci.IsNeutralCulture && ci.Parent != null)
                    {
                        cultureParentName = ci.Parent.Name;
                    }
                }
                catch { }

                ViewBag.DownloadableLanguages = checkResult.Resources
                    .Where(x => x.Published)
                    .Select(x =>
                    {
                        var srcCulture = x.Language.Culture;
                        if (srcCulture.HasValue())
                        {
                            var downloadDisplayOrder = srcCulture.EqualsNoCase(language.LanguageCulture) ? 1 : 0;

                            if (downloadDisplayOrder == 0 && cultureParentName.EqualsNoCase(srcCulture))
                            {
                                downloadDisplayOrder = 2;
                            }

                            if (downloadDisplayOrder == 0 && x.Language.TwoLetterIsoCode.EqualsNoCase(language.UniqueSeoCode))
                            {
                                downloadDisplayOrder = 3;
                            }

                            if (downloadDisplayOrder != 0)
                            {
                                var alModel = new AvailableLanguageModel();
                                PrepareAvailableLanguageModel(alModel, x, lastImportInfos, language);
                                alModel.DisplayOrder = downloadDisplayOrder;

                                return alModel;
                            }
                        }

                        return null;
                    })
                    .Where(x => x != null)
                    .ToList();
            }
        }

        private void PrepareAvailableLanguageModel(
            AvailableLanguageModel model,
            AvailableResourcesModel resources,
            Dictionary<int, LastResourcesImportInfo> lastImportInfos,
            Language language = null,
            LanguageDownloadState state = null)
        {
            // Source Id (aka SetId), not entity Id!
            model.Id = resources.Id;
            model.PreviousSetId = resources.PreviousSetId;
            model.IsInstalled = language != null;
            model.Name = GetCultureDisplayName(resources.Language.Culture) ?? resources.Language.Name;
            model.LanguageCulture = resources.Language.Culture;
            model.UniqueSeoCode = resources.Language.TwoLetterIsoCode;
            model.Rtl = resources.Language.Rtl;
            model.Version = resources.Version;
            model.Type = resources.Type;
            model.Published = resources.Published;
            model.DisplayOrder = resources.DisplayOrder;
            model.TranslatedCount = resources.TranslatedCount;
            model.TranslatedPercentage = resources.TranslatedPercentage;
            model.IsDownloadRunning = state != null && state.Id == resources.Id;
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(resources.UpdatedOn, DateTimeKind.Utc);
            model.UpdatedOnString = model.UpdatedOn.Humanize(false);
            model.FlagImageFileName = GetFlagFileName(resources.Language.Culture, Services.ApplicationContext);

            if (language != null && lastImportInfos.TryGetValue(language.Id, out LastResourcesImportInfo info))
            {
                // Only show percent at last import if it's less than the current percentage.
                var percentAtLastImport = Math.Round(info.TranslatedPercentage, 2);
                if (percentAtLastImport < model.TranslatedPercentage)
                {
                    model.TranslatedPercentageAtLastImport = percentAtLastImport;
                }

                model.LastResourcesImportOn = info.ImportedOn;
                model.LastResourcesImportOnString = info.ImportedOn.Humanize(true);
            }
        }

        private static string GetCultureDisplayName(string culture)
        {
            if (culture.HasValue())
            {
                try
                {
                    return new CultureInfo(culture).DisplayName;
                }
                catch
                {
                }
            }

            return null;
        }

        private static string GetFlagFileName(string culture, IApplicationContext applicationContext)
        {
            culture = culture.EmptyNull().ToLower();

            if (culture.HasValue() && culture.SplitToPair(out _, out string cultureRight, "-"))
            {
                var fileName = cultureRight + ".png";

                if (applicationContext.WebRoot.FileExists("images/flags/" + fileName))
                {
                    return fileName;
                }
            }

            return null;
        }
    }
}
