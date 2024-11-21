// Copyright (c) Richasy. All rights reserved.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 应用视图模型.
/// </summary>
public sealed partial class AppViewModel
{
    /// <summary>
    /// 版本号.
    /// </summary>
    [ObservableProperty]
    private string _version;

    /// <summary>
    /// 应用视图模型实例.
    /// </summary>
    public static AppViewModel Instance { get; } = new Lazy<AppViewModel>(() => new AppViewModel()).Value;

    /// <summary>
    /// 主窗口对象.
    /// </summary>
    public Window MainWindow { get; set; }

    /// <summary>
    /// 窗口下的Xaml Root.
    /// </summary>
    public XamlRoot XamlRoot => MainWindow.Content.XamlRoot;
}
