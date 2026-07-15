using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoopbackManager.Shell.Tests;

[TestClass]
public sealed class AppStoreTests
{
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

        Assert.IsTrue(store.Apps.Value![0].IsLoopback);
        Assert.IsTrue(store.Apps.Value[1].IsLoopback);
        Assert.IsFalse(store.CanSelectAll);
        Assert.IsTrue(store.CanSave);

        store.ResetAll();

        Assert.IsFalse(store.Apps.Value[0].IsLoopback);
        Assert.IsTrue(store.Apps.Value[1].IsLoopback);
        Assert.IsTrue(store.CanSelectAll);
        Assert.IsFalse(store.CanSave);

        var previousRows = store.Apps.Value;
        await store.ReloadAsync();

        Assert.AreEqual(2, service.LoadCalls);
        Assert.AreNotSame(previousRows, store.Apps.Value);
    }

    private static AppItemStore AssertSingleApp(AppStore store)
    {
        Assert.IsTrue(store.Apps.IsSuccess);
        Assert.AreEqual(1, store.Apps.Value!.Count);
        return store.Apps.Value[0];
    }

    private sealed class FakeLoopbackService : ILoopbackService
    {
        private readonly TaskCompletionSource _releaseSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int LoadCalls { get; private set; }

        public bool BlockSave { get; init; }

        public Exception? SaveException { get; set; }

        public IReadOnlyList<AppContainerInfo> AppSnapshots { get; init; } =
        [
            new("test", "Test app", @"C:\Test", "S-1-15-2-1", "Test_1.0.0.0_x64__test", false),
        ];

        public IReadOnlyList<string> LastSavedSids { get; private set; } = [];

        public TaskCompletionSource SaveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<AppContainerInfo>> GetAppsAsync(CancellationToken cancellationToken)
        {
            LoadCalls++;
            return Task.FromResult(AppSnapshots);
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
