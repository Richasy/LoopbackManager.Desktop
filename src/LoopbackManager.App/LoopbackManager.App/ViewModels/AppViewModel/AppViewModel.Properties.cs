// Copyright (c) Richasy. All rights reserved.

using System;
using LoopbackManager.App.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ReactiveUI.Fody.Helpers;

namespace LoopbackManager.App.ViewModels
{
    /// <summary>
    /// 应用视图模型.
    /// </summary>
    internal sealed partial class AppViewModel
    {
        internal const double MinWindowWidth = 612d;
        internal const double MinWindowHeight = 740d;

        private readonly IDisposable _mainWindowSubscription;

        private bool _disposedValue;

        /// <summary>
        /// 请求显示提醒.
        /// </summary>
        public event EventHandler<AppTipNotificationEventArgs> RequestShowTip;

        /// <summary>
        /// 应用视图模型实例.
        /// </summary>
        public static AppViewModel Instance { get; } = new Lazy<AppViewModel>(() => new AppViewModel()).Value;

        /// <summary>
        /// 主窗口对象.
        /// </summary>
        [Reactive]
        public Window MainWindow { get; private set; }

        /// <summary>
        /// 主窗口句柄.
        /// </summary>
        public IntPtr MainWindowHandle { get; private set; }

        /// <summary>
        /// 应用窗口对象.
        /// </summary>
        [Reactive]
        public AppWindow AppWindow { get; private set; }

        /// <summary>
        /// 窗口下的Xaml Root.
        /// </summary>
        public XamlRoot XamlRoot => MainWindow.Content.XamlRoot;

        /// <summary>
        /// UI线程.
        /// </summary>
        public DispatcherQueue DispatcherQueue { get; private set; }

        /// <summary>
        /// 版本号.
        /// </summary>
        [Reactive]
        public string Version { get; set; }
    }
}
