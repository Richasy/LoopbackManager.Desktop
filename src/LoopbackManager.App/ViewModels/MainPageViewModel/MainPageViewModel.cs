// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using LoopbackManager.App.Models;
using LoopbackManager.App.Toolkits;
using PInvoke;
using ReactiveUI;
using Splat;

namespace LoopbackManager.App.ViewModels
{
    /// <summary>
    /// 主页视图模型.
    /// </summary>
    internal sealed partial class MainPageViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
        /// </summary>
        public MainPageViewModel()
        {
            _appViewModel = AppViewModel.Instance;
            _totalPrograms = new List<ProgramItemViewModel>();
            Programs = new ObservableCollection<ProgramItemViewModel>();

            ReloadCommand = ReactiveCommand.CreateFromTask(ReloadAsync, outputScheduler: RxApp.MainThreadScheduler);
            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: RxApp.MainThreadScheduler);
            SelectAllCommand = ReactiveCommand.Create(SelectAll, outputScheduler: RxApp.MainThreadScheduler);
            ResetCommand = ReactiveCommand.Create(ResetAll, outputScheduler: RxApp.MainThreadScheduler);
            CheckStatusCommand = ReactiveCommand.Create(CheckStatus, outputScheduler: RxApp.MainThreadScheduler);

            _isReloading = ReloadCommand.IsExecuting.ToProperty(this, x => x.IsReloading, scheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.SearchKeyword)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(Search);

            ReloadCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    IsFailed = true;
                    this.Log().Debug(ex.Message + "\n" + ex.StackTrace);
                });
        }

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

        private async Task ReloadAsync()
        {
            ResetStatus();
            List<INET_FIREWALL_APP_CONTAINER> appList = null;

            await Task.Run(() =>
            {
                appList = LoopbackToolkit.GetApps().ToList();
            });

            if (appList.Count == 0)
            {
                throw new InvalidOperationException("未检测到应用程序");
            }

            var sb = new StringBuilder();
            foreach (var app in appList)
            {
                LoopbackToolkit.ConvertSidToStringSid(app.appContainerSid, out var containerSid);
                if (string.IsNullOrEmpty(containerSid) || string.IsNullOrEmpty(app.workingDirectory))
                {
                    continue;
                }

                var appName = app.displayName;
                if (appName.StartsWith("@"))
                {
                    sb.Clear();
                    var transResult = LoopbackToolkit.SHLoadIndirectString(app.displayName, sb);
                    if (transResult == (int)HResult.Code.S_OK)
                    {
                        appName = sb.ToString();
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

            _appViewModel.DispatcherQueue.TryEnqueue(() =>
            {
                Search(SearchKeyword);
            });
        }

        private void SelectAll()
            => _totalPrograms.ForEach(p => p.IsLoopback = true);

        private void ResetAll()
            => _totalPrograms.ForEach(p => p.ResetCommand.Execute().Subscribe());

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

            _appViewModel.DispatcherQueue.TryEnqueue(() =>
            {
                if (result)
                {
                    _totalPrograms.ForEach(p => p.SaveLoopbackCommand.Execute().Subscribe());
                    CheckStatus();
                }
                else
                {
                    var msg = ResourceToolkit.GetLocaleString(Enums.LanguageNames.SaveFailed);
                    _appViewModel.ShowTip(msg, Enums.InfoType.Error);
                }
            });
        }

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

        private void CheckStatus()
        {
            CanSaveOrReset = _totalPrograms.Any(p => p.IsLoopbackChanged);
            CanSelectAll = _totalPrograms.Any(p => !p.IsLoopback);
        }
    }
}
