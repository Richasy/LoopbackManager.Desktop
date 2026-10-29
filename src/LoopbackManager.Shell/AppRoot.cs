using LoopbackManager.Shell.Controls;

using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;

using static Sprout.Markup;

namespace LoopbackManager.Shell;

/// <summary>
/// Composes the application sections and presents load/save diagnostics from the shared store.
/// </summary>
public sealed partial class AppRoot : Control
{
    private readonly AppStore _store;

    public AppRoot() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    public Ui Body => Grid(
        [GridLength.Star()],
        [GridLength.Auto, GridLength.Auto, GridLength.Star(), GridLength.Auto],
        [
            AppHeader()
                .Cell(0, 0),
            Column([
                InfoBar(Resources.SaveFailedTitle, Resources.SaveFailedMessage).Severity(InfoBarSeverity.Error).Closable(true).Open(_store.ShouldShowSaveError).OnClosed(_ => _store.DismissSaveError()),
                InfoBar(Resources.PartialLoadTitle, PartialLoadMessage).Severity(InfoBarSeverity.Warning).Closable(false).MessageMaxLines(4).Open(_store.ShouldShowPartialLoadWarning)
            ]).Spacing(8f).Margin(new Thickness(12f, 0f, 12f, 8f)).Visible(_store.ShouldShowSaveError || _store.ShouldShowPartialLoadWarning).Cell(0, 1),
            AppList()
                .Cell(0, 2),
            AppFooter()
                .Cell(0, 3),
        ]);

    private string PartialLoadMessage
    {
        get
        {
            var diagnostics = _store.Apps.Value?.Diagnostics;
            if (diagnostics is null)
            {
                // InfoBar fixes title/message widget presence at mount; keep a non-empty closed-state placeholder.
                return Resources.PartialLoadFallbackMessage;
            }

            if (diagnostics.UsedFallback)
            {
                return diagnostics.SkippedCount > 0
                    ? string.Format(Resources.PartialLoadFallbackSkippedMessage, diagnostics.SkippedCount)
                    : Resources.PartialLoadFallbackMessage;
            }

            return string.Format(Resources.PartialLoadSkippedMessage, diagnostics.SkippedCount);
        }
    }
}
