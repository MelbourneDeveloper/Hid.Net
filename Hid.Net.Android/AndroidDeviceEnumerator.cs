using Android.Hardware.Usb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hid.Net.Android
{
    public class AndroidDeviceEnumerator : IHidDeviceEnumerator<AndroidHidDevice>
    {
        #region Public Properties
        public UsbManager UsbManager { get; }
        #endregion

        #region Private Properties
        private static string LogSection => nameof(AndroidDeviceEnumerator);
        #endregion

        #region Constructor
        public AndroidDeviceEnumerator(UsbManager usbManager)
        {
            UsbManager = usbManager;
        }
        #endregion

        #region Implementation
        public async Task<List<DeviceInformation>> GetDeviceInformationListAsync(IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds)
        {
            return await Task.Run(() =>
            {
                return GetDeviceInformationList(UsbManager, filterVendorIdAndProductIds);
            });
        }

        public Task<AndroidHidDevice> GetFirstDeviceAsync(IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds)
        {
            throw new System.NotImplementedException();
        }

        public Task<AndroidHidDevice> GetDeviceAsync(string deviceId)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Private Static Methods
        private static List<DeviceInformation> GetDeviceInformationList(UsbManager usbManager, IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds)
        {
            var usbDevices = GetUsbDevices(usbManager, filterVendorIdAndProductIds);

            return usbDevices.Select(d => new DeviceInformation { DeviceId = d.DeviceId.ToString(), VendorId = d.VendorId, ProductId = d.ProductId, SerialNumber = d.SerialNumber }).ToList();
        }

        private static IEnumerable<UsbDevice> GetUsbDevices(UsbManager usbManager, IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds)
        {
            var devices = usbManager.DeviceList.Select(kvp => kvp.Value).ToList();

            Logger.Log($"Connected devices: {string.Join(",", devices.Select(d => $"Vid: {d.VendorId} Pid: {d.ProductId} Product Name: {d.ProductName} Serial Number: {d.SerialNumber} Device Id: {d.DeviceId}"))}", null, LogSection);

            return devices.Where(d => filterVendorIdAndProductIds.Any(vp => (vp.VendorId == d.VendorId) && (vp.ProductId == d.ProductId)));
        }
        #endregion

        #region Public Static Methods
        public static UsbDevice GetFirstUsbDevice(UsbManager usbManager, IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds)
        {
            return GetUsbDevices(usbManager, filterVendorIdAndProductIds).FirstOrDefault();
        }
        #endregion
    }
}