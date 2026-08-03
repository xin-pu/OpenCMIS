/*
 ## Cypress CyUSB C# library source file (CyScript.cs)
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

namespace OpenCMIS.Cypress
{
    public static class TTLock
    {
        public static object GlobalWriteLock { get; } = new ();
    }

    public class TTransaction
    {
        public const byte ReqType_DIR_MASK  = 0x80;
        public const byte ReqType_TYPE_MASK = 0x60;
        public const byte ReqType_TGT_MASK  = 0x03;
        public const byte TotalHeaderSize   = 32;

        public uint   Signature;  //4
        public uint   RecordSize; //8
        public ushort HeaderSize; //10
        public byte   Tag;        //11
        public byte   ConfigNum;  //12
        public byte   IntfcNum;   //13
        public byte   AltIntfc;   //14
        public byte   EndPtAddr;  //15

        public byte bReqType;   //16 //EP0 Xfer
        public byte CtlReqCode; //17  //EP0 Xfer
        public byte reserved0;  //18

        public ushort wValue;    //20
        public ushort wIndex;    //22
        public byte   reserved1; //23
        public byte   reserved2; //24

        public uint Timeout; //28
        public uint DataLen; //32

        public TTransaction()
        {
            Signature  = 0x54505343;
            HeaderSize = TotalHeaderSize;

            ConfigNum = 0;
            IntfcNum  = 0;
            AltIntfc  = 0;
            EndPtAddr = 0;

            Tag      = 0;
            bReqType = 0;

            //this.Target = 0x00;//TGT_DEVICE
            //this.ReqType = 0x40;//REQ_VENDOR
            //this.Direction = 0x00; //DIR_TO_DEVICE
        }

        public void WriteToStream(FileStream f)
        {
            lock (TTLock.GlobalWriteLock)
            {
                var wr = new BinaryWriter(f);
                wr.Write(Signature);
                wr.Write(RecordSize);
                wr.Write(HeaderSize);
                wr.Write(Tag);
                wr.Write(ConfigNum);
                wr.Write(IntfcNum);
                wr.Write(AltIntfc);
                wr.Write(EndPtAddr);
                wr.Write(bReqType);
                wr.Write(CtlReqCode);
                wr.Write(reserved0);
                wr.Write(wValue);
                wr.Write(wIndex);
                wr.Write(reserved1);
                wr.Write(reserved2);
                wr.Write(Timeout);
                wr.Write(DataLen);
                Thread.Sleep(0);
            }
        }

        public void ReadFromStream(FileStream f)
        {
            lock (TTLock.GlobalWriteLock)
            {
                var rd = new BinaryReader(f);

                Signature  = rd.ReadUInt32();
                RecordSize = rd.ReadUInt32();
                HeaderSize = rd.ReadUInt16();
                Tag        = rd.ReadByte();
                ConfigNum  = rd.ReadByte();
                IntfcNum   = rd.ReadByte();
                AltIntfc   = rd.ReadByte();
                EndPtAddr  = rd.ReadByte();
                bReqType   = rd.ReadByte();
                CtlReqCode = rd.ReadByte();
                reserved0  = rd.ReadByte();
                wValue     = rd.ReadUInt16();
                wIndex     = rd.ReadUInt16();
                reserved1  = rd.ReadByte();
                reserved2  = rd.ReadByte();
                Timeout    = rd.ReadUInt32();
                DataLen    = rd.ReadUInt32();
            }
        }

        public void ReadToBuffer(FileStream f, ref byte[] buffer, ref int len)
        {
            if (len > 0)
            {
                lock (TTLock.GlobalWriteLock)
                {
                    var rd = new BinaryReader(f);
                    rd.Read(buffer, 0, len);
                }
            }
        }

        public void WriteFromBuffer(FileStream f, ref byte[] buffer, ref int len)
        {
            if (len > 0)
            {
                lock (TTLock.GlobalWriteLock)
                {
                    var wr = new BinaryWriter(f);
                    wr.Write(buffer, 0, len);
                    Thread.Sleep(0);
                }
            }
        }
    }
}
