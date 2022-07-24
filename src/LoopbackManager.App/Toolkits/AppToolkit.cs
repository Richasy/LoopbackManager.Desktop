// Copyright (c) Richasy. All rights reserved.

using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace LoopbackManager.App.Toolkits
{
    /// <summary>
    /// 应用工具类.
    /// </summary>
    internal static class AppToolkit
    {
        /// <summary>
        /// 获取缩放像素.
        /// </summary>
        /// <param name="pixel">传入像素.</param>
        /// <param name="windowHandle">窗口句柄.</param>
        /// <returns>转换后的像素.</returns>
        internal static int GetScalePixel(double pixel, IntPtr windowHandle)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(windowHandle);
            return Convert.ToInt32(pixel * (dpi / 96.0));
        }

        /// <summary>
        /// 获取标准像素.
        /// </summary>
        /// <param name="pixel">传入像素.</param>
        /// <param name="windowHandle">窗口句柄.</param>
        /// <returns>转换后的像素.</returns>
        internal static int GetNormalizePixel(double pixel, IntPtr windowHandle)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(windowHandle);
            return Convert.ToInt32(pixel / (dpi / 96.0));
        }

        /// <summary>
        /// 初始化标题栏.
        /// </summary>
        /// <param name="titleBar">标题栏.</param>
        internal static void InitializeTitleBar(AppWindowTitleBar titleBar)
        {
            titleBar.ExtendsContentIntoTitleBar = true;
            var app = Application.Current;
            if (app.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.BackgroundColor = Colors.Transparent;
                titleBar.InactiveBackgroundColor = Colors.White;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.DarkGray;
                titleBar.ButtonHoverBackgroundColor = Colors.LightGray;
                titleBar.ButtonHoverForegroundColor = Colors.DarkGray;
                titleBar.ButtonPressedBackgroundColor = Colors.Gray;
                titleBar.ButtonPressedForegroundColor = Colors.DarkGray;
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 240, 243, 249);
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                titleBar.BackgroundColor = Colors.Transparent;
                titleBar.InactiveBackgroundColor = Colors.Black;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 20, 20, 20);
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 40, 40, 40);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }
    }
}
