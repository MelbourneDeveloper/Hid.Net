using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using wde = Windows.Devices.Enumeration;

namespace Hid.Net.UWP
{
    public class UWPHidDevicePoller
    {
        #region Fields
        private Timer _PollTimer = new Timer(3000);
        private bool _IsPolling;
        #endregion

        #region Public Properties
        public int ProductId { get; }
        public int VendorId { get; }
        public UWPHidDevice UWPHidDevice { get; private set; }
        #endregion

        #region Constructor
        public UWPHidDevicePoller(int productId, int vendorId, UWPHidDevice uwpHidDevice)
        {
            _PollTimer.Elapsed += _PollTimer_Elapsed;
            _PollTimer.Start();
            ProductId = productId;
            VendorId = vendorId;
            UWPHidDevice = uwpHidDevice;
        }
        #endregion

        #region Event Handlers
        private async void _PollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_IsPolling)
            {
                return;
            }

            _IsPolling = true;

            try
            {
                var foundDeviceInformations = await GetDevicesByIdSlowAsync();

                if (UWPHidDevice.WindowsDeviceInformationList == null && (foundDeviceInformations.Count > 0))
                {
                    UWPHidDevice.WindowsDeviceInformationList = foundDeviceInformations;
                }

                if (UWPHidDevice.WindowsDeviceInformationList != null && (foundDeviceInformations.Count == 0))
                {
                    UWPHidDevice.WindowsDeviceInformationList = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Hid polling error", ex, nameof(UWPHidDevicePoller));
            }

            _IsPolling = false;
        }
        #endregion

        #region Private Methods
        public static async Task<wde.DeviceInformationCollection> GetAllDevices()
        {
            return await wde.DeviceInformation.FindAllAsync().AsTask();
        }

        private async Task<List<wde.DeviceInformation>> GetDevicesByIdSlowAsync()
        {
            var allDevices = await GetAllDevices();

            Logger.Log($"Device Ids:{string.Join(", ", allDevices.Select(d => d.Id))} Names:{string.Join(", ", allDevices.Select(d => d.Name))}", null, nameof(UWPHidDevicePoller));

            var vendorIdString = $"VID_{ VendorId.ToString("X").PadLeft(4, '0')}".ToLower();
            var productIdString = $"PID_{ ProductId.ToString("X").PadLeft(4, '0')}".ToLower();

            var filteredDevices = allDevices.Where(args => args.Id.ToLower().Contains(vendorIdString) && args.Id.ToLower().Contains(productIdString) && args.IsEnabled).ToList();

            foreach(var deviceInformation in filteredDevices)
            {
                foreach(var keyValuePair in deviceInformation.Properties)
                {
                    System.Diagnostics.Debug.WriteLine($"Key: {keyValuePair.Key} Value: {keyValuePair.Value}");
                }
            }

            return filteredDevices;
        }
        #endregion

        #region Public Methods
        public void Stop()
        {
            _PollTimer.Stop();
        }
        #endregion
    }
}
