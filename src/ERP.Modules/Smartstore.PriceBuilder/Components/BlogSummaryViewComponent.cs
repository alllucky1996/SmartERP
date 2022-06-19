using Microsoft.AspNetCore.Mvc;
using Smartstore.PriceBuilder.Controllers;
using Smartstore.Web.Components;

namespace Smartstore.PriceBuilder.Components
{
    /// <summary>
    /// Component to render blog section via page builder block.
    /// </summary>
    public class BlogSummaryViewComponent : SmartViewComponent
    {
        private readonly BlogHelper _helper;

        public BlogSummaryViewComponent(BlogHelper helper)
        {
            _helper = helper;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            int? maxPostAmount,
            int? maxAgeInDays,
            bool renderHeading,
            string blogHeading,
            bool disableCommentCount,
            string postsWithTag)
        {
            var model = await _helper.PrepareBlogPostListModelAsync(maxPostAmount, maxAgeInDays, renderHeading, blogHeading, disableCommentCount, postsWithTag);
            model.RssToLinkButton = true;

            return View(model);
        }
    }
}
