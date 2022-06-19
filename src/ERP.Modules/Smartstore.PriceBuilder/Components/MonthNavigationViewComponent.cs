﻿using Microsoft.AspNetCore.Mvc;
using Smartstore.PriceBuilder.Hooks;
using Smartstore.PriceBuilder.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.PriceBuilder.Components
{
    /// <summary>
    /// Component to render month navigation on the right side of blog item list.
    /// </summary>
    public class MonthNavigationViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var settings = Services.SettingFactory.LoadSettings<PriceBuilderSettings>(storeId);

            if (!settings.Enabled)
            {
                return Empty();
            }

            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.BLOG_MONTHS_MODEL_KEY, languageId, storeId);

            var cachedModel = await Services.Cache.GetAsync(cacheKey, async () =>
            {
                var model = new List<BlogPostYearModel>();
                var blogPosts = await Services.DbContext.BlogPosts()
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId, languageId)
                    .ToListAsync();

                if (blogPosts.Count > 0)
                {
                    var months = new SortedDictionary<DateTime, int>();

                    var first = blogPosts[blogPosts.Count() - 1].CreatedOnUtc;
                    while (DateTime.SpecifyKind(first, DateTimeKind.Utc) <= DateTime.UtcNow.AddMonths(1))
                    {
                        var list = blogPosts.GetPostsByDate(
                            new DateTime(first.Year, first.Month, 1),
                            new DateTime(first.Year, first.Month, 1).AddMonths(1).AddSeconds(-1));

                        if (list.Count > 0)
                        {
                            var date = new DateTime(first.Year, first.Month, 1);
                            months.Add(date, list.Count);
                        }

                        first = first.AddMonths(1);
                    }

                    var current = 0;
                    foreach (var kvp in months.Reverse())
                    {
                        var date = kvp.Key;
                        var blogPostCount = kvp.Value;
                        if (current == 0)
                        {
                            current = date.Year;
                        }

                        if (date.Year < current || model.Count == 0)
                        {
                            var yearModel = new BlogPostYearModel
                            {
                                Year = date.Year
                            };
                            model.Add(yearModel);
                        }

                        model.Last().Months.Add(new BlogPostMonthModel
                        {
                            Month = date.Month,
                            BlogPostCount = blogPostCount
                        });

                        current = date.Year;
                    }
                }

                return model;
            });

            return View(cachedModel);
        }
    }
}
