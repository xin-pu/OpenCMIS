/*
 ## Cypress CyUSB C# library source file (CyUSBConfig.cs)
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
    ///     The CyUSBConfig Class
    /// </summary>
    public class CyUSBConfig
    {
        private readonly CyUSBInterfaceContainer[] IntfcContainer;

        public CyUSBInterface[] Interfaces;

        internal unsafe CyUSBConfig(IntPtr handle, byte[] DescrData, CyControlEndPoint ctlEndPt)
        {
            // This contructore is to initialize usb2.0 device
            fixed (byte * buf = DescrData)
            {
                var ConfigDescr = (USB_CONFIGURATION_DESCRIPTOR *) buf;

                bLength             = ConfigDescr->bLength;
                bDescriptorType     = ConfigDescr->bDescriptorType;
                wTotalLength        = ConfigDescr->wTotalLength;
                bNumInterfaces      = ConfigDescr->bNumInterfaces;
                AltInterfaces       = 0;
                bConfigurationValue = ConfigDescr->bConfigurationValue;
                iConfiguration      = ConfigDescr->iConfiguration;
                bmAttributes        = ConfigDescr->bmAttributes;
                MaxPower            = ConfigDescr->MaxPower;

                int tLen = ConfigDescr->wTotalLength;

                var desc = buf + ConfigDescr->bLength;
                int bytesConsumed = ConfigDescr->bLength;

                Interfaces = new CyUSBInterface[CyConst.MAX_INTERFACES];

                var i = 0;
                do
                {
                    var interfaceDesc = (USB_INTERFACE_DESCRIPTOR *) desc;

                    if (interfaceDesc->bDescriptorType == CyConst.USB_INTERFACE_DESCRIPTOR_TYPE)
                    {
                        Interfaces[i] = new (handle, desc, ctlEndPt);
                        i++;
                        AltInterfaces++; // Actually the total number of interfaces for the config
                        bytesConsumed += Interfaces[i - 1].wTotalLength;
                    }
                    else
                            // Unexpected descriptor type
                            // Just skip it and go on  - could have thrown an exception instead
                            // since this indicates that the descriptor structure is invalid.
                        bytesConsumed += interfaceDesc->bLength;

                    desc = buf + bytesConsumed;
                } while (bytesConsumed < tLen && i < CyConst.MAX_INTERFACES);

                // Count the alt interfaces for each interface number
                for (i = 0; i < AltInterfaces; i++)
                {
                    Interfaces[i]._bAltSettings = 0;

                    for (var j = 0; j < AltInterfaces; j++) // Walk the list looking for identical bInterfaceNumbers
                        if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            Interfaces[i]._bAltSettings++;
                }

                // Create the Interface Container (this is done only for Tree view purpose).
                IntfcContainer = new CyUSBInterfaceContainer[bNumInterfaces];

                var altDict = new Dictionary<int, bool>();
                var intfcCount = 0;

                for (i = 0; i < AltInterfaces; i++)
                    if (!altDict.ContainsKey(Interfaces[i].bInterfaceNumber))
                    {
                        var altIntfcCount = 0;
                        IntfcContainer[intfcCount] = new (Interfaces[i].bInterfaceNumber, Interfaces[i].bAltSettings);

                        for (var j = i; j < AltInterfaces; j++)
                            if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            {
                                IntfcContainer[intfcCount].Interfaces[altIntfcCount] = Interfaces[j];
                                altIntfcCount++;
                            }

                        intfcCount++;
                        altDict.Add(Interfaces[i].bInterfaceNumber, true);
                    }
            } /* end of fixed loop */
        }

        internal unsafe CyUSBConfig(IntPtr handle, byte[] DescrData, CyControlEndPoint ctlEndPt, byte usb30Dummy)
        {
            // This constructure will be called for USB3.0 device initialization
            fixed (byte * buf = DescrData)
            {
                var ConfigDescr = (USB_CONFIGURATION_DESCRIPTOR *) buf;

                bLength             = ConfigDescr->bLength;
                bDescriptorType     = ConfigDescr->bDescriptorType;
                wTotalLength        = ConfigDescr->wTotalLength;
                bNumInterfaces      = ConfigDescr->bNumInterfaces;
                AltInterfaces       = 0;
                bConfigurationValue = ConfigDescr->bConfigurationValue;
                iConfiguration      = ConfigDescr->iConfiguration;
                bmAttributes        = ConfigDescr->bmAttributes;
                MaxPower            = ConfigDescr->MaxPower;

                int tLen = ConfigDescr->wTotalLength;

                var desc = buf + ConfigDescr->bLength;
                int bytesConsumed = ConfigDescr->bLength;

                Interfaces = new CyUSBInterface[CyConst.MAX_INTERFACES];

                var i = 0;
                do
                {
                    var interfaceDesc = (USB_INTERFACE_DESCRIPTOR *) desc;

                    if (interfaceDesc->bDescriptorType == CyConst.USB_INTERFACE_DESCRIPTOR_TYPE)
                    {
                        Interfaces[i] = new (handle, desc, ctlEndPt, usb30Dummy);
                        i++;
                        AltInterfaces++; // Actually the total number of interfaces for the config
                        bytesConsumed += Interfaces[i - 1].wTotalLength;
                    }
                    else
                            // Unexpected descriptor type
                            // Just skip it and go on  - could have thrown an exception instead
                            // since this indicates that the descriptor structure is invalid.
                        bytesConsumed += interfaceDesc->bLength;

                    desc = buf + bytesConsumed;
                } while (bytesConsumed < tLen && i < CyConst.MAX_INTERFACES);

                // Count the alt interfaces for each interface number
                for (i = 0; i < AltInterfaces; i++)
                {
                    Interfaces[i]._bAltSettings = 0;

                    for (var j = 0; j < AltInterfaces; j++) // Walk the list looking for identical bInterfaceNumbers
                        if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            Interfaces[i]._bAltSettings++;
                }

                // Create the Interface Container (this is done only for Tree view purpose).
                IntfcContainer = new CyUSBInterfaceContainer[bNumInterfaces];

                var altDict = new Dictionary<int, bool>();
                var intfcCount = 0;

                for (i = 0; i < AltInterfaces; i++)
                    if (!altDict.ContainsKey(Interfaces[i].bInterfaceNumber))
                    {
                        var altIntfcCount = 0;
                        IntfcContainer[intfcCount] = new (Interfaces[i].bInterfaceNumber, Interfaces[i].bAltSettings);

                        for (var j = i; j < AltInterfaces; j++)
                            if (Interfaces[i].bInterfaceNumber == Interfaces[j].bInterfaceNumber)
                            {
                                IntfcContainer[intfcCount].Interfaces[altIntfcCount] = Interfaces[j];
                                altIntfcCount++;
                            }

                        intfcCount++;
                        altDict.Add(Interfaces[i].bInterfaceNumber, true);
                    }
            } /* end of fixed loop */
        }

        public byte bLength { get; }

        public byte bDescriptorType { get; }

        public ushort wTotalLength { get; }

        public byte bNumInterfaces { get; }

        public byte bConfigurationValue { get; }

        public byte iConfiguration { get; }

        public byte bmAttributes { get; }

        public byte MaxPower { get; }

        public byte AltInterfaces { get; }

        public TreeNode Tree
        {
            get
            {
                var tmp = "Configuration " + bConfigurationValue;
                //string tmp = "Primary Configuration";
                //if (iConfiguration == 1)
                //    tmp = "Secondary Configuration";

                //TreeNode[] iTree = new TreeNode[_AltInterfaces + 1];
                var iTree = new TreeNode[bNumInterfaces + 1];

                iTree[0] = new ("Control endpoint (0x00)")
                {
                        Tag = Interfaces[0].EndPoints[0]
                };

                for (var i = 0; i < bNumInterfaces; i++)
                    iTree[i + 1] = IntfcContainer[i].Tree;

                //for (int i = 0; i < _AltInterfaces; i++)
                //    iTree[i + 1] = Interfaces[i].Tree;

                var t = new TreeNode(tmp, iTree)
                {
                        Tag = this
                };

                return t;
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder("\t<CONFIGURATION>\r\n");

            s.Append($"\t\tConfiguration=\"{iConfiguration}\"\r\n");
            s.Append($"\t\tConfigurationValue=\"{bConfigurationValue}\"\r\n");
            s.Append($"\t\tAttributes=\"{bmAttributes:X2}h\"\r\n");
            s.Append($"\t\tInterfaces=\"{bNumInterfaces}\"\r\n");
            s.Append($"\t\tDescriptorType=\"{bDescriptorType}\"\r\n");
            s.Append($"\t\tDescriptorLength=\"{bLength}\"\r\n");
            s.Append($"\t\tTotalLength=\"{wTotalLength}\"\r\n");
            s.Append($"\t\tMaxPower=\"{MaxPower}\"\r\n");

            for (var i = 0; i < AltInterfaces; i++)
                s.Append(Interfaces[i]);

            s.Append("\t</CONFIGURATION>\r\n");
            return s.ToString();
        }
    }
}