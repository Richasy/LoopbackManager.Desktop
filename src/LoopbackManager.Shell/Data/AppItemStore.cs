using Sprout.Reactive;

namespace LoopbackManager.Shell;

/// <summary>
/// One app row's state — a small per-item store (the Sprout analogue of the old <c>ProgramItemViewModel</c>). It holds
/// the immutable identity/display data plus mutable state: <see cref="IsLoopback"/> (the user's pending exemption
/// toggle), diffed against <see cref="BaselineLoopback"/> (the last-saved / system state). Because <b>both</b> are
/// signal-backed, <see cref="IsLoopbackChanged"/> is reactive — so the parent <see cref="AppStore"/>'s
/// <see cref="AppStore.CanSave"/> recomputes automatically when a row toggles or commits, with no manual change
/// notification (which the old view-model needed via <c>OnIsLoopbackChanged → CheckStatus</c>).
/// </summary>
[Store]
internal sealed partial class AppItemStore
{
    /// <summary>The AppContainer's internal name.</summary>
    public string ContainerName { get; }

    /// <summary>The resolved, user-facing display name.</summary>
    public string DisplayName { get; }

    /// <summary>The app's working directory (empty when unknown).</summary>
    public string WorkingDirectory { get; }

    /// <summary>The AppContainer SID as a string (the key used to set exemptions).</summary>
    public string Sid { get; }

    /// <summary>The package full name.</summary>
    public string PackageFullName { get; }

    /// <summary>The user's pending loopback-exemption toggle — externally read-only, changed only via the methods.</summary>
    public partial bool IsLoopback { get; private set; }

    /// <summary>The last-saved (or initially loaded) exemption state — the baseline <see cref="IsLoopbackChanged"/> diffs against.</summary>
    public partial bool BaselineLoopback { get; private set; }

    /// <summary>Creates a row from a service snapshot; the pending toggle starts at the current system state.</summary>
    /// <param name="info">The AppContainer snapshot.</param>
    public AppItemStore(AppContainerInfo info)
    {
        ContainerName = info.ContainerName;
        DisplayName = info.DisplayName;
        WorkingDirectory = info.WorkingDirectory;
        Sid = info.Sid;
        PackageFullName = info.PackageFullName;
        IsLoopback = info.IsExempt;
        BaselineLoopback = info.IsExempt;
    }

    /// <summary>Whether the pending toggle differs from the saved baseline (a row that needs saving).</summary>
    public bool IsLoopbackChanged => IsLoopback != BaselineLoopback;

    /// <summary>Flips the pending exemption toggle.</summary>
    public void Toggle() => IsLoopback = !IsLoopback;

    /// <summary>Sets the pending exemption toggle.</summary>
    /// <param name="value">The new toggle value.</param>
    public void Set(bool value) => IsLoopback = value;

    /// <summary>Reverts the pending toggle to the saved baseline.</summary>
    public void Reset() => IsLoopback = BaselineLoopback;

    /// <summary>Marks the current toggle as saved — the baseline becomes the current value, so the row is no longer "changed".</summary>
    public void Commit() => BaselineLoopback = IsLoopback;
}
