using LoopbackManager.Shell.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sprout.Semantics;

namespace LoopbackManager.Shell.Tests;

[TestClass]
public sealed class AppCardTests
{
    [TestMethod]
    public void Rebind_UpdatesAccessibleStateAndActions_WithoutReplacingTheRow()
    {
        using var original = CreateItem("Original");
        using var replacement = CreateItem("Refreshed");
        using var card = new AppCard(original);
        card.Mount();
        var render = card.Render;
        var interaction = card.Interaction;
        var toggle = FindToggle(card.Semantics!);
        Assert.IsNotNull(toggle);

        toggle.Toggled!();
        Assert.IsTrue(original.IsLoopback);
        Assert.AreEqual(true, toggle.IsChecked);

        card.SetItem(replacement);

        Assert.AreSame(render, card.Render);
        Assert.AreSame(interaction, card.Interaction);
        Assert.AreSame(toggle, FindToggle(card.Semantics!));
        Assert.AreEqual("Refreshed", toggle.Name);
        Assert.AreEqual(false, toggle.IsChecked);
        toggle.Toggled!();
        Assert.IsTrue(replacement.IsLoopback, "the retained accessible action must target the refreshed store.");
        Assert.IsTrue(original.IsLoopback, "the old store must no longer receive row actions.");

        original.Set(false);
        Assert.AreEqual(true, toggle.IsChecked, "changes to the old store must not redraw the recycled row.");
    }

    private static AppItemStore CreateItem(string name)
        => new(new("container", name, string.Empty, "S-1-15-2-1", "TestPackage", false));

    private static SemanticsNode? FindToggle(SemanticsNode root)
        => root.Toggled is not null ? root : root.Children.Select(FindToggle).FirstOrDefault(node => node is not null);
}
