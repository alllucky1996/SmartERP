﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay payment button.
    /// </summary>
    public class PayButtonViewComponent : SmartViewComponent
    {
        private static readonly string[] _supportedLedgerCurrencies = new[] { "USD", "EUR", "GBP", "JPY" };

        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly AmazonPaySettings _settings;
        private readonly OrderSettings _orderSettings;

        public PayButtonViewComponent(
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            AmazonPaySettings amazonPaySettings,
            OrderSettings orderSettings)
        {
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _settings = amazonPaySettings;
            _orderSettings = orderSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (_settings.PublicKeyId.IsEmpty() ||
                _settings.PrivateKey.IsEmpty() ||
                (!_orderSettings.AnonymousCheckoutAllowed && customer.IsGuest()) ||
                (_settings.ShowPayButtonForAdminOnly && !customer.IsAdmin()))
            {
                return Empty();
            }

            var currencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            if (!_supportedLedgerCurrencies.Contains(currencyCode, StringComparer.OrdinalIgnoreCase))
            {
                return Empty();
            }

            var store = Services.StoreContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.HasItems || !await _paymentService.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, cart, store.Id))
            {
                return Empty();
            }

            var model = new AmazonPayButtonModel(
                _settings,
                cart.IsShippingRequired() ? "PayAndShip" : "PayOnly",
                currencyCode,
                Services.WorkContext.WorkingLanguage.UniqueSeoCode);

            return View(model);
        }
    }
}
