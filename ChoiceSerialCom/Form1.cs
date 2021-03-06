using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChoiceApp;

namespace ChoiceSerialCom
{
    public partial class Form1 : Form
    {
        private ChoiceMcuIO mMcuIO;
        private ChoiceMcuIO_RW mFirmwareIO;

        public Form1()
        {
            InitializeComponent();

            labelError.Text = "";
            labelButtonUp.Text = "";
            labelButtonDown.Text = "";

            mMcuIO = new ChoiceMcuIO(this, "COM1");
            mMcuIO.McuStateUpdate += OnMcuStateUpdate;
            mMcuIO.OnError += OnMcuError;

            mMcuIO.ButtonUp += OnButtonUp;
            mMcuIO.ButtonDown += OnButtonDown;

            mFirmwareIO = new ChoiceMcuIO_RW(this, "COM60");
            mFirmwareIO.McuControlUpdate += OnMcuControlUpdate;
            mFirmwareIO.OnError += OnMcuError;
        }

        // Send MCU control data to the MCU
        private void buttonSendMcuControl_Click(object sender, EventArgs e)
        {
            labelError.Text = "";
            mMcuIO.CONTROL.TurnWhiteLedOn();
            labelMcuControlBytes.Text = "sending...";

            mMcuIO.SendMcuControl();
        }

        // Send MCU state data from the MCU (simulated)
        private void buttonSendMcuState_Click(object sender, EventArgs e)
        {
            labelError.Text = "";
            labelButtonUp.Text = "";
            labelButtonDown.Text = "";
            labelMcuStateBytes.Text = "sending...";

            mFirmwareIO.STATE_RW.STATE_BUTTON_1 = true;
            mFirmwareIO.SendMcuState();
            mFirmwareIO.STATE_RW.STATE_BUTTON_1 = false;
            mFirmwareIO.SendMcuState();
        }

        // Receive an MCU state update (show the raw hex bytes)
        private void OnMcuStateUpdate(object sender, McuStateEventArgs e)
        {
            labelMcuStateBytes.Text = getHexString(e.CurState.IOBuffer);
        }

        // Receive a button up event 
        private void OnButtonUp(object sender, McuStateEventArgs e)
        {
            labelButtonUp.Text = "Button UP";
        }

        // Receive a button down event 
        private void OnButtonDown(object sender, McuStateEventArgs e)
        {
            labelButtonDown.Text = "Button Down";
        }

        // Receive an MCU control update (show the raw hex bytes)
        private void OnMcuControlUpdate(object sender, McuControlEventArgs e)
        {
            labelMcuControlBytes.Text = getHexString(e.McuControl.IOBuffer);
        }

        // Receive an error
        private void OnMcuError(object sender, ErrorEventArgs e)
        {
            labelError.Text = e.Error.ToString();
        }

        // Convert a byte[] tp a hex string
        private string getHexString(byte[] buffer)
        {
            if (buffer == null)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();

            foreach (byte b in buffer)
            {
                sb.AppendFormat("{0:X2} ", b);
            }

            return sb.ToString();
        }


    }
}
