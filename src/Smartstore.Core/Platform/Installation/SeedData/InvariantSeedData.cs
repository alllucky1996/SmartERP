﻿using System.Globalization;
using Autofac;
using Smartstore.Caching.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Common.Tasks;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Tasks;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Identity.Tasks;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Logging.Tasks;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Tasks;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Scheduling;

namespace Smartstore.Core.Installation
{
    public abstract partial class InvariantSeedData
    {
        private SmartDbContext _db;
        private Language _language;
        private IApplicationContext _appContext;
        private SampleMediaUtility _mediaUtility;

        protected InvariantSeedData()
        {
        }

        public void Initialize(SmartDbContext db, SeedDataConfiguration configuration, IApplicationContext appContext)
        {
            _db = db;
            _language = configuration.Language;
            _appContext = appContext;
            _mediaUtility = new SampleMediaUtility(db, "/App_Data/Samples");
        }

        #region Mandatory data creators

        public IList<MediaFile> Pictures()
        {
            var entities = new List<MediaFile>
            {
                CreatePicture("company-logo.png"),
                CreatePicture("product/allstar_charcoal.jpg"),
                CreatePicture("product/allstar_maroon.jpg"),
                CreatePicture("product/allstar_navy.jpg"),
                CreatePicture("product/allstar_purple.jpg"),
                CreatePicture("product/allstar_white.jpg"),
                CreatePicture("product/wayfarer_havana.png"),
                CreatePicture("product/wayfarer_havana_black.png"),
                CreatePicture("product/wayfarer_rayban-black.png")
            };

            Alter(entities);
            return entities;
        }

        public IList<Store> Stores()
        {
            var imgCompanyLogo = _db.MediaFiles.Where(x => x.Name == "company-logo.png").FirstOrDefault();
            var currency = _db.Currencies.FirstOrDefault(x => x.CurrencyCode == "EUR") ?? _db.Currencies.First();

            var entities = new List<Store>
            {
                new Store
                {
                    Name = "Your store name",
                    Url = "http://www.yourStore.com/",
                    Hosts = "yourstore.com,www.yourstore.com",
                    SslEnabled = false,
                    DisplayOrder = 1,
                    LogoMediaFileId = imgCompanyLogo?.Id ?? 0,
                    DefaultCurrencyId = currency.Id,
                    PrimaryExchangeRateCurrencyId = currency.Id
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<MeasureDimension> MeasureDimensions()
        {
            var entities = new List<MeasureDimension>()
            {
                new MeasureDimension()
                {
                    Name = "millimetre",
                    SystemKeyword = "mm",
                    Ratio = 25.4M,
                    DisplayOrder = 1,
                },
                new MeasureDimension()
                {
                    Name = "centimetre",
                    SystemKeyword = "cm",
                    Ratio = 0.254M,
                    DisplayOrder = 2,
                },
                new MeasureDimension()
                {
                    Name = "meter",
                    SystemKeyword = "m",
                    Ratio = 0.0254M,
                    DisplayOrder = 3,
                },
                new MeasureDimension()
                {
                    Name = "in",
                    SystemKeyword = "inch",
                    Ratio = 1M,
                    DisplayOrder = 4,
                },
                new MeasureDimension()
                {
                    Name = "feet",
                    SystemKeyword = "ft",
                    Ratio = 0.08333333M,
                    DisplayOrder = 5,
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<MeasureWeight> MeasureWeights()
        {
            var entities = new List<MeasureWeight>()
            {
                new MeasureWeight()
                {
                    Name = "ounce", // Ounce, Unze
					SystemKeyword = "oz",
                    Ratio = 16M,
                    DisplayOrder = 5,
                },
                new MeasureWeight()
                {
                    Name = "lb", // Pound
					SystemKeyword = "lb",
                    Ratio = 1M,
                    DisplayOrder = 6,
                },

                new MeasureWeight()
                {
                    Name = "kg",
                    SystemKeyword = "kg",
                    Ratio = 0.45359237M,
                    DisplayOrder = 1,
                },
                new MeasureWeight()
                {
                    Name = "gram",
                    SystemKeyword = "g",
                    Ratio = 453.59237M,
                    DisplayOrder = 2,
                },
                new MeasureWeight()
                {
                    Name = "liter",
                    SystemKeyword = "l",
                    Ratio = 0.45359237M,
                    DisplayOrder = 3,
                },
                new MeasureWeight()
                {
                    Name = "milliliter",
                    SystemKeyword = "ml",
                    Ratio = 0.45359237M,
                    DisplayOrder = 4,
                }
            };

            Alter(entities);
            return entities;
        }

        protected virtual string TaxNameBooks => "Books";
        protected virtual string TaxNameDigitalGoods => "Downloadable Products";
        protected virtual string TaxNameJewelry => "Jewelry";
        protected virtual string TaxNameApparel => "Apparel & Shoes";
        protected virtual string TaxNameFood => "Food";
        protected virtual string TaxNameElectronics => "Electronics & Software";
        protected virtual string TaxNameTaxFree => "Tax free";
        public virtual decimal[] FixedTaxRates => new decimal[] { 0, 0, 0, 0, 0 };

        public IList<TaxCategory> TaxCategories()
        {
            var entities = new List<TaxCategory>
            {
                new TaxCategory
                {
                    Name = TaxNameTaxFree,
                    DisplayOrder = 0,
                },
                new TaxCategory
                {
                    Name = TaxNameBooks,
                    DisplayOrder = 1,
                },
                new TaxCategory
                {
                    Name = TaxNameElectronics,
                    DisplayOrder = 5,
                },
                new TaxCategory
                {
                    Name = TaxNameDigitalGoods,
                    DisplayOrder = 10,
                },
                new TaxCategory
                {
                    Name = TaxNameJewelry,
                    DisplayOrder = 15,
                },
                new TaxCategory
                {
                    Name = TaxNameApparel,
                    DisplayOrder = 20,
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<Currency> Currencies()
        {
            var entities = new List<Currency>()
            {
                CreateCurrency("en-US", published: true, rate: 1M, order: 0),
                CreateCurrency("en-GB", published: true, rate: 0.61M, order: 5),
                CreateCurrency("en-AU", published: true, rate: 0.94M, order: 10),
                CreateCurrency("en-CA", published: true, rate: 0.98M, order: 15),
                CreateCurrency("de-DE", rate: 0.79M, order: 20/*, formatting: string.Format("0.00 {0}", "\u20ac")*/),
                CreateCurrency("de-CH", rate: 0.93M, order: 25, formatting: "CHF #,##0.00"),
                CreateCurrency("zh-CN", rate: 6.48M, order: 30),
                CreateCurrency("zh-HK", rate: 7.75M, order: 35),
                CreateCurrency("ja-JP", rate: 80.07M, order: 40),
                CreateCurrency("ru-RU", rate: 27.7M, order: 45),
                CreateCurrency("tr-TR", rate: 1.78M, order: 50),
                CreateCurrency("sv-SE", rate: 6.19M, order: 55)
            };

            Alter(entities);
            return entities;
        }

        public IList<ShippingMethod> ShippingMethods(bool includeSamples)
        {
            var entities = new List<ShippingMethod>
            {
                new ShippingMethod
                {
                    Name = "In-Store Pickup",
                    Description ="Pick up your items at the store",
                    DisplayOrder = 0
                },
                new ShippingMethod
                {
                    Name = "By Ground",
                    Description ="Compared to other shipping methods, like by flight or over seas, ground shipping is carried out closer to the earth",
                    DisplayOrder = 1
                },
            };

            if (includeSamples)
            {
                entities.Add(new ShippingMethod
                {
                    Name = "Free shipping",
                    DisplayOrder = 2,
                    IgnoreCharges = true
                });
            }

            Alter(entities);
            return entities;
        }

        public IList<CustomerRole> CustomerRoles(bool includeSamples)
        {
            var entities = new List<CustomerRole>
            {
                new CustomerRole
                {
                    Name = "Administrators",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Administrators,
                },
                new CustomerRole
                {
                    Name = "Forum Moderators",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.ForumModerators,
                },
                new CustomerRole
                {
                    Name = "Registered",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Registered,
                },
                new CustomerRole
                {
                    Name = "Guests",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Guests,
                }
            };

            if (includeSamples)
            {
                entities.Add(new CustomerRole
                {
                    Name = "Inactive new customers",
                    Active = true,
                    IsSystemRole = false,
                    // SystemName is not required. It's only used here to assign a rule set later.
                    SystemName = "InactiveNewCustomers"
                });
            }

            Alter(entities);
            return entities;
        }

        public Address AdminAddress()
        {
            var country = _db.Countries.Where(x => x.ThreeLetterIsoCode == "USA").FirstOrDefault();

            var entity = new Address()
            {
                FirstName = "John",
                LastName = "Smith",
                PhoneNumber = "12345678",
                Email = "admin@myshop.com",
                FaxNumber = "",
                Company = "John Smith LLC",
                Address1 = "1234 Main Road",
                Address2 = "",
                City = "New York",
                StateProvince = country.StateProvinces.FirstOrDefault(),
                Country = country,
                ZipPostalCode = "12212",
                CreatedOnUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer SearchEngineUser()
        {
            var entity = new Customer
            {
                Email = "builtin@search-engine-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system guest record used for requests from search engines.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.SearchEngine,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer BackgroundTaskUser()
        {
            var entity = new Customer
            {
                Email = "builtin@background-task-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for background tasks.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.BackgroundTask,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer PdfConverterUser()
        {
            var entity = new Customer
            {
                Email = "builtin@pdf-converter-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for the PDF converter.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.PdfConverter,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public IList<EmailAccount> EmailAccounts()
        {
            var entities = new List<EmailAccount>
            {
                new EmailAccount
                {
                    Email = "test@mail.com",
                    DisplayName = "Store name",
                    Host = "smtp.mail.com",
                    Port = 25,
                    Username = "123",
                    Password = "123",
                    EnableSsl = false,
                    UseDefaultCredentials = false
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<Topic> Topics()
        {
            var entities = new List<Topic>
            {
                new Topic
                    {
                        SystemName = "AboutUs",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "About Us",
                        Body = "<p>Put your &quot;About Us&quot; information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "CheckoutAsGuestOrRegister",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "",
                        Body = "<p><strong>Register and save time!</strong><br />Register with us for future convenience:</p><ul><li>Fast and easy check out</li><li>Easy access to your order history and status</li></ul>"
                    },
                new Topic
                    {
                        SystemName = "ConditionsOfUse",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Conditions of use",
                        Body = "<p>Put your conditions of use information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "ContactUs",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "Contact us",
                        Body = "<p>Put your contact information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "HomePageText",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "Welcome to our store",
                        Body = "<p>Online shopping is the process consumers go through to purchase products or services over the Internet. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "LoginRegistrationInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "About login / registration",
                        Body = "<p><strong>Not registered yet?</strong></p><p>Create your own account now and experience our diversity. With an account you can place orders faster and will always have a&nbsp;perfect overview of your current and previous orders.</p>"
                    },
                new Topic
                    {
                        SystemName = "PrivacyInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        ShortTitle = "Privacy",
                        Title = "Privacy policy",
                        Body = "<p><strong></strong></p>"
                    },
                new Topic
                    {
                        SystemName = "ShippingInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Shipping & Returns",
                        Body = "<p>Put your shipping &amp; returns information here. You can edit this in the admin site.</p>"
                    },

                new Topic
                    {
                        SystemName = "Imprint",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Imprint",
                        Body = "<p>Put your imprint information here. YOu can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "Disclaimer",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Disclaimer",
                        Body = "<p>Put your disclaimer information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "PaymentInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Payment info",
                        Body = "<p>Put your payment information here. You can edit this in the admin site.</p>"
                    },
            };
            Alter(entities);
            return entities;
        }

        public IList<ISettings> Settings()
        {
            var typeScanner = EngineContext.Current.Application.TypeScanner;
            var settings = typeScanner.FindTypes<ISettings>()
                .Select(x => Activator.CreateInstance(x))
                .OfType<ISettings>()
                .ToList();

            var defaultLanguageId = _language.Id;
            var localizationSettings = settings.OfType<LocalizationSettings>().FirstOrDefault();
            if (localizationSettings != null)
            {
                localizationSettings.DefaultAdminLanguageId = defaultLanguageId;
            }

            var defaultDimensionId = (_db.MeasureDimensions.FirstOrDefault(x => x.SystemKeyword == "inch"))?.Id ?? 0;
            var defaultWeightId = (_db.MeasureWeights.FirstOrDefault(x => x.SystemKeyword == "lb"))?.Id ?? 0;
            var measureSettings = settings.OfType<MeasureSettings>().FirstOrDefault();
            if (measureSettings != null)
            {
                measureSettings.BaseDimensionId = defaultDimensionId;
                measureSettings.BaseWeightId = defaultWeightId;
            }

            var paymentSettings = settings.OfType<PaymentSettings>().FirstOrDefault();
            if (paymentSettings != null)
            {
                paymentSettings.ActivePaymentMethodSystemNames = new List<string>
                {
                    "Payments.CashOnDelivery",
                    "Payments.Manual",
                    "Payments.PayInStore",
                    "Payments.Prepayment"
                };
            }

            var defaultEmailAccountId = _db.EmailAccounts.FirstOrDefault()?.Id ?? 0;
            var emailAccountSettings = settings.OfType<EmailAccountSettings>().FirstOrDefault();
            if (emailAccountSettings != null)
            {
                emailAccountSettings.DefaultEmailAccountId = defaultEmailAccountId;
            }

            var currencySettings = settings.OfType<CurrencySettings>().FirstOrDefault();
            if (currencySettings != null)
            {
                var currency = _db.Currencies.FirstOrDefault(x => x.CurrencyCode == "EUR") ?? _db.Currencies.First();
                if (currency != null)
                {
                    currencySettings.PrimaryCurrencyId = currency.Id;
                    currencySettings.PrimaryExchangeCurrencyId = currency.Id;
                }
            }

            Alter(settings);
            return settings;
        }

        public IList<ProductTemplate> ProductTemplates()
        {
            var entities = new List<ProductTemplate>
            {
                new ProductTemplate
                {
                    Name = "Default Product Template",
                    ViewPath = "Product",
                    DisplayOrder = 10
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<CategoryTemplate> CategoryTemplates()
        {
            var entities = new List<CategoryTemplate>
            {
                new CategoryTemplate
                {
                    Name = "Products in Grid or Lines",
                    ViewPath = "CategoryTemplate.ProductsInGridOrLines",
                    DisplayOrder = 1
                },
            };

            Alter(entities);
            return entities;
        }

        public IList<ManufacturerTemplate> ManufacturerTemplates()
        {
            var entities = new List<ManufacturerTemplate>
            {
                new ManufacturerTemplate
                {
                    Name = "Products in Grid or Lines",
                    ViewPath = "ManufacturerTemplate.ProductsInGridOrLines",
                    DisplayOrder = 1
                },
            };

            Alter(entities);
            return entities;
        }

        public IList<TaskDescriptor> TaskDescriptors()
        {
            var entities = new List<TaskDescriptor>
            {
                new TaskDescriptor
                {
                    Name = "Send emails",
                    CronExpression = "* * * * *", // every Minute
					Type = nameof(QueuedMessagesSendTask),
                    Enabled = true,
                    StopOnError = false,
                    Priority = TaskPriority.High
                },
                new TaskDescriptor
                {
                    Name = "Clear email queue",
                    CronExpression = "0 2 * * *", // At 02:00
					Type = nameof(QueuedMessagesClearTask),
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Delete guests",
                    CronExpression = "*/10 * * * *", // Every 10 minutes
					Type = nameof(DeleteGuestsTask),
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Delete logs",
                    CronExpression = "0 1 * * *", // At 01:00
					Type = nameof(DeleteLogsTask),
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Clear cache",
                    CronExpression = "0 */12 * * *", // Every 12 hours
					Type = nameof(ClearCacheTask),
                    Enabled = false,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Update currency exchange rates",
                    CronExpression = "0 */6 * * *", // Every 6 hours
					Type = nameof(UpdateExchangeRateTask),
                    Enabled = false,
                    StopOnError = false,
                    Priority = TaskPriority.High
                },
                new TaskDescriptor
                {
                    Name = "Clear transient uploads",
                    CronExpression = "30 1,13 * * *", // At 01:30 and 13:30
					Type = nameof(TransientMediaClearTask),
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Cleanup temporary files",
                    CronExpression = "30 3 * * *", // At 03:30
					Type = nameof(TempFileCleanupTask),
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Rebuild XML Sitemap",
                    CronExpression = "45 3 * * *",
                    Type = nameof(RebuildXmlSitemapTask),
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Update assignments of customers to customer roles",
                    CronExpression = "15 2 * * *", // At 02:15
                    Type = nameof(TargetGroupEvaluatorTask),
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Update assignments of products to categories",
                    CronExpression = "20 2 * * *", // At 02:20
                    Type = nameof(ProductRuleEvaluatorTask),
                    Enabled = true,
                    StopOnError = false
                }
            };

            Alter(entities);
            return entities;
        }

        #endregion

        #region Sample data creators

        public IList<Discount> Discounts()
        {
            var ruleSets = _db.RuleSets.Include(x => x.Rules).AsQueryable();

            var couponCodeDiscount = new Discount
            {
                Name = "Sample discount with coupon code",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = false,
                DiscountAmount = 10,
                RequiresCouponCode = true,
                CouponCode = "123"
            };

            var orderTotalDiscount = new Discount
            {
                Name = "20% order total discount",
                DiscountType = DiscountType.AssignedToOrderTotal,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 20,
                StartDateUtc = new DateTime(2020, 2, 1),
                EndDateUtc = new DateTime(2020, 2, 10)
            };
            orderTotalDiscount.RuleSets.Add(ruleSets.FirstOrDefault(x => x.Rules.Any(y => y.RuleType == "CartOrderCount")));

            var weekendDiscount = new Discount
            {
                Name = "5% on weekend orders",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 5,
                StartDateUtc = new DateTime(2020, 3, 1),
                EndDateUtc = new DateTime(2020, 3, 10)
            };
            weekendDiscount.RuleSets.Add(ruleSets.FirstOrDefault(x => x.Rules.Any(y => y.RuleType == "Weekday")));

            var manufacturersDiscount = new Discount
            {
                Name = "10% for certain manufacturers",
                DiscountType = DiscountType.AssignedToManufacturers,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 10,
                StartDateUtc = new DateTime(2020, 4, 5),
                EndDateUtc = new DateTime(2020, 4, 10)
            };

            var categoriesDiscount = new Discount
            {
                Name = "20% for certain categories",
                DiscountType = DiscountType.AssignedToCategories,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 20,
                StartDateUtc = new DateTime(2020, 6, 1),
                EndDateUtc = new DateTime(2020, 6, 30)
            };

            var productsDiscount = new Discount
            {
                Name = "25% on certain products",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 25,
                StartDateUtc = new DateTime(2020, 5, 10),
                EndDateUtc = new DateTime(2020, 5, 15)
            };

            var entities = new List<Discount>
            {
                couponCodeDiscount, orderTotalDiscount, weekendDiscount,
                manufacturersDiscount, categoriesDiscount, productsDiscount
            };

            Alter(entities);
            return entities;
        }

        public IList<DeliveryTime> DeliveryTimes()
        {
            var entities = new List<DeliveryTime>()
            {
                new DeliveryTime
                    {
                        Name = "available and ready to ship",
                        DisplayOrder = 0,
                        ColorHexValue = "#008000",
                        MinDays = 1,
                        MaxDays = 3
                    },
                new DeliveryTime
                    {
                        Name = "2-5 woking days",
                        DisplayOrder = 1,
                        ColorHexValue = "#FFFF00",
                        MinDays = 2,
                        MaxDays = 5
                    },
                new DeliveryTime
                    {
                        Name = "7 working days",
                        DisplayOrder = 2,
                        ColorHexValue = "#FF9900",
                        MinDays = 7,
                        MaxDays = 14
                    },
            };
            Alter(entities);
            return entities;
        }

        public IList<QuantityUnit> QuantityUnits()
        {
            var count = 0;
            var entities = new List<QuantityUnit>();

            var quPluralEn = new Dictionary<string, string>
            {
                { "Piece", "Pieces" },
                { "Box", "Boxes" },
                { "Parcel", "Parcels" },
                { "Palette", "Pallets" },
                { "Unit", "Units" },
                { "Sack", "Sacks" },
                { "Bag", "Bags" },
                { "Can", "Cans" },
                { "Packet", "Packets" },
                { "Bar", "Bars" },
                { "Bottle", "Bottles" },
                { "Glass", "Glasses" },
                { "Bunch", "Bunches" },
                { "Roll", "Rolls" },
                { "Cup", "Cups" },
                { "Bundle", "Bundles" },
                { "Barrel", "Barrels" },
                { "Set", "Sets" },
                { "Bucket", "Buckets" }
            };

            foreach (var qu in quPluralEn)
            {
                entities.Add(new QuantityUnit
                {
                    Name = qu.Key,
                    NamePlural = qu.Value,
                    Description = qu.Key,
                    IsDefault = qu.Key == "Piece",
                    DisplayOrder = count++
                });
            }

            Alter(entities);
            return entities;
        }

        public IList<Campaign> Campaigns()
        {
            var entities = new List<Campaign>
            {
                new Campaign
                {
                    Name = "Reminder of inactive new customers",
                    Subject = "New, exciting products are waiting for you to be discovered.",
                    Body = "<p>Efficiently unleash client-centric technologies and go forward information. Conveniently benchmark client-focused resources vis-a-vis interdependent paradigms. Synergistically disseminate interdependent supply chains via equity invested internal or 'organic' sources. Objectively exploit seamless growth strategies without orthogonal methodologies. Intrinsicly disseminate bricks-and-clicks web-readiness and e-business e-services.</p><p>Objectively develop performance based e-business and interdependent sources. Objectively evolve flexible markets via leveraged interfaces. Professionally deliver focused 'outside the box' thinking rather than global sources. Energistically redefine leveraged supply chains through customized relationships. Dramatically actualize resource sucking content rather than cross-platform e-business.</p><p>Seamlessly synthesize vertical mindshare without flexible sources. Distinctively productize timely infrastructures rather than cross-media niches. Dynamically evisculate pandemic convergence and scalable mindshare. Seamlessly embrace fully tested relationships whereas go forward initiatives. Globally actualize user-centric channels.</p>",
                    CreatedOnUtc = DateTime.UtcNow,
                    SubjectToAcl = true
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<RuleSetEntity> RuleSets()
        {
            // Cart: weekends.
            var weekends = new RuleSetEntity
            {
                Scope = RuleScope.Cart,
                Name = "Weekends",
                IsActive = true
            };
            weekends.Rules.Add(new RuleEntity
            {
                RuleType = "Weekday",
                Operator = RuleOperator.In,
                Value = $"{(int)DayOfWeek.Sunday},{(int)DayOfWeek.Saturday}"
            });

            // Cart: major customers.
            var majorCustomers = new RuleSetEntity
            {
                Scope = RuleScope.Cart,
                Name = "Major customers",
                Description = "3 or more orders and current order value at least 200,- Euro.",
                IsActive = true
            };
            majorCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CartSubtotal",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "200"
            });
            majorCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CartOrderCount",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "3"
            });

            // Product: sale.
            var saleProducts = new RuleSetEntity
            {
                Scope = RuleScope.Product,
                Name = "Sale",
                Description = "Products with applied discounts.",
                IsActive = true
            };
            saleProducts.Rules.Add(new RuleEntity
            {
                RuleType = "Discount",
                Operator = RuleOperator.IsEqualTo,
                Value = "true"
            });

            // Customer: inactive new customers.
            var inactiveNewCustomers = new RuleSetEntity
            {
                Scope = RuleScope.Customer,
                Name = "Inactive new customers",
                Description = "One completed order placed at least 90 days ago.",
                IsActive = true
            };
            inactiveNewCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CompletedOrderCount",
                Operator = RuleOperator.IsEqualTo,
                Value = "1"
            });
            inactiveNewCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "LastOrderDateDays",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "90"
            });


            // Offer free shipping method for major customers.
            var freeShipping = _db.ShippingMethods.FirstOrDefault(x => x.DisplayOrder == 2);
            if (freeShipping != null)
            {
                freeShipping.RuleSets.Add(majorCustomers);
            }

            // Assign rule conditions for inactive new customers to the related customer role.
            // We later bind the reminder campaign to it.
            var inactiveNewCustomersRole = _db.CustomerRoles.FirstOrDefault(x => x.SystemName == "InactiveNewCustomers");
            if (inactiveNewCustomersRole != null)
            {
                inactiveNewCustomersRole.RuleSets.Add(inactiveNewCustomers);
            }


            var entities = new List<RuleSetEntity>
            {
                weekends, majorCustomers, saleProducts, inactiveNewCustomers
            };

            Alter(entities);
            return entities;
        }

        public void FinalizeSamples()
        {
            // Bind the reminder campaign to the rule conditions for inactive new customers.
            var reminderCampaign = _db.Campaigns.FirstOrDefault(x => x.SubjectToAcl);
            if (reminderCampaign != null)
            {
                var inactiveNewCustomersRole = _db.CustomerRoles.FirstOrDefault(x => x.SystemName == "InactiveNewCustomers");
                if (inactiveNewCustomersRole != null)
                {
                    _db.AclRecords.Add(new AclRecord
                    {
                        EntityId = reminderCampaign.Id,
                        EntityName = nameof(Campaign),
                        CustomerRoleId = inactiveNewCustomersRole.Id,
                        IsIdle = false
                    });
                }
            }
        }

        #region Alterations

        protected virtual void Alter(IList<MeasureDimension> entities)
        {
        }

        protected virtual void Alter(IList<MeasureWeight> entities)
        {
        }

        protected virtual void Alter(IList<TaxCategory> entities)
        {
        }

        protected virtual void Alter(IList<Currency> entities)
        {
        }

        protected virtual void Alter(IList<Country> entities)
        {
        }

        protected virtual void Alter(IList<ShippingMethod> entities)
        {
        }

        protected virtual void Alter(IList<CustomerRole> entities)
        {
        }

        protected virtual void Alter(Address entity)
        {
        }

        protected virtual void Alter(Customer entity)
        {
        }

        protected virtual void Alter(IList<DeliveryTime> entities)
        {
        }

        protected virtual void Alter(IList<QuantityUnit> entities)
        {
        }

        protected virtual void Alter(IList<EmailAccount> entities)
        {
        }

        protected virtual void Alter(IList<MessageTemplate> entities)
        {
        }

        protected virtual void Alter(IList<Topic> entities)
        {
        }

        protected virtual void Alter(IList<MenuEntity> entities)
        {
        }

        protected virtual void Alter(IList<Store> entities)
        {
        }

        protected virtual void Alter(IList<MediaFile> entities)
        {
        }

        protected virtual void Alter(IList<ISettings> settings)
        {
        }

        protected virtual void Alter(IList<StoreInformationSettings> settings)
        {
        }

        protected virtual void Alter(IList<OrderSettings> settings)
        {
        }

        protected virtual void Alter(IList<MeasureSettings> settings)
        {
        }

        protected virtual void Alter(IList<ShippingSettings> settings)
        {
        }

        protected virtual void Alter(IList<PaymentSettings> settings)
        {
        }

        protected virtual void Alter(IList<TaxSettings> settings)
        {
        }

        protected virtual void Alter(IList<EmailAccountSettings> settings)
        {
        }

        protected virtual void Alter(IList<ActivityLogType> entities)
        {
        }

        protected virtual void Alter(IList<ProductTemplate> entities)
        {
        }

        protected virtual void Alter(IList<CategoryTemplate> entities)
        {
        }

        protected virtual void Alter(IList<ManufacturerTemplate> entities)
        {
        }

        protected virtual void Alter(IList<TaskDescriptor> entities)
        {
        }

        protected virtual void Alter(IList<SpecificationAttribute> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttribute> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttributeOptionsSet> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttributeOption> entities)
        {
        }

        protected virtual void Alter(IList<ProductVariantAttribute> entities)
        {
        }

        protected virtual void Alter(IList<Category> entities)
        {
        }

        protected virtual void Alter(IList<Manufacturer> entities)
        {
        }

        protected virtual void Alter(IList<Product> entities)
        {
        }

        protected virtual void Alter(IList<Discount> entities)
        {
        }

        protected virtual void Alter(IList<Campaign> entities)
        {
        }

        protected virtual void Alter(IList<RuleSetEntity> entities)
        {
        }

        protected virtual void Alter(IList<ProductTag> entities)
        {
        }

        protected virtual void Alter(IList<ProductBundleItem> entities)
        {
        }

        protected virtual void Alter(UrlRecord entity)
        {
        }

        #endregion Alterations

        #endregion Sample data creators

        #region Helpers

        protected SmartDbContext DbContext 
            => _db;

        protected SampleMediaUtility MediaUtility 
            => _mediaUtility;

        public virtual UrlRecord CreateUrlRecordFor<T>(T entity) where T : BaseEntity, ISlugSupported, new()
        {
            string name;

            if (entity is Topic topic)
            {
                name = BuildSlug(topic.SystemName).Truncate(400);
            }
            else
            {
                name = BuildSlug(entity.GetDisplayName()).Truncate(400);
            }

            if (name.HasValue())
            {
                var result = new UrlRecord
                {
                    EntityId = entity.Id,
                    EntityName = entity.GetEntityName(),
                    LanguageId = 0,
                    Slug = name,
                    IsActive = true
                };

                Alter(result);
                return result;
            }

            return null;
        }

        protected MediaFile CreatePicture(string fileName, string seoFileName = null)
        {
            return _mediaUtility.CreateMediaFileAsync(fileName, seoFileName).GetAwaiter().GetResult();
        }

        protected void AddProductPicture(
            Product product,
            string imageName,
            string seName = null,
            int displayOrder = 1)
        {
            if (seName == null)
            {
                seName = BuildSlug(Path.GetFileNameWithoutExtension(imageName));
            }

            var picture = CreatePicture(imageName, seName);
            if (picture != null)
            {
                product.ProductPictures.Add(new ProductMediaFile
                {
                    MediaFile = picture,
                    DisplayOrder = displayOrder
                });
            }
        }

        protected string BuildSlug(string name)
            => SeoHelper.BuildSlug(name);

        protected static Currency CreateCurrency(string locale, decimal rate = 1M, string formatting = "", bool published = false, int order = 1)
        {
            Currency currency = null;
            try
            {
                var info = new RegionInfo(locale);
                if (info != null)
                {
                    currency = new Currency
                    {
                        DisplayLocale = locale,
                        Name = info.CurrencyNativeName,
                        CurrencyCode = info.ISOCurrencySymbol,
                        Rate = rate,
                        CustomFormatting = formatting,
                        Published = published,
                        DisplayOrder = order
                    };
                }
            }
            catch
            {
                return null;
            }

            return currency;
        }

        protected static string FormatAttributeJson(List<(int Attribute, object Value)> Attributes)
        {
            var selection = new ProductVariantAttributeSelection(string.Empty);

            foreach (var attribute in Attributes)
            {
                selection.AddAttribute(attribute.Attribute, new List<object> { attribute.Value });
            }

            return selection.AsJson();
        }

        #endregion
    }
}