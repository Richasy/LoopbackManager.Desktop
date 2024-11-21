// Copyright (c) Richasy. All rights reserved.

using System;
using System.Threading.Tasks;
using LoopbackManager.Models.Constants;
using LoopbackManager.UI.Toolkits;
using LoopbackManager.UI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Richasy.WinUI.Share.Base;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace LoopbackManager.UI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowBase, ITipWindow
{
    private const int WindowMinWidth = 612;
    private const int WindowMinHeight = 740;
    private bool _isFirstActivated = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        SetTitleBar(MainTitleBar);
        Title = ResourceToolkit.GetLocalizedString(StringNames.AppName);
        this.SetIcon("Assets/logo.ico");
        MinWidth = WindowMinWidth;
        MinHeight = WindowMinHeight;
        AppViewModel.Instance.MainWindow = this;
        MainFrame.Navigate(typeof(MainPage), default);
        Activated += OnActivated;
        Closed += OnClosed;
    }

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        var popup = new TipPopup() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private static PointInt32 GetSavedWindowPosition()
    {
        var left = SettingsToolkit.ReadLocalSetting(SettingNames.WindowPositionLeft, 0);
        var top = SettingsToolkit.ReadLocalSetting(SettingNames.WindowPositionTop, 0);
        return new PointInt32(left, top);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (!_isFirstActivated)
        {
            return;
        }

        MoveAndResize();
        var isMaximized = SettingsToolkit.ReadLocalSetting(SettingNames.IsMainWindowMaximized, false);
        if (isMaximized)
        {
            (AppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        _isFirstActivated = false;
    }

    private void OnClosed(object sender, WindowEventArgs e)
    {
        Activated -= OnActivated;
        Closed -= OnClosed;
        SaveCurrentWindowStats();
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        var scaleFactor = this.GetDpiForWindow() / 96d;
        var previousWidth = SettingsToolkit.ReadLocalSetting(SettingNames.WindowWidth, 612d);
        var previousHeight = SettingsToolkit.ReadLocalSetting(SettingNames.WindowHeight, 740d);
        var width = Convert.ToInt32(previousWidth * scaleFactor);
        var height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        var lastPoint = GetSavedWindowPosition();
        var isZeroPoint = lastPoint.X == 0 && lastPoint.Y == 0;
        var isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        var left = isZeroPoint || !isValidPosition
            ? (workArea.Width - width) / 2d
            : lastPoint.X;
        var top = isZeroPoint || !isValidPosition
            ? (workArea.Height - height) / 2d
            : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        var lastPoint = GetSavedWindowPosition();
        var displayArea = DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary)
            ?? DisplayArea.Primary;
        var rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        var left = AppWindow.Position.X;
        var top = AppWindow.Position.Y;
        var isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        SettingsToolkit.WriteLocalSetting(SettingNames.IsMainWindowMaximized, (bool)isMaximized);

        if (!isMaximized)
        {
            SettingsToolkit.WriteLocalSetting(SettingNames.WindowPositionLeft, left);
            SettingsToolkit.WriteLocalSetting(SettingNames.WindowPositionTop, top);

            if (Height >= WindowMinHeight && Width >= WindowMinWidth)
            {
                SettingsToolkit.WriteLocalSetting(SettingNames.WindowHeight, Height);
                SettingsToolkit.WriteLocalSetting(SettingNames.WindowWidth, Width);
            }
        }
    }
}
