﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace BlackFox.Win32.SetupApi
{
    [SuppressUnmanagedCodeSecurity]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class SetupApiDllNativeMethods
    {
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern SafeDeviceInfoListHandle SetupDiGetClassDevs(IntPtr gClass,
            [MarshalAs(UnmanagedType.LPStr)] string strEnumerator,
            IntPtr hParent,
            GetClassDevsFlags nFlags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            SafeDeviceInfoListHandle lpDeviceInfoSet,
            uint nDeviceInfoData,
            ref Guid gClass,
            uint nIndex,
            ref DeviceInterfaceData oInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetail", CharSet = CharSet.Auto)]
        public static extern bool GetDeviceInterfaceDetail(
            SafeDeviceInfoListHandle lpDeviceInfoSet, ref DeviceInterfaceData oInterfaceData,
            IntPtr oDetailData,
            uint nDeviceInterfaceDetailDataSize, IntPtr nRequiredSize,
            IntPtr lpDeviceInfoData);
    }
}