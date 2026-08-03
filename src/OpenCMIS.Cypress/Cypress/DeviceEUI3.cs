namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Driver class of EUI3 device
    /// </summary>
    public class DeviceEUI3 : EZUSBDevice
    {
        private static readonly object lockdevice = new ();

        private ushort PortEnableSetting;
        private ushort PortValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeviceEUI3" /> class.
        /// </summary>
        /// <param name="usbDeviceInstance">The usb device instance.</param>
        public DeviceEUI3(USBDevice usbDeviceInstance)
                : base(usbDeviceInstance)
        {
            CyUsbEndPointOut = (_usbDevice as CyUSBDevice)?.EndPointOf(OUT_ENDPOINTADDR);
            CyUsbEndPointIn  = (_usbDevice as CyUSBDevice)?.EndPointOf(IN_ENDPOINTADDR);
            SerialNumber     = GetSerailNumber();

            PortValue         = 0;
            PortEnableSetting = 0;
        }

        public sealed override string GetSerailNumber()
        {
            return CyUtils.AsciiStringToDecString(_usbDevice.SerialNumber.Substring(6, 6), 0x30);
        }

        //ARR
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

        //_ARR

        /// <summary>
        ///     Gets the fpga fw version.
        /// </summary>
        /// <returns></returns>
        public override string GetFPGAFWVersion()
        {
            var retValue = string.Empty;
            var buffer   = new byte[512];
            var length   = 16;
            var version  = new string[8];

            buffer[0] = CMD_VERSION & 0x0F;
            buffer[1] = CMD_VERSION >> 8;

            // buffer[2 - 15]
            for (var i = 2; i < 2 + 14; i++)
                buffer[i] = 0;

            if (CyUsbEndPointOut.XferData(ref buffer, ref length) &&
                CyUsbEndPointIn.XferData(ref buffer, ref length))
            {
                version[0] = Convert.ToChar(buffer[3]).ToString();
                version[1] = Convert.ToString(buffer[2], 16);
                version[2] = ".";
                version[3] = Convert.ToString(buffer[5], 16);
                version[4] = Convert.ToString(buffer[4], 16);
                version[5] = ".";
                version[6] = Convert.ToString(buffer[7], 16);
                version[7] = Convert.ToString(buffer[6], 16);

                //for (int i = 0; i < version.Length; i++) retValue += version[i];
                retValue = string.Join("", version);
            }

            return retValue;
        }

        // returned ERROR CODES
        //0 - SUCCESS
        //1 - Unable to open device of current device index
        //2 - Incorrect ProductID or VendorID
        //3 - Incorrect parameter value passed
        //4 - Invalid returned packet/ discrepancies found in return packet
        //5 - Invalid turn-around bit
        //6 - i2c bit error
        //7 - i2c no acknowledge
        //8 - i2c ok (same with SUCCESS)
        //9 - USB transaction not successful

        //ARR
        /// <summary>
        ///     Mdioes the write.
        /// </summary>
        /// <param name="sel">The sel.</param>
        /// <param name="port">The port.</param>
        /// <param name="device">The device.</param>
        /// <param name="address">The address.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void MDIOWrite(byte sel, byte port, byte device, uint address, uint data)
        {
            // packet size only 512 bytes, max data only 31

            var bufferOut = new byte[16 + 8];
            var bufferIn  = new byte[16 + 8];

            //Word 0:  Command Code (0x0001)
            //Word 1:  ST(15~14) + OP(13~12) + Port(11~7) + Device(6~2) + TA(1~0)
            //Word 2:  16-bit DATA/ADDRESS
            //Word 3:  OP Code - ADDR(0x00) / WRITE(0x01) / READ INC(0x02) / READ(0x03)

            // ADDR packet
            bufferOut[0] = CMD_MDIOMASTER & 0xFF;
            bufferOut[1] = CMD_MDIOMASTER >> 8;
            bufferOut[2] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x02);
            bufferOut[3] = (byte) (0x00               | (port   & 0x1F) >> 1); // (ST is always 0 | ADDR) = 0x00
            bufferOut[4] = (byte) (address & 0xFF);
            bufferOut[5] = (byte) ((address & 0xFF00) >> 8);
            bufferOut[6] = 0x00; // ADDR

            //bufferOut[7] = 0x00;
            bufferOut[7] = (byte) (sel << 7);

            // WRITE data packet
            bufferOut[8]  = CMD_MDIOMASTER & 0xFF;
            bufferOut[9]  = CMD_MDIOMASTER >> 8;
            bufferOut[10] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x02);
            bufferOut[11] = (byte) (0x10               | (port   & 0x1F) >> 1); // (ST is always 0 | WRITE ) = 01b
            bufferOut[12] = (byte) (data & 0xFF);                               // DATA byte
            bufferOut[13] = (byte) ((data & 0xFF00) >> 8);                      // DATA byte
            bufferOut[14] = 0x01;                                               // WRITE OP - 01b

            //bufferOut[15] = 0x00;
            bufferOut[15] = (byte) (sel << 7);

            // END packet
            bufferOut[16] = 0x00;
            bufferOut[17] = 0x00;
            bufferOut[18] = 0x00;
            bufferOut[19] = 0x00;
            bufferOut[20] = 0x00;
            bufferOut[21] = 0x00;
            bufferOut[22] = 0x00;
            bufferOut[23] = 0x00;
            var length = 24;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // do the proper checking of returned bytes
                for (var i = 0; i < length; i++)
                        /*// exclude DATA (12, 13) & TA (10) fields
                            if ((((i - 12) % 16) != 0) && (((i - 13) % 16) != 0) && (((i - 10) % 16) != 0))
                            {
                                if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))
                                {
                                    //retValue = 3;   // throw new Exception("Invalid data returned.");
                                    //break
                                    MessageBox.Show("Sent & returned data packet are mismatched.");
                                    throw new CyPacketMismatchException();
                                }
                            }*/
                    if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                            //MessageBox.Show("Sent & returned data packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet are mismatched.");
                    }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        /// <summary>
        ///     Mdioes the read.
        /// </summary>
        /// <param name="sel">The sel.</param>
        /// <param name="port">The port.</param>
        /// <param name="device">The device.</param>
        /// <param name="address">The address.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void MDIORead(byte sel, byte port, byte device, uint address, ref uint data)
        {
            // packet size only 512 bytes, max data only 31

            var bufferOut = new byte[16 + 8];
            var bufferIn  = new byte[16 + 8];

            //Word 0:  Command Code (0x0001)
            //Word 1:  ST(15~14) + OP(13~12) + Port(11~7) + Device(6~2) + TA(1~0)
            //Word 2:  16-bit DATA/ADDRESS
            //Word 3:  OP Code - ADDR(0x00) / WRITE(0x01) / READ INC(0x02) / READ(0x03)

            // ADDR packet
            bufferOut[0] = CMD_MDIOMASTER & 0xFF;
            bufferOut[1] = CMD_MDIOMASTER >> 8;
            bufferOut[2] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x02);
            bufferOut[3] = (byte) (0x00               | (port   & 0x1F) >> 1); // (ST is always 0 | ADDR) = 0x00
            bufferOut[4] = (byte) (address & 0xFF);
            bufferOut[5] = (byte) ((address & 0xFF00) >> 8);
            bufferOut[6] = 0x00; // ADDR

            //bufferOut[7] = 0x00;
            bufferOut[7] = (byte) (sel << 7);

            // READ data packet
            bufferOut[8]  = CMD_MDIOMASTER & 0xFF;
            bufferOut[9]  = CMD_MDIOMASTER >> 8;
            bufferOut[10] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x03);
            bufferOut[11] = (byte) (0x30               | (port   & 0x1F) >> 1); // (ST is always 0 | READ ) = 0x03
            bufferOut[12] = 0x00;                                               // DATA byte
            bufferOut[13] = 0x00;                                               // DATA byte
            bufferOut[14] = 0x03;                                               // READ OP - 11b

            //bufferOut[15] = 0x00;
            bufferOut[15] = (byte) (sel << 7);

            // END packet
            bufferOut[16] = 0x00;
            bufferOut[17] = 0x00;
            bufferOut[18] = 0x00;
            bufferOut[19] = 0x00;
            bufferOut[20] = 0x00;
            bufferOut[21] = 0x00;
            bufferOut[22] = 0x00;
            bufferOut[23] = 0x00;
            var length = 24;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // do the proper checking of returned bytes
                for (var i = 0; i < length; i++)

                        // exclude DATA (12, 13) & TA (10) fields
                    if ((i - 12) % 16 != 0 && (i - 13) % 16 != 0 && (i - 10) % 16 != 0)
                    {
                        if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                                //MessageBox.Show("Sent & returned data packet are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet are mismatched.");
                        }
                    }

                //retValue = 3;   // throw new Exception("Invalid data returned.");
                //break;

                // fetch data
                data = (uint) bufferIn[13] << 8 | bufferIn[12];
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        /// <summary>
        ///     Mdioes the read inc.
        /// </summary>
        /// <param name="sel">The sel.</param>
        /// <param name="port">The port.</param>
        /// <param name="device">The device.</param>
        /// <param name="address">The address.</param>
        /// <param name="data">The data.</param>
        /// <param name="wordlength">The wordlength.</param>
        /// <exception cref="System.ArgumentException">Exceed MAX number of words allowed to read.</exception>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void MDIOReadInc(byte       sel,
                                byte       port,
                                byte       device,
                                uint       address,
                                ref uint[] data,
                                int        wordlength)
        {
            // packet size only 512 bytes, max data only 31
            if (wordlength > 62)

                    //MessageBox.Show("Exceeds allowable max number of data.");
                throw new ArgumentException("Exceed MAX number of words allowed to read.");

            //return 3;

            var bufferOut  = new byte[wordlength * 8 + 16];
            var bufferIn   = new byte[wordlength * 8 + 16];
            var addressInc = address;

            //Word 0:  Command Code (0x0001)
            //Word 1:  ST(15~14) + OP(13~12) + Port(11~7) + Device(6~2) + TA(1~0)
            //Word 2:  16-bit DATA/ADDRESS
            //Word 3:  OP Code - ADDR(0x00) / WRITE(0x01) / READ INC(0x02) / READ(0x03)

            // ADDR packet
            bufferOut[0] = CMD_MDIOMASTER & 0xFF;
            bufferOut[1] = CMD_MDIOMASTER >> 8;
            bufferOut[2] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x02);
            bufferOut[3] = (byte) (0x00               | (port   & 0x1F) >> 1); // (ST is always 0 | ADDR) = 0x00
            bufferOut[4] = (byte) (addressInc & 0xFF);
            bufferOut[5] = (byte) ((addressInc & 0xFF00) >> 8);
            bufferOut[6] = 0x00; // ADDR

            //bufferOut[7] = 0x00;
            bufferOut[7] = (byte) (sel << 7);

            for (var i = 0; i < wordlength; i++)
            {
                // READ data packet
                bufferOut[i * 8 + 8]  = CMD_MDIOMASTER & 0xFF;
                bufferOut[i * 8 + 9]  = CMD_MDIOMASTER >> 8;
                bufferOut[i * 8 + 10] = (byte) ((port & 0x01) << 7 | (device & 0x1F) << 2 | 0x02);
                bufferOut[i * 8 + 11] =
                        (byte) (0x20 | (port & 0x1F) >> 1); // (ST is always 0 | READ ) = 0x03
                bufferOut[i * 8 + 12] = 0x00;               // DATA byte
                bufferOut[i * 8 + 13] = 0x00;               // DATA byte
                bufferOut[i * 8 + 14] = 0x02;               // READ INC OP - 10b

                //bufferOut[(i * 8) + 15] = 0x00;
                bufferOut[i * 8 + 15] = (byte) (sel << 7);

                // increment address
                addressInc++;
            }

            var index = 8 * wordlength + 8;

            // END packet
            bufferOut[index]     = 0x00;
            bufferOut[index + 1] = 0x00;
            bufferOut[index + 2] = 0x00;
            bufferOut[index + 3] = 0x00;
            bufferOut[index + 4] = 0x00;
            bufferOut[index + 5] = 0x00;
            bufferOut[index + 6] = 0x00;
            bufferOut[index + 7] = 0x00;
            var length = index + 8;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // do the proper checking of returned bytes
                for (var i = 0; i < length; i++)

                        // exclude DATA (4, 5) & TA (2) fields, include all of address packet
                    if ((i - 4) % 8 != 0 && (i - 5) % 8 != 0 && (i - 2) % 8 != 0 || i < 8)
                    {
                        if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                                //MessageBox.Show("Sent & returned data packet are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet are mismatched.");
                        }
                    }

                //retValue = 3;   // throw new Exception("Invalid data returned.");
                //break;

                // fetch data
                for (var i = 0; i < wordlength; i++)
                    data[i] = (uint) bufferIn[13 + i * 8] << 8 | bufferIn[12 + i * 8];
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        //_ARR

        //ARR
        /// <summary>
        ///     I2s the c init.
        /// </summary>
        /// <exception cref="CyPacketMismatchException">
        ///     Sent and returned data packet Command ID are mismatched.
        ///     or
        ///     Sent and returned data END packet are mismatched.
        /// </exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CInit()
        {
            var bufferOut = new byte[2 * 8];
            var bufferIn  = new byte[2 * 8];
            var length    = 16;

            //int i = 0;

            /////////////////////////////////////////////////////////////////////////////////////////////////
            //Word_1: Command ID : 0x0008
            //Word_2: data_1 : N/A + OP
            //Word_3: data_2 : N/A + CONFIGURE
            //Word_4: data_3 : DATA
            /////////////////////////////////////////////////////////////////////////////////////////////////

            // I2C CONFIG T_BUF packet
            bufferOut[0] = CMD_I2C & 0xFF;
            bufferOut[1] = CMD_I2C >> 8;
            bufferOut[2] = I2C_SETUP & 0xFF;
            bufferOut[3] = I2C_SETUP >> 8;
            bufferOut[4] = 0x0A; //T_BUF
            bufferOut[5] = 0x00;
            bufferOut[6] = 0x58; // (LSB) 30us (Default 10us does not work for I2CRead() on XFP modules)
            bufferOut[7] = 0x02; // (MSB)

            // END packet
            bufferOut[8]  = 0x00;
            bufferOut[9]  = 0x00;
            bufferOut[10] = 0x00;
            bufferOut[11] = 0x00;
            bufferOut[12] = 0x00;
            bufferOut[13] = 0x00;
            bufferOut[14] = 0x00;
            bufferOut[15] = 0x00;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                ///////////////////////////////////////////////////////////////////////////////////
                //                           Check I2C Data
                ///////////////////////////////////////////////////////////////////////////////////
                //Step 1:the Command ID should be the same
                int j;
                for (j = 0; j < length - 8 - 1; j += 8)
                    if (bufferIn[j] != bufferOut[j])
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet Command ID are mismatched.");
                    }

                if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                {
                    throw new CyPacketMismatchException(
                            "Sent & returned data END packet are mismatched.");
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 50 instead
        /// <summary>
        ///     I2C write to FCC chips in an ICC chain
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="address">The address array in bytes, MSB = address[0]</param>
        /// <param name="dataArray">The data array in bytes</param>
        /// <param name="byteLength">Length of the byte array</param>
        /// <param name="addrLength">Length of the address.</param>
        public void I2CWrite(byte device, byte[] address, byte[] dataArray, int byteLength, int addrLength)
        {
            lock (lockdevice)
            {
                // maximum USB packet size - 512 bytes
                var maxDataByte = 64 - (addrLength + 3);
                if (byteLength > maxDataByte)

                        //MessageBox.Show("Exceeds allowable max number of data.");
                    throw new ArgumentException("Exceed MAX number of words allowed to write.");

                //return 3;

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + (addrLength + 3) * 8];
                var bufferIn  = new byte[byteLength * 8 + (addrLength + 3) * 8];
                int i;
                int k;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & device & 0xFF);
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                // data ADDRESS packet
                var addrIndex = 8;
                for (k = 0; k < addrLength; k++)
                {
                    bufferOut[addrIndex + 8 * k]     = CMD_I2C & 0xFF;
                    bufferOut[addrIndex + 8 * k + 1] = CMD_I2C >> 8;
                    bufferOut[addrIndex + 8 * k + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[addrIndex + 8 * k + 3] = (byte) (CMD_I2C_W >> 8 & address[k]);
                    bufferOut[addrIndex + 8 * k + 4] = 0x00;
                    bufferOut[addrIndex + 8 * k + 5] = 0x00;
                    bufferOut[addrIndex + 8 * k + 6] = 0x00;
                    bufferOut[addrIndex + 8 * k + 7] = 0x00;
                }

                var dataIndex = addrIndex + 8 * k; // start index of DATA packet

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to write ((1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = (byte) (CMD_I2C_W >> 8 & dataArray[i] & 0xFF);
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i; // start index of STOP packet

                // STOP packet (with DATA at byteLength)
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_W_STP & 0xFF;
                bufferOut[stopIndex + 3] = (byte) (CMD_I2C_W_STP >> 8 & dataArray[i] & 0xFF);
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])

                                //MessageBox.Show("Sent & returned data packet Command ID are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                            //MessageBox.Show("Sent & returned data END packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device and Start ACK
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);

                //return retValue;
            }
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 50 instead
        /// <summary>
        ///     I2s the c write.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="address">The address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <param name="addrLength">Length of the address.</param>
        /// <exception cref="System.ArgumentException">Exceed MAX number of words allowed to write.</exception>
        /// <exception cref="CyPacketMismatchException">
        ///     Sent and returned data packet Command ID are mismatched.
        ///     or
        ///     Sent and returned data END packet are mismatched.
        /// </exception>
        /// <exception cref="I2CNoACKException">Device  + device.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException">I2C Slave Clock Stretch overflow Error</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWrite(byte device, int address, byte[] dataArray, int byteLength, int addrLength)
        {
            lock (lockdevice)
            {
                // maximum USB packet size - 512 bytes
                var maxDataByte = 64 - (addrLength + 3);
                if (byteLength > maxDataByte)

                        //MessageBox.Show("Exceeds allowable max number of data.");
                    throw new ArgumentException("Exceed MAX number of words allowed to write.");

                //return 3;

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + (addrLength + 3) * 8];
                var bufferIn  = new byte[byteLength * 8 + (addrLength + 3) * 8];
                int i;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & device & 0xFF);
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                // ADDRESS packet
                var addrIndex = 8;
                if (addrLength == 2)
                {
                    bufferOut[addrIndex + 0] = CMD_I2C & 0xFF;
                    bufferOut[addrIndex + 1] = CMD_I2C >> 8;
                    bufferOut[addrIndex + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[addrIndex + 3] = (byte) (CMD_I2C_W >> 8 & address >> 8 & 0xFF);
                    bufferOut[addrIndex + 4] = 0x00;
                    bufferOut[addrIndex + 5] = 0x00;
                    bufferOut[addrIndex + 6] = 0x00;
                    bufferOut[addrIndex + 7] = 0x00;

                    // start index of LSB address packet
                    addrIndex = 16;
                }

                bufferOut[addrIndex + 0] = CMD_I2C & 0xFF;
                bufferOut[addrIndex + 1] = CMD_I2C >> 8;
                bufferOut[addrIndex + 2] = CMD_I2C_W & 0xFF;
                bufferOut[addrIndex + 3] = (byte) (CMD_I2C_W >> 8 & address & 0xFF);
                bufferOut[addrIndex + 4] = 0x00;
                bufferOut[addrIndex + 5] = 0x00;
                bufferOut[addrIndex + 6] = 0x00;
                bufferOut[addrIndex + 7] = 0x00;

                var dataIndex = addrIndex + 8; // start index of DATA packet

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to write ((1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = (byte) (CMD_I2C_W >> 8 & dataArray[i] & 0xFF);
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i; // start index of STOP packet

                // STOP packet (with DATA at byteLength)
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_W_STP & 0xFF;
                bufferOut[stopIndex + 3] = (byte) (CMD_I2C_W_STP >> 8 & dataArray[i] & 0xFF);
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])

                                //MessageBox.Show("Sent & returned data packet Command ID are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                            //MessageBox.Show("Sent & returned data END packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device and Start ACK
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);
            }

            //return retValue;
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 60 instead
        /// <summary>
        ///     I2s the c write.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentException">Exceed MAX number of words allowed to write.</exception>
        /// <exception cref="CyPacketMismatchException">
        ///     Sent and  returned data packet Command ID are mismatched.
        ///     or
        ///     Sent and returned data END packet are mismatched.
        /// </exception>
        /// <exception cref="I2CNoACKException">Device  + device.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException">I2C Slave Clock Stretch overflow Error</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWrite(byte device, byte[] dataArray, int byteLength)
        {
            lock (lockdevice)
            {
                // maximum USB packet size - 512 bytes
                if (byteLength > 60)

                        //MessageBox.Show("Exceeds allowable max number of data.");
                    throw new ArgumentException("Exceed MAX number of words allowed to write.");

                //return 3;

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + 5 * 8];
                var bufferIn  = new byte[byteLength * 8 + 5 * 8];
                int i;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & device & 0xFF);
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                var dataIndex = 8;

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to write ((1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = (byte) (CMD_I2C_W >> 8 & dataArray[i] & 0xFF);
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i;

                // STOP packet (with DATA at byteLength)
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_W_STP & 0xFF;
                bufferOut[stopIndex + 3] = (byte) (CMD_I2C_W_STP >> 8 & dataArray[i] & 0xFF);
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])

                                //MessageBox.Show("Sent & returned data packet Command ID are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                            //MessageBox.Show("Sent & returned data END packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device and Start ACK
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);
            }

            //return retValue;
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 50 instead
        /// <summary>
        ///     I2C read to FCC chips in an ICC chain
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="address">The address array in bytes, MSB = address[0]</param>
        /// <param name="dataArray">The data array in bytes</param>
        /// <param name="byteLength">Length of the byte array</param>
        /// <param name="addrLength">Length of the address.</param>
        public void I2CRead(byte       device,
                            byte[]     address,
                            ref byte[] dataArray,
                            int        byteLength,
                            int        addrLength)
        {
            lock (lockdevice)
            {
                // maximum Read Size
                var maxDataByte = 64 - (addrLength + 4);
                if (byteLength > maxDataByte)

                        //MessageBox.Show("Exceeds allowable max number of data.");
                    throw new ArgumentException("Exceed MAX number of words allowed to read.");

                //return 3;

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + (addrLength + 4) * 8];
                var bufferIn  = new byte[byteLength * 8 + (addrLength + 4) * 8];
                int i;
                int k;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & device & 0xFF);
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                // data ADDRESS packet
                var addrIndex = 8;
                for (k = 0; k < addrLength; k++)
                {
                    bufferOut[addrIndex + 8 * k]     = CMD_I2C & 0xFF;
                    bufferOut[addrIndex + 8 * k + 1] = CMD_I2C >> 8;
                    bufferOut[addrIndex + 8 * k + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[addrIndex + 8 * k + 3] = (byte) (CMD_I2C_W >> 8 & address[k]);
                    bufferOut[addrIndex + 8 * k + 4] = 0x00;
                    bufferOut[addrIndex + 8 * k + 5] = 0x00;
                    bufferOut[addrIndex + 8 * k + 6] = 0x00;
                    bufferOut[addrIndex + 8 * k + 7] = 0x00;
                }

                // Re-START+DEVICE packet
                var devIndex = addrIndex + 8 * k;

                bufferOut[devIndex + 0] = CMD_I2C & 0xFF;
                bufferOut[devIndex + 1] = CMD_I2C >> 8;
                bufferOut[devIndex + 2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[devIndex + 3] = (byte) (CMD_I2C_W_STA >> 8 & (device | 0x01) & 0xFF); //READ bit
                bufferOut[devIndex + 4] = 0x00;
                bufferOut[devIndex + 5] = 0x00;
                bufferOut[devIndex + 6] = 0x00;
                bufferOut[devIndex + 7] = 0x00;

                var dataIndex = devIndex + 8; // start index of DATA packet

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to read (1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_R      & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = CMD_I2C_R >> 8 & 0xFF;
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i; // start index of STOP packet

                // STOP+DATA(byteLength) packet - LAST data byte
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_R_STP      & 0xFF;
                bufferOut[stopIndex + 3] = CMD_I2C_R_STP >> 8 & 0xFF;
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])

                                //MessageBox.Show("Sent & returned data packet Command ID are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                            //MessageBox.Show("Sent & returned data END packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device, DATA ACK and Last DATA NACK
                    for (j = 0;
                         j < length - 8 * 2 - 1;
                         j = j + 8) //The Last CDB is END packet. Second to Last CDB is Last DATA packet.
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    if ((bufferIn[j + 2] & 0x80) ==
                        0) //The Second to Last CDB is Last DATA Packet. Check for NACK
                            //  error - NO NACK
                        throw new I2CAccessException("No NACK received from I2C Slave");

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");

                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Retrieve I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    j = (2 + addrLength) * 8;        //3 is from the  "(STA)Device+Addr(STOP)+(Re-STA)Device"
                    for (i = 0; i < byteLength; i++) //The Last CDB is END Packet
                    {
                        dataArray[i] = bufferIn[j + 3]; //i2c data from the CDB packet
                        j            = j + 8;
                    }
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);
            }

            //return retValue;
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 50 instead
        /// <summary>
        ///     I2s the c read.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="address">The address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <param name="addrLength">Length of the address.</param>
        /// <exception cref="System.ArgumentException">Exceed MAX number of words allowed to read.</exception>
        /// <exception cref="CyPacketMismatchException">
        ///     Sent and returned data packet Command ID are mismatched.
        ///     or
        ///     Sent and returned data END packet are mismatched.
        /// </exception>
        /// <exception cref="I2CNoACKException">Device  + device.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException">
        ///     No NACK received from I2C Slave
        ///     or
        ///     I2C Slave Clock Stretch overflow Error
        /// </exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CRead(byte device, int address, ref byte[] dataArray, int byteLength, int addrLength)
        {
            lock (lockdevice)
            {
                // maximum Read Size
                var maxDataByte = 64 - (addrLength + 4);
                if (byteLength > maxDataByte)
                    throw new ArgumentException("Exceed MAX number of words allowed to read.");

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + (addrLength + 4) * 8];
                var bufferIn  = new byte[byteLength * 8 + (addrLength + 4) * 8];
                int i;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & device & 0xFF);
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                // ADDRESS packet
                var addrIndex = 8;
                if (addrLength == 2)
                {
                    bufferOut[addrIndex + 0] = CMD_I2C & 0xFF;
                    bufferOut[addrIndex + 1] = CMD_I2C >> 8;
                    bufferOut[addrIndex + 2] = CMD_I2C_W & 0xFF;
                    bufferOut[addrIndex + 3] = (byte) (CMD_I2C_W >> 8 & address >> 8 & 0xFF);
                    bufferOut[addrIndex + 4] = 0x00;
                    bufferOut[addrIndex + 5] = 0x00;
                    bufferOut[addrIndex + 6] = 0x00;
                    bufferOut[addrIndex + 7] = 0x00;

                    // start index of LSB address packet
                    addrIndex = 16;
                }

                bufferOut[addrIndex + 0] = CMD_I2C & 0xFF;
                bufferOut[addrIndex + 1] = CMD_I2C >> 8;
                bufferOut[addrIndex + 2] = CMD_I2C_W & 0xFF;
                bufferOut[addrIndex + 3] = (byte) (CMD_I2C_W >> 8 & address & 0xFF);
                bufferOut[addrIndex + 4] = 0x00;
                bufferOut[addrIndex + 5] = 0x00;
                bufferOut[addrIndex + 6] = 0x00;
                bufferOut[addrIndex + 7] = 0x00;

                // Re-START+DEVICE packet
                var devIndex = addrIndex + 8;
                bufferOut[devIndex + 0] = CMD_I2C & 0xFF;
                bufferOut[devIndex + 1] = CMD_I2C >> 8;
                bufferOut[devIndex + 2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[devIndex + 3] = (byte) (CMD_I2C_W_STA >> 8 & (device | 0x01) & 0xFF); //READ bit
                bufferOut[devIndex + 4] = 0x00;
                bufferOut[devIndex + 5] = 0x00;
                bufferOut[devIndex + 6] = 0x00;
                bufferOut[devIndex + 7] = 0x00;

                var dataIndex = devIndex + 8; // start index of DATA packet

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to read (1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_R      & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = CMD_I2C_R >> 8 & 0xFF;
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i; // start index of STOP packet

                // STOP+DATA(byteLength) packet - LAST data byte
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_R_STP      & 0xFF;
                bufferOut[stopIndex + 3] = CMD_I2C_R_STP >> 8 & 0xFF;
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device, DATA ACK and Last DATA NACK
                    for (j = 0;
                         j < length - 8 * 2 - 1;
                         j = j + 8) //The Last CDB is END packet. Second to Last CDB is Last DATA packet.
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    if ((bufferIn[j + 2] & 0x80) ==
                        0) //The Second to Last CDB is Last DATA Packet. Check for NACK
                            //  error - NO NACK
                        throw new I2CAccessException("No NACK received from I2C Slave");

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");

                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Retrieve I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    j = (2 + addrLength) * 8;        //3 is from the  "(STA)Device+Addr(STOP)+(Re-STA)Device"
                    for (i = 0; i < byteLength; i++) //The Last CDB is END Packet
                    {
                        dataArray[i] = bufferIn[j + 3]; //i2c data from the CDB packet
                        j            = j + 8;
                    }
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);
            }

            //return retValue;
        }

        //DOES NOT check for CDB command limit = 32
        //CHECKS byteLength limit = 59 instead
        /// <summary>
        ///     I2s the c read.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentException">Exceed MAX number of words allowed to read.</exception>
        /// <exception cref="CyPacketMismatchException">
        ///     Sent and returned data packet Command ID are mismatched.
        ///     or
        ///     Sent and returned data END packet are mismatched.
        /// </exception>
        /// <exception cref="I2CNoACKException">Device  + device.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException">
        ///     No NACK received from I2C Slave
        ///     or
        ///     I2C Slave Clock Stretch overflow Error
        /// </exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CRead(byte device, ref byte[] dataArray, int byteLength)
        {
            lock (lockdevice)
            {
                // maximum Read Size
                if (byteLength > 59)

                        //MessageBox.Show("Exceeds allowable max number of data.");
                    throw new ArgumentException("Exceed MAX number of words allowed to read.");

                //return 3;

                //int retValue = 0;
                var bufferOut = new byte[byteLength * 8 + 7 * 8];
                var bufferIn  = new byte[byteLength * 8 + 7 * 8];
                int i;

                /////////////////////////////////////////////////////////////////////////////////////////////////
                //Word_1: Command ID : 0x0008
                //Word_2: data_1 : (DATA+ACK) + STA + STP + DELAY + CLK_STR + (OV) + OP
                //Word_3: data_2 : N/A + N/A
                //Word_4: data_3 : N/A
                /////////////////////////////////////////////////////////////////////////////////////////////////

                // START+DEVICE packet
                bufferOut[0] = CMD_I2C & 0xFF;
                bufferOut[1] = CMD_I2C >> 8;
                bufferOut[2] = CMD_I2C_W_STA & 0xFF;
                bufferOut[3] = (byte) (CMD_I2C_W_STA >> 8 & (device | 0x01) & 0xFF); //READ bit
                bufferOut[4] = 0x00;
                bufferOut[5] = 0x00;
                bufferOut[6] = 0x00;
                bufferOut[7] = 0x00;

                var dataIndex = 8;

                for (i = 0; i < byteLength - 1; i++)
                {
                    // DATA to read (1 to byteLength-1) packet
                    bufferOut[dataIndex + 8 * i]     = CMD_I2C & 0xFF;
                    bufferOut[dataIndex + 8 * i + 1] = CMD_I2C >> 8;
                    bufferOut[dataIndex + 8 * i + 2] = CMD_I2C_R      & 0xFF;
                    bufferOut[dataIndex + 8 * i + 3] = CMD_I2C_R >> 8 & 0xFF;
                    bufferOut[dataIndex + 8 * i + 4] = 0x00;
                    bufferOut[dataIndex + 8 * i + 5] = 0x00;
                    bufferOut[dataIndex + 8 * i + 6] = 0x00;
                    bufferOut[dataIndex + 8 * i + 7] = 0x00;
                }

                var stopIndex = dataIndex + 8 * i;

                // STOP+DATA(byteLength) packet
                bufferOut[stopIndex]     = CMD_I2C & 0xFF;
                bufferOut[stopIndex + 1] = CMD_I2C >> 8;
                bufferOut[stopIndex + 2] = CMD_I2C_R_STP      & 0xFF;
                bufferOut[stopIndex + 3] = CMD_I2C_R_STP >> 8 & 0xFF;
                bufferOut[stopIndex + 4] = 0x00;
                bufferOut[stopIndex + 5] = 0x00;
                bufferOut[stopIndex + 6] = 0x00;
                bufferOut[stopIndex + 7] = 0x00;

                // END packet
                bufferOut[stopIndex + 8]  = 0x00;
                bufferOut[stopIndex + 9]  = 0x00;
                bufferOut[stopIndex + 10] = 0x00;
                bufferOut[stopIndex + 11] = 0x00;
                bufferOut[stopIndex + 12] = 0x00;
                bufferOut[stopIndex + 13] = 0x00;
                bufferOut[stopIndex + 14] = 0x00;
                bufferOut[stopIndex + 15] = 0x00;
                var length = stopIndex + 16;

                // send PACKET
                if (CommandData(ref bufferIn, ref bufferOut, ref length))
                {
                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Check I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    //Step 1:the Command ID should be the same
                    int j;
                    for (j = 0; j < length - 8 - 1; j = j + 8)
                        if (bufferIn[j] != bufferOut[j])

                                //MessageBox.Show("Sent & returned data packet Command ID are mismatched.");
                        {
                            throw new CyPacketMismatchException(
                                    "Sent & returned data packet Command ID are mismatched.");
                        }

                    if (bufferIn[j] != bufferOut[j]) //Compare the END Packet
                            //MessageBox.Show("Sent & returned data END packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data END packet are mismatched.");
                    }

                    //Step 2:the Device, DATA ACK and Last DATA NACK
                    for (j = 0;
                         j < length - 8 * 2 - 1;
                         j = j + 8) //The Last CDB is END packet. Second to Last CDB is Last DATA packet.
                        if ((bufferIn[j + 2] & 0x80) != 0)

                                // error - NO ACK
                        {
                            throw new I2CNoACKException(
                                    "Device " + device.ToString("X") + " may be disconnected.");
                        }

                    if ((bufferIn[j + 2] & 0x80) ==
                        0) //The Second to Last CDB is Last DATA Packet. Check for NACK
                            //  error - NO NACK
                        throw new I2CAccessException("No NACK received from I2C Slave");

                    //Step 3: CLK_STETCH_OVER_FLOW
                    for (j = 0; j < length - 8 - 1; j = j + 8) //The Last CDB is END Packet
                        if ((bufferIn[j + 2] & 0x04) != 0)

                                //error - clock stretch overflow
                            throw new I2CAccessException("I2C Slave Clock Stretch overflow Error");

                    ///////////////////////////////////////////////////////////////////////////////////
                    //                           Retrieve I2C Data
                    ///////////////////////////////////////////////////////////////////////////////////
                    j = 1 * 8;                       //1 is from the  "(STA)Device"
                    for (i = 0; i < byteLength; i++) //The Last CDB is END Packet
                    {
                        dataArray[i] = bufferIn[j + 3]; //i2c data from the CDB packet
                        j            = j + 8;
                    }
                }
                else
                    throw new CyXferDataEndPointException("USB Device " + ProductName);
            }

            //return retValue;
        }

        /// <summary>
        ///     I2s the c set frequency.
        /// </summary>
        /// <param name="Freq">The freq.</param>
        public void I2CSetFrequency(double Freq)
        {
            ushort[] SetData;

            if (Freq == 50) //50KHz
            {
                SetData = new ushort[]
                          {
                              0x00C9, 0x01CA, 0x00C1, 0x0101, 0x0184, 0x01D4, 0x01D7, 0x02F4, 0x046E, 0x0258, 0x0FA0,
                              0x0009,
                              0x000A
                          };
            }
            else if (Freq == 90) //90KHz
            {
                SetData = new ushort[]
                          {
                              0x0078, 0x00F0, 0x0078, 0x008C, 0x00A0, 0x0104, 0x0104, 0x0190, 0x0208, 0x0258, 0x0FA0,
                              0x0009,
                              0x000A
                          };
            }
            else if (Freq == 200) //200KHz
            {
                SetData = new ushort[]
                          {
                              0x0098, 0x00D8, 0x0030, 0x0040, 0x0060, 0x0074, 0x0075, 0x0170, 0x0271, 0x0258, 0x0FA0,
                              0x0009,
                              0x000A
                          };
            }
            else if (Freq == 400) //400KHz
            {
                SetData = new ushort[]
                          {
                              0x000D, 0x0028, 0x0014, 0x001A, 0x002C, 0x0037, 0x0038, 0x0050, 0x007B, 0x000F, 0x0FA0,
                              0x0009,
                              0x000A
                          };
            }
            else //90KHz-Default
            {
                SetData = new ushort[]
                          {
                              0x0078, 0x00F0, 0x0078, 0x008C, 0x00A0, 0x0104, 0x0104, 0x0190, 0x0208, 0x0258, 0x0FA0,
                              0x0009,
                              0x000A
                          };
            }

            var max  = 13;
            var data = new ushort[SetData.Length * max + 5];

            for (ushort i = 0; i < SetData.Length; i++)
            {
                data[i * 4]     = 0x08; //The default command code
                data[1 + i * 4] = 0x00; //Set-up the I2C specification
                data[2 + i * 4] = (ushort) (i + 1);
                data[3 + i * 4] = SetData[i];
            }

            data[SetData.Length * max + 1] = 0x00;
            data[SetData.Length * max + 2] = 0x00;
            data[SetData.Length * max + 3] = 0x00;
            data[SetData.Length * max + 4] = 0x00;

            ushort[] outbuf;
            CDBcomm(data, out outbuf, data.Length);
        }

        /// <summary>
        ///     Gpioes the write.
        /// </summary>
        /// <param name="Value">The value.</param>
        /// <param name="PortEnable">The port enable.</param>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void GPIOWrite(ushort Value, ushort PortEnable)
        {
            var bufferOut = new byte[16];
            var bufferIn  = new byte[16];
            PortEnableSetting = 0;
            PortValue         = 0;

            //Word 0:  Command Code (0x0010)
            //Word 1:  Pin value
            //Word 2:  Port enable
            //Word 3:  empty
            PortEnableSetting |= PortEnable; //OR with new enabled bits
            PortValue         &= (ushort) ~PortEnable;
            PortValue         |= Value;

            // GPIO Write packet
            bufferOut[0] = CMD_GPIOWRITE & 0xFF;
            bufferOut[1] = CMD_GPIOWRITE >> 8;
            bufferOut[2] = (byte) (PortValue & 0xFF);
            bufferOut[3] = (byte) (PortValue >> 8);
            bufferOut[4] = (byte) (PortEnableSetting & 0xFF);
            bufferOut[5] = (byte) (PortEnableSetting >> 8);
            bufferOut[6] = 0x00;
            bufferOut[7] = 0x00;

            // END packet
            bufferOut[8]  = 0x00;
            bufferOut[9]  = 0x00;
            bufferOut[10] = 0x00;
            bufferOut[11] = 0x00;
            bufferOut[12] = 0x00;
            bufferOut[13] = 0x00;
            bufferOut[14] = 0x00;
            bufferOut[15] = 0x00;

            var length = 16;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // do the proper checking of returned bytes
                for (var i = 0; i < 4; i++)
                    if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                            //MessageBox.Show("Sent & returned data packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet are mismatched.");
                    }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     Gpioes the read.
        /// </summary>
        /// <param name="MyportValue">The myport value.</param>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void GPIORead(ref int MyportValue)
        {
            var retValue  = 0;
            var bufferOut = new byte[16];
            var bufferIn  = new byte[16];

            //Word 0:  Command Code (0x0020)
            //Word 1:  Pin value
            //Word 2:  Port enable
            //Word 3:  empty

            // GPIO Read packet
            bufferOut[0] = CMD_GPIOREAD & 0xFF;
            bufferOut[1] = CMD_GPIOREAD >> 8;
            bufferOut[2] = 0x00;
            bufferOut[3] = 0x00;
            bufferOut[4] = 0x00;
            bufferOut[5] = 0x00;
            bufferOut[6] = 0x00;
            bufferOut[7] = 0x00;

            // END packet
            bufferOut[8]  = 0x00;
            bufferOut[9]  = 0x00;
            bufferOut[10] = 0x00;
            bufferOut[11] = 0x00;
            bufferOut[12] = 0x00;
            bufferOut[13] = 0x00;
            bufferOut[14] = 0x00;
            bufferOut[15] = 0x00;

            var length = 16;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // do the proper checking of returned bytes
                for (var i = 0; i < 2; i++)
                    if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                            //MessageBox.Show("Sent & returned data packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet are mismatched.");
                    }

                if (retValue == 0)
                {
                    MyportValue =   bufferIn[3];
                    MyportValue <<= 8;
                    MyportValue |=  bufferIn[2];
                    MyportValue <<= 8;
                    MyportValue |=  bufferIn[5];
                    MyportValue <<= 8;
                    MyportValue |=  bufferIn[4];
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     Set MDIO frequency.
        /// </summary>
        /// <param name="sel">The sel.</param>
        /// <param name="Freq">The freq in KHz</param>
        public void MDIOFrequency(byte sel, double Freq)
        {
            var bufferOut = new byte[16];
            var bufferIn  = new byte[16];
            if (Freq > 4000 || Freq <= 95)
                throw new ArgumentException("Wrong frequency setting for MDIO interface.");

            //The equation:  T=(n+1) * 84ns
            //   n (counter) = ((1/(T * 0.000000084)) - 1
            //MDIO Clock	Test Value	Clock Divide Counter
            //  4M	            4M	            0x02
            //  3M	            3.03M	        0x03
            //  2.5M	        2.4M	        0x04
            //  1M	            1M	            0x0B
            //  500K	        500K	        0x17

            //Word 0:  Command Code (0x0020)
            //Word 1:  Pin value
            //Word 2:  Port enable
            //Word 3:  empty
            var counter = Convert.ToByte(Math.Ceiling(1 / (Freq * 1000 * 0.000000084) - 1));

            // MDIO Freq setting
            bufferOut[0] = CMD_MDIOMASTER & 0xFF;
            bufferOut[1] = CMD_MDIOMASTER >> 8;
            bufferOut[2] = 0;
            bufferOut[3] = 0;
            bufferOut[4] = 0;
            bufferOut[5] = 0;
            bufferOut[6] = 1 << 2; // configuration (bit 2-3) = 1
            bufferOut[7] = counter;

            // END packet
            bufferOut[8]  = 0x00;
            bufferOut[9]  = 0x00;
            bufferOut[10] = 0x00;
            bufferOut[11] = 0x00;
            bufferOut[12] = 0x00;
            bufferOut[13] = 0x00;
            bufferOut[14] = 0x00;
            bufferOut[15] = 0x00;

            var length = 16;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))

                    // do the proper checking of returned bytes
            {
                for (var i = 0; i < length; i = i + 8)
                {
                    var temp = "";
                    if ((bufferIn[i]     & 0xFF) != (bufferOut[i]     & 0xFF) && bufferOut[i]     != 0x00 ||
                        (bufferIn[i + 1] & 0xFF) != (bufferOut[i + 1] & 0xFF) && bufferOut[i + 1] != 0x00)
                    {
                        for (var k = 0; k < length; k++)
                            temp = "bufferIn["                        + Convert.ToString(k) + "]=" +
                                   Convert.ToString(bufferIn[k], 16)  +
                                   "   bufferOut["                    + Convert.ToString(k) + "]=" +
                                   Convert.ToString(bufferOut[k], 16) +
                                   "\r\n";

                        //MessageBox.Show("Sent & returned data packet are mismatched MDIO frequency set.\r\n" + temp);
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet are mismatched MDIO frequency set.\r\n" + temp);
                    }
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     Cds the bcomm.
        /// </summary>
        /// <param name="InBuf">The in buf.</param>
        /// <param name="OutBuf">The out buf.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="CyPacketMismatchException">Sent and returned data packet are mismatched.</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void CDBcomm(ushort[] InBuf, out ushort[] OutBuf, int length)
        {
            var bufferOut = new byte[length * 2];
            var bufferIn  = new byte[length * 2];
            OutBuf = new ushort[length];
            int i;

            //Word 0:  Command Code (0x0020)
            //Word 1:  Pin value
            //Word 2:  Port enable
            //Word 3:  empty
            // send PACKET
            for (i = 0; i < length; i++)
            {
                bufferOut[2 * i]     = (byte) (InBuf[i] & 0xff);
                bufferOut[2 * i + 1] = (byte) (InBuf[i] >> 8);
                OutBuf[i]            = 0;
            }

            var len = length * 2;
            if (CommandData(ref bufferIn, ref bufferOut, ref len))
            {
                // do the proper checking of returned bytes
                for (i = 0; i < 2; i++)
                    if ((bufferIn[i] & 0xFF) != (bufferOut[i] & 0xFF))

                            //MessageBox.Show("Sent & returned data packet are mismatched.");
                    {
                        throw new CyPacketMismatchException(
                                "Sent & returned data packet are mismatched.");
                    }

                for (i = 0; i < length; i++)
                {
                    OutBuf[i] =   bufferIn[2 * i + 1];
                    OutBuf[i] <<= 8;
                    OutBuf[i] |=  bufferIn[2 * i];
                }
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        #region COMMAND words
        // COMMAND words
        private const int CMD_END = 0;

        private const int CMD_MDIOMASTER    = 0x0001;
        private const int CMD_I2CMASTER     = 0x0008;
        private const int CMD_GPIOWRITE     = 0x0010;
        private const int CMD_GPIOREAD      = 0x0020;
        private const int CMD_TIMINGCONTROL = 0x0100;
        private const int CMD_SERIALNO      = 0x2000;
        private const int CMD_LOOPBACK      = 0x4000;
        private const int CMD_VERSION       = 0x8000;

        //ARR
        private const int CMD_I2C = 0x0008;

        private const int CMD_I2CCONFIG = 0x0001;

        //private const int CMD_I2CCOMMAND = 0x0002;
        // Bit7   Bit6   Bit5   Bit4  ||   Bit3   Bit2   Bit1   Bit0
        // AK     STA    STP    DELAY ||  CLK_STR  OV   OP[1  :  0]
        private const int
                CMD_I2C_W_STA = 0xFFDA; //  1      1      0      1    ||    1      0      1       0

        private const int CMD_I2C_W = 0xFF9A; //  1      0      0      1    ||    1      0      1       0

        private const int
                CMD_I2C_W_STP = 0xFFBA; //  1      0      1      1    ||    1      0      1       0

        private const int CMD_I2C_R = 0xFF1A; //  0      0      0      1    ||    1      0      1       0

        private const int
                CMD_I2C_R_STP = 0xFFBA; //  1      0      1      1    ||    1      0      1       0

        //_ARR

        private const byte MDIO_ADDR    = 0x00;
        private const byte MDIO_WRITE   = 0x01;
        private const byte MDIO_READ    = 0x03;
        private const byte MDIO_READINC = 0x02;

        private const byte I2C_SETUP   = 0x00; // (3 bits)
        private const byte I2C_CHECK   = 0x01; // (3 bits)
        private const byte I2C_NORMAL  = 0x02; // (3 bits)
        private const byte I2C_CMD     = 0x02; // (2 bits)
        private const byte I2C_OVF     = 0x04;
        private const byte I2C_CLK_STR = 0x08;

        //ARR
        private const byte I2C_ACK = 0x80;

        private const byte I2C_STRETCH = 0x40;

        //_ARR

        // endpoint configuration
        private const byte OUT_ENDPOINTADDR = 0x02; // OUT from USB

        private const byte IN_ENDPOINTADDR = 0x86; // IN to USB
        #endregion COMMAND words
    }
}
