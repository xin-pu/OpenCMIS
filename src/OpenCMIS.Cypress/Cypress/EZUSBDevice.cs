using System.Diagnostics;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Base class of Cypress EZ USB device
    /// </summary>
    public abstract class EZUSBDevice
    {
        protected EZUSBDevice(USBDevice usbDeviceInstance)
        {
            _usbDevice   = usbDeviceInstance;
            VendorID     = _usbDevice.VendorID;
            ProductID    = _usbDevice.ProductID;
            DeviceType   = (DeviceType) _usbDevice.ProductID;
            SerialNumber = _usbDevice.SerialNumber;

            ProductName  = _usbDevice.Product;
            DriverName   = _usbDevice.DriverName;
            FriendlyName = _usbDevice.FriendlyName;
            Manufacturer = _usbDevice.Manufacturer;
        }

        public CyUSBEndPoint CyUsbEndPointIn  { get; protected set; } // IN from USB to HOST
        public CyUSBEndPoint CyUsbEndPointOut { get; protected set; } // OUT to USB from HOST
        public CyFX2Device   CyFX2Device      { get; set; }
        public USBDevice     _usbDevice       { get; protected set; }

        /// <summary>
        ///     Get DeviceType
        /// </summary>
        public DeviceType DeviceType { get; protected set; }

        /// <summary>
        ///     Gets the product id.
        /// </summary>
        public int ProductID { get; protected set; }

        /// <summary>
        ///     Gets the vendor id.
        /// </summary>
        public int VendorID { get; protected set; }

        /// <summary>
        ///     Gets the serial number.
        /// </summary>
        public string SerialNumber { get; protected set; }

        /// <summary>
        ///     Gets the name of the product.
        /// </summary>
        public string ProductName { get; protected set; }

        /// <summary>
        ///     Gets the name of the driver.
        /// </summary>
        public string DriverName { get; protected set; }

        /// <summary>
        ///     Gets the name of the friendly.
        /// </summary>
        public string FriendlyName { get; protected set; }

        /// <summary>
        ///     Gets the manufacturer.
        /// </summary>
        public string Manufacturer { get; protected set; }

        /// <summary>
        ///     Gets the cypress fw version.
        /// </summary>
        public string CypressFWVersion => GetCypressFWVersion();

        public string FPGAFWVersion => GetFPGAFWVersion();

        /// <summary>
        ///     Gets or sets the us b_timeout.
        /// </summary>
        public uint USB_timeout { get; set; } = 1;

        public virtual string GetSerailNumber()
        {
            return _usbDevice.SerialNumber;
        }

        public virtual string GetCypressFWVersion()
        {
            return "CY." + _usbDevice.SerialNumber.Substring(0, 5);
        }

        public virtual string GetFPGAFWVersion()
        {
            return string.Empty;
        }

        /// <summary>
        ///     Determines whether this instance is detached.
        /// </summary>
        /// <returns></returns>
        public bool IsDetached()
        {
            var retValue = false;
            if (_usbDevice != null)

                    // this instance may not be not valid already, device detached
                    // do dummy read to confirm
            {
                try
                {
                    var dum = ((CyUSBDevice) _usbDevice).EndPointCount;
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.WriteLine(ex.ToString());

                    // validated, device already plugged out
                    retValue = true;
                }
            }
            else
                retValue = true;

            return retValue;
        }

        /// <summary>
        ///     Commands the data.
        /// </summary>
        /// <param name="bufferIn">The buffer in.</param>
        /// <param name="bufferOut">The buffer outs.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public bool CommandData(ref byte[] bufferIn, ref byte[] bufferOut, ref int length)
        {
            var retValue = false;

            if (CyUsbEndPointIn != null && CyUsbEndPointOut != null)
            {
                if (!CyUsbEndPointOut.XferData(ref bufferOut, ref length))
                    return false;

                if (CyUsbEndPointIn.XferData(ref bufferIn, ref length))
                    retValue = true;
            }

            return retValue;
        }

        /// <summary>
        ///     Commands the data.
        /// </summary>
        /// <param name="bufferIn">The buffer in.</param>
        /// <param name="bufferOut">The buffer out.</param>
        /// <param name="Outlength">The out length.</param>
        /// <param name="InLength">Length of the in.</param>
        /// <returns></returns>
        public bool CommandData(ref byte[] bufferIn, ref byte[] bufferOut, ref int Outlength, ref int InLength)
        {
            var retValue = false;

            if (CyUsbEndPointIn != null && CyUsbEndPointOut != null)
            {
                if (!CyUsbEndPointOut.XferData(ref bufferOut, ref Outlength))
                    return false;

                if (CyUsbEndPointIn.XferData(ref bufferIn, ref InLength))
                    retValue = true;
            }

            return retValue;
        }

        /// <summary>
        ///     USBio
        /// </summary>
        /// <param name="CommandName"></param>
        /// <param name="CommandBuffer"></param>
        /// <param name="ResponseBuffer"></param>
        /// <param name="TimeOutInSec"></param>
        /// <returns></returns>
        public bool USBIO(ref byte[] CommandBuffer, ref byte[] ResponseBuffer, float TimeOutInSec = 1f)
        {
            var flag = false;
            if (CyUsbEndPointIn != null && CyUsbEndPointOut != null)
            {
                try
                {
                    ref var local1 = ref CommandBuffer;
                    var     length = CommandBuffer.Length;
                    ref var local2 = ref length;
                    if (!CyUsbEndPointOut.XferData(ref local1, ref local2))
                    {
                        flag = false;
                        return flag;
                    }

                    ref var local3 = ref ResponseBuffer;
                    length = ResponseBuffer.Length;
                    ref var local4 = ref length;
                    if (!CyUsbEndPointIn.XferData(ref local3, ref local4))
                    {
                        flag = false;
                        return flag;
                    }
                }
                catch (Exception ex)
                {
                    var exception = ex;
                    CyFX2Device.BulkOutEndPt.Reset();
                    CyFX2Device.BulkInEndPt.Reset();

                    flag = false;

                    return flag;
                }

                flag = true;
                return flag;
            }

            return flag;
        }

        public virtual bool CheckResponse(byte[] mCommand, byte[] mResponse)
        {
            return mResponse[0] == mCommand[0] && mResponse[checked(mCommand.Length - 1)] == 1;
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            _usbDevice       = null;
            CyUsbEndPointOut = null;
            CyUsbEndPointIn  = null;
        }
    }
}
