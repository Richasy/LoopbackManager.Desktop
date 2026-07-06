using Sprout;
using Sprout.Graphics;
using Sprout.Reconcile;
using static Sprout.Markup;

namespace LoopbackManager.Shell.Controls;

public sealed partial class AppTitleBar : Control
{
    private AppTitleBarView Build() => new AppTitleBarView();
}

[Composite]
public readonly partial record struct AppTitleBarView()
{
    public Ui Body => Row(
        Image(ImageSource.FromUri("Assets/logo.ico"))
            .Layout(width: 16, height: 16, align: Sprout.Layout.CrossAlign.Center, verticalAlignment: Sprout.Layout.Alignment.Center),
        Text(Resources.AppName)
        )
        .Spacing(8)
        .Padding(4, 0, 0, 0);
}
