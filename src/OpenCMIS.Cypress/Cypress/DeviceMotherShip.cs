namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Instrument driver of the Mothership
    /// </summary>
    public sealed class DeviceMotherShip : EZUSBDevice
    {
        private const byte CMD_I2CREAD      = 0x01;
        private const byte CMD_I2CREAD16B   = 0x13;
        private const byte CMD_I2CWRITE     = 0x00;
        private const byte CMD_I2CTWOWRITES = 0x11;
        private const byte CMD_I2CFREQ      = 0x9;
        private const byte CMD_I2CMUXRESET  = 0x12;

        // endpoint configuration
        private const byte OUT_ENDPOINTADDR = 0x04; // OUT from USB

        private const byte IN_ENDPOINTADDR = 0x88; // IN to USB

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeviceMotherShip" /> class.
        /// </summary>
        /// <param name="usbDeviceInstance">The usb device instance.</param>
        public DeviceMotherShip(USBDevice usbDeviceInstance)
                : base(usbDeviceInstance)
        {
            DeviceType = DeviceType.DeviceMotherShip;
            var device = (CyUSBDevice) _usbDevice;
            CyUsbEndPointOut = device.EndPointOf(OUT_ENDPOINTADDR);
            CyUsbEndPointIn  = device.EndPointOf(IN_ENDPOINTADDR);
            SerialNumber     = GetSerailNumber();
        }

        // for MotherShip - serial number descriptor returns:
        //     XXXXX-YYYY
        //          where X.XX.XX  >> Cypress FW version (e.g. 1.05.00)
        //                YYYY     >> board serial number (e.g. S005)
        public override string GetSerailNumber()
        {
            var retValue = string.Empty;
            if (_usbDevice != null)
            {
                var tempSN = _usbDevice.SerialNumber;
                var i      = tempSN.IndexOf('-');
                if (i > 0)
                    retValue = tempSN.Substring(i + 1);
            }

            return retValue;
        }

        // for MotherShip - serial number descriptor returns:
        //     XXXXX-YYYY
        //          where GX.XX.XX  >> Cypress FW version (e.g. G1.05.00)
        //                YYYY     >> board serial number (e.g. S005)
        /// <summary>
        ///     Gets the cypress fw version.
        /// </summary>
        /// <returns></returns>
        public override string GetCypressFWVersion()
        {
            var retValue = string.Empty;
            if (_usbDevice != null)
            {
                var tempSN = _usbDevice.SerialNumber;
                var i      = tempSN.IndexOf('-');
                if (i > 0)
                {
                    retValue = tempSN.Substring(0, 5);
                    retValue = "G" + retValue.Insert(1, ".").Insert(4, ".");
                }
            }

            return retValue;
        }

        //    EUI RETURN CODES:
        //0 - SUCCESS
        //1 - Unable to open device/invalid device instance
        //2 - Incorrect ProductID or VendorID
        //3 - Incorrect parameter value passed
        //4 - error during transferring data at end points
        //5 -
        //6 - i2c bit error
        //7 - i2c no acknowledge
        //8 - i2c ok (same with SUCCESS)

        // By default, SCL is driven at ~100 kHz - setting I2CTL bit 0 to 1 causes the EZ-USB to drive SCL at ~400 kHz.
        /// <summary>
        ///     I2s the c freqency400 k hz.
        /// </summary>
        /// <param name="freq400KHz">if set to <c>true</c> [freq400 k hz].</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="CyXferDataEndPointException">
        ///     Command error - USB Device  + ProductName
        ///     or
        ///     USB Device  + ProductName
        /// </exception>
        public void I2CFreqency400KHz(bool freq400KHz)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            var length    = 8;
            var bufferOut = new byte[length];
            var bufferIn  = new byte[length];

            bufferOut[0] = CMD_I2CFREQ;
            bufferOut[1] = Convert.ToByte(freq400KHz); // true = 400 kHz, false = 100 kHz
            bufferOut[2] = 0;
            bufferOut[3] = 0;
            bufferOut[4] = 0;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];

                //dataArray = new byte[length];
                if (retValue != 0)
                    throw new CyXferDataEndPointException("Command error - USB Device " + ProductName);
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     I2s the cmux reset.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="CyXferDataEndPointException">
        ///     Command error - USB Device  + ProductName
        ///     or
        ///     USB Device  + ProductName
        /// </exception>
        public void I2CMUXReset()
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            var length    = 8;
            var bufferOut = new byte[length];
            var bufferIn  = new byte[length];

            bufferOut[0] = CMD_I2CMUXRESET;
            bufferOut[1] = 0;
            bufferOut[2] = 0;
            bufferOut[3] = 0;
            bufferOut[4] = 0;

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref length))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];

                //dataArray = new byte[length];
                if (retValue != 0)
                    throw new CyXferDataEndPointException("Command error - USB Device " + ProductName);
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     I2s the c read.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CRead(byte deviceAddress, ref byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 5)

                    //MessageBox.Show("Exceed number of bytes allowed to read.");
                    //dataArray = new byte[0];
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = 5;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CREAD;
            bufferOut[1] = (byte) (deviceAddress >> 1);
            bufferOut[2] = 0; // don't care for current read mode
            bufferOut[3] = (byte) byteLength;
            bufferOut[4] = 1; // current address read mode

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];

                //dataArray = new byte[length];
                if (retValue == 0)
                {
                    // fetch data
                    var j = 1;
                    for (var i = 0; i < byteLength; i++)
                    {
                        dataArray[i] = bufferIn[j];
                        j++;
                    }
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else

                    //dataArray = new byte[0];
                    //retValue = 4;  //throw new Exception("Transaction not successfully completed.");
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        // I2C read with 16-bit internal address
        /// <summary>
        ///     I2s the c read.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="internalAddress">The internal address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CRead(byte deviceAddress, ushort internalAddress, ref byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 5)

                    //MessageBox.Show("Exceed number of bytes allowed to read.");
                    //dataArray = new byte[0];
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = 6;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CREAD16B;
            bufferOut[1] = (byte) (deviceAddress >> 1);
            bufferOut[2] =
                    (byte) (internalAddress >> 8);          // high byte first  >>> WORKING correctly per Yuehao Peng 7/12/2012
            bufferOut[3] = (byte) (internalAddress & 0xFF); // low byte next    >>>
            bufferOut[4] = (byte) byteLength;
            bufferOut[5] = 0; // random address read mode

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];

                //dataArray = new byte[length];
                if (retValue == 0)
                {
                    var j = 1;
                    for (var i = 0; i < byteLength; i++)
                    {
                        dataArray[i] = bufferIn[j];
                        j++;
                    }
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     I2s the c read.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="internalAddress">The internal address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CRead(byte deviceAddress, byte internalAddress, ref byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 5)

                    //MessageBox.Show("Exceed number of bytes allowed to read.");
                    //dataArray = new byte[0];
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = 5;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CREAD;
            bufferOut[1] = (byte) (deviceAddress >> 1);
            bufferOut[2] = internalAddress;
            bufferOut[3] = (byte) byteLength;
            bufferOut[4] = 0; // random address read mode

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];

                //dataArray = new byte[length];
                if (retValue == 0)
                {
                    var j = 1;
                    for (var i = 0; i < byteLength; i++)
                    {
                        dataArray[i] = bufferIn[j];
                        j++;
                    }
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else

                    //dataArray = new byte[0];
                    //retValue = 4;  //throw new Exception("Transaction not successfully completed.");
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        /// <summary>
        ///     I2s the c write.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWrite(byte deviceAddress, byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 4)

                    //MessageBox.Show("Exceed number of bytes allowed to write.");
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = byteLength + 3;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CWRITE;
            bufferOut[1] = (byte) (deviceAddress >> 1);
            bufferOut[2] = dataArray[0];
            bufferOut[3] = (byte) byteLength;

            // propagate buffer with data starting from index 1
            for (var i = 1; i < byteLength; i++)
                bufferOut[i + 3] = dataArray[i];

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];
                if (retValue == 0)
                {
                    // success, do nothing
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else

                    //dataArray = new byte[0];
                    //retValue = 4;  //throw new Exception("Transaction not successfully completed.");
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        // I2C write with 16-bit internal address
        /// <summary>
        ///     I2s the c write.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="internalAddress">The internal address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWrite(byte deviceAddress, ushort internalAddress, byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 4)

                    //MessageBox.Show("Exceed number of bytes allowed to write.");
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = byteLength + 5;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CWRITE;
            bufferOut[1] = (byte) (deviceAddress   >> 1);
            bufferOut[2] = (byte) (internalAddress >> 8);   // address high first
            bufferOut[3] = (byte) (byteLength + 2);         // data includes 2-byte internalAddress
            bufferOut[4] = (byte) (internalAddress & 0xFF); // address low next

            // propagate buffer with data starting from index 0
            for (var i = 0; i < byteLength; i++)
                bufferOut[i + 5] = dataArray[i];

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];
                if (retValue == 0)
                {
                    // success, do nothing
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else
                throw new CyXferDataEndPointException("USB Device " + ProductName);
        }

        /// <summary>
        ///     I2s the c write.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="internalAddress">The internal address.</param>
        /// <param name="dataArray">The data array.</param>
        /// <param name="byteLength">Length of the byte.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">Device  + deviceAddress.ToString(X) +  may be disconnected.</exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWrite(byte deviceAddress, byte internalAddress, byte[] dataArray, int byteLength)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength > 256 - 4)

                    //MessageBox.Show("Exceed number of bytes allowed to write.");
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = byteLength + 4;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];

            bufferOut[0] = CMD_I2CWRITE;
            bufferOut[1] = (byte) (deviceAddress >> 1);
            bufferOut[2] = internalAddress;
            bufferOut[3] = (byte) (byteLength + 1); // data includes internalAddress

            // propagate buffer with data starting from index 0
            for (var i = 0; i < byteLength; i++)
                bufferOut[i + 4] = dataArray[i];

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];
                if (retValue == 0)
                {
                    // success, do nothing
                }
                else if (retValue == 6)
                    throw new I2CBitErrorException(" device " + deviceAddress.ToString("X"));
                else if (retValue == 7)
                    throw new I2CNoACKException("Device " + deviceAddress.ToString("X") + " may be disconnected.");
                else
                    throw new I2CAccessException(" device " + deviceAddress.ToString("X"));
            }
            else

                    //dataArray = new byte[0];
                    //retValue = 4;  //throw new Exception("Transaction not successfully completed.");
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }

        /// <summary>
        ///     I2s the c write two bytes.
        /// </summary>
        /// <param name="deviceAddress0">The device address0.</param>
        /// <param name="internalAddress0">The internal address0.</param>
        /// <param name="dataArray0">The data array0.</param>
        /// <param name="byteLength0">The byte length0.</param>
        /// <param name="deviceAddress1">The device address1.</param>
        /// <param name="internalAddress1">The internal address1.</param>
        /// <param name="dataArray1">The data array1.</param>
        /// <param name="byteLength1">The byte length1.</param>
        /// <param name="delayMS">The delay ms.</param>
        /// <exception cref="System.ArgumentNullException">EZUSBDevice is NULL.</exception>
        /// <exception cref="System.ArgumentException">Exceed MAX number of bytes allowed to read.</exception>
        /// <exception cref="I2CBitErrorException"> device  + deviceAddress0.ToString(X) + / + deviceAddress1.ToString(X)</exception>
        /// <exception cref="I2CNoACKException">
        ///     Device  + deviceAddress0.ToString(X) + / + deviceAddress1.ToString(X) +  may be
        ///     disconnected.
        /// </exception>
        /// <exception cref="I2CAccessException"> device  + deviceAddress0.ToString(X) + / + deviceAddress1.ToString(X)</exception>
        /// <exception cref="CyXferDataEndPointException">USB Device  + ProductName</exception>
        public void I2CWriteTwoBytes(byte   deviceAddress0,
                                     byte   internalAddress0,
                                     byte[] dataArray0,
                                     int    byteLength0,
                                     byte   deviceAddress1,
                                     byte   internalAddress1,
                                     byte[] dataArray1,
                                     int    byteLength1,
                                     int    delayMS)
        {
            if (_usbDevice == null)
            {
                throw new ArgumentNullException("Device instance is NULL, actual device may have been detached.",
                                                new Exception("EZUSBDevice is NULL"));
            }

            if (byteLength0 + byteLength1 > 256 - 8)

                    //MessageBox.Show("Exceed number of bytes allowed to write.");
                    //return 3;
                throw new ArgumentException("Exceed MAX number of bytes allowed to read.");

            var outLength = byteLength0 + byteLength1 + 8;
            var inLength  = 256;
            var bufferOut = new byte[outLength];
            var bufferIn  = new byte[inLength];
            var index     = 0;

            bufferOut[0] = CMD_I2CTWOWRITES;
            bufferOut[1] = (byte) (deviceAddress0 >> 1);
            bufferOut[2] = internalAddress0;
            bufferOut[3] = (byte) (byteLength0 + 1); // data includes internalAddress

            // propagate buffer with data
            for (var i = 0; i < byteLength0; i++)
                bufferOut[i + 4] = dataArray0[i];

            index                = byteLength0 + 4;
            bufferOut[index]     = (byte) delayMS; // delay after first write command in milliseconds
            bufferOut[index + 1] = (byte) (deviceAddress1 >> 1);
            bufferOut[index + 2] = internalAddress1;
            bufferOut[index + 3] = (byte) (byteLength1 + 1);

            // propagate buffer with data
            for (var j = 0; j < byteLength1; j++)
                bufferOut[j + index + 4] = dataArray1[j];

            // send PACKET
            if (CommandData(ref bufferIn, ref bufferOut, ref outLength, ref inLength))
            {
                // index 0 is the return code
                int retValue = bufferIn[0];
                if (retValue == 0)
                {
                    // success, do nothing
                }
                else if (retValue == 6)
                {
                    throw new I2CBitErrorException(" device " + deviceAddress0.ToString("X") + "/"
                                                 + deviceAddress1.ToString("X"));
                }
                else if (retValue == 7)
                {
                    throw new I2CNoACKException("Device "                    + deviceAddress0.ToString("X") + "/"
                                              + deviceAddress1.ToString("X") + " may be disconnected.");
                }
                else
                {
                    throw new I2CAccessException(" device " + deviceAddress0.ToString("X") + "/"
                                               + deviceAddress1.ToString("X"));
                }
            }
            else

                    //dataArray = new byte[0];
                    //retValue = 4;  //throw new Exception("Transaction not successfully completed.");
                throw new CyXferDataEndPointException("USB Device " + ProductName);

            //return retValue;
        }
    }
}
