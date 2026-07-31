/*
 ## Cypress CyUSB C# library source file (CyUSBStorDevice.cs)
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
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Summary description for CyUSBStorDevice.
    /// </summary>
    public class CyUSBStorDevice : USBDevice
    {
        //internal CyUSBStorDevice(Guid g):base(Guid.Empty)
        internal CyUSBStorDevice(Guid g)
                : base(g)
        {
            BlockSize = 512;
            TimeOut   = 20;
        }

        public int BlockSize { get; }

        public uint TimeOut { get; set; }

        internal override byte DeviceCount
        {
            get
            {
                string dPath;
                uint i;
                byte dCount = 0;

                i = 0;
                do
                {
                    dPath = PInvoke.GetDevicePath(CyConst.DiskGuid, i);
                    if (dPath.IndexOf("\\usbstor#") > -1) dCount++;
                    i++;
                } while (dPath.Length > 0);

                i = 0;
                do
                {
                    dPath = PInvoke.GetDevicePath(CyConst.CdGuid, i);
                    if (dPath.IndexOf("\\usbstor#") > -1) dCount++;
                    i++;
                } while (dPath.Length > 0);

                return dCount;
            }
        }

        public override string ToString()
        {
            if (_alreadyDisposed) throw new ObjectDisposedException("");

            var s = new StringBuilder("<MSC_DEVICE>\r\n");

            s.Append($"\tFriendlyName=\"{FriendlyName}\"\r\n");
            s.Append($"\tManufacturer=\"{Manufacturer}\"\r\n");
            s.Append($"\tProduct=\"{Product}\"\r\n");
            s.Append($"\tSerialNumber=\"{SerialNumber}\"\r\n");
            s.Append($"\tVendorID=\"{Util.byteStr(VendorID)}\"\r\n");
            s.Append($"\tProductID=\"{Util.byteStr(ProductID)}\"\r\n");
            s.Append($"\tClass=\"{_devClass:X2}h\"\r\n");
            s.Append($"\tSubClass=\"{_devSubClass:X2}h\"\r\n");
            s.Append($"\tProtocol=\"{_devProtocol:X2}h\"\r\n");
            s.Append($"\tBcdUSB=\"{Util.byteStr(_bcdUSB)}\"\r\n");

            s.Append("</MSC_DEVICE>\r\n");
            return s.ToString();
        }

        public bool SendScsiCmd(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            if (IntPtr.Size == 8)
                return SendScsiCmd64(cmd, op, lun, dirIn, bank, lba, bytes, data); //64bit process
            return SendScsiCmd32(cmd, op, lun, dirIn, bank, lba, bytes, data);     //32 bit process
        }

        // Opens a handle to the devTH device attached the USBSTOR.SYS driver
        internal override bool Open(byte dev)
        {
            // If this CCyUSBDevice object already has the driver open, close it.
            if (_hDevice != CyConst.INVALID_HANDLE)
                Close();

            _devices = DeviceCount;
            if (_devices == 0) return false;
            if (dev      > _devices - 1) return false;

            _path    = GetDevicePath(dev); // Also sets the DrvGuid to either CdGuid or DiskGuid.
            _hDevice = PInvoke.GetDeviceHandle(_path, true);
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            _devNum = dev;
            GetDeviceParameters();

            _driverName = "usbstor.sys";

            return true;
        }

        private void GetDeviceParameters()
        {
            var a = Path.IndexOf("usbstor");
            var len = Path.IndexOf("#{") - a;
            var tmp = Path.Substring(a, len).Replace("#", "\\");
            var DevKeyString = "SYSTEM\\CurrentControlSet\\Enum\\" + tmp; // Location in XP

            var LocalMachine = Registry.LocalMachine;
            var DevKey = LocalMachine.OpenSubKey(DevKeyString);
            if (DevKey == null)
            {
                DevKeyString = "Enum\\" + tmp; // Location in 98
                DevKey       = LocalMachine.OpenSubKey(DevKeyString);
                if (DevKey == null) return;
            }

            _friendlyName = (string) DevKey.GetValue("FriendlyName");

            var x = tmp.LastIndexOf("\\") + 1;
            len           = tmp.Length;
            _serialNumber = tmp.Substring(x, len - x).ToUpper();

            // Many Serial Numbers have '&0' tacked-on at the end
            x = _serialNumber.LastIndexOf("&");
            if (x > 0)
            {
                len           = _serialNumber.Length;
                _serialNumber = _serialNumber.Substring(0, x);
            }

            var lastAmpersand = _serialNumber.LastIndexOf("&");
            var ParentIDPrefix = lastAmpersand >= 0 ? _serialNumber.Substring(0, lastAmpersand) : "";

            DevKey = GetUSBDevKey(ParentIDPrefix);
            if (DevKey == null) return;

            _manufacturer = (string) DevKey.GetValue("Mfg");
            _name         = (string) DevKey.GetValue("DeviceDesc");
            _product      = (string) DevKey.GetValue("LocationInformation");

            var vidpid = (string[]) DevKey.GetValue("HardwareID");

            x = vidpid[0].LastIndexOf("Vid_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("VID_");
            x = x + 4;
            var sVid = vidpid[0].Substring(x, 4);
            _vendorID = (ushort) Util.HexToInt(sVid);

            x = vidpid[0].LastIndexOf("Pid_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("PID_");
            x = x + 4;
            var sPid = vidpid[0].Substring(x, 4);
            _productID = (ushort) Util.HexToInt(sPid);

            x = vidpid[0].LastIndexOf("Rev_");
            if (x == -1)
                x = vidpid[0].LastIndexOf("REV_");
            x = x + 4;
            var sRev = vidpid[0].Substring(x, 4);
            _bcdUSB = (ushort) Util.HexToInt(sRev);

            var classids = (string[]) DevKey.GetValue("CompatibleIDs");

            x = classids[0].IndexOf("Class_") + 6;
            var sClass = classids[0].Substring(x, 2);
            _devClass = (byte) Util.HexToInt(sClass);

            x = classids[0].IndexOf("SubClass_") + 9;
            var sSub = classids[0].Substring(x, 2);
            _devSubClass = (byte) Util.HexToInt(sSub);

            x = classids[0].IndexOf("Prot_") + 5;
            var sProt = classids[0].Substring(x, 2);
            _devProtocol = (byte) Util.HexToInt(sProt);
        }

        private RegistryKey GetUSBDevKey(string ParentPrefix)
        {
            var LocalMachine = Registry.LocalMachine;
            var DevKey = LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USB");
            if (DevKey == null)
            {
                //DevKey = LocalMachine.OpenSubKey("Enum\\USB"); // Location in 98
                //if (DevKey == null) return null;
            }

            var usbKeys = DevKey.SubKeyCount;
            var sUKeys = DevKey.GetSubKeyNames();

            for (var i = 0; i < usbKeys; i++)
            {
                RegistryKey usbKey = null;
                try
                {
                    usbKey = DevKey.OpenSubKey(sUKeys[i]);
                }
                catch (SecurityException) { }

                var dKeys = usbKey.SubKeyCount;
                var sDKeys = usbKey.GetSubKeyNames();

                for (var j = 0; j < dKeys; j++)
                {
                    RegistryKey snKey = null;
                    try
                    {
                        snKey = usbKey.OpenSubKey(SerialNumber); // The Serial # key
                    }
                    catch (SecurityException) { }

                    if (snKey != null)
                        return snKey;

                    // Not a real Serial Number - Try to match ParentPrefix string to ParentIdPrefix value
                    RegistryKey itemKey = null;
                    try
                    {
                        itemKey = usbKey.OpenSubKey(sDKeys[j]);
                    }
                    catch (SecurityException) { }

                    var tmp = (string) itemKey.GetValue("ParentIdPrefix");
                    if (tmp != null)
                        if (ParentPrefix.Equals(tmp.ToUpper()))
                        {
                            _serialNumber = "";
                            return itemKey;
                        }
                }
            }

            return null;
        }

        private string GetDevicePath(byte dev)
        {
            string dPath;
            byte dCnt = 0;

            //string s = "USBSTOR";
            //byte[] sClass = new byte[s.Length+1];
            //Encoding.ASCII.GetBytes(s,0,s.Length,sClass,0);

            //dPath = PInvoke.GetDevicePath(Guid.Empty,0,sClass);

            // First look for the devTH USBSTOR Disk
            byte i = 0;
            do
            {
                dPath = PInvoke.GetDevicePath(CyConst.DiskGuid, i);

                if (dPath.IndexOf("\\usbstor#") > -1)
                {
                    if (dev == dCnt)
                    {
                        _drvGuid = CyConst.DiskGuid;
                        return dPath;
                    }

                    dCnt++;
                }

                i++;
            } while (dPath.Length > 0);

            // Next look for the devTH USBSTOR CD
            i = 0;
            do
            {
                dPath = PInvoke.GetDevicePath(CyConst.CdGuid, i);

                if (dPath.IndexOf("\\usbstor#") > -1)
                {
                    if (dev == dCnt)
                    {
                        _drvGuid = CyConst.CdGuid;
                        return dPath;
                    }

                    dCnt++;
                }

                i++;
            } while (dPath.Length > 0);

            return "";
        }

        private unsafe bool SendScsiCmd64(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            var sptB = new SCSI_PASS_THROUGH_WITH_BUFFERS();
            var len = Marshal.SizeOf(sptB) + bytes; // total size of buffer
            var buffer = new byte[len];

            fixed (byte * buf = buffer)
            {
                var sptBuf = (SCSI_PASS_THROUGH_WITH_BUFFERS *) buf;
                var spt = (SCSI_PASS_THROUGH *) buf;
                var cdb = (CDB10 *) &spt->Cdb;
                bool bRetVal;

                sptBuf->totalSize = (uint) len;

                spt->Length             = (ushort) Marshal.SizeOf(*spt);
                spt->Lun                = lun;
                spt->CdbLength          = 10;
                spt->SenseInfoLength    = 18;
                spt->DataIn             = dirIn;
                spt->DataTransferLength = (uint) bytes;
                spt->TimeOutValue       = TimeOut;
                spt->SenseInfoOffset    = (uint) Marshal.SizeOf(*spt) + 4;
                spt->DataBufferOffset   = (uint) Marshal.SizeOf(sptB);

                cdb->Cmd    = cmd;
                cdb->OpCode = op;
                cdb->LBA    = (uint) lba;
                cdb->Bank   = (byte) bank;
                Util.ReverseBytes((byte *) &cdb->LBA, 4);

                cdb->Blocks = (ushort) (bytes / BlockSize);
                Util.ReverseBytes((byte *) &cdb->Blocks, 2);

                if (dirIn == 0 && data != null)
                    Marshal.Copy(data, 0, (IntPtr) (buf + Marshal.SizeOf(sptB)), bytes);

                var BytesXfered = new int[1];
                BytesXfered[0] = 0;
                fixed (byte * lpInBuffer = buffer)
                {
                    fixed (byte * lpOutBuffer = buffer)
                    {
                        fixed (int * lpBytesXferred = BytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice,
                                                              CyConst.IOCTL_SCSI_PASS_THROUGH,
                                                              (IntPtr) lpInBuffer,
                                                              len,
                                                              (IntPtr) lpOutBuffer,
                                                              len,
                                                              (IntPtr) lpBytesXferred,
                                                              (IntPtr) null);
                        }
                    }
                }

                var error = Marshal.GetLastWin32Error();

                if (dirIn == 1)
                    Marshal.Copy((IntPtr) (buf + Marshal.SizeOf(sptB)), data, 0, bytes);

                return bRetVal;
            }
        }

        private unsafe bool SendScsiCmd32(byte cmd, byte op, byte lun, byte dirIn, int bank, int lba, int bytes, byte[] data)
        {
            var sptB = new SCSI_PASS_THROUGH_WITH_BUFFERS32();
            var len = Marshal.SizeOf(sptB) + bytes; // total size of buffer
            var buffer = new byte[len];

            fixed (byte * buf = buffer)
            {
                var sptBuf = (SCSI_PASS_THROUGH_WITH_BUFFERS32 *) buf;
                var spt = (SCSI_PASS_THROUGH32 *) buf;
                var cdb = (CDB10 *) &spt->Cdb;
                bool bRetVal;

                sptBuf->totalSize = (uint) len;

                spt->Length             = (ushort) Marshal.SizeOf(*spt);
                spt->Lun                = lun;
                spt->CdbLength          = 10;
                spt->SenseInfoLength    = 18;
                spt->DataIn             = dirIn;
                spt->DataTransferLength = (uint) bytes;
                spt->TimeOutValue       = TimeOut;
                spt->SenseInfoOffset    = (uint) Marshal.SizeOf(*spt) + 4;
                spt->DataBufferOffset   = (uint) Marshal.SizeOf(sptB);

                cdb->Cmd    = cmd;
                cdb->OpCode = op;
                cdb->LBA    = (uint) lba;
                cdb->Bank   = (byte) bank;
                Util.ReverseBytes((byte *) &cdb->LBA, 4);

                cdb->Blocks = (ushort) (bytes / BlockSize);
                Util.ReverseBytes((byte *) &cdb->Blocks, 2);

                if (dirIn == 0 && data != null)
                    Marshal.Copy(data, 0, (IntPtr) (buf + Marshal.SizeOf(sptB)), bytes);

                var BytesXfered = new int[1];
                BytesXfered[0] = 0;
                fixed (byte * lpInBuffer = buffer)
                {
                    fixed (byte * lpOutBuffer = buffer)
                    {
                        fixed (int * lpBytesXferred = BytesXfered)
                        {
                            bRetVal = PInvoke.DeviceIoControl(_hDevice,
                                                              CyConst.IOCTL_SCSI_PASS_THROUGH,
                                                              (IntPtr) lpInBuffer,
                                                              len,
                                                              (IntPtr) lpOutBuffer,
                                                              len,
                                                              (IntPtr) lpBytesXferred,
                                                              (IntPtr) null);
                        }
                    }
                }

                var error = Marshal.GetLastWin32Error();

                if (dirIn == 1)
                    Marshal.Copy((IntPtr) (buf + Marshal.SizeOf(sptB)), data, 0, bytes);

                return bRetVal;
            }
        }
    }
}