namespace Hid.Net
{
    public class WindowsDeviceInformation : DeviceInformation
    {
        public int InputReportByteLength { get; set; }
        public string Manufacturer { get; set; }
        public int OutputReportByteLength { get; set; }
        public string Product { get; set; }
        public ushort VersionNumber { get; set; }
    }
}