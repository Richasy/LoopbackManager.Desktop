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

                var sid = SidToString(app.appContainerSid);
                var workingDirectory = Marshal.PtrToStringUni(app.workingDirectory) ?? string.Empty;
                if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(workingDirectory))
                {
                    // Skip system/service containers with no SID or working directory (the original toolkit's filter).
                    continue;
                }

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

    // Reads the current loopback-exemption set as a set of string SIDs. The config array is owned by the API (the
    // original toolkit does not free it), so it is only read, not freed.
    private static HashSet<string> GetExemptSids()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = NetworkIsolationGetAppContainerConfig(out var count, out var configPtr);
        if (result != ErrorSuccess || configPtr == IntPtr.Zero || count == 0)
        {
            return set;
        }

        var stride = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
        var cursor = configPtr;
        for (uint i = 0; i < count; i++)
        {
            var entry = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(cursor);
            cursor = IntPtr.Add(cursor, stride);

            var sid = SidToString(entry.Sid);
            if (!string.IsNullOrEmpty(sid))
            {
                _ = set.Add(sid);
            }
        }

        return set;
    }

    private static void SetExemptions(IReadOnlyList<string> exemptSids)
    {
        var entries = new List<SID_AND_ATTRIBUTES>(exemptSids.Count);
        var allocated = new List<IntPtr>(exemptSids.Count);
        try
        {
            foreach (var sid in exemptSids)
            {
                if (ConvertStringSidToSidW(sid, out var sidPtr) && sidPtr != IntPtr.Zero)
                {
                    allocated.Add(sidPtr);
                    entries.Add(new SID_AND_ATTRIBUTES { Sid = sidPtr, Attributes = 0 });
                }
            }

            // A full-set replace: passing the complete exempt list (count 0 clears every exemption).
            SID_AND_ATTRIBUTES[] array = [.. entries];
            var result = NetworkIsolationSetAppContainerConfig((uint)array.Length, array);
            if (result != ErrorSuccess)
            {
                throw new InvalidOperationException($"NetworkIsolationSetAppContainerConfig failed (0x{result:X8}).");
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
    private static string SidToString(IntPtr sid)
    {
        if (sid == IntPtr.Zero || !ConvertSidToStringSidW(sid, out var stringSid) || stringSid == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUni(stringSid) ?? string.Empty;
        }
        finally
        {
            _ = LocalFree(stringSid);
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
