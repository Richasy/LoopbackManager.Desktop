// Copyright (c) Richasy. All rights reserved.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Richasy.WinUI.Share.Base;
using Richasy.WinUI.Share.ViewModels;
using Windows.ApplicationModel;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 应用视图模型.
/// </summary>
public sealed partial class AppViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppViewModel"/> class.
    /// </summary>
    public AppViewModel()
    {
        var version = Package.Current.Id.Version;
        Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    /// <summary>
    /// 显示提示.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [RelayCommand]
    private async Task ShowTipAsync((string, InfoType) data)
    {
        if (MainWindow is ITipWindow tipWindow)
        {
            await tipWindow.ShowTipAsync(data.Item1, data.Item2);
        }
    }
}
