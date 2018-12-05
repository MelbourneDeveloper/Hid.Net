namespace Hid.Net
{
    public class WindowsDevice : WindowsDeviceBase
    {
        public WindowsDevice(string deviceId, ushort inputReportByteLength, ushort outputReportByteLength) : base(deviceId, inputReportByteLength, outputReportByteLength)
        {
        }
    }
}
