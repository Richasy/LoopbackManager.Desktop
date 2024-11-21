// Copyright (c) Richasy. All rights reserved.

using System;
using Microsoft.UI.Xaml;

namespace LoopbackManager.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private const string Id = "Richasy.LoopbackManager";

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        FluentIcons.WinUI.Extensions.UseSegoeMetrics(this);
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var instance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(Id);
        if (!instance.IsCurrent)
        {
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            // Redirect to the existing instance
            await instance.RedirectActivationToAsync(activatedArgs);

            // Kill the current instance
            Current.Exit();
            return;
        }

        new MainWindow().Activate();
    }
}
