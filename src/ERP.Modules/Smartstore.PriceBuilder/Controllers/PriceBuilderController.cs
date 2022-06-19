using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.PriceBuilder.Models;
using Smartstore.PriceBuilder.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.PriceBuilder.Controllers
{
    [Route("[area]/pricebuilder/{action=index}/{id?}")]
    public class PriceBuilderController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IBlogService _blogService;
        private readonly IUrlService _urlService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICustomerService _customerService;

        public PriceBuilderController(
            SmartDbContext db,
            IBlogService blogService,
            IUrlService urlService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ICustomerService customerService)
        {
            _db = db;
            _blogService = blogService;
            _urlService = urlService;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _customerService = customerService;
        }

        #region Configure settings

        [AuthorizeAdmin, Permission(PriceBuilderPermissions.Read)]
        [LoadSetting]
        public IActionResult Settings(PriceBuilderSettings settings, int storeId)
        {
            var model = MiniMapper.Map<PriceBuilderSettings, BlogSettingsModel>(settings);
            return View(model);
        }

        [AuthorizeAdmin, Permission(PriceBuilderPermissions.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Settings(BlogSettingsModel model, PriceBuilderSettings settings, int storeId)
        {
            if (!ModelState.IsValid)
            {
                return Settings(settings, storeId);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

          
            return RedirectToAction("Settings");
        }

        #endregion
         
    }
}
