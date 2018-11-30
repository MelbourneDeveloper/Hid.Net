namespace Hid.Net
{

    public class DeviceInformation
    {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string DeviceId { get; set; }
        public string SerialNumber { get; set; }
        public ushort Usage { get; set; }
        public ushort UsagePage { get; set; }
    }
}