using LoopbackManager.Shell.Controls;

using Sprout;
using Sprout.Controls;
using Sprout.Layout;

using static Sprout.Markup;

namespace LoopbackManager.Shell;

/// <summary>
/// The window content — the app's vertical composition: the search/save <see cref="AppHeader"/>, a dismissible save
/// error banner, the app list, and the <see cref="AppFooter"/> actions. Each section reads the shared
/// <see cref="AppStore"/> ambiently; this root only lays them out.
/// </summary>
public sealed partial class AppRoot : Control
{
    private readonly AppStore _store;

    public AppRoot() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    public Ui Body => Grid(
        [GridLength.Star()],
        [GridLength.Auto, GridLength.Auto, GridLength.Star(), GridLength.Auto],
        new AppHeader()
            .Cell(0, 0),
        InfoBar(Resources.SaveFailedTitle, Resources.SaveFailedMessage)
            .Severity(InfoBarSeverity.Error)
            .Closable(true)
            .Open(_store.ShouldShowSaveError)
            .OnClosed(_ => _store.DismissSaveError())
            .Margin(12f, 0f)
            .Cell(0, 1),
        new AppList()
            .Cell(0, 2),
        new AppFooter()
            .Cell(0, 3));
}
