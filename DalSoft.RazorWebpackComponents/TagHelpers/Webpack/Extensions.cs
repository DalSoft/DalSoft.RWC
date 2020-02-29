using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DalSoft.RazorWebpackComponents.TagHelpers.Webpack
{
    public static class Extensions
    {
        public const string GeneratedComponentPrefix = "_Webpack.GeneratedRazorComponents";
        private static string _assemblyName;
        private static Type[] _generatedComponentTypes;

        /// <summary>Get the component type using ExecutingFilePath convention if exists or returns null</summary>
        public static Type GetComponent(this ModelMetadata modelMetadata, string executingFilePath)
        {
            if (_assemblyName == null)
            {
                var assembly = modelMetadata.ModelType.Assembly;
                _assemblyName = assembly.GetName().Name;
                _generatedComponentTypes = assembly.GetTypes().Where(x => x.FullName != null && x.FullName.StartsWith($"{_assemblyName}.{GeneratedComponentPrefix}")).ToArray();
            }

            // Remove Pages Root
            var componentTypeFullName = executingFilePath.Substring(executingFilePath.IndexOf("/", 2, StringComparison.OrdinalIgnoreCase), executingFilePath.Length - executingFilePath.IndexOf("/", 2, StringComparison.OrdinalIgnoreCase));

            // Remove Extension
            componentTypeFullName = componentTypeFullName.Substring(0, componentTypeFullName.LastIndexOf(".cshtml", StringComparison.OrdinalIgnoreCase));

            componentTypeFullName = $"{GeneratedComponentPrefix}{componentTypeFullName}";

            componentTypeFullName = $"{_assemblyName}.{componentTypeFullName.Replace("/", ".")}";

            return _generatedComponentTypes.SingleOrDefault(x => x.FullName == componentTypeFullName);
        }
    }
}
