// Copyright (c) Richasy. All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace LoopbackManager.App.Models
{
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1202 // Elements should be ordered by access
    internal enum NETISO_FLAG
    {
        NETISO_FLAG_FORCE_COMPUTE_BINARIES = 0x1,
        NETISO_FLAG_MAX = 0x2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INET_FIREWALL_AC_CAPABILITIES
    {
        public uint count;
        public IntPtr capabilities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INET_FIREWALL_AC_BINARIES
    {
        public uint count;
        public IntPtr binaries;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INET_FIREWALL_APP_CONTAINER
    {
        internal IntPtr appContainerSid;
        internal IntPtr userSid;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string appContainerName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string displayName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string description;
        internal INET_FIREWALL_AC_CAPABILITIES capabilities;
        internal INET_FIREWALL_AC_BINARIES binaries;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string workingDirectory;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string packageFullName;
    }
}
