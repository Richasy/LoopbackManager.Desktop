// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LoopbackManager.Models;

namespace LoopbackManager.UI.Toolkits;

internal static class LoopbackToolkit
{
    private static List<SID_AND_ATTRIBUTES> _appListConfig;
    private static IntPtr _pACs;

    internal static IEnumerable<INET_FIREWALL_APP_CONTAINER> GetApps()
    {
        _pACs = IntPtr.Zero;

        // Full List of Apps
        var appList = PI_NetworkIsolationEnumAppContainers();

        // List of Apps that have LoopUtil enabled.
        _appListConfig = PI_NetworkIsolationGetAppContainerConfig();

        return appList;
    }

    internal static bool CheckLoopback(IntPtr intPtr)
    {
        foreach (var item in _appListConfig)
        {
            ConvertSidToStringSid(item.Sid, out var left);
            ConvertSidToStringSid(intPtr, out var right);

            if (left == right)
            {
                return true;
            }
        }

        return false;
    }

    internal static void FreeResources()
        => NetworkIsolationFreeAppContainers(_pACs);

    [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

    // Use this API to convert a string reference (e.g. "@{blah.pri?ms-resource://whatever}") into a plain string.
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf);

    // Use this API to convert a string SID into an actual SID.
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool ConvertStringSidToSid(string strSid, out IntPtr pSid);

    // Call this API to free the memory returned by the Enumeration API .
    [DllImport("FirewallAPI.dll")]
    internal static extern void NetworkIsolationFreeAppContainers(IntPtr pACs);

    // Call this API to set the LoopUtil-exemption list.
    [DllImport("FirewallAPI.dll")]
    internal static extern uint NetworkIsolationSetAppContainerConfig(uint pdwCntACs, SID_AND_ATTRIBUTES[] appContainerSids);

    // Call this API to load the current list of LoopUtil-enabled AppContainers
    [DllImport("FirewallAPI.dll")]
    internal static extern uint NetworkIsolationGetAppContainerConfig(out uint pdwCntACs, out IntPtr appContainerSids);

    // Call this API to enumerate all of the AppContainers on the system.
    [DllImport("FirewallAPI.dll")]
    internal static extern uint NetworkIsolationEnumAppContainers(uint flags, out uint pdwCntPublicACs, out IntPtr ppACs);

    [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool ConvertSidToStringSid(
        [MarshalAs(UnmanagedType.LPArray)] byte[] pSID,
        out IntPtr ptrSid);

    private static List<SID_AND_ATTRIBUTES> GetCapabilites(INET_FIREWALL_AC_CAPABILITIES cap)
    {
        var mycap = new List<SID_AND_ATTRIBUTES>();
        var arrayValue = cap.capabilities;

        var structSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
        for (var i = 0; i < cap.count; i++)
        {
            var cur = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(arrayValue);
            mycap.Add(cur);
            arrayValue = new IntPtr((long)arrayValue + structSize);
        }

        return mycap;
    }

    private static List<SID_AND_ATTRIBUTES> PI_NetworkIsolationGetAppContainerConfig()
    {
        var arrayValue = IntPtr.Zero;
        uint size = 0;
        var list = new List<SID_AND_ATTRIBUTES>();

        // Pin down variables
        var handle_pdwCntPublicACs = GCHandle.Alloc(size, GCHandleType.Pinned);
        var handle_ppACs = GCHandle.Alloc(arrayValue, GCHandleType.Pinned);

        var retval = NetworkIsolationGetAppContainerConfig(out size, out arrayValue);

        var structSize = Marshal.SizeOf<SID_AND_ATTRIBUTES>();
        for (var i = 0; i < size; i++)
        {
            var cur = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(arrayValue);
            list.Add(cur);
            arrayValue = new IntPtr((long)arrayValue + structSize);
        }

        // release pinned variables.
        handle_pdwCntPublicACs.Free();
        handle_ppACs.Free();

        return list;
    }

    private static List<INET_FIREWALL_APP_CONTAINER> PI_NetworkIsolationEnumAppContainers()
    {
        var arrayValue = IntPtr.Zero;
        uint size = 0;
        var list = new List<INET_FIREWALL_APP_CONTAINER>();

        // Pin down variables
        var handle_pdwCntPublicACs = GCHandle.Alloc(size, GCHandleType.Pinned);
        var handle_ppACs = GCHandle.Alloc(arrayValue, GCHandleType.Pinned);

        var retval = NetworkIsolationEnumAppContainers((int)NETISO_FLAG.NETISO_FLAG_MAX, out size, out arrayValue);
        _pACs = arrayValue;

        var structSize = Marshal.SizeOf<INET_FIREWALL_APP_CONTAINER>();
        for (var i = 0; i < size; i++)
        {
            var cur = Marshal.PtrToStructure<INET_FIREWALL_APP_CONTAINER>(arrayValue);
            list.Add(cur);
            arrayValue = new IntPtr((long)arrayValue + structSize);
        }

        // release pinned variables.
        handle_pdwCntPublicACs.Free();
        handle_ppACs.Free();

        return list;
    }
}
