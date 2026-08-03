/*
 ## Cypress CyUSB C# library source file (MsgForm.cs)
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

using System.ComponentModel;
using System.Security.Permissions;

namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Summary description for MsgForm.
    /// </summary>
    internal class MsgForm : Form
    {
        private readonly Container components = null;
        private          bool      bPnP_DevNodeChange;
        private          bool      bPnP_Arrival;

        internal App_PnP_Callback AppCallback;

        private IntPtr hRemovedDevice;

        public MsgForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            bPnP_DevNodeChange = false;
            bPnP_Arrival       = false;
        }

        public void Call_Dispose(bool x)
        {
            Dispose(x);
        }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == CyConst.WM_DEVICECHANGE)
            {
                // Tracks DBT_DEVICEARRIVAL followed by DBT_DEVNODES_CHANGED
                if (m.WParam == CyConst.DBT_DEVICEARRIVAL)
                {
                    bPnP_Arrival       = true;
                    bPnP_DevNodeChange = false;
                }

                // Tracks DBT_DEVNODES_CHANGED followed by DBT_DEVICEREMOVECOMPLETE
                if (m.WParam == CyConst.DBT_DEVNODES_CHANGED)
                    bPnP_DevNodeChange = true;

                if (m.WParam == CyConst.DBT_DEVICEREMOVECOMPLETE)
                {
                    var bcastHdr = new DEV_BROADCAST_HDR();
                    bcastHdr = (DEV_BROADCAST_HDR) m.GetLParam(bcastHdr.GetType());
                    if (bcastHdr.dbch_devicetype == CyConst.DBT_DEVTYP_HANDLE)
                    {
                        hRemovedDevice = bcastHdr.dbch_handle;
                        if (AppCallback != null)
                            AppCallback(CyConst.DBT_DEVICEREMOVECOMPLETE, hRemovedDevice);
                    }
                }

                // If DBT_DEVICEARRIVAL followed by DBT_DEVNODES_CHANGED
                if (bPnP_DevNodeChange && bPnP_Arrival)
                {
                    bPnP_Arrival       = false;
                    bPnP_DevNodeChange = false;
                    if (AppCallback != null)
                        AppCallback(CyConst.DBT_DEVICEARRIVAL, CyConst.INVALID_HANDLE);
                }
            }

            if (m.Msg == CyConst.WM_POWERBROADCAST)

                    //if (m.WParam == CyConst.PBT_APMRESUMEAUTOMATIC)
            {
                if (m.WParam == CyConst.PBT_APMSUSPEND || m.WParam == CyConst.PBT_APMRESUMEAUTOMATIC)
                    CyConst.Hibernate_first_call = true;
            }

            base.WndProc(ref m);
        }

        #region Windows Form Designer generated code
        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            //
            // MsgForm
            //
            AutoScaleBaseSize = new Size(5,  13);
            ClientSize        = new Size(90, 0);
            FormBorderStyle   = FormBorderStyle.FixedToolWindow;
            Name              = "MsgForm";
            Text              = "MsgForm";
        }
        #endregion
    }
}
