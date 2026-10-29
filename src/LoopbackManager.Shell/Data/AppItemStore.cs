using System.IO;
using System.Runtime.InteropServices;
using Sprout.Reactive;
using Windows.System;

namespace LoopbackManager.Shell;

/// <summary>
/// An app's immutable identity, pending exemption, and last-saved baseline. Pending edits survive the completion
/// of an older save; only the value actually sent to Windows becomes the new baseline.
/// </summary>
[Store]
public sealed partial class AppItemStore
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

    /// <summary>
    /// Marks the value captured by a completed save as the new baseline. A newer toggle made while that save was in
    /// flight remains pending instead of being incorrectly marked as persisted.
    /// </summary>
    /// <param name="savedValue">The value included in the completed save.</param>
    public void Commit(bool savedValue) => BaselineLoopback = savedValue;

    /// <summary>Whether this row has a working directory that can be opened.</summary>
    public bool CanOpenFolder => !string.IsNullOrEmpty(WorkingDirectory);

    /// <summary>
    /// Opens the app's working directory in the file explorer. A no-op when the directory is unknown or no longer on
    /// disk; a platform launch failure (blocked by policy, etc.) is swallowed so it can never crash the UI.
    /// </summary>
    public void OpenFolder()
    {
        if (string.IsNullOrEmpty(WorkingDirectory) || !Directory.Exists(WorkingDirectory))
        {
            return;
        }

        _ = LaunchFolderAsync(WorkingDirectory);
    }

    private static async Task LaunchFolderAsync(string path)
    {
        try
        {
            _ = await Launcher.LaunchFolderPathAsync(path).AsTask();
        }
        catch (COMException)
        {
        }
    }
}
