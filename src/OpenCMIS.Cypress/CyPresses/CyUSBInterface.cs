/*
 ## Cypress CyUSB C# library source file (CyUSBInterface.cs)
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
    public class CyUSBInterfaceContainer
    {
        public CyUSBInterface[] Interfaces;

        public CyUSBInterfaceContainer(byte intfcNum, byte altIntfcCount)
        {
            bInterfaceNumber   = intfcNum;
            AltInterfacesCount = altIntfcCount;
            Interfaces         = new CyUSBInterface[altIntfcCount];
        }

        public byte bInterfaceNumber { get; }

        public byte AltInterfacesCount { get; }

        public TreeNode Tree
        {
            get
            {
                var itmp = "Interface " + bInterfaceNumber;
                var altTree = new TreeNode[AltInterfacesCount];
                for (var i = 0; i < AltInterfacesCount; i++)
                    altTree[i] = Interfaces[i].Tree;
                var iNode = new TreeNode(itmp, altTree)
                {
                        Tag = this
                };

                return iNode;
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder("\t<INTERFACE " + bInterfaceNumber + ">\r\n");

            for (var i = 0; i < AltInterfacesCount; i++)
                s.Append(Interfaces[i]);

            s.Append("\t<INTERFACE " + bInterfaceNumber + ">\r\n");
            return s.ToString();
        }
    }

    /// <summary>
    ///     The CyUSBInterface Class
    /// </summary>
    public class CyUSBInterface
    {
        public CyUSBEndPoint[] EndPoints; // Holds pointers to all the interface's endpoints, plus a pointer to the Control endpoint zero

        internal byte _bAltSettings;

        internal unsafe CyUSBInterface(IntPtr handle, byte * DescrData, CyControlEndPoint ctlEndPt)
        {
            var pIntfcDescriptor = (USB_INTERFACE_DESCRIPTOR *) DescrData;

            bLength            = pIntfcDescriptor->bLength;
            bDescriptorType    = pIntfcDescriptor->bDescriptorType;
            bInterfaceNumber   = pIntfcDescriptor->bInterfaceNumber;
            bAlternateSetting  = pIntfcDescriptor->bAlternateSetting;
            bNumEndpoints      = pIntfcDescriptor->bNumEndpoints;
            bInterfaceClass    = pIntfcDescriptor->bInterfaceClass;
            bInterfaceSubClass = pIntfcDescriptor->bInterfaceSubClass;
            bInterfaceProtocol = pIntfcDescriptor->bInterfaceProtocol;
            iInterface         = pIntfcDescriptor->iInterface;

            _bAltSettings = 0;
            wTotalLength  = bLength;

            var desc = DescrData + pIntfcDescriptor->bLength;

            int i;
            var unexpected = 0;

            EndPoints    = new CyUSBEndPoint[bNumEndpoints + 1];
            EndPoints[0] = ctlEndPt;

            for (i = 1; i <= bNumEndpoints; i++)
            {
                var endPtDesc = (USB_ENDPOINT_DESCRIPTOR *) desc;
                wTotalLength += endPtDesc->bLength;

                if (endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE)
                {
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;

                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc);
                            break;

                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc);
                            break;

                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc);
                            break;
                    }

                    desc += endPtDesc->bLength;
                }
                else
                {
                    unexpected++;
                    if (unexpected < 12)
                    {
                        // Sanity check - prevent infinite loop
                        // This may have been a class-specific descriptor (like HID).  Skip it.
                        desc += endPtDesc->bLength;

                        // Stay in the loop, grabbing the next descriptor
                        i--;
                    }
                }
            }
        }

        internal unsafe CyUSBInterface(IntPtr handle, byte * DescrData, CyControlEndPoint ctlEndPt, byte usb30dummy)
        {
            var pIntfcDescriptor = (USB_INTERFACE_DESCRIPTOR *) DescrData;

            bLength            = pIntfcDescriptor->bLength;
            bDescriptorType    = pIntfcDescriptor->bDescriptorType;
            bInterfaceNumber   = pIntfcDescriptor->bInterfaceNumber;
            bAlternateSetting  = pIntfcDescriptor->bAlternateSetting;
            bNumEndpoints      = pIntfcDescriptor->bNumEndpoints;
            bInterfaceClass    = pIntfcDescriptor->bInterfaceClass;
            bInterfaceSubClass = pIntfcDescriptor->bInterfaceSubClass;
            bInterfaceProtocol = pIntfcDescriptor->bInterfaceProtocol;
            iInterface         = pIntfcDescriptor->iInterface;

            _bAltSettings = 0;
            wTotalLength  = bLength;

            var desc = DescrData + pIntfcDescriptor->bLength;

            int i;
            var unexpected = 0;

            EndPoints    = new CyUSBEndPoint[bNumEndpoints + 1];
            EndPoints[0] = ctlEndPt;

            for (i = 1; i <= bNumEndpoints; i++)
            {
                var bSSDec = false;
                var endPtDesc = (USB_ENDPOINT_DESCRIPTOR *) desc;
                desc += endPtDesc->bLength;
                var ssendPtDesc = (USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR *) desc;
                wTotalLength += endPtDesc->bLength;

                if (ssendPtDesc != null)
                    bSSDec = ssendPtDesc->bDescriptorType == CyConst.USB_SUPERSPEED_ENDPOINT_COMPANION;

                if (endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE && bSSDec)
                {
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;

                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;

                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;

                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc, ssendPtDesc);
                            break;
                    }

                    wTotalLength += ssendPtDesc->bLength;
                    desc         += ssendPtDesc->bLength;
                }
                else if (endPtDesc->bDescriptorType == CyConst.USB_ENDPOINT_DESCRIPTOR_TYPE)
                    switch (endPtDesc->bmAttributes)
                    {
                        case 0:
                            EndPoints[i] = ctlEndPt;
                            break;

                        case 1:
                            EndPoints[i] = new CyIsocEndPoint(handle, endPtDesc);
                            break;

                        case 2:
                            EndPoints[i] = new CyBulkEndPoint(handle, endPtDesc);
                            break;

                        case 3:
                            EndPoints[i] = new CyInterruptEndPoint(handle, endPtDesc);
                            break;
                    }
                else
                {
                    unexpected++;
                    if (unexpected < 12)
                    {
                        // Sanity check - prevent infinite loop
                        // This may have been a class-specific descriptor (like HID).  Skip it.
                        desc += endPtDesc->bLength;

                        // Stay in the loop, grabbing the next descriptor
                        i--;
                    }
                }
            }
        }

        public byte bLength { get; }

        public byte bDescriptorType { get; }

        public byte bInterfaceNumber { get; }

        public byte bAlternateSetting { get; }

        public byte bNumEndpoints { get; }

        public byte bInterfaceClass { get; }

        public byte bInterfaceSubClass { get; }

        public byte bInterfaceProtocol { get; }

        public byte iInterface { get; }

        public byte   bAltSettings => _bAltSettings;
        public ushort wTotalLength { get; }

        public TreeNode Tree
        {
            get
            {
                var tmp = "Alternate Setting " + bAlternateSetting;

                //string tmp = "Interface " + bInterfaceNumber.ToString();

                var eTree = new TreeNode[bNumEndpoints];
                for (var i = 0; i < bNumEndpoints; i++)
                    eTree[i] = EndPoints[i + 1].Tree;

                var t = new TreeNode(tmp, eTree)
                {
                        Tag = this
                };

                return t;
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder("\t\t<INTERFACE>\r\n");

            s.Append($"\t\t\tInterface=\"{iInterface}\"\r\n");
            s.Append($"\t\t\tInterfaceNumber=\"{bInterfaceNumber}\"\r\n");
            s.Append($"\t\t\tAltSetting=\"{bAlternateSetting}\"\r\n");
            s.Append($"\t\t\tClass=\"{bInterfaceClass:X2}h\"\r\n");
            s.Append($"\t\t\tSubclass=\"{bInterfaceSubClass:X2}h\"\r\n");
            s.Append($"\t\t\tProtocol=\"{bInterfaceProtocol}\"\r\n");
            s.Append($"\t\t\tEndpoints=\"{bNumEndpoints}\"\r\n");
            s.Append($"\t\t\tDescriptorType=\"{bDescriptorType}\"\r\n");
            s.Append($"\t\t\tDescriptorLength=\"{bLength}\"\r\n");

            for (var i = 0; i < bNumEndpoints; i++)
                s.Append(EndPoints[i + 1]);

            s.Append("\t\t</INTERFACE>\r\n");
            return s.ToString();
        }
    }
}