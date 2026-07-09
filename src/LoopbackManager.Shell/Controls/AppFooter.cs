using Sprout;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Theming;
using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

public sealed partial class AppFooter : Control
{
    private readonly AppStore _store;

    public AppFooter() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    public Ui Body => Grid(
        [GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Star(), GridLength.Auto],
        [GridLength.Auto],
        HyperlinkButton(Resources.SelectAll, HyperlinkButtonPalette.FromTheme(Theme.Resolve().Colors))
            .OnNavigate(_ => _store.SelectAll())
            .Enabled(_store.CanSelectAll)
            .Cell(0, 0),
        Divider.Vertical(color: Theme.Resolve().Colors.DividerStrokeColorDefault).Cell(1, 0),
        HyperlinkButton(Resources.Reset, HyperlinkButtonPalette.FromTheme(Theme.Resolve().Colors))
            .OnNavigate(_ => _store.ResetAll())
            .Enabled(_store.CanSave)
            .Cell(2, 0),
        HyperlinkButton(Resources.Refresh, HyperlinkButtonPalette.FromTheme(Theme.Resolve().Colors))
            .OnNavigate(_ => _store.ReloadAsync())
            .Cell(4, 0)
        ).ColumnSpacing(12).Padding(12, 8)
        .Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault))
        .BorderBrush(Brush.Theme(ThemeColorToken.CardStrokeColorDefault))
        .BorderThickness(0f, 1f, 0f, 0f);
}
