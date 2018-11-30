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
        private string LogSection => nameof(AndroidDeviceEnumerator);
        #endregion

        #region Implementation
        public Task<IList<DeviceInformation>> GetDeviceIds(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            var devices = UsbManager.DeviceList.Select(kvp => kvp.Value).ToList();

            Logger.Log($"Connected devices: {string.Join(",", devices.Select(d => $"Vid: {d.VendorId} Pid: {d.ProductId} Product Name: {d.ProductName} Serial Number: {d.SerialNumber} Device Id: {d.DeviceId}"))}", null, LogSection);
        }

        public Task<IList<DeviceInformation>> GetDeviceInformationList(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            throw new System.NotImplementedException();
        }

        public Task<IHidDevice> GetFirstDevice(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds)
        {
            throw new System.NotImplementedException();
        }

        public Task<IHidDevice> GetDevice(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public AndroidDeviceEnumerator(UsbManager usbManager)
        {
            UsbManager = usbManager;
        }
        #endregion
    }
}