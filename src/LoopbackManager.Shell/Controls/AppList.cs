using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

/// <summary>
/// The scrolling, virtualized app list with its four load phases. It reads the shared <see cref="AppStore"/> ambiently
/// and overlays the phase views — a virtualized <c>ItemsRepeater</c> of <see cref="AppCard"/> rows when the load
/// succeeded with results, a centered spinner while loading, a retryable error, and an empty-state message — each shown
/// by <c>.Visible</c> off the store's reactive phase flags, so a load settling or a filter change re-renders the right
/// one. The phases are mutually exclusive, so exactly one overlay child is visible at a time; the <c>ItemsRepeater</c>
/// stays mounted across phases (collapsed, then reconciled in place) rather than torn down and rebuilt. Because
/// <see cref="AppCard"/> is a <c>Control</c> (a live checkbox + open-folder button per row), it goes in through the
/// two-closure <c>ItemsRepeater</c> overload; only a screenful is realized.
/// </summary>
public sealed partial class AppList : Control
{
    private readonly AppStore _store;

    public AppList() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    // Success with at least one (filtered) row → show the list.
    private bool ShowList => _store.Apps.IsSuccess && _store.FilteredApps.Count > 0;

    // Success but no (filtered) rows → show the empty state (covers both "no apps" and "no search match").
    private bool ShowEmpty => _store.Apps.IsSuccess && _store.FilteredApps.Count == 0;

    // Idle (before the first load) or a load in flight → show the spinner.
    private bool ShowLoading => _store.Apps.IsIdle || _store.Apps.IsLoading;

    // The empty-state text distinguishes "no apps at all" from "the filter matched nothing".
    private string EmptyMessage => string.IsNullOrWhiteSpace(_store.Filter) ? Resources.NoApps : Resources.NoSearchResults;

    public Ui Body => Overlay(
        ScrollView(
            ItemsRepeater(
                _store.FilteredApps,
                static itemStore => new AppCard(itemStore),
                static (card, itemStore) => { },
                new UniformGridLayout
                {
                    ItemWidth = 320f,
                    ItemHeight = 56f,
                    ColumnSpacing = 8f,
                    RowSpacing = 8f,
                    ItemsStretch = UniformGridItemsStretch.Fill,
                })
                .Margin(0f, 8f)
            )
            .Vertical(ScrollMode.Auto)
            .HorizontalScrollBar(ScrollBarVisibility.Hidden)
            .Padding(12f, 0f)
            .Visible(ShowList),
        Stack(
            ProgressRing(new Size(32f, 32f)).HAlign(HorizontalAlignment.Center),
            Text(Resources.Loading)
                .Foreground(Theme.Resolve().Colors.TextFillColorSecondary)
                .HAlign(HorizontalAlignment.Center))
            .Spacing(12f)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Visible(ShowLoading),
        Stack(
            Text(Resources.LoadFailed)
                .Foreground(Theme.Resolve().Colors.TextFillColorSecondary)
                .HAlign(HorizontalAlignment.Center),
            HyperlinkButton(Resources.Retry, HyperlinkButtonPalette.FromTheme(Theme.Resolve().Colors))
                .OnClick(OnRetry)
                .HAlign(HorizontalAlignment.Center))
            .Spacing(8f)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Visible(_store.Apps.IsError),
        Text(EmptyMessage)
            .Foreground(Theme.Resolve().Colors.TextFillColorSecondary)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Visible(ShowEmpty));

    private void OnRetry() => _ = _store.ReloadAsync();
}
