using System.Collections.Generic;

namespace Hid.Net
{
    public class DeviceQuery
    {
        /// <summary>
        /// Specify this if the device id specific to the platform is known
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Specify a list of known VendorId ProductId pairs
        /// </summary>
        public List<VendorProductIdPair> VendorProductIdPairs { get; set; }
    }
}
