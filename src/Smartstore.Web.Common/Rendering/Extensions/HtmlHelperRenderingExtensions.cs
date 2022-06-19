﻿using System.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering.Builders;
using Smartstore.Web.TagHelpers;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering
{
    public static class HtmlHelperRenderingExtensions
    {
        // Get the protected "HtmlHelper.GenerateEditor" method
        private readonly static MethodInfo GenerateEditorMethod = typeof(HtmlHelper).GetMethod("GenerateEditor", BindingFlags.NonPublic | BindingFlags.Instance);

        #region EditorFor

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression)
            => EditorFor(helper, expression, null, null, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName, string htmlFieldName)
            => EditorFor(helper, expression, templateName, htmlFieldName, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName)
            => EditorFor(helper, expression, templateName, null, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName, object additionalViewData)
            => EditorFor(helper, expression, templateName, null, additionalViewData);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, object additionalViewData)
            => EditorFor(helper, expression, null, null, additionalViewData);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, 
            ModelExpression expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(expression, nameof(expression));

            if (helper is HtmlHelper htmlHelper)
            {
                return (IHtmlContent)GenerateEditorMethod.Invoke(htmlHelper, new object[] 
                { 
                    expression.ModelExplorer,
                    htmlFieldName ?? expression.Name,
                    templateName,
                    additionalViewData
                });
            }
            else
            {
                return helper.Editor(htmlFieldName ?? expression.Name, templateName, additionalViewData);
            }
        }

        #endregion

        #region DropDownList Extensions

        /// <summary>
        /// Returns a single-selection HTML select element for the enum expression with localized enum values.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration.</typeparam>
        public static IHtmlContent DropDownListForEnum<TModel, TEnum>(
            this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression) where TEnum : struct
        {
            return DropDownListForEnum(helper, expression, optionLabel: null, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML select element for the enum expression with localized enum values.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration.</typeparam>
        public static IHtmlContent DropDownListForEnum<TModel, TEnum>(
            this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression,
            object htmlAttributes) where TEnum : struct
        {
            return DropDownListForEnum(helper, expression, optionLabel: null, htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns a single-selection HTML select element for the enum expression with localized enum values.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration.</typeparam>
        public static IHtmlContent DropDownListForEnum<TModel, TEnum>(
            this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression,
            string optionLabel) where TEnum : struct
        {
            return DropDownListForEnum(helper, expression, optionLabel: optionLabel, htmlAttributes: null);
        }

        /// <summary>
        /// Returns a single-selection HTML select element for the enum expression with localized enum values.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration.</typeparam>
        public static IHtmlContent DropDownListForEnum<TModel, TEnum>(
            this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TEnum>> expression,
            string optionLabel,
            object htmlAttributes) where TEnum : struct
        {
            Guard.IsEnumType(typeof(TEnum), nameof(expression));

            return helper.DropDownListFor(
                expression,
                helper.GetLocalizedEnumSelectList(typeof(TEnum)), 
                optionLabel, 
                htmlAttributes);
        }

        #endregion

        #region ValidationMessageFor

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
        /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
        /// disabled.
        /// </returns>
        public static IHtmlContent ValidationMessageFor(this IHtmlHelper helper, ModelExpression expression)
            => ValidationMessageFor(helper, expression, null, null, null);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="message">
        /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
        /// <see cref="ModelStateDictionary"/> object. Message will always be visible but client-side
        /// validation may update the associated CSS class.
        /// </param>
        /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
        /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
        /// disabled.
        /// </returns>
        public static IHtmlContent ValidationMessageFor(this IHtmlHelper helper, ModelExpression expression, string message)
            => ValidationMessageFor(helper, expression, message, null, null);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="message">
        /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
        /// <see cref="ModelStateDictionary"/> object. Message will always be visible but client-side
        /// validation may update the associated CSS class.
        /// </param>
        /// <param name="tag">
        /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
        /// <see cref="ViewContext.ValidationMessageElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
        /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
        /// disabled.
        /// </returns>
        public static IHtmlContent ValidationMessageFor(this IHtmlHelper helper, ModelExpression expression, string message, string tag)
            => ValidationMessageFor(helper, expression, message, null, tag);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="message">
        /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
        /// <see cref="ModelStateDictionary"/> object. Message will always be visible but client-side
        /// validation may update the associated CSS class.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the
        /// (<see cref="ViewContext.ValidationMessageElement"/>) element. Alternatively, an
        /// <see cref="IDictionary{String, Object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>
        /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
        /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
        /// disabled.
        /// </returns>
        public static IHtmlContent ValidationMessageFor(this IHtmlHelper helper, ModelExpression expression, string message, object htmlAttributes)
            => ValidationMessageFor(helper, expression, message, htmlAttributes, null);

        /// <summary>
        /// Returns the validation message if an error exists in the <see cref="ModelStateDictionary"/>
        /// object for the specified <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <param name="message">
        /// The message to be displayed. If <c>null</c> or empty, method extracts an error string from the
        /// <see cref="ModelStateDictionary"/> object. Message will always be visible but client-side
        /// validation may update the associated CSS class.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the
        /// (<see cref="ViewContext.ValidationMessageElement"/>) element. Alternatively, an
        /// <see cref="IDictionary{String, Object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <param name="tag">
        /// The tag to wrap the <paramref name="message"/> in the generated HTML. Its default value is
        /// <see cref="ViewContext.ValidationMessageElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="IHtmlContent"/> containing the <paramref name="tag"/> element. An empty
        /// <see cref="IHtmlContent"/> if the <paramref name="expression"/> is valid and client-side validation is
        /// disabled.
        /// </returns>
        public static IHtmlContent ValidationMessageFor(this IHtmlHelper helper,
            ModelExpression expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(expression, nameof(expression));

            var htmlGenerator = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IHtmlGenerator>();
            return htmlGenerator.GenerateValidationMessage(
                helper.ViewContext,
                expression.ModelExplorer,
                expression.Name,
                message,
                tag,
                htmlAttributes);
        }

        #endregion

        #region ColorBox

        public static IHtmlContent ColorBoxFor(
            this IHtmlHelper helper,
            ModelExpression expression, 
            string defaultColor = null)
        {
            Guard.NotNull(expression, nameof(expression));

            return ColorBox(helper, expression.Name, expression.Model?.ToString().EmptyNull(), defaultColor);
        }

        public static IHtmlContent ColorBoxFor<TModel>(
            this IHtmlHelper<TModel> helper, 
            Expression<Func<TModel, string>> expression, 
            string defaultColor = null)
        {
            Guard.NotNull(expression, nameof(expression));

            return ColorBox(helper, helper.NameFor(expression), helper.ValueFor(expression), defaultColor);
        }

        public static IHtmlContent ColorBox(
            this IHtmlHelper helper, 
            string name, 
            string color, 
            string defaultColor = null)
        {
            defaultColor = defaultColor.EmptyNull();
            var isDefault = color.EqualsNoCase(defaultColor);

            var builder = new HtmlContentBuilder();

            builder.AppendHtml("<div class='input-group colorpicker-component sm-colorbox edit-control' data-fallback-color='{0}' data-editor='color'>".FormatInvariant(defaultColor));

            builder.AppendHtml(helper.TextBox(name, isDefault ? string.Empty : color, new { @class = "form-control colorval", placeholder = defaultColor }));
            builder.AppendFormat("<div class='input-group-append input-group-addon'><div class='input-group-text'><i class='thecolor' style='{0}'>&nbsp;</i></div></div>", defaultColor.HasValue() ? "background-color: " + defaultColor : string.Empty);

            builder.AppendHtml("</div>");

            return builder;
        }

        #endregion

        #region Labels & Hints

        public static IHtmlContent SmartLabelFor<TModel, TResult>(this IHtmlHelper<TModel> helper,
            Expression<Func<TModel, TResult>> expression,
            object htmlAttributes = null)
        {
            return SmartLabelFor(helper, expression, true, htmlAttributes);
        }

        public static IHtmlContent SmartLabelFor<TModel, TResult>(this IHtmlHelper<TModel> helper, 
            Expression<Func<TModel, TResult>> expression, 
            bool displayHint,
            object htmlAttributes = null)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var metadata = modelExpression.Metadata;
            var labelText = metadata.DisplayName ?? metadata.PropertyName?.SplitPascalCase();
            var hintText = displayHint ? metadata.Description : null;

            return SmartLabel(helper, modelExpression.Name, labelText, hintText, htmlAttributes);
        }

        public static IHtmlContent SmartLabel(this IHtmlHelper helper, 
            string expression, 
            string labelText, 
            string hint = null, 
            object htmlAttributes = null)
        {
            var div = new TagBuilder("div");
            div.Attributes["class"] = "ctl-label";

            if (expression.IsEmpty() && labelText.IsEmpty())
            {
                div.InnerHtml.AppendHtml("<label>&nbsp;</label>");
            }
            else
            {
                div.InnerHtml.AppendHtml(helper.Label(expression, labelText, htmlAttributes));
            }

            if (hint.HasValue())
            {
                div.InnerHtml.AppendHtml(helper.HintTooltip(hint));
            }

            return div;
        }

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltipFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return HintTooltip(helper, hintText);
        }

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltip(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create a
            var a = new TagBuilder("a");
            a.Attributes.Add("href", "#");
            a.Attributes.Add("onclick", "return false;");
            //a.Attributes.Add("rel", "tooltip");
            a.Attributes.Add("title", hintText);
            a.Attributes.Add("tabindex", "-1");
            a.Attributes.Add("class", "hint");

            // Create symbol
            var img = new TagBuilder("i");
            img.Attributes.Add("class", "fa fa-question-circle");

            a.InnerHtml.SetHtmlContent(img);

            // Return <a> tag
            return a;
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent HintFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return Hint(helper, hintText);
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent Hint(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create <small>
            var small = new TagBuilder("small");
            small.Attributes.Add("class", "form-text text-muted");

            small.InnerHtml.SetContent(hintText);

            return small;
        }

        #endregion

        #region SelectList

        private static readonly SelectListItem _singleEmptyItem = new() { Text = string.Empty, Value = string.Empty };

        public static IEnumerable<SelectListItem> GetLocalizedEnumSelectList(this IHtmlHelper helper, Type enumType)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.IsEnumType(enumType, nameof(enumType));

            var requestServices = helper.ViewContext.HttpContext.RequestServices;
            var metadataProvider = requestServices.GetService<IModelMetadataProvider>();
            var metadata = metadataProvider.GetMetadataForType(enumType);
            if (!metadata.IsEnum)
            {
                throw new ArgumentException($"The type '{enumType.FullName}' is not supported. Type must be an {nameof(Enum).ToLowerInvariant()}.");
            }
            
            var localizationService = requestServices.GetService<ILocalizationService>();
            var selectList = new List<SelectListItem>();
            var groupList = new Dictionary<string, SelectListGroup>();
            var enumTypeName = enumType.GetAttribute<EnumAliasNameAttribute>(false)?.Name ?? enumType.Name;

            foreach (var kvp in metadata.EnumGroupedDisplayNamesAndValues)
            {
                var selectListItem = new SelectListItem
                {
                    Text = GetLocalizedEnumValue(kvp.Key.Name),
                    Value = kvp.Value,
                };

                if (kvp.Key.Group.HasValue())
                {
                    if (!groupList.ContainsKey(kvp.Key.Group))
                    {
                        groupList[kvp.Key.Group] = new SelectListGroup { Name = kvp.Key.Group };
                    }

                    selectListItem.Group = groupList[kvp.Key.Group];
                }

                selectList.Add(selectListItem);
            }

            if (metadata.IsNullableValueType)
            {
                selectList.Insert(0, _singleEmptyItem);
            }

            return selectList;

            string GetLocalizedEnumValue(string enumValue)
            {
                var resourceName = string.Format($"Enums.{enumTypeName}.{enumValue}");
                var result = localizationService.GetResource(resourceName, logIfNotFound: false, returnEmptyIfNotFound: true);
                return result.NullEmpty() ?? enumValue.Titleize();
            }
        }

        #endregion

        #region LocalizedEditor

        public static IHtmlContent LocalizedEditor<TModel, TLocalizedModelLocal>(this IHtmlHelper<TModel> helper,
            string name,
            Func<int, HelperResult> localizedTemplate,
            Func<TModel, HelperResult> masterTemplate)
            where TModel : ILocalizedModel<TLocalizedModelLocal>
            where TLocalizedModelLocal : ILocalizedLocaleModel
        {
            // INFO: We cannot rely on HelperResult's deferred rendering strategy here, because then client validation rules
            // are missing in the output. Instead we immediately render the content to an HtmlString instance.
            
            var locales = helper.ViewData.Model.Locales;
            var hasMasterTemplate = masterTemplate != null;

            if (locales.Count > 1)
            {
                var services = helper.ViewContext.HttpContext.RequestServices;
                var db = services.GetRequiredService<SmartDbContext>();
                var languageService = services.GetRequiredService<ILanguageService>();
                var localizationService = services.GetRequiredService<ILocalizationService>();
                var tabs = new List<TabItem>(locales.Count + 1);
                var languages = new List<Language>(locales.Count + 1);
                
                // Create the parent tabstrip
                var strip = new TabStripTagHelper 
                {
                    ViewContext = helper.ViewContext,
                    Id = name,
                    SmartTabSelection = false,
                    Style = TabsStyle.Tabs,
                    PublishEvent = false
                };

                if (hasMasterTemplate)
                {
                    var masterLanguage = db.Languages.FindById(languageService.GetMasterLanguageId(), false);
                    languages.Add(masterLanguage);

                    // Add the first default tab for the master template
                    tabs.Add(new TabItem
                    {
                        Selected = true,
                        Text = localizationService.GetResource("Admin.Common.Standard"),
                        Content = masterTemplate(helper.ViewData.Model).ToHtmlString()
                    });
                }

                // Add all language specific tabs
                for (var i = 0; i < locales.Count; i++)
                {
                    var locale = locales[i];
                    var language = db.Languages.FindById(locale.LanguageId, false);
                    languages.Add(language);

                    tabs.Add(new TabItem
                    {
                        Selected = !hasMasterTemplate && i == 0,
                        Text = language.Name,
                        ImageUrl = "~/images/flags/" + language.FlagImageFileName,
                        Content = localizedTemplate(i).ToHtmlString()
                    });
                }

                // Create TagHelperContext for tabstrip.
                var stripContext = new TagHelperContext("tabstrip", new TagHelperAttributeList(), new Dictionary<object, object>(), CommonHelper.GenerateRandomDigitCode(10));

                // Must init tabstrip, otherwise "Parent" is null inside tab helpers.
                strip.Init(stripContext);

                // Create tab factory
                var tabFactory = new TabFactory(strip, stripContext);

                // Create AttributeList for tabstrip
                var stripOutputAttrList = new TagHelperAttributeList { new TagHelperAttribute("class", "nav-locales") };

                // Emulate tabstrip output
                var stripOutput = new TagHelperOutput("tabstrip", stripOutputAttrList, (_, _) => 
                {
                    // getChildContentAsync for tabstrip
                    for (var i = 0; i < tabs.Count; i++)
                    {
                        var isMaster = hasMasterTemplate && i == 0;
                        var language = languages[i];

                        tabFactory.AddAsync(builder => 
                        {
                            builder.Item = tabs[i];
                            builder
                                .HtmlAttributes("title", language.Name, !isMaster)
                                .ContentHtmlAttributes(new
                                {
                                    @class = "locale-editor-content",
                                    data_lang = language.LanguageCulture,
                                    data_rtl = language.Rtl.ToString().ToLower()
                                });
                        }).GetAwaiter().GetResult();
                    }

                    // We don't need the child content for tabstrip. It builds everything without any child content.
                    return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
                });

                // Process parent tabstrip
                strip.ProcessAsync(stripContext, stripOutput).GetAwaiter().GetResult();

                var wrapper = new TagBuilder("div");
                wrapper.Attributes.Add("class", "locale-editor");

                return stripOutput.WrapElementWith(wrapper);
            }
            else if (hasMasterTemplate)
            {
                return masterTemplate(helper.ViewData.Model).ToHtmlString();
            }

            return HtmlString.Empty;
        }

        #endregion

        #region Icon

        /// <summary>
        /// Generates HTML for a <c>FontAwesome (fa)</c> or a <c>Bootstrap (bi)</c> icon.
        /// </summary>
        /// <param name="name">
        /// If <c>fa</c>: the fully qualified CSS class, e.g. "far fa-envelope fa-fw".
        /// If <c>bi</c>: The icon name prefixed with <c>bi:</c>, e.g. "bi:envelope".
        /// </param>
        public static IHtmlContent Icon(this IHtmlHelper helper, string name, object htmlAttributes = null)
        {
            Guard.NotNull(name, nameof(name));

            if (name.StartsWith("bi:"))
            {
                return helper.BootstrapIcon(
                    name[3..],
                    fill: "currentColor",
                    fontScale: null,
                    animation: null,
                    transforms: null,
                    htmlAttributes: htmlAttributes);
            }

            var i = new TagBuilder("i");
            i.Attributes["class"] = name;

            if (htmlAttributes != null)
            {
                i.Attributes.Merge(CommonHelper.ObjectToStringDictionary(htmlAttributes));
            }

            return i;
        }

        public static IHtmlContent BootstrapIcon(this IHtmlHelper helper,
            string name,
            string fill = "currentColor",
            float? fontScale = null,
            CssAnimation? animation = null,
            string transforms = null,
            object htmlAttributes = null)
        {
            return helper.BootstrapIcon(
                name,
                false,
                fill, 
                fontScale, 
                animation, 
                transforms, 
                htmlAttributes);
        }

        internal static IHtmlContent BootstrapIcon(this IHtmlHelper helper, 
            string name,
            bool isStackItem = false,
            string fill = null,
            float? fontScale = null,
            CssAnimation? animation = null,
            string transforms = null,
            object htmlAttributes = null)
        {
            Guard.NotNull(name, nameof(name));

            var svg = new TagBuilder("svg");

            // Root attributes
            svg.Attributes["fill"] = fill.NullEmpty() ?? "currentColor";

            if (!isStackItem)
            {
                if (fontScale > 0)
                {
                    svg.AddCssStyle("font-size", $"{fontScale.Value * 100}%");
                }

                svg.Attributes["width"] = "1em";
                svg.Attributes["height"] = "1em";
                svg.Attributes["role"] = "img";
                svg.Attributes["focusable"] = "false";
            }

            // Use tag
            var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();
            var symbol = new TagBuilder("use");
            symbol.Attributes["xlink:href"] = urlHelper.Content($"~/lib/bi/bootstrap-icons.svg#{name}");

            var el = symbol;

            if (htmlAttributes != null)
            {
                svg.Attributes.Merge(CommonHelper.ObjectToStringDictionary(htmlAttributes));
            }

            svg.AppendCssClass("bi");

            // Animation
            if (animation != null)
            {
                var animClass = $"animate-{animation.Value.ToString().Kebaberize()}";
                if (isStackItem)
                {
                    el.AppendCssClass(animClass);
                }
                else
                {
                    svg.AppendCssClass(animClass);
                }
            }

            if (transforms.HasValue())
            {
                if (isStackItem)
                {
                    // Apply transforms to inner <g> when stacked item.
                    el = new TagBuilder("g");
                    el.InnerHtml.AppendHtml(symbol);
                }

                el.Attributes["transform"] = string.Join(' ', transforms);
            }

            svg.InnerHtml.AppendHtml(el);

            return svg;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Renderes badged product name including link to edit view. Not intended to be used in grids (use LabeledProductName there). 
        /// </summary>
        public static IHtmlContent BadgedProductName(this IHtmlHelper helper, int id, string name, string typeName, string typeLabelHint)
        {
            if (id == 0 && name.IsEmpty())
                return null;

            string namePart;

            if (id != 0)
            {
                var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
                string url = urlHelper.Content("~/Admin/Product/Edit/");
                namePart = $"<a href='{url}{id}' title='{name}'>{name}</a>";
            }
            else
            {
                namePart = $"<span>{helper.Encode(name)}</span>";
            }

            var builder = new HtmlContentBuilder();
            builder.AppendHtml($"<span class='badge badge-{typeLabelHint} mr-1'>{typeName}</span>{namePart}");
            return builder;
        }

        /// <summary>
        /// Gets the antiforgery token for the current request. Usage is intended for ajax calls where you don't have a post form which renders the token automatically.
        /// </summary>
        public static string GetAntiforgeryToken(this IHtmlHelper helper)
        {
            var httpContext = helper.ViewContext.HttpContext;
            var antiforgery = httpContext.RequestServices.GetService<IAntiforgery>();
            var tokenSet = antiforgery.GetAndStoreTokens(httpContext);
            return tokenSet.RequestToken;
        }

        #endregion
    }
}
