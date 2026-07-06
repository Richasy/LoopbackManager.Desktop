# LoopbackManager.Shell

A [Sprout](https://github.com/Richasy/Sprout) application that also targets the **Windows platform** through WinRT and
the **Windows App SDK** (the Foundation feature area — **no WinUI**). Sprout still draws the entire UI itself; the
Windows App SDK is there for platform capabilities (app lifecycle, resources, notifications, power, …).

## Why this template

- A `-windows` target framework turns on the WinRT projection, so you can call Windows platform APIs directly.
- `Microsoft.WindowsAppSDK.Foundation` adds the Windows App SDK's non-UI APIs — **without** pulling in WinUI XAML.
- **Self-contained + unpackaged**: the Windows App SDK runtime is bundled next to the `.exe`, so there is no runtime to
  install, no bootstrapper, and no MSIX requirement. It is still "just run the `.exe`" (Sprout's model).
- **NativeAOT**: `dotnet publish` produces one self-contained native executable.

## Run

```pwsh
dotnet run
```

The window uses a Mica backdrop and follows the system theme. Press **Ctrl+T** to cycle System → Light → Dark. The card
shows two live WinRT values (OS memory usage + the Windows App SDK activation kind) to prove platform access.

## Publish a self-contained native executable (NativeAOT)

```pwsh
dotnet publish -c Release -r win-x64
```

One self-contained `.exe` (with the bundled Windows App SDK runtime) under `bin/Release/.../win-x64/publish/`.

## MSIX (opt-in)

Generate with `--packaging msix` (or add the `Sprout.Packaging` reference + `<SproutPackageFormat>Msix</SproutPackageFormat>`)
to also produce a signed MSIX on publish — the **same** Sprout packer is used; no Windows App SDK MSIX tooling.
Packaging is **publish-time only**: `dotnet build` / `dotnet run` never package (development stays unpackaged); the
`.msix` is produced only by `dotnet publish -c Release -r win-x64`.

## App icon and MSIX assets

`app.ico` is the application icon — embedded into the `.exe` (`<ApplicationIcon>`) for Explorer / the taskbar, and shown
as the **window** icon automatically (no code). Replace it to re-brand. With `--packaging msix` you also have
`Package.appxmanifest` (your editable MSIX identity + tiles + splash) and `Assets/` (a WinUI-aligned minimal icon set —
7 logo bases + the unplated taskbar icon); leave the
manifest's `$...$` placeholders (filled at publish, kept consistent with the dev signing certificate). The MSIX identity
is readable by default (the project name); scaffold with `--identity-style guid` for a GUID identity + a `CN=<your user>`
publisher (the Visual Studio convention).

## Where to start

- `Program.cs` — the entry point.
- `App.cs` — opens the window and handles theme switching.
- `MainView.cs` — the window content (reads two WinRT values). **Edit `Build()` to start building your UI**, and call
  any `Windows.*` or `Microsoft.Windows.*` API you need.
