﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Hid.Net
{
    internal static class APICalls
    {
        #region Constants
        public const int DigcfDeviceinterface = 16;
        public const int DigcfPresent = 2;
        public const uint FileShareRead = 1;
        public const uint FileShareWrite = 2;
        public const uint GenericRead = 2147483648;
        public const uint GenericWrite = 1073741824;
        public const uint OpenExisting = 3;
        #endregion

        #region Methods

        #region Kernel32
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        #endregion

        #region Hid
        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_FreePreparsedData(ref IntPtr pointerToPreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HidAttributes attributes);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid(ref Guid hidGuid);

        [DllImport("hid.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool HidD_GetManufacturerString(SafeFileHandle hidDeviceObject, IntPtr pointerToBuffer, uint bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, ref IntPtr pointerToPreparsedData);

        [DllImport("hid.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool HidD_GetProductString(SafeFileHandle hidDeviceObject, IntPtr pointerToBuffer, uint bufferLength);

        [DllImport("hid.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool HidD_GetSerialNumberString(SafeFileHandle hidDeviceObject, IntPtr pointerToBuffer, uint bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        internal static extern int HidP_GetCaps(IntPtr pointerToPreparsedData, ref HidCollectionCapabilities hidCollectionCapabilities);
        #endregion

        #region SetupAPI
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport(@"setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

        [DllImport(@"setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SpDeviceInterfaceData deviceInterfaceData, ref SpDeviceInterfaceDetailData deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, ref SpDeviceInfoData deviceInfoData);
        #endregion

        #endregion
    }
}
