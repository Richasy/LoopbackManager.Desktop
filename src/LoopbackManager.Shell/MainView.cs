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
public sealed partial class MainView(Window window) : Control
{
    private Ui Build()
    {
        return Grid(
            [GridLength.Star()],
            [GridLength.Auto, GridLength.Star()],
            new AppTitleBar().Cell(0, 0)
         );
    }
}
