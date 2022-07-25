// Copyright (c) Richasy. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LoopbackManager.App.Models;
using LoopbackManager.App.Toolkits;
using LoopbackManager.App.ViewModels;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using WinRT.Interop;

namespace LoopbackManager.App
{
    /// <summary>
    /// 主窗口.
    /// </summary>
    public sealed partial class MainWindow : Window, IDisposable
    {
        private WinProc _newWndProc = null;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private MicaController _micaController;
        private SystemBackdropConfiguration _configurationSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            DispatcherToolkit.EnsureWindowsSystemDispatcherQueueController();
            SubClassing();
            TrySetMicaBackdrop();

            AppViewModel.Instance.RequestShowTip += OnRequestShowTip;
        }

        private delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam);

        /// <inheritdoc/>
        public void Dispose() => _micaController.Dispose();

        [DllImport("user32.dll")]
        internal static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        private void SubClassing()
        {
            var windowHandle = WindowNative.GetWindowHandle(this);
            _newWndProc = new WinProc(NewWindowProc);
            _oldWndProc = SetWindowLongPtr(windowHandle, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);
        }

        private IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    {
                        var getActualPixel = AppToolkit.GetScalePixel;
                        var minMaxInfo = Marshal.PtrToStructure<PInvoke.User32.MINMAXINFO>(lParam);

                        var screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                        var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                        var width = screenWidth < AppViewModel.MinWindowWidth ? screenWidth - 40 : AppViewModel.MinWindowWidth;
                        var height = screenHeight < AppViewModel.MinWindowHeight ? screenHeight - 40 : AppViewModel.MinWindowHeight;
                        minMaxInfo.ptMinTrackSize.x = getActualPixel(width, hWnd);
                        minMaxInfo.ptMinTrackSize.y = getActualPixel(height, hWnd);
                        Marshal.StructureToPtr(minMaxInfo, lParam, true);
                        break;
                    }
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private bool TrySetMicaBackdrop()
        {
            if (MicaController.IsSupported())
            {
                // Hooking up the policy object
                _configurationSource = new SystemBackdropConfiguration();
                this.Activated += OnActivated;
                this.Closed += OnClosed;
                ((FrameworkElement)this.Content).ActualThemeChanged += OnThemeChanged;

                // Initial configuration state.
                _configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                _micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                _micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void OnActivated(object sender, WindowActivatedEventArgs args)
            => _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;

        private void OnClosed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (_micaController != null)
            {
                _micaController.Dispose();
                _micaController = null;
            }

            Activated -= OnActivated;
            _configurationSource = null;
        }

        private void OnThemeChanged(FrameworkElement sender, object args)
        {
            if (_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)Content).ActualTheme)
            {
                case ElementTheme.Dark:
                    _configurationSource.Theme = SystemBackdropTheme.Dark;
                    break;
                case ElementTheme.Light:
                    _configurationSource.Theme = SystemBackdropTheme.Light;
                    break;
                case ElementTheme.Default:
                    _configurationSource.Theme = SystemBackdropTheme.Default;
                    break;
            }
        }

        private void OnRequestShowTip(object sender, AppTipNotificationEventArgs e)
        {
            var messageType = e.Type switch
            {
                Enums.InfoType.Information => InfoBarSeverity.Informational,
                Enums.InfoType.Error => InfoBarSeverity.Error,
                Enums.InfoType.Warning => InfoBarSeverity.Warning,
                Enums.InfoType.Success => InfoBarSeverity.Success,
                _ => InfoBarSeverity.Warning,
            };

            var msg = e.Message;
            MessageInfoBar.IsOpen = true;
            MessageInfoBar.Message = msg;
            MessageInfoBar.Severity = messageType;
        }

        private void OnMainFrameLoaded(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(typeof(MainPage));
    }
}
