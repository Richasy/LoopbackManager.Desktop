using LoopbackManager.Shell;
using Sprout;
using Sprout.Backend.D2D;

// The whole entry point is one expression: configure the app, name the Direct2D render backend, build, and run.
return SproutApp.CreateBuilder(args)
    .UseApp<App>()
    .UseD2DBackend()
    // A Sprout app is single-instance by default: a second launch (e.g. opening an associated file) is redirected to
    // this instance's App.OnActivated instead of starting a new process. This states it explicitly; pass a key to share
    // or separate instances, or call .AllowMultipleInstances() to opt out.
    .UseSingleInstance()
    .Build()
    .Run();
