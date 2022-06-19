﻿using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Widgets;
using Smartstore.DevTools.Components;

namespace Smartstore.DevTools.Filters
{
    public class WidgetZoneFilter : IActionFilter, IResultFilter
    {
        private static readonly Regex _widgetZonePattern 
            = new(@"^(?!header)(?!footer)(?!stylesheets)(?!head_scripts)(?!head_canonical)(?!head_links)(?!head).*$", RegexOptions.Compiled);
        
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ProfilerSettings _profilerSettings;
        private readonly IDisplayControl _displayControl;

        public WidgetZoneFilter(
            ICommonServices services,
            IWidgetProvider widgetProvider,
            ProfilerSettings profilerSettings,
            IDisplayControl displayControl)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _profilerSettings = profilerSettings;
            _displayControl = displayControl;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_profilerSettings.DisplayWidgetZones)
                return;

            _displayControl.MarkRequestAsUncacheable();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_profilerSettings.DisplayWidgetZones)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                if (!ShouldRender(filterContext.HttpContext.Request))
                {
                    return;
                }

                // INFO: Don't render in zones where replace-content is true & no <head> zones
                _widgetProvider.RegisterWidget(
                    _widgetZonePattern,
                    new ComponentWidgetInvoker(typeof(WidgetZoneViewComponent), null));
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        private bool ShouldRender(HttpRequest request)
        {
            if (!_services.WorkContext.CurrentCustomer.IsAdmin())
            {
                if (request.Path.ToString().StartsWith("/common/pdfreceipt", StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }

                return request.HttpContext.Connection.IsLocal();
            }

            return true;
        }
    }
}