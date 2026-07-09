namespace LoopbackManager.Shell;

/// <summary>
/// The UI-free loopback domain surface the <see cref="AppStore"/> depends on — enumerate the AppContainers with their
/// current exemption state, and set the whole exemption set. It is injected (a real Win32 implementation in the app, a
/// fake in a headless test), so the store is testable without touching interop or the OS.
/// </summary>
public interface ILoopbackService
{
    /// <summary>Enumerates every AppContainer on the system with its current loopback-exemption state.</summary>
    /// <param name="cancellationToken">Cancelled on a superseding reload or the store's disposal.</param>
    /// <returns>The AppContainers.</returns>
    Task<IReadOnlyList<AppContainerInfo>> GetAppsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the <b>entire</b> loopback-exemption set to exactly <paramref name="exemptSids"/>. The underlying Win32
    /// <c>NetworkIsolationSetAppContainerConfig</c> is a <b>full replace</b>, not an incremental add/remove — a SID
    /// absent from the list is left un-exempt.
    /// </summary>
    /// <param name="exemptSids">The string SIDs that should be exempt after the call.</param>
    /// <param name="cancellationToken">Cancelled on the store's disposal.</param>
    /// <returns>A task that completes when the set is applied.</returns>
    Task SetExemptionsAsync(IReadOnlyList<string> exemptSids, CancellationToken cancellationToken);
}
