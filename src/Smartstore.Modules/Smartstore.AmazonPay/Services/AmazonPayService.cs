﻿using Amazon.Pay.API.WebStore.CheckoutSession;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using AmazonPayTypes = Amazon.Pay.API.Types;

namespace Smartstore.AmazonPay.Services
{
    public class AmazonPayService : IAmazonPayService
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;

        public AmazonPayService(SmartDbContext db, ICommonServices services)
        {
            _db = db;
            _services = services;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        //public async Task<bool> AddCustomerOrderNoteLoopAsync(AmazonPayActionState state, CancellationToken cancelToken = default)
        //{
        //    if (state == null || state.OrderGuid == Guid.Empty)
        //    {
        //        return false;
        //    }

        //    try
        //    {
        //        const int sleepMillSec = 4000;
        //        const int loopMillSec = 40000;
        //        var startTime = DateTime.Now.TimeOfDay;

        //        for (var i = 0; i < 99 && (DateTime.Now.TimeOfDay.Milliseconds - startTime.Milliseconds) <= loopMillSec; ++i)
        //        {
        //            var order = await _db.Orders
        //                .Where(x => x.OrderGuid == state.OrderGuid)
        //                .FirstOrDefaultAsync(cancelToken);

        //            if (order != null)
        //            {
        //                using var psb = StringBuilderPool.Instance.Get(out var sb);
        //                sb.AppendLine(T("Plugins.Payments.AmazonPay.AuthorizationHardDeclineMessage"));

        //                if (state.Errors?.Any() ?? false)
        //                {
        //                    foreach (var error in state.Errors)
        //                    {
        //                        sb.AppendFormat("<p>{0}</p>", error);
        //                    }
        //                }

        //                var orderNote = new OrderNote
        //                {
        //                    DisplayToCustomer = true,
        //                    Note = sb.ToString(),
        //                    CreatedOnUtc = DateTime.UtcNow,
        //                };

        //                order.OrderNotes.Add(orderNote);
        //                await _db.SaveChangesAsync(cancelToken);

        //                await _messageFactory.SendNewOrderNoteAddedCustomerNotificationAsync(orderNote, _services.WorkContext.WorkingLanguage.Id);
        //                break;
        //            }

        //            Thread.Sleep(sleepMillSec);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //    }

        //    return false;
        //}

        public async Task<int> UpdateAccessKeysAsync(string json, int storeId)
        {
            if (json.IsEmpty())
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.MissingPayloadParameter"));
            }

            dynamic jsonData = JObject.Parse(json);
            var settings = await _services.SettingFactory.LoadSettingsAsync<AmazonPaySettings>(storeId);

            var encryptedPayload = (string)jsonData.encryptedPayload;
            if (encryptedPayload.HasValue())
            {
                throw new SmartException(T("Plugins.Payments.AmazonPay.EncryptionNotSupported"));
            }

            settings.SellerId = (string)jsonData.merchant_id;
            settings.PublicKeyId = (string)jsonData.public_key_id;
            settings.ClientId = (string)jsonData.store_id;

            return await _services.SettingFactory.SaveSettingsAsync(settings, storeId);
        }

        public async Task<CheckoutAdressResult> CreateAddressAsync(CheckoutSessionResponse session, Customer customer, bool createBillingAddress)
        {
            Guard.NotNull(session, nameof(session));
            Guard.NotNull(customer, nameof(customer));

            var result = new CheckoutAdressResult();

            var src = createBillingAddress ? session.BillingAddress : session.ShippingAddress;
            if (src != null && src.CountryCode.HasValue())
            {
                result.CountryCode = src.CountryCode;

                var country = await _db.Countries
                    .AsNoTracking()
                    .ApplyIsoCodeFilter(src.CountryCode)
                    .FirstOrDefaultAsync();

                if (country != null)
                {
                    result.IsCountryAllowed = createBillingAddress
                        ? country.AllowsBilling
                        : country.AllowsShipping;

                    if (result.IsCountryAllowed)
                    {
                        var (firstName, lastName) = GetFirstAndLastName(src.Name);

                        var stateProvince = src.StateOrRegion.HasValue()
                            ? await _db.StateProvinces
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Abbreviation == src.StateOrRegion)
                            : null;

                        var address1 = src.AddressLine1.TrimSafe().Truncate(500);
                        var address2 = src.AddressLine2.TrimSafe()
                                .Grow(src.AddressLine3.TrimSafe(), ", ")
                                .Truncate(500);

                        if (address1.IsEmpty() && address2.HasValue())
                        {
                            address1 = address2;
                            address2 = null;
                        }

                        result.Address = new Address
                        {
                            CreatedOnUtc = DateTime.UtcNow,
                            FirstName = firstName.Truncate(255),
                            LastName = lastName.Truncate(255),
                            Address1 = address1,
                            Address2 = address2,
                            City = src.County
                                .Grow(src.District, " ")
                                .Grow(src.City, " ")
                                .TrimSafe()
                                .Truncate(100),
                            ZipPostalCode = src.PostalCode.TrimSafe().Truncate(50),
                            PhoneNumber = src.PhoneNumber.TrimSafe().Truncate(100),
                            Email = session.Buyer.Email.TrimSafe().NullEmpty() ?? customer.Email,
                            CountryId = country.Id,
                            StateProvinceId = stateProvince?.Id
                        };

                        result.Success = true;
                    }
                }
            }

            return result;
        }

        public AmazonPayTypes.Currency GetAmazonPayCurrency(string currencyCode = null)
        {
            currencyCode ??= _services.CurrencyService.PrimaryCurrency.CurrencyCode;

            return currencyCode.EmptyNull().ToLower() switch
            {
                "usd" => AmazonPayTypes.Currency.USD,
                "gbp" => AmazonPayTypes.Currency.GBP,
                "jpy" => AmazonPayTypes.Currency.JPY,
                "aud" => AmazonPayTypes.Currency.AUD,
                "zar" => AmazonPayTypes.Currency.ZAR,
                "chf" => AmazonPayTypes.Currency.CHF,
                "nok" => AmazonPayTypes.Currency.NOK,
                "dkk" => AmazonPayTypes.Currency.DKK,
                "sek" => AmazonPayTypes.Currency.SEK,
                "nzd" => AmazonPayTypes.Currency.NZD,
                "hkd" => AmazonPayTypes.Currency.HKD,
                _ => AmazonPayTypes.Currency.EUR,
            };
        }

        #region Utilities

        internal static (string FirstName, string LastName) GetFirstAndLastName(string name)
        {
            if (name.HasValue())
            {
                var index = name.LastIndexOf(' ');
                if (index == -1)
                {
                    return (string.Empty, name);
                }
                else
                {
                    return (name[..index], name[(index + 1)..]);
                }
            }

            return (string.Empty, string.Empty);
        }

        #endregion
    }
}
