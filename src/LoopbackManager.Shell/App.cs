using LoopbackManager.Shell.Toolkits;
using Sprout;

namespace LoopbackManager.Shell;

/// <summary>
/// Opens the system-themed window and restores its last placement.
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

        // The store captures startup failures for the retryable error view.
        _ = Application.Current.Services.GetRequiredService<AppStore>().ReloadAsync();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
        => SettingsToolkit.WriteLocalSetting(WindowPlacementSetting, _window.SavePlacement().Serialize());

    private void SyncBackground() => _window.Background = _window.Theme.Colors.SolidBackgroundFillColorBase;
}
