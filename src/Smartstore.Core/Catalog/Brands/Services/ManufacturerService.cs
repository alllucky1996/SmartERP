﻿using System.Runtime.CompilerServices;
using Dasync.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Brands
{
    public partial class ManufacturerService : IManufacturerService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public ManufacturerService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public virtual async Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdsAsync(int[] productIds, bool includeHidden = false)
        {
            Guard.NotNull(productIds, nameof(productIds));

            if (!productIds.Any())
            {
                return new List<ProductManufacturer>();
            }

            var storeId = includeHidden ? 0 : _storeContext.CurrentStore.Id;
            var customerRoleIds = includeHidden ? null : _workContext.CurrentCustomer.GetRoleIds();

            var manufacturersQuery = _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(includeHidden, customerRoleIds, storeId);

            var productManufacturerQuery = _db.ProductManufacturers
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Manufacturer).ThenInclude(x => x.MediaFile)
                .Include(x => x.Manufacturer).ThenInclude(x => x.AppliedDiscounts);

            var query =
                from pm in productManufacturerQuery
                join m in manufacturersQuery on pm.ManufacturerId equals m.Id
                where productIds.Contains(pm.ProductId)
                orderby pm.DisplayOrder
                select pm;

            return await query.ToListAsync();
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesManufacturers)
            {
                return null;
            }

            var customerRoleIds = _workContext.CurrentCustomer.GetRoleIds();

            var query = _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(false, customerRoleIds, context.RequestStoreId);

            return new ManufacturerXmlSitemapResult { Query = query };
        }


        class ManufacturerXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<Manufacturer> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var manufacturers = await Query.Select(x => new { x.Id, x.UpdatedOnUtc }).ToListAsync(cancelToken);

                await foreach (var x in manufacturers)
                {
                    yield return new NamedEntity { EntityName = nameof(Manufacturer), Id = x.Id, LastMod = x.UpdatedOnUtc };
                }
            }

            public override int Order => int.MinValue + 100;
        }
    }
}
