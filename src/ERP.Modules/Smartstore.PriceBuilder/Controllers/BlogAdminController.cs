﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.PriceBuilder.Models;
using Smartstore.PriceBuilder.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.PriceBuilder.Controllers
{
    [Route("[area]/blog/{action=index}/{id?}")]
    public class BlogAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IBlogService _blogService;
        private readonly IUrlService _urlService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICustomerService _customerService;

        public BlogAdminController(
            SmartDbContext db,
            IBlogService blogService,
            IUrlService urlService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ICustomerService customerService)
        {
            _db = db;
            _blogService = blogService;
            _urlService = urlService;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _customerService = customerService;
        }

        #region Configure settings

        [AuthorizeAdmin, Permission(PriceBuilderPermissions.Read)]
        [LoadSetting]
        public IActionResult Settings(PriceBuilderSettings settings, int storeId)
        {
            var model = MiniMapper.Map<PriceBuilderSettings, BlogSettingsModel>(settings);
            return View(model);
        }

        [AuthorizeAdmin, Permission(PriceBuilderPermissions.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Settings(BlogSettingsModel model, PriceBuilderSettings settings, int storeId)
        {
            if (!ModelState.IsValid)
            {
                return Settings(settings, storeId);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

          
            return RedirectToAction("Settings");
        }

        #endregion

        #region Utilities

        private async Task UpdateLocalesAsync(BlogPost blogPost, BlogPostModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Title, localized.Title, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Intro, localized.Intro, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Body, localized.Body, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);

                var validateSlugResult = await blogPost.ValidateSlugAsync(localized.SeName, localized.Title, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;
            }
        }

        private async Task PrepareBlogPostModelAsync(BlogPostModel model, BlogPost blogPost)
        {
            if (blogPost != null)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(blogPost);
                model.Tags = blogPost.ParseTags();
            }

            var allTags = await _blogService.GetAllBlogPostTagsAsync(0, 0, true);
            model.AvailableTags = new MultiSelectList(allTags.Select(x => x.Name).ToList(), model.AvailableTags);

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
        }

        #endregion

        #region Blog posts

        // AJAX.
        public async Task<IActionResult> AllBlogPostsAsync(string selectedIds)
        {
            var query = _db.BlogPosts().AsNoTracking();
            var pager = new FastPager<BlogPost>(query, 500);
            var allBlogPosts = new List<dynamic>();
            var ids = selectedIds.ToIntArray().ToList();

            while ((await pager.ReadNextPageAsync<BlogPost>()).Out(out var blogPosts))
            {
                foreach (var blogPost in blogPosts)
                {
                    dynamic obj = new
                    {
                        blogPost.Id,
                        blogPost.CreatedOnUtc,
                        Title = blogPost.GetLocalized(x => x.Title).Value
                    };

                    allBlogPosts.Add(obj);
                }
            }

            var data = allBlogPosts
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Title,
                    Selected = ids.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(PriceBuilderPermissions.Read)]
        public async Task<IActionResult> List()
        {
            var allTags = (await _blogService.GetAllBlogPostTagsAsync(0, 0, true))
                .Select(x => x.Name)
                .ToList();

            var model = new BlogListModel
            {
                SearchEndDate = DateTime.UtcNow,
                SearchAvailableTags = new MultiSelectList(allTags)
            };

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View(model);
        }

        [Permission(PriceBuilderPermissions.Read)]
        public async Task<IActionResult> BlogPostList(GridCommand command, BlogListModel model)
        {
            var query = _db.BlogPosts()
                .Include(x => x.Language)
                .AsNoTracking()
                .ApplyTimeFilter(model.SearchStartDate, model.SearchEndDate)
                .ApplyStandardFilter(model.SearchStoreId, model.SearchLanguageId, true)
                .Where(x => x.IsPublished == model.SearchIsPublished || model.SearchIsPublished == null);

            if (model.SearchTitle.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Title, model.SearchTitle);
            }

            if (model.SearchIntro.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Intro, model.SearchIntro);
            }

            if (model.SearchBody.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Body, model.SearchBody);
            }

            if (model.SearchTags.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Tags, model.SearchTags);
            }

            var blogPosts = await query
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<BlogPost, BlogPostModel>();
            var blogPostModels = await blogPosts
                .SelectAsync(async x => await mapper.MapAsync(x))
                .AsyncToList();

            var gridModel = new GridModel<BlogPostModel>
            {
                Rows = blogPostModels,
                Total = await blogPosts.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(PriceBuilderPermissions.Update)]
        public async Task<IActionResult> BlogPostUpdate(BlogPostModel model)
        {
            var success = false;
            var blogPost = await _db.BlogPosts().FindByIdAsync(model.Id);

            if (blogPost != null)
            {
                await MapperFactory.MapAsync(model, blogPost);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(PriceBuilderPermissions.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new BlogPostModel
            {
                CreatedOnUtc = DateTime.UtcNow,
                AllowComments = true
            };

            await PrepareBlogPostModelAsync(model, null);
            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(PriceBuilderPermissions.Create)]
        public async Task<IActionResult> Create(BlogPostModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var blogPost = await MapperFactory.MapAsync<BlogPostModel, BlogPost>(model);

                _db.BlogPosts().Add(blogPost);
                await _db.SaveChangesAsync();

                var validateSlugResult = await blogPost.ValidateSlugAsync(model.SeName, blogPost.Title, true);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(blogPost, model);
                await _storeMappingService.ApplyStoreMappingsAsync(blogPost, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, blogPost, form));
                NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Added"));

                return continueEditing 
                    ? RedirectToAction("Edit", new { id = blogPost.Id }) 
                    : RedirectToAction("List");
            }

            await PrepareBlogPostModelAsync(model, null);

            return View(model);
        }

        [Permission(PriceBuilderPermissions.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(id, false);
            if (blogPost == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<BlogPost, BlogPostModel>(blogPost);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Title = blogPost.GetLocalized(x => x.Title, languageId, false, false);
                locale.Intro = blogPost.GetLocalized(x => x.Intro, languageId, false, false);
                locale.Body = blogPost.GetLocalized(x => x.Body, languageId, false, false);
                locale.MetaKeywords = blogPost.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = blogPost.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = blogPost.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await blogPost.GetActiveSlugAsync(languageId, false, false);
            });

            await PrepareBlogPostModelAsync(model, blogPost);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(PriceBuilderPermissions.Update)]
        public async Task<IActionResult> Edit(BlogPostModel model, bool continueEditing, IFormCollection form)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(model.Id);
            if (blogPost == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, blogPost);

                var validateSlugResult = await blogPost.ValidateSlugAsync(model.SeName, blogPost.Title, true);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(blogPost, model);
                await _storeMappingService.ApplyStoreMappingsAsync(blogPost, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, blogPost, form));
                NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Updated"));

                return continueEditing 
                    ? RedirectToAction(nameof(Edit), new { id = blogPost.Id }) 
                    : RedirectToAction(nameof(List));
            }

            await PrepareBlogPostModelAsync(model, blogPost);

            return View(model);
        }

        [HttpPost]
        [Permission(PriceBuilderPermissions.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(id);
            if (blogPost == null)
            {
                return NotFound();
            }

            _db.BlogPosts().Remove(blogPost);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Deleted"));

            return RedirectToAction("List");
        }

        [HttpPost]
        [Permission(PriceBuilderPermissions.Delete)]
        public async Task<IActionResult> BlogPostDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var blogPosts = await _db.BlogPosts().GetManyAsync(ids, true);

                _db.BlogPosts().RemoveRange(blogPosts);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Comments

        [Permission(PriceBuilderPermissions.Read)]
        public IActionResult Comments(int? blogPostId)
        {
            ViewBag.BlogPostId = blogPostId;

            return View();
        }

        [HttpPost]
        [Permission(PriceBuilderPermissions.Read)]
        public async Task<IActionResult> BlogCommentList(int? blogPostId, GridCommand command)
        {
            var query = _db.CustomerContent
                .AsNoTracking()
                .OfType<BlogComment>();

            if (blogPostId.HasValue)
            {
                query = query.Where(x => x.BlogPostId == blogPostId.Value);
            }

            var comments = await query
                .Include(x => x.BlogPost)
                .Include(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = comments.Select(blogComment => new BlogCommentModel
            {
                Id = blogComment.Id,
                BlogPostId = blogComment.BlogPostId,
                BlogPostTitle = blogComment.BlogPost.GetLocalized(x => x.Title),
                CustomerId = blogComment.CustomerId,
                IpAddress = blogComment.IpAddress,
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(blogComment.CreatedOnUtc, DateTimeKind.Utc),
                CommentText = blogComment.CommentText.Truncate(270, "…"),
                CustomerName = blogComment.Customer.GetDisplayName(T),
                EditBlogPostUrl = Url.Action(nameof(Edit), "Blog", new { id = blogComment.BlogPostId }),
                EditCustomerUrl = Url.Action("Edit", "Customer", new { id = blogComment.CustomerId })
            });

            var gridModel = new GridModel<BlogCommentModel>
            {
                Rows = rows,
                Total = await comments.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        //[HttpPost]
        //[Permission(PriceBuilderPermissions.EditComment)]
        //public async Task<IActionResult> BlogCommentDelete(GridSelection selection)
        //{
        //    var success = false;
        //    var ids = selection.GetEntityIds();

        //    if (ids.Any())
        //    {
        //        var blogComments = await _db.BlogComments().GetManyAsync(ids, true);
        //        _db.BlogComments().RemoveRange(blogComments);

        //        await _db.SaveChangesAsync();
        //        success = true;
        //    }

        //    return Json(new { Success = success });
        //}

        #endregion
    }
}
