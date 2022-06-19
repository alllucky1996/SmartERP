using Microsoft.AspNetCore.Mvc;
using Smartstore.PriceBuilder.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.PriceBuilder.Components
{
    /// <summary>
    /// Component to render blog post list via module partial & page builder block.
    /// </summary>
    public class BlogSummaryListViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(BlogPostListModel model)
        {
            return View(model);
        }
    }
}
