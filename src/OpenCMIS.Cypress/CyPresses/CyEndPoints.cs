/*
 ## Cypress CyUSB C# library source file (CyEndPoints.cs)
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
using System.Text;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     The CyUSBEndPoint ABSTRACT Class
    /// </summary>
    public abstract class CyUSBEndPoint
    {
        protected byte _address;

        protected byte _attributes;

        protected bool _bIn;

        protected uint _bytesWritten;

        // The fields of an EndPoint Descriptor
        protected byte _dscLen;

        protected byte   _dscType;
        protected IntPtr _hDevice;

        protected byte _interval;

        protected uint _lastError;

        protected int _maxPktSize;

        protected uint _ntStatus;

        protected byte _ssbmAttribute; // store endpoint attribute like for bulk it will be number of streams

        protected ushort _ssbytesperinterval;

        // Super speed endpoint companion
        protected byte _ssdscLen;

        protected byte _ssdscType;

        protected byte _ssmaxburst; /* Maximum number of packets endpoint can send in one burst*/

        // Other fields
        protected uint _timeOut = 10000; // 10 Sec timeout is default;

        protected uint _usbdStatus;

        // Will be XMODE.DIRECT if the driver is version 1.05.0500 or later.
        public XMODE _xferMode = XMODE.DIRECT;

        protected OVERLAPPED Ovlap;

        // Constructor
        internal unsafe CyUSBEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor)
        {
            _hDevice = h;

            if (EndPtDescriptor != (USB_ENDPOINT_DESCRIPTOR *) 0)
            {
                var pkts = (EndPtDescriptor->wMaxPacketSize & 0x1800) >> 11;
                pkts++;

                _dscLen     = EndPtDescriptor->bLength;
                _dscType    = EndPtDescriptor->bDescriptorType;
                _address    = EndPtDescriptor->bEndpointAddress;
                _attributes = EndPtDescriptor->bmAttributes;
                _maxPktSize = (EndPtDescriptor->wMaxPacketSize & 0x7ff) * pkts;
                _interval   = EndPtDescriptor->bInterval;
                _bIn        = (Address & 0x80) > 0;
            }

            // initialize all SS paramter to zero
            _ssdscLen           = 0;
            _ssdscType          = 0;
            _ssmaxburst         = 0;
            _ssbmAttribute      = 0;
            _ssbytesperinterval = 0;
        }

        internal unsafe CyUSBEndPoint(IntPtr h,
                                      USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor,
                                      USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR * SSEndPtDescriptor)
        {
            _hDevice = h;

            if (EndPtDescriptor != (USB_ENDPOINT_DESCRIPTOR *) 0)
            {
                var pkts = (EndPtDescriptor->wMaxPacketSize & 0x1800) >> 11;
                pkts++;

                _dscLen     = EndPtDescriptor->bLength;
                _dscType    = EndPtDescriptor->bDescriptorType;
                _address    = EndPtDescriptor->bEndpointAddress;
                _attributes = EndPtDescriptor->bmAttributes;
                _maxPktSize = (EndPtDescriptor->wMaxPacketSize & 0x7ff) * pkts;
                _interval   = EndPtDescriptor->bInterval;
                _bIn        = (Address & 0x80) > 0;
            }

            if (SSEndPtDescriptor != (USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR *) 0)
            {
                _ssdscLen      =  SSEndPtDescriptor->bLength;
                _ssdscType     =  SSEndPtDescriptor->bDescriptorType;
                _ssmaxburst    =  SSEndPtDescriptor->bMaxBurst;
                _maxPktSize    *= _ssmaxburst + 1; // Multiply the Maxpacket size with Max burst
                _ssbmAttribute =  SSEndPtDescriptor->bmAttributes;
                if ((_attributes & 0x03) == 1)                                   // MULT is valid for Isochronous transfer only
                    _maxPktSize *= (SSEndPtDescriptor->bmAttributes & 0x03) + 1; // Adding the MULT fields.

                _ssbytesperinterval = SSEndPtDescriptor->bBytesPerInterval;
            }
        }

        public IntPtr hDevice           => _hDevice;
        public byte   DscLen            => _dscLen;
        public byte   DscType           => _dscType;
        public byte   Address           => _address;
        public byte   Attributes        => _attributes;
        public int    MaxPktSize        => _maxPktSize;
        public byte   Interval          => _interval;
        public byte   SSDscLen          => _ssdscLen;
        public byte   SSDscType         => _ssdscType;
        public byte   SSMaxBurst        => _ssmaxburst;
        public byte   SSBmAttribute     => _ssbmAttribute;
        public ushort SSBytePerInterval => _ssbytesperinterval;

        public uint TimeOut
        {
            get => _timeOut;
            set => _timeOut = value;
        }

        public uint UsbdStatus             => _usbdStatus;
        public uint NtStatus               => _ntStatus;
        public uint BytesWritten           => _bytesWritten;
        public uint LastError              => _lastError;
        public bool bIn                    => _bIn;
        public int  OverlapSignalAllocSize => Marshal.SizeOf(Ovlap);

        public XMODE XferMode
        {
            get => _xferMode;
            set => _xferMode = value;
        }

        public TreeNode Tree
        {
            get
            {
                var sType = Attributes switch
                            {
                                    1 => "Isoc",
                                    3 => "Interrupt",
                                    _ => "Bulk"
                            };

                var sIn = bIn ? "in" : "out";

                var tmp = $"{sType} {sIn} endpoint (0x{Address:X2})";
                var t = new TreeNode(tmp)
                {
                        Tag = this
                };

                return t;
            }
        }

        // Implemented as a property
        public unsafe int XferSize
        {
            get
            {
                if (_hDevice == CyConst.INVALID_HANDLE) return 0;

                var IdleXferVar = new SET_TRANSFER_SIZE_INFO();

                // The size of SET_TRANSFER_SIZE_INFO
                var len = Marshal.SizeOf(IdleXferVar);

                var buffer = new byte[len];

                bool bRetVal;

                fixed (byte * buf = buffer)
                {
                    var SetTransferInfo = (SET_TRANSFER_SIZE_INFO *) buf;
                    SetTransferInfo->EndpointAddress = Address;

                    var Xferred = new int[1];
                    Xferred[0] = 0;

                    fixed (byte * lpInBuffer = buffer)
                    {
                        fixed (byte * lpOutBuffer = buffer)
                        {
                            fixed (int * lpBytesXfered = Xferred)
                            {
                                bRetVal = PInvoke.DeviceIoControl(_hDevice,
                                                                  CyConst.IOCTL_ADAPT_GET_TRANSFER_SIZE,
                                                                  (IntPtr) lpInBuffer,
                                                                  len,
                                                                  (IntPtr) lpOutBuffer,
                                                                  len,
                                                                  (IntPtr) lpBytesXfered,
                                                                  (IntPtr) null);
                            }
                        }
                    }

                    if (bRetVal && Xferred[0] >= len)
                        return SetTransferInfo->TransferSize;
                }

                return 0;
            }

            set
            {
                if (_hDevice == CyConst.INVALID_HANDLE) return;

                if (MaxPktSize == 0)
                    return;
                // Force a multiple of MaxPktSize
                var pkts = value % MaxPktSize > 0 ? 1 + value / MaxPktSize : value / MaxPktSize;
                var xferSize = pkts * MaxPktSize;

                var len = 5; // The size of SET_TRANSFER_SIZE_INFO
                var buffer = new byte[len];

                fixed (byte * buf = buffer)
                {
                    var SetTransferInfo = (SET_TRANSFER_SIZE_INFO *) buf;
                    SetTransferInfo->EndpointAddress = Address;
                    SetTransferInfo->TransferSize    = xferSize;

                    var Xferred = new int[1];
                    Xferred[0] = 0;

                    fixed (byte * lpInBuffer = buffer)
                    {
                        fixed (byte * lpOutBuffer = buffer)
                        {
                            fixed (int * lpBytesXfered = Xferred)
                            {
                                PInvoke.DeviceIoControl(_hDevice,
                                                        CyConst.IOCTL_ADAPT_SET_TRANSFER_SIZE,
                                                        (IntPtr) lpInBuffer,
                                                        len,
                                                        (IntPtr) lpOutBuffer,
                                                        len,
                                                        (IntPtr) lpBytesXfered,
                                                        (IntPtr) null);
                            }
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            var sType = Attributes switch
                        {
                                0 => "CONTROL",
                                1 => "ISOC",
                                3 => "INTERRUPT",
                                _ => "BULK"
                        };

            var sIn = bIn ? "IN" : "OUT";
            if (Attributes == 0) sIn = "BIDI";

            var s = new StringBuilder("\t\t\t<ENDPOINT>\r\n");

            s.Append($"\t\t\t\tType=\"{sType}\"\r\n");
            s.Append($"\t\t\t\tDirection=\"{sIn}\"\r\n");
            s.Append($"\t\t\t\tAddress=\"{Address:X2}h\"\r\n");
            s.Append($"\t\t\t\tAttributes=\"{Attributes:X2}h\"\r\n");
            s.Append($"\t\t\t\tMaxPktSize=\"{MaxPktSize}\"\r\n");
            s.Append($"\t\t\t\tDescriptorType=\"{DscType}\"\r\n");
            s.Append($"\t\t\t\tDescriptorLength=\"{DscLen}\"\r\n");
            s.Append($"\t\t\t\tInterval=\"{Interval}\"\r\n");

            if (_ssdscLen != 0)
            {
                // USB3.0 super speed device endpoint
                var sSSType = "SUPERSPEED_USB_ENDPOINT_COMPANION";

                s.Append("\t\t\t<SUPER SPEED ENDPOINT COMPANION>\r\n");
                s.Append($"\t\t\t\tType=\"{sSSType}\"\r\n");
                s.Append($"\t\t\t\tMaxBurst=\"{SSMaxBurst}\"\r\n");
                s.Append($"\t\t\t\tAttributes=\"{SSBmAttribute:X2}h\"\r\n");
                s.Append($"\t\t\t\tBytesPerInterval=\"{SSBytePerInterval:X2}h\"\r\n");
            }

            s.Append("\t\t\t</ENDPOINT>\r\n");
            return s.ToString();
        }

        public bool XferData(ref byte[] buf, ref int len, bool PacketMode)
        {
            if (!bIn || !PacketMode)
                return XferData(ref buf, ref len);
            var size = 0;
            var xferLen = MaxPktSize;
            var status = true;
            var bufPtr = new byte[MaxPktSize];

            while (status && size < len)
            {
                if (len - size < MaxPktSize)
                    xferLen = len - size;

                status = XferData(ref bufPtr, ref xferLen);
                if (status)
                {
                    for (var i = 0; i < xferLen; ++i) buf[size + i] = bufPtr[i];
                    size += xferLen;

                    if (xferLen < MaxPktSize)
                        break;
                }
            }

            len = size;
            return len > 0 || status;
        }

        // These used by both BULK and INTERRUPT endpoints
        public virtual unsafe bool XferData(ref byte[] buf, ref int len)
        {
            var ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte * fixedOvLap = ovLap)
            {
                var ovLapStatus = (OVERLAPPED *) fixedOvLap;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                var bufSz = CyConst.SINGLE_XFER_LEN + (XferMode == XMODE.DIRECT ? 0 : len);
                var cmdBuf = new byte[bufSz];

                // These nested fixed blocks ensure that the buffers don't move in memory
                // While we're doing the asynchronous IO - Begin/Wait/Finish
                fixed (byte * tmp1 = cmdBuf, tmp2 = buf)
                {
                    var bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    //
                    //  This waits for driver to call IoRequestComplete on the IRP
                    //  we just sent.
                    //
                    var wResult = WaitForIO(ovLapStatus->hEvent);
                    var fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }
        }

        public virtual unsafe bool BeginDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            var cmdLen = singleXfer.Length;

            fixed (byte * buf = singleXfer)
            {
                var transfer = (SINGLE_TRANSFER *) buf;
                transfer->WaitForever       = 0;
                transfer->ucEndpointAddress = Address;
                transfer->IsoPacketLength   = 0;

                var Xferred = new int[1];
                Xferred[0] = 0;
                uint IOCTL;

                if (XferMode == XMODE.BUFFERED)
                {
                    transfer->BufferOffset = CyConst.SINGLE_XFER_LEN;
                    transfer->BufferLength = (uint) len;
                    IOCTL                  = CyConst.IOCTL_ADAPT_SEND_NON_EP0_TRANSFER;

                    for (var i = 0; i < len; i++) buf[CyConst.SINGLE_XFER_LEN + i] = buffer[i];

                    fixed (byte * lpInBuffer = singleXfer)
                    {
                        fixed (byte * lpOutBuffer = singleXfer)
                        {
                            fixed (byte * lpOvLap = ov)
                            {
                                fixed (int * lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice,
                                                            IOCTL,
                                                            (IntPtr) lpInBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpOutBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpBytesXfered,
                                                            (IntPtr) lpOvLap);
                                }
                            }
                        }
                    }
                }
                else
                {
                    transfer->BufferOffset = 0;
                    transfer->BufferLength = 0;
                    IOCTL                  = CyConst.IOCTL_ADAPT_SEND_NON_EP0_DIRECT;

                    fixed (byte * lpInBuffer = singleXfer)
                    {
                        fixed (byte * lpOutBuffer = buffer)
                        {
                            fixed (byte * lpOvLap = ov)
                            {
                                fixed (int * lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice,
                                                            IOCTL,
                                                            (IntPtr) lpInBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpOutBuffer,
                                                            len,
                                                            (IntPtr) lpBytesXfered,
                                                            (IntPtr) lpOvLap);
                                }
                            }
                        }
                    }
                }

                if (Xferred[0] > 0)
                    len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus   = transfer->NtStatus;
            }

            _lastError = (uint) Marshal.GetLastWin32Error();

            return true;
        }

        public virtual unsafe bool FinishDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            bool rResult;
            var bytes = new uint[1];

            // DWY fix single variable during call by converting it into an 'array of 1'
            //  and fixing it to a pointer.
            fixed (uint * buf0 = bytes)
            {
                fixed (byte * buf = singleXfer)
                {
                    var transfer = (SINGLE_TRANSFER *) buf;
                    rResult = PInvoke.GetOverlappedResult(_hDevice, ov, ref bytes[0], 0);
                    if (!rResult) transfer->NtStatus = PInvoke.GetLastError();
                }
            }

            len = (int) bytes[0];

            fixed (byte * buf = singleXfer)
            {
                var transfer = (SINGLE_TRANSFER *) buf;
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus   = transfer->NtStatus;

                if (XferMode == XMODE.BUFFERED && len > 0)
                        //len -= (int)transfer->BufferOffset; This is not required becuse we pass the actual data buffer length
                    for (var i = 0; i < len; i++)
                        buffer[i] = buf[transfer->BufferOffset + i];
            }

            _bytesWritten = (uint) len;

            return rResult && _usbdStatus == 0 && _ntStatus == 0;
        }

        public bool WaitForXfer(IntPtr ovlapEvent, uint tOut)
        {
            var waitResult = PInvoke.WaitForSingleObject(ovlapEvent, tOut);
            if (waitResult == CyConst.WAIT_OBJECT_0) return true;
            return false;
        }

        public bool Reset()
        {
            return UnsafeReset();
        }

        public bool Abort()
        {
            return UnsafeAbort();
        }

        internal bool WaitForIO(IntPtr ovlapEvent)
        {
            //_lastError = (ushort)Marshal.GetLastWin32Error();

            //if (_lastError == CyConst.ERROR_SUCCESS) return true;  // The command completed

            //if (_lastError == CyConst.ERROR_IO_PENDING)
            {
                var waitResult = PInvoke.WaitForSingleObject(ovlapEvent, TimeOut);

                if (waitResult == CyConst.WAIT_OBJECT_0) return true;

                if (waitResult == CyConst.WAIT_TIMEOUT)
                {
                    Abort();
                    // Wait for the stalled command to complete - should be done already
                    PInvoke.WaitForSingleObject(ovlapEvent, CyConst.INFINITE);
                }
            }

            return false;
        }

        private unsafe bool UnsafeReset()
        {
            var dwBytes = new int[1];
            dwBytes[0] = 0;
            var buffer = new byte[1];
            buffer[0] = Address;

            fixed (byte * lpInBuffer = buffer)
            {
                fixed (int * lpBytesXfered = dwBytes)
                {
                    return PInvoke.DeviceIoControl(_hDevice,
                                                   CyConst.IOCTL_ADAPT_RESET_PIPE,
                                                   (IntPtr) lpInBuffer,
                                                   1,
                                                   (IntPtr) null,
                                                   0,
                                                   (IntPtr) lpBytesXfered,
                                                   (IntPtr) null);
                }
            }
        }

        private unsafe bool UnsafeAbort()
        {
            var dwBytes = new int[1];
            dwBytes[0] = 0;
            var buffer = new byte[1];
            buffer[0] = Address;

            fixed (byte * lpInBuffer = buffer)
            {
                fixed (int * lpBytesXfered = dwBytes)
                {
                    return PInvoke.DeviceIoControl(_hDevice,
                                                   CyConst.IOCTL_ADAPT_ABORT_PIPE,
                                                   (IntPtr) lpInBuffer,
                                                   1,
                                                   (IntPtr) null,
                                                   0,
                                                   (IntPtr) lpBytesXfered,
                                                   (IntPtr) null);
                }
            }
        }
    }

    /// <summary>
    ///     The Control Endpoint Class
    /// </summary>
    public class CyControlEndPoint : CyUSBEndPoint
    {
        private byte _Direction = CyConst.DIR_TO_DEVICE;

        internal unsafe CyControlEndPoint(IntPtr h, int MaxPacketSize)
                : base(h, null)
        {
            _bIn        = false;
            _dscLen     = 7;
            _dscType    = 5;
            _maxPktSize = MaxPacketSize;
        }

        public byte Target { get; set; } = CyConst.TGT_DEVICE;

        public byte ReqType { get; set; } = CyConst.REQ_VENDOR;

        public byte Direction
        {
            get => _Direction;
            set
            {
                _Direction = value;
                _bIn       = _Direction == CyConst.DIR_FROM_DEVICE;
            }
        }

        public byte ReqCode { get; set; }

        public ushort Value { get; set; }

        public ushort Index { get; set; }

        public bool Read(ref byte[] buf, ref int len)
        {
            Direction = CyConst.DIR_FROM_DEVICE;
            return XferData(ref buf, ref len);
        }

        public bool Write(ref byte[] buf, ref int len)
        {
            Direction = CyConst.DIR_TO_DEVICE;
            return XferData(ref buf, ref len);
        }

        // Control endpoint uses the BUFFERED xfer method.  So that it
        // doesn't collide with the base class' XferData, we declare it 'new'
        public new unsafe bool XferData(ref byte[] buf, ref int len)
        {
            var ovLap = new byte[sizeof(OVERLAPPED)];

            fixed (byte * tmp0 = ovLap)
            {
                var ovLapStatus = (OVERLAPPED *) tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                bool bResult, wResult, fResult;

                // Create a temporary buffer that will contain a SINGLE_TRANSFER structure
                // followed by the actual data.
                var tmpBuf = new byte[CyConst.SINGLE_XFER_LEN + len];
                for (var i = 0; i < len; i++)
                    tmpBuf[CyConst.SINGLE_XFER_LEN + i] = buf[i];

                var bufSingleTransfer = GCHandle.Alloc(tmpBuf, GCHandleType.Pinned);
                var bufDataAllocation = GCHandle.Alloc(buf,    GCHandleType.Pinned);

                fixed (int * lenTemp = &len)
                {
                    bResult = BeginDataXfer(ref tmpBuf, ref *lenTemp, ref ovLap);
                    wResult = WaitForIO(ovLapStatus->hEvent);
                    fResult = FinishDataXfer(ref buf, ref tmpBuf, ref *lenTemp, ref ovLap);
                }

                PInvoke.CloseHandle(ovLapStatus->hEvent);
                bufSingleTransfer.Free();
                bufDataAllocation.Free();

                return wResult && fResult;
            }
        }

        // Control Endpoints don't support the Begin/Wait/Finish advanced technique from the
        // app level.  So, these methods are declared private.
        private unsafe bool BeginDataXfer(ref byte[] buffer, ref int len, ref byte[] ov)
        {
            var bRetVal = false;

            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            var tmo = TimeOut > 0 && TimeOut < 1000 ? 1 : TimeOut / 1000;

            _bIn = Direction == CyConst.DIR_FROM_DEVICE;

            var bufSz = len + CyConst.SINGLE_XFER_LEN;

            fixed (byte * buf = buffer)
            {
                var transfer = (SINGLE_TRANSFER *) buf;
                transfer->SetupPacket.bmRequest = (byte) (Target | ReqType | Direction);
                transfer->SetupPacket.bRequest  = ReqCode;
                transfer->SetupPacket.wValue    = Value;
                transfer->SetupPacket.wLength   = (ushort) len;
                transfer->SetupPacket.wIndex    = Index;
                transfer->SetupPacket.dwTimeOut = tmo;
                transfer->WaitForever           = 0;
                transfer->ucEndpointAddress     = 0x00; // control pipe
                transfer->IsoPacketLength       = 0;
                transfer->BufferOffset          = CyConst.SINGLE_XFER_LEN; // size of the SINGLE_TRANSFER part
                transfer->BufferLength          = (uint) len;

                var Xferred = new int[1];
                Xferred[0] = 0;

                fixed (byte * lpInBuffer = buffer)
                {
                    fixed (byte * lpOutBuffer = buffer)
                    {
                        fixed (byte * lpOv = ov)
                        {
                            fixed (int * lpBytesXfered = Xferred)
                            {
                                bRetVal = PInvoke.DeviceIoControl(_hDevice,
                                                                  CyConst.IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER,
                                                                  (IntPtr) lpInBuffer,
                                                                  bufSz,
                                                                  (IntPtr) lpOutBuffer,
                                                                  bufSz,
                                                                  (IntPtr) lpBytesXfered,
                                                                  (IntPtr) lpOv);
                            }
                        }
                    }
                }

                len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus   = transfer->NtStatus;
            }

            _lastError = (uint) Marshal.GetLastWin32Error();

            return bRetVal;
        }

        // This is a BUFFERED xfer method - specific version of FinishDataXfer.  So that it
        // doesn't collide with the base class' FinishDataXfer, we declare it 'new'
        private new unsafe bool FinishDataXfer(ref byte[] userBuf, ref byte[] xferBuf, ref int len, ref byte[] ov)
        {
            uint bytes = 0;
            var rResult = PInvoke.GetOverlappedResult(_hDevice, ov, ref bytes, 0);

            uint dataOffset;

            fixed (byte * buf = xferBuf)
            {
                var transfer = (SINGLE_TRANSFER *) buf;

                len           = bytes > CyConst.SINGLE_XFER_LEN ? (int) bytes - (int) transfer->BufferOffset : 0;
                _bytesWritten = (uint) len;

                dataOffset  = transfer->BufferOffset;
                _usbdStatus = transfer->UsbdStatus;
                _ntStatus   = transfer->NtStatus;
            }

            // Extract the acquired data and move from xferBuf to userBuf
            if (bIn)
                for (var i = 0; i < len; i++)
                    userBuf[i] = xferBuf[dataOffset + i];

            return rResult && _usbdStatus == 0 && _ntStatus == 0;
        }
    }

    /// <summary>
    ///     The Isoc Endpoint Class
    /// </summary>
    public class CyIsocEndPoint : CyUSBEndPoint
    {
        internal unsafe CyIsocEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor)
                :
                base(h, EndPtDescriptor) { }

        internal unsafe CyIsocEndPoint(IntPtr h,
                                       USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor,
                                       USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR * SSEndPtDescriptor)
                : base(
                        h,
                        EndPtDescriptor,
                        SSEndPtDescriptor) { }

        public int GetPktBlockSize(int len)
        {
            if (MaxPktSize == 0)
                return 0;

            var pkts = len / MaxPktSize;
            if (len % MaxPktSize > 0) pkts++;
            if (pkts             == 0) return 0;

            var p = new ISO_PKT_INFO();

            var blkSize = pkts * Marshal.SizeOf(p);
            return blkSize;
        }

        public int GetPktCount(int len)
        {
            if (MaxPktSize == 0)
                return 0;

            var pkts = len / MaxPktSize;
            if (len % MaxPktSize > 0) pkts++;
            return pkts;
        }

        // Call this one if you don't care about the PacketInfo information
        public override unsafe bool XferData(ref byte[] buf, ref int len)
        {
            var ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte * tmp0 = ovLap)
            {
                var ovLapStatus = (OVERLAPPED *) tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                var bufSz = CyConst.SINGLE_XFER_LEN + GetPktBlockSize(len) + (XferMode == XMODE.DIRECT ? 0 : len);
                var cmdBuf = new byte[bufSz];

                fixed (byte * tmp1 = cmdBuf, tmp2 = buf)
                {
                    var bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    var wResult = WaitForIO(ovLapStatus->hEvent);
                    var fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }
        }

        // Call this one if you want the PacketInfo data passed-back
        public unsafe bool XferData(ref byte[] buf, ref int len, ref ISO_PKT_INFO[] pktInfos)
        {
            var ovLap = new byte[OverlapSignalAllocSize];

            fixed (byte * tmp0 = ovLap)
            {
                var ovLapStatus = (OVERLAPPED *) tmp0;
                ovLapStatus->hEvent = PInvoke.CreateEvent(0, 0, 0, 0);

                // This SINGLE_TRANSFER buffer must be allocated at this level.
                var bufSz = CyConst.SINGLE_XFER_LEN + GetPktBlockSize(len) + (XferMode == XMODE.DIRECT ? 0 : len);
                var cmdBuf = new byte[bufSz];

                fixed (byte * tmp1 = cmdBuf, tmp2 = buf)
                {
                    var bResult = BeginDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap);
                    var wResult = WaitForIO(ovLapStatus->hEvent);
                    var fResult = FinishDataXfer(ref cmdBuf, ref buf, ref len, ref ovLap, ref pktInfos);

                    PInvoke.CloseHandle(ovLapStatus->hEvent);

                    return wResult && fResult;
                }
            }
        }

        public override unsafe bool BeginDataXfer(ref byte[] singleXfer, ref byte[] buffer, ref int len, ref byte[] ov)
        {
            if (_hDevice == CyConst.INVALID_HANDLE) return false;

            var pktBlockSize = GetPktBlockSize(len);
            var cmdLen = singleXfer.Length;

            fixed (byte * buf = singleXfer)
            {
                var transfer = (SINGLE_TRANSFER *) buf;
                transfer->WaitForever       = 0;
                transfer->ucEndpointAddress = Address;
                transfer->IsoPacketOffset   = CyConst.SINGLE_XFER_LEN;
                transfer->IsoPacketLength   = (uint) pktBlockSize;

                var Xferred = new int[1];
                Xferred[0] = 0;

                uint IOCTL;

                if (XferMode == XMODE.BUFFERED)
                {
                    transfer->BufferOffset = (uint) (CyConst.SINGLE_XFER_LEN + pktBlockSize);
                    transfer->BufferLength = (uint) len;

                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_TRANSFER;

                    for (var i = 0; i < len; i++) buf[CyConst.SINGLE_XFER_LEN + pktBlockSize + i] = buffer[i];

                    fixed (byte * lpInBuffer = singleXfer)
                    {
                        fixed (byte * lpOutBuffer = singleXfer)
                        {
                            fixed (byte * lpOv = ov)
                            {
                                fixed (int * lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice,
                                                            IOCTL,
                                                            (IntPtr) lpInBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpOutBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpBytesXfered,
                                                            (IntPtr) lpOv);
                                }
                            }
                        }
                    }
                }
                else
                {
                    transfer->BufferOffset = 0;
                    transfer->BufferLength = 0;

                    IOCTL = CyConst.IOCTL_ADAPT_SEND_NON_EP0_DIRECT;

                    fixed (byte * lpInBuffer = singleXfer)
                    {
                        fixed (byte * lpOutBuffer = buffer)
                        {
                            fixed (byte * lpOv = ov)
                            {
                                fixed (int * lpBytesXfered = Xferred)
                                {
                                    PInvoke.DeviceIoControl(_hDevice,
                                                            IOCTL,
                                                            (IntPtr) lpInBuffer,
                                                            cmdLen,
                                                            (IntPtr) lpOutBuffer,
                                                            len,
                                                            (IntPtr) lpBytesXfered,
                                                            (IntPtr) lpOv);
                                }
                            }
                        }
                    }
                }

                if (Xferred[0] > 0)
                    len = Xferred[0];

                _usbdStatus = transfer->UsbdStatus;
                _ntStatus   = transfer->NtStatus;
            }

            _lastError = (uint) Marshal.GetLastWin32Error();

            return true;
        }

        // This FinishDataXfer is only called by the second XferData method of CyIsocEndPoint
        // This called when ISO_PKT_INFO data is requested
        public virtual unsafe bool FinishDataXfer(ref byte[] singleXfer,
                                                  ref byte[] buffer,
                                                  ref int len,
                                                  ref byte[] ov,
                                                  ref ISO_PKT_INFO[] pktInfo)
        {
            // Call the base class' FinishDataXfer to do most of the work
            var rResult = FinishDataXfer(ref singleXfer, ref buffer, ref len, ref ov);

            // Pass-back the Isoc packet info records
            if (len > 0)
                fixed (byte * buf = singleXfer)
                {
                    var transfer = (SINGLE_TRANSFER *) buf;
                    var packets = (ISO_PKT_INFO *) (buf + transfer->IsoPacketOffset);

                    var pktCnt = (int) transfer->IsoPacketLength / Marshal.SizeOf(*packets);

                    for (var i = 0; i < pktCnt; i++)
                        pktInfo[i] = packets[i];
                }

            return rResult;
        }
    }

    /// <summary>
    ///     The Bulk Endpoint Class
    /// </summary>
    public class CyBulkEndPoint : CyUSBEndPoint
    {
        internal unsafe CyBulkEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor)
                :
                base(h, EndPtDescriptor) { }

        internal unsafe CyBulkEndPoint(IntPtr h,
                                       USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor,
                                       USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR * SSEndPtDescriptor)
                : base(
                        h,
                        EndPtDescriptor,
                        SSEndPtDescriptor) { }
    }

    /// <summary>
    ///     The Interrupt Endpoint Class
    /// </summary>
    public class CyInterruptEndPoint : CyUSBEndPoint
    {
        internal unsafe CyInterruptEndPoint(IntPtr h, USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor)
                : base(
                        h,
                        EndPtDescriptor) { }

        internal unsafe CyInterruptEndPoint(IntPtr h,
                                            USB_ENDPOINT_DESCRIPTOR * EndPtDescriptor,
                                            USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR * SSEndPtDescriptor)
                : base(
                        h,
                        EndPtDescriptor,
                        SSEndPtDescriptor) { }
    }
}