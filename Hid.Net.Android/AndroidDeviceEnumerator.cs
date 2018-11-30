using Android.Hardware.Usb;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hid.Net.Android
{
    public class AndroidDeviceEnumerator : IHidDeviceEnumerator
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
        public async Task<List<DeviceInformation>> GetDeviceInformationListAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            return await Task.Run(() =>
            {
                return GetDeviceInformationList(UsbManager, filterVendorIdAndProductIds);
            });
        }

        public Task<IHidDevice> GetFirstDeviceAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            throw new System.NotImplementedException();
        }

        public Task<IHidDevice> GetDeviceAsync(string deviceId)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Public Static Methods
        public static List<DeviceInformation> GetDeviceInformationList(UsbManager usbManager, IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            var devices = usbManager.DeviceList.Select(kvp => kvp.Value).ToList();

            Logger.Log($"Connected devices: {string.Join(",", devices.Select(d => $"Vid: {d.VendorId} Pid: {d.ProductId} Product Name: {d.ProductName} Serial Number: {d.SerialNumber} Device Id: {d.DeviceId}"))}", null, LogSection);

            var usbDevices = devices.Where(d => filterVendorIdAndProductIds.Any(o => (o.VendorId == d.VendorId) && (o.ProductId == d.ProductId)));

            return usbDevices.Select(d => new DeviceInformation { DeviceId = d.DeviceId.ToString(), VendorId = d.VendorId, ProductId = d.ProductId }).ToList();
        }
        #endregion
    }
}