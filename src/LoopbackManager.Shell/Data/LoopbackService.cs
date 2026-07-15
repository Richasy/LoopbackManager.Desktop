using System.ComponentModel;
using System.Runtime.InteropServices;

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
    private const uint NetisoFlagMax = 0x2;
    private const uint ErrorSuccess = 0;

    /// <inheritdoc/>
    public Task<IReadOnlyList<AppContainerInfo>> GetAppsAsync(CancellationToken cancellationToken)
        => Task.Run(() => GetApps(cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task SetExemptionsAsync(IReadOnlyList<string> exemptSids, CancellationToken cancellationToken)
        => Task.Run(() => SetExemptions(exemptSids), cancellationToken);

    private static IReadOnlyList<AppContainerInfo> GetApps(CancellationToken cancellationToken)
    {
        // The SIDs that currently have a loopback exemption — a set the enumeration below tests each app against.
        var exempt = GetExemptSids();

        var enumResult = NetworkIsolationEnumAppContainers(NetisoFlagMax, out var count, out var appsPtr);
        if (enumResult != ErrorSuccess)
        {
            throw new InvalidOperationException($"NetworkIsolationEnumAppContainers failed (0x{enumResult:X8}).");
        }

        if (appsPtr == IntPtr.Zero || count == 0)
        {
            return [];
        }

        var apps = new List<AppContainerInfo>((int)count);
        try
        {
            var stride = Marshal.SizeOf<INET_FIREWALL_APP_CONTAINER>();
            var cursor = appsPtr;
            for (uint i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var app = Marshal.PtrToStructure<INET_FIREWALL_APP_CONTAINER>(cursor);
                cursor = IntPtr.Add(cursor, stride);

                var workingDirectory = Marshal.PtrToStringUni(app.workingDirectory) ?? string.Empty;
                if (app.appContainerSid == IntPtr.Zero || string.IsNullOrEmpty(workingDirectory))
                {
                    // Skip system/service containers with no SID or working directory (the original toolkit's filter).
                    continue;
                }
                var sid = SidToString(app.appContainerSid, "an enumerated AppContainer");

                var packageFullName = Marshal.PtrToStringUni(app.packageFullName) ?? string.Empty;
                var displayName = ResolveDisplayName(Marshal.PtrToStringUni(app.displayName));
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = packageFullName;
                }

                var containerName = Marshal.PtrToStringUni(app.appContainerName) ?? string.Empty;

                apps.Add(new AppContainerInfo(
                    containerName,
                    displayName,
                    workingDirectory,
                    sid,
                    packageFullName,
                    exempt.Contains(sid)));
            }
        }
        finally
        {
            NetworkIsolationFreeAppContainers(appsPtr);
        }

        return apps;
    }

    // Reads the current loopback-exemption set as a set of string SIDs.
    private static HashSet<string> GetExemptSids()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = NetworkIsolationGetAppContainerConfig(out var count, out var configPtr);
        if (result != ErrorSuccess)
        {
            throw new Win32Exception(
                unchecked((int)result),
                $"NetworkIsolationGetAppContainerConfig failed (0x{result:X8}).");
        }

        if (configPtr == IntPtr.Zero)
        {
            if (count == 0)
            {
                return set;
            }

            throw new InvalidOperationException(
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
                throw new Win32Exception(
                    unchecked((int)result),
                    $"NetworkIsolationSetAppContainerConfig failed (0x{result:X8}).");
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
            throw new InvalidOperationException($"NetworkIsolation returned a null SID for {source}.");
        }

        if (!ConvertSidToStringSidW(sid, out var stringSid))
        {
            var error = Marshal.GetLastPInvokeError();
            throw new Win32Exception(
                error,
                $"ConvertSidToStringSidW failed for {source} (0x{error:X8}).");
        }
        if (stringSid == IntPtr.Zero)
        {
            throw new InvalidOperationException($"ConvertSidToStringSidW returned no value for {source}.");
        }

        try
        {
            return Marshal.PtrToStringUni(stringSid)
                ?? throw new InvalidOperationException($"ConvertSidToStringSidW returned invalid text for {source}.");
        }
        finally
        {
            _ = LocalFree(stringSid);
        }
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
    private static partial uint NetworkIsolationEnumAppContainers(uint flags, out uint count, out IntPtr appContainers);

    [LibraryImport("FirewallAPI.dll")]
    private static partial uint NetworkIsolationGetAppContainerConfig(out uint count, out IntPtr appContainerSids);

    [LibraryImport("FirewallAPI.dll")]
    private static partial uint NetworkIsolationSetAppContainerConfig(uint count, [In] SID_AND_ATTRIBUTES[] appContainerSids);

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
