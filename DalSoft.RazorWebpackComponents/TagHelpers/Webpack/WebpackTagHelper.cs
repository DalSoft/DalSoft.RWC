using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DalSoft.RazorWebpackComponents.TagHelpers.Webpack
{
    [HtmlTargetElement("webpack", TagStructure = TagStructure.WithoutEndTag)]
    public class WebpackTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // Reset the TagName. We don't want `component` to render.

            if (ViewContext.HttpContext.Items.All(x => x.Key.ToString() != ViewContext.ExecutingFilePath))
                ViewContext.HttpContext.Items.Add(ViewContext.ExecutingFilePath, nameof(WebpackTagHelper));

            if (!context.Items.TryGetValue(nameof(WebpackContext), out var result))
                return;
            
            var webpackContext = (WebpackContext)result;
            webpackContext.ShouldRender = true; // Notify parent that we need the css tags
        }
    }

    public abstract class WebpackComponentTagHelperBase<TComponent> : TagHelper where TComponent : IComponent
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // Reset the TagName. We don't want `component` to render.

            var executingFilePath = TypeToToExecutingFilePath();
            if (ViewContext.HttpContext.Items.All(x => x.Key.ToString() != executingFilePath))
                ViewContext.HttpContext.Items.Add(executingFilePath, nameof(WebpackTagHelper));

            if (!context.Items.TryGetValue(nameof(WebpackContext), out var result))
                return;

            var webpackContext = (WebpackContext)result;
            webpackContext.ShouldRender = true; // Notify parent that we need the css tags
        }

        private static string TypeToToExecutingFilePath()
        {
            var executingFilePath = typeof(TComponent).FullName ?? string.Empty; // WebpackHeadOrBodyTagHelperBase expects executingFilePath
            var prefixPosition = executingFilePath.IndexOf(WebpackHeadOrBodyTagHelperBase.GeneratedComponentPrefix, StringComparison.Ordinal) + WebpackHeadOrBodyTagHelperBase.GeneratedComponentPrefix.Length;
            
            executingFilePath = $"{executingFilePath?.Substring(prefixPosition, executingFilePath.Length - prefixPosition)}";
            executingFilePath = executingFilePath.Replace(".", "/");
            executingFilePath = $"/Pages{executingFilePath}.cshtml";

            return executingFilePath;
        }
    }


}