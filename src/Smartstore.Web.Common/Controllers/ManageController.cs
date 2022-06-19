﻿using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Controllers
{
    [RequireSsl]
    [TrackActivity(Order = 100)]
    [SaveChanges(typeof(SmartDbContext), Order = int.MaxValue)]
    public abstract class ManageController : SmartController
    {
        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        protected virtual void AddLocales<TLocalizedModelLocal>(IList<TLocalizedModelLocal> locales) 
            where TLocalizedModelLocal : ILocalizedLocaleModel, new()
        {
            AddLocales(locales, null);
        }

        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        /// <param name="configure">Configure action</param>
        protected virtual void AddLocales<TLocalizedModelLocal>(IList<TLocalizedModelLocal> locales, Action<TLocalizedModelLocal, int> configure) 
            where TLocalizedModelLocal : ILocalizedLocaleModel, new()
        {
            Guard.NotNull(locales, nameof(locales));
            
            foreach (var language in Services.Resolve<ILanguageService>().GetAllLanguages(true))
            {
                var locale = Activator.CreateInstance<TLocalizedModelLocal>();
                locale.LanguageId = language.Id;
                if (configure != null)
                {
                    configure.Invoke(locale, locale.LanguageId);
                }

                locales.Add(locale);
            }
        }

        /// <summary>
        /// Save the store mappings for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="selectedStoreIds">Selected store identifiers.</param>
        protected virtual async Task<int> SaveStoreMappingsAsync<T>(T entity, int[] selectedStoreIds) where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(entity, nameof(entity));

            await Services.Resolve<IStoreMappingService>().ApplyStoreMappingsAsync(entity, selectedStoreIds);
            return await Services.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Save the ACL mappings for an entity.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">The entity</param>
        protected virtual async Task<int> SaveAclMappingsAsync<T>(T entity, params int[] selectedCustomerRoleIds) where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(entity, nameof(entity));

            await Services.Resolve<IAclService>().ApplyAclMappingsAsync(entity, selectedCustomerRoleIds);
            return await Services.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Get active store scope (for multi-store configuration mode)
        /// </summary>
        /// <returns>Store ID; 0 if we are in a shared mode</returns>
        protected internal virtual int GetActiveStoreScopeConfiguration()
        {
            // Ensure that we have 2 (or more) stores
            if (Services.StoreContext.GetAllStores().Count < 2)
                return 0;

            var storeId = Services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var store = Services.StoreContext.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }

        // INFO: instead, throw new AccessDeniedException()

        /// <summary>
        /// Access denied view
        /// </summary>
        /// <returns>Access denied view</returns>
        //protected IActionResult AccessDeniedView()
        //{
        //    return RedirectToAction("AccessDenied", "Security", new { pageUrl = this.Request.RawUrl(), area = "Admin" });
        //}

        /// <summary>
        /// Renders default access denied view as a partial
        /// </summary>
        //protected IActionResult AccessDeniedPartialView()
        //{
        //    return PartialView("~/Areas/Admin/Views/Security/AccessDenied.cshtml");
        //}
    }
}
