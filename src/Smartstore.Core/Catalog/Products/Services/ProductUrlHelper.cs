﻿using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductUrlHelper
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;

        public ProductUrlHelper(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            Lazy<IUrlHelper> urlHelper,
            IHttpContextAccessor httpContextAccessor,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _urlHelper = urlHelper;
            _httpContextAccessor = httpContextAccessor;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
        }

        /// <summary>
        /// URL of the product page used to create the new product URL. Created from route if <c>null</c>.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Initial query string used to create the new query string. Usually <c>null</c>.
        /// </summary>
        public MutableQueryCollection InitialQuery { get; set; }

        /// <summary>
        /// Converts a query object into a URL query string.
        /// </summary>
        /// <param name="query">Product variant query.</param>
        /// <returns>URL query string.</returns>
        public virtual string ToQueryString(ProductVariantQuery query)
        {
            Guard.NotNull(query, nameof(query));

            var qs = InitialQuery ?? new MutableQueryCollection();
            var languageId = _workContext.WorkingLanguage.Id;

            // Checkout attributes.
            foreach (var item in query.CheckoutAttributes)
            {
                if (item.Date.HasValue)
                {
                    qs.Add(item.ToString(), string.Join("-", item.Date.Value.Year, item.Date.Value.Month, item.Date.Value.Day));
                }
                else
                {
                    qs.Add(item.ToString(), item.Value);
                }
            }

            // Gift cards.
            foreach (var item in query.GiftCards)
            {
                qs.Add(item.ToString(), item.Value);
            }

            // Variants.
            foreach (var item in query.Variants)
            {
                if (item.Alias.IsEmpty())
                {
                    item.Alias = _catalogSearchQueryAliasMapper.Value.GetVariantAliasById(item.AttributeId, languageId);
                }

                if (item.Date.HasValue)
                {
                    qs.Add(item.ToString(), string.Join("-", item.Date.Value.Year, item.Date.Value.Month, item.Date.Value.Day));
                }
                else if (item.IsFile || item.IsText)
                {
                    qs.Add(item.ToString(), item.Value);
                }
                else
                {
                    if (item.ValueAlias.IsEmpty())
                    {
                        item.ValueAlias = _catalogSearchQueryAliasMapper.Value.GetVariantOptionAliasById(item.Value.ToInt(), languageId);
                    }

                    var value = item.ValueAlias.HasValue()
                        ? $"{item.ValueAlias}-{item.Value}"
                        : item.Value;

                    qs.Add(item.ToString(), value);
                }
            }

            return qs.ToString();
        }

        /// <summary>
        /// Adds selected product variant attributes to a product variant query.
        /// </summary>
        /// <param name="query">Target product variant query.</param>
        /// <param name="source">Selected attributes.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="bundleItemId">Bundle item identifier.</param>
        /// <param name="attributes">Product variant attributes.</param>
        public virtual async Task AddAttributesToQueryAsync(
            ProductVariantQuery query,
            ProductVariantAttributeSelection source,
            int productId,
            int bundleItemId = 0,
            ICollection<ProductVariantAttribute> attributes = null)
        {
            Guard.NotNull(query, nameof(query));

            if (productId == 0 || !(source?.AttributesMap?.Any() ?? false))
            {
                return;
            }

            if (attributes == null)
            {
                var ids = source.AttributesMap.Select(x => x.Key);
                attributes = await _db.ProductVariantAttributes.GetManyAsync(ids);
            }

            var languageId = _workContext.WorkingLanguage.Id;

            foreach (var attribute in attributes)
            {
                var item = source.AttributesMap.FirstOrDefault(x => x.Key == attribute.Id);
                if (item.Key != 0)
                {
                    foreach (var originalValue in item.Value)
                    {
                        var value = originalValue.ToString();
                        DateTime? date = null;

                        if (attribute.AttributeControlType == AttributeControlType.Datepicker)
                        {
                            date = value.ToDateTime(new[] { "D" }, CultureInfo.CurrentCulture, DateTimeStyles.None, null);
                            if (date == null)
                            {
                                continue;
                            }

                            value = string.Join("-", date.Value.Year, date.Value.Month, date.Value.Day);
                        }

                        var queryItem = new ProductVariantQueryItem(value)
                        {
                            ProductId = productId,
                            BundleItemId = bundleItemId,
                            AttributeId = attribute.ProductAttributeId,
                            VariantAttributeId = attribute.Id,
                            Alias = _catalogSearchQueryAliasMapper.Value.GetVariantAliasById(attribute.ProductAttributeId, languageId),
                            Date = date,
                            IsFile = attribute.AttributeControlType == AttributeControlType.FileUpload,
                            IsText = attribute.AttributeControlType == AttributeControlType.TextBox || attribute.AttributeControlType == AttributeControlType.MultilineTextbox
                        };

                        if (attribute.IsListTypeAttribute())
                        {
                            queryItem.ValueAlias = _catalogSearchQueryAliasMapper.Value.GetVariantOptionAliasById(value.ToInt(), languageId);
                        }

                        query.AddVariant(queryItem);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="query">Product variant query.</param>
        /// <returns>Product URL.</returns>
        public virtual string GetProductUrl(string productSlug, ProductVariantQuery query)
        {
            if (productSlug.IsEmpty())
            {
                return null;
            }

            var url = Url ?? _urlHelper.Value.RouteUrl("Product", new { SeName = productSlug });
            return url.TrimEnd('/') + ToQueryString(query);
        }

        /// <summary>
        /// Creates a product URL including variant query string.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="selection">Selected attributes.</param>
        /// <returns>Product URL.</returns>
        public virtual async Task<string> GetProductUrlAsync(int productId, string productSlug, ProductVariantAttributeSelection selection)
        {
            var query = new ProductVariantQuery();
            await AddAttributesToQueryAsync(query, selection, productId);

            return GetProductUrl(productSlug, query);
        }

        /// <summary>
        /// Creates an absolute product URL.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="productSlug">Product URL slug.</param>
        /// <param name="selection">Selected attributes.</param>
        /// <param name="store">Store.</param>
        /// <param name="language">Language.</param>
        /// <returns>Absolute product URL.</returns>
        public virtual async Task<string> GetAbsoluteProductUrlAsync(
            int productId,
            string productSlug,
            ProductVariantAttributeSelection selection = null,
            Store store = null,
            Language language = null)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            if (request == null || productSlug.IsEmpty())
            {
                return null;
            }

            var url = Url;

            if (url.IsEmpty())
            {
                store ??= _storeContext.CurrentStore;
                language ??= _workContext.WorkingLanguage;

                string hostName = null;

                try
                {
                    // Do not create crappy URLs (exclude scheme, include port and no slash)!
                    hostName = new Uri(store.GetHost(true)).Authority;
                }
                catch { }

                url = _urlHelper.Value.RouteUrl(
                    "Product",
                    new { SeName = productSlug, culture = language.UniqueSeoCode },
                    store.SupportsHttps() ? "https" : "http",
                    hostName);
            }

            if (selection?.AttributesMap?.Any() ?? false)
            {
                var query = new ProductVariantQuery();
                await AddAttributesToQueryAsync(query, selection, productId);

                url = url.TrimEnd('/') + ToQueryString(query);
            }

            return url;
        }
    }
}
