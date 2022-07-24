// Copyright (c) Richasy. All rights reserved.

using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LoopbackManager.App.ViewModels
{
    /// <summary>
    /// 程序条目视图模型.
    /// </summary>
    internal sealed partial class ProgramItemViewModel
    {
        private bool _isOriginalLoopback;

        /// <summary>
        /// 保存当前本地网络回环状态的命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveLoopbackCommand { get; }

        /// <summary>
        /// 重置当前回环状态的命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }

        /// <summary>
        /// 容器名.
        /// </summary>
        [Reactive]
        public string ContainerName { get; set; }

        /// <summary>
        /// 显示名称.
        /// </summary>
        [Reactive]
        public string DisplayName { get; set; }

        /// <summary>
        /// 工作路径.
        /// </summary>
        [Reactive]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// 是否启用本地网络回环.
        /// </summary>
        [Reactive]
        public bool IsLoopback { get; set; }

        /// <summary>
        /// 标识符.
        /// </summary>
        public string Sid { get; set; }

        /// <summary>
        /// 本地回环状态是否发生了改变.
        /// </summary>
        public bool IsLoopbackChanged => IsLoopback != _isOriginalLoopback;
    }
}
