using Sprout.Reactive;

namespace LoopbackManager.Shell;

/// <summary>
/// The loopback list's data-response layer (the Sprout analogue of the old <c>MainPageViewModel</c>). It owns the app
/// list as an <see cref="AsyncValue{T}"/> loaded latest-wins, a text <see cref="Filter"/>, and the whole-set save; the
/// per-row toggle state lives in each <see cref="AppItemStore"/>. Every "can I…" flag is a reactive derived that reads
/// the rows' signals, so toggling a row or finishing a save recomputes it with no manual notification. The loopback
/// domain is injected as <see cref="ILoopbackService"/>, so the whole store is headless-testable with a fake.
/// </summary>
[Store]
public sealed partial class AppStore
{
    private readonly ILoopbackService _service;
    private readonly LatestOperation<IReadOnlyList<AppItemStore>> _load;
    private readonly DroppableOperation<bool> _save;
    private IReadOnlyList<SavedAppState> _saveSnapshot = [];

    /// <summary>The search keyword the list is filtered by — externally read-only, set via <see cref="SetFilter"/>.</summary>
    public partial string Filter { get; private set; }

    /// <summary>Whether the user dismissed the current save failure.</summary>
    public partial bool IsSaveErrorDismissed { get; private set; }

    /// <summary>Creates the store over an injected loopback service.</summary>
    /// <param name="service">The loopback domain surface (a real Win32 impl in the app, a fake in a test).</param>
    /// <param name="scheduler">The optional home-thread scheduler used to marshal async state transitions; omit for synchronous headless tests.</param>
    public AppStore(ILoopbackService service, IScheduler? scheduler = null)
    {
        _service = service;
        Filter = string.Empty;
        _load = scheduler is null
            ? new LatestOperation<IReadOnlyList<AppItemStore>>(LoadAppsAsync, Lifetime)
            : new LatestOperation<IReadOnlyList<AppItemStore>>(LoadAppsAsync, scheduler, Lifetime);
        _save = scheduler is null
            ? new DroppableOperation<bool>(SaveExemptionsAsync, Lifetime)
            : new DroppableOperation<bool>(SaveExemptionsAsync, scheduler, Lifetime);
    }

    /// <summary>The loaded app rows — Idle → Loading → Success(rows) / Error. Render with the four phases.</summary>
    public AsyncValue<IReadOnlyList<AppItemStore>> Apps => _load.State;

    /// <summary>The current load error classified for actionable UI guidance, or <see cref="AppLoadFailureKind.None"/>.</summary>
    public AppLoadFailure LoadFailure => AppLoadFailure.From(Apps.Error);

    /// <summary>The last save's result — Idle until saved, then Loading → Success(true) / Error.</summary>
    public AsyncValue<bool> SaveResult => _save.State;

    /// <summary>The rows matching the current <see cref="Filter"/> (by display name or package full name), sorted by display name (a stable order that does not jump when a row toggles).</summary>
    public IReadOnlyList<AppItemStore> FilteredApps
    {
        get
        {
            var all = Apps.Value ?? [];
            var filtered = string.IsNullOrWhiteSpace(Filter)
                ? all
                : all.Where(a => a.DisplayName.Contains(Filter, StringComparison.OrdinalIgnoreCase)
                    || a.PackageFullName.Contains(Filter, StringComparison.OrdinalIgnoreCase));
            return filtered
                .OrderByDescending(a => a.BaselineLoopback)
                .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    /// <summary>Whether a load succeeded but produced no apps.</summary>
    public bool IsEmpty => Apps.IsSuccess && (Apps.Value?.Count ?? 0) == 0;

    /// <summary>Whether the load failed.</summary>
    public bool IsFailed => Apps.IsError;

    /// <summary>Whether a load is in flight.</summary>
    public bool IsLoading => Apps.IsLoading;

    /// <summary>Whether a save is in flight.</summary>
    public bool IsSaving => SaveResult.IsLoading;

    /// <summary>Whether the dismissible save-failure banner should be open.</summary>
    public bool ShouldShowSaveError => SaveResult.IsError && !IsSaveErrorDismissed;

    /// <summary>Whether any row has a pending change (enables Save / Reset).</summary>
    public bool CanSave => Apps.Value?.Any(a => a.IsLoopbackChanged) ?? false;

    /// <summary>Whether any row is not yet exempt (enables Select-all).</summary>
    public bool CanSelectAll => Apps.Value?.Any(a => !a.IsLoopback) ?? false;

    /// <summary>
    /// Loads (or reloads) the app list; a new reload cancels a previous in-flight one (latest-wins). Reload is ignored
    /// while a save is applying its full-set replacement so the list cannot be replaced with a stale concurrent read.
    /// </summary>
    /// <returns>A task that completes when the load settles.</returns>
    public Task ReloadAsync() => _save.IsRunning ? Task.CompletedTask : _load.Run();

    /// <summary>Sets the search keyword (re-filters <see cref="FilteredApps"/> reactively).</summary>
    /// <param name="keyword">The new keyword (null treated as empty).</param>
    public void SetFilter(string keyword) => Filter = keyword ?? string.Empty;

    /// <summary>Marks every row as exempt (the "select all" action).</summary>
    public void SelectAll()
    {
        foreach (var app in Apps.Value ?? [])
        {
            app.Set(true);
        }
    }

    /// <summary>Reverts every row's pending toggle to its saved baseline (the "reset all" action).</summary>
    public void ResetAll()
    {
        foreach (var app in Apps.Value ?? [])
        {
            app.Reset();
        }
    }

    /// <summary>
    /// Saves the current exemption set; ignored if a save is already in flight (no double-submit). On success, each
    /// saved snapshot baseline is committed after the operation's terminal state reaches its home scheduler, so signal
    /// writes stay on the UI thread in the app (or inline on the test thread without a scheduler).
    /// </summary>
    /// <returns><see langword="true"/> only when this invocation saved successfully; <see langword="false"/> when rejected or failed.</returns>
    public async Task<bool> SaveAsync()
    {
        if (_save.IsRunning || _load.IsRunning)
        {
            return false;
        }

        var run = _save.Run();
        IsSaveErrorDismissed = false;
        await run;
        if (_save.State.IsSuccess)
        {
            foreach (var saved in _saveSnapshot)
            {
                saved.App.Commit(saved.IsLoopback);
            }

            return true;
        }

        return false;
    }

    /// <summary>Dismisses the current save-failure banner without clearing the pending changes.</summary>
    public void DismissSaveError()
    {
        if (SaveResult.IsError)
        {
            IsSaveErrorDismissed = true;
        }
    }

    // The latest-wins load work: fetch the snapshots and wrap each in a per-row store.
    private async Task<IReadOnlyList<AppItemStore>> LoadAppsAsync(CancellationToken cancellationToken)
    {
        var infos = await _service.GetAppsAsync(cancellationToken);
        return infos.Select(static info => new AppItemStore(info)).ToList();
    }

    // The droppable save work: push the whole exempt set (a full replace). Baseline commit happens in SaveAsync after
    // the run settles, not here, so the signal writes are not on a background continuation.
    private async Task<bool> SaveExemptionsAsync(CancellationToken cancellationToken)
    {
        _saveSnapshot = (Apps.Value ?? [])
            .Select(static app => new SavedAppState(app, app.IsLoopback))
            .ToList();
        IReadOnlyList<string> exemptSids = _saveSnapshot
            .Where(static saved => saved.IsLoopback)
            .Select(static saved => saved.App.Sid)
            .ToList();
        await _service.SetExemptionsAsync(exemptSids, cancellationToken);
        return true;
    }

    private readonly record struct SavedAppState(AppItemStore App, bool IsLoopback);
}
