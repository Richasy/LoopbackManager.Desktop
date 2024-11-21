// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 主页视图模型.
/// </summary>
public sealed partial class MainPageViewModel
{
    private readonly List<ProgramItemViewModel> _totalPrograms = new();
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private bool _isReloading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isFailed;

    [ObservableProperty]
    private string _searchKeyword;

    [ObservableProperty]
    private bool _canSaveOrReset;

    [ObservableProperty]
    private bool _canSelectAll;

    /// <summary>
    /// 应用视图模型实例.
    /// </summary>
    public static MainPageViewModel Instance { get; } = new Lazy<MainPageViewModel>(() => new MainPageViewModel()).Value;

    /// <summary>
    /// 程序列表.
    /// </summary>
    public ObservableCollection<ProgramItemViewModel> Programs { get; } = new();
}
