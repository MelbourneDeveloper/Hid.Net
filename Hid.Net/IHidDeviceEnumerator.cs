using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator
    {
        Task<List<DeviceInformation>> GetDeviceInformationListAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<IHidDevice> GetFirstDeviceAsync(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<IHidDevice> GetDeviceAsync(string deviceId);
    }
}
