using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hid.Net
{
    public interface IHidDeviceEnumerator
    {
        Task<IEnumerable<string>> GetDeviceIds(IEnumerable<VendorIdAndProductId> filterVendorIdAndProductIds);
    }
}
