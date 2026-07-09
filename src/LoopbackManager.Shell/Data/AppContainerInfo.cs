namespace LoopbackManager.Shell;

/// <summary>
/// A UI-free snapshot of one AppContainer the loopback list shows: its identity, its working directory, its string
/// SID, and whether it currently has a loopback exemption. This is the clean domain shape the
/// <see cref="ILoopbackService"/> hands the store — the raw Win32 <c>IntPtr</c> SID is already converted to a string
/// and the (possibly <c>@</c>-resource) display name already resolved, so the store never touches interop.
/// </summary>
/// <param name="ContainerName">The AppContainer's internal name.</param>
/// <param name="DisplayName">The resolved, user-facing display name.</param>
/// <param name="WorkingDirectory">The app's working directory (empty when unknown).</param>
/// <param name="Sid">The AppContainer SID as a string (the key used to set exemptions).</param>
/// <param name="PackageFullName">The package full name.</param>
/// <param name="IsExempt">Whether this AppContainer currently has a loopback exemption.</param>
public sealed record AppContainerInfo(
    string ContainerName,
    string DisplayName,
    string WorkingDirectory,
    string Sid,
    string PackageFullName,
    bool IsExempt);
