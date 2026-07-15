using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Pacing;
using Sprout.Reactive;
using Sprout.Reconcile;
using Sprout.Styling;
using Sprout.Theming;
using Sprout.Widgets;

using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

/// <summary>
/// The top bar — a search box bound to the shared <see cref="AppStore"/>'s filter, plus the Save button (enabled only
/// when a row has a pending change). Like <see cref="AppFooter"/>, it reads the one shared store <b>ambiently</b>
/// (<c>Application.Current.Services</c>), so typing re-filters the list and toggling a row enables Save — both without
/// any cross-control wiring, because every section reads the same store's signals.
/// </summary>
public sealed partial class AppHeader : Control
{
    private readonly AppStore _store;
    private readonly Signal<bool> _saveRequested = new(false);
    private readonly Signal<bool> _justSaved = new(false);
    private Debouncer? _resetSaved;

    public AppHeader() => _store = Application.Current.Services.GetRequiredService<AppStore>();

    private string SaveLabel => _saveRequested.Value || _store.IsSaving
        ? Resources.Saving
        : _justSaved.Value ? Resources.Saved : Resources.Save;

    public Ui Body => Grid(
        [GridLength.Star(), GridLength.Auto],
        [GridLength.Auto],
        TextBox()
            .Text(_store.Filter)
            .Placeholder(Resources.SearchPlaceholder)
            .OnTextChanged(t => _store.SetFilter(t ?? string.Empty))
            .HAlign(HorizontalAlignment.Stretch)
            .VAlign(VerticalAlignment.Center)
            .Cell(0, 0),
        Button(
            Stack(
                Icon.Fluent(FluentSymbol.Checkmark, size: 14).Visible(_justSaved.Value),
                AnimatedText(SaveLabel))
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center),
            OnSave,
            ButtonPalette.FromTheme(Theme.Resolve().Colors, ButtonColorScheme.Accent))
            .Enabled(_store.CanSave && !_store.IsLoading && !_store.IsSaving && !_saveRequested.Value && !_justSaved.Value)
            .Automation(new()
            {
                Name = SaveLabel,
                AutomationId = "SaveButton",
            })
            .VAlign(VerticalAlignment.Stretch)
            .MinWidth(120)
            .Cell(1, 0))
        .ColumnSpacing(12)
        .Padding(12, 8);

    // Saves the pending exemptions, then briefly flashes the button to a confirmed state. A failure remains pending and
    // is surfaced by the dismissible InfoBar in AppRoot.
    private void OnSave() => _ = SaveAndConfirmAsync();

    protected override IDisposable? OnMounted()
    {
        _resetSaved = new Debouncer(
            () => _justSaved.Value = false,
            TimeSpan.FromSeconds(1.5));
        return _resetSaved;
    }

    private async Task SaveAndConfirmAsync()
    {
        if (_saveRequested.Value)
        {
            return;
        }

        _saveRequested.Value = true;
        try
        {
            if (await _store.SaveAsync())
            {
                _justSaved.Value = true;
                _resetSaved?.Invoke();
            }
        }
        finally
        {
            _saveRequested.Value = false;
        }
    }
}
