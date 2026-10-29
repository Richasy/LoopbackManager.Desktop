using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Pacing;
using Sprout.Reconcile;
using Sprout.Styling;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

/// <summary>
/// Search and save commands, with a short confirmation after a successful save.
/// </summary>
public sealed partial class AppHeader : Control
{
    private readonly AppStore _store;
    private partial bool SaveRequested { get; set; }
    private partial bool JustSaved { get; set; }
    private Debouncer? _resetSaved;

    public AppHeader() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    private string SaveLabel => SaveRequested || _store.IsSaving
        ? Resources.Saving
        : JustSaved ? Resources.Saved : Resources.Save;

    public Ui Body => Grid(
        [GridLength.Star(), GridLength.Auto],
        [GridLength.Auto],
        [
            TextBox(_store.Filter, _store.SetFilter)
                .Placeholder(Resources.SearchPlaceholder)
                .Automation(new() { Name = Resources.SearchPlaceholder, AutomationId = "AppSearchBox" }).HAlign(HorizontalAlignment.Stretch).VAlign(VerticalAlignment.Center).Cell(0, 0),
            Button(
                Row([
                    Icon.Fluent(FluentSymbol.Checkmark, size: 14).Visible(JustSaved),
                    AnimatedText(SaveLabel)
                ]).Spacing(6).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center),
                OnSave,
                ButtonColorScheme.Accent).Enabled(_store.CanSave && !_store.IsLoading && !_store.IsSaving && !SaveRequested && !JustSaved).Automation(new()
            {
                Name = SaveLabel,
                AutomationId = "SaveButton",
            }).VAlign(VerticalAlignment.Stretch).MinWidth(120).Cell(1, 0),
        ]
    ).ColumnSpacing(12).Padding(12, 8);

    // Saves the pending exemptions, then briefly flashes the button to a confirmed state. A failure remains pending and
    // is surfaced by the dismissible InfoBar in AppRoot.
    private void OnSave() => _ = SaveAndConfirmAsync();

    protected override IDisposable? OnMounted()
    {
        _resetSaved = new Debouncer(
            () => JustSaved = false,
            TimeSpan.FromSeconds(1.5));
        return _resetSaved;
    }

    private async Task SaveAndConfirmAsync()
    {
        if (SaveRequested)
        {
            return;
        }

        SaveRequested = true;
        try
        {
            if (await _store.SaveAsync())
            {
                JustSaved = true;
                _resetSaved?.Invoke();
            }
        }
        finally
        {
            SaveRequested = false;
        }
    }
}
