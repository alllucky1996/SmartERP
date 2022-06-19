﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Components;

namespace Smartstore.PayPal.Filters
{
    public class CheckoutFilter : IAsyncResultFilter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public CheckoutFilter(
            SmartDbContext db,
            ICommonServices services, 
            PayPalSettings settings, 
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            Lazy<IWidgetProvider> widgetProvider)
        {
            _db = db;
            _services = services;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _httpContextAccessor = httpContextAccessor;
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // If client id or secret haven't been configured yet, don't do anything.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                await next();
                return;
            }

            var checkoutState = _checkoutStateAccessor.CheckoutState;

            if (!checkoutState.CustomProperties.ContainsKey("PayPalButtonUsed"))
            {
                _widgetProvider.Value.RegisterWidget(
                    "checkout_payment_method_buttons", 
                    new ComponentWidgetInvoker(typeof(PayPalViewComponent), false));

                await next();
                return;
            }

            var skipPaymentPage = (bool)checkoutState.CustomProperties.Get("PayPalButtonUsed");

            // Should only run on a full view rendering result or HTML ContentResult.
            if ((filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult()) && skipPaymentPage)
            {
                _services.WorkContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod = "Payments.PayPalStandard";
                await _db.SaveChangesAsync();

                var session = _httpContextAccessor.HttpContext.Session;

                if (!session.ContainsKey("OrderPaymentInfo"))
                {
                    session.TrySetObject("OrderPaymentInfo", new ProcessPaymentRequest
                    {
                        PaypalOrderId = (string)checkoutState.CustomProperties.Get("PayPalOrderId")
                    });
                };

                // Delete property for backward navigation.
                checkoutState.CustomProperties.Remove("PayPalButtonUsed");

                filterContext.Result = new RedirectToActionResult("Confirm", "Checkout", new { area = "" });
            }

            await next();
        }
    }
}

