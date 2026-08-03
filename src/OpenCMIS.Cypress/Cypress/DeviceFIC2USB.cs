using System.Diagnostics;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Driver class of FIC USB device
    /// </summary>
    public class DeviceFIC2USB : EZUSBDevice
    {
        private const byte FSB2W_DevAddr = 0xF8;

        //FCC3B
        private const byte PASSWRDADDR = 0x7B;

        private const          byte   TABLE_SEL_ADDR        = 0x7F;
        private const          string cFinisarPassWord_A0_S = "C66AA466A0"; // Leave Page Select #32
        private const          string cFinisarPassWord_A1_S = "C66AA466A1"; // Leave Page Select #33
        private const          string cFinisarPassWord_E1_S = "C66AA466E1"; // Leave Page Select #33 with quiesce
        public static readonly object lockdevice            = new ();

        private bool init_bridge;
        private int  myFSBspeed;

        private int myPortValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeviceFIC2USB" /> class.
        /// </summary>
        /// <param name="usbDeviceInstance">The usb device instance.</param>
        public DeviceFIC2USB(USBDevice usbDeviceInstance)
                : base(usbDeviceInstance)
        {
            var device = (CyUSBDevice) _usbDevice;
            device.AltIntfc = 0x1;

            CyUsbEndPointOut = device.EndPointOf(OUT_ENDPOINTADDR);
            CyUsbEndPointIn  = device.EndPointOf(IN_ENDPOINTADDR);
            SerialNumber     = GetSerailNumber();
        }

        public sealed override string GetSerailNumber()
        {
            var getVal = EvaluateSerialnum(); //_usbDevice.SerialNumber;
            return getVal != string.Empty ? getVal : "";
        }

        /// <summary>
        ///     Resets the end point.
        /// </summary>
        public void ResetEndPoint()
        {
            var buffer = new byte[512];
            var length = 512;
            CyUsbEndPointIn.Reset();
            CyUsbEndPointOut.Reset();
            CyUsbEndPointIn.XferData(ref buffer, ref length);
        }

        /// <summary>
        ///     port selection for FIC2USB card I2C bus
        /// </summary>
        /// <param name="port">0-7 is the 3:8 chip selection, 0->Y0 and 7->Y7</param>
        public void PortSelection(int port)
        {
            var length    = 3;
            var bufferOut = new byte[3];
            var bufferIn  = new byte[3];
            myPortValue = port;

            //*************************** I2C Bus Switch Format*********************************************
            //  I2C port selection
            //	Offset 	Use		Value
            //	0		Command	CMD_I2CBusSelect==0x10
            //	1		port	0x07 (0-7 is valid)
            //	2		Result	0x01 = True
            //**********************************************************************************************
            bufferOut[0] = CMD_I2CSelectPort;
            bufferOut[1] = (byte) myPortValue;
            bufferOut[2] = 0;
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                if (bufferIn[length - 1] != 0x1)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " enable I2C port command not executed successfully.");
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        #region ADC
        public double[] GetADCValues()
        {
            var length    = 26;
            var bufferOut = new byte[26];
            var bufferIn  = new byte[26];
            bufferOut[0] = 97;
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                if (bufferIn[length - 1] != 0x1)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " GPIOWrite command not executed successfully.");
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            var listVol = new List<double>();
            foreach (var i in Enumerable.Range(0, 12))
            {
                var data = bufferIn.Skip(1 + i * 2).Take(2).ToArray();
                var vol  = (data[0] * 256.0 + data[1]) / 1000;
                listVol.Add(vol);
            }

            listVol[10] *= 2.0;
            listVol[11] *= 2.0;

            return listVol.ToArray();
        }
        #endregion ADC

        private string EvaluateSerialnum()
        {
            lock (lockdevice)
            {
                PortSelection(8); //select port8 for onboard EEPROM

                var data = new byte[8];
                I2CWrite(0xAA, new byte[] {0, 0}, 2); //write memory address
                I2CRead(0xAA, ref data, 6);           //read back first 6 bytes

                if (data.Take(6).All(a => a == 0xff))
                {
                    var myvalue = DateTime.Now.AddMinutes(55).ToString("dd'.'MM'.'yyyy'.'HH'.'mm'.'ss");
                    var items   = myvalue.Split('.');
                    data = new byte[]
                           {
                               0,
                               0,
                               (byte) (Convert.ToInt16(items[0])        & 0xFF),
                               (byte) (Convert.ToInt16(items[1])        & 0xFF),
                               (byte) (Convert.ToInt16(items[2]) - 2000 & 0xFF),
                               (byte) (Convert.ToInt16(items[3])        & 0xFF),
                               (byte) (Convert.ToInt16(items[4])        & 0xFF),
                               (byte) (Convert.ToInt16(items[5])        & 0xFF)
                           };
                    I2CWrite(0xAA, data, 2); //write memory address
                    I2CWrite(0xAA, data, 8); //write serial number
                    return string.Join("", data.Where((a, b) => b >= 2).Select(a => a.ToString()));
                }

                return string.Join("", data.Take(6).Select(a => a.ToString()));
            }
        }

        private void Init_FIC2USBCard()
        {
            if (!init_bridge)
            {
                init_bridge = Init_fsb_bridge();
                if (!init_bridge)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " initialize bridge for FSB not executed successfully.");
                }

                init_bridge = init_SPI_bridge();
                if (!init_bridge)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " initialize bridge for SPI not executed successfully.");
                }
            }
        }

        private bool RAM_write(byte device_addr, int port, int addr, byte[] data)
        {
            int i;
            var mydata = new byte[2 + data.Length];
            mydata[0] = (byte) ((addr & 0xFF00) >> 8);
            mydata[1] = (byte) (addr & 0xFF);
            for (i = 0; i < data.Length; i++)
                mydata[2 + i] = data[i];

            myPortValue = port;
            try
            {
                I2CWrite(device_addr, mydata, data.Length + 2);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region COMMAND words
        // COMMAND words
        //-----------------------------------------------------------------------------
        //USB Command Codes
        //-----------------------------------------------------------------------------
        //Diagnostic Commands
        private const int CMD_Bulkloop = 0x00;

        private const int CMD_ReadRAM    = 0x01;
        private const int CMD_WriteRAM   = 0x02;
        private const int CMD_ExtRAMTest = 0x03;
        private const int CMD_Delay      = 0x0D;

        //I2C Command Codes
        private const int CMD_I2CSelectPort = 0x10;

        private const int CMD_I2CRead               = 0x11;
        private const int CMD_I2CWrite              = 0x12;
        private const int CMD_I2CWriteRead          = 0x13;
        private const int CMD_I2CWriteWrite         = 0x14;
        private const int CMD_I2CWaitForEEPROMWrite = 0x15;
        private const int CMD_FindAllDevices        = 0x16;
        private const int CMD_TestComm              = 0x17;

        //Digital I/O Commands
        private const int CMD_DIOReadPortOE = 0xD0;

        private const int CMD_DIOWritePortOE    = 0xD1;
        private const int CMD_DIOReadPort       = 0xD2;
        private const int CMD_DIOWritePort      = 0xD3;
        private const int CMD_DIOWriteReadPort  = 0xD4;
        private const int CMD_DIOReadBitOE      = 0xD5;
        private const int CMD_DIOWriteBitOE     = 0xD6;
        private const int CMD_DIOReadBit        = 0xD7;
        private const int CMD_DIOWriteBit       = 0xD8;
        private const int CMD_DIOReadPortArray  = 0xD9;
        private const int CMD_DIOWritePortArray = 0xDA;

        private const int FSB_PORT = 6;

        private const int SPI_PORT = 4;

        // endpoint configuration
        private const byte OUT_ENDPOINTADDR = 0x02; // OUT from USB

        private const byte IN_ENDPOINTADDR = 0x86; // IN to USB
        #endregion COMMAND words

        #region block build functions
        private byte[] GetFSBDelay(int NumRegisters, FIC2USB_FSBSpeed Speed, bool EnableCRC)
        {
            int d;
            var command = new byte[4];
            if (EnableCRC)

                    //Delay with CRC
                    //'(100uS*Speed)+150uS
                    //Return in Milliseconds
                d = (int) Math.Round(NumRegisters * (0.1 * (int) Speed + 0.15));

            //Check for minimum of 1mS delay
            //If d < 1 Then d = 1
            else

                    //Delay w/o CRC
                    //(80uS*Speed)+120uS
                    //Return in Milliseconds
                d = (int) Math.Round(NumRegisters * (0.08 * (int) Speed + 0.12));

            //Check for minimum of 1mS delay
            //If d < 1 Then d = 1

            command[0] = 0xD;
            command[1] = (byte) (d >> 8 & 0xff);
            command[2] = (byte) (d      & 0xff);
            command[3] = 0;
            return command;
        }

        private byte[] GetGPIOCfgFSB2W(FIC2USB_FSB2WPort Port)
        {
            var GPIOBlk = new byte[10];

            var FSBClock = ((int) GPIO_CFG_DSEL.FRV_LO_FSB0C      << 4) +
                           ((int) GPIO_CFG_TYPE.FRV_IOB_IS_OUTPUT << 2);
            var FSBData = ((int) GPIO_CFG_DSEL.FRV_LB_FSB0D     << 4) +
                          ((int) GPIO_CFG_TYPE.FRV_IOB_IS_BIDIR << 2);
            var FSBLED = ((int) GPIO_CFG_DSEL.FRV_HWP_GPIOR     << 4) +
                         ((int) GPIO_CFG_TYPE.FRV_IOB_IS_OUTPUT << 2);

            switch (Port)
            {
                case FIC2USB_FSB2WPort.FSB2WPort1:
                    //GPIO Pin Configuration
                    //Pin Function
                    GPIOBlk[0] = 0x10;            //MSB -Register 0x1020
                    GPIOBlk[1] = 0x20;            //LSB
                    GPIOBlk[2] = (byte) FSBData;  //A32L_GPIO_00_CFG[]     //GPIO0 = FSB_SDA1
                    GPIOBlk[3] = 0;               //A32L_GPIO_01_CFG[]     //GPIO1 = FSB_SCL3
                    GPIOBlk[4] = 0;               //A32L_GPIO_02_CFG[]     //GPIO2 = FSB_SCL2
                    GPIOBlk[5] = (byte) FSBClock; //A32L_GPIO_03_CFG[]     //GPIO3 = FSB_SCL1
                    GPIOBlk[6] = 0;               //A32L_GPIO_04_CFG[]     //GPIO4 = FSB_SDA2
                    GPIOBlk[7] = 0;               //A32L_GPIO_05_CFG[]     //GPIO5 = SHUTDOWN_N
                    GPIOBlk[8] = 0;               //A32L_GPIO_06_CFG[]    //GPIO6 = FSB_SDA3
                    GPIOBlk[9] = (byte) FSBLED;   //A32L_GPIO_07_CFG[]    //GPIO7 = FSB2W_BUSY_N
                    break;

                case FIC2USB_FSB2WPort.FSB2WPort2:
                    //GPIO Pin Configuration
                    //Pin Function
                    GPIOBlk[0] = 0x10;            //MSB -Register 0x1020
                    GPIOBlk[1] = 0x20;            //LSB
                    GPIOBlk[2] = 0;               //A32L_GPIO_00_CFG[]     //GPIO0 = FSB_SDA1
                    GPIOBlk[3] = 0;               //A32L_GPIO_01_CFG[]     //GPIO1 = FSB_SCL3
                    GPIOBlk[4] = (byte) FSBClock; //A32L_GPIO_02_CFG[]     //GPIO2 = FSB_SCL2
                    GPIOBlk[5] = 0;               //A32L_GPIO_03_CFG[]     //GPIO3 = FSB_SCL1
                    GPIOBlk[6] = (byte) FSBData;  //A32L_GPIO_04_CFG[]     //GPIO4 = FSB_SDA2
                    GPIOBlk[7] = 0;               //A32L_GPIO_05_CFG[]     //GPIO5 = SHUTDOWN_N
                    GPIOBlk[8] = 0;               //A32L_GPIO_06_CFG[]    //GPIO6 = FSB_SDA3
                    GPIOBlk[9] = (byte) FSBLED;   //A32L_GPIO_07_CFG[]    //GPIO7 = FSB2W_BUSY_N
                    break;

                case FIC2USB_FSB2WPort.FSB2WPort3:
                    //GPIO Pin Configuration
                    //Pin Function
                    GPIOBlk[0] = 0x10;            //MSB -Register 0x1020
                    GPIOBlk[1] = 0x20;            //LSB
                    GPIOBlk[2] = 0;               //A32L_GPIO_00_CFG[]     //GPIO0 = FSB_SDA1
                    GPIOBlk[3] = (byte) FSBClock; //A32L_GPIO_01_CFG[]     //GPIO1 = FSB_SCL3
                    GPIOBlk[4] = 0;               //A32L_GPIO_02_CFG[]     //GPIO2 = FSB_SCL2
                    GPIOBlk[5] = 0;               //A32L_GPIO_03_CFG[]     //GPIO3 = FSB_SCL1
                    GPIOBlk[6] = 0;               //A32L_GPIO_04_CFG[]     //GPIO4 = FSB_SDA2
                    GPIOBlk[7] = 0;               //A32L_GPIO_05_CFG[]     //GPIO5 = SHUTDOWN_N
                    GPIOBlk[8] = (byte) FSBData;  //A32L_GPIO_06_CFG[]    //GPIO6 = FSB_SDA3
                    GPIOBlk[9] = (byte) FSBLED;   //A32L_GPIO_07_CFG[]    //GPIO7 = FSB2W_BUSY_N
                    break;
            }

            return GPIOBlk;
        }

        private byte[] GetGPIOCfgFSB2W_Reset()
        {
            var GPIOBlk = new byte[10];

            //Any Port
            //GPIO Pin Configuration
            //Pin Function
            GPIOBlk[0] = 0x10; //MSB -Register 0x1020
            GPIOBlk[1] = 0x20; //LSB
            GPIOBlk[2] = 0;    //A32L_GPIO_00_CFG[]     //GPIO0 = FSB_SDA1           //All inputs [Hi-Z]
            GPIOBlk[3] = 0;    //A32L_GPIO_01_CFG[]     //GPIO1 = FSB_SCL3
            GPIOBlk[4] = 0;    //A32L_GPIO_02_CFG[]     //GPIO2 = FSB_SCL2
            GPIOBlk[5] = 0;    //A32L_GPIO_03_CFG[]     //GPIO3 = FSB_SCL1
            GPIOBlk[6] = 0;    //A32L_GPIO_04_CFG[]     //GPIO4 = FSB_SDA2
            GPIOBlk[7] = 0;    //A32L_GPIO_05_CFG[]     //GPIO5 = SHUTDOWN_N
            GPIOBlk[8] = 0;    //A32L_GPIO_06_CFG[]    //GPIO6 = FSB_SDA3
            GPIOBlk[9] = 0;    //A32L_GPIO_07_CFG[]    //GPIO7 = FSB2W_BUSY_N

            return GPIOBlk;
        }

        private byte[] GetMSCReadBlock(FIC2USB_FSBSpeed Speed,
                                       byte             FSBDevAddr,
                                       bool             EnableCRC,
                                       byte             StartReg,
                                       int              Length)
        {
            //Determine Op Code
            var OpCode = EnableCRC ? MSC_OPCODE.MSCO_FSB_RD_W_CRC : MSC_OPCODE.MSCO_FSB_RD_WO_CRC;

            //MSC Block
            var MSCBlk = new byte[11];
            MSCBlk[0]  = 0x0;                 //MSB -Register 0x80
            MSCBlk[1]  = 0x80;                //LSB
            MSCBlk[2]  = (byte) OpCode;       //0  opcode
            MSCBlk[3]  = (byte) (Length - 1); //1  xferlen
            MSCBlk[4]  = 0x2;                 //2  sysaddr_h   //Starts at 0x0200
            MSCBlk[5]  = 0x0;                 //3  sysaddr_l
            MSCBlk[6]  = 0;                   //4  memaddr_h
            MSCBlk[7]  = StartReg;            //5  memaddr_l   //FSB Starting Register
            MSCBlk[8]  = FSBDevAddr;          //6  devaddr
            MSCBlk[9]  = (byte) Speed;        //7  mckrate
            MSCBlk[10] = 0;                   //8  spi_cfg

            return MSCBlk;
        }

        private byte[] GetMSCWriteBlock(FIC2USB_FSBSpeed Speed,
                                        byte             FSBDevAddr,
                                        bool             EnableCRC,
                                        byte             StartReg,
                                        int              Length)
        {
            //Determine Op Code
            var OpCode = EnableCRC ? MSC_OPCODE.MSCO_FSB_WR_W_CRC : MSC_OPCODE.MSCO_FSB_WR_WO_CRC;

            //MSC Block
            var MSCBlk = new byte[11];
            MSCBlk[0]  = 0x0;                 //MSB -Register 0x80
            MSCBlk[1]  = 0x80;                //LSB
            MSCBlk[2]  = (byte) OpCode;       //0  opcode
            MSCBlk[3]  = (byte) (Length - 1); //1  xferlen
            MSCBlk[4]  = 0x1;                 //2  sysaddr_h   //Starts at 0x0100
            MSCBlk[5]  = 0x0;                 //3  sysaddr_l
            MSCBlk[6]  = 0;                   //4  memaddr_h
            MSCBlk[7]  = StartReg;            //5  memaddr_l
            MSCBlk[8]  = FSBDevAddr;          //6  devaddr
            MSCBlk[9]  = (byte) Speed;        //7  mckrate
            MSCBlk[10] = 0;                   //8  spi_cfg

            return MSCBlk;
        }

        private byte[] ConvertToByteArray(ref int[] WordArray, int Length) //Convert Word Array to Byte Array
        {
            var ba = new byte[Length * 2];
            int i;
            for (i = 0; i < Length; i++)
            {
                ba[i * 2 + 1] = (byte) (WordArray[i]      & 0xFF);
                ba[i * 2]     = (byte) (WordArray[i] >> 8 & 0xFF);
            }

            return ba;
        }

        private byte[] GetSPIMSCReadBlock(byte Port, int Speed, byte Length, byte spi_config)
        {
            var MSCBlk = new byte[10];
            MSCBlk[0] = 0x0;                 //MSB register 0x80
            MSCBlk[1] = 0x80;                //LSB
            MSCBlk[2] = 0x3D;                //Opcode
            MSCBlk[3] = (byte) (Length - 1); //xfer Length
            MSCBlk[4] = 0x2;                 //sysaddr H
            MSCBlk[5] = 0x0;                 //sysaddr L
            MSCBlk[6] = 0x0;                 //mscs rate
            MSCBlk[7] = (byte) Speed;        //mck rate
            MSCBlk[8] = spi_config;          //spi config
            MSCBlk[9] = (byte) (2 ^ Port);   //chip sel
            return MSCBlk;
        }

        private byte[] GetSPIMSCWriteBlock(byte Port, int Speed, byte Length, byte spi_config)
        {
            var MSCBlk = new byte[10];
            MSCBlk[0] = 0x0;
            MSCBlk[1] = 0x80;
            MSCBlk[2] = 0x3E;
            MSCBlk[3] = (byte) (Length - 1);
            MSCBlk[4] = 0x2;
            MSCBlk[5] = 0x0;
            MSCBlk[6] = 0x0;
            MSCBlk[7] = (byte) Speed;
            MSCBlk[8] = spi_config;
            MSCBlk[9] = (byte) (2 ^ Port);
            return MSCBlk;
        }

        private byte[] GetSPIMSCWriteReadBlock(byte Port, int Speed, byte Length, byte spi_config)
        {
            var MSCBlk = new byte[10];
            MSCBlk[0] = 0x0;
            MSCBlk[1] = 0x80;
            MSCBlk[2] = 0x3F;
            MSCBlk[3] = (byte) (Length - 1);
            MSCBlk[4] = 0x2;
            MSCBlk[5] = 0x0;
            MSCBlk[6] = 0x0;
            MSCBlk[7] = (byte) Speed;
            MSCBlk[8] = spi_config;
            MSCBlk[9] = (byte) (2 ^ Port);
            return MSCBlk;
        }

        private byte[] GetSPIGPIOState(int Port)
        {
            var data = new byte[3];
            switch (Port)
            {
                case 0:
                {
                    data[0] = 0x10;
                    data[1] = 0xC2;
                    data[2] = 0xE;
                    break;
                }
                case 1:
                {
                    data[0] = 0x10;
                    data[1] = 0xC2;
                    data[2] = 0x16;
                    break;
                }
                case 2:
                {
                    data[0] = 0x10;
                    data[1] = 0xC2;
                    data[2] = 0x1A;
                    break;
                }
                case 3:
                {
                    data[0] = 0x10;
                    data[1] = 0xC2;
                    data[2] = 0x1C;
                    break;
                }
            }

            return data;
        }
        #endregion block build functions

        #region SPI
        private bool init_SPI_bridge()
        {
            var MSCblock = new byte[7];
            myPortValue = SPI_PORT; //FCC03 SPI Port
            PortSelection(myPortValue);

            //write level3 password
            MSCblock[0] = 0x7B; //password entry
            MSCblock[1] = 0xC6;
            MSCblock[2] = 0x6A;
            MSCblock[3] = 0xA4;
            MSCblock[4] = 0x66;
            MSCblock[5] = 0xE1;
            try
            {
                I2CWrite(0xFE, MSCblock, 6);
            }
            catch (Exception)
            {
                return false;
            }

            //increase SYSCLK oscillator
            var data = new byte[1];
            data[0] = 0x0;
            if (!RAM_write(FSB2W_DevAddr, 4, 0x1008, data))
                return false;

            //config digital IO pin
            data    = new byte[8];
            data[0] = 0x94;
            data[1] = 0xA4;
            data[2] = 0xA0;
            data[3] = 0x55;
            data[4] = 0x65;
            data[5] = 0x75;
            data[6] = 0x85;
            data[7] = 0x4;
            if (!RAM_write(FSB2W_DevAddr, 4, 0x1020, data))
                return false;

            //GPIO setting
            data    = new byte[3];
            data[0] = 0x1F;
            data[1] = 0x0;
            data[2] = 0x1F;
            if (!RAM_write(FSB2W_DevAddr, 4, 0x10C0, data))
                return false;

            data[0] = 0x20;
            data[1] = 0x0;
            data[2] = 0x0;
            if (!RAM_write(FSB2W_DevAddr, 4, 0x101C, data))
                return false;

            return true;
        }

        /// <summary>
        ///     SPI write and read function
        /// </summary>
        /// <param name="Port">Port of SPI chip selection</param>
        /// <param name="WriteData">data for write to SPI bus</param>
        /// <param name="ReadData">data for read from SPI bus</param>
        /// <param name="Length">length of data</param>
        /// <param name="cpol">CLK leading edge</param>
        /// <param name="cpha_miso">MISO sample edge</param>
        /// <param name="cpha_mosi">MOSI sample edge</param>
        public void SPIWriteRead(int       Port,
                                 int[]     WriteData,
                                 ref int[] ReadData,
                                 int       Length,
                                 int       cpol,
                                 int       cpha_miso,
                                 int       cpha_mosi)
        {
            if (Port > 3)
                throw new ("FIC2USB card " + ProductName + " SPI Port select cannot be great than 3.");

            if (!init_bridge)
                Init_FIC2USBCard();
            myPortValue = SPI_PORT; //FCC03 SPI Port
            PortSelection(myPortValue);
            var MSCblock = new byte[Length + 2];
            int i;

            //Data
            MSCblock[0] = 0x2;
            MSCblock[1] = 0x0;
            for (i = 0; i < Length; i++)
                MSCblock[i + 2] = (byte) WriteData[i];

            I2CWrite(FSB2W_DevAddr, MSCblock, Length + 2);

            //MSC Req Block
            MSCblock = GetSPIMSCWriteReadBlock((byte) Port, 4, (byte) Length, SPI_Config(cpol, cpha_miso, cpha_mosi));
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Set ChipSelect and LED Status
            MSCblock = GetSPIGPIOState(Port);
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Wrote REQ_BLCK_AddrH/L + RESET MSC_STUS
            MSCblock    = new byte[5];
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEA;
            MSCblock[2] = 0x0;
            MSCblock[3] = 0x80;
            MSCblock[4] = 0x35;
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Read Status Byte
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xED;
            I2CWrite(FSB2W_DevAddr, MSCblock, 2);
            I2CRead(FSB2W_DevAddr, ref MSCblock, 1);

            //Reset
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEC;
            MSCblock[2] = 0x3F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);

            //Reset GPIO Cfg registers
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xC2;
            MSCblock[2] = 0x1F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);

            //Read bytes
            MSCblock[0] = 0x2;
            MSCblock[1] = 0x0;
            I2CWrite(FSB2W_DevAddr, MSCblock, 2);

            var tempdata = new byte[Length];
            I2CRead(FSB2W_DevAddr, ref tempdata, Length);
            for (i = 0; i < Length; i++)
                ReadData[i] = tempdata[i];
        }

        private byte SPI_Config(int cpol, int cpha_miso, int cpha_mosi)
        {
            byte spi_config = 0;
            if (cpol == 1) //CPOL=0, leading edge is rising, CPOL = 1, leading edge is trailing
                spi_config |= 0x1;

            if (cpha_miso == 1) //CPHA = 0, sample at rising edge, CPHA = 1, sample at trailing edge
                spi_config |= 0x4;

            if (cpha_mosi == 1)
                spi_config |= 0x2;

            return spi_config;
        }

        /// <summary>
        ///     SPI Read function
        /// </summary>
        /// <param name="Port">Port of SPI chip selection</param>
        /// <param name="ReadData">data for read from SPI bus</param>
        /// <param name="Length">length of data</param>
        /// <param name="cpol">CLK leading edge</param>
        /// <param name="cpha_miso">MISO sample edge</param>
        /// <param name="cpha_mosi">MOSI sample edge</param>
        public void SPIRead(int Port, ref int[] ReadData, int Length, int cpol, int cpha_miso, int cpha_mosi)
        {
            if (Port > 3)
                throw new ("FIC2USB card " + ProductName + " SPI Port select cannot be great than 3.");

            if (!init_bridge)
                Init_FIC2USBCard();

            myPortValue = SPI_PORT; //FCC03 SPI Port
            PortSelection(myPortValue);
            int i;

            //Write RAM - MSC REQ Block
            var MSCblock = GetSPIMSCReadBlock((byte) Port, 1, (byte) Length, SPI_Config(cpol, cpha_miso, cpha_mosi));
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Set ChipSelect and LED Status
            MSCblock = GetSPIGPIOState(Port);
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Wrote REQ_BLCK_AddrH/L + RESET MSC_STUS
            MSCblock    = new byte[5];
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEA;
            MSCblock[2] = 0x0;
            MSCblock[3] = 0x80;
            MSCblock[4] = 0x35;
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Read Status Byte
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xED;
            I2CWrite(FSB2W_DevAddr, MSCblock, 2);
            I2CRead(FSB2W_DevAddr, ref MSCblock, 1);

            //Reset
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEC;
            MSCblock[2] = 0x3F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);

            //Reset GPIO Cfg registers
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xC2;
            MSCblock[2] = 0x1F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);

            //Read bytes
            MSCblock[0] = 0x2;
            MSCblock[1] = 0x0;
            I2CWrite(FSB2W_DevAddr, MSCblock, 2);

            var tempdata = new byte[Length];
            I2CRead(FSB2W_DevAddr, ref tempdata, Length);
            for (i = 0; i < Length; i++)
                ReadData[i] = tempdata[i];
        }

        /// <summary>
        ///     SPI write function
        /// </summary>
        /// <param name="Port">Port of SPI chip selection</param>
        /// <param name="WriteData">data for write to SPI bus</param>
        /// <param name="Length">length of data</param>
        /// <param name="cpol">CLK leading edge</param>
        /// <param name="cpha_miso">MISO sample edge</param>
        /// <param name="cpha_mosi">MOSI sample edge</param>
        public void SPIWrite(int Port, int[] WriteData, int Length, int cpol, int cpha_miso, int cpha_mosi)
        {
            if (Port > 3)
                throw new ("FIC2USB card " + ProductName + " SPI Port select cannot be great than 3.");

            if (!init_bridge)
                Init_FIC2USBCard();

            myPortValue = SPI_PORT; //FCC03 SPI Port
            PortSelection(myPortValue);
            var MSCblock = new byte[Length + 2];
            int i;

            //Data
            MSCblock[0] = 0x2;
            MSCblock[1] = 0x0;
            for (i = 0; i < Length; i++)
                MSCblock[i + 2] = (byte) WriteData[i];

            I2CWrite(FSB2W_DevAddr, MSCblock, Length + 2);

            //MSC Req Block
            MSCblock = GetSPIMSCWriteBlock((byte) Port, 4, (byte) Length, SPI_Config(cpol, cpha_miso, cpha_mosi));
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Set ChipSelect and LED Status
            MSCblock = GetSPIGPIOState(Port);
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Wrote REQ_BLCK_AddrH/L + RESET MSC_STUS
            MSCblock    = new byte[5];
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEA;
            MSCblock[2] = 0x0;
            MSCblock[3] = 0x80;
            MSCblock[4] = 0x35;
            I2CWrite(FSB2W_DevAddr, MSCblock, MSCblock.Length);

            //Read Status Byte
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xED;
            I2CWrite(FSB2W_DevAddr, MSCblock, 2);
            I2CRead(FSB2W_DevAddr, ref MSCblock, 1);

            //Reset
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xEC;
            MSCblock[2] = 0x3F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);

            //Reset GPIO Cfg registers
            MSCblock[0] = 0x10;
            MSCblock[1] = 0xC2;
            MSCblock[2] = 0x1F;
            I2CWrite(FSB2W_DevAddr, MSCblock, 3);
        }
        #endregion SPI

        #region FSB
        private bool Init_fsb_bridge()
        {
            var MSCblock = new byte[7];
            myPortValue = FSB_PORT; //FCC03 I2C Port
            PortSelection(myPortValue);

            //write level3 password
            MSCblock[0] = 0x7B; //password entry
            MSCblock[1] = 0xC6;
            MSCblock[2] = 0x6A;
            MSCblock[3] = 0xA4;
            MSCblock[4] = 0x66;
            MSCblock[5] = 0xE1;
            try
            {
                I2CWrite(0xFE, MSCblock, 6);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }

            MSCblock[0] = 0x10; //password entry
            MSCblock[1] = 0x08;
            MSCblock[2] = 0x0;
            try
            {
                I2CWrite(FSB2W_DevAddr, MSCblock, 3);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }

            MSCblock[0] = 0x10;
            MSCblock[1] = 0xC0;
            MSCblock[2] = 0x1;
            MSCblock[3] = 0x0;
            MSCblock[4] = 0x0;
            try
            {
                I2CWrite(FSB2W_DevAddr, MSCblock, 5);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Gets or sets the fs bspeed.
        /// </summary>
        /// <value>
        ///     The fs bspeed.
        /// </value>
        public int FSBspeed
        {
            get => myFSBspeed;
            set
            {
                if (value <= 15)
                    myFSBspeed = value;
            }
        }

        public bool FSB2WRead(FIC2USB_FSB2WPort Port,
                              FIC2USB_FSBSpeed  Speed,
                              byte              FSBDevAddr,
                              bool              EnableCRC,
                              byte              StartReg,
                              ref int[]         ReadData)
        {
            var Length = ReadData.Length;
            if (!init_bridge)
                Init_FIC2USBCard();

            // Step 1 Create miscellenous block
            var MSCblock = GetMSCReadBlock(Speed, FSBDevAddr, EnableCRC, StartReg, Length);
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);

            // Step 2 Write RAM - GPIO cfg Registers
            MSCblock = GetGPIOCfgFSB2W(Port);
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);

            //Write REQ_BLk_AddrH/L + RESET MSC_STUS

            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xEA,
                         0x0,
                         0x80,
                         0x35
                     });

            //set delay
            var bufferIn  = new byte[4];
            var bufferOut = GetFSBDelay(ReadData.Length, Speed, EnableCRC);
            var length    = 4;
            if (!CommandData(ref bufferIn, ref bufferOut, ref length) || bufferIn[length - 1] != 0x1)
                return false;

            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xED
                     });

            //read status
            var status = new byte[1];
            I2CRead(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, 1, ref status);

            //reset
            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xEC,
                         0x3F
                     });

            //Reset GPIO Cfg Registers
            MSCblock = GetGPIOCfgFSB2W_Reset();
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);

            //writing address then read data
            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x2,
                         0x0
                     });

            var rdbackdata = new byte[Length * 2];
            I2CRead(FIC2USB_I2CPort.I2CPort7,
                    FIC2USB_I2CSpeed.HS400Khz,
                    FSB2W_DevAddr,
                    rdbackdata.Length,
                    ref rdbackdata);
            int i;
            for (i = 0; i < Length; i++)
                ReadData[i] = (short) ((rdbackdata[2 * i] << 8) +
                                       rdbackdata[2 * i + 1]);

            return true;
        }

        public bool FSB2WWrite(FIC2USB_FSB2WPort Port,
                               FIC2USB_FSBSpeed  Speed,
                               byte              FSBDevAddr,
                               bool              EnableCRC,
                               byte              StartReg,
                               ref int[]         WriteData)
        {
            var Length = WriteData.Length;
            if (!init_bridge)
                Init_FIC2USBCard();

            var bWriteData = ConvertToByteArray(ref WriteData, Length);
            var index      = 0;
            var wadd       = 0x100;

            //pages
            for (var i = 0; i < bWriteData.Length / 32; i++)
            {
                var page = new byte[34];
                page[0] = (byte) (wadd >> 8);
                page[1] = (byte) (wadd & 0xff);
                for (var j = 0; j < 32; j++)
                    page[j + 2] = bWriteData[i * 32 + j];

                index = (i + 1) * 32;
                I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, page);
                wadd += 32;
            }

            //left bytes
            var restpage = new byte[bWriteData.Length - index + 2];
            restpage[0] = (byte) (wadd >> 8);
            restpage[1] = (byte) (wadd & 0xff);
            for (var i = index; i < bWriteData.Length; i++)
                restpage[2 + i] = bWriteData[i];

            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, restpage);

            //Write RAM - MSC REQ Block
            var MSCblock = GetMSCWriteBlock(Speed, FSBDevAddr, EnableCRC, StartReg, Length);
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);

            //Write RAM - GPIO Cfg Registers
            MSCblock = GetGPIOCfgFSB2W(Port);
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);

            //Write REQ_BLk_AddrH/L + RESET MSC_STUS
            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xEA,
                         0x0,
                         0x80,
                         0x35
                     });

            //Add Delay (During Execution of FSB command)
            //set delay
            var bufferIn  = new byte[4];
            var bufferOut = GetFSBDelay(WriteData.Length, Speed, EnableCRC);
            var length    = 4;
            if (!CommandData(ref bufferIn, ref bufferOut, ref length) || bufferIn[length - 1] != 0x1)
                return false;

            //Read Status Byte
            //Start By writing the address, then reading the byte
            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xED
                     });

            //read status
            var status = new byte[1];
            I2CRead(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, 1, ref status);

            //MSC - RESET NOW
            I2CWrite(FIC2USB_I2CPort.I2CPort7,
                     FIC2USB_I2CSpeed.HS400Khz,
                     FSB2W_DevAddr,
                     new byte[]
                     {
                         0x10,
                         0xEC,
                         0x3F
                     });

            //Write RAM - Reset GPIO Cfg Registers
            MSCblock = GetGPIOCfgFSB2W_Reset();
            I2CWrite(FIC2USB_I2CPort.I2CPort7, FIC2USB_I2CSpeed.HS400Khz, FSB2W_DevAddr, MSCblock);
            return true;
        }

        public bool FSB4WRead(FIC2USB_FSB4WPort Port,
                              FIC2USB_FSBSpeed  Speed,
                              byte              FSBDevAddr,
                              bool              EnableCRC,
                              byte              StartReg,
                              ref int[]         ReadData,
                              FIC2USB_I2CPort   FCC03Port = FIC2USB_I2CPort.I2CPort6)
        {
            return true;
        }

        public bool FSB4WWrite(FIC2USB_FSB4WPort Port,
                               FIC2USB_FSBSpeed  Speed,
                               byte              FSBDevAddr,
                               bool              EnableCRC,
                               byte              StartReg,
                               ref int[]         WriteData,
                               ref byte          ReturnStatus,
                               FIC2USB_I2CPort   FCC03Port = FIC2USB_I2CPort.I2CPort6)
        {
            return true;
        }

        public bool FSB4WExtRead(FIC2USB_FSB4WPort Port,
                                 FIC2USB_FSBSpeed  Speed,
                                 byte              FSBDevAddr,
                                 bool              EnableCRC,
                                 ref byte[]        ExtRegAddrs,
                                 int               ExtRegVal,
                                 ref int[]         ExtReadData,
                                 ref byte          ReturnStatus,
                                 FIC2USB_I2CPort   FCC03Port = FIC2USB_I2CPort.I2CPort6)
        {
            return true;
        }
        #endregion FSB

        #region I2C
        //*************************** I2C Bus Switch Format*********************************************
        //  I2C write
        //	Offset 	Use		Value
        //	0		Command		0x12
        //	1		port		0x0-7
        //	2		highspeed	0x01 = True, 0x00 = FALSE
        //	3		addr		0xFF
        //	4		length_MSB	0xFF
        //	5		length_LSB	0xFF
        //	6..N	*dat		Byte array with 'length' elements
        //	N+1		result		Result Byte
        //**********************************************************************************************
        public bool I2CWrite(FIC2USB_I2CPort  i2cPort,
                             FIC2USB_I2CSpeed speed,
                             byte             device_addr,
                             byte[]           dataArray)
        {
            var byteLength = dataArray.Length;
            var bufferOut  = new byte[7 + byteLength];
            var bufferIn   = new byte[7 + byteLength];

            //select the port for I2C communication
            PortSelection(myPortValue);

            bufferOut[0] = CMD_I2CWrite;
            bufferOut[1] = (byte) i2cPort;
            bufferOut[2] = (byte) speed;
            bufferOut[3] = device_addr;
            bufferOut[4] = (byte) ((byteLength & 0xFF00) >> 8);
            bufferOut[5] = (byte) (byteLength & 0xFF);
            for (var i = 0; i < byteLength; i++)
                bufferOut[6 + i] = dataArray[i];

            bufferOut[7 + byteLength - 1] = 0;
            var length = bufferOut.Length;

            return CommandData(ref bufferIn, ref bufferOut, ref length) && bufferIn[length - 1] == 0x1;
        }

        /// <summary>
        ///     I2C write
        /// </summary>
        /// <param name="device_addr">I2C device address</param>
        /// <param name="dataArray">data for write</param>
        /// <param name="byteLength">lenght of data</param>
        public void I2CWrite(byte device_addr, byte[] dataArray, int byteLength)
        {
            I2CWrite((FIC2USB_I2CPort) myPortValue, FIC2USB_I2CSpeed.LS100Khz, device_addr, dataArray);
        }

        //*************************** I2C Bus Switch Format*********************************************
        //  I2C read
        //	Offset 	Use			Value
        //	0		Command		0x11
        //	1		port		0x0-7
        //	2		highspeed	0x01 = True, 0x00 = FALSE
        //	3		addr		0xFF
        //	4		length_MSB	0xFF
        //	5		length_LSB	0xFF
        //	6..N	*dat		Byte array with 'length' elements
        //	N+1		result		Result Byte
        //**********************************************************************************************
        /// <returns></returns>
        public bool I2CRead(FIC2USB_I2CPort  i2cPort,
                            FIC2USB_I2CSpeed speed,
                            byte             device_addr,
                            int              byteLength,
                            ref byte[]       readData)
        {
            var bufferOut = new byte[7 + byteLength];
            var bufferIn  = new byte[7 + byteLength];

            //select the port for I2C communication
            PortSelection(myPortValue);

            bufferOut[0] = CMD_I2CRead;
            bufferOut[1] = (byte) i2cPort;
            bufferOut[2] = (byte) speed;
            bufferOut[3] = device_addr;
            bufferOut[4] = (byte) ((byteLength & 0xFF00) >> 8);
            bufferOut[5] = (byte) (byteLength & 0xFF);
            for (var i = 0; i < byteLength; i++)
                bufferOut[6 + i] = 0;

            bufferOut[7 + byteLength - 1] = 0;
            var length = bufferOut.Length;

            if (CommandData(ref bufferIn, ref bufferOut, ref length) && bufferIn[length - 1] == 0x1)
            {
                for (var i = 0; i < byteLength; i++)
                    readData[i] = bufferIn[6 + i];
                return true;
            }

            return false;
        }

        /// <summary>
        ///     I2C current address read
        /// </summary>
        /// <param name="device_addr">I2C device address</param>
        /// <param name="dataArray">data for read</param>
        /// <param name="byteLength">number of data for read</param>
        public void I2CRead(byte device_addr, ref byte[] dataArray, int byteLength)
        {
            I2CRead((FIC2USB_I2CPort) myPortValue, FIC2USB_I2CSpeed.LS100Khz, device_addr, byteLength, ref dataArray);
        }
        #endregion I2C

        #region I2C Package Method
        public bool I2CWaitForEEPROMWrite(FIC2USB_I2CPort I2CPort, FIC2USB_I2CSpeed HighSpeed, byte I2CAddr)
        {
            return true;
        }

        public bool I2CFindAllI2CAddresses(FIC2USB_I2CPort  I2CPort,
                                           FIC2USB_I2CSpeed HighSpeed,
                                           byte             StartAddr,
                                           byte             SearchNum,
                                           ref byte[]       Addresses)
        {
            return true;
        }
        #endregion I2C Package Method

        #region GPIO
        /// <summary>
        ///     Gets or sets the port setting.
        /// </summary>
        /// <value>
        ///     The port setting.
        /// </value>
        public int PortSetting
        {
            get => myPortValue;
            set => PortSelection(value);
        }

        /// <summary>
        ///     Gets the gpio port oe.
        /// </summary>
        /// <value>
        ///     The gpio port oe.
        /// </value>
        public int GPIOPortOE { get; private set; }

        public int GPIOPort { get; set; } = 0;

        /// <summary>
        ///     Gpio's the write.
        /// </summary>
        /// <param name="Value">The value.</param>
        /// <param name="PortEnable">The port enable.</param>
        /// <exception cref="CyXferDataEndPointException">
        ///     USB Device  + ProductName +  enable GPIO port command not executed successfully.
        ///     or
        ///     USB Device  + ProductName
        ///     or
        ///     USB Device  + ProductName +  GPIOWrite command not executed successfully.
        ///     or
        ///     USB Device  + ProductName
        /// </exception>
        public void GPIOWrite(ushort Value, ushort PortEnable)
        {
            var length    = 4;
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];

            //**********************************************************************************************
            // write port
            //	Offset 	Use		Value
            //	0		Command		0xD3
            //	1		port		0x1-5 (for A,B,C,D,E respectively)
            //	2		dat			pointer to byte
            //	3		result		Result Byte
            //**********************************************************************************************
            bufferOut[0] = CMD_DIOWritePort;
            bufferOut[1] = (byte) GPIOPort;
            bufferOut[2] = (byte) Value;
            bufferOut[3] = 0;
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                if (bufferIn[length - 1] != 0x1)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " GPIOWrite command not executed successfully.");
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //**********************************************************************************************
            //  write port OE
            //	Offset 	Use		Value
            //	0		Command		0xD1
            //	1		port		0x1-5 (for A,B,C,D,E respectively...0=All ports)
            //	2		OE			Output Enable (0x01 = TRUE/output, 0x00 = FALSE/input)
            //	3		result		Result Byte
            //**********************************************************************************************
            bufferOut[0] = CMD_DIOWritePortOE;
            bufferOut[1] = (byte) GPIOPort;
            bufferOut[2] = (byte) PortEnable;
            bufferOut[3] = 0;
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                if (bufferIn[length - 1] != 0x1)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " enable GPIO port command not executed successfully.");
                }

                GPIOPortOE = PortEnable; //record the latest port OE value
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     Gpioes the read.
        /// </summary>
        /// <param name="MyportValue">The myport value.</param>
        /// <exception cref="CyXferDataEndPointException">
        ///     USB Device  + ProductName +  GPIOWrite command not executed successfully.
        ///     or
        ///     USB Device  + ProductName
        /// </exception>
        public void GPIORead(ref int MyportValue)
        {
            //**********************************************************************************************
            //	Offset 	Use		Value
            //	0		Command		0xD2
            //	1		port		0x1-5 (for A,B,C,D,E respectively)
            //	2		dat			pointer to byte
            //	3		result		Result Byte
            //**********************************************************************************************
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];
            var length    = 4;
            bufferOut[0] = CMD_DIOReadPort;
            bufferOut[1] = (byte) GPIOPort;
            bufferOut[2] = 0;
            bufferOut[3] = 0;
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                if (bufferIn[length - 1] != 0x1)
                {
                    throw new CyXferDataEndPointException("USB Device " + ProductName +
                                                          " GPIOWrite command not executed successfully.");
                }

                MyportValue = bufferIn[2];
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }
        #endregion GPIO

        #region
        public byte ReadPortOE(FIC2USB_DIOPort DIOPort)
        {
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];

            bufferIn[0] = 208;
            bufferIn[1] = checked((byte) DIOPort);
            bufferIn[2] = 0;
            bufferIn[3] = 0;

            USBIO(ref bufferIn, ref bufferOut);

            var res = CheckResponse(bufferIn, bufferOut);

            return bufferOut[2];
        }

        public bool WritePortOE(FIC2USB_DIOPort DIOPort, byte PortOE)
        {
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];

            bufferIn[0] = 209;
            bufferIn[1] = checked((byte) DIOPort);
            bufferIn[2] = PortOE;
            bufferIn[3] = 0;

            USBIO(ref bufferIn, ref bufferOut);

            var res = CheckResponse(bufferIn, bufferOut);

            return res;
        }

        public byte ReadPort(FIC2USB_DIOPort DIOPort)
        {
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];

            bufferIn[0] = 210;
            bufferIn[1] = checked((byte) DIOPort);
            bufferIn[2] = 0;
            bufferIn[3] = 0;

            USBIO(ref bufferIn, ref bufferOut);

            var res = CheckResponse(bufferIn, bufferOut);

            return bufferOut[2];
        }

        public bool WritePort(FIC2USB_DIOPort DIOPort, byte PortData)
        {
            var bufferOut = new byte[4];
            var bufferIn  = new byte[4];

            bufferIn[0] = 211;
            bufferIn[1] = checked((byte) DIOPort);
            bufferIn[2] = PortData;
            bufferIn[3] = 0;

            USBIO(ref bufferIn, ref bufferOut);

            var res = CheckResponse(bufferIn, bufferOut);

            return res;
        }
        #endregion
    }

    #region enums
    public enum MSC_CMD
    {
        FRV_MSCC_REQ_SAVE_0       = 0x10,
        FRV_MSCC_REQ_SAVE_2       = 0x11,
        FRV_MSCC_REQ_SAVE_4       = 0x12,
        FRV_MSCC_REQ_SAVE_6       = 0x13,
        FRV_MSCC_REQ_SAVE_8       = 0x14,
        FRV_MSCC_REQ_SAVE_10      = 0x15,
        FRV_MSCC_REQ_LOAD_0       = 0x20,
        FRV_MSCC_REQ_LOAD_2       = 0x21,
        FRV_MSCC_REQ_LOAD_4       = 0x22,
        FRV_MSCC_REQ_LOAD_6       = 0x23,
        FRV_MSCC_REQ_LOAD_8       = 0x24,
        FRV_MSCC_REQ_LOAD_10      = 0x25,
        FRV_MSCC_REQ_EXEC_LOAD_0  = 0x30,
        FRV_MSCC_REQ_EXEC_LOAD_2  = 0x31,
        FRV_MSCC_REQ_EXEC_LOAD_4  = 0x32,
        FRV_MSCC_REQ_EXEC_LOAD_6  = 0x33,
        FRV_MSCC_REQ_EXEC_LOAD_8  = 0x34,
        FRV_MSCC_REQ_EXEC_LOAD_10 = 0x35,
        FRV_MSCC_REQ_I2C_START    = 0x39,
        FRV_MSCC_REQ_I2C_STOP     = 0x3A,
        FRV_MSCC_RESET_STUS       = 0x3C,
        FRV_MSCC_RESET_SYNC       = 0x3E,
        FRV_MSCC_RESET_NOW        = 0x3F
    }

    public enum MSC_STUS
    {
        FBM_MSCS_BUSY         = 0x80,
        FBM_MSCS_STUS_PND     = 0x40,
        FBM_MSCS_FSB_FACK_ERR = 0x20,
        FBM_MSCS_FSB_PCOL_ERR = 0x10,
        FBM_MSCS_FSB_CRC_ERR  = 0x8,
        FBM_MSCS_FSB_TIMEOUT  = 0x4,
        FBM_MSCS_ERROR        = 0x2,
        FBM_MSCS_DONE         = 0x1
    }

    //MSC Request Block Opcodes
    public enum MSC_OPCODE
    {
        MSCO_FSB_WR_WO_CRC       = 0x12,
        MSCO_FSB_RD_WO_CRC       = 0x16,
        MSCO_FSB_WR_W_CRC        = 0x10,
        MSCO_FSB_RD_W_CRC        = 0x14,
        MSCO_FSB_XWR_WO_CRC      = 0x13,
        MSCO_FSB_XRD_WO_CRC      = 0x17,
        MSCO_FSB_XWR_W_CRC       = 0x11,
        MSCO_FSB_XRD_W_CRC       = 0x15,
        MSCO_SPI_IMM             = 0x30,
        MSCO_SPI_IMM_RST_CS      = 0x34,
        MSCO_SPI_SEQ_RD          = 0x39,
        MSCO_SPI_SEQ_WR          = 0x3A,
        MSCO_SPI_SEQ_WRRD        = 0x3B,
        MSCO_SPI_SEQ_RD_RST_CS   = 0x3D,
        MSCO_SPI_SEQ_WR_RST_CS   = 0x3E,
        MSCO_SPI_SEQ_WRRD_RST_CS = 0x3F
    }

    public enum GPIO_CFG_DSEL
    {
        FRV_HWP_GPIOR   = 0,
        FRV_LO_FSB0C    = 1,
        FRV_LB_FSB0D    = 2,
        FRV_LO_FSB1C    = 3,
        FRV_LB_FSB1D    = 4,
        FRV_LO_SPI_MCS0 = 5,
        FRV_LO_SPI_MCS1 = 6,
        FRV_LO_SPI_MCS2 = 7,
        FRV_LO_SPI_MCS3 = 8,
        FRV_LB_SPI_MCK  = 9,
        FRV_LB_SPI_MD   = 10,
        FRV_LI_SPI_SCS  = 11,
        FRV_LB_SPI_SCK  = 12,
        FRV_LB_SPI_SD   = 13,
        FRV_LO_PWM      = 14,
        FRV_LO_CLK_DIV4 = 15
    }

    public enum GPIO_CFG_TYPE
    {
        FRV_IOB_IS_INPUT    = 0,
        FRV_IOB_IS_OUTPUT   = 1,
        FRV_IOB_IS_BIDIR    = 2,
        FRV_IOB_IS_OD_BIDIR = 3
    }

    public enum FIC2USB_CommResult
    {
        CommNull  = 0,
        CommPass  = 1,
        CommFail  = 2,
        CommError = 3
    }

    public enum FIC2USB_I2CPort
    {
        I2CPort1 = 0,
        I2CPort2 = 1,
        I2CPort3 = 2,
        I2CPort4 = 3,
        I2CPort5 = 4,
        I2CPort6 = 5,
        I2CPort7 = 6,
        I2CPort8 = 7
    }

    public enum FIC2USB_I2CSpeed
    {
        LS100Khz = 0,
        HS400Khz = 1
    }

    public enum FIC2USB_FSB2WPort
    {
        FSB2WPort1 = 0,
        FSB2WPort2 = 1,
        FSB2WPort3 = 2
    }

    public enum FIC2USB_FSB4WPort
    {
        FSB4W_CS0 = 0,
        FSB4W_CS1 = 1,
        FSB4W_CS2 = 2,
        FSB4W_CS3 = 3
    }

    public enum FIC2USB_FSBSpeed
    {
        FSB_MCK_5Mhz    = 0,
        FSB_MCK_2p5Mhz  = 1,
        FSB_MCK_1p6Mhz  = 2,
        FSB_MCK_1p25Mhz = 3,
        FSB_MCK_1Mhz    = 4,
        FSB_MCK_833Khz  = 5,
        FSB_MCK_714Khz  = 6,
        FSB_MCK_625Khz  = 7,
        FSB_MCK_556Khz  = 8,
        FSB_MCK_500Khz  = 9,
        FSB_MCK_455Khz  = 10,
        FSB_MCK_417Khz  = 11,
        FSB_MCK_385Khz  = 12,
        FSB_MCK_357Khz  = 13,
        FSB_MCK_333Khz  = 14,
        FSB_MCK_312Khz  = 15
    }

    public enum FIC2USB_SPIPort
    {
        SPIChipSel0 = 0,
        SPIChipSel1 = 1,
        SPIChipSel2 = 2,
        SPIChipSel3 = 3
    }

    public enum FIC2USB_SPISpeed
    {
        SPI_MCK_5Mhz    = 0,
        SPI_MCK_2p5Mhz  = 1,
        SPI_MCK_1p6Mhz  = 2,
        SPI_MCK_1p25Mhz = 3,
        SPI_MCK_1Mhz    = 4,
        SPI_MCK_833Khz  = 5,
        SPI_MCK_714Khz  = 6,
        SPI_MCK_625Khz  = 7,
        SPI_MCK_556Khz  = 8,
        SPI_MCK_500Khz  = 9,
        SPI_MCK_455Khz  = 10,
        SPI_MCK_417Khz  = 11,
        SPI_MCK_385Khz  = 12,
        SPI_MCK_357Khz  = 13,
        SPI_MCK_333Khz  = 14,
        SPI_MCK_312Khz  = 15
    }

    public enum FIC2USB_DIOPortBit
    {
        Bit0 = 0,
        Bit1 = 1,
        Bit2 = 2,
        Bit3 = 3,
        Bit4 = 4,
        Bit5 = 5,
        Bit6 = 6,
        Bit7 = 7
    }

    public enum FIC2USB_DIOPort
    {
        DIOPortA = 1,
        DIOPortB = 2,
        DIOPortC = 3,
        DIOPortD = 4,
        DIOPortE = 5
    }

    public enum FIC2USB_BulkDataType
    {
        BulkDataConstant = 0,
        BulkDataRandom   = 1,
        BulkDataIncByte  = 2,
        BulkDataIncDWord = 3
    }

    public enum FIC2USB_Endpoints
    {
        EPOut1In1 = 1,
        EPOut2In6 = 2,
        EPOut4In8 = 3
    }
    #endregion
}
