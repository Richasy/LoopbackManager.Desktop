using Sprout;
using Sprout.Controls;
using Sprout.Graphics;
using Sprout.Layout;
using Sprout.Reconcile;
using Sprout.Theming;
using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

public sealed partial class AppCard : Control
{
    private readonly AppItemStore _itemStore;

    public AppCard(AppItemStore itemStore) => _itemStore = itemStore;

    public Ui Body => Grid(
        [GridLength.Star(), GridLength.Auto],
        [GridLength.Auto],
        CheckBox(
            Stack(
                Text(_itemStore.DisplayName)
                .Trim(TextTrimming.CharacterEllipsis)
                .TooltipWhenTrimmed(),
                Text(_itemStore.PackageFullName)
                .FontSize(12)
                .Foreground(Theme.Resolve().Colors.TextFillColorSecondary)
                .Trim(TextTrimming.CharacterEllipsis)
                .TooltipWhenTrimmed()).Spacing(4),
            _itemStore.IsLoopback,
            _itemStore.Toggle,
            CheckBoxPalette.FromTheme(Theme.Resolve().Colors))
            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
            .Automation(new() { Name = _itemStore.DisplayName })
            .VAlign(VerticalAlignment.Center)
            .Cell(0, 0),
        Button(Icon.Fluent(FluentSymbol.Folder, size: 14), _itemStore.OpenFolder)
            .Enabled(_itemStore.CanOpenFolder)
            .VAlign(VerticalAlignment.Center)
            .HAlign(HorizontalAlignment.Right)
            .Cell(1, 0)
        ).Padding(12, 8).ColumnSpacing(12).Background(Brush.Theme(ThemeColorToken.CardBackgroundFillColorDefault)).CornerRadius(4f);
}
