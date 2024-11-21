// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.UI.ViewModels;
using Richasy.WinUI.Share.Base;

namespace LoopbackManager.UI;

/// <summary>
/// 主页.
/// </summary>
public sealed partial class MainPage : MainPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    public MainPage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded()
        => ViewModel.ReloadCommand.Execute(default);
}

/// <summary>
/// 主页基类.
/// </summary>
public abstract class MainPageBase : LayoutPageBase<MainPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageBase"/> class.
    /// </summary>
    protected MainPageBase() => ViewModel = MainPageViewModel.Instance;
}
