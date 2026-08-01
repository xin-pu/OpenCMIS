/*
 ## Cypress CyUSB C# library source file (USBDevice.cs)
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

using System.Runtime.InteropServices;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Summary description for USBDevice.
    /// </summary>
    public abstract class USBDevice : IDisposable
    {
        protected bool _alreadyDisposed; // Auto initialized to false.
        protected bool _nullEndpointFlag;

        internal IntPtr _hDevice = CyConst.INVALID_HANDLE;
        internal IntPtr _hHndNotification;

        internal Guid _drvGuid;
        internal Guid _hidGuid;

        internal byte _devices;
        internal byte _devNum;

        protected string _name;

        protected string _friendlyName;

        protected string _manufacturer;

        protected string _product;

        protected string _serialNumber;

        protected ushort _vendorID;

        protected ushort _productID;

        public string _path;

        protected byte _usbAddress;

        protected ushort _bcdUSB;

        protected byte _devClass;

        protected byte _devSubClass;

        protected byte _devProtocol;

        protected string _driverName;

        internal USBDevice(Guid g)
        {
            _drvGuid = g;

            // Find-out the HID GUID
            PInvoke.HidD_GetHidGuid(ref _hidGuid);
        }

        public string Name
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _name;
            }
        }

        public string FriendlyName
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _friendlyName;
            }
        }

        public string Manufacturer
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _manufacturer;
            }
        }

        public string Product
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _product;
            }
        }

        public string SerialNumber
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _serialNumber;
            }
        }

        public ushort VendorID
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _vendorID;
            }
        }

        public ushort ProductID
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _productID;
            }
        }

        public string Path
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _path;
            }
        }

        public byte USBAddress
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _usbAddress;
            }
        }

        public ushort BcdUSB
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _bcdUSB;
            }
        }

        public byte DevClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devClass;
            }
        }

        public byte DevSubClass
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devSubClass;
            }
        }

        public byte DevProtocol
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return _devProtocol;
            }
        }

        public virtual TreeNode Tree
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");

                var t = new TreeNode(FriendlyName)
                {
                        Tag = this
                };

                return t;
            }
        }

        public string DriverName => _driverName;

        internal virtual byte DeviceCount
        {
            get
            {
                if (_alreadyDisposed) throw new ObjectDisposedException("");
                return PInvoke.CountDevices(_drvGuid);
            }
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        public override bool Equals(object right)
        {
            if (right == null) return false;

            if (ReferenceEquals(this, right)) return true;

            if (GetType() != right.GetType()) return false;

            var dev = right as USBDevice;

            // The device paths of 2 different devices are unique in Windows
            return _path.Equals(dev._path);
        }

        public override int GetHashCode()
        {
            var rnd = new Random();
            var nRandom = rnd.Next(int.MinValue, int.MaxValue);

            return nRandom ^ GetType().ToString().GetHashCode();
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed) return;

            if (isDisposing)
            {
                // Free managed members that implement IDisposable
            }

            // Free the un-managed resources (handles)
            Close();

            _alreadyDisposed = true;
        }

        internal abstract bool Open(byte dev);

        internal void Close()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            if (_hDevice != CyConst.INVALID_HANDLE) PInvoke.CloseHandle(_hDevice);
            _hDevice = CyConst.INVALID_HANDLE;

            if (_hHndNotification != IntPtr.Zero)
                PInvoke.UnregisterDeviceNotification(_hHndNotification);
        }

        internal bool RegisterForPnPEvents(IntPtr hWnd)
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            var hFilter = new DEV_BROADCAST_HANDLE();
            hFilter.dbch_size       = Marshal.SizeOf(hFilter);
            hFilter.dbch_devicetype = CyConst.DBT_DEVTYP_HANDLE;
            hFilter.dbch_handle     = _hDevice;

            _hHndNotification = PInvoke.RegisterDeviceNotification(hWnd, hFilter, CyConst.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (_hHndNotification == IntPtr.Zero) return false;

            return true;
        }

        // finalizer
        ~USBDevice()
        {
            Dispose(false);
        }
    }
}