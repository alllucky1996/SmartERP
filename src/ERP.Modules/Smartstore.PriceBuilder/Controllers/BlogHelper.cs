using System.Globalization;
using Smartstore.PriceBuilder.Models.Mappers;
using Smartstore.PriceBuilder.Models.Public;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.OutputCache;

namespace Smartstore.PriceBuilder.Controllers
{
    public partial class BlogHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly PriceBuilderSettings _blogSettings;

        public BlogHelper(SmartDbContext db, ICommonServices services, PriceBuilderSettings blogSettings)
        {
            _db = db;
            _services = services;
            _blogSettings = blogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<BlogPostListModel> PrepareBlogPostListModelAsync(BlogPagingFilteringModel command)
        {
            Guard.NotNull(command, nameof(command));

            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel();
            model.PagingFilteringContext.Tag = command.Tag;
            model.PagingFilteringContext.Month = command.Month;

            if (command.PageSize <= 0)
                command.PageSize = _blogSettings.PostsPageSize;
            if (command.PageNumber <= 0)
                command.PageNumber = 1;

            DateTime? dateFrom = command.GetFromMonth();
            DateTime? dateTo = command.GetToMonth();

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(storeId, languageId, isAdmin)
                .AsQueryable();

            if (!command.Tag.HasValue())
            {
                query = query.ApplyTimeFilter(dateFrom, dateTo);
            }

            var blogPosts = command.Tag.HasValue()
                ? (await query.ToListAsync())
                    .FilterByTag(command.Tag)
                    .ToPagedList(command.PageNumber - 1, command.PageSize)
                : query.ToPagedList(command.PageNumber - 1, command.PageSize);

            var pagedBlogPosts = await blogPosts.LoadAsync();

            model.PagingFilteringContext.LoadPagedList(pagedBlogPosts);

            // Prepare SEO model.
            var parsedMonth = model.PagingFilteringContext.GetParsedMonth();
            var tag = model.PagingFilteringContext.Tag;

            
            model.StoreName = _services.StoreContext.CurrentStore.Name;

            _services.DisplayControl.AnnounceRange(pagedBlogPosts);

            model.BlogPosts = await pagedBlogPosts
                .SelectAsync(async x =>
                {
                    return await x.MapAsync(new { PrepareComments = false });
                })
                .AsyncToList();

            return model;
        }

        public async Task<BlogPostListModel> PrepareBlogPostListModelAsync(
            int? maxPostAmount,
            int? maxAgeInDays,
            bool renderHeading,
            string blogHeading,
            bool disableCommentCount,
            string postsWithTag)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel
            {
                BlogHeading = blogHeading,
                RenderHeading = renderHeading,
                RssToLinkButton = renderHeading,
                DisableCommentCount = disableCommentCount
            };

            DateTime? maxAge = null;
            if (maxAgeInDays.HasValue)
            {
                maxAge = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
            }

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(storeId, languageId, isAdmin)
                .ApplyTimeFilter(maxAge: maxAge)
                .AsQueryable();

            var blogPosts = await query.ToListAsync();

            if (!postsWithTag.IsEmpty())
            {
                blogPosts = blogPosts.FilterByTag(postsWithTag).ToList();
            }

            var pagedBlogPosts = await blogPosts
                .ToPagedList(0, maxPostAmount ?? 100)
                .LoadAsync();

            _services.DisplayControl.AnnounceRange(blogPosts);

            model.BlogPosts = await blogPosts
                .SelectAsync(async x =>
                {
                    return await x.MapAsync(new { PrepareComments = false });
                })
                .AsyncToList();

            return model;
        }
    }
}
