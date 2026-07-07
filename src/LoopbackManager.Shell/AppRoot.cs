using System.Runtime.CompilerServices;
using Microsoft.Windows.AppLifecycle;

using Sprout;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Theming;

using static Sprout.Markup;

namespace LoopbackManager.Shell;

/// <summary>
/// The window content: a tiny reactive <see cref="Control"/> that also demonstrates reaching the Windows platform.
/// Its <c>Build()</c> reads the window's effective theme (so it re-renders on a light/dark switch) and two WinRT values
/// — an OS API and a Windows App SDK API — then renders a <see cref="WelcomeView"/>. Edit this to start building.
/// </summary>
/// <param name="window">The window whose effective theme this reports.</param>
public sealed partial class AppRoot : Control
{
    public partial string? SearchText { get; private set; }

    private readonly Action<string?> _searchTextAction;
    private readonly Action _saveAction;

    public AppRoot()
    {
        SearchText = string.Empty;
        _searchTextAction = t => SearchText = t;
        _saveAction = () => System.Diagnostics.Debug.WriteLine("Saved");
    }

    public Ui Body => Grid(
        [GridLength.Star(), GridLength.Auto],
        [GridLength.Auto, GridLength.Star()],
        TextBox()
            .Text(SearchText ?? string.Empty)
            .OnTextChanged(_searchTextAction)
            .HAlign(HorizontalAlignment.Stretch)
            .VAlign(VerticalAlignment.Center)
            .Cell(0, 0),
        Button(Resources.Save, _saveAction, ButtonPalette.FromTheme(Theme.Resolve().Colors, ButtonColorScheme.Accent))
            .VAlign(VerticalAlignment.Stretch)
            .MinWidth(120)
            .Cell(1, 0)
        )
        .Padding(12, 8)
        .ColumnSpacing(12);
}
