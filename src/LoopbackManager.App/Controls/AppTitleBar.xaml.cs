// Copyright (c) Richasy. All rights reserved.

using LoopbackManager.App.Toolkits;
using LoopbackManager.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoopbackManager.App.Controls
{
    /// <summary>
    /// 应用标题栏.
    /// </summary>
    public sealed partial class AppTitleBar : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppTitleBar"/> class.
        /// </summary>
        public AppTitleBar()
        {
            InitializeComponent();
            ActualThemeChanged += OnActualThemeChanged;
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args)
            => AppToolkit.InitializeTitleBar(AppViewModel.Instance.AppWindow.TitleBar);
    }
}
