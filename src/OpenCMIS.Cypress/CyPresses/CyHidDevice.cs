/*
 ## Cypress CyUSB C# library source file (CyHidDevice.cs)
 ## =======================================================
 ##
 ##  Copyright Cypress Semiconductor Corporation, 2009-2012,
 ##  All Rights Reserved
 ##  UNPUBLISHED, LICENSED SOFTWARE.
 ##
 ##  CONFIDENTIAL AND PROPRIETARY INFORMATION
 ##  WHICH IS THE PROPERTY OF CYPRESS.
 ##
 ##  Use of this file is governed
 ##  by the license agreement included in the file
 ##
 ##  <install>/license/license.rtf
 ##
 ##  where <install> is the Cypress software
 ##  install root directory path.
 ##
 ## =======================================================
*/

using System.Text;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Summary description for CyHidDevice.
    /// </summary>
    public unsafe class CyHidDevice : USBDevice
    {
        private  byte *          PreParsedData;
        internal HIDD_ATTRIBUTES Attributes;

        private HIDP_CAPS _Capabilities;

        private int _Access;

        internal CyHidDevice(Guid g)
                : base(g) { }

        public HIDP_CAPS Capabilities => _Capabilities;
        public ushort    Usage        { get; private set; }

        public ushort UsagePage { get; private set; }

        public ushort Version { get; private set; }

        public CyHidReport Inputs { get; private set; }

        public CyHidReport Outputs { get; private set; }

        public CyHidReport Features { get; private set; }

        public override TreeNode Tree
        {
            get
            {
                var nodes = 0;
                if (Features.NumItems > 0)
                    nodes++;
                if (Inputs.NumItems > 0)
                    nodes++;
                if (Outputs.NumItems > 0)
                    nodes++;

                var n = 0;

                var hidTree = new TreeNode[nodes];
                if (Features.NumItems > 0)
                    hidTree[n++] = Features.Tree;
                if (Inputs.NumItems > 0)
                    hidTree[n++] = Inputs.Tree;
                if (Outputs.NumItems > 0)
                    hidTree[n++] = Outputs.Tree;

                var t = new TreeNode(Product, hidTree)
                        {
                            Tag = this
                        };

                return t;
            }
        }

        public bool RwAccessible => _Access > 0;

        internal static Guid Guid
        {
            get
            {
                var hG = Guid.Empty;
                PInvoke.HidD_GetHidGuid(ref hG);
                return hG;
            }
        }

        public override string ToString()
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException("");

            var s = new StringBuilder("<HID_DEVICE>\r\n");

            s.Append($"\tFriendlyName=\"{FriendlyName}\"\r\n");
            s.Append($"\tManufacturer=\"{Manufacturer}\"\r\n");
            s.Append($"\tProduct=\"{Product}\"\r\n");
            s.Append($"\tSerialNumber=\"{SerialNumber}\"\r\n");

            //s.Append(string.Format("\tVendorID=\"{0:X4}\"\r\n", VendorID));
            s.Append($"\tVendorID=\"{Util.byteStr(VendorID)}\"\r\n");
            s.Append($"\tProductID=\"{Util.byteStr(ProductID)}\"\r\n");
            s.Append($"\tClass=\"{_devClass:X2}h\"\r\n");
            s.Append($"\tSubClass=\"{_devSubClass:X2}h\"\r\n");
            s.Append($"\tProtocol=\"{_devProtocol:X2}h\"\r\n");
            s.Append($"\tBcdUSB=\"{Util.byteStr(_bcdUSB)}\"\r\n");
            s.Append($"\tUsage=\"{Util.byteStr(Usage)}\"\r\n");
            s.Append($"\tUsagePage=\"{Util.byteStr(UsagePage)}\"\r\n");
            s.Append($"\tVersion=\"{Util.byteStr(Version)}\"\r\n");

            if (Features.NumItems > 0)
                s.Append(Features);

            if (Inputs.NumItems > 0)
                s.Append(Inputs);

            if (Outputs.NumItems > 0)
                s.Append(Outputs);

            s.Append("</HID_DEVICE>\r\n");
            return s.ToString();
        }

        public bool GetFeature(int rptID)
        {
            if (Features.RptByteLen == 0)
                return false;

            //if (!RwAccessible) return false;

            Features.Clear();
            Features.DataBuf[0] = (byte) rptID;

            fixed (byte * buf = Features.DataBuf)
            {
                return PInvoke.HidD_GetFeature(_hDevice, Features.DataBuf, Features.RptByteLen);
            }

            //return PInvoke.HidD_GetFeature(_hDevice, ref _Features.DataBuf[0], _Features.RptByteLen);
        }

        public bool SetFeature(int rptID)
        {
            if (Features.RptByteLen == 0)
                return false;

            //if (!RwAccessible) return false;

            Features.DataBuf[0] = (byte) rptID;

            fixed (byte * buf = Features.DataBuf)
            {
                return PInvoke.HidD_SetFeature(_hDevice, Features.DataBuf, Features.RptByteLen);
            }

            //return PInvoke.HidD_SetFeature(_hDevice, ref _Features.DataBuf[0], _Features.RptByteLen);
        }

        public bool GetInput(int rptID)
        {
            if (Inputs.RptByteLen == 0)
                return false;
            if (!RwAccessible)
                return false;

            Inputs.Clear();
            Inputs.DataBuf[0] = (byte) rptID;

            // ReadFile will hang if the device does not have an input report ready.
            //int bytesRead = 0;
            //return PInvoke.ReadFile(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen, ref bytesRead, null);

            // GetInputReport always returns right away
            fixed (byte * buf = Inputs.DataBuf)
            {
                return PInvoke.HidD_GetInputReport(_hDevice, Inputs.DataBuf, Inputs.RptByteLen);
            }

            //return PInvoke.HidD_GetInputReport(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen);
        }

        public bool SetOutput(int rptID)
        {
            if (Outputs.RptByteLen == 0)
                return false;
            if (!RwAccessible)
                return false;

            Outputs.DataBuf[0] = (byte) rptID;

            fixed (byte * buf = Outputs.DataBuf)
            {
                return PInvoke.HidD_SetOutputReport(_hDevice, Outputs.DataBuf, Outputs.RptByteLen);
            }

            //return PInvoke.HidD_SetOutputReport(_hDevice, ref _Outputs.DataBuf[0], _Outputs.RptByteLen);
        }

        public bool WriteOutput()
        {
            var bytesWritten = 0;

            if (Outputs.RptByteLen == 0)
                return false;
            if (!RwAccessible)
                return false;

            Outputs.DataBuf[0] = Outputs.ID;

            fixed (byte * buf = Outputs.DataBuf)
            {
                return PInvoke.WriteFile(_hDevice, Outputs.DataBuf, Outputs.RptByteLen, ref bytesWritten, IntPtr.Zero);
            }

            //return PInvoke.WriteFile(_hDevice, ref _Outputs.DataBuf[0], _Outputs.RptByteLen, ref bytesWritten, IntPtr.Zero);
        }

        public bool ReadInput()
        {
            if (Inputs.RptByteLen == 0)
                return false;
            if (!RwAccessible)
                return false;

            if (CyConst.Hibernate_first_call)
            {
                CyConst.Hibernate_first_call = false;
                return false;
            }

            Inputs.Clear();

            // ReadFile will hang if the device does not have an input report ready.
            var bytesRead = 0;

            fixed (byte * buf = Inputs.DataBuf)
            {
                return PInvoke.ReadFile(_hDevice, Inputs.DataBuf, Inputs.RptByteLen, ref bytesRead, IntPtr.Zero);
            }

            //return PInvoke.ReadFile(_hDevice, ref _Inputs.DataBuf[0], _Inputs.RptByteLen, ref bytesRead, IntPtr.Zero);
        }

        // Opens a handle to the devTH device attached the HIDUSB.SYS driver
        internal override bool Open(byte dev)
        {
            // If this object already has the driver open, close it.
            if (_hDevice != CyConst.INVALID_HANDLE)
                Close();

            int Devices = DeviceCount;
            if (Devices == 0)
                return false;
            if (dev > Devices - 1)
                return false;

            string pathDetect;
            _path      = PInvoke.GetDevicePath(_drvGuid, dev);
            pathDetect = _path;
            if (pathDetect.Contains("&mi_00#"))
                return false;

            _hDevice = PInvoke.GetDeviceHandle(_path, false, ref _Access);
            if (_hDevice == CyConst.INVALID_HANDLE)
                return false;

            _devNum = dev;

            PInvoke.HidD_GetPreparsedData(_hDevice, ref PreParsedData);
            PInvoke.HidD_GetAttributes(_hDevice, ref Attributes);
            PInvoke.HidP_GetCaps(PreParsedData, ref _Capabilities);

            Inputs   = new (HIDP_REPORT_TYPE.HidP_Input, _Capabilities, PreParsedData);
            Outputs  = new (HIDP_REPORT_TYPE.HidP_Output, _Capabilities, PreParsedData);
            Features = new (HIDP_REPORT_TYPE.HidP_Feature, _Capabilities, PreParsedData);

            if (null != PreParsedData)
                PInvoke.HidD_FreePreparsedData(PreParsedData);
            PreParsedData = null;

            var buffer = new byte[512];

            fixed (byte * buf = buffer)
            {
                var sChars = (char *) buf;

                if (PInvoke.HidD_GetManufacturerString(_hDevice, buffer, 512))
                    _manufacturer = new (sChars);

                if (PInvoke.HidD_GetProductString(_hDevice, buffer, 512))
                    _product = new (sChars);

                if (PInvoke.HidD_GetSerialNumberString(_hDevice, buffer, 512))
                    _serialNumber = new (sChars);
            }

            // Shortcut members.
            _vendorID  = Attributes.VendorID;
            _productID = Attributes.ProductID;
            Version    = Attributes.VersionNumber;
            Usage      = _Capabilities.Usage;
            UsagePage  = _Capabilities.UsagePage;

            _driverName = "usbhid.sys";

            return true;
        }
    }
}
