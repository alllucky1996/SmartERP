﻿using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.PriceBuilder
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            // Insert menu items for list views.
            var blogMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog")
                .Icon("rss", "bi")
                .PermissionNames(PriceBuilderPermissions.Self)
                .AsItem();

            var blogPostsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog.BlogPosts")
                .Action("List", "Blog", new { area = "Admin" })
                .AsItem();

            var blogCommentsMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.ContentManagement.Blog.Comments")
                .Action("Comments", "Blog", new { area = "Admin" })
                .AsItem();

            var blogNode = new TreeNode<MenuItem>(blogMenuItem);
            var parent = modulesNode.Root.SelectNodeById("cms");
            var refNode = parent.SelectNodeById("news") ?? parent.SelectNodeById("menus");

            blogNode.InsertAfter(refNode);

            var blogPostsNode = new TreeNode<MenuItem>(blogPostsMenuItem);
            var blogCommentsNode = new TreeNode<MenuItem>(blogCommentsMenuItem);
            blogNode.Append(blogPostsNode);
            blogNode.Append(blogCommentsNode);
        }
    }
}
