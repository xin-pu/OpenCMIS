/*
 ## Cypress CyUSB C# library source file (CyHidReport.cs)
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
    ///     Summary description for CyHidReport.
    /// </summary>
    public unsafe class CyHidReport
    {
        private readonly HIDP_REPORT_TYPE _rptType;

        private readonly HID_DATA[] Items;

        public CyHidButton[] Buttons;
        public CyHidValue[]  Values;

        public byte[] DataBuf;

        internal CyHidReport(HIDP_REPORT_TYPE rType, HIDP_CAPS hidCaps, byte * PreparsedDta)
        {
            _rptType = rType;

            if (rType == HIDP_REPORT_TYPE.HidP_Input)
            {
                RptByteLen = hidCaps.InputReportByteLength;
                NumBtnCaps = hidCaps.NumberInputButtonCaps;
                NumValCaps = hidCaps.NumberInputValueCaps;
            }
            else if (rType == HIDP_REPORT_TYPE.HidP_Output)
            {
                RptByteLen = hidCaps.OutputReportByteLength;
                NumBtnCaps = hidCaps.NumberOutputButtonCaps;
                NumValCaps = hidCaps.NumberOutputValueCaps;
            }
            else
            {
                RptByteLen = hidCaps.FeatureReportByteLength;
                NumBtnCaps = hidCaps.NumberFeatureButtonCaps;
                NumValCaps = hidCaps.NumberFeatureValueCaps;
            }

            // Big enough to hold the report ID and the report data
            if (RptByteLen > 0) DataBuf = new byte[RptByteLen + 1];

            if (NumBtnCaps > 0)
            {
                HIDP_BTN_VAL_CAPS ButtonCaps;
                Buttons = new CyHidButton[NumBtnCaps];

                var bc = new HIDP_BTN_VAL_CAPS();
                var buffer = new byte[NumBtnCaps * Marshal.SizeOf(bc)];

                fixed (byte * buf = buffer)
                {
                    var numCaps = NumBtnCaps;
                    //
                    //  BUGFIX 3/07/2008 - HidP_GetButtonCaps will modify numCaps to the
                    //      "actual number of elements that the routine returns".
                    //      In the somewhat rare event that numcaps is < _NumBtnCaps
                    //      on return, the reference to bCaps[i] in the loop below
                    //      will throw an "Index Was Outside the Bounds of the Array"
                    //      Exception.  This would occur for example when the
                    //      top-level HID report (being reported here) contains
                    //      a subset of the available buttons in the full HID interface.
                    //
                    PInvoke.HidP_GetButtonCaps(rType, buf, ref numCaps, PreparsedDta);

                    //
                    //  Reset _NumBtnCaps to the actual returned value.
                    //
                    NumBtnCaps = numCaps;

                    var bCaps = (HIDP_BTN_VAL_CAPS *) buf;
                    for (var i = 0; i < NumBtnCaps; i++)
                    {
                        // This assignment copies values from buf into ButtonCaps.
                        ButtonCaps = bCaps[i];

                        // Note that you must pass ButtonCaps to the
                        // below constructor and not bCaps[i]
                        Buttons[i] = new (ButtonCaps);

                        // Each button should have the same ReportID
                        ID = ButtonCaps.ReportID;
                    }
                }
            }

            if (NumValCaps > 0)
            {
                Values = new CyHidValue[NumValCaps];

                var vc = new HIDP_BTN_VAL_CAPS();
                var buffer = new byte[NumValCaps * Marshal.SizeOf(vc)];

                fixed (byte * buf = buffer)
                {
                    var numCaps = NumValCaps;
                    PInvoke.HidP_GetValueCaps(rType, buf, ref numCaps, PreparsedDta);

                    var vCaps = (HIDP_BTN_VAL_CAPS *) buf;
                    for (var i = 0; i < NumValCaps; i++)
                    {
                        // This assignment copies values from buf into ValueCaps.
                        var ValueCaps = vCaps[i];

                        // Note that you must pass ValueCaps[i] to the
                        // below constructor and not vCaps[i]
                        Values[i] = new (ValueCaps);

                        // Each value should have the same ReportID
                        ID = ValueCaps.ReportID;
                    }
                }
            }

            NumValues = 0;
            for (var i = 0; i < NumValCaps; i++)
                    //if (Values[i].IsRange)
                    //    _NumValues += Values[i].UsageMax - Values[i].Usage + 1;
                    //else
                NumValues++;

            NumItems = NumBtnCaps + NumValues;

            if (NumItems > 0) Items = new HID_DATA[NumItems];

            //if ((ButtonCaps != null) && (Items != null))
            if (NumBtnCaps > 0 && Items != null)
                for (var i = 0; i < NumBtnCaps; i++)
                {
                    Items[i].IsButtonData = 1;
                    Items[i].Status       = CyConst.HIDP_STATUS_SUCCESS;
                    Items[i].UsagePage    = Buttons[i].UsagePage;

                    if (Buttons[i].IsRange)
                    {
                        Items[i].Usage    = Buttons[i].Usage;
                        Items[i].UsageMax = Buttons[i].UsageMax;
                    }
                    else
                        Items[i].Usage = Items[i].UsageMax = Buttons[i].Usage;

                    Items[i].MaxUsageLength = PInvoke.HidP_MaxUsageListLength(
                            rType,
                            Buttons[i].UsagePage,
                            PreparsedDta);

                    Items[i].Usages = new ushort[Items[i].MaxUsageLength];

                    Items[i].ReportID = Buttons[i].ReportID;
                }

            for (var i = 0; i < NumValues; i++)
                if (Values[i].IsRange)
                    for (var usage = Values[i].Usage;
                         usage <= Values[i].UsageMax;
                         usage++)
                    {
                        Items[i].IsButtonData = 0;
                        Items[i].Status       = CyConst.HIDP_STATUS_SUCCESS;
                        Items[i].UsagePage    = Values[i].UsagePage;
                        Items[i].Usage        = usage;
                        Items[i].ReportID     = Values[i].ReportID;
                    }
                else
                {
                    Items[i].IsButtonData = 0;
                    Items[i].Status       = CyConst.HIDP_STATUS_SUCCESS;
                    Items[i].UsagePage    = Values[i].UsagePage;
                    Items[i].Usage        = Values[i].Usage;
                    Items[i].ReportID     = Values[i].ReportID;
                }
        } // End of CyHidReport constructor

        public byte ID { get; }

        public int RptByteLen { get; }

        public int NumBtnCaps { get; }

        public int NumValCaps { get; }

        public int NumValues { get; }

        public int NumItems { get; }

        public TreeNode Tree
        {
            get
            {
                var sType = _rptType switch
                            {
                                    HIDP_REPORT_TYPE.HidP_Input   => "Input",
                                    HIDP_REPORT_TYPE.HidP_Output  => "Output",
                                    HIDP_REPORT_TYPE.HidP_Feature => "Feature",
                                    _                             => ""
                            };

                if (NumItems > 0)
                {
                    var subTree = new TreeNode[NumItems];

                    var b = 0;
                    for (b = 0; b < NumBtnCaps; b++)
                    {
                        var t = new TreeNode("Button")
                        {
                                Tag = Buttons[b]
                        };
                        subTree[b] = t;
                    }

                    for (var v = 0; v < NumValCaps; v++)
                    {
                        var t = new TreeNode("Value")
                        {
                                Tag = Values[v]
                        };
                        subTree[b + v] = t;
                    }

                    var tr = new TreeNode(sType, subTree)
                    {
                            Tag = this
                    };

                    return tr;
                }

                return null;
            }
        }

        public void Clear()
        {
            for (var i = 0; i <= RptByteLen; i++)
                DataBuf[i] = 0;
        }

        public override string ToString()
        {
            var s = new StringBuilder();

            var sRptType = _rptType switch
                           {
                                   HIDP_REPORT_TYPE.HidP_Feature => "FEATURE",
                                   HIDP_REPORT_TYPE.HidP_Input   => "INPUT",
                                   HIDP_REPORT_TYPE.HidP_Output  => "OUTPUT",
                                   _                             => ""
                           };

            s.Append($"\t<{sRptType}>\r\n");
            //s.Append(string.Format("\t\tReportID=\"{0}\"\r\n", _ReportID));
            s.Append($"\t\tRptByteLen=\"{RptByteLen}\"\r\n");

            s.Append($"\t\tButtons=\"{NumBtnCaps}\"\r\n");
            s.Append($"\t\tValues=\"{NumValues}\"\r\n");

            if (NumBtnCaps > 0)
                foreach (var btn in Buttons)
                    s.Append(btn);

            if (NumValCaps > 0)
                foreach (var val in Values)
                    s.Append(val);

            s.Append($"\t</{sRptType}>\r\n");

            return s.ToString();
        }
    } // End of CyHidReport class

    public class CyHidButton
    {
        protected HIDP_BTN_VAL_CAPS Caps;

        public CyHidButton(HIDP_BTN_VAL_CAPS bc)
        {
            Caps = bc;
        }

        public ushort ReportID => Caps.ReportID;

        public ushort BitField => Caps.BitField;

        public ushort LinkUsage => Caps.LinkUsage;

        public ushort LinkUsagePage => Caps.LinkUsagePage;

        public ushort LinkCollection => Caps.LinkCollection;

        public ushort DataIndex => Caps.DataIndex;

        public ushort DataIndexMax => Caps.DataIndexMax;

        public ushort StringIndex => Caps.StringIndex;

        public ushort StringMax => Caps.StringMax;

        public ushort DesignatorIndex => Caps.DesignatorIndex;

        public ushort DesignatorIndexMax => Caps.DesignatorMax;

        public ushort Usage => Caps.Usage;

        public ushort UsagePage => Caps.UsagePage;

        public ushort UsageMax => Caps.UsageMax;

        public bool IsAlias => Caps.IsAlias > 0;

        public bool IsRange => Caps.IsRange > 0;

        public bool IsStringRange => Caps.IsStringRange > 0;

        public bool IsDesignatorRange => Caps.IsDesignatorRange > 0;

        public bool IsAbsolute => Caps.IsAbsolute > 0;

        public override string ToString()
        {
            var s = new StringBuilder();

            s.Append("\t\t<BUTTON>\r\n");
            s.Append($"\t\t\tReportID=\"{Caps.ReportID}\"\r\n");
            s.Append($"\t\t\tUsage=\"{Util.byteStr(Caps.Usage)}\"\r\n");
            s.Append($"\t\t\tUsagePage=\"{Util.byteStr(Caps.UsagePage)}\"\r\n");
            s.Append($"\t\t\tUsageMax=\"{Util.byteStr(Caps.UsageMax)}\"\r\n");

            s.Append($"\t\t\tBitField=\"{Util.byteStr(Caps.BitField)}\"\r\n");
            s.Append($"\t\t\tLinkCollection=\"{Util.byteStr(Caps.LinkCollection)}\"\r\n");
            s.Append($"\t\t\tLinkUsage=\"{Util.byteStr(Caps.LinkUsage)}\"\r\n");
            s.Append($"\t\t\tLinkUsagePage=\"{Util.byteStr(Caps.LinkUsagePage)}\"\r\n");

            s.Append($"\t\t\tIsAlias=\"{Caps.IsAlias                     > 0}\"\r\n");
            s.Append($"\t\t\tIsRange=\"{Caps.IsRange                     > 0}\"\r\n");
            s.Append($"\t\t\tIsStringRange=\"{Caps.IsStringRange         > 0}\"\r\n");
            s.Append($"\t\t\tIsDesignatorRange=\"{Caps.IsDesignatorRange > 0}\"\r\n");
            s.Append($"\t\t\tIsAbsolute=\"{Caps.IsAbsolute               > 0}\"\r\n");

            s.Append($"\t\t\tStringIndex=\"{Caps.StringIndex}\"\r\n");
            s.Append($"\t\t\tStringMax=\"{Caps.StringMax}\"\r\n");
            s.Append($"\t\t\tDesignatorIndex=\"{Caps.DesignatorIndex}\"\r\n");
            s.Append($"\t\t\tDesignatorMax=\"{Caps.DesignatorMax}\"\r\n");
            s.Append($"\t\t\tDataIndex=\"{Caps.DataIndex}\"\r\n");
            s.Append($"\t\t\tDataIndexMax=\"{Caps.DataIndexMax}\"\r\n");
            s.Append("\t\t</BUTTON>\r\n");

            return s.ToString();
        }
    }

    // A HidValueCaps struct is a superset of a HidButtonCaps
    public class CyHidValue : CyHidButton
    {
        // Just invoke the base constructor
        public CyHidValue(HIDP_BTN_VAL_CAPS vc)
                : base(vc) { }

        public ushort BitSize => Caps.BitSize;

        public bool HasNull => Caps.HasNull > 0;

        public uint Units => Caps.Units;

        public uint UnitsExp => Caps.UnitsExp;

        public int LogicalMin => Caps.LogicalMin;

        public int LogicalMax => Caps.LogicalMax;

        public int PhysicalMin => Caps.PhysicalMin;

        public int PhysicalMax => Caps.PhysicalMax;

        public override string ToString()
        {
            var s = new StringBuilder();

            s.Append("\t\t<VALUE>\r\n");
            s.Append($"\t\t\tReportID=\"{Caps.ReportID}\"\r\n");
            s.Append($"\t\t\tUsage=\"{Util.byteStr(Caps.Usage)}\"\r\n");
            s.Append($"\t\t\tUsagePage=\"{Util.byteStr(Caps.UsagePage)}\"\r\n");
            s.Append($"\t\t\tUsageMax=\"{Util.byteStr(Caps.UsageMax)}\"\r\n");

            s.Append($"\t\t\tBitField=\"{Util.byteStr(Caps.BitField)}\"\r\n");
            s.Append($"\t\t\tLinkCollection=\"{Util.byteStr(Caps.LinkCollection)}\"\r\n");
            s.Append($"\t\t\tLinkUsage=\"{Util.byteStr(Caps.LinkUsage)}\"\r\n");
            s.Append($"\t\t\tLinkUsagePage=\"{Util.byteStr(Caps.LinkUsagePage)}\"\r\n");

            s.Append($"\t\t\tIsAlias=\"{Caps.IsAlias                     > 0}\"\r\n");
            s.Append($"\t\t\tIsRange=\"{Caps.IsRange                     > 0}\"\r\n");
            s.Append($"\t\t\tIsStringRange=\"{Caps.IsStringRange         > 0}\"\r\n");
            s.Append($"\t\t\tIsDesignatorRange=\"{Caps.IsDesignatorRange > 0}\"\r\n");
            s.Append($"\t\t\tIsAbsolute=\"{Caps.IsAbsolute               > 0}\"\r\n");
            s.Append($"\t\t\tHasNull=\"{Caps.HasNull                     > 0}\"\r\n");

            s.Append($"\t\t\tStringIndex=\"{Caps.StringIndex}\"\r\n");
            s.Append($"\t\t\tStringMax=\"{Caps.StringMax}\"\r\n");
            s.Append($"\t\t\tDesignatorIndex=\"{Caps.DesignatorIndex}\"\r\n");
            s.Append($"\t\t\tDesignatorMax=\"{Caps.DesignatorMax}\"\r\n");
            s.Append($"\t\t\tDataIndex=\"{Caps.DataIndex}\"\r\n");
            s.Append($"\t\t\tDataIndexMax=\"{Caps.DataIndexMax}\"\r\n");

            s.Append($"\t\t\tBitField=\"{Util.byteStr(Caps.BitField)}\"\r\n");
            s.Append($"\t\t\tLinkCollection=\"{Util.byteStr(Caps.LinkCollection)}\"\r\n");
            s.Append($"\t\t\tLinkUsage=\"{Util.byteStr(Caps.LinkUsage)}\"\r\n");
            s.Append($"\t\t\tLinkUsagePage=\"{Util.byteStr(Caps.LinkUsagePage)}\"\r\n");

            s.Append($"\t\t\tBitSize=\"{Caps.BitSize}\"\r\n");
            s.Append($"\t\t\tReportCount=\"{Caps.ReportCount}\"\r\n");
            s.Append($"\t\t\tUnits=\"{Caps.Units}\"\r\n");
            s.Append($"\t\t\tUnitsExp=\"{Caps.UnitsExp}\"\r\n");

            s.Append($"\t\t\tLogicalMin=\"{Caps.LogicalMin}\"\r\n");
            s.Append($"\t\t\tLogicalMax=\"{Caps.LogicalMax}\"\r\n");
            s.Append($"\t\t\tPhysicalMin=\"{Caps.PhysicalMin}\"\r\n");
            s.Append($"\t\t\tPhysicalMax=\"{Caps.PhysicalMax}\"\r\n");

            s.Append("\t\t</VALUE>\r\n");

            return s.ToString();
        }
    }
}