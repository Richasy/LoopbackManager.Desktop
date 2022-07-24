// Copyright (c) Richasy. All rights reserved.

using System;
using System.IO;
using LoopbackManager.App.Enums;
using LoopbackManager.App.Models;
using LoopbackManager.App.Toolkits;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ReactiveUI;
using Windows.ApplicationModel;
using Windows.Graphics;
using WinRT.Interop;

namespace LoopbackManager.App.ViewModels
{
    internal sealed partial class AppViewModel : ReactiveObject, IDisposable
    {
        public AppViewModel()
        {
            LoadVersioNumber();
            _mainWindowSubscription = this.WhenAnyValue(x => x.MainWindow)
                .WhereNotNull()
                .Subscribe(w => InitializeMainWindow());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 显示提示.
        /// </summary>
        /// <param name="message">消息内容.</param>
        /// <param name="type">消息类型.</param>
        public void ShowTip(string message, InfoType type = InfoType.Information)
            => RequestShowTip?.Invoke(this, new AppTipNotificationEventArgs(message, type));

        /// <summary>
        /// 设置主窗口.
        /// </summary>
        /// <param name="mainWindow">主窗口对象.</param>
        internal void SetMainWindow(Window mainWindow)
            => MainWindow = mainWindow;

        /// <summary>
        /// 加载当前版本号.
        /// </summary>
        private void LoadVersioNumber()
        {
            var version = Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        /// <summary>
        /// 初始化主窗口内容.
        /// </summary>
        /// <param name="mainWindow">主窗口.</param>
        private void InitializeMainWindow()
        {
            MainWindowHandle = WindowNative.GetWindowHandle(MainWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(MainWindowHandle);
            AppWindow = AppWindow.GetFromWindowId(windowId);
            AppWindow.Closing += OnAppWindowClosing;
            AppWindow.Title = ResourceToolkit.GetLocaleString(LanguageNames.AppName);
            var path = Path.Combine(Package.Current.InstalledPath, "Assets/favicon.ico");
            AppWindow.SetIcon(path);
            AppToolkit.InitializeTitleBar(AppWindow.TitleBar);
            RestoreWindowSizeAndPosition();
            DispatcherQueue = MainWindow.DispatcherQueue;
        }

        private void RestoreWindowSizeAndPosition()
        {
            var actualWidth = AppToolkit.GetScalePixel(MinWindowWidth, MainWindowHandle);
            var actualHeight = AppToolkit.GetScalePixel(MinWindowHeight, MainWindowHandle);
            AppWindow.Resize(new SizeInt32(actualWidth, actualHeight));
        }

        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args) => Dispose();

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mainWindowSubscription?.Dispose();
                }

                _disposedValue = true;
            }
        }
    }
}
