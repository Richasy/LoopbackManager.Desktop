// Copyright (c) Richasy. All rights reserved.

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 程序条目模型.
/// </summary>
public sealed partial class ProgramItemViewModel
{
    private bool _isOriginalLoopback;

    [ObservableProperty]
    private string _containerName;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _workingDirectory;

    [ObservableProperty]
    private bool _isLoopback;

    [ObservableProperty]
    private string _packageFullName;

    [ObservableProperty]
    private string _sid;

    /// <summary>
    /// 本地回环状态是否发生了改变.
    /// </summary>
    public bool IsLoopbackChanged => IsLoopback != _isOriginalLoopback;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProgramItemViewModel model && Sid == model.Sid;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Sid);
}
