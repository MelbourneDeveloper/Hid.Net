using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Hid.Net
{
    public abstract class WindowsDeviceBase : DeviceBase, IDevice
    {
        //TODO: Implement
        #region Events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        #endregion

        #region Fields
        private FileStream _ReadFileStream;
        private SafeFileHandle _ReadSafeFileHandle;
        private FileStream _WriteFileStream;
        private SafeFileHandle _WriteSafeFileHandle;
        #endregion

        #region Private Properties
        private string LogSection => nameof(WindowsDeviceBase);
        #endregion

        #region Public Properties
        public bool DataHasExtraByte { get; set; } = true;
        public string DeviceId { get; }
        public bool IsInitialized { get; private set; }
        public abstract ushort WriteBufferSize { get; }
        public abstract ushort ReadBufferSize { get; }
        #endregion

        #region Public Static Methods
        public static Collection<DeviceInformation> GetConnectedDeviceInformations(Guid classGuid)
        {
            var deviceInformations = new Collection<DeviceInformation>();
            var spDeviceInterfaceData = new SpDeviceInterfaceData();
            var spDeviceInfoData = new SpDeviceInfoData();
            var spDeviceInterfaceDetailData = new SpDeviceInterfaceDetailData();
            spDeviceInterfaceData.CbSize = (uint)Marshal.SizeOf(spDeviceInterfaceData);
            spDeviceInfoData.CbSize = (uint)Marshal.SizeOf(spDeviceInfoData);

            var i = APICalls.SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, APICalls.DigcfDeviceinterface | APICalls.DigcfPresent);

            if (IntPtr.Size == 8)
            {
                spDeviceInterfaceDetailData.CbSize = 8;
            }
            else
            {
                spDeviceInterfaceDetailData.CbSize = 4 + Marshal.SystemDefaultCharSize;
            }

            var x = -1;

            while (true)
            {
                x++;

                var setupDiEnumDeviceInterfacesResult = APICalls.SetupDiEnumDeviceInterfaces(i, IntPtr.Zero, ref classGuid, (uint)x, ref spDeviceInterfaceData);
                var errorNumber = Marshal.GetLastWin32Error();

                //TODO: deal with error numbers. Give a meaningful error message

                if (setupDiEnumDeviceInterfacesResult == false)
                {
                    break;
                }

                APICalls.SetupDiGetDeviceInterfaceDetail(i, ref spDeviceInterfaceData, ref spDeviceInterfaceDetailData, 256, out _, ref spDeviceInfoData);

                var deviceInformation = GetDeviceInformation(spDeviceInterfaceDetailData.DevicePath);
                if (deviceInformation == null)
                {
                    continue;
                }

                deviceInformations.Add(deviceInformation);
            }

            APICalls.SetupDiDestroyDeviceInfoList(i);

            return deviceInformations;
        }

        #endregion

        #region Constructor
        protected WindowsDeviceBase(string deviceId)
        {
            DeviceId = deviceId;
        }
        #endregion

        #region Public Methods
        public void Dispose()
        {
            _ReadFileStream?.Dispose();
            _WriteFileStream?.Dispose();

            if (_ReadSafeFileHandle != null && !(_ReadSafeFileHandle.IsInvalid))
            {
                _ReadSafeFileHandle.Dispose();
            }

            if (_WriteSafeFileHandle != null && !_WriteSafeFileHandle.IsInvalid)
            {
                _WriteSafeFileHandle.Dispose();
            }

            Disconnected?.Invoke(this, new EventArgs());
        }

        //TODO
#pragma warning disable CS1998
        public async Task<bool> GetIsConnectedAsync()
#pragma warning restore CS1998
        {
            return IsInitialized;
        }

        public bool Initialize()
        {
            Dispose();

            if (string.IsNullOrEmpty(DeviceId))
            {
                throw new WindowsException($"{nameof(DeviceInformation)} must be specified before {nameof(Initialize)} can be called.");
            }
            var pointerToBuffer = Marshal.AllocHGlobal(126);

            _ReadSafeFileHandle = APICalls.CreateFile(DeviceId, APICalls.GenericRead, APICalls.FileShareRead , IntPtr.Zero, APICalls.OpenExisting, 0, IntPtr.Zero);
            _WriteSafeFileHandle = APICalls.CreateFile(DeviceId, APICalls.GenericWrite,  APICalls.FileShareWrite, IntPtr.Zero, APICalls.OpenExisting, 0, IntPtr.Zero);

            //TODO: Deal with issues here

            Marshal.FreeHGlobal(pointerToBuffer);

            //TODO: Deal with issues here

            if (_ReadSafeFileHandle.IsInvalid)
            {
                return false;
            }

            _ReadFileStream = new FileStream(_ReadSafeFileHandle, FileAccess.Read, ReadBufferSize, false);
            _WriteFileStream = new FileStream(_WriteSafeFileHandle, FileAccess.Write, WriteBufferSize, false);

            IsInitialized = true;

            Connected?.Invoke(this, new EventArgs());

            return true;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() => Initialize());
        }

        public async Task<byte[]> ReadAsync()
        {
            if (_ReadFileStream == null)
            {
                throw new Exception("The device has not been initialized");
            }

            var bytes = new byte[ReadBufferSize];

            try
            {
                _ReadFileStream.Read(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(Helpers.ReadErrorMessage, ex, LogSection);
                throw new IOException(Helpers.ReadErrorMessage, ex);
            }

            byte[] retVal;
            if (DataHasExtraByte)
            {
                retVal = Helpers.RemoveFirstByte(bytes);
            }
            else
            {
                retVal = bytes;
            }

            Tracer?.Trace(false, retVal);

            return retVal;
        }

        public async Task WriteAsync(byte[] data)
        {
            if (_WriteFileStream == null)
            {
                throw new Exception("The device has not been initialized");
            }

            if (data.Length > WriteBufferSize)
            {
                throw new Exception($"Data is longer than {WriteBufferSize} bytes which is the device's OutputReportByteLength.");
            }

            byte[] bytes;
            if (DataHasExtraByte)
            {
                if (WriteBufferSize == data.Length)
                {
                    throw new DeviceException($"The data sent to the device was a the same length as the HidCollectionCapabilities.OutputReportByteLength. This probably indicates that DataHasExtraByte should be set to false.");
                }

                bytes = new byte[WriteBufferSize];
                Array.Copy(data, 0, bytes, 1, data.Length);
                bytes[0] = 0;
            }
            else
            {
                bytes = data;
            }

            if (_WriteFileStream.CanWrite)
            {
                try
                {
                    _WriteFileStream.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Logger.Log(Helpers.WriteErrorMessage, ex, LogSection);
                    throw new IOException(Helpers.WriteErrorMessage, ex);
                }

                Tracer?.Trace(true, bytes);
            }
            else
            {
                throw new IOException("The file stream cannot be written to");
            }
        }
        #endregion

        #region Private Static Methods
        private static DeviceInformation GetDeviceInformation(string devicePath)
        {
            using (var safeFileHandle = APICalls.CreateFile(devicePath, APICalls.GenericRead | APICalls.GenericWrite, APICalls.FileShareRead | APICalls.FileShareWrite, IntPtr.Zero, APICalls.OpenExisting, 0, IntPtr.Zero))
            {
                var hidCollectionCapabilities = new HidCollectionCapabilities();
                var hidAttributes = new HidAttributes();
                var product = string.Empty;
                var serialNumber = string.Empty;
                var manufacturer = string.Empty;
                var pointerToBuffer = Marshal.AllocHGlobal(126);

                Marshal.FreeHGlobal(pointerToBuffer);

                //TODO: Deal with issues here

                var deviceInformation = new DeviceInformation
                {
                    DevicePath = devicePath,
                    InputReportByteLength = hidCollectionCapabilities.InputReportByteLength,
                    Manufacturer = manufacturer,
                    OutputReportByteLength = hidCollectionCapabilities.OutputReportByteLength,
                    Product = product,
                    ProductId = (ushort)hidAttributes.ProductId,
                    SerialNumber = serialNumber,
                    Usage = hidCollectionCapabilities.Usage,
                    UsagePage = hidCollectionCapabilities.UsagePage,
                    VendorId = (ushort)hidAttributes.VendorId,
                    VersionNumber = (ushort)hidAttributes.VersionNumber
                };

                return deviceInformation;
            }
        }
        #endregion
    }
}