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
        public async Task<List<DeviceInformation>> GetDeviceInformationListAsync(DeviceQuery deviceQuery)
        {
            return await Task.Run(() =>
            {
                return GetDeviceInformationList(UsbManager, deviceQuery);
            });
        }

        public Task<AndroidHidDevice> GetFirstDeviceAsync(DeviceQuery deviceQuery)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Private Static Methods
        private static List<DeviceInformation> GetDeviceInformationList(UsbManager usbManager, DeviceQuery deviceQuery)
        {
            var usbDevices = GetUsbDevices(usbManager, deviceQuery);

            return usbDevices.Select(d => new DeviceInformation { DeviceId = d.DeviceId.ToString(), VendorId = d.VendorId, ProductId = d.ProductId, SerialNumber = d.SerialNumber }).ToList();
        }

        private static IEnumerable<UsbDevice> GetUsbDevices(UsbManager usbManager, DeviceQuery deviceQuery )
        {
            var devices = usbManager.DeviceList.Select(kvp => kvp.Value).ToList();

            Logger.Log($"Connected devices: {string.Join(",", devices.Select(d => $"Vid: {d.VendorId} Pid: {d.ProductId} Product Name: {d.ProductName} Serial Number: {d.SerialNumber} Device Id: {d.DeviceId}"))}", null, LogSection);

            return devices.Where(d => deviceQuery.VendorProductIdPairs.Any(vp => (vp.VendorId == d.VendorId) && (vp.ProductId == d.ProductId)));
        }
        #endregion

        #region Public Static Methods
        public static UsbDevice GetFirstUsbDevice(UsbManager usbManager, DeviceQuery deviceQuery)
        {
            return GetUsbDevices(usbManager, deviceQuery).FirstOrDefault();
        }
        #endregion
    }
}