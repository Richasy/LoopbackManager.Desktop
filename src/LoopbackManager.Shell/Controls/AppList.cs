using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Styling;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

/// <summary>
/// Virtualizes app rows and overlays loading, failure, or empty states. Keep the scroller mounted while empty so
/// its native input host is established before the initial load completes.
/// </summary>
public sealed partial class AppList : Control
{
    private readonly AppStore _store;

    public AppList() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    // Success but no (filtered) rows → show the empty state (covers both "no apps" and "no search match").
    private bool ShowEmpty => _store.Apps.IsSuccess && _store.FilteredApps.Count == 0;

    // Idle (before the first load) or a load in flight → show the spinner.
    private bool ShowLoading => _store.Apps.IsIdle || _store.Apps.IsLoading;

    // The empty-state text distinguishes "no apps at all" from "the filter matched nothing".
    private string EmptyMessage => string.IsNullOrWhiteSpace(_store.Filter) ? Resources.NoApps : Resources.NoSearchResults;

    public Ui Body => Overlay([
        ScrollView(
            ItemsRepeater(
                _store.FilteredApps,
                static itemStore => itemStore.Sid,
                static itemStore => new AppCard(itemStore),
                static (card, itemStore) => card.SetItem(itemStore),
                new UniformGridLayout
                {
                    ItemWidth = 320f,
                    ItemHeight = 56f,
                    ColumnSpacing = 8f,
                    RowSpacing = 8f,
                    ItemsStretch = UniformGridItemsStretch.Fill,
                })
                .Margin(new Thickness(0f, 0f, 0f, 8f))
            ).Vertical(ScrollMode.Auto).HorizontalScrollBar(ScrollBarVisibility.Hidden).Padding(12f, 0f),
        Column([
            ProgressRing(new Size(32f, 32f)).HAlign(HorizontalAlignment.Center),
            Text(Resources.Loading).Foreground(Brush.Theme(ThemeColorToken.TextFillColorSecondary)).HAlign(HorizontalAlignment.Center)
        ]).Spacing(12f).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Visible(ShowLoading),
        Column([
            Text(Resources.LoadFailed).Style(TextStyles.BodyStrong).Align(TextAlignment.Center).HAlign(HorizontalAlignment.Center),
            Text(LoadFailureGuidance).Foreground(Brush.Theme(ThemeColorToken.TextFillColorSecondary)).Wrap().Align(TextAlignment.Center).Width(480f).HAlign(HorizontalAlignment.Center),
            Border(
                Text(LoadFailureDetails).Foreground(Brush.Theme(ThemeColorToken.TextFillColorSecondary)).Style(TextStyles.Caption).Wrap().Align(TextAlignment.Leading))
                .Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault))
                .BorderBrush(Brush.Theme(ThemeColorToken.CardStrokeColorDefault))
                .BorderThickness(1f)
                .CornerRadius(6f)
                .Padding(12f, 8f)
                .Width(480f)
                .HAlign(HorizontalAlignment.Center),
            HyperlinkButton(Resources.Retry).OnClick(OnRetry).Automation(new()
                {
                    Name = Resources.Retry,
                    AutomationId = "RetryLoadButton",
                }).HAlign(HorizontalAlignment.Center)
        ]).Spacing(8f).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Visible(_store.Apps.IsError),
        Text(EmptyMessage).Automation(new() { Name = EmptyMessage, AutomationId = "EmptyStateMessage" }).Foreground(Brush.Theme(ThemeColorToken.TextFillColorSecondary)).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Visible(ShowEmpty),
    ]);

    private string LoadFailureGuidance => _store.LoadFailure.Kind switch
    {
        AppLoadFailureKind.FirewallServiceUnavailable => Resources.LoadErrorFirewallServiceHelp,
        AppLoadFailureKind.AccessDenied => Resources.LoadErrorAccessDeniedHelp,
        AppLoadFailureKind.UnsupportedSystem => Resources.LoadErrorUnsupportedSystemHelp,
        AppLoadFailureKind.MissingSystemComponent => Resources.LoadErrorMissingComponentHelp,
        AppLoadFailureKind.InvalidSystemConfiguration => Resources.LoadErrorInvalidConfigurationHelp,
        AppLoadFailureKind.ResourceExhausted => Resources.LoadErrorResourceExhaustedHelp,
        _ => Resources.LoadErrorUnknownHelp,
    };

    private string LoadFailureDetails
        => $"{Resources.LoadErrorDetails}: {_store.LoadFailure.Details}";

    private void OnRetry() => _ = _store.ReloadAsync();
}
