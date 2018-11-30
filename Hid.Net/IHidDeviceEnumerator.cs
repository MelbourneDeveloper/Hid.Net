using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator<T> where T : IHidDevice
    {
        Task<List<DeviceInformation>> GetDeviceInformationListAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<T> GetFirstDeviceAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<T> GetDeviceAsync(string deviceId);
    }
}
