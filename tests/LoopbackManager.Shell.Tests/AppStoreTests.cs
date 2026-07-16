using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoopbackManager.Shell.Tests;

[TestClass]
public sealed class AppStoreTests
{
    [TestMethod]
    public async Task LoadFailure_PreservesUnderlyingError_AndCanRetryAsync()
    {
        var expected = new InvalidOperationException("read failed");
        var service = new FakeLoopbackService { LoadException = expected };
        using var store = new AppStore(service);

        await store.ReloadAsync();

        Assert.IsTrue(store.Apps.IsError);
        Assert.AreSame(expected, store.Apps.Error);
        Assert.AreEqual(AppLoadFailureKind.Unknown, store.LoadFailure.Kind);
        StringAssert.Contains(store.LoadFailure.Details, "read failed");

        service.LoadException = null;
        await store.ReloadAsync();

        AssertSingleApp(store);
        Assert.AreEqual(AppLoadFailureKind.None, store.LoadFailure.Kind);
        Assert.AreEqual(2, service.LoadCalls);
    }

    [DataTestMethod]
    [DataRow(5, AppLoadFailureKind.AccessDenied)]
    [DataRow(87, AppLoadFailureKind.UnsupportedSystem)]
    [DataRow(126, AppLoadFailureKind.MissingSystemComponent)]
    [DataRow(1058, AppLoadFailureKind.FirewallServiceUnavailable)]
    [DataRow(1062, AppLoadFailureKind.FirewallServiceUnavailable)]
    [DataRow(1722, AppLoadFailureKind.FirewallServiceUnavailable)]
    [DataRow(1780, AppLoadFailureKind.InvalidSystemConfiguration)]
    public async Task LoadFailure_ClassifiesKnownWin32ErrorsAsync(
        int nativeError,
        AppLoadFailureKind expectedKind)
    {
        var service = new FakeLoopbackService
        {
            LoadException = new Win32Exception(nativeError),
        };
        using var store = new AppStore(service);

        await store.ReloadAsync();

        Assert.AreEqual(expectedKind, store.LoadFailure.Kind);
        StringAssert.Contains(store.LoadFailure.Details, $"0x{nativeError:X8}");
    }

    [TestMethod]
    public async Task LoadFailure_ClassifiesManagedFailuresAsync()
    {
        var cases = new (Exception Error, AppLoadFailureKind Kind)[]
        {
            (new UnauthorizedAccessException(), AppLoadFailureKind.AccessDenied),
            (new DllNotFoundException("FirewallAPI.dll"), AppLoadFailureKind.MissingSystemComponent),
            (new PlatformNotSupportedException(), AppLoadFailureKind.UnsupportedSystem),
            (new InvalidDataException(), AppLoadFailureKind.InvalidSystemConfiguration),
            (new OutOfMemoryException(), AppLoadFailureKind.ResourceExhausted),
        };
        var service = new FakeLoopbackService();
        using var store = new AppStore(service);

        foreach (var (error, kind) in cases)
        {
            service.LoadException = error;
            await store.ReloadAsync();
            Assert.AreEqual(kind, store.LoadFailure.Kind, error.GetType().Name);
        }
    }

    [TestMethod]
    public async Task PartialLoad_ExposesDiagnosticsWithoutHidingReadableAppsAsync()
    {
        var service = new FakeLoopbackService
        {
            UsedFallback = true,
            SkippedCount = 2,
            BatchFailureDetails = "NetworkIsolationEnumAppContainers failed (0x000006F4).",
        };
        using var store = new AppStore(service);

        await store.ReloadAsync();

        Assert.IsTrue(store.Apps.IsSuccess);
        AssertSingleApp(store);
        Assert.IsTrue(store.ShouldShowPartialLoadWarning);
        Assert.IsTrue(store.Apps.Value!.Diagnostics.UsedFallback);
        Assert.AreEqual(2, store.Apps.Value.Diagnostics.SkippedCount);
    }

    [TestMethod]
    public async Task PartialLoad_SavePreservesExemptionsWithoutVisibleRowsAsync()
    {
        var service = new FakeLoopbackService
        {
            PreservedExemptSids = ["S-1-15-2-hidden"],
        };
        using var store = new AppStore(service);
        await store.ReloadAsync();
        var app = AssertSingleApp(store);
        app.Set(true);

        Assert.IsTrue(await store.SaveAsync());

        CollectionAssert.AreEquivalent(
            new[] { app.Sid, "S-1-15-2-hidden" },
            service.LastSavedSids.ToArray());
    }

    [TestMethod]
    public async Task SaveFailure_KeepsChangesPending_AndCanRetryAsync()
    {
        var service = new FakeLoopbackService();
        using var store = new AppStore(service);
        await store.ReloadAsync();
        var app = AssertSingleApp(store);
        app.Set(true);

        service.SaveException = new InvalidOperationException("write failed");
        Assert.IsFalse(await store.SaveAsync());

        Assert.IsTrue(store.SaveResult.IsError);
        Assert.IsTrue(store.ShouldShowSaveError);
        Assert.IsTrue(store.CanSave);
        Assert.IsFalse(app.BaselineLoopback);

        store.DismissSaveError();
        Assert.IsFalse(store.ShouldShowSaveError);

        service.SaveException = null;
        Assert.IsTrue(await store.SaveAsync());

        Assert.IsTrue(store.SaveResult.IsSuccess);
        Assert.IsFalse(store.ShouldShowSaveError);
        Assert.IsFalse(store.CanSave);
        Assert.IsTrue(app.BaselineLoopback);
        CollectionAssert.AreEqual(new[] { app.Sid }, service.LastSavedSids.ToArray());
    }

    [TestMethod]
    public async Task ConcurrentSave_ReturnsFalseForTheRejectedInvocationAsync()
    {
        var service = new FakeLoopbackService { BlockSave = true };
        using var store = new AppStore(service);
        await store.ReloadAsync();
        AssertSingleApp(store).Set(true);

        var accepted = store.SaveAsync();
        await service.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(await store.SaveAsync(), "the invocation rejected by the in-flight save must not inherit an older success state.");

        service.ReleaseSave();
        Assert.IsTrue(await accepted);
    }

    [TestMethod]
    public async Task EditDuringSave_RemainsPendingAfterSnapshotCommitsAsync()
    {
        var service = new FakeLoopbackService { BlockSave = true };
        using var store = new AppStore(service);
        await store.ReloadAsync();
        var app = AssertSingleApp(store);
        app.Set(true);

        var save = store.SaveAsync();
        await service.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        app.Set(false);
        service.ReleaseSave();
        await save;

        Assert.IsTrue(store.SaveResult.IsSuccess);
        Assert.IsTrue(app.BaselineLoopback);
        Assert.IsFalse(app.IsLoopback);
        Assert.IsTrue(store.CanSave);
        CollectionAssert.AreEqual(new[] { app.Sid }, service.LastSavedSids.ToArray());
    }

    [TestMethod]
    public async Task ReloadDuringSave_DoesNotReplaceRowsWithAStaleReadAsync()
    {
        var service = new FakeLoopbackService { BlockSave = true };
        using var store = new AppStore(service);
        await store.ReloadAsync();
        var original = AssertSingleApp(store);
        original.Set(true);

        var save = store.SaveAsync();
        await service.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await store.ReloadAsync();

        Assert.AreSame(original, AssertSingleApp(store));
        Assert.AreEqual(1, service.LoadCalls);

        service.ReleaseSave();
        await save;
    }

    [TestMethod]
    public async Task FooterCommands_SelectAllResetAndRefreshLoadedRowsAsync()
    {
        var service = new FakeLoopbackService
        {
            AppSnapshots =
            [
                new("first", "First app", @"C:\First", "S-1-15-2-1", "First_1.0.0.0_x64__test", false),
                new("second", "Second app", @"C:\Second", "S-1-15-2-2", "Second_1.0.0.0_x64__test", true),
            ],
        };
        using var store = new AppStore(service);
        await store.ReloadAsync();

        store.SelectAll();

        Assert.IsTrue(store.Apps.Value!.Items[0].IsLoopback);
        Assert.IsTrue(store.Apps.Value.Items[1].IsLoopback);
        Assert.IsFalse(store.CanSelectAll);
        Assert.IsTrue(store.CanSave);

        store.ResetAll();

        Assert.IsFalse(store.Apps.Value.Items[0].IsLoopback);
        Assert.IsTrue(store.Apps.Value.Items[1].IsLoopback);
        Assert.IsTrue(store.CanSelectAll);
        Assert.IsFalse(store.CanSave);

        var previousRows = store.Apps.Value.Items;
        await store.ReloadAsync();

        Assert.AreEqual(2, service.LoadCalls);
        Assert.AreNotSame(previousRows, store.Apps.Value.Items);
    }

    private static AppItemStore AssertSingleApp(AppStore store)
    {
        Assert.IsTrue(store.Apps.IsSuccess);
        Assert.AreEqual(1, store.Apps.Value!.Items.Count);
        return store.Apps.Value.Items[0];
    }

    private sealed class FakeLoopbackService : ILoopbackService
    {
        private readonly TaskCompletionSource _releaseSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int LoadCalls { get; private set; }

        public bool BlockSave { get; init; }

        public Exception? LoadException { get; set; }

        public Exception? SaveException { get; set; }

        public bool UsedFallback { get; init; }

        public int SkippedCount { get; init; }

        public string? BatchFailureDetails { get; init; }

        public IReadOnlyList<AppContainerInfo> AppSnapshots { get; init; } =
        [
            new("test", "Test app", @"C:\Test", "S-1-15-2-1", "Test_1.0.0.0_x64__test", false),
        ];

        public IReadOnlyList<string> LastSavedSids { get; private set; } = [];

        public IReadOnlyList<string> PreservedExemptSids { get; init; } = [];

        public TaskCompletionSource SaveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<AppEnumerationResult> GetAppsAsync(CancellationToken cancellationToken)
        {
            LoadCalls++;
            if (LoadException is not null)
            {
                return Task.FromException<AppEnumerationResult>(LoadException);
            }

            return Task.FromResult(new AppEnumerationResult(
                AppSnapshots,
                PreservedExemptSids,
                new AppEnumerationDiagnostics(UsedFallback, SkippedCount, BatchFailureDetails)));
        }

        public async Task SetExemptionsAsync(
            IReadOnlyList<string> exemptSids,
            CancellationToken cancellationToken)
        {
            LastSavedSids = exemptSids.ToArray();
            SaveStarted.TrySetResult();
            if (BlockSave)
            {
                await _releaseSave.Task.WaitAsync(cancellationToken);
            }
            if (SaveException is not null)
            {
                throw SaveException;
            }
        }

        public void ReleaseSave() => _releaseSave.TrySetResult();
    }
}
