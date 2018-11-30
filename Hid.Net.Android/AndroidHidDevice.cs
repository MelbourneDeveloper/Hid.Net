using Android.Content;
using Android.Hardware.Usb;
using Java.Nio;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hid.Net.Android
{
    public class AndroidHidDevice : HidDeviceBase, IHidDevice
    {
        #region Fields
        private UsbDeviceConnection _UsbDeviceConnection;
        private UsbDevice _UsbDevice;
        private UsbEndpoint _WriteEndpoint;
        private UsbEndpoint _ReadEndpoint;
        private SemaphoreSlim _SemaphoreSlim = new SemaphoreSlim(1);
        #endregion

        #region Public Constants
        public const string LogSection = "AndroidHidDevice";
        #endregion

        #region Public Properties
        public UsbManager UsbManager { get; }
        public Context AndroidContext { get; private set; }
        public int TimeoutMilliseconds { get; }
        public int ReadBufferLength { get; }
        public int VendorId => _UsbDevice != null ? _UsbDevice.VendorId : 0;
        public int ProductId => _UsbDevice != null ? _UsbDevice.ProductId : 0;
        public DeviceQuery DeviceQuery { get; }
        #endregion

        #region Events
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        #endregion

        #region Constructor
        public AndroidHidDevice(UsbManager usbManager, Context androidContext, int timeoutMilliseconds, int readBufferLength, int vendorId, int productId) : this(usbManager, androidContext, timeoutMilliseconds, readBufferLength, new DeviceQuery { VendorProductIdPairs = { new VendorProductIdPair(vendorId, productId) } })
        {
        }

        /// <summary>
        /// Initializes and Android device based on DeviceQuery. To ensure uniqueness, please specify DeviceId in the query. Otherwise, the first found device will be connected to. Please see AndroidDeviceEnumerator for enumeration utilities.
        /// </summary>
        /// <param name="usbManager"></param>
        /// <param name="androidContext"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="readBufferLength"></param>
        /// <param name="deviceQuery">The query used to filter down the list of connected devices</param>
        public AndroidHidDevice(UsbManager usbManager, Context androidContext, int timeoutMilliseconds, int readBufferLength, DeviceQuery deviceQuery)
        {
            UsbManager = usbManager;
            AndroidContext = androidContext;
            TimeoutMilliseconds = timeoutMilliseconds;
            ReadBufferLength = readBufferLength;
            DeviceQuery = deviceQuery;
        }
        #endregion

        #region Public Methods 

        public async Task<bool> GetIsConnectedAsync()
        {
            try
            {
                RefreshDevice();
                return _UsbDeviceConnection != null;
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting IsConnected on Android device", ex, LogSection);
                throw;
            }
        }

        public async Task UsbDeviceAttached()
        {
            Logger.Log("Device attached", null, LogSection);
            RefreshDevice();
        }

        public async Task UsbDeviceDetached()
        {
            Logger.Log("Device detached", null, LogSection);
            RefreshDevice();
        }

        public void Dispose()
        {
            var wasConnected = _UsbDeviceConnection != null;

            if (wasConnected)
            {
                Logger.Log("Disconnecting previously connected device...", null, LogSection);
                _UsbDeviceConnection?.Dispose();
            }

            _UsbDevice?.Dispose();
            _WriteEndpoint?.Dispose();
            _ReadEndpoint?.Dispose();
            _UsbDevice?.Dispose();

            _UsbDeviceConnection = null;
            _UsbDevice = null;
            _WriteEndpoint = null;
            _ReadEndpoint = null;
            _UsbDevice = null;

            if (wasConnected)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        //TODO: Make async properly
        public async Task<byte[]> ReadAsync()
        {
            try
            {
                var byteBuffer = ByteBuffer.Allocate(ReadBufferLength);
                var request = new UsbRequest();
                request.Initialize(_UsbDeviceConnection, _ReadEndpoint);
                request.Queue(byteBuffer, ReadBufferLength);
                await _UsbDeviceConnection.RequestWaitAsync();
                var buffers = new byte[ReadBufferLength];

                byteBuffer.Rewind();
                for (var i = 0; i < ReadBufferLength; i++)
                {
                    buffers[i] = (byte)byteBuffer.Get();
                }

                //Marshal.Copy(byteBuffer.GetDirectBufferAddress(), buffers, 0, ReadBufferLength);

                Tracer?.Trace(false, buffers);

                return buffers;
            }
            catch (Exception ex)
            {
                Logger.Log(Helpers.ReadErrorMessage, ex, LogSection);
                throw new IOException(Helpers.ReadErrorMessage, ex);
            }
        }

        //TODO: Perhaps we should implement Batch Begin/Complete so that the UsbRequest is not created again and again. This will be expensive
        public async Task WriteAsync(byte[] data)
        {
            try
            {
                var request = new UsbRequest();
                request.Initialize(_UsbDeviceConnection, _WriteEndpoint);
                var byteBuffer = ByteBuffer.Wrap(data);

                Tracer?.Trace(true, data);

                request.Queue(byteBuffer, data.Length);
                await _UsbDeviceConnection.RequestWaitAsync();
            }
            catch (Exception ex)
            {
                Logger.Log(Helpers.WriteErrorMessage, ex, LogSection);
                throw new IOException(Helpers.WriteErrorMessage, ex);
            }
        }

        public async Task InitializeAsync()
        {
            //Wait for existing initialization tasks
            await _SemaphoreSlim.WaitAsync();

            try
            {
                Logger.Log("Initializing Android Hid device", null, LogSection);

                RefreshDevice();

                if (_UsbDevice == null) return;

                var isPermissionGranted = await RequestPermissionAsync();

                if (!isPermissionGranted.HasValue)
                {
                    throw new Exception("User did not respond to permission request");
                }

                if (!isPermissionGranted.Value)
                {
                    throw new Exception("The user did not give the permission to access the device");
                }

                var usbInterface = _UsbDevice.GetInterface(0);

                //TODO: This selection stuff needs to be moved up higher. The constructor should take these arguments
                for (var i = 0; i < usbInterface.EndpointCount; i++)
                {
                    var ep = usbInterface.GetEndpoint(i);
                    if (_ReadEndpoint == null && ep.Type == UsbAddressing.XferInterrupt && ep.Address == (UsbAddressing)129)
                    {
                        _ReadEndpoint = ep;
                        continue;
                    }

                    if (_WriteEndpoint == null && ep.Type == UsbAddressing.XferInterrupt && (ep.Address == (UsbAddressing)1 || ep.Address == (UsbAddressing)2))
                    {
                        _WriteEndpoint = ep;
                    }
                }

                //TODO: This is a bit of a guess. It only kicks in if the previous code fails. This needs to be reworked for different devices
                if (_ReadEndpoint == null)
                {
                    _ReadEndpoint = usbInterface.GetEndpoint(0);
                }

                if (_WriteEndpoint == null)
                {
                    _WriteEndpoint = usbInterface.GetEndpoint(1);
                }

                if (_ReadEndpoint.MaxPacketSize != ReadBufferLength)
                {
                    throw new Exception("Wrong packet size for read endpoint");
                }

                if (_WriteEndpoint.MaxPacketSize != ReadBufferLength)
                {
                    throw new Exception("Wrong packet size for write endpoint");
                }

                _UsbDeviceConnection = UsbManager.OpenDevice(_UsbDevice);

                if (_UsbDeviceConnection == null)
                {
                    throw new Exception("could not open connection");
                }

                if (!_UsbDeviceConnection.ClaimInterface(usbInterface, true))
                {
                    throw new Exception("could not claim interface");
                }

                Logger.Log("Hid device initialized. About to tell everyone.", null, LogSection);

                Connected?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Logger.Log("Error initializing Hid Device", ex, LogSection);
            }
            finally
            {
                _SemaphoreSlim.Release();
            }
        }
        #endregion

        #region Private  Methods
        private void RefreshDevice()
        {
            var usbDevice = AndroidDeviceEnumerator.GetFirstUsbDevice(UsbManager, DeviceQuery);

            if (usbDevice == null)
            {
                Logger.Log("Hid device is not connected", null, LogSection);
                Dispose();
            }
            else
            {
                Logger.Log("Hid device is connected", null, LogSection);

                if (_UsbDevice.DeviceId != usbDevice.DeviceId)
                {
                    Dispose();
                    _UsbDevice = usbDevice;
                }
            }
        }

        private Task<bool?> RequestPermissionAsync()
        {
            Logger.Log("Requesting USB permission", null, LogSection);

            var taskCompletionSource = new TaskCompletionSource<bool?>();

            var usbPermissionBroadcastReceiver = new UsbPermissionBroadcastReceiver(UsbManager, _UsbDevice, AndroidContext);
            usbPermissionBroadcastReceiver.Received += (sender, eventArgs) =>
            {
                taskCompletionSource.SetResult(usbPermissionBroadcastReceiver.IsPermissionGranted);
            };

            usbPermissionBroadcastReceiver.Register();

            return taskCompletionSource.Task;
        }
        #endregion
    }
}