namespace Hid.Net
{
    public class WindowsDevice : WindowsDeviceBase
    {
        #region Public Methods
        public override ushort InputReportByteLength { get; }
        public override ushort OutputReportByteLength { get; }
        #endregion

        #region Constructor
        public WindowsDevice(string deviceId, ushort inputReportByteLength, ushort outputReportByteLength) : base(deviceId)
        {
            InputReportByteLength = inputReportByteLength;
            OutputReportByteLength = outputReportByteLength;
        }
        #endregion
    }
}
