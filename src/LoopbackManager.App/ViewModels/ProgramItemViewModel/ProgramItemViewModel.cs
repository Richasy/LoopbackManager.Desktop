// Copyright (c) Richasy. All rights reserved.

using System;
using ReactiveUI;

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
            bool isLoopback)
        {
            ContainerName = containerName;
            DisplayName = displayName;
            WorkingDirectory = workingDirectory;
            Sid = sid;
            _isOriginalLoopback = isLoopback;
            IsLoopback = isLoopback;

            SaveLoopbackCommand = ReactiveCommand.Create(SaveLoopbackStatus, outputScheduler: RxApp.MainThreadScheduler);
            ResetCommand = ReactiveCommand.Create(ResetLoopbackStatus, outputScheduler: RxApp.MainThreadScheduler);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ProgramItemViewModel model && Sid == model.Sid;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Sid);

        private void SaveLoopbackStatus()
            => _isOriginalLoopback = IsLoopback;

        private void ResetLoopbackStatus()
            => IsLoopback = _isOriginalLoopback;
    }
}
