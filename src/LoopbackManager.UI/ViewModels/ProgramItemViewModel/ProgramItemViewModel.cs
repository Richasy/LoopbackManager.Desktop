// Copyright (c) Richasy. All rights reserved.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using LoopbackManager.Models.Constants;
using LoopbackManager.UI.Toolkits;
using Richasy.WinUI.Share.Base;
using Richasy.WinUI.Share.ViewModels;
using Windows.System;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 程序条目模型.
/// </summary>
public sealed partial class ProgramItemViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramItemViewModel"/> class.
    /// </summary>
    public ProgramItemViewModel(
        string containerName,
        string displayName,
        string workingDirectory,
        string sid,
        string packageFullName,
        bool isLoopback)
    {
        ContainerName = containerName;
        DisplayName = displayName;
        WorkingDirectory = workingDirectory;
        Sid = sid;
        PackageFullName = packageFullName;
        _isOriginalLoopback = isLoopback;
        IsLoopback = isLoopback;
    }

    [RelayCommand]
    private void SaveLoopbackStatus()
        => _isOriginalLoopback = IsLoopback;

    [RelayCommand]
    private void ResetLoopbackStatus()
        => IsLoopback = _isOriginalLoopback;

    [RelayCommand]
    private async Task OpenWorkFolderAsync()
    {
        if (string.IsNullOrEmpty(WorkingDirectory))
        {
            AppViewModel.Instance.ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.NoWorkDirectory), InfoType.Error));
        }
        else
        {
            await Launcher.LaunchFolderPathAsync(WorkingDirectory).AsTask();
        }
    }

    partial void OnIsLoopbackChanged(bool value)
        => MainPageViewModel.Instance.CheckStatusCommand.Execute(default);
}
