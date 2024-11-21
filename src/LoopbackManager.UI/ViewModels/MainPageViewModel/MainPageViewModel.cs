// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using LoopbackManager.Models;
using LoopbackManager.Models.Constants;
using LoopbackManager.UI.Toolkits;
using Microsoft.UI.Dispatching;
using Richasy.WinUI.Share.Base;
using Richasy.WinUI.Share.ViewModels;
using Windows.Win32;

namespace LoopbackManager.UI.ViewModels;

/// <summary>
/// 主页视图模型.
/// </summary>
public sealed partial class MainPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    public MainPageViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    private void ResetStatus()
    {
        _totalPrograms.Clear();
        if (Programs.Count > 0)
        {
            Programs.Clear();
        }

        IsFailed = false;
        IsEmpty = false;
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        IsReloading = true;
        ResetStatus();
        List<INET_FIREWALL_APP_CONTAINER> appList = null;

        try
        {
            await Task.Run(() =>
            {
                appList = LoopbackToolkit.GetApps().ToList();
            });

            if (appList.Count == 0)
            {
                throw new InvalidOperationException("未检测到应用程序");
            }

            foreach (var app in appList)
            {
                try
                {
                    LoopbackToolkit.ConvertSidToStringSid(app.appContainerSid, out var containerSid);
                    if (string.IsNullOrEmpty(containerSid) || string.IsNullOrEmpty(app.workingDirectory))
                    {
                        continue;
                    }

                    var appName = app.displayName;
                    if (appName.StartsWith("@"))
                    {
                        unsafe
                        {
                            fixed (char* nameChars = new char[app.displayName.Length + 1])
                            {
                                if (PInvoke.SHLoadIndirectString(appName, nameChars, Convert.ToUInt32(app.displayName.Length + 1)) == 0)
                                {
                                    appName = new string(nameChars);
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(appName))
                    {
                        appName = app.packageFullName;
                    }

                    var isLoopback = LoopbackToolkit.CheckLoopback(app.appContainerSid);
                    var item = new ProgramItemViewModel(app.appContainerName, appName, app.workingDirectory, containerSid, app.packageFullName, isLoopback);
                    if (_totalPrograms.Contains(item))
                    {
                        continue;
                    }

                    _totalPrograms.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Search(SearchKeyword);
            });
        }
        catch (Exception ex)
        {
            IsFailed = true;
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            IsReloading = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
            => _totalPrograms.ForEach(p => p.IsLoopback = true);

    [RelayCommand]
    private void ResetAll()
        => _totalPrograms.ForEach(p => p.ResetLoopbackStatusCommand.Execute(default));

    [RelayCommand]
    private async Task SaveAsync()
    {
        var countEnabled = _totalPrograms.Count(p => p.IsLoopback);
        var arr = new SID_AND_ATTRIBUTES[countEnabled];
        var count = 0;

        foreach (var item in _totalPrograms.Where(p => p.IsLoopback))
        {
            arr[count].Attributes = 0;
            LoopbackToolkit.ConvertStringSidToSid(item.Sid, out var ptr);
            arr[count].Sid = ptr;
            count++;
        }

        var result = false;

        await Task.Run(() =>
        {
            result = LoopbackToolkit.NetworkIsolationSetAppContainerConfig((uint)countEnabled, arr) == 0;
        });

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (result)
            {
                _totalPrograms.ForEach(p => p.SaveLoopbackStatusCommand.Execute(default));
                CheckStatus();
            }
            else
            {
                var msg = ResourceToolkit.GetLocalizedString(StringNames.SaveFailed);
                AppViewModel.Instance.ShowTipCommand.Execute((msg, InfoType.Error));
            }
        });
    }

    [RelayCommand]
    private void Search(string keyword)
    {
        var items = string.IsNullOrEmpty(keyword)
            ? _totalPrograms
            : _totalPrograms.Where(p => p.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (Programs.Count > 0)
        {
            Programs.Clear();
        }

        items = items.OrderByDescending(p => p.IsLoopback).ThenBy(p => p.DisplayName);

        foreach (var item in items)
        {
            Programs.Add(item);
        }

        IsEmpty = Programs.Count == 0;
    }

    [RelayCommand]
    private void CheckStatus()
    {
        CanSaveOrReset = _totalPrograms.Any(p => p.IsLoopbackChanged);
        CanSelectAll = _totalPrograms.Any(p => !p.IsLoopback);
    }

    partial void OnSearchKeywordChanged(string value)
        => SearchCommand.Execute(value);
}
