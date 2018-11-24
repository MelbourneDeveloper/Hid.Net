﻿using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.HumanInterfaceDevice;

namespace Hid.Net.UWP
{
    public class UWPHidDevice : HidDeviceBase, IHidDevice
    {
        #region Events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        #endregion

        #region Fields
        private HidDevice _HidDevice;
        private TaskCompletionSource<byte[]> _TaskCompletionSource = null;
        private readonly Collection<byte[]> _Chunks = new Collection<byte[]>();

        public int VendorId => _HidDevice.VendorId;
        public int ProductId => _HidDevice.ProductId;
        private bool _IsReading;
        #endregion

        #region Public Properties
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
        #endregion

        #region Private Methods
        public async Task InitializeAsync()
        {
            //TODO: Put a lock here to stop reentrancy of multiple calls

            //TODO: Dispose but this seems to cause initialization to never occur
            //Dispose();

            if (string.IsNullOrEmpty(DeviceId))
            {
                throw new Exception($"{nameof(DeviceInformation)} has not been initialized");
            }

            Logger.Log("Initializing Hid device", null, nameof(UWPHidDevice));

            var hidDeviceOperation = HidDevice.FromIdAsync(DeviceId, Windows.Storage.FileAccessMode.ReadWrite);
            var task = hidDeviceOperation.AsTask();
            _HidDevice = await task;

            if (_HidDevice == null)
            {
                throw new Exception($"Could not obtain a connection to the device.");
            }

            _HidDevice.InputReportReceived += _HidDevice_InputReportReceived;

            if (_HidDevice == null)
            {
                throw new Exception("Could not connect to the device");
            }

            Connected?.Invoke(this, new EventArgs());
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
            var outReport = _HidDevice.CreateOutputReport();
            outReport.Data = buffer;
            var operation = _HidDevice.SendOutputReportAsync(outReport);

            Tracer?.Trace(false, bytes);

            await operation.AsTask();
        }
        #endregion
    }
}
