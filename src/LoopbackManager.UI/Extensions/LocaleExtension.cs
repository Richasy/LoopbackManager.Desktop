// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.Models.Constants;
using LoopbackManager.UI.Toolkits;
using Microsoft.UI.Xaml.Markup;

namespace LoopbackManager.UI.Extensions;

/// <summary>
/// Localized text extension.
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class LocaleExtension : MarkupExtension
{
    /// <summary>
    /// Language name.
    /// </summary>
    public StringNames Name { get; set; }

    /// <inheritdoc/>
    protected override object ProvideValue()
        => ResourceToolkit.GetLocalizedString(Name);
}
