using Microsoft.AspNetCore.Mvc;
using Smartstore.PriceBuilder.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.PriceBuilder.Components
{
    /// <summary>
    /// Component to render blog summary heading via module partial & page builder block.
    /// </summary>
    public class BlogSummaryHeadingViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(BlogPostListModel model)
        {
            return View(model);
        }
    }
}
