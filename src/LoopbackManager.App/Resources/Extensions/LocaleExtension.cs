// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.App.Enums;
using LoopbackManager.App.Toolkits;
using Microsoft.UI.Xaml.Markup;

namespace LoopbackManager.App.Resources.Extensions
{
    /// <summary>
    /// Localized text extension.
    /// </summary>
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    internal sealed class LocaleExtension : MarkupExtension
    {
        /// <summary>
        /// Language name.
        /// </summary>
        public LanguageNames Name { get; set; }

        /// <inheritdoc/>
        protected override object ProvideValue() => ResourceToolkit.GetLocaleString(Name);
    }
}
