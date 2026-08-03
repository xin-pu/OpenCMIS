/*
 ## Cypress CyUSB C# library source file (USBDeviceList.cs)
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

using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

// Any devices attached to the CyUSB3.sys driver via CyConst.DEVICES_CYUSB or
// via a custom GUID version of the driver are instantiated as FX2 devices.
//
// That is, they have built into them the additional FX2-specific functionality.
// Of course, since CyFX2Device is derived from CyUSBDevice, they have all the
// normal CyUSBDevice functionality as well.  The difference is how the application
// chooses to cast the object.  It can be done in either of the following ways
//
//   CyFX2Device fx2 = usbDevices[0] as CyFX2Device;
// or
//   CyUSBDevice dev = usbDevices[0] as CyUSBDevice;
//
// If a non-FX2 device is casted as a CyFX2Device, the FX2-specific functions will fail.
// It is left to the application developer to only cast as CyFX2Device if he knows that
// the device is really an FX2.
//
//

namespace OpenCMIS.Cypress
{
    public class USBDeviceList : IDisposable, IEnumerable
    {
        private readonly ArrayList Items;
        private readonly ArrayList hDevNotifications;
        private readonly ArrayList USBDriverGuids;

        private readonly Guid HidGuid;

        private readonly App_PnP_Callback EventCallBack; // The internal event handler
        private readonly App_PnP_Callback AppCallBack;   // The application's event handler, passed in to the constructor

        private readonly MsgForm MsgWin = new ();  // Used as a hook for PnP event notification
        private          bool    _alreadyDisposed; // Auto initialized to false

        // Provide 2 constructor signatures for optional PnP event handler delegate
        public USBDeviceList(byte DeviceMask)
                : this(DeviceMask, null) { }

        public USBDeviceList(byte DeviceMask, App_PnP_Callback fnCallBack)
        {
            Items             = new ();
            hDevNotifications = new ();
            USBDriverGuids    = new ();

            EventCallBack = PnP_Event_Handler;

            MsgWin.AppCallback = EventCallBack;
            AppCallBack        = fnCallBack;

            // Get the HID GUID
            PInvoke.HidD_GetHidGuid(ref HidGuid);

            // Create list of driver GUIDs for this instance
            FillDriverGuids(DeviceMask);

            USBDevice tmpDev, tmp;
            var       devs = 0;

            foreach (Guid guid in USBDriverGuids)
            {
                // tmpDev is just used for the DeviceCount functionality
                if (guid.Equals(CyConst.StorGuid))
                    tmpDev = new CyUSBStorDevice(guid);
                else if (guid.Equals(HidGuid))
                    tmpDev = new CyHidDevice(HidGuid);
                else
                    tmpDev = new CyFX2Device(guid);

                // DeviceCount is IO intensive. Don't use it as for loop limit
                devs = tmpDev.DeviceCount;

                for (var d = 0; d < devs; d++)
                {
                    // Create the new USBDevice objects of the correct type, based on guid
                    if (guid.Equals(CyConst.StorGuid))
                        tmp = new CyUSBStorDevice(guid);
                    else if (guid.Equals(HidGuid))
                        tmp = new CyHidDevice(HidGuid);
                    else
                    {
                        tmp = new CyFX2Device(guid);
                        if (tmp.Open((byte) d))
                        {
                            // open handle to check device type
                            var t = tmp as CyUSBDevice;
                            if (!t.CheckDeviceTypeFX3FX2())
                            {
                                //FX3
                                tmp.Close();
                                tmp = new CyFX3Device(guid);
                            }
                            else
                                tmp.Close();
                        }
                    }

                    if (tmp.Open((byte) d))
                    {
                        Items.Add(tmp); // This creates new reference to tmp in Items
                        tmp.RegisterForPnPEvents(MsgWin.Handle);
                    }
                }

                if (guid.Equals(CyConst.StorGuid)) // We're not sure which drivers were identified, so setup PnP with both
                {
                    RegisterForPnpEvents(MsgWin.Handle, CyConst.DiskGuid);
                    RegisterForPnpEvents(MsgWin.Handle, CyConst.CdGuid);
                }
                else
                    RegisterForPnpEvents(MsgWin.Handle, guid);
            } // foreach guid
        }

        // Indexers . . . cool!
        public USBDevice this[int index]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                if (Items.Count == 0)
                    return null;

                return (USBDevice) Items[index];
            }

            set
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                Items[index] = value;
            }
        }

        public USBDevice this[string infName]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                foreach (USBDevice tmp in Items)
                    if (infName.Equals(tmp.FriendlyName))
                        return tmp;

                return null;
            }
        }

        public USBDevice this[int VID, int PID]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                foreach (USBDevice tmp in Items)
                    if (VID == tmp.VendorID && PID == tmp.ProductID)
                        return tmp;

                return null;
            }
        }

        public int Count
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                return Items.Count;
            }
        }

        public CyHidDevice this[int VID, int PID, int UsagePg, int Usage]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                foreach (USBDevice dev in Items)
                {
                    var tmp = dev as CyHidDevice;
                    if (tmp     != null          && VID   == tmp.VendorID && PID == tmp.ProductID &&
                        UsagePg == tmp.UsagePage && Usage == tmp.Usage)
                        return tmp;
                }

                return null;
            }
        }

        public CyHidDevice this[string sMfg, string sProd]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                foreach (USBDevice dev in Items)
                {
                    var tmp = dev as CyHidDevice;
                    if (tmp != null && sMfg.Equals(tmp.Manufacturer) && sProd.Equals(tmp.Product))
                        return tmp;
                }

                return null;
            }
        }

        public CyHidDevice this[string sMfg, string sProd, int UsagePg, int Usage]
        {
            get
            {
                if (_alreadyDisposed)
                    throw new ObjectDisposedException("");

                foreach (USBDevice dev in Items)
                {
                    var tmp = dev as CyHidDevice;
                    if (tmp     != null          && sMfg.Equals(tmp.Manufacturer) && sProd.Equals(tmp.Product) &&
                        UsagePg == tmp.UsagePage && Usage == tmp.Usage)
                        return tmp;
                }

                return null;
            }
        }

        // IDisposable implementation
        public void Dispose()
        {
            MsgWin.Call_Dispose(true);
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        //IEnumerable implementation
        public IEnumerator GetEnumerator()
        {
            foreach (USBDevice dev in Items)
                yield return dev;
        }

        public event EventHandler DeviceAttached;

        public event EventHandler DeviceRemoved;

        public void Remove(IntPtr hDev)
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException("");

            // Can't use foreach here, as we're modifying Items within the loop
            for (byte i = 0; i < Count; i++)
            {
                var tmp = (USBDevice) Items[i];
                if (hDev.Equals(tmp._hDevice))
                {
                    Items.Remove(tmp);
                    tmp.Dispose();
                }
            }
        }

        public void Remove(IntPtr hDev, USBEventArgs e)
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException("");

            // Can't use foreach here, as we're modifying Items within the loop
            for (byte i = 0; i < Count; i++)
            {
                var tmp = (USBDevice) Items[i];
                if (hDev.Equals(tmp._hDevice))
                {
                    e.Device       = null;
                    e.FriendlyName = tmp.FriendlyName;
                    e.Manufacturer = tmp.Manufacturer;
                    e.Product      = tmp.Product;
                    e.VendorID     = tmp.VendorID;
                    e.ProductID    = tmp.ProductID;
                    e.SerialNum    = tmp.SerialNumber;

                    Items.Remove(tmp);
                    tmp.Dispose();
                }
            }
        }

        // Uses the Equals method to determine if dev is already in the list
        public byte DeviceIndex(USBDevice dev)
        {
            byte x = 0; // Index of tmp

            foreach (USBDevice tmp in Items)
            {
                if (dev.Equals(tmp))
                    return x;

                x++;
            }

            return 0xFF; // Device wasn't found
        }

        public USBDevice Add()
        {
            if (_alreadyDisposed)
                throw new ObjectDisposedException("");

            USBDevice tmp, tmpDev;

            foreach (Guid guid in USBDriverGuids)
            {
                // tmpDev is just used for the DeviceCount functionality
                if (guid.Equals(CyConst.StorGuid))
                    tmpDev = new CyUSBStorDevice(guid);
                else if (guid.Equals(HidGuid))
                    tmpDev = new CyHidDevice(guid);
                else
                    tmpDev = new CyFX2Device(guid);

                // The number of devices now connected to this GUID
                int connectedDevs = tmpDev.DeviceCount;
                tmpDev.Dispose();

                // Find out how many items have this guid
                var listedDevs = 0;
                foreach (USBDevice dev in Items)
                    if (guid.Equals(CyConst.StorGuid) && (dev._drvGuid.Equals(CyConst.CdGuid) || dev._drvGuid.Equals(CyConst.DiskGuid)))
                        listedDevs++;
                    else if (dev._drvGuid.Equals(guid))
                        listedDevs++;

                // If greater, add
                if (connectedDevs > listedDevs)
                {
                    for (byte d = 0; d < connectedDevs; d++)
                    {
                        // Create the new USBDevice object of the correct type, based on guid
                        if (guid.Equals(CyConst.StorGuid))
                            tmp = new CyUSBStorDevice(guid);
                        else if (guid.Equals(HidGuid))
                            tmp = new CyHidDevice(guid);
                        else
                        {
                            tmp = new CyFX2Device(guid);
                            if (tmp.Open(d))
                            {
                                // open handle to check device type
                                var t = tmp as CyUSBDevice;
                                if (!t.CheckDeviceTypeFX3FX2())
                                {
                                    //FX3
                                    tmp.Close();
                                    tmp = new CyFX3Device(guid);
                                }
                                else
                                    tmp.Close();
                            }
                        }

                        // If this device not already in the list
                        if (tmp.Open(d) && DeviceIndex(tmp) == 0xFF)
                        {
                            Items.Add(tmp);
                            tmp.RegisterForPnPEvents(MsgWin.Handle);
                            if (connectedDevs == 1 && d == 0)
                                return tmp;
                        }
                    }
                }
            }

            return null;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_alreadyDisposed)
                return;

            if (isDisposing)

                    // Free managed members that implement IDisposable
            {
                foreach (USBDevice u in Items)
                    u.Dispose();
            }

            // Free the un-managed resources (handles)
            foreach (IntPtr h in hDevNotifications)
                PInvoke.UnregisterDeviceNotification(h);

            _alreadyDisposed = true;
        }

        private void PnP_Event_Handler(IntPtr pnpEvent, IntPtr hRemovedDevice)
        {
            if (AppCallBack != null)
                AppCallBack(pnpEvent, hRemovedDevice);
            else
            {
                var e = new USBEventArgs();

                if (pnpEvent.Equals(CyConst.DBT_DEVICEREMOVECOMPLETE))
                {
                    Remove(hRemovedDevice, e); // Sets the contents of e

                    if (DeviceRemoved != null)
                        DeviceRemoved(this, e);
                }

                if (pnpEvent.Equals(CyConst.DBT_DEVICEARRIVAL))
                {
                    var newDev = Add(); // Find and add the new device to the list

                    if (DeviceAttached != null)
                    {
                        if (newDev != null)
                        {
                            e.Device       = newDev;
                            e.FriendlyName = newDev.FriendlyName;
                            e.Manufacturer = newDev.Manufacturer;
                            e.Product      = newDev.Product;
                            e.VendorID     = newDev.VendorID;
                            e.ProductID    = newDev.ProductID;
                            e.SerialNum    = newDev.SerialNumber;
                        }

                        DeviceAttached(this, e);
                    }
                }
            }
        }

        private Guid GuidFromString(string sguid)
        {
            // This routine created because, occasionally, ill-formed GUIDs were being
            // found in the registry.  This routine handles such situations, returning Guid.Empty.
            try
            {
                var g = new Guid(sguid);
                return g;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        private void FillDriverGuids(byte DevMask)
        {
            // Look through the registry for USB drivers that have a DriverGUID in their
            // Device Parameters key.  Assume any such device is a derivative of CyUSB3.sys
            if ((DevMask & CyConst.DEVICES_CYUSB) == CyConst.DEVICES_CYUSB)
            {
                var rkDevs = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USB");

                if (rkDevs == null)
                    return;

                var sDevs = rkDevs.GetSubKeyNames();

                foreach (var sUsbDev in sDevs)
                {
                    RegistryKey rkSNums = null;
                    try
                    {
                        rkSNums = rkDevs.OpenSubKey(sUsbDev);
                    }
                    catch (SecurityException) { }

                    if (rkSNums != null)
                    {
                        var sSNums = rkSNums.GetSubKeyNames();

                        foreach (var serNum in sSNums)
                        {
                            RegistryKey rkSNum = null;
                            try
                            {
                                rkSNum = rkSNums.OpenSubKey(serNum);
                            }
                            catch (SecurityException) { }

                            if (rkSNum != null)
                            {
                                var sDriver = rkSNum.GetValue("Driver") as string;
                                if (sDriver != null)
                                {
                                    var slashPos = sDriver.LastIndexOf("\\");
                                    if (slashPos > 0)
                                    {
                                        var dx = $"{sDriver.Substring(slashPos + 1, 4):4}";
                                        var rkDriver = Registry.LocalMachine.OpenSubKey(
                                                "SYSTEM\\CurrentControlSet\\Control\\Class\\{36FC9E60-C465-11CF-8056-444553540000}\\" + dx);
                                        if (rkDriver != null)
                                        {
                                            var sDriverName = rkDriver.GetValue("DriverBase") as string;
                                            if (sDriverName == null)
                                                sDriverName = rkDriver.GetValue("NTMPDriver") as string;

                                            if (sDriverName != null)
                                            {
                                                var rkDevParams = rkSNum.OpenSubKey("Device Parameters");
                                                if (rkDevParams != null)
                                                {
                                                    var sGuid = rkDevParams.GetValue("DriverGUID") as string;

                                                    var newGuid = GuidFromString(sGuid);

                                                    if (!newGuid.Equals(Guid.Empty))
                                                    {
                                                        var bNew = true;

                                                        foreach (Guid storedGuid in USBDriverGuids)
                                                            bNew &= !newGuid.Equals(storedGuid);

                                                        if (bNew)
                                                            USBDriverGuids.Add(newGuid);
                                                    }

                                                    rkDevParams.Close();
                                                } //rkDevParams != null
                                            }     //sDriverName != null

                                            rkDriver.Close();
                                        } // rkDriver != null

                                        if (!CyConst.Customer_ClassGuid.Equals(0))
                                        {
                                            var rkDriver1 = Registry.LocalMachine.OpenSubKey(
                                                    "SYSTEM\\CurrentControlSet\\Control\\Class\\{" + CyConst.Customer_ClassGuid + "}\\" + dx);

                                            if (rkDriver1 != null)
                                            {
                                                var sDriverName1 = rkDriver1.GetValue("DriverBase") as string;
                                                if (sDriverName1 == null)
                                                    sDriverName1 = rkDriver1.GetValue("NTMPDriver") as string;

                                                if (sDriverName1 != null)
                                                {
                                                    var rkDevParams = rkSNum.OpenSubKey("Device Parameters");
                                                    if (rkDevParams != null)
                                                    {
                                                        var sGuid = rkDevParams.GetValue("DriverGUID") as string;

                                                        var newGuid = GuidFromString(sGuid);

                                                        if (!newGuid.Equals(Guid.Empty))
                                                        {
                                                            var bNew = true;

                                                            foreach (Guid storedGuid in USBDriverGuids)
                                                                bNew &= !newGuid.Equals(storedGuid);

                                                            if (bNew)
                                                                USBDriverGuids.Add(newGuid);
                                                        }

                                                        rkDevParams.Close();
                                                    } //rkDevParams != null
                                                }     //sDriverName1 != null

                                                rkDriver1.Close();
                                            }
                                        }
                                    } //slashPos > 0
                                }     //sDriver != null

                                rkSNum.Close();
                            } //rkSNum != null
                        }     // foreach serNum

                        rkSNums.Close();
                    } //rkSNums != null
                }

                rkDevs.Close();
            }

            // Add the Storage Class guid, if DevMask asks for it
            if ((DevMask & CyConst.DEVICES_MSC) == CyConst.DEVICES_MSC)
                USBDriverGuids.Add(CyConst.StorGuid);

            // Add the HID Class guid, if DevMask asks for it
            if ((DevMask & CyConst.DEVICES_HID) == CyConst.DEVICES_HID)
                USBDriverGuids.Add(HidGuid);

            if ((DevMask & CyConst.DEVICES_CYUSB) == CyConst.DEVICES_CYUSB)
            {
                //Anyway add cyguid
                var bcheck = true;
                foreach (Guid storedGuid in USBDriverGuids)
                    bcheck &= !CyConst.CyGuid.Equals(storedGuid);

                if (bcheck)
                    USBDriverGuids.Add(CyConst.CyGuid);
            }
        }

        private bool RegisterForPnpEvents(IntPtr h, Guid DrvGuid)
        {
            var dFilter = new DEV_BROADCAST_DEVICEINTERFACE();
            dFilter.dbcc_size       = Marshal.SizeOf(dFilter);
            dFilter.dbcc_devicetype = CyConst.DBT_DEVTYP_DEVICEINTERFACE;
            dFilter.dbcc_classguid  = DrvGuid;

            var hNotify = PInvoke.RegisterDeviceNotification(h, dFilter, CyConst.DEVICE_NOTIFY_WINDOW_HANDLE);
            if (hNotify == IntPtr.Zero)
                return false;

            hDevNotifications.Add(hNotify);

            return true;
        }

        // finalizer
        ~USBDeviceList()
        {
            Dispose(false);
        }
    }

    public class USBEventArgs : EventArgs
    {
        public USBDevice Device;
        public ushort    VendorID;
        public ushort    ProductID;
        public string    SerialNum    = "";
        public string    Product      = "";
        public string    Manufacturer = "";
        public string    FriendlyName = "";
    }
}
