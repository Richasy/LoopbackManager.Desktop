using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
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
    private readonly Signal<bool> _justSaved = new(false);

    public AppHeader() => _store = Application.Current.Services.GetRequiredService<AppStore>();

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
                AnimatedText(_justSaved.Value ? Resources.Saved : Resources.Save))
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center),
            OnSave,
            ButtonPalette.FromTheme(Theme.Resolve().Colors, ButtonColorScheme.Accent))
            .Enabled(_store.CanSave && !_justSaved.Value)
            .VAlign(VerticalAlignment.Stretch)
            .MinWidth(120)
            .Cell(1, 0))
        .ColumnSpacing(12)
        .Padding(12, 8);

    // Saves the pending exemptions, then flashes the button to a confirmed state — a check icon appears and the label
    // rolls "保存" → "已保存" — for 3 seconds before rolling back. The button is disabled during the confirmation.
    private void OnSave() => _ = SaveAndConfirmAsync();

    private async Task SaveAndConfirmAsync()
    {
        await _store.SaveAsync();
        if (_store.SaveResult.IsSuccess)
        {
            _justSaved.Value = true;
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            _justSaved.Value = false;
        }
    }
}
