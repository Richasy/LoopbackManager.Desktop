using Sprout;
using Sprout.Graphics;
using Sprout.Reconcile;
using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

[Composite]
public readonly partial record struct AppTitleBarView
{
    internal static readonly ImageHandle Logo = new(ImageSource.FromFile(
        System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico")));

    public Ui Body => Stack(
        Image(Logo)
            .Width(16)
            .Height(16)
            .VAlign(Sprout.Layout.VerticalAlignment.Center)
            .HAlign(Sprout.Layout.HorizontalAlignment.Left),
        Text(Resources.AppName)
            .FontSize(12)
            .VAlign(Sprout.Layout.VerticalAlignment.Center)
            .TooltipWhenTrimmed()
        )
        .Orientation(Sprout.Widgets.Orientation.Horizontal)
        .Spacing(12)
        .Padding(12, 0, 0, 0);
}
