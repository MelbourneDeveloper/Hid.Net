using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator<T> where T : IHidDevice
    {
        Task<List<DeviceInformation>> GetDeviceInformationListAsync(IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds);
        Task<T> GetFirstDeviceAsync(IEnumerable<VendorProductIdPair> filterVendorIdAndProductIds);
        Task<T> GetDeviceAsync(string deviceId);
    }
}
