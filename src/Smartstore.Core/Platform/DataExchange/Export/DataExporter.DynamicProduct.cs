﻿using System.Web;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.DataExchange.Export.Events;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter
    {
        /// <summary>
        /// The main method to create expando objects for a product to be exported.
        /// Returns several objects if variant combinations are to be exported as products.
        /// </summary>
        private async Task<IEnumerable<dynamic>> Convert(Product product, DataExporterContext ctx)
        {
            var result = new List<dynamic>();
            var seName = ctx.GetUrlRecord(product);

            var productContext = new DynamicProductContext
            {
                SeName = seName,
                Combinations = await ctx.ProductBatchContext.AttributeCombinations.GetOrLoadAsync(product.Id),
                AbsoluteProductUrl = await _productUrlHelper.GetAbsoluteProductUrlAsync(product.Id, seName, null, ctx.Store, ctx.ContextLanguage)
            };

            if (ctx.Projection.AttributeCombinationAsProduct && productContext.Combinations.Where(x => x.IsActive).Any())
            {
                if (ctx.Supports(ExportFeatures.UsesAttributeCombinationParent))
                {
                    var dynObject = await ToDynamic(product, true, ctx, productContext);
                    result.Add(dynObject);
                }

                var dbContext = _db as DbContext;

                foreach (var combination in productContext.Combinations.Where(x => x.IsActive))
                {
                    var trackedProduct = _db.FindTracked<Product>(product.Id);
                    if (trackedProduct == null)
                    {
                        var attachedProduct = _db.Attach(product);
                        product = attachedProduct.Entity;
                    }
                    else
                    {
                        product = trackedProduct;
                    }

                    var entry = dbContext.Entry(product);

                    // The returned object is not the entity and is not being tracked by the context.
                    // It also does not have any relationships set to other objects.
                    // CurrentValues only includes database (thus primitive) values.
                    var productClone = entry.CurrentValues.ToObject() as Product;
                    _db.DetachEntity(product);

                    productContext.Combination = combination;

                    var dynObject = await ToDynamic(productClone, false, ctx, productContext);
                    result.Add(dynObject);
                }
            }
            else
            {
                var dynObject = await ToDynamic(product, false, ctx, productContext);
                result.Add(dynObject);
            }

            if (result.Any())
            {
                await _services.EventPublisher.PublishAsync(new RowExportingEvent
                {
                    Row = result.First(),
                    EntityType = ExportEntityType.Product,
                    ExportRequest = ctx.Request,
                    ExecuteContext = ctx.ExecuteContext
                });
            }

            return result;
        }

        /// <summary>
        /// Creates an expando object with the most important general properties of a product such as the name and description.
        /// This method is used for entities where products are to be exported as related data, e.g. order items.
        /// </summary>
        private async Task<dynamic> ToDynamic(Product product, DataExporterContext ctx, string seName = null, Money? price = null)
        {
            if (product == null)
            {
                return null;
            }

            dynamic result = new DynamicEntity(product);
            var localizedName = ctx.GetTranslation(product, nameof(product.Name), product.Name);

            result.AppliedDiscounts = null;
            result.Downloads = null;
            result.TierPrices = null;
            result.ProductAttributes = null;
            result.ProductAttributeCombinations = null;
            result.ProductPictures = null;
            result.ProductCategories = null;
            result.ProductManufacturers = null;
            result.ProductTags = null;
            result.ProductSpecificationAttributes = null;
            result.ProductBundleItems = null;
            result.Name = localizedName;

            if (!ctx.IsPreview)
            {
                result.SeName = seName ?? ctx.GetUrlRecord(product);
                result.ShortDescription = ctx.GetTranslation(product, nameof(product.ShortDescription), product.ShortDescription);
                result.FullDescription = ctx.GetTranslation(product, nameof(product.FullDescription), product.FullDescription);
                result.MetaKeywords = ctx.GetTranslation(product, nameof(product.MetaKeywords), product.MetaKeywords);
                result.MetaDescription = ctx.GetTranslation(product, nameof(product.MetaDescription), product.MetaDescription);
                result.MetaTitle = ctx.GetTranslation(product, nameof(product.MetaTitle), product.MetaTitle);
                result.BundleTitleText = ctx.GetTranslation(product, nameof(product.BundleTitleText), product.BundleTitleText);

                result._ProductTemplateViewPath = ctx.ProductTemplates.ContainsKey(product.ProductTemplateId)
                    ? ctx.ProductTemplates[product.ProductTemplateId]
                    : string.Empty;

                if (product.BasePriceHasValue && product.BasePriceAmount != 0)
                {
                    if (price == null)
                    {
                        var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, ctx.ContextCustomer, ctx.ContextCurrency, ctx.ProductBatchContext);
                        var productPrice = await _priceCalculationService.CalculatePriceAsync(new PriceCalculationContext(product, calculationOptions));

                        price = productPrice.FinalPrice;
                    }

                    result._BasePriceInfo = _priceCalculationService.GetBasePriceInfo(product, price.Value, ctx.ContextCurrency, ctx.ContextLanguage, false);
                }
                else
                {
                    result._BasePriceInfo = string.Empty;
                }

                result.DeliveryTime = ctx.DeliveryTimes.TryGetValue(product.DeliveryTimeId ?? 0, out var deliveryTime)
                    ? ToDynamic(deliveryTime, ctx)
                    : null;

                result.QuantityUnit = ctx.QuantityUnits.TryGetValue(product.QuantityUnitId ?? 0, out var quantityUnit)
                    ? ToDynamic(quantityUnit, ctx)
                    : null;

                result.CountryOfOrigin = product.CountryOfOriginId.HasValue
                    ? ToDynamic(product.CountryOfOrigin, ctx)
                    : null;

                result._Localized = GetLocalized(ctx, product,
                    x => x.Name,
                    x => x.ShortDescription,
                    x => x.FullDescription,
                    x => x.MetaKeywords,
                    x => x.MetaDescription,
                    x => x.MetaTitle,
                    x => x.BundleTitleText);
            }

            return result;
        }

        /// <summary>
        /// Creates an expando object with all product properties.
        /// This method is used when exporting products and when exporting variant combinations as products.
        /// </summary>
        private async Task<dynamic> ToDynamic(Product product, bool isParent, DataExporterContext ctx, DynamicProductContext productContext)
        {
            var combination = productContext.Combination;

            product.MergeWithCombination(combination);

            var productManufacturers = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);
            var productCategories = await ctx.ProductBatchContext.ProductCategories.GetOrLoadAsync(product.Id);
            var productAttributes = await ctx.ProductBatchContext.Attributes.GetOrLoadAsync(product.Id);
            var productTags = await ctx.ProductBatchContext.ProductTags.GetOrLoadAsync(product.Id);
            var specificationAttributes = await ctx.ProductBatchContext.SpecificationAttributes.GetOrLoadAsync(product.Id);
            var selectedAttributes = combination?.AttributeSelection;
            var variantAttributeValues = combination?.AttributeSelection?.MaterializeProductVariantAttributeValues(productAttributes);

            // Price calculation.
            var calculationContext = new PriceCalculationContext(product, ctx.PriceCalculationOptions);
            calculationContext.AddSelectedAttributes(combination?.AttributeSelection, product.Id);
            var price = await _priceCalculationService.CalculatePriceAsync(calculationContext);

            dynamic dynObject = await ToDynamic(product, ctx, productContext.SeName, price.FinalPrice);
            dynObject._IsParent = isParent;
            dynObject._CategoryName = null;
            dynObject._CategoryPath = null;
            dynObject._AttributeCombinationValues = null;
            dynObject._AttributeCombinationId = 0;

            dynObject.Price = price.FinalPrice.Amount;

            if (combination != null)
            {
                dynObject._AttributeCombinationId = combination.Id;
                dynObject._UniqueId = product.Id + "-" + combination.Id;

                if (ctx.Supports(ExportFeatures.UsesAttributeCombination))
                {
                    dynObject._AttributeCombinationValues = variantAttributeValues;
                }

                if (ctx.Projection.AttributeCombinationValueMerging == ExportAttributeValueMerging.AppendAllValuesToName)
                {
                    var valueNames = variantAttributeValues
                        .Select(x => ctx.GetTranslation(x, nameof(x.Name), x.Name))
                        .ToList();

                    dynObject.Name = ((string)dynObject.Name).Grow(string.Join(", ", valueNames), " ");
                }
            }
            else
            {
                dynObject._UniqueId = product.Id.ToString();
            }

            if (selectedAttributes?.AttributesMap?.Any() ?? false)
            {
                var query = new ProductVariantQuery();
                await _productUrlHelper.AddAttributesToQueryAsync(query, selectedAttributes, product.Id, 0, productAttributes);

                dynObject._DetailUrl = productContext.AbsoluteProductUrl + _productUrlHelper.ToQueryString(query);
            }
            else
            {
                dynObject._DetailUrl = productContext.AbsoluteProductUrl;
            }

            // Category path.
            {
                var categoryPath = string.Empty;
                var pc = productCategories.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                if (pc != null)
                {
                    var node = await _categoryService.GetCategoryTreeAsync(pc.CategoryId, true, ctx.Store.Id);
                    if (node != null)
                    {
                        categoryPath = _categoryService.GetCategoryPath(node, ctx.Projection.LanguageId, null, " > ");
                    }
                }

                dynObject._CategoryPath = categoryPath;
            }

            dynObject.CountryOfOrigin = ctx.Countries.TryGetValue(product.CountryOfOriginId ?? 0, out var countryOfOrigin)
                ? ToDynamic(countryOfOrigin, ctx)
                : null;

            dynObject.ProductManufacturers = productManufacturers
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Manufacturer = ToDynamic(x.Manufacturer, ctx);
                    dyn.Manufacturer.Picture = x.Manufacturer != null && x.Manufacturer.MediaFileId.HasValue
                        ? ToDynamic(x.Manufacturer.MediaFile, _mediaSettings.ManufacturerThumbPictureSize, _mediaSettings.ManufacturerThumbPictureSize, ctx)
                        : null;

                    return dyn;
                })
                .ToList();

            dynObject.ProductCategories = productCategories
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Category = ToDynamic(x.Category, ctx);
                    dyn.Category.Picture = x.Category != null && x.Category.MediaFileId.HasValue
                        ? ToDynamic(x.Category.MediaFile, _mediaSettings.CategoryThumbPictureSize, _mediaSettings.CategoryThumbPictureSize, ctx)
                        : null;

                    if (dynObject._CategoryName == null)
                    {
                        dynObject._CategoryName = (string)dyn.Category.Name;
                    }

                    return dyn;
                })
                .ToList();

            dynObject.ProductAttributes = productAttributes
                .OrderBy(x => x.DisplayOrder)
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            // Do not export combinations if a combination is exported as a product.
            if (productContext.Combinations != null && productContext.Combination == null)
            {
                var pictureSize = ctx.Projection.PictureSize > 0 
                    ? _mediaSettings.GetNextValidThumbnailSize(ctx.Projection.PictureSize)
                    : _mediaSettings.ProductDetailsPictureSize;

                var productMediaFiles = (await ctx.ProductBatchContext.ProductMediaFiles.GetOrLoadAsync(product.Id))
                    .ToDictionarySafe(x => x.MediaFileId, x => x);

                dynObject.ProductAttributeCombinations = productContext.Combinations
                    .Select(x =>
                    {
                        dynamic dyn = DataExporter.ToDynamic(x, ctx);
                        var assignedFiles = new List<dynamic>();

                        foreach (var fileId in x.GetAssignedMediaIds().Take(ctx.Projection.NumberOfMediaFiles ?? int.MaxValue))
                        {
                            if (productMediaFiles.TryGetValue(fileId, out var assignedFile) && assignedFile.MediaFile != null)
                            {
                                assignedFiles.Add(ToDynamic(assignedFile.MediaFile, _mediaSettings.ProductThumbPictureSize, pictureSize, ctx));
                            }
                        }

                        dyn.Pictures = assignedFiles;
                        return dyn;
                    })
                    .ToList();
            }
            else
            {
                dynObject.ProductAttributeCombinations = Enumerable.Empty<ProductVariantAttributeCombination>();
            }

            if (product.HasTierPrices)
            {
                var tierPrices = await ctx.ProductBatchContext.TierPrices.GetOrLoadAsync(product.Id);

                dynObject.TierPrices = tierPrices
                    .RemoveDuplicatedQuantities()
                    .Select(x =>
                    {
                        dynamic dyn = new DynamicEntity(x);
                        return dyn;
                    })
                    .ToList();
            }

            if (product.HasDiscountsApplied)
            {
                var appliedDiscounts = await ctx.ProductBatchContext.AppliedDiscounts.GetOrLoadAsync(product.Id);

                dynObject.AppliedDiscounts = appliedDiscounts
                    .Select(x => CreateDynamic(x))
                    .ToList();
            }

            if (product.IsDownload)
            {
                var downloads = await ctx.ProductBatchContext.Downloads.GetOrLoadAsync(product.Id);

                dynObject.Downloads = downloads
                    .Select(x => CreateDynamic(x))
                    .ToList();
            }

            dynObject.ProductTags = productTags
                .Select(x =>
                {
                    var localizedName = ctx.GetTranslation(x, nameof(x.Name), x.Name);
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Name = localizedName;
                    dyn.SeName = SeoHelper.BuildSlug(localizedName, _seoSettings);
                    dyn._Localized = GetLocalized(ctx, x, y => y.Name);

                    return dyn;
                })
                .ToList();

            dynObject.ProductSpecificationAttributes = specificationAttributes
                .Select(x => ToDynamic(x, ctx))
                .ToList();

            if (product.ProductType == ProductType.BundledProduct)
            {
                var bundleItems = await ctx.ProductBatchContext.ProductBundleItems.GetOrLoadAsync(product.Id);

                dynObject.ProductBundleItems = bundleItems
                    .Select(x =>
                    {
                        dynamic dyn = new DynamicEntity(x);
                        dyn.Name = ctx.GetTranslation(x, nameof(x.Name), x.Name);
                        dyn.ShortDescription = ctx.GetTranslation(x, nameof(x.ShortDescription), x.ShortDescription);
                        dyn._Localized = GetLocalized(ctx, x, y => y.Name, y => y.ShortDescription);

                        return dyn;
                    })
                    .ToList();
            }

            var mediaFiles = await ApplyMediaFiles(dynObject, product, ctx, productContext);
            await ApplyExportFeatures(dynObject, product, price, mediaFiles, ctx, productContext);

            return dynObject;
        }

        /// <summary>
        /// Applies data of media files (product pictures) to an expando object.
        /// </summary>
        private async Task<IEnumerable<ProductMediaFile>> ApplyMediaFiles(dynamic dynObject, Product product, DataExporterContext ctx, DynamicProductContext productContext)
        {
            IEnumerable<ProductMediaFile> mediaFiles = await ctx.ProductBatchContext.ProductMediaFiles.GetOrLoadAsync(product.Id);
            var productPictureSize = ctx.Projection.PictureSize > 0 
                ? _mediaSettings.GetNextValidThumbnailSize(ctx.Projection.PictureSize)
                : _mediaSettings.ProductDetailsPictureSize;

            if (productContext.Combination != null)
            {
                var mediaIds = productContext.Combination.GetAssignedMediaIds();
                if (mediaIds.Any())
                {
                    mediaFiles = mediaFiles.Where(x => mediaIds.Contains(x.MediaFileId));
                }
            }

            mediaFiles = mediaFiles.Take(ctx.Projection.NumberOfMediaFiles ?? int.MaxValue);

            dynObject.ProductPictures = mediaFiles
                .OrderBy(x => x.DisplayOrder)
                .Select(x =>
                {
                    dynamic dyn = new DynamicEntity(x);
                    dyn.Picture = ToDynamic(x.MediaFile, _mediaSettings.ProductThumbPictureSize, productPictureSize, ctx);
                    return dyn;
                })
                .ToList();

            return mediaFiles;
        }

        /// <summary>
        /// Applies extra data to an expando object.
        /// Export feature flags set by the export provider controls whether and what to be exported here.
        /// </summary>
        private async Task ApplyExportFeatures(
            dynamic dynObject,
            Product product, 
            CalculatedPrice price,
            IEnumerable<ProductMediaFile> mediaFiles, 
            DataExporterContext ctx, 
            DynamicProductContext productContext)
        {
            if (ctx.Supports(ExportFeatures.CanProjectDescription))
            {
                await ApplyProductDescription(dynObject, product, ctx);
            }

            if (ctx.Supports(ExportFeatures.OffersBrandFallback))
            {
                string brand = null;
                var productManus = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);

                if (productManus?.Any() ?? false)
                {
                    var manufacturer = productManus.First().Manufacturer;
                    brand = ctx.GetTranslation(manufacturer, nameof(manufacturer.Name), manufacturer.Name);
                }
                if (brand.IsEmpty())
                {
                    brand = ctx.Projection.Brand;
                }

                dynObject._Brand = brand;
            }

            if (ctx.Supports(ExportFeatures.CanIncludeMainPicture))
            {
                var imageQuery = ctx.Projection.PictureSize > 0 
                    ? new ProcessImageQuery { MaxWidth = _mediaSettings.GetNextValidThumbnailSize(ctx.Projection.PictureSize) } 
                    : null;

                if (mediaFiles?.Any() ?? false)
                {
                    var file = _mediaService.ConvertMediaFile(mediaFiles.Select(x => x.MediaFile).First());
                    
                    dynObject._MainPictureUrl = _mediaService.GetUrl(file, imageQuery, ctx.Store.GetHost());
                    dynObject._MainPictureRelativeUrl = _mediaService.GetUrl(file, imageQuery);
                }
                else if (!_catalogSettings.HideProductDefaultPictures)
                {
                    // Get fallback image URL.
                    dynObject._MainPictureUrl = _mediaService.GetUrl(null, imageQuery, ctx.Store.GetHost());
                    dynObject._MainPictureRelativeUrl = _mediaService.GetUrl(null, imageQuery);
                }
                else
                {
                    dynObject._MainPictureUrl = null;
                    dynObject._MainPictureRelativeUrl = null;
                }
            }

            if (ctx.Supports(ExportFeatures.UsesSkuAsMpnFallback) && product.ManufacturerPartNumber.IsEmpty())
            {
                dynObject.ManufacturerPartNumber = product.Sku;
            }

            if (ctx.Supports(ExportFeatures.OffersShippingTimeFallback))
            {
                dynamic deliveryTime = dynObject.DeliveryTime;
                dynObject._ShippingTime = deliveryTime == null ? ctx.Projection.ShippingTime : deliveryTime.Name;
            }

            if (ctx.Supports(ExportFeatures.OffersShippingCostsFallback))
            {
                dynObject._FreeShippingThreshold = ctx.Projection.FreeShippingThreshold;

                dynObject._ShippingCosts = product.IsFreeShipping || (ctx.Projection.FreeShippingThreshold.HasValue && (decimal)dynObject.Price >= ctx.Projection.FreeShippingThreshold.Value)
                    ? decimal.Zero
                    : ctx.Projection.ShippingCosts;
            }

            if (ctx.Supports(ExportFeatures.UsesOldPrice))
            {
                if (product.OldPrice != decimal.Zero && product.OldPrice != (decimal)dynObject.Price && !(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing))
                {
                    if (ctx.Projection.ConvertNetToGrossPrices)
                    {
                        var tax = await _taxCalculator.CalculateProductTaxAsync(product, product.OldPrice, true, ctx.ContextCustomer, ctx.ContextCurrency);
                        dynObject._OldPrice = tax.Price;
                    }
                    else
                    {
                        dynObject._OldPrice = product.OldPrice;
                    }
                }
                else
                {
                    dynObject._OldPrice = null;
                }
            }

            if (ctx.Supports(ExportFeatures.UsesSpecialPrice))
            {
                dynObject._SpecialPrice = null;         // Special price which is valid now.
                dynObject._FutureSpecialPrice = null;   // Special price which is valid now and in future.
                dynObject._RegularPrice = null;         // Price as if a special price would not exist.

                if (!(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing))
                {
                    if (price.OfferPrice.HasValue && product.SpecialPriceEndDateTimeUtc.HasValue)
                    {
                        var endDate = DateTime.SpecifyKind(product.SpecialPriceEndDateTimeUtc.Value, DateTimeKind.Utc);
                        if (endDate > DateTime.UtcNow)
                        {
                            dynObject._FutureSpecialPrice = price.OfferPrice.Value.Amount;
                        }
                    }

                    dynObject._SpecialPrice = price.OfferPrice?.Amount ?? null;

                    if (price.OfferPrice.HasValue || dynObject._FutureSpecialPrice != null)
                    {
                        var clonedOptions = ctx.PriceCalculationOptions.Clone();
                        clonedOptions.IgnoreOfferPrice = true;

                        var calculationContext = new PriceCalculationContext(product, clonedOptions);
                        calculationContext.AddSelectedAttributes(productContext?.Combination?.AttributeSelection, product.Id);
                        var priceWithoutOfferPrice = await _priceCalculationService.CalculatePriceAsync(calculationContext);

                        dynObject._RegularPrice = priceWithoutOfferPrice.FinalPrice.Amount;
                    }
                }
            }
        }

        /// <summary>
        /// Applies the product description to an expando object.
        /// Projection settings controls in detail how the product description is to be exported here.
        /// </summary>
        private static async Task ApplyProductDescription(dynamic dynObject, Product product, DataExporterContext ctx)
        {
            try
            {
                var languageId = ctx.LanguageId;
                string description = "";

                // Description merging.
                if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.None)
                {
                    // Export empty description.
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ShortDescriptionOrNameIfEmpty)
                {
                    description = dynObject.FullDescription;

                    if (description.IsEmpty())
                        description = dynObject.ShortDescription;
                    if (description.IsEmpty())
                        description = dynObject.Name;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ShortDescription)
                {
                    description = dynObject.ShortDescription;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.Description)
                {
                    description = dynObject.FullDescription;
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.NameAndShortDescription)
                {
                    description = ((string)dynObject.Name).Grow((string)dynObject.ShortDescription, " ");
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.NameAndDescription)
                {
                    description = ((string)dynObject.Name).Grow((string)dynObject.FullDescription, " ");
                }
                else if (ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndShortDescription ||
                    ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndDescription)
                {
                    var productManus = await ctx.ProductBatchContext.ProductManufacturers.GetOrLoadAsync(product.Id);

                    if (productManus != null && productManus.Any())
                    {
                        var manufacturer = productManus.First().Manufacturer;
                        description = ctx.GetTranslation(manufacturer, nameof(manufacturer.Name), manufacturer.Name);
                    }

                    description = description.Grow((string)dynObject.Name, " ");
                    description = ctx.Projection.DescriptionMerging == ExportDescriptionMerging.ManufacturerAndNameAndShortDescription
                        ? description.Grow((string)dynObject.ShortDescription, " ")
                        : description.Grow((string)dynObject.FullDescription, " ");
                }

                // Append text.
                if (ctx.Projection.AppendDescriptionText.HasValue() && ((string)dynObject.ShortDescription).IsEmpty() && ((string)dynObject.FullDescription).IsEmpty())
                {
                    var appendText = ctx.Projection.AppendDescriptionText.SplitSafe(',').ToArray();
                    if (appendText.Length > 0)
                    {
                        var rnd = CommonHelper.GenerateRandomInteger(0, appendText.Length - 1);
                        description = description.Grow(appendText.ElementAtOrDefault(rnd), " ");
                    }
                }

                // Remove critical characters.
                if (description.HasValue() && ctx.Projection.RemoveCriticalCharacters)
                {
                    foreach (var str in ctx.Projection.CriticalCharacters.SplitSafe(','))
                    {
                        description = description.Replace(str, "");
                    }
                }

                // Convert to plain text.
                if (description.HasValue() && ctx.Projection.DescriptionToPlainText)
                {
                    //Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
                    //description = HttpUtility.HtmlDecode(reg.Replace(description, ""));

                    description = HtmlUtility.ConvertHtmlToPlainText(description);
                    description = HtmlUtility.StripTags(HttpUtility.HtmlDecode(description));
                }

                dynObject.FullDescription = description.TrimSafe();
            }
            catch 
            { 
            }
        }
    }

    internal class DynamicProductContext
    {
        public string SeName { get; set; }
        public string AbsoluteProductUrl { get; set; }
        public ICollection<ProductVariantAttributeCombination> Combinations { get; set; }
        public ProductVariantAttributeCombination Combination { get; set; }
    }
}
