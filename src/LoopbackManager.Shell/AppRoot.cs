using LoopbackManager.Shell.Controls;

using Sprout;
using Sprout.Graphics;
using Sprout.Layout;

using static Sprout.Markup;

namespace LoopbackManager.Shell;

/// <summary>
/// The window content — the app's vertical composition: the search/save <see cref="AppHeader"/> on top, the (future)
/// app list filling the middle, and the <see cref="AppFooter"/> actions at the bottom. Each section is its own control
/// that reads the shared <see cref="AppStore"/> ambiently, so this root only <b>lays them out</b>: a single-column,
/// three-row grid (auto / star / auto). The single column also sidesteps the column-span sizing issue a shared
/// two-column grid hit — the header and footer never share a track with each other.
/// </summary>
public sealed partial class AppRoot : Control
{
    public Ui Body => Grid(
        [GridLength.Star()],
        [GridLength.Auto, GridLength.Star(), GridLength.Auto],
        new AppHeader()
            .Margin(new Thickness(0f, 0f, 0f, 12f))
            .Cell(0, 0),
        new AppList()
            .Cell(0, 1),
        new AppFooter()
            .Cell(0, 2));
}
