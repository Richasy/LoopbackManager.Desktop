// Copyright (c) Richasy. All rights reserved.

using System;
using LoopbackManager.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace LoopbackManager.App
{
    /// <summary>
    /// 主页.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// <see cref="ViewModel"/> 的依赖属性.
        /// </summary>
        internal static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainPageViewModel), typeof(MainPage), new PropertyMetadata(default));

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            ViewModel = MainPageViewModel.Instance;
            Loaded += OnLoaded;
        }

        /// <summary>
        /// 视图模型.
        /// </summary>
        internal MainPageViewModel ViewModel
        {
            get { return (MainPageViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
            => ViewModel.ReloadCommand.Execute().Subscribe();
    }
}
