using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator<T> where T : IHidDevice
    {
        Task<List<DeviceInformation>> GetDeviceInformationListAsync(DeviceQuery deviceQuery);
        Task<T> GetFirstDeviceAsync(DeviceQuery deviceQuery);
    }
}
