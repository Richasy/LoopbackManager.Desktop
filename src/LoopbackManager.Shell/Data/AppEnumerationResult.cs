namespace LoopbackManager.Shell;

/// <summary>Non-fatal diagnostics produced while enumerating AppContainers.</summary>
/// <param name="UsedFallback">Whether the FirewallAPI batch failed and per-package enumeration was used.</param>
/// <param name="SkippedCount">The number of malformed entries or packages skipped individually.</param>
/// <param name="BatchFailureDetails">The original batch failure when fallback enumeration was used.</param>
public sealed record AppEnumerationDiagnostics(
    bool UsedFallback,
    int SkippedCount,
    string? BatchFailureDetails)
{
    /// <summary>No fallback and no skipped entries.</summary>
    public static AppEnumerationDiagnostics None { get; } = new(false, 0, null);

    /// <summary>Whether the UI should disclose that the result is best-effort.</summary>
    public bool IsPartial => UsedFallback || SkippedCount > 0;
}

/// <summary>The service-level app inventory plus state that must survive a full-set save.</summary>
/// <param name="Apps">The AppContainers safe to display and edit.</param>
/// <param name="PreservedExemptSids">Exempt SIDs not represented by a visible row; saves must retain them.</param>
/// <param name="Diagnostics">Non-fatal enumeration diagnostics.</param>
public sealed record AppEnumerationResult(
    IReadOnlyList<AppContainerInfo> Apps,
    IReadOnlyList<string> PreservedExemptSids,
    AppEnumerationDiagnostics Diagnostics)
{
    /// <summary>Creates a complete result with no hidden exemptions or diagnostics.</summary>
    /// <param name="apps">The complete app inventory.</param>
    public AppEnumerationResult(IReadOnlyList<AppContainerInfo> apps)
        : this(apps, [], AppEnumerationDiagnostics.None)
    {
    }
}

/// <summary>The store-owned rows and their enumeration/save-safety metadata.</summary>
/// <param name="Items">Reactive per-app rows.</param>
/// <param name="PreservedExemptSids">Exempt SIDs that have no editable row.</param>
/// <param name="Diagnostics">Non-fatal enumeration diagnostics.</param>
public sealed record AppLoadSnapshot(
    IReadOnlyList<AppItemStore> Items,
    IReadOnlyList<string> PreservedExemptSids,
    AppEnumerationDiagnostics Diagnostics);
