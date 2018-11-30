using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator
    {
        Task<IList<DeviceInformation>> GetDeviceInformationList(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<IHidDevice> GetFirstDevice(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
        Task<IHidDevice> GetDevice(string deviceId);
    }
}
