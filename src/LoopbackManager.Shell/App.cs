using LoopbackManager.Shell.Toolkits;
using Sprout;

namespace LoopbackManager.Shell;

/// <summary>
/// The application. <see cref="OnLaunched"/> opens a Mica window, points it at a reactive view, and shows it. The
/// window follows the system light/dark theme; press <b>Ctrl+T</b> to cycle System → Light → Dark.
/// </summary>
public sealed class App : Application
{
    private const string WindowPlacementSetting = "WindowPlacement";

    private Window _window = null!;

    /// <inheritdoc/>
    protected override void OnLaunched(LaunchEventArgs e)
    {
        var lastPlacement = SettingsToolkit.ReadLocalSetting(WindowPlacementSetting, string.Empty);
        _window = OpenWindow(new WindowSpec
        {
            Title = Resources.AppName,
            Placement = WindowPlacement.TryParse(lastPlacement, out var placement) ? placement : null,
            MinSize = new(520, 560),
            Size = new(612, 740),
            StartupLocation = WindowStartupLocation.CenterScreen,
            SystemBackdrop = SystemBackdrop.Mica,
        });

        _window.ActualThemeChanged += (_, _) => SyncBackground();
        _window.Closing += OnClosing;
        SyncBackground();

        _window.SetContent(new AppRoot());
        _window.Show();

        // Kick off the initial load on startup (fire-and-forget): the shared store flips Apps Idle → Loading →
        // Success/Error, and every section reading it (the list via FilteredApps, the header/footer via CanSave etc.)
        // re-renders through the store's signals.
        _ = Application.Current.Services.GetRequiredService<AppStore>().ReloadAsync();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
        => SettingsToolkit.WriteLocalSetting(WindowPlacementSetting, _window.SavePlacement().Serialize());

    private void SyncBackground() => _window.Background = _window.Theme.Colors.SolidBackgroundFillColorBase;
}
