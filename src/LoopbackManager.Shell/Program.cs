using LoopbackManager.Shell;
using Sprout;
using Sprout.Backend.D2D;

return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    .UseSingleInstance()
    .ConfigureServices(s => s.AddSingleton(
        _ => new AppStore(new LoopbackService(), new DispatcherQueueScheduler())))
    .Build()
    .Run();
