using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace LoopbackManager.Shell;

/// <summary>
/// The real Win32 implementation of <see cref="ILoopbackService"/> — the FirewallAPI <c>NetworkIsolation*</c> family
/// that enumerates AppContainers and reads/writes their loopback-exemption set, ported from the app's original
/// <c>LoopbackToolkit</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>AOT-clean interop.</b> The app publishes with NativeAOT, so this avoids the fragile classic-marshalling shapes:
/// every P/Invoke is a source-generated <see cref="LibraryImportAttribute"/> (compile-time stubs, no runtime IL
/// marshaller), the native structs are <b>blittable</b> (each <c>LPWStr</c> field is an <see cref="IntPtr"/> read
/// on demand with <see cref="Marshal.PtrToStringUni(IntPtr)"/>, so <see cref="Marshal.PtrToStructure{T}(IntPtr)"/> has
/// no string/marshaller dependency), and the two SID-conversion APIs return a <c>LocalAlloc</c>'d pointer (freed with
/// <c>LocalFree</c>) rather than an <c>out string</c>.
/// </para>
/// <para>
/// <b>Blocking APIs off the UI thread.</b> The FirewallAPI calls are synchronous, so each public method wraps its work
/// in <see cref="Task.Run(Action)"/>; the store's <c>LatestOperation</c>/<c>DroppableOperation</c> await that.
/// </para>
/// </remarks>
internal sealed partial class LoopbackService : ILoopbackService
{
    private const uint NetisoFlagsNone = 0;
    private const uint NetisoFlagForceComputeBinaries = 0x1;
    private const uint ErrorSuccess = 0;
    private const uint RpcXNullRefPointer = 1780;

    /// <inheritdoc/>
    public Task<AppEnumerationResult> GetAppsAsync(CancellationToken cancellationToken)
        => Task.Run(() => GetApps(cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task SetExemptionsAsync(IReadOnlyList<string> exemptSids, CancellationToken cancellationToken)
        => Task.Run(() => SetExemptions(exemptSids), cancellationToken);

    private static AppEnumerationResult GetApps(CancellationToken cancellationToken)
    {
        var exempt = GetExemptSids();
        if (TryGetAppsFromFirewall(exempt, cancellationToken, out var result, out var batchFailure))
        {
            return result;
        }

        return GetAppsFromPackages(exempt, cancellationToken, batchFailure);
    }

    private static bool TryGetAppsFromFirewall(
        HashSet<string> exempt,
        CancellationToken cancellationToken,
        out AppEnumerationResult result,
        out Win32Exception batchFailure)
    {
        uint count = 0;
        var appsPtr = IntPtr.Zero;
        var enumResult = NetworkIsolationEnumAppContainers(NetisoFlagsNone, ref count, ref appsPtr);
        if (enumResult == RpcXNullRefPointer && appsPtr == IntPtr.Zero)
        {
            // Retry once with freshly computed binary metadata, as Microsoft's WFPSampler does. This is best-effort;
            // per-package enumeration below remains the recovery path if the batch RPC still fails.
            count = 0;
            enumResult = NetworkIsolationEnumAppContainers(
                NetisoFlagForceComputeBinaries,
                ref count,
                ref appsPtr);
        }

        if (enumResult != ErrorSuccess)
        {
            if (appsPtr != IntPtr.Zero)
            {
                NetworkIsolationFreeAppContainers(appsPtr);
            }

            result = null!;
            batchFailure = CreateWin32Exception(enumResult, nameof(NetworkIsolationEnumAppContainers));
            return false;
        }

        if (appsPtr == IntPtr.Zero || count == 0)
        {
            result = CreateEnumerationResult([], exempt, AppEnumerationDiagnostics.None);
            batchFailure = null!;
            return true;
        }

        var apps = new List<AppContainerInfo>((int)count);
        var visibleSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedCount = 0;
        try
        {
            var stride = Marshal.SizeOf<INET_FIREWALL_APP_CONTAINER>();
            var cursor = appsPtr;
            for (uint i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entryPtr = cursor;
                cursor = IntPtr.Add(cursor, stride);

                try
                {
                    var app = Marshal.PtrToStructure<INET_FIREWALL_APP_CONTAINER>(entryPtr);
                    if (TryCreateFirewallApp(app, exempt, out var info) && visibleSids.Add(info.Sid))
                    {
                        apps.Add(info);
                    }
                }
                catch (Win32Exception)
                {
                    skippedCount++;
                }
                catch (InvalidDataException)
                {
                    skippedCount++;
                }
                catch (ArgumentException)
                {
                    skippedCount++;
                }
            }
        }
        finally
        {
            NetworkIsolationFreeAppContainers(appsPtr);
        }

        result = CreateEnumerationResult(
            apps,
            exempt,
            new AppEnumerationDiagnostics(false, skippedCount, null));
        batchFailure = null!;
        return true;
    }

    private static bool TryCreateFirewallApp(
        INET_FIREWALL_APP_CONTAINER app,
        HashSet<string> exempt,
        out AppContainerInfo info)
    {
        var workingDirectory = Marshal.PtrToStringUni(app.workingDirectory) ?? string.Empty;
        if (app.appContainerSid == IntPtr.Zero || string.IsNullOrEmpty(workingDirectory))
        {
            info = null!;
            return false;
        }

        var sid = SidToString(app.appContainerSid, "an enumerated AppContainer");
        var packageFullName = Marshal.PtrToStringUni(app.packageFullName) ?? string.Empty;
        var displayName = ResolveDisplayName(Marshal.PtrToStringUni(app.displayName));
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = packageFullName;
        }

        info = new AppContainerInfo(
            Marshal.PtrToStringUni(app.appContainerName) ?? string.Empty,
            displayName,
            workingDirectory,
            sid,
            packageFullName,
            exempt.Contains(sid));
        return true;
    }

    private static AppEnumerationResult GetAppsFromPackages(
        HashSet<string> exempt,
        CancellationToken cancellationToken,
        Win32Exception batchFailure)
    {
        var apps = new List<AppContainerInfo>();
        var visibleSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedCount = 0;

        try
        {
            var packages = new PackageManager()
                .FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main);
            foreach (var package in packages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (TryCreatePackageApp(package, exempt, out var info) && visibleSids.Add(info.Sid))
                    {
                        apps.Add(info);
                    }
                }
                catch (COMException)
                {
                    skippedCount++;
                }
                catch (UnauthorizedAccessException)
                {
                    skippedCount++;
                }
                catch (Win32Exception)
                {
                    skippedCount++;
                }
                catch (InvalidDataException)
                {
                    skippedCount++;
                }
                catch (ArgumentException)
                {
                    skippedCount++;
                }
                catch (FileNotFoundException)
                {
                    skippedCount++;
                }
                catch (DirectoryNotFoundException)
                {
                    skippedCount++;
                }
                catch (InvalidOperationException)
                {
                    skippedCount++;
                }
            }
        }
        catch (COMException fallbackFailure)
        {
            throw CreateFallbackFailure(batchFailure, fallbackFailure);
        }
        catch (UnauthorizedAccessException fallbackFailure)
        {
            throw CreateFallbackFailure(batchFailure, fallbackFailure);
        }
        catch (FileNotFoundException fallbackFailure)
        {
            throw CreateFallbackFailure(batchFailure, fallbackFailure);
        }

        return CreateEnumerationResult(
            apps,
            exempt,
            new AppEnumerationDiagnostics(
                true,
                skippedCount,
                AppLoadFailure.From(batchFailure).Details));
    }

    private static bool TryCreatePackageApp(
        Package package,
        HashSet<string> exempt,
        out AppContainerInfo info)
    {
        var familyName = package.Id.FamilyName;
        var packageFullName = package.Id.FullName;
        var workingDirectory = package.InstalledLocation?.Path ?? string.Empty;
        if (string.IsNullOrWhiteSpace(familyName) || string.IsNullOrWhiteSpace(workingDirectory))
        {
            info = null!;
            return false;
        }

        var sid = DeriveAppContainerSid(familyName);
        var displayName = ResolveDisplayName(package.DisplayName);
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = string.IsNullOrEmpty(package.Id.Name) ? packageFullName : package.Id.Name;
        }

        info = new AppContainerInfo(
            familyName,
            displayName,
            workingDirectory,
            sid,
            packageFullName,
            exempt.Contains(sid));
        return true;
    }

    private static string DeriveAppContainerSid(string appContainerName)
    {
        var sidPtr = IntPtr.Zero;
        var result = DeriveAppContainerSidFromAppContainerName(appContainerName, ref sidPtr);
        if (result != 0)
        {
            throw new COMException(
                $"DeriveAppContainerSidFromAppContainerName failed for '{appContainerName}' (0x{unchecked((uint)result):X8}).",
                result);
        }
        if (sidPtr == IntPtr.Zero)
        {
            throw new InvalidDataException(
                $"DeriveAppContainerSidFromAppContainerName returned no SID for '{appContainerName}'.");
        }

        try
        {
            return SidToString(sidPtr, $"the AppContainer '{appContainerName}'");
        }
        finally
        {
            if (FreeSid(sidPtr) != IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to free the derived SID for AppContainer '{appContainerName}'.");
            }
        }
    }

    private static AppEnumerationResult CreateEnumerationResult(
        IReadOnlyList<AppContainerInfo> apps,
        HashSet<string> exempt,
        AppEnumerationDiagnostics diagnostics)
    {
        var visibleSids = apps.Select(static app => app.Sid).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preservedExemptSids = exempt
            .Where(sid => !visibleSids.Contains(sid))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new AppEnumerationResult(apps, preservedExemptSids, diagnostics);
    }

    private static AggregateException CreateFallbackFailure(
        Win32Exception batchFailure,
        Exception fallbackFailure)
        => new(
            "Both FirewallAPI batch enumeration and per-package AppContainer enumeration failed.",
            fallbackFailure,
            batchFailure);

    // Reads the current loopback-exemption set as a set of string SIDs.
    private static HashSet<string> GetExemptSids()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        uint count = 0;
        var configPtr = IntPtr.Zero;
        var result = NetworkIsolationGetAppContainerConfig(ref count, ref configPtr);
        if (result != ErrorSuccess)
        {
            throw CreateWin32Exception(result, nameof(NetworkIsolationGetAppContainerConfig));
        }

        if (configPtr == IntPtr.Zero)
        {
            if (count == 0)
            {
                return set;
            }

            throw new InvalidDataException(
                "NetworkIsolationGetAppContainerConfig returned entries without a configuration buffer.");
        }

        try
        {
            var stride = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
            var cursor = configPtr;
            for (uint i = 0; i < count; i++)
            {
                var entry = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(cursor);
                cursor = IntPtr.Add(cursor, stride);
                if (entry.Sid == IntPtr.Zero)
                {
                    continue;
                }
                var sid = SidToString(entry.Sid, "the loopback exemption configuration");
                _ = set.Add(sid);
            }
        }
        finally
        {
            FreeAppContainerConfig(configPtr, count);
        }

        return set;
    }

    private static void SetExemptions(IReadOnlyList<string> exemptSids)
    {
        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sid in exemptSids)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                throw new InvalidOperationException("The loopback exemption set contains an invalid SID.");
            }

            _ = desired.Add(sid);
        }

        var entries = new List<SID_AND_ATTRIBUTES>(desired.Count);
        var allocated = new List<IntPtr>(desired.Count);
        try
        {
            foreach (var sid in desired)
            {
                if (!ConvertStringSidToSidW(sid, out var sidPtr) || sidPtr == IntPtr.Zero)
                {
                    var error = Marshal.GetLastPInvokeError();
                    throw new Win32Exception(
                        error,
                        $"ConvertStringSidToSidW failed for '{sid}' (0x{error:X8}).");
                }

                allocated.Add(sidPtr);
                entries.Add(new SID_AND_ATTRIBUTES { Sid = sidPtr, Attributes = 0 });
            }

            // A full-set replace: passing the complete exempt list (count 0 clears every exemption).
            SID_AND_ATTRIBUTES[] array = [.. entries];
            var result = NetworkIsolationSetAppContainerConfig((uint)array.Length, array);
            if (result != ErrorSuccess)
            {
                throw CreateWin32Exception(result, nameof(NetworkIsolationSetAppContainerConfig));
            }

            var persisted = GetExemptSids();
            if (!persisted.SetEquals(desired))
            {
                throw new InvalidOperationException(
                    "The loopback exemption configuration did not match the requested state after saving.");
            }
        }
        finally
        {
            foreach (var sidPtr in allocated)
            {
                _ = LocalFree(sidPtr);
            }
        }
    }

    // Converts a native SID pointer to its string form. ConvertSidToStringSidW LocalAlloc's the string, so it is copied
    // out and freed.
    private static string SidToString(IntPtr sid, string source)
    {
        if (sid == IntPtr.Zero)
        {
            throw new InvalidDataException($"NetworkIsolation returned a null SID for {source}.");
        }

        if (!ConvertSidToStringSidW(sid, out var stringSid))
        {
            var error = Marshal.GetLastPInvokeError();
            throw CreateWin32Exception(
                unchecked((uint)error),
                $"{nameof(ConvertSidToStringSidW)} for {source}");
        }
        if (stringSid == IntPtr.Zero)
        {
            throw new InvalidDataException($"ConvertSidToStringSidW returned no value for {source}.");
        }

        try
        {
            return Marshal.PtrToStringUni(stringSid)
                ?? throw new InvalidDataException($"ConvertSidToStringSidW returned invalid text for {source}.");
        }
        finally
        {
            _ = LocalFree(stringSid);
        }
    }

    private static Win32Exception CreateWin32Exception(uint error, string operation)
    {
        var nativeError = unchecked((int)error);
        var systemMessage = new Win32Exception(nativeError).Message;
        return new Win32Exception(nativeError, $"{operation} failed: {systemMessage} (0x{error:X8}).");
    }

    // NetworkIsolationGetAppContainerConfig allocates each SID and the outer array from the process heap.
    private static void FreeAppContainerConfig(IntPtr configPtr, uint count)
    {
        var processHeap = GetProcessHeap();
        if (processHeap == IntPtr.Zero)
        {
            throw new InvalidOperationException("GetProcessHeap returned no process heap.");
        }

        var stride = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
        var cursor = configPtr;
        var freeFailed = false;
        for (uint i = 0; i < count; i++)
        {
            var entry = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(cursor);
            cursor = IntPtr.Add(cursor, stride);
            if (entry.Sid != IntPtr.Zero && !HeapFree(processHeap, 0, entry.Sid))
            {
                freeFailed = true;
            }
        }

        if (!HeapFree(processHeap, 0, configPtr))
        {
            freeFailed = true;
        }
        if (freeFailed)
        {
            throw new InvalidOperationException("Failed to free loopback configuration memory.");
        }
    }

    // Resolves an "@{package?ms-resource://…}" indirect display name to its localized string; a plain name is returned
    // as-is.
    private static string ResolveDisplayName(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        if (raw[0] != '@')
        {
            return raw;
        }

        var buffer = new char[1024];
        if (SHLoadIndirectString(raw, buffer, (uint)buffer.Length, IntPtr.Zero) == 0)
        {
            var end = Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, end < 0 ? buffer.Length : end);
        }

        return raw;
    }

    [LibraryImport("FirewallAPI.dll")]
    private static partial uint NetworkIsolationEnumAppContainers(uint flags, ref uint count, ref IntPtr appContainers);

    [LibraryImport("FirewallAPI.dll")]
    private static partial uint NetworkIsolationGetAppContainerConfig(ref uint count, ref IntPtr appContainerSids);

    [LibraryImport("FirewallAPI.dll")]
    private static partial uint NetworkIsolationSetAppContainerConfig(uint count, [In] SID_AND_ATTRIBUTES[] appContainerSids);

    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int DeriveAppContainerSidFromAppContainerName(
        string appContainerName,
        ref IntPtr appContainerSid);

    [LibraryImport("FirewallAPI.dll")]
    private static partial void NetworkIsolationFreeAppContainers(IntPtr appContainers);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ConvertSidToStringSidW(IntPtr sid, out IntPtr stringSid);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ConvertStringSidToSidW(string stringSid, out IntPtr sid);

    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHLoadIndirectString(string source, [Out] char[] outBuffer, uint charCount, IntPtr reserved);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr LocalFree(IntPtr handle);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetProcessHeap();

    [LibraryImport("advapi32.dll")]
    private static partial IntPtr FreeSid(IntPtr sid);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool HeapFree(IntPtr heap, uint flags, IntPtr memory);

    // Blittable mirrors of the FirewallAPI structs: every LPWStr field is an IntPtr (read with PtrToStringUni), so the
    // whole struct is blittable and PtrToStructure needs no marshaller — the NativeAOT-safe shape.
    [StructLayout(LayoutKind.Sequential)]
    private struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INET_FIREWALL_AC_CAPABILITIES
    {
        public uint Count;
        public IntPtr Capabilities;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INET_FIREWALL_AC_BINARIES
    {
        public uint Count;
        public IntPtr Binaries;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INET_FIREWALL_APP_CONTAINER
    {
        public IntPtr appContainerSid;
        public IntPtr userSid;
        public IntPtr appContainerName;
        public IntPtr displayName;
        public IntPtr description;
        public INET_FIREWALL_AC_CAPABILITIES capabilities;
        public INET_FIREWALL_AC_BINARIES binaries;
        public IntPtr workingDirectory;
        public IntPtr packageFullName;
    }
}
