using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace DalSoft.RazorWebpackComponents.TagHelpers.Webpack
{
    public abstract class WebpackHeadOrBodyTagHelperBase : TagHelper
    {
        internal const string GeneratedComponentPrefix = "_Webpack.GeneratedRazorComponents";
        private static Type[] _generatedTypes = { };
        private static string _assemblyName;

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Mvc.Rendering.ViewContext"/> for the current request.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        { 
            var webpackContext = new WebpackContext();

            context.Items[nameof(WebpackContext)] = webpackContext;

            // ReSharper disable once MustUseReturnValue
            await output.GetChildContentAsync(); // Execute children, they can read WebpackContext

            if (webpackContext.ShouldRender)
                await AppendWebpackComponents(context, output);
        }

        private async Task AppendWebpackComponents(TagHelperContext context, TagHelperOutput output)
        {
            var executingFilePath = ViewContext.ExecutingFilePath;

            var isHeadTag = string.Equals(context.TagName, "head", StringComparison.OrdinalIgnoreCase);
            var isBodyTag = string.Equals(context.TagName, "body", StringComparison.OrdinalIgnoreCase);

            if (isBodyTag)
            {   // Vendor chunk Razor component
                var vendors = $"{executingFilePath.Substring(0, executingFilePath.IndexOf("/", 2))}/Vendors.cshtml";
                if (ViewContext.HttpContext.Items.All(x => x.Key.ToString() != vendors))
                    ViewContext.HttpContext.Items.Add(vendors, nameof(WebpackTagHelper));
            }

            var components = ViewContext.HttpContext.Items
                .Where(x => x.Value.ToString() == nameof(WebpackTagHelper))
                .Reverse()
                .ToDictionary(x => x.Key, x => x.Value);

            if (!components.Any())
                return;
            
            var htmlHelper = ViewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();
            (htmlHelper as IViewContextAware)?.Contextualize(ViewContext);

            foreach (var componentType in components.Keys.Select(component => GetComponent(component.ToString())).Where(componentType => componentType != null))
            {
                var result = await htmlHelper.RenderComponentAsync(componentType, RenderMode.Static, new { isBodyTag, isHeadTag });
                output.PostContent.AppendHtml(result);
            }
        }

        /// <summary>Get the component type using ExecutingFilePath convention if exists or returns null</summary>
        private Type GetComponent(string executingFilePath)
        {
            if (_assemblyName == null)
            {
                var assembly = ViewContext.ViewData.ModelMetadata.ModelType.Assembly;
                _assemblyName = assembly.GetName().Name;
                _generatedTypes = assembly.GetTypes().Where(x => x.FullName != null && x.FullName.StartsWith($"{_assemblyName}.{GeneratedComponentPrefix}")).ToArray();
            }
            
            // Remove Pages Root
            var componentTypeFullName = executingFilePath.Substring(executingFilePath.IndexOf("/", 2, StringComparison.OrdinalIgnoreCase), executingFilePath.Length - executingFilePath.IndexOf("/", 2, StringComparison.OrdinalIgnoreCase));

            // Remove Extension
            componentTypeFullName = componentTypeFullName.Substring(0, componentTypeFullName.LastIndexOf(".cshtml", StringComparison.OrdinalIgnoreCase));

            componentTypeFullName = $"{GeneratedComponentPrefix}{componentTypeFullName}";

            componentTypeFullName = $"{_assemblyName}.{componentTypeFullName.Replace("/", ".")}";

            return _generatedTypes.SingleOrDefault(x => x.FullName == componentTypeFullName);
        }
    }
}