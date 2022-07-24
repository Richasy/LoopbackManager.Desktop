// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LoopbackManager.App.ViewModels
{
    /// <summary>
    /// 主页视图模型.
    /// </summary>
    internal sealed partial class MainPageViewModel
    {
        private readonly AppViewModel _appViewModel;
        private readonly List<ProgramItemViewModel> _totalPrograms;
        private readonly ObservableAsPropertyHelper<bool> _isReloading;

        /// <summary>
        /// 应用视图模型实例.
        /// </summary>
        public static MainPageViewModel Instance { get; } = new Lazy<MainPageViewModel>(() => new MainPageViewModel()).Value;

        /// <summary>
        /// 保存设置的命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        /// <summary>
        /// 全选命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }

        /// <summary>
        /// 重置命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ResetCommand { get; }

        /// <summary>
        /// 重载命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; }

        /// <summary>
        /// 显示的程序列表.
        /// </summary>
        public ObservableCollection<ProgramItemViewModel> Programs { get; }

        /// <summary>
        /// 是否正在重新载入.
        /// </summary>
        public bool IsReloading => _isReloading.Value;

        /// <summary>
        /// 结果是否为空.
        /// </summary>
        [Reactive]
        public bool IsEmpty { get; set; }

        /// <summary>
        /// 检索过程是否出错.
        /// </summary>
        [Reactive]
        public bool IsFailed { get; set; }

        /// <summary>
        /// 搜索关键词.
        /// </summary>
        [Reactive]
        public string SearchKeyword { get; set; }
    }
}
