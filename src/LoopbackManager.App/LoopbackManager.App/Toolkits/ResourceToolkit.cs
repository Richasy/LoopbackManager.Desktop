// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.App.Enums;
using Windows.ApplicationModel.Resources.Core;

namespace LoopbackManager.App.Toolkits
{
    internal static class ResourceToolkit
    {
        /// <summary>
        /// 获取本地化文本资源.
        /// </summary>
        /// <param name="languageName">资源名称.</param>
        /// <returns>文本资源.</returns>
        internal static string GetLocaleString(LanguageNames languageName)
            => ResourceManager.Current.MainResourceMap[$"Resources/{languageName}"].Candidates[0].ValueAsString;
    }
}
