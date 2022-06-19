﻿using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.PriceBuilder
{
    public class SettingsMenu : IMenuProvider
    {
        void IMenuProvider.BuildMenu(TreeNode<MenuItem> rootNode)
        {
            var blogMenuItem = new MenuItem().ToBuilder()
                .ResKey("Admin.Configuration.Settings.Blog")
                .Id("blog")
                .Icon("rss", "bi")
                .PermissionNames(PriceBuilderPermissions.Read)
                .Action("Settings", "Blog", new { area = "Admin" })
                .AsItem();

            var blogNode = new TreeNode<MenuItem>(blogMenuItem);
            var media = rootNode.SelectNodeById("media");
            blogNode.InsertAfter(media);
        }

        public string MenuName => "settings";

        public virtual int Ordinal => 0;
    }
}
