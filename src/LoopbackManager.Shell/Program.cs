using LoopbackManager.Shell;
using Sprout;
using Sprout.Backend.D2D;

// The whole entry point is one expression: configure the app, name the Direct2D render backend, build, and run.
return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    .UseSingleInstance()
    .ConfigureServices(s => s.AddSingleton(
        _ => new AppStore(new LoopbackService(), new DispatcherQueueScheduler())))
    .Build()
    .Run();
