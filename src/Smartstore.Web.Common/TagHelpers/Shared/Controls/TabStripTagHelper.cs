﻿using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Events;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum TabsPosition
    {
        Top,
        Right,
        Below,
        Left
    }

    public enum TabsStyle
    {
        Tabs,
        Pills,
        Material
    }

    public class SelectedTabInfo
    {
        public string TabId { get; set; }
        public string Path { get; set; }
    }

    [OutputElementHint("div")]
    [HtmlTargetElement("tabstrip", Attributes = "id")]
    [RestrictChildren("tab", "tab-content-header")]
    public class TabStripTagHelper : SmartTagHelper
    {   
        const string HideSingleItemAttributeName = "sm-hide-single-item";
        const string ResponsiveAttributeName = "sm-responsive";
        const string BreakpointAttributeName = "sm-breakpoint";
        const string PositionAttributeName = "sm-nav-position";
        const string StyleAttributeName = "sm-nav-style";
        const string FadeAttributeName = "sm-fade";
        const string SmartTabSelectionAttributeName = "sm-smart-tab-selection";
        const string OnAjaxBeginAttributeName = "sm-onajaxbegin";
        const string OnAjaxSuccessAttributeName = "sm-onajaxsuccess";
        const string OnAjaxFailureAttributeName = "sm-onajaxfailure";
        const string OnAjaxCompleteAttributeName = "sm-onajaxcomplete";
        const string PublishEventAttributeName = "sm-publish-event";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(TabStripTagHelper)] = this;
        }

        #region Properties

        [HtmlAttributeNotBound]
        internal List<TabTagHelper> Tabs { get; set; } = new();

        [HtmlAttributeNotBound]
        internal TabContentHeaderTagHelper TabContentHeader { get; set; }

        /// <summary>
        /// Whether to hide tabstrip nav if there's only one tab item. Default = true.
        /// </summary>
        [HtmlAttributeName(HideSingleItemAttributeName)]
        public bool HideSingleItem { get; set; } = true;

        /// <summary>
        /// Whether to hide tabstrip if there's only one tab item. Default = false.
        /// </summary>
        [HtmlAttributeName(ResponsiveAttributeName)]
        public bool Responsive { get; set; }

        /// <summary>
        /// When given device expression matches, the tabstrip switches to responsive/compact mode,
        /// but only when <see cref="Responsive"/> is True (e.g.: "&gt;md", "&lt;=lg" etc.).
        /// Default = &lt;=lg
        /// </summary>
        [HtmlAttributeName(BreakpointAttributeName)]
        public string Breakpoint { get; set; }

        /// <summary>
        /// Tab nav position. Default = Top.
        /// </summary>
        [HtmlAttributeName(PositionAttributeName)]
        public TabsPosition Position { get; set; } = TabsPosition.Top;

        /// <summary>
        /// Tab nav style
        /// </summary>
        [HtmlAttributeName(StyleAttributeName)]
        public TabsStyle Style { get; set; }

        /// <summary>
        /// Whether to activate fade animations. Default = true.
        /// </summary>
        [HtmlAttributeName(FadeAttributeName)]
        public bool Fade { get; set; } = true;

        /// <summary>
        /// Whether to reselect active tab on page reload. Default = true.
        /// </summary>
        [HtmlAttributeName(SmartTabSelectionAttributeName)]
        public bool SmartTabSelection { get; set; } = true;

        [HtmlAttributeName(OnAjaxBeginAttributeName)]
        public string OnAjaxBegin { get; set; }

        [HtmlAttributeName(OnAjaxSuccessAttributeName)]
        public string OnAjaxSuccess { get; set; }

        [HtmlAttributeName(OnAjaxFailureAttributeName)]
        public string OnAjaxFailure { get; set; }

        [HtmlAttributeName(OnAjaxCompleteAttributeName)]
        public string OnAjaxComplete { get; set; }

        /// <summary>
        /// Whether to publish the <see cref="TabStripCreated"/> event.
        /// </summary>
        [HtmlAttributeName(PublishEventAttributeName)]
        public bool PublishEvent { get; set; } = true;

        #endregion

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();

            // Give integrators the chance to add tabs and widgets.
            if (PublishEvent && Id.HasValue())
            {
                var eventPublisher = ViewContext.HttpContext.RequestServices.GetRequiredService<IEventPublisher>();
                var e = new TabStripCreated(this, context);
                await  eventPublisher.PublishAsync(e);

                if (e.Widgets != null && e.Widgets.Count > 0)
                {
                    var widgetContent = new HtmlContentBuilder();

                    foreach (var widget in e.Widgets.OrderBy(x => x.Order))
                    {
                        widgetContent.AppendHtml(await widget.InvokeAsync(ViewContext, e.Model));
                        widgetContent.AppendLine();
                    }
                    
                    // Combine all custom widgets into one special tab named MODULE_WIDGETS
                    await e.TabFactory.AddAsync(builder => builder
                        .Text(EngineContext.Current.ResolveService<IText>().Get("Admin.Plugins"))
                        .Name("tab-special-module-widgets")
                        .Icon("puzzle", "bi")
                        .LinkHtmlAttributes(new { data_tab_name = "MODULE_WIDGETS" })
                        .Content(widgetContent)
                        .Ajax(false));
                }
            }

            if (Tabs.Count == 0)
            {
                output.SuppressOutput();
            }

            var hasContent = Tabs.Any(x => x.HasContent || x.Ajax);
            var isTabbable = Position != TabsPosition.Top;
            var isStacked = Position == TabsPosition.Left || Position == TabsPosition.Right;
            var classList = output.GetClassList();

            output.TagName = "div";
            classList.Add("tabbable");

            if (isTabbable)
            {
                classList.Add("tabs-{0}".FormatInvariant(Position.ToString().ToLower()));
            }

            if (SmartTabSelection)
            {
                classList.Add("tabs-autoselect");
                // TODO: (core) Rethink SetSelectedTab in StateController (after we handle DataGrid states with model binders now).
                output.Attributes.Add("data-tabselector-href", UrlHelper.Action("SetSelectedTab", "State", new { area = string.Empty }));
            }

            if (isStacked)
            {
                classList.Add("tabs-stacked");
            }

            if (OnAjaxBegin.HasValue())
            {
                output.Attributes.Add("data-ajax-onbegin", OnAjaxBegin);
            }

            if (OnAjaxSuccess.HasValue())
            {
                output.Attributes.Add("data-ajax-onsuccess", OnAjaxSuccess);
            }

            if (OnAjaxFailure.HasValue())
            {
                output.Attributes.Add("data-ajax-onfailure", OnAjaxFailure);
            }

            if (OnAjaxComplete.HasValue())
            {
                output.Attributes.Add("data-ajax-oncomplete", OnAjaxComplete);
            }

            if (Responsive)
            {
                classList.Add("nav-responsive");
                if (Breakpoint.HasValue())
                {
                    output.Attributes.Add("data-breakpoint", Breakpoint);
                }
                
            }

            // Flush classes
            classList.Dispose();

            // tab-content above nav
            if (Position == TabsPosition.Below && hasContent)
            {
                RenderTabContent(output.Content, isStacked);
            }

            // Enable smart tab selection
            string selector = null;
            if (SmartTabSelection)
            {
                selector = TrySelectRememberedTab();
            }

            // nav/items
            RenderNav(output.Content, isStacked);

            // tab-content below nav
            if (Position != TabsPosition.Below && hasContent)
            {
                RenderTabContent(output.Content, isStacked);
            }

            if (selector != null)
            {
                output.Content.AppendHtmlLine(
@"<script>
	$(function() {{
		_.delay(function() {{
			$(""{0}"").trigger(""show"");
		}}, 100);
	}})
</script>".FormatInvariant(selector));
            }

            var loadedTabNames = Tabs.Where(x => x.HasContent).Select(x => x.TabName).ToList();
            if (loadedTabNames.Count > 0)
            {
                foreach (var tabName in loadedTabNames)
                {
                    output.Content.AppendHtmlLine($"<input type='hidden' class='loaded-tab-name' name='LoadedTabs' value='{tabName}' />");
                }
            }

            if (Responsive /* && tab.TabContentHeaderContent != null*/)
            {
                output.Content.AppendHtmlLine(@"<script>$(function() {{ $('#{0}').responsiveNav(); }})</script>".FormatInvariant(Id));
            }
        }

        #region TabStrip

        private void RenderNav(TagHelperContent content, bool isStacked)
        {
            TagBuilder ul = new("ul");
            var classList = ul.GetClassList();
            classList.Add("nav");

            if (Style == TabsStyle.Tabs)
            {
                classList.Add("nav-tabs");
            }
            else if (Style == TabsStyle.Pills)
            {
                classList.Add("nav-pills");
            }
            else if (Style == TabsStyle.Material)
            {
                classList.Add("nav-tabs", "nav-tabs-line");
            }

            if (HideSingleItem && Tabs.Count == 1)
            {
                classList.Add("d-none");
            }

            if (isStacked)
            {
                classList.Add("nav-stacked", "flex-row",  "flex-lg-column");
            }

            if (Position != TabsPosition.Top)
            {
                classList.Add("nav-{0}".FormatInvariant(Position.ToString().ToLower()));
            }

            classList.Dispose();

            if (isStacked)
            {
                // opening left/right tabs col
                content.AppendHtml("<aside class=\"nav-aside\">");
            }

            content.AppendHtml(ul.RenderStartTag());

            var hasIcons = Tabs.Any(x => x.Icon.HasValue() || x.ImageUrl.HasValue());

            foreach (var tab in Tabs)
            {
                content.AppendHtml(BuildTabItem(tab, isStacked, hasIcons));
            }

            content.AppendHtml(ul.RenderEndTag());

            if (isStacked)
            {
                // closing left/right tabs col
                content.AppendHtml("</aside>");
            }
        }

        private void RenderTabContent(TagHelperContent content, bool isStacked)
        {
            if (isStacked)
            {
                // opening left/right content col
                content.AppendHtmlLine("<div class=\"nav-content\">");
            }

            content.AppendHtmlLine("<div class=\"tab-content\">");

            // Tab content header
            if (Responsive && TabContentHeader != null)
            {
                content.AppendHtml(BuildTabContentHeader(TabContentHeader));
            }

            foreach (var tab in Tabs)
            {
                content.AppendHtml(BuildTabPane(tab));
            }

            content.AppendHtmlLine("</div>");

            if (isStacked)
            {
                // closing left/right content col
                content.AppendHtmlLine("</div>");
            }
        }

        private static TagBuilder BuildTabContentHeader(TabContentHeaderTagHelper header)
        {
            TagBuilder div = new("div");

            // Copy all attributes from output to div tag
            foreach (var attr in header.Attributes)
            {
                div.MergeAttribute(attr.Name, attr.ValueAsString());
            }

            div.AppendCssClass("tab-content-header");
            div.InnerHtml.SetHtmlContent(header.Content);

            return div;
        }

        #endregion

        #region Tab Items

        private TagBuilder BuildTabPane(TabTagHelper tab)
        {
            TagBuilder div = new("div");

            var classList = div.GetClassList();
            classList.Add("tab-pane");

            div.MergeAttribute("role", "tabpanel");

            if (Fade)
            {
                classList.Add("fade");

                if (tab.Selected)
                {
                    classList.Add("show");
                }
            }

            if (tab.Selected)
            {
                classList.Add("active");
            }

            div.GenerateId(tab.Id, "-");
            div.MergeAttribute("aria-labelledby", $"{div.Attributes["id"]}-tab");
            div.MergeAttribute("data-tab-name", tab.Name);

            classList.Dispose();
            div.InnerHtml.SetHtmlContent(tab.TabInnerContent);

            return div;
        }

        private TagBuilder BuildTabItem(TabTagHelper tab, bool isStacked, bool hasIcons)
        {
            // <li [class="nav-item [d-none]"]><a href="#{id}" class="nav-link [active]" data-toggle="tab">{text}</a></li>
            TagBuilder li = new("li");

            // Copy all attributes from output to div tag (except for "id" and "href")
            foreach (var attr in tab.Attributes)
            {
                li.MergeAttribute(attr.Name, attr.ValueAsString());
            }
            li.Attributes.TryRemove("id", out _);
            li.Attributes.TryRemove("href", out _);

            li.AppendCssClass("nav-item");

            if (!tab.Selected && !tab.Visible)
            {
                li.AppendCssClass("d-none");
            }

            {
                TagBuilder a = new("a");

                // Link/Target
                var itemId = "#" + tab.Id;
                a.AppendCssClass("nav-link" + (tab.Selected ? " active" : ""));

                if (!tab.TabInnerContent.IsEmptyOrWhiteSpace)
                {
                    a.MergeAttribute("href", itemId);
                    a.MergeAttribute("data-toggle", "tab");
                    a.MergeAttribute("data-loaded", "true");
                }
                else
                {
                    // No content, create real link instead
                    var url = tab.Attributes["href"]?.ValueAsString();

                    if (url == null)
                    {
                        a.MergeAttribute("href", "#");
                    }
                    else
                    {
                        if (tab.Ajax)
                        {
                            a.MergeAttribute("href", itemId);
                            a.MergeAttribute("data-ajax-url", url);
                            a.MergeAttribute("data-toggle", "tab");
                            a.MergeAttribute("data-tab-name", tab.Name);
                        }
                        else
                        {
                            a.MergeAttribute("href", UrlHelper.Content(url));
                        }
                    }
                }

                if (tab.BadgeText.HasValue())
                {
                    a.AppendCssClass("clearfix");
                }
                
                // Icon/Image
                if (hasIcons)
                {
                    BuildTabIcon(tab, a, isStacked);
                }

                // Caption
                BuildTabCaption(tab, a);

                // Badge
                BuildTabBadge(tab, a);

                // Nav link short summary for collapsed state
                BuildTabSummary(tab, a);

                li.InnerHtml.SetHtmlContent(a);
            }

            return li;
        }

        private void BuildTabIcon(TabTagHelper tab, TagBuilder a, bool isStacked)
        {
            if (tab.Icon.HasValue())
            {
                var el = (TagBuilder)HtmlHelper.Icon(tab.Icon);

                if (isStacked)
                {
                    el.AppendCssClass("bi-fw");
                }

                el.AppendCssClass("nav-icon");
                if (tab.IconClass.HasValue())
                {
                    el.AppendCssClass(tab.IconClass);
                }

                a.InnerHtml.AppendHtml(el);
            }
            else if (tab.ImageUrl.HasValue())
            {
                TagBuilder img = new("img");
                img.Attributes["src"] = UrlHelper.Content(tab.ImageUrl);
                img.Attributes["alt"] = "Icon";
                a.InnerHtml.AppendHtml(img);
            }
            else if (isStacked)
            {
                a.InnerHtml.AppendHtml("<i class=\"fa fa-fw\"></i>");
            }
        }

        private static void BuildTabCaption(TabTagHelper tab, TagBuilder a)
        {
            TagBuilder caption = new("span");
            caption.AppendCssClass("tab-caption");
            caption.InnerHtml.Append(tab.Title);
            a.InnerHtml.AppendHtml(caption);
        }

        private void BuildTabBadge(TabTagHelper tab, TagBuilder a)
        {
            if (tab.BadgeText.HasValue())
            {
                var temp = "ml-2 badge";
                temp += " badge-" + tab.BadgeStyle.ToString().ToLower();
                if (Position == TabsPosition.Left)
                {
                    temp += " float-right"; // looks nicer 
                }

                TagBuilder span = new("span");
                span.AddCssClass(temp);
                span.InnerHtml.Append(tab.BadgeText);
                a.InnerHtml.AppendHtml(span);
            }
        }

        private void BuildTabSummary(TabTagHelper tab, TagBuilder a)
        {
            if (Responsive && tab.Summary.HasValue())
            {
                TagBuilder span = new("span");
                span.AddCssClass("nav-link-summary");
                span.InnerHtml.Append(tab.Summary);
                a.InnerHtml.AppendHtml(span);
            }
        }

        #endregion

        #region Helpers

        private static string GetTabName(TabTagHelper tab)
        {
            if (tab.Attributes.TryGetAttribute("data-tab-name", out var attr))
            {
                return attr.ValueAsString();
            }

            return null;
        }

        private static void RecalculateTabIndexes(List<TabTagHelper> tabs)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                tab.Index = i + 1;
            }
        }

        // 
        /// <summary>
        /// Returns a query selector
        /// </summary>
        private string TrySelectRememberedTab()
        {
            if (Id.IsEmpty())
                return null;

            if (ViewContext.ViewData.Model is EntityModelBase model && model.Id == 0)
            {
                // it's a "create" operation: don't select
                return null;
            }

            var rememberedTab = (SelectedTabInfo)ViewContext.TempData["SelectedTab." + Id];
            if (rememberedTab != null && rememberedTab.Path.Equals(ViewContext.HttpContext.Request.RawUrl(), StringComparison.OrdinalIgnoreCase))
            {
                // get tab to select
                var tabToSelect = GetTabById(rememberedTab.TabId);

                if (tabToSelect != null)
                {
                    // unselect former selected tab(s)
                    Tabs.Each(x => x.Selected = false);

                    // select the new tab
                    tabToSelect.Selected = true;

                    // persist again for the next request
                    ViewContext.TempData["SelectedTab." + Id] = rememberedTab;

                    if (tabToSelect.Ajax && tabToSelect.TabInnerContent.IsEmptyOrWhiteSpace)
                    {
                        return ".nav a[data-ajax-url][href='#{0}']".FormatInvariant(rememberedTab.TabId);
                    }
                }
            }

            return null;
        }

        private TabTagHelper GetTabById(string tabId)
        {
            int i = 1;
            foreach (var tab in Tabs)
            {
                var id = tab.Id;
                if (id == tabId)
                {
                    if (!tab.Visible || tab.Disabled)
                        break;

                    return tab;
                }

                i++;
            }

            return null;
        }

        #endregion
    }
}
