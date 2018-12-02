﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Storage;

namespace Hid.Net.UWP
{
    public class UWPHidDevice : HidDeviceBase, IHidDevice
    {
        #region Events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        #endregion

        #region Fields
        private UsbDevice _HidDevice;
        private TaskCompletionSource<byte[]> _TaskCompletionSource = null;
        private readonly Collection<byte[]> _Chunks = new Collection<byte[]>();
        private bool _IsReading;
        #endregion

        #region Public Properties
        public int VendorId { get; set; }
        public int ProductId { get; set; }

        public string DeviceId { get; set; }
        public bool DataHasExtraByte { get; set; } = true;
        #endregion

        #region Event Handlers

        private void _HidDevice_InputReportReceived(HidDevice sender, HidInputReportReceivedEventArgs args)
        {
            if (!_IsReading)
            {
                lock (_Chunks)
                {
                    var bytes = InputReportToBytes(args);
                    _Chunks.Add(bytes);
                }
            }
            else
            {
                var bytes = InputReportToBytes(args);

                _IsReading = false;

                _TaskCompletionSource.SetResult(bytes);
            }
        }

        private byte[] InputReportToBytes(HidInputReportReceivedEventArgs args)
        {
            byte[] bytes;
            using (var stream = args.Report.Data.AsStream())
            {
                bytes = new byte[args.Report.Data.Length];
                stream.Read(bytes, 0, (int)args.Report.Data.Length);
            }

            if (DataHasExtraByte)
            {
                bytes = Helpers.RemoveFirstByte(bytes);
            }

            return bytes;
        }
        #endregion

        #region Constructors
        public UWPHidDevice()
        {
        }

        public UWPHidDevice(string deviceId)
        {
            DeviceId = deviceId;
        }

        /// <summary>
        /// TODO: Further filter by UsagePage. The problem is that this syntax never seems to work: AND System.DeviceInterface.Hid.UsagePage:=?? 
        /// </summary>
        public UWPHidDevice(int vendorId, int productId)
        {
            VendorId = vendorId;
            ProductId = productId;
        }
        #endregion

        #region Private Methods
        public async Task InitializeAsync()
        {
            //TODO: Put a lock here to stop reentrancy of multiple calls

            //TODO: Dispose but this seems to cause initialization to never occur
            //Dispose();

            Logger.Log("Initializing Hid device", null, nameof(UWPHidDevice));

            if (string.IsNullOrEmpty(DeviceId))
            {
                var foundDevices = await UWPHelpers.GetDevicesByProductAndVendorAsync(VendorId, ProductId);

                if (foundDevices.Count == 0)
                {
                    throw new Exception($"There were no enabled devices connected with the ProductId of {ProductId} and VendorId of {VendorId}");
                }

                foreach (var deviceInformation in foundDevices)
                {
                    try
                    {
                        //Attempt to connect
                        Logger.Log($"Attempting to connect to device Id {deviceInformation.Id} ...", null, nameof(UWPHidDevice));

                        var hidDevice = await GetDevice(deviceInformation.Id);

                        if (hidDevice != null)
                        {
                            _HidDevice = hidDevice;
                            //Connection was successful
                            DeviceId = deviceInformation.Id;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error attempting to connect to {deviceInformation.Id}", ex, nameof(UWPHidDevice));
                    }
                }

                if (string.IsNullOrEmpty(DeviceId))
                {
                    throw new Exception($"Attempted to connect to {foundDevices.Count} devices, but they all failed to connect");
                }
            }
            else
            {
                _HidDevice = await GetDevice(DeviceId);
            }

            if (_HidDevice != null)
            {
                _HidDevice.InputReportReceived += _HidDevice_InputReportReceived;
                Connected?.Invoke(this, new EventArgs());
            }
        }

        private static async Task<UsbDevice> GetDevice(string id)
        {
            var hidDeviceOperation = UsbDevice.FromIdAsync(id);
            var task = hidDeviceOperation.AsTask();
            var hidDevice = await task;
            return hidDevice;
        }
        #endregion

        #region Public Methods
        public async Task<bool> GetIsConnectedAsync()
        {
            return _HidDevice != null;
        }

        public void Dispose()
        {
            _HidDevice.Dispose();
            _TaskCompletionSource?.Task?.Dispose();
        }

        public async Task<byte[]> ReadAsync()
        {
            if (_IsReading)
            {
                throw new Exception("Reentry");
            }

            lock (_Chunks)
            {
                if (_Chunks.Count > 0)
                {
                    var retVal = _Chunks[0];
                    Tracer?.Trace(false, retVal);
                    _Chunks.RemoveAt(0);
                    return retVal;
                }
            }

            _IsReading = true;
            _TaskCompletionSource = new TaskCompletionSource<byte[]>();
            return await _TaskCompletionSource.Task;
        }

        public async Task WriteAsync(byte[] data)
        {
            byte[] bytes;
            if (DataHasExtraByte)
            {
                bytes = new byte[data.Length + 1];
                Array.Copy(data, 0, bytes, 1, data.Length);
                bytes[0] = 0;
            }
            else
            {
                bytes = data;
            }

            var buffer = bytes.AsBuffer();

            try
            {
                var setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.Out,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor
                    },
                    //Whats this then?
                    //Request = SuperMutt.VendorCommand.SetLedBlinkPattern,
                    //Value = pattern,
                    Length = 0
                };

                var bytesTransferred = await _HidDevice.SendControlOutTransferAsync(setupPacket, buffer);

            }
            catch (ArgumentException ex)
            {
                //TODO: Check the string is nasty. Validation on the size of the array being sent should be done earlier anyway
                if (ex.Message == "Value does not fall within the expected range.")
                {
                    throw new Exception("It seems that the data being sent to the device does not match the accepted size. Have you checked DataHasExtraByte?", ex);
                }
            }

            Tracer?.Trace(false, bytes);
        }
        #endregion
    }
}
