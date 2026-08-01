using System.Diagnostics;
using System.Reflection;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Base class of Cypress USB devices
    /// </summary>
    public class CyUSBDevices : IDisposable
    {
        private readonly USBDeviceList _usbDeviceList;

        // add NEW Cypress-based devices here
        private readonly int[] PIDSupported = Enum.GetValues(typeof(DeviceType)).Cast<int>().ToArray();

        private readonly int VIDSupport = 0x2086;

        private Dictionary<string, EZUSBDevice> ezusbDevices = new ();

        /// <summary>
        ///     Initializes a new instance of the <see cref="CyUSBDevices" /> class.
        /// </summary>
        /// <param name="isDisableHandler">if set to <c>true</c> [is disable handler].</param>
        /// <exception cref="CyUSBInitException"></exception>
        public CyUSBDevices(bool isDisableHandler = false)
        {
            try
            {
                if (_usbDeviceList != null)
                {
                    if (!isDisableHandler)
                    {
                        _usbDeviceList.DeviceRemoved  -= EZUSBDetach;
                        _usbDeviceList.DeviceAttached -= EZUSBAttach;
                    }

                    _usbDeviceList.Dispose();
                }

                // get new device list instance, dispose if already exists
                _usbDeviceList = new (CyConst.DEVICES_CYUSB);

                if (!isDisableHandler)
                {
                    _usbDeviceList.DeviceRemoved  += EZUSBDetach;
                    _usbDeviceList.DeviceAttached += EZUSBAttach;
                }

                ezusbDevices.Clear();

                foreach (USBDevice device in _usbDeviceList)
                {
                    // not the correct VID, ignore it
                    if (device.VendorID != VIDSupport) continue;
                    // create corresponding device objects & add into dictionary
                    try
                    {
                        var ezUSBDevice = GetEzusbDevice(device);
                        ezusbDevices[ezUSBDevice.SerialNumber] = ezUSBDevice;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch (Exception)
            {
                throw new CyUSBInitException();
            }
        }

        /// <summary>
        ///     DLLs the version.
        /// </summary>
        /// <returns></returns>
        public string DLLVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            ezusbDevices.Values.ToList().ForEach(device => device.Dispose());
            ezusbDevices.Clear();
            ezusbDevices = null;
        }

        // handler where event is propagated to the application
        /// <summary>
        ///     Occurs when [device attached ez].
        /// </summary>
        public event EZUSBHandler DeviceAttachedEZ;

        /// <summary>
        ///     Occurs when [device detached ez].
        /// </summary>
        public event EZUSBHandler DeviceDetachedEZ;

        /// <summary>
        ///     Gets the serial numbers.
        /// </summary>
        public string[] GetSerialNumbers(DeviceType deviceType)
        {
            var serailNumber = ezusbDevices.Values
                                           .Where(a => a.ProductID == (int) deviceType
                                                    && a.VendorID  == VIDSupport)
                                           .Select(a => a.SerialNumber)
                                           .ToArray();
            return serailNumber;
        }

        public EZUSBDevice[] GetEzusbDevice(DeviceType deviceType)
        {
            return ezusbDevices.Values
                               .Where(a => a.DeviceType == deviceType)
                               .ToArray();
        }

        public EZUSBDevice GetEzusbDevice(string serailNumber)
        {
            return ezusbDevices[serailNumber];
        }

        public EZUSBDevice GetEzusbDevice(string serailNumber, DeviceType deviceType)
        {
            return ezusbDevices.Values
                               .First(a => a.DeviceType   == deviceType &&
                                           a.SerialNumber == serailNumber);
        }

        private void EZUSBDetach(object sender, EventArgs e)
        {
            var eUSB = e as USBEventArgs;

            if (eUSB != null && IsVIDSupported(eUSB.VendorID) && IsPIDSupported(eUSB.ProductID))
            {
                // refresh dictionary, locate device which is inactive
                var ezUSBDevice = GetDetachedUSBDevice();

                if (ezUSBDevice != null)
                {
                    // found, remove from dictionary & get details
                    ezusbDevices.Remove(ezUSBDevice.SerialNumber);

                    // propagate to upper application
                    var ezUSBEventArgs = new EZUSBEventArgs(ezUSBDevice.VendorID,
                                                            ezUSBDevice.ProductID,
                                                            ezUSBDevice.SerialNumber);
                    DeviceDetachedEZ?.Invoke(this, ezUSBEventArgs);
                }
            }
        }

        private void EZUSBAttach(object sender, EventArgs e)
        {
            var eUSB = e as USBEventArgs;

            if (eUSB != null && IsVIDSupported(eUSB.VendorID) && IsPIDSupported(eUSB.ProductID))
            {
                // refresh dictionary with attached device
                // create corresponding device objects & add into dictionary
                var ezUSBDevice = GetEzusbDevice(eUSB.Device);

                if (ezUSBDevice != null)
                {
                    // propagate to upper application
                    var ezUSBEventArgs = new EZUSBEventArgs(ezUSBDevice.VendorID,
                                                            ezUSBDevice.ProductID,
                                                            ezUSBDevice.SerialNumber);
                    DeviceAttachedEZ?.Invoke(this, ezUSBEventArgs);
                }
            }
        }

        private USBDevice GetAttachedUSBDevice()
        {
            // look for USBDevice which is NOT on the _usbObjects list yet
            USBDevice usbDevice = null;
            var usbList = new USBDeviceList(CyConst.DEVICES_CYUSB);
            foreach (USBDevice usbDev in usbList)
            {
                var objExists = false;
                foreach (var obj in ezusbDevices.Values)
                {
                    objExists = usbDev.SerialNumber.Contains(obj.SerialNumber);
                    if (objExists) break;
                }

                // this is the new device - not in the list yet
                if (!objExists)
                {
                    usbDevice = usbDev;
                    break;
                }
            }

            return usbDevice;
        }

        private EZUSBDevice GetDetachedUSBDevice()
        {
            // look for USBDevice which is no longer in the current USBDeviceList since it was detached already
            EZUSBDevice usbDevice = null;
            var usbList = new USBDeviceList(CyConst.DEVICES_CYUSB);
            foreach (var obj in ezusbDevices.Values)
                try
                {
                    var devExists = false;
                    foreach (USBDevice usbDev in usbList)
                    {
                        var usbDevSN = usbDev.SerialNumber;
                        devExists = usbDevSN.Contains(usbDev.SerialNumber);
                        if (devExists) break;
                    }

                    // this is the new device - not in the list yet
                    if (!devExists)
                    {
                        usbDevice = obj;
                        break;
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.WriteLine(ex.ToString());
                    usbDevice = obj;
                    break;
                }

            return usbDevice;
        }

        private void AddEZUSBDevice(EZUSBDevice usbObjects)
        {
            if (!ezusbDevices.ContainsKey(usbObjects.SerialNumber))
                ezusbDevices.Add(usbObjects.SerialNumber, usbObjects);
        }

        private void RemoveEZUSBDevice(EZUSBDevice usbObjects)
        {
            if (ezusbDevices.ContainsKey(usbObjects.SerialNumber)) ezusbDevices.Remove(usbObjects.SerialNumber);
        }

        private bool IsVIDSupported(int VID)
        {
            return VIDSupport.Equals(VID);
        }

        private bool IsPIDSupported(int PID)
        {
            return PIDSupported.Contains(PID);
        }

        private static EZUSBDevice GetEzusbDevice(USBDevice device)
        {
            switch (device.ProductID)
            {
                //case 0x1131:
                //    return new DeviceMotherShip(device);

                //case 0x1113:
                //    return new DeviceEUI2(device);

                //case 0x1151:
                //    return new DeviceEUI2Plus(device);

                //case 0x1111:
                //    return new DeviceEUI1(device);

                case 0x1115:
                    return new DeviceEUI3(device);

                case 0x1140:
                    return new DeviceFIC2USB(device);

                default:
                    return null;
            }
        }
    }
}