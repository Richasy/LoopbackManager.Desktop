// Copyright (c) Richasy. All rights reserved.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LoopbackManager.App.Toolkits;
using ReactiveUI;
using Windows.System;

namespace LoopbackManager.App.ViewModels
{
    /// <summary>
    /// 程序条目视图模型.
    /// </summary>
    internal sealed partial class ProgramItemViewModel : ReactiveObject
    {
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

            SaveLoopbackCommand = ReactiveCommand.Create(SaveLoopbackStatus, outputScheduler: RxApp.MainThreadScheduler);
            ResetCommand = ReactiveCommand.Create(ResetLoopbackStatus, outputScheduler: RxApp.MainThreadScheduler);
            OpenWorkFolderCommand = ReactiveCommand.CreateFromTask(OpenWorkFolderAsync, outputScheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.IsLoopback)
                .Select(_ => Unit.Default)
                .InvokeCommand(MainPageViewModel.Instance.CheckStatusCommand);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ProgramItemViewModel model && Sid == model.Sid;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Sid);

        private void SaveLoopbackStatus()
            => _isOriginalLoopback = IsLoopback;

        private void ResetLoopbackStatus()
            => IsLoopback = _isOriginalLoopback;

        private async Task OpenWorkFolderAsync()
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                AppViewModel.Instance.ShowTip(ResourceToolkit.GetLocaleString(Enums.LanguageNames.NoWorkDirectory), Enums.InfoType.Warning);
            }
            else
            {
                await Launcher.LaunchFolderPathAsync(WorkingDirectory).AsTask();
            }
        }
    }
}
