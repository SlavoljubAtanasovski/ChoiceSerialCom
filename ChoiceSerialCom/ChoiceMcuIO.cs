using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using ChoiceAnalyticalBeta;
using System.Reflection;

namespace ChoiceApp
{
    //
    // 
    //
    public enum IOStatus
    {
        IO_SUCCESS = 0,  // All specified bytes were successfully sent or received
        IO_PENDING = 1,  // The buffer has not been sent or received
        IO_PORT_ERROR = 2,  // Port not open etc.
        IO_NULL_BUFFER = 3,  // null IO buffer specified
        IO_OUT_OF_RANGE = 4,  // offset, numBytes or offset + numBytes exceeds the buffer size
        IO_TIMEOUT = 5,  // A timeout occurred before all bytes were sent or received
        IO_INSUFFICIENT_DATA = 6,  // Insufficient data was send or received (not the specified amount)
        IO_OTHER_EXCEPTION = 7,  // Another exception was caught during the IO
    }

    //
    //-------------------------------------------------------------------------
    //  Base data structure for all firmware-software interface buffers
    //  in the Choice Analytical Beta units.
    //-------------------------------------------------------------------------
    //
    public abstract class ChoiceMcuIOBuffer
    {

        //
        // IO Buffer
        //
        protected byte[]      mIOBuffer;

        //
        // Constructors
        //

        public ChoiceMcuIOBuffer(int byteLen) {
            mIOBuffer = new byte[byteLen];
        }

        public ChoiceMcuIOBuffer(ChoiceMcuIOBuffer copyFrom)
        {
            int bufferLen = copyFrom.IOBuffer.Length;
            mIOBuffer = new byte[bufferLen];
            System.Array.Copy(copyFrom.IOBuffer, mIOBuffer, bufferLen);
        }

        //
        // Allow access to the raw IO buffer
        //
        public byte[]   IOBuffer {
            get { return mIOBuffer; }
        }

        //
        // Provide access to the header constants
        //

        public virtual byte CONST_HEADER0
        {
            get { return (byte)0; }
        }

        public virtual byte CONST_HEADER1
        {
            get { return (byte)0; }
        }

        //
        // Data Fields
        //

        public byte HEADER0 {
            get { return mIOBuffer[0]; }
        }

        public byte HEADER1 {
            get { return mIOBuffer[1]; }
        }

        public byte CHECKSUM {
            get { return mIOBuffer[mIOBuffer.Length - 1]; }
        }

        //
        // Utility Methods
        //

        protected ushort getUShort(int offset)
        {
            return (ushort)(mIOBuffer[offset + 1] << 8 | mIOBuffer[offset]);
        }

        protected void setUShort(int offset, ushort data)
        {
            mIOBuffer[offset + 1] = (byte)(data >> 8);
            mIOBuffer[offset] = (byte)(data & 0xff);
        }

        //
        // Computes the checksum byte from mIOBuffer
        //
        public byte ComputeChecksum() {
            byte    checksum = 0;
            int iLen = mIOBuffer.Length - 1;
            for (int i=0; i < iLen; i++) {
                checksum += mIOBuffer[i];
            }

            return checksum;
        }

        //
        // Determine if the computed checksum matches the checksum stored in the IO buffer
        //
        public bool IsChecksumMatch() {
            return CHECKSUM == ComputeChecksum();
        }

        //
        // Set the IO buffer checksum
        //
        public byte SetChecksum() {
            return mIOBuffer[mIOBuffer.Length - 1] = ComputeChecksum();
        }
    }




    //
    //-------------------------------------------------------------------------
    //  Data structure from the main App to the STM32 MCU
    //-------------------------------------------------------------------------
    //
    public class AppToMcuIOBuffer : ChoiceMcuIOBuffer
    {
        //
        // Header Constants
        //
        private const byte  BYTE_HEADER0 = 0xC3;
        private const byte  BYTE_HEADER1 = 0xA5;

        //
        // STATE and CONTROL bits
        //
        private const int  BIT_WHITE_LED   = 0x01;
        private const int  BIT_NIR_LED     = 0x02;
        private const int  BIT_FAN_1       = 0x04;
        private const int  BIT_FAN_2       = 0x08;
        private const int  BIT_FAN_3       = 0x10;
        private const int  BIT_FAN_4       = 0x20;
        private const int  BIT_FAN_5       = 0x40;
        private const int  BIT_FAN_6       = 0x80;

        //
        // LED CONTROL bits
        //
        private const int BIT_WHITE_LED_DUTY_CYCLE = 0x01;
        private const int BIT_NIR_LED_DUTY_CYCLE   = 0x02;
        private const int UNUSED1 = 0x04;
        private const int UNUSED2 = 0x08;
        private const int UNUSED3 = 0x10;
        private const int UNUSED4 = 0x20;
        private const int UNUSED5 = 0x40;
        private const int UNUSED6 = 0x80;
        
        //
        // CUVETTE CONTROL bits
        //
        private const int BIT_CUVETTE_EMPTY = 0x01;
        private const int BIT_CUVETTE_FULL = 0x02;

        //
        // Constructor
        //
        public AppToMcuIOBuffer() :
            base(StaticValues.AppToMcuIOBufferSize)
        {
            Reset();
        }

        public AppToMcuIOBuffer(AppToMcuIOBuffer copyFrom) :
            base(copyFrom)
        {
            Reset();
        }

        //
        // Reset
        //
        public void Reset()
        {
            mIOBuffer[0] = BYTE_HEADER0;
            mIOBuffer[1] = BYTE_HEADER1;
            mIOBuffer[2] = 0;
            mIOBuffer[3] = 0;
            mIOBuffer[4] = 0;
            mIOBuffer[5] = 0;
            mIOBuffer[6] = 0;
            mIOBuffer[7] = 0;
            mIOBuffer[8] = 0;
            mIOBuffer[9] = 0;
            mIOBuffer[10] = 0;
            mIOBuffer[11] = 0;
            mIOBuffer[12] = 0;
            mIOBuffer[13] = 0;
            mIOBuffer[14] = 0;
            mIOBuffer[15] = 0;
            SetChecksum();
        }

        //
        // Determines if this IO buffer is valid
        // (proper header bytes and checksum)
        //
        public bool IsValid() {
            return HEADER0 == BYTE_HEADER0 && HEADER1 == BYTE_HEADER1 && IsChecksumMatch();
        }

        //
        // Provide access to the header constants
        //

        public override byte CONST_HEADER0
        {
            get { return BYTE_HEADER0; }
        }

        public override byte CONST_HEADER1
        {
            get { return BYTE_HEADER1; }
        }

        //
        // Data Fields
        //

        public byte CONTROL {
            get { return mIOBuffer[3]; }
            set { mIOBuffer[3] = value; }
        }

        public byte STATE {
            get { return mIOBuffer[4]; }
            set { mIOBuffer[4] = value; }
        }

        public byte CONTROL2
        {
            get { return mIOBuffer[5]; }
            set { mIOBuffer[5] = value; }
        }

        // Control byte 2 (contains LED duty cycle control)
        public byte WHITE_LED_DUTY_CYCLE
        {
            get { return mIOBuffer[6]; }
            set { mIOBuffer[6] = value; }
        }

        // set to percentage value betwen 0 and 100
        public byte NIR_LED_DUTY_CYCLE
        {
            get { return mIOBuffer[7]; }
            set { mIOBuffer[7] = value; }
        }
        public byte CUVETTE_CONTROL
        {
            get { return mIOBuffer[8]; }
            set { mIOBuffer[8] = value; }
        }

        //
        // STATE bits
        //

        public bool STATE_WHITE_LED {
            get { return (STATE & BIT_WHITE_LED) == BIT_WHITE_LED; }
            set { if (value) STATE |= BIT_WHITE_LED;
                  else STATE &= ~BIT_WHITE_LED & 0xff;
                }
        } 

        public bool STATE_NIR_LED {
            get { return (STATE & BIT_NIR_LED) == BIT_NIR_LED; }
            set { if (value) STATE |= BIT_NIR_LED;
                  else STATE &= ~BIT_NIR_LED & 0xff;
                }
        } 

        public bool STATE_FAN_1 {
            get { return (STATE & BIT_FAN_1) == BIT_FAN_1; }
            set { if (value) STATE |= BIT_FAN_1;
                  else STATE &= ~BIT_FAN_1 & 0xff;
                }
        } 

        public bool STATE_FAN_2 {
            get { return (STATE & BIT_FAN_2) == BIT_FAN_2; }
            set { if (value) STATE |= BIT_FAN_2;
                  else STATE &= ~BIT_FAN_2 & 0xff;
            }
        } 

        public bool STATE_FAN_3 {
            get { return (STATE & BIT_FAN_3) == BIT_FAN_3; }
            set { if (value) STATE |= BIT_FAN_3;
                  else STATE &= ~BIT_FAN_3 & 0xff;
            }
        } 

        public bool STATE_FAN_4 {
            get { return (STATE & BIT_FAN_4) == BIT_FAN_4; }
            set { if (value) STATE |= BIT_FAN_4;
                  else STATE &= ~BIT_FAN_4 & 0xff;
            }
        } 

        public bool STATE_FAN_5 {
            get { return (STATE & BIT_FAN_5) == BIT_FAN_5; }
            set { if (value) STATE |= BIT_FAN_5;
                  else STATE &= ~BIT_FAN_5 & 0xff;
            }
        } 

        public bool STATE_FAN_6 {
            get { return (STATE & BIT_FAN_6) == BIT_FAN_6; }
            set { if (value) STATE |= BIT_FAN_6;
                  else STATE &= ~BIT_FAN_6 & 0xff;
            }
        } 

        //
        // CONTROL bits
        //

        public bool CONTROL_WHITE_LED {
            get { return (CONTROL & BIT_WHITE_LED) == BIT_WHITE_LED; }
            set { if (value) CONTROL |= BIT_WHITE_LED;
                  else CONTROL &= ~BIT_WHITE_LED & 0xff;
                }
        } 

        public bool CONTROL_NIR_LED {
            get { return (CONTROL & BIT_NIR_LED) == BIT_NIR_LED; }
            set { if (value) CONTROL |= BIT_NIR_LED;
                  else CONTROL &= ~BIT_NIR_LED & 0xff;
                }
        } 

        public bool CONTROL_FAN_1 {
            get { return (CONTROL & BIT_FAN_1) == BIT_FAN_1; }
            set { if (value) CONTROL |= BIT_FAN_1;
                  else CONTROL &= ~BIT_FAN_1 & 0xff;
                }
        } 

        public bool CONTROL_FAN_2 {
            get { return (CONTROL & BIT_FAN_2) == BIT_FAN_2; }
            set { if (value) CONTROL |= BIT_FAN_2;
                  else CONTROL &= ~BIT_FAN_2 & 0xff;
            }
        } 

        public bool CONTROL_FAN_3 {
            get { return (CONTROL & BIT_FAN_3) == BIT_FAN_3; }
            set { if (value) CONTROL |= BIT_FAN_3;
                  else CONTROL &= ~BIT_FAN_3 & 0xff;
            }
        } 

        public bool CONTROL_FAN_4 {
            get { return (CONTROL & BIT_FAN_4) == BIT_FAN_4; }
            set { if (value) CONTROL |= BIT_FAN_4;
                  else CONTROL &= ~BIT_FAN_4 & 0xff;
            }
        } 

        public bool CONTROL_FAN_5 {
            get { return (CONTROL & BIT_FAN_5) == BIT_FAN_5; }
            set { if (value) CONTROL |= BIT_FAN_5;
                  else CONTROL &= ~BIT_FAN_5 & 0xff;
            }
        } 

        public bool CONTROL_FAN_6 {
            get { return (CONTROL & BIT_FAN_6) == BIT_FAN_6; }
            set { if (value) CONTROL |= BIT_FAN_6;
                  else CONTROL &= ~BIT_FAN_6 & 0xff;
            }
        }

        //
        // CONTROL2 bits
        //

        public bool CONTROL_WHITE_LED_DUTY_CYCLE
        {
            get { return (CONTROL2 & BIT_WHITE_LED_DUTY_CYCLE) == BIT_WHITE_LED_DUTY_CYCLE; }
            set
            {
                if (value) CONTROL2 |= BIT_WHITE_LED_DUTY_CYCLE;
                else CONTROL2 &= ~BIT_WHITE_LED_DUTY_CYCLE & 0xff;
            }
        }

        public bool CONTROL_NIR_LED_DUTY_CYCLE
        {
            get { return (CONTROL2 & BIT_NIR_LED_DUTY_CYCLE) == BIT_NIR_LED_DUTY_CYCLE; }
            set
            {
                if (value) CONTROL2 |= BIT_NIR_LED_DUTY_CYCLE;
                else CONTROL2 &= ~BIT_NIR_LED_DUTY_CYCLE & 0xff;
            }
        }

        //
        //CUVETTE CONTROL BITS, tells the mcu that the chamber is empty or full
        //
        public bool CONTROL_CUVETTE_EMPTY
        {
            get { return (CUVETTE_CONTROL & BIT_CUVETTE_EMPTY) == BIT_CUVETTE_EMPTY; }
            set
            {
                if (value) CUVETTE_CONTROL |= BIT_CUVETTE_EMPTY;
                else CUVETTE_CONTROL &= ~BIT_CUVETTE_EMPTY & 0xff;
            }
        }

        public bool CONTROL_CUVETTE_FULL
        {
            get { return (CUVETTE_CONTROL & BIT_CUVETTE_FULL) == BIT_CUVETTE_FULL; }
            set
            {
                if (value) CUVETTE_CONTROL |= BIT_CUVETTE_FULL;
                else CUVETTE_CONTROL &= ~BIT_CUVETTE_FULL & 0xff;
            }
        }


        //
        // Helper Methods
        //

        public void TurnWhiteLedOn()
        {
            CONTROL_WHITE_LED = true;
            STATE_WHITE_LED = true;
        }

        public void TurnWhiteLedOff()
        {
            CONTROL_WHITE_LED = true;
            STATE_WHITE_LED = false;
        }

        public void TurnNirLedOn()
        {
            CONTROL_NIR_LED = true;
            STATE_NIR_LED = true;
        }

        public void TurnNirLedOff()
        {
            CONTROL_NIR_LED = true;
            STATE_NIR_LED = false;
        }

        public void TurnFan1On()
        {
            CONTROL_FAN_1 = true;
            STATE_FAN_1 = true;
        }

        public void TurnFan1Off()
        {
            CONTROL_FAN_1 = true;
            STATE_FAN_1 = false;
        }

        public void TurnFan2On()
        {
            CONTROL_FAN_2 = true;
            STATE_FAN_2 = true;
        }

        public void TurnFan2Off()
        {
            CONTROL_FAN_2 = true;
            STATE_FAN_2 = false;
        }

        public void TurnFan3On()
        {
            CONTROL_FAN_3 = true;
            STATE_FAN_3 = true;
        }

        public void TurnFan3Off()
        {
            CONTROL_FAN_3 = true;
            STATE_FAN_3 = false;
        }

        public void TurnFan4On()
        {
            CONTROL_FAN_4 = true;
            STATE_FAN_4 = true;
        }

        public void TurnFan4Off()
        {
            CONTROL_FAN_4 = true;
            STATE_FAN_4 = false;
        }

        public void TurnFan5On()
        {
            CONTROL_FAN_5 = true;
            STATE_FAN_5 = true;
        }

        public void TurnFan5Off()
        {
            CONTROL_FAN_5 = true;
            STATE_FAN_5 = false;
        }

        public void TurnFan6On()
        {
            CONTROL_FAN_6 = true;
            STATE_FAN_6 = true;
        }

        public void    TurnFan6Off() {
            CONTROL_FAN_6 = true;
            STATE_FAN_6 = false;
        }


        public void SetWhiteLedDutyCycle(byte dutyCycle)
        {
            CONTROL_WHITE_LED_DUTY_CYCLE = true;
            WHITE_LED_DUTY_CYCLE = dutyCycle;
        }

        public void SetNirLedDutyCycle(byte dutyCycle)
        {
            CONTROL_NIR_LED_DUTY_CYCLE = true;
            NIR_LED_DUTY_CYCLE = dutyCycle;
        }

        public void CalibrateEmpty()
        {
            CONTROL_CUVETTE_FULL = false;
            CONTROL_CUVETTE_EMPTY = true;            
        }

        // not used. The MCU actually does nothing with this information...
        public void CalibrateFull()
        {
            CONTROL_CUVETTE_FULL = true;
            CONTROL_CUVETTE_EMPTY = false;
        }
        public void CalibrateNone()
        {
            CONTROL_CUVETTE_FULL = false;
            CONTROL_CUVETTE_EMPTY = false;
        }

      
    }

    //
    //-------------------------------------------------------------------------
    //  Data structure sent from the STM32 MCU to the main App
    //-------------------------------------------------------------------------
    //
    public class McuToAppIOBuffer : ChoiceMcuIOBuffer
    { 
        //
        // Header Constants
        //
        protected const byte  BYTE_HEADER0 = 0x5A;
        protected const byte BYTE_HEADER1 = 0x3C;

        protected const byte BYTE_PROTOCOL_VERSION = 0x01;

        //
        // LED and SWITCH state bits
        //
        protected const int BIT_WHITE_LED  = 0x01;
        protected const int BIT_NIR_LED    = 0x02;
        protected const int BIT_RESERVED1  = 0x04;
        protected const int BIT_RESERVED2  = 0x08;
        protected const int BIT_CUVETTE_1  = 0x10;
        protected const int BIT_CUVETTE_2  = 0x20;
        protected const int BIT_CUVETTE_3  = 0x40;
        protected const int BIT_LID_SWITCH = 0x80;

        //
        // BUTTON state bits
        //
        protected const int BIT_BUTTON_1  = 0x01;  // top, left
        protected const int BIT_BUTTON_2  = 0x02;  // middle, left
        protected const int BIT_BUTTON_3  = 0x04;  // bottom, left
        protected const int BIT_BUTTON_4  = 0x08;  // top, right
        protected const int BIT_BUTTON_5  = 0x10;  // middle, right
        protected const int BIT_BUTTON_6  = 0x20;  // bottom, right
        protected const int BIT_RESERVED3 = 0x40;
        protected const int BIT_RESERVED4 = 0x80;

        //
        // FAN state bits
        //
        protected const int BIT_FAN_1     = 0x01;
        protected const int BIT_FAN_2     = 0x02;
        protected const int BIT_FAN_3     = 0x04;
        protected const int BIT_FAN_4     = 0x08;
        protected const int BIT_FAN_5     = 0x10;
        protected const int BIT_FAN_6     = 0x20;
        protected const int BIT_RESERVED5 = 0x40;
        protected const int BIT_RESERVED6 = 0x80;


        //
        //  Cuvette SensorSwitch Values
        //
        // Going forward, there will be no support for the earlier version of test chamber, so cuvette sensing will be done with
        //  optical rather than mechanical sensors
        // protected ushort CUVETTE_SENSOR_FIRMWARE_VERSION = 9;

        //
        // Constructor
        //
        public McuToAppIOBuffer() :
            base(StaticValues.McuToAppIOBufferSize)
        {
            Clear();
        }

        //
        // Clear
        //
        public void Clear()
        {
            System.Array.Clear(mIOBuffer, 0, mIOBuffer.Length);
        }

        //
        // Determines if this IO buffer is valid
        // (proper header bytes and checksum)
        //
        public bool IsValid() {
            return HEADER0 == BYTE_HEADER0 && HEADER1 == BYTE_HEADER1 && IsChecksumMatch();
        }

        //
        // Provide access to the header constants
        //

        public override byte CONST_HEADER0
        {
            get { return BYTE_HEADER0; }
        }

        public override byte CONST_HEADER1
        {
            get { return BYTE_HEADER1; }
        }

        //
        // Data Fields
        //

        public byte PROTOCOL_VERSION {
            get { return (mIOBuffer[2]); }
        }

        public byte LED_SWITCH_STATE { 
            get { return mIOBuffer[3]; }
        }

        public byte BUTTON_STATE {
            get { return mIOBuffer[4]; }
        }

        public byte FAN_STATE {
            get { return mIOBuffer[5]; }
        }

        public ushort THERMISTOR_1  //  Ambient Temperature
        {  
            get { return getUShort(6); }
        }

        public float THERMISTOR_1_DEG_C
        {
            get { return THERMISTOR_1 * 0.06f - 156.58f; }
        }

        public float THERMISTOR_AMBIENT_DEG_C
        {
            get { return THERMISTOR_1_DEG_C; }
        }

        public ushort THERMISTOR_2  // Transmission Photodiode Board Temperature
        {  
            get { return getUShort(8); }
        }

        public float THERMISTOR_2_DEG_C
        {
            get { return (THERMISTOR_2 * .0058f) - 281.5f; }
        }

        public float THERMISTOR_CHAMBER_CASE_DEG_C
        {
            get { return THERMISTOR_2_DEG_C; }
        }

        public ushort THERMISTOR_3  // Scatter Photodiode Board Temperature
        {  
            get { return getUShort(10); }
        }

        public float THERMISTOR_3_DEG_C
        {
            get { return (THERMISTOR_3 * .0058f) - 281.5f; }
        }

        public float SCATTER_PHOTODIODE_BOARD_DEG_C
        {
            get { return THERMISTOR_3_DEG_C; }
        }

        public ushort THERMISTOR_4  // Chamber Temperature
        {
            get { return getUShort(12); }
        }

        public float THERMISTOR_4_DEG_C
        {
            get { return THERMISTOR_4 * 0.060f - 156.58f; }
        }

        public ushort THERMOPILE_1  // Ambient Temperature (where thermopile is placed)
        {  
            get { return getUShort(14); }
        }

        public float THERMOPILE_1_DEG_C
        {
            get { return (THERMOPILE_1 - 0x2DE4) * 0.02f - 38.2f; }
        }

        public float THERMOPILE_CASE_DEG_C
        {
            get { return THERMOPILE_1_DEG_C; }
        }

        public ushort THERMOPILE_2   // Object Temperature (object being measured... the cuvette)
        {
            get { return getUShort(16); }
        }

        public float THERMOPILE_2_DEG_C
        {
            get { return (THERMOPILE_2 - 0x2DE4) * 0.02f - 38.2f; }
        }

        public float THERMOPILE_OBJECT_DEG_C
        {
            get { return THERMOPILE_2_DEG_C; }
        }

        public ushort PHOTODIODE_1  // Transmission Photodiode
        {
            get { return getUShort(18); }
        }

        public ushort TRANSMISSION_PHOTODIODE
        {
            get { return PHOTODIODE_1; }
        }

        public ushort PHOTODIODE_2  // Scatter Photodiode
        {  
            get { return getUShort(20); }
        }

        public ushort SCATTER_PHOTODIODE
        {
            get { return PHOTODIODE_2; }
        }

        public byte WHITE_LED_DUTY_CYCLE 
        {
            get { return mIOBuffer[22]; }
        }

        public byte NIR_LED_DUTY_CYCLE
        {
            get { return mIOBuffer[23]; }
        }

        // Cuvette Sensor Changes
        // Get the new cuvette data
        public ushort CUVETTE1_SENSOR  
        {
            get { return getUShort(24); }
        }

        public ushort CUVETTE2_SENSOR  
        {
            get { return getUShort(26); }
        }

        public ushort CUVETTE3_SENSOR 
        {
            get { return getUShort(28); }
        }

        //
        // FIRMWARE VERSION
        //

            public byte MCU_FIRMWARE_VERSION
        {
            get { return mIOBuffer[30]; }
        }

        //
        // MCU to CPU message checksum
        //

        public byte MCU_CHECKSUM
        {
            get { return mIOBuffer[31]; }
        }

        //
        // LED and SWITCH STATE bits
        //

        public bool STATE_WHITE_LED {
            get { return (LED_SWITCH_STATE & BIT_WHITE_LED) == BIT_WHITE_LED; }
        } 

        public bool STATE_NIR_LED {
            get { return (LED_SWITCH_STATE & BIT_NIR_LED) == BIT_NIR_LED; }
        }

        public bool STATE_CUVETTE_1
        {
            get
            {
                bool cuvette_1 = (CUVETTE1_SENSOR <= StaticValues.CuvetteSensorThreshold_1);
                bool cuvette_2 = (CUVETTE2_SENSOR <= StaticValues.CuvetteSensorThreshold_2);
                bool cuvette_3 = (CUVETTE3_SENSOR <= StaticValues.CuvetteSensorThreshold_3);
                return (cuvette_1 && !cuvette_2 && !cuvette_3);
            }
        }

        public bool STATE_CUVETTE_2
        {
            get
            {
                bool cuvette_1 = (CUVETTE1_SENSOR <= StaticValues.CuvetteSensorThreshold_1);
                bool cuvette_2 = (CUVETTE2_SENSOR <= StaticValues.CuvetteSensorThreshold_2);
                bool cuvette_3 = (CUVETTE3_SENSOR <= StaticValues.CuvetteSensorThreshold_3);
                return (cuvette_1 && cuvette_2 && !cuvette_3);
            }
        }

        public bool STATE_CUVETTE_3
        {
            get
            {
                
                bool cuvette_1 = (CUVETTE1_SENSOR <= StaticValues.CuvetteSensorThreshold_1);
                bool cuvette_2 = (CUVETTE2_SENSOR <= StaticValues.CuvetteSensorThreshold_2);
                bool cuvette_3 = (CUVETTE3_SENSOR <= StaticValues.CuvetteSensorThreshold_3);
                return (cuvette_1 && cuvette_2 && cuvette_3);
            }
        }

        public bool CHAMBER_EMPTY
        {
            get
            {
                return (CUVETTE1_SENSOR > StaticValues.CuvetteSensorThreshold_1) &&
                    (CUVETTE2_SENSOR > StaticValues.CuvetteSensorThreshold_2) &&
                    (CUVETTE3_SENSOR > StaticValues.CuvetteSensorThreshold_3);
            }
        }

        // Lid sensor
        public bool STATE_LID
        {
            get { return (LED_SWITCH_STATE & BIT_LID_SWITCH) == BIT_LID_SWITCH; }
        }
        //
        // BUTTON STATE bits
        //

        public bool STATE_BUTTON_1 {
            get { return (BUTTON_STATE & BIT_BUTTON_1) == BIT_BUTTON_1; }
        } 

        public bool STATE_BUTTON_2 {
            get { return (BUTTON_STATE & BIT_BUTTON_2) == BIT_BUTTON_2; }
        } 

        public bool STATE_BUTTON_3 {
            get { return (BUTTON_STATE & BIT_BUTTON_3) == BIT_BUTTON_3; }
        } 

        public bool STATE_BUTTON_4 {
            get { return (BUTTON_STATE & BIT_BUTTON_4) == BIT_BUTTON_4; }
        } 

        public bool STATE_BUTTON_5 {
            get { return (BUTTON_STATE & BIT_BUTTON_5) == BIT_BUTTON_5; }
        } 

        public bool STATE_BUTTON_6 {
            get { return (BUTTON_STATE & BIT_BUTTON_6) == BIT_BUTTON_6; }
        } 

        //
        // FAN STATE bits
        //

        public bool STATE_FAN_1 {
            get { return (FAN_STATE & BIT_FAN_1) == BIT_FAN_1; }
        } 

        public bool STATE_FAN_2 {
            get { return (FAN_STATE & BIT_FAN_2) == BIT_FAN_2; }
        } 

        public bool STATE_FAN_3 {
            get { return (FAN_STATE & BIT_FAN_3) == BIT_FAN_3; }
        } 

        public bool STATE_FAN_4 {
            get { return (FAN_STATE & BIT_FAN_4) == BIT_FAN_4; }
        } 

        public bool STATE_FAN_5 {
            get { return (FAN_STATE & BIT_FAN_5) == BIT_FAN_5; }
        } 

        public bool STATE_FAN_6 {
            get { return (FAN_STATE & BIT_FAN_6) == BIT_FAN_6; }
        } 


    }

    //
    //-------------------------------------------------------------------------
    // Read/Write version of McuToAppIO
    //-------------------------------------------------------------------------
    //
    public class McuToAppIOBuffer_RW : McuToAppIOBuffer
    {
        //
        // Constructor
        //
        public McuToAppIOBuffer_RW() :
            base()
        {
            Reset();
        }

        //
        // Reset
        //
        public void Reset()
        {
            Clear();

            mIOBuffer[0] = BYTE_HEADER0;
            mIOBuffer[1] = BYTE_HEADER1;
            PROTOCOL_VERSION = BYTE_PROTOCOL_VERSION;

            SetChecksum();
        }

        //
        // Data Fields
        //

        public new byte PROTOCOL_VERSION
        {
            get { return base.PROTOCOL_VERSION; }
            set { mIOBuffer[2] = value; }
        }

        public new byte LED_SWITCH_STATE
        {
            get { return base.LED_SWITCH_STATE; }
            set { mIOBuffer[3] = value;  }
        }

        public new byte BUTTON_STATE
        {
            get { return base.BUTTON_STATE; }
            set { mIOBuffer[4] = value; }
        }

        public new byte FAN_STATE
        {
            get { return base.FAN_STATE; }
            set { mIOBuffer[5] = value; }
        }

        public new ushort THERMISTOR_1
        {
            get { return base.THERMISTOR_1; }
            set { setUShort(6, value); }
        }

        public new ushort THERMISTOR_2
        {
            get { return base.THERMISTOR_2; }
            set { setUShort(8, value); }
        }

        public new ushort THERMISTOR_3
        {
            get { return base.THERMISTOR_3; }
            set { setUShort(10, value); }
        }

        public new ushort THERMISTOR_4
        {
            get { return base.THERMISTOR_4; }
            set { setUShort(12, value); }
        }

        public new ushort THERMOPILE_1
        {
            get { return base.THERMOPILE_1; }
            set { setUShort(14, value); }
        }

        public new ushort THERMOPILE_2
        {
            get { return base.THERMOPILE_2; }
            set { setUShort(16, value); }
        }

        public new ushort PHOTODIODE_1
        {
            get { return base.PHOTODIODE_1; }
            set { setUShort(18, value); }
        }

        public new ushort TRANSMISSION_PHOTODIODE
        {
            get { return base.TRANSMISSION_PHOTODIODE; }
            set { setUShort(18, value); }
        }

        public new ushort PHOTODIODE_2
        {
            get { return base.PHOTODIODE_2; }
            set { setUShort(20, value); }
        }

        public new ushort SCATTER_PHOTODIODE
        {
            get { return base.SCATTER_PHOTODIODE; }
            set { setUShort(20, value); }
        }

        public new byte WHITE_LED_DUTY_CYCLE
        {
            get { return base.WHITE_LED_DUTY_CYCLE; }
            set { mIOBuffer[22] = value; }
        }

        public new byte NIR_LED_DUTY_CYCLE
        {
            get { return base.NIR_LED_DUTY_CYCLE; }
            set { mIOBuffer[23] = value; }
        }

        //
        // LED and SWITCH STATE bits
        //

        public new bool STATE_WHITE_LED
        {
            get { return base.STATE_WHITE_LED; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_WHITE_LED;
                else LED_SWITCH_STATE &= ~BIT_WHITE_LED & 0xff;
            }
        }

        public new bool STATE_NIR_LED
        {
            get { return base.STATE_NIR_LED; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_NIR_LED;
                else LED_SWITCH_STATE &= ~BIT_NIR_LED & 0xff;
            }
        }

        public new bool STATE_CUVETTE_1
        {
            get { return base.STATE_CUVETTE_1; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_CUVETTE_1;
                else LED_SWITCH_STATE &= ~BIT_CUVETTE_1 & 0xff;
            }
        }

        public new bool STATE_CUVETTE_2
        {
            get { return base.STATE_CUVETTE_2; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_CUVETTE_2;
                else LED_SWITCH_STATE &= ~BIT_CUVETTE_2 & 0xff;
            }
        }

        public new bool STATE_CUVETTE_3
        {
            get { return base.STATE_CUVETTE_3; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_CUVETTE_3;
                else LED_SWITCH_STATE &= ~BIT_CUVETTE_3 & 0xff;
            }
        }

        public new bool STATE_LID
        {
            get { return base.STATE_LID; }
            set
            {
                if (value) LED_SWITCH_STATE |= BIT_LID_SWITCH;
                else LED_SWITCH_STATE &= ~BIT_LID_SWITCH & 0xff;
            }
        }

        //
        // BUTTON STATE bits
        //

        public new bool STATE_BUTTON_1
        {
            get { return base.STATE_BUTTON_1; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_1;
                else BUTTON_STATE &= ~BIT_BUTTON_1 & 0xff;
            }
        }

        public new bool STATE_BUTTON_2
        {
            get { return base.STATE_BUTTON_2; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_2;
                else BUTTON_STATE &= ~BIT_BUTTON_2 & 0xff;
            }
        }

        public new bool STATE_BUTTON_3
        {
            get { return base.STATE_BUTTON_3; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_3;
                else BUTTON_STATE &= ~BIT_BUTTON_3 & 0xff;
            }
        }

        public new bool STATE_BUTTON_4
        {
            get { return base.STATE_BUTTON_4; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_4;
                else BUTTON_STATE &= ~BIT_BUTTON_4 & 0xff;
            }
        }

        public new bool STATE_BUTTON_5
        {
            get { return base.STATE_BUTTON_5; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_5;
                else BUTTON_STATE &= ~BIT_BUTTON_5 & 0xff;
            }
        }

        public new bool STATE_BUTTON_6
        {
            get { return base.STATE_BUTTON_6; }
            set
            {
                if (value) BUTTON_STATE |= BIT_BUTTON_6;
                else BUTTON_STATE &= ~BIT_BUTTON_6 & 0xff;
            }
        }

        //
        // FAN STATE bits
        //

        public new bool STATE_FAN_1
        {
            get { return base.STATE_FAN_1; }
            set
            {
                if (value) FAN_STATE |= BIT_FAN_1;
                else FAN_STATE &= ~BIT_FAN_1 & 0xff;
            }
        }

        public new bool STATE_FAN_2
        {
            get { return base.STATE_FAN_2; }
            set
            {
                if (value) FAN_STATE |= BIT_FAN_2;
                else FAN_STATE &= ~BIT_FAN_2 & 0xff;
            }
        }

        public new bool STATE_FAN_3
        {
            get { return base.STATE_FAN_3; }
            set
            {
                if (value) FAN_STATE |= BIT_FAN_3;
                else FAN_STATE &= ~BIT_FAN_3 & 0xff;
            }
        }

        public new bool STATE_FAN_4
        {
            get { return base.STATE_FAN_4; }
            set
            {
                if (value) FAN_STATE |= BIT_FAN_4;
                else FAN_STATE &= ~BIT_FAN_4 & 0xff;
            }
        }

        public new bool STATE_FAN_5
        {
            get { return base.STATE_FAN_5; }
            set
            {
                if (value) FAN_STATE |= BIT_FAN_5;
                else FAN_STATE &= ~BIT_FAN_5 & 0xff;
            }
        }

        public new bool STATE_FAN_6
        {
            get { return base.STATE_FAN_6; }
            set {
                if (value) FAN_STATE |= BIT_FAN_6;
                else FAN_STATE &= ~BIT_FAN_6 & 0xff;
            }
        }

    }

    //
    //-------------------------------------------------------------------------
    // Event Args for for MCU state change events
    //-------------------------------------------------------------------------
    //
    public class McuStateEventArgs : EventArgs
    {
        //
        // Private Members
        //

        private McuToAppIOBuffer mPrevState;
        private McuToAppIOBuffer mCurState;
        private int mItemNum;

        //
        // Constructor
        //

        public McuStateEventArgs(McuToAppIOBuffer prevState, McuToAppIOBuffer curState, int itemNum=0) {
            mPrevState = prevState;
            mCurState = curState;
            mItemNum = itemNum;
        }

        //
        // Public Properties
        //

        public McuToAppIOBuffer PrevState
        {
            get { return mPrevState; }
        }

        public McuToAppIOBuffer CurState
        {
            get { return mCurState; }
        }

        public int ItemNum
        {
            get { return mItemNum;  }
        }
    }

    //
    //-------------------------------------------------------------------------
    // Event Args for for MCU control events
    //-------------------------------------------------------------------------
    //
    public class McuControlEventArgs : EventArgs
    {
        //
        // Private Members
        //

        private AppToMcuIOBuffer mMcuControl;
        private int mItemNum;

        //
        // Constructor
        //

        public McuControlEventArgs(AppToMcuIOBuffer mcuControl, int itemNum = 0)
        {
            mMcuControl = mcuControl;
            mItemNum = itemNum;
        }

        //
        // Public Properties
        //

        public AppToMcuIOBuffer McuControl
        {
            get { return mMcuControl; }
        }

        public int ItemNum
        {
            get { return mItemNum; }
        }
    }

    //
    //-------------------------------------------------------------------------
    // Event Args for for error events
    //-------------------------------------------------------------------------
    //
    public class ErrorEventArgs : EventArgs
    {
        //
        // Private Members
        //

        private Exception mException;

        //
        // Constructor
        //

        public ErrorEventArgs(Exception inExcept)
        {
            mException = inExcept;
        }

        //
        // Public Properties
        //

        public Exception Error
        {
            get { return mException; }
        }

    }

    //
    //-------------------------------------------------------------------------
    // Event Args for for firmware update status events
    //-------------------------------------------------------------------------
    //
    public class FirmwareUpdateEventArgs : EventArgs
    {
        //
        // Private Members
        //

        private MCU_FLASH_STATE mFlashState;
        private int mBytesWritten;
        private int mTotalBytesToWrite;

        //
        // Constructor
        //

        public FirmwareUpdateEventArgs(MCU_FLASH_STATE flashState, int bytesWritten, int totalBytesToWrite)
        {
            mFlashState = flashState;
            mBytesWritten = bytesWritten;
            mTotalBytesToWrite = totalBytesToWrite;
        }

        //
        // Public Properties
        //

        public MCU_FLASH_STATE FlashState
        {
            get { return mFlashState; }
        }

        public int BytesWritten
        {
            get { return mBytesWritten; }
        }

        public int TotalBytesToWrite
        {
            get { return mTotalBytesToWrite; }
        }

    }

    //
    //-------------------------------------------------------------------------
    // Exception classes
    //-------------------------------------------------------------------------
    //

    public class SerialPortException : Exception
    {
        public SerialPortException() :
            base()
        {
            // nothing to do
        }

        public SerialPortException(string message) :
            base(message)
        {
            // nothing to do
        }

        public SerialPortException(string message, Exception innerException) :
            base(message, innerException)
        {
            // nothing to do
        }
    }

    public class SerialPortInputException : SerialPortException
    {
        public SerialPortInputException() :
            base()
        {
            // nothing to do
        }

        public SerialPortInputException(string message) :
            base(message)
        {
            // nothing to do
        }

        public SerialPortInputException(string message, Exception innerException) :
            base(message, innerException)
        {
            // nothing to do
        }
    }

    public class SerialPortOutputException : SerialPortException
    {
        public SerialPortOutputException() :
            base()
        {
            // nothing to do
        }

        public SerialPortOutputException(string message) :
            base(message)
        {
            // nothing to do
        }

        public SerialPortOutputException(string message, Exception innerException) :
            base(message, innerException)
        {
            // nothing to do
        }
    }

    //
    //-------------------------------------------------------------------------
    // States associated with updating the MCU firmware
    //-------------------------------------------------------------------------
    //
    public enum MCU_FLASH_STATE
    {
        ENTER_BOOTLOADER,
        IN_BOOTLOADER,
        ERASE_CMD,
        ERASE_ALL,
        WRITE_COMMAND,
        WRITE_ADDRESS,
        DATA_WRITTEN,
        ALL_DATA_WRITTEN,
        GO_CMD,
        GO_ADDR,
        COMPLETE_SUCCESS,
        COMPLETE_FAILED,
    }

    //
    //-------------------------------------------------------------------------
    // Base Serial Port class
    //-------------------------------------------------------------------------
    //
    public class ChoiceMcuCom : SerialPort
    {
        //
        // Private Members
        //

        private ChoiceMcuIOBuffer mInBuffer;
        private List<byte> dataBuffer = new List<byte>(StaticValues.McuToAppIOBufferSize * 2); // large enough to catch frame overruns

        private int mNumberOfBytesThatWereRead;
        private ChoiceMcuIO mMCU;

        private Object mInputLock = new Object();
        private Object mOutputLock = new Object();

        private static byte FLASH_ACK = 0x79;
        private static byte FLASH_NACK = 0x1F;
        private static uint FLASH_START_ADDR = 0x08000000;

        private const int FLASH_COMMAND_TIMEOUT = 10; // seconds
        private const int FLASH_ERASE_TIMEOUT = 60;   // seconds

        private byte[] mFlashOutBuffer;
        private uint mFlashAddr;

        private byte[] mFlashData;  // The data to be flashed
        private int mBytesWritten;  // The number of bytes flashed

        private volatile MCU_FLASH_STATE mFlashState = MCU_FLASH_STATE.ENTER_BOOTLOADER;

        //
        // Constructors
        //

        public ChoiceMcuCom(ChoiceMcuIO mcuIO, ChoiceMcuIOBuffer theDataBuffer, string portName=null, int baudRate=115200) 
        {
            ReadBufferSize = StaticValues.McuToAppIOBufferSize * 4; // plenty of buffer for several MCU messages
            mNumberOfBytesThatWereRead = 0;
            mInBuffer = theDataBuffer;
            mMCU = mcuIO;
            dataBuffer.Clear();

            BaudRate = baudRate;
            DataBits = 8;

            this.Parity = Parity.Even;

            ReadTimeout = 500;   // ms
            WriteTimeout = 500;  // ms

            DataReceived += DataReceivedHandler;

            if (portName != null)
            {
                Open(portName);
            }
        }

        //
        // Open the serial port
        //
        public void Open(string portName=null)
        {
            if (base.IsOpen)
            {
                base.Close();
            }

            if (portName != null)
            {
                PortName = portName;
            }

            try
            {
                base.Open();
            }
            catch (Exception openExcept)
            {
                logError(new SerialPortException("Error opening serial port: " + PortName, openExcept));
            }
        }

        //
        // Called on a secondary thread when data is available
        //
        //
        void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            StaticValues.FirmwareResponds = true;
            lock (mInputLock)
            {
                try
                {
                    int numberOfBytesForThisRead = BytesToRead; // BytesToRead can change while the data is being handled. Use the entry value.
                    byte[] localBuffer = new byte[numberOfBytesForThisRead];
                    int bytesRead = Read(localBuffer, 0, numberOfBytesForThisRead);
                    if(bytesRead == 1 && localBuffer[0] == FLASH_ACK)
                    {
                        onFlashAck();
                        return;
                    }
                    else if (bytesRead == 1 && localBuffer[0] == FLASH_NACK)
                    {
                        onFlashNack();
                        return;
                    }

                    dataBuffer.AddRange(localBuffer.Take<byte>(bytesRead));
                    int dataCount = dataBuffer.Count();
                    while (dataCount > 31)// Are there enough bytes for a frame
                    {
                        for(int index = 0; index<dataCount-1; index++)
                        {
                            if ((dataBuffer.ElementAt<byte>(index) == mInBuffer.CONST_HEADER0) &&
                                (dataBuffer.ElementAt<byte>(index + 1) == mInBuffer.CONST_HEADER1) && ((dataCount - index) > 31))
                            {
                                // If this is a good frame, the last byte should be the checksum of the preceeding 31
                                byte checksum = 0;
                                for (int checksumIndex = index; checksumIndex < index + 31; checksumIndex++)
                                {
                                    checksum += dataBuffer[checksumIndex];
                                }
                                if (checksum == dataBuffer[index + 31])
                                {
                                    // the frame is good
                                    dataBuffer.CopyTo(index, mInBuffer.IOBuffer, 0, 32);
                                    // Generate an event on the MCU IO object and get a new IO buffer
                                    mInBuffer = mMCU.InvokeMcuBufferReceived(mInBuffer);
                                    dataBuffer.RemoveRange(0, index + 32); // note that the second number is the count, not the ending index
                                    break;
                                }
                            }
                        }
                        dataCount = dataBuffer.Count();
                    }
                }
                catch (Exception inExcept)
                {
                    logError(new SerialPortInputException("Serial port input exception: " + PortName, inExcept));
                }
            }
        }

        //
        // Sends the specified theIOBuffer on a new thread
        //
        public void SendIOBuffer(ChoiceMcuIOBuffer ioBuffer)
        {
            if (ioBuffer == null)
            {
                return;
            }

            Thread sendThread = new Thread(() => doSendIOBuffer(ioBuffer));
            sendThread.Start();
        }

        //
        // Writes the specified theIOBuffer out the serial port
        //
        private void doSendIOBuffer(ChoiceMcuIOBuffer ioBuffer)
        {
            lock (mOutputLock)
            {
                try
                {
                    // Ensure the checksum is updated
                    ioBuffer.SetChecksum();

                    // Send the bytes
                    Write(ioBuffer.IOBuffer, 0, ioBuffer.IOBuffer.Length);
                }
                catch (Exception outExcept)
                {
                    logError(new SerialPortInputException("Serial port output exception: " + PortName, outExcept));
                }
            }
        }

        private void logError(Exception inException)
        {
            mMCU.InvokeLogError(inException);
        }

        //
        // Flashes the specified byte array into the MCU's firmware flash memory
        //
        public void FlashFirmware(byte[] fwBytes, bool bRetry=false)
        {
            StaticValues.FirmwareStarted = true;
            Thread flashThread = new Thread(() => doFlashFirmware2(fwBytes, bRetry));
            flashThread.Start();
        }

        private void doFlashFirmware2(byte[] fwBytes, bool bRetry)
        {
            DataReceived -= DataReceivedHandler;
            DataReceived += FlashReceivedHandler;

            int prevBaudrate = BaudRate;            

            try
            {
                //
                // Ensure an even 256 bytes of flash data
                //
                int extraBytes = fwBytes.Length & 0xff;
                if (extraBytes == 0)
                {
                    mFlashData = fwBytes;
                }
                else
                {
                    int newLen = fwBytes.Length + 256 - extraBytes;
                    mFlashData = new byte[newLen];
                    System.Array.Copy(fwBytes, mFlashData, fwBytes.Length);
                    for (int i = fwBytes.Length; i < newLen; i++)
                    {
                        mFlashData[i] = 0xFF;  // per the App Note fill with 0xFF
                    }
                }

                mBytesWritten = 0;

                mFlashOutBuffer = new byte[512];
                mFlashAddr = FLASH_START_ADDR;

                mFlashState = MCU_FLASH_STATE.ENTER_BOOTLOADER;

                // If retrying...
                if (bRetry)
                {
                    // Assume we're in the bootloader
                    mFlashState = MCU_FLASH_STATE.IN_BOOTLOADER;
                }

                while (mFlashState != MCU_FLASH_STATE.COMPLETE_SUCCESS && mFlashState != MCU_FLASH_STATE.COMPLETE_FAILED)
                {

                    Thread.Sleep(50);  // Allow a little time after receiving an Ack

                    //
                    // Take action based on the new state (Ack received)
                    //
                    switch (mFlashState)
                    {
                        case MCU_FLASH_STATE.ENTER_BOOTLOADER:
                            Thread.Sleep(5000); // Ensure the background update has stopped
                            
                            //
                            // Send the MCU firmware command that puts the MCU in bootloader mode
                            //
                            mFlashOutBuffer[0] = 0xC3;
                            mFlashOutBuffer[1] = 0xA5;
                            mFlashOutBuffer[2] = 0xAA;
                            mFlashOutBuffer[3] = 0xFF;
                            mFlashOutBuffer[4] = 0x00;
                            mFlashOutBuffer[5] = 0x00;
                            mFlashOutBuffer[6] = 0x00;
                            mFlashOutBuffer[7] = 0x00;
                            mFlashOutBuffer[8] = 0x00;
                            mFlashOutBuffer[9] = 0x00;
                            mFlashOutBuffer[10] = 0x00;
                            mFlashOutBuffer[11] = 0x00;
                            mFlashOutBuffer[12] = 0x00;
                            mFlashOutBuffer[13] = 0x00;
                            mFlashOutBuffer[14] = 0x00;
                            mFlashOutBuffer[15] = 0x11;

                            Write(mFlashOutBuffer, 0, 16);
                            Thread.Sleep(5000); // Wait for bootloader mode
                            FlushInputBytes();

                            //
                            // Send the enter boot loader command mode byte
                            //
                            mFlashOutBuffer[0] = 0x7F;

                            Write(mFlashOutBuffer, 0, 1);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.IN_BOOTLOADER);
                            break;

                        case MCU_FLASH_STATE.IN_BOOTLOADER:
                            // UI update
                            FlushInputBytes();
                            mMCU.InvokeFirmwareUpdate(mFlashState, mBytesWritten, mFlashData.Length);

                            //
                            // Send the Extended Erase Flash Bootloader Command
                            //
                            mFlashOutBuffer[0] = 0x44;
                            mFlashOutBuffer[1] = 0xBB;

                            Write(mFlashOutBuffer, 0, 2);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.ERASE_CMD);
                            break;

                        case MCU_FLASH_STATE.ERASE_CMD:
                            //
                            // Send the global mass erase parameter value
                            //
                            FlushInputBytes();
                            mFlashOutBuffer[0] = 0xFF;  // Global mass erase
                            mFlashOutBuffer[1] = 0xFF;
                            mFlashOutBuffer[2] = 0x00;  // Checksum

                            Write(mFlashOutBuffer, 0, 3);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.ERASE_ALL, FLASH_ERASE_TIMEOUT);
                            break;

                        case MCU_FLASH_STATE.ERASE_ALL:
                            // UI update
                            FlushInputBytes();
                            mMCU.InvokeFirmwareUpdate(mFlashState, mBytesWritten, mFlashData.Length);

                            //
                            // Send the write flash command
                            //
                            mFlashOutBuffer[0] = 0x31;
                            mFlashOutBuffer[1] = 0xCe;

                            Write(mFlashOutBuffer, 0, 2);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.WRITE_COMMAND);
                            break;

                        case MCU_FLASH_STATE.WRITE_COMMAND:
                            //
                            // Send the write flash address
                            //
                            FlushInputBytes();
                            mFlashOutBuffer[0] = (byte)(mFlashAddr >> 24);
                            mFlashOutBuffer[1] = (byte)(mFlashAddr >> 16);
                            mFlashOutBuffer[2] = (byte)(mFlashAddr >> 8);
                            mFlashOutBuffer[3] = (byte)mFlashAddr;
                            mFlashOutBuffer[4] = (byte)(mFlashOutBuffer[0] ^ mFlashOutBuffer[1] ^ mFlashOutBuffer[2] ^ mFlashOutBuffer[3]);

                            Write(mFlashOutBuffer, 0, 5);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.WRITE_ADDRESS);
                            break;

                        case MCU_FLASH_STATE.WRITE_ADDRESS:
                            //
                            // Send the data bytes to be written
                            //
                            FlushInputBytes();
                            mFlashOutBuffer[0] = (byte)255;  // Number of bytes to write minus 1
                            mFlashOutBuffer[257] = mFlashOutBuffer[0];  // Initialize the checksum
                            for (int i = 0; i < 256; i++)
                            {
                                mFlashOutBuffer[i + 1] = mFlashData[mBytesWritten + i];
                                mFlashOutBuffer[257] ^= mFlashOutBuffer[i + 1];
                            }

                            Write(mFlashOutBuffer, 0, 258);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.DATA_WRITTEN);
                            break;

                        case MCU_FLASH_STATE.DATA_WRITTEN:
                            mBytesWritten += 256;
                            mFlashAddr += 256;

                            // If all the flash data has been written...
                            if (mBytesWritten >= mFlashData.Length)
                            {
                                mFlashState = MCU_FLASH_STATE.ALL_DATA_WRITTEN;

                                // UI update
                                mMCU.InvokeFirmwareUpdate(mFlashState, mBytesWritten, mFlashData.Length);

                                //
                                // Send the Go command
                                //
                                mFlashOutBuffer[0] = 0x21;
                                mFlashOutBuffer[1] = 0xDE;

                                Write(mFlashOutBuffer, 0, 2);

                                // Read the response
                                ReadFlashResponse(MCU_FLASH_STATE.ALL_DATA_WRITTEN);
                            }
                            else
                            {
                                // UI update
                                mMCU.InvokeFirmwareUpdate(mFlashState, mBytesWritten, mFlashData.Length);

                                //
                                // Send the write flash command for the next data chunk
                                //
                                mFlashOutBuffer[0] = 0x31;
                                mFlashOutBuffer[1] = 0xCE;

                                Write(mFlashOutBuffer, 0, 2);

                                // Read the response
                                ReadFlashResponse(MCU_FLASH_STATE.WRITE_COMMAND);
                            }

                            break;

                        case MCU_FLASH_STATE.ALL_DATA_WRITTEN:
                            //
                            // Send the Go address (jump address)
                            //
                            FlushInputBytes();
                            mFlashAddr = FLASH_START_ADDR;
                            mFlashOutBuffer[0] = (byte)(mFlashAddr >> 24);
                            mFlashOutBuffer[1] = (byte)(mFlashAddr >> 16);
                            mFlashOutBuffer[2] = (byte)(mFlashAddr >> 8);
                            mFlashOutBuffer[3] = (byte)mFlashAddr;
                            mFlashOutBuffer[4] = (byte)(mFlashOutBuffer[0] ^ mFlashOutBuffer[1] ^ mFlashOutBuffer[2] ^ mFlashOutBuffer[3]);

                            Write(mFlashOutBuffer, 0, 5);

                            // Read the response
                            ReadFlashResponse(MCU_FLASH_STATE.COMPLETE_SUCCESS);
                            break;

                        case MCU_FLASH_STATE.COMPLETE_SUCCESS:
                            // NOTE: Never executes because while() loop exits on this condition
                            break;

                        case MCU_FLASH_STATE.COMPLETE_FAILED:
                            // NOTE: Never executes because while() loop exits on this condition
                            break;
                    }

                } // while()

            }
            catch (Exception ex)
            {
                mFlashState = MCU_FLASH_STATE.COMPLETE_FAILED;
            }

            // Final state event
            mMCU.InvokeFirmwareUpdate(mFlashState, mBytesWritten, mFlashData.Length);

            DataReceived -= FlashReceivedHandler;
            DataReceived += DataReceivedHandler;
            StaticValues.FirmwareStarted = false;
        }

        //
        // Received an Ack from the bootloader (advance the state)
        //
        private void onFlashAck()
        {
            switch (mFlashState)
            {
                case MCU_FLASH_STATE.ENTER_BOOTLOADER:
                    mFlashState = MCU_FLASH_STATE.IN_BOOTLOADER;  // Ack received
                    break;

                case MCU_FLASH_STATE.IN_BOOTLOADER:
                    mFlashState = MCU_FLASH_STATE.ERASE_CMD;  // Ack received
                    break;

                case MCU_FLASH_STATE.ERASE_CMD:
                    mFlashState = MCU_FLASH_STATE.ERASE_ALL;  // Ack received (flash erased)
                    break;

                case MCU_FLASH_STATE.ERASE_ALL:
                case MCU_FLASH_STATE.DATA_WRITTEN:
                    mFlashState = MCU_FLASH_STATE.WRITE_COMMAND;  // Ack received
                    break;

                case MCU_FLASH_STATE.WRITE_COMMAND:
                    mFlashState = MCU_FLASH_STATE.WRITE_ADDRESS;  // Ack received
                    break;

                case MCU_FLASH_STATE.WRITE_ADDRESS:
                    mFlashState = MCU_FLASH_STATE.DATA_WRITTEN;  // Ack received
                    break;

                case MCU_FLASH_STATE.ALL_DATA_WRITTEN:
                    mFlashState = MCU_FLASH_STATE.GO_CMD;  // Ack received
                    break;

                case MCU_FLASH_STATE.GO_CMD:
                    //mFlashState = MCU_FLASH_STATE.GO_ADDR;  // Ack received
                    mFlashState = MCU_FLASH_STATE.COMPLETE_SUCCESS;  // Indicate success on first Ack from Go Addr
                    break;

                case MCU_FLASH_STATE.GO_ADDR:
                    mFlashState = MCU_FLASH_STATE.COMPLETE_SUCCESS;  // 2nd Go Addr Ack recevied
                    break;

            }
        }

        //
        // Received an Nack from the bootloader
        //
        private void onFlashNack()
        {
            mFlashState = MCU_FLASH_STATE.COMPLETE_FAILED;
        }

        //
        // Called on a secondary thread when data is available
        //
        private void FlashReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            //
            // Because of different timeout values within the various flash state routines,
            // this handler will be delegated to 'ReadFlashResponse'. 
            //
        }

        //
        // Polls until a valid response from the MCU bootloader
        //
        private bool ReadFlashResponse(MCU_FLASH_STATE nextState, int timeoutSecs=FLASH_COMMAND_TIMEOUT)
        {
            bool rtnVal = false;

            try
            {
                DateTime startTime = DateTime.Now;
                bool bTimeout = false;
                TimeSpan deltaTime;
                int inByte = -1;

                while (inByte == -1 && !bTimeout)
                {
                    if (BytesToRead > 0)
                    {
                        inByte = ReadByte();
                    }

                    deltaTime = DateTime.Now - startTime;
                    if (deltaTime.Seconds > timeoutSecs)
                    {
                        bTimeout = true;
                    }

                    Thread.Sleep(10);
                }

                if (inByte == FLASH_ACK)
                {
                    mFlashState = nextState;
                    rtnVal = true;
                }
                else
                {
                    mFlashState = MCU_FLASH_STATE.COMPLETE_FAILED;
                }
            }
            catch (Exception inExcept)
            {
                logError(new SerialPortInputException("Serial port input exception: " + PortName, inExcept));
            }

            return rtnVal;
        }

        //
        // Flush all bytes in the input buffer
        //
        private void FlushInputBytes()
        {
            int inByte = 0;

            while (BytesToRead > 0)
            {
                inByte = ReadByte();
            }
        }

    }

    //
    //-------------------------------------------------------------------------
    // Container for MCU IO including state, control, and event dispatching.
    //-------------------------------------------------------------------------
    //
    public class ChoiceMcuIO
    {
        //
        // Private Members
        //

        protected McuToAppIOBuffer mMcuState;
        protected AppToMcuIOBuffer mMcuControl;
        protected Form mMainForm;
        protected ChoiceMcuCom mSerialCom;

        //
        // Constructor
        //
        public ChoiceMcuIO(Form mainForm, string portName=null)
        {
            //
            // Create default state and control objects
            //
            mMcuState = new McuToAppIOBuffer();
            mMcuControl = new AppToMcuIOBuffer();

            //
            // Initialize the form to be used to invoke events on the UI thread
            //
            mMainForm = mainForm;

            //
            // Serial port to MCU (receive the MCU state structure)
            //
            mSerialCom = new ChoiceMcuCom(this, mMcuState, portName);
        }

        //
        // Send the MCU control buffer
        //
        public void SendMcuControl(AppToMcuIOBuffer ioBuffer=null)
        {
            
            if (ioBuffer == null)
            {
                ioBuffer = mMcuControl;
            }

            // Create a new buffer to send
            AppToMcuIOBuffer sendBuffer = new AppToMcuIOBuffer();

            // Copy all the data from the source buffer
            System.Array.Copy(ioBuffer.IOBuffer, sendBuffer.IOBuffer, sendBuffer.IOBuffer.Length);

            // Reset the control bits
            ioBuffer.CONTROL = 0;

            // Write the data to the serial port
            mSerialCom.SendIOBuffer(sendBuffer); // starts a new thread
            System.Threading.Thread.Sleep(100); // SendMcuControl gets used like a memory read for status. It is not. The MCU needs time to act. TODO: Fix This
            Application.DoEvents(); // allow the system some time
        }

        public bool PerformEmptyChamberCalibration()
        {
            bool result = false;

            try
            {
                CONTROL.CalibrateEmpty();
                if (GetMcuState())
                {
                    StaticValues.CuvetteSensorThreshold_1 = Convert.ToUInt16(STATE.CUVETTE1_SENSOR * StaticValues.CuvetteSensorThresholdOffsetFromMaximum);
                    StaticValues.CuvetteSensorThreshold_2 = Convert.ToUInt16(STATE.CUVETTE2_SENSOR * StaticValues.CuvetteSensorThresholdOffsetFromMaximum);
                    StaticValues.CuvetteSensorThreshold_3 = Convert.ToUInt16(STATE.CUVETTE3_SENSOR * StaticValues.CuvetteSensorThresholdOffsetFromMaximum);

                    StaticValues.StoreSystemSettingsData();
                    result = true;
                }

                CONTROL.CalibrateNone(); // reset the bits in the CONTROL message so that they don't get sent later
            }
            catch
            {
                ErrorHandler.ReportErrorLogOnly(StaticValues.CouldNotSaveCuvetteThreshold, this.GetType().Name, MethodBase.GetCurrentMethod().Name);
            }

            return result;
        }

        public bool GetMcuState()
        {
            // send a command to the MCU and wait for a reply
            bool result = true;
            if(StaticValues.FirmwareStarted)
            {
                return result;
            }


            // if the header changes from 0 to a valid value then the MCU is working
            try
            {
                for (int i = 0; i < 10; i++) // rather than a time out, try up to 10 times to get a good data packet. If it doesn't happen return false
                {
                    result = true;

                    STATE.Clear();
                    SendMcuControl(); // elicit a response from the mcu
                    const int timeout_ms = 500;
                    DateTime startTime = DateTime.Now;
                    while ((STATE.HEADER0 != STATE.CONST_HEADER0) && (STATE.HEADER1 != STATE.CONST_HEADER1)) // wait for the response, header0 should equal CONST_HEADER0
                    {
                        
                        double timeSpent = DateTimeOffset.Now.Subtract(startTime).TotalMilliseconds;
                        if (timeSpent > timeout_ms)
                        {
                            result = false;
                            break;
                        }
                        Application.DoEvents();
                    }
                    if (result)
                        result = STATE.IsValid();

                    if (result)
                        break;
                }

            }
            catch
            {
                // if anything goes wrong the self test fails
                result = false;
            }
            return result;
        }


        //
        // The serial port to the MCU
        //
        public ChoiceMcuCom SerialCom
        {
            get { return mSerialCom;  }
        }

        //
        // The MCU STATE object (the most recently received MCU state)
        //
        public McuToAppIOBuffer STATE
        {
            get { return mMcuState; }
        }

        //
        // The MCU CONTROL object (the control state to be sent to the MCU)
        //
        public AppToMcuIOBuffer CONTROL
        {
            get { return mMcuControl; }
        }

        //
        // Event types
        //

        // A delegate type for MCU events
        public delegate void McuStateEventHandler(object sender, McuStateEventArgs e);

        // A delegate type for Error events
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

        // A delegate type for firmware update events
        public delegate void FirmwareUpdateEventHandler(object sender, FirmwareUpdateEventArgs e);

        //-----------------------------
        // Subscribable Events
        //-----------------------------

        //
        // Error Event
        //

        // Invoked when an unexpected error occurs (an exception was caught etc.)
        public event ErrorEventHandler OnError;

        //
        // MCU State Update
        //

        // Invoked every time an MCU state updated is received
        public event McuStateEventHandler McuStateUpdate;

        //
        // Button Events
        //

        public event McuStateEventHandler ButtonTopLeft;       // Button 1
        public event McuStateEventHandler ButtonMiddleLeft;    // Button 2
        public event McuStateEventHandler ButtonBottomLeft;    // Button 3
        public event McuStateEventHandler ButtonTopRight;      // Button 4
        public event McuStateEventHandler ButtonMiddleRight;   // Button 5
        public event McuStateEventHandler ButtonBottomRight;   // Button 6


        //
        // Fan Events
        //
        public event McuStateEventHandler FanStarted;
        public event McuStateEventHandler FanStopped;

        //
        // Error Event
        //

        // Invoked when a firmware status update occurs
        public event FirmwareUpdateEventHandler OnFirmwareUpdate;

        //
        // Log an error
        //
        protected void logError(Exception except)
        {
            try
            {
                OnError(this, new ErrorEventArgs(except));
            }
            catch (Exception Ex)
            {
                // TODO: Very bad... exception logging threw an exception!
                //ErrorHandler.ReportErrorLogOnly(StaticValues.ChoiceErrorLoggingMcuIOError, this.GetType().Name, MethodBase.GetCurrentMethod().Name, Ex.Message);
            }
        }

        //
        // Invoke error handlers
        //
        public void InvokeFirmwareUpdate(MCU_FLASH_STATE flashState, int bytesWritten, int totalBytesToWrite)
        {
            // Invoke on a new thread so the blocking Invoke call doesn't block RS232 input
            new Thread(() => mMainForm.Invoke((MethodInvoker)delegate
            {
                try
                {
                    OnFirmwareUpdate(this, new FirmwareUpdateEventArgs(flashState, bytesWritten, totalBytesToWrite));
                }
                catch (Exception)
                {
                    // another empty catch. TODO: FixMe 
                }
            })).Start();

            Thread.Sleep(1);

        }

        //
        // Flashes the specified byte array into the MCU's firmware flash memory
        //
        public void FlashFirmware(byte[] fwBytes, bool bRetry=false)
        {
            mSerialCom.FlashFirmware(fwBytes, bRetry);
        }

        //
        // Invoke error handlers
        //
        public void InvokeLogError(Exception except)
        {
            // Invoke on a new thread so the blocking Invoke call doesn't block RS232 input
            new Thread(() => mMainForm.Invoke((MethodInvoker)delegate
            {
                logError(except); // invoke on UI thread
            })).Start();
            
        }

        //
        // Invoke receive handlers
        //
        public virtual ChoiceMcuIOBuffer InvokeMcuBufferReceived(ChoiceMcuIOBuffer ioBuffer)
        {
            McuToAppIOBuffer mcuState = ioBuffer as McuToAppIOBuffer;
            if (mcuState != null)
            {
                new Thread(() => mMainForm.Invoke((MethodInvoker)delegate
                {
                    OnMcuStateUpdate(mcuState); // invoke on UI thread
                })).Start();
            }

            return new McuToAppIOBuffer();
        }


        //
        // Receive updated MCU state
        //
        // The original version of this mixed events and states. The MCU firmware as of 2018-05-16, sends an event after any 
        //  control transmission from this application and anytime there is a state change (event) for the chamber lid, cuvettes, buttons or fans.
        // Arguably, only a button push or fan failure should be considered an event. The other sensors can be interrogated 
        //  periodically, or at the start of new  test states. For instance, the lid need only checked before a measurement is taken. An event
        //  propagating through the controls is much more complicated to debug and interpret than simply checking the state of the lid when necessary.
        // For consistancy, events and states shall separated. States will be limited in scope, providing information necessary for testing, whereas
        //  events will be used for information outside of the scope of testing.

        //  The next version of firmware should have an indication as to whether a message was generated from an event, a button push or failure of some sort
        //    of from a data request by this application

        protected void OnMcuStateUpdate(McuToAppIOBuffer mcuState)
        {
            if (!mcuState.IsValid())
            {
                return;
            }

            lock (this)
            {
                try
                {
                    //
                    // Update the current MCU state
                    //

                    McuToAppIOBuffer prevMcuState = mMcuState;
                    mMcuState = mcuState;


                    //
                    // Invoke Button Up/Down Events
                    //

                    // Notify the operating system of button events

                    if (mcuState.STATE_BUTTON_1 && (ButtonTopLeft != null))
                        ButtonTopLeft(this, new McuStateEventArgs(prevMcuState, mcuState));

                    if (mcuState.STATE_BUTTON_2 && (ButtonMiddleLeft != null))
                        ButtonMiddleLeft(this, new McuStateEventArgs(prevMcuState, mcuState));

                    if (mcuState.STATE_BUTTON_3 && (ButtonBottomLeft != null))
                        ButtonBottomLeft(this, new McuStateEventArgs(prevMcuState, mcuState));

                    if (mcuState.STATE_BUTTON_4 && (ButtonTopRight != null))
                        ButtonTopRight(this, new McuStateEventArgs(prevMcuState, mcuState));

                    if (mcuState.STATE_BUTTON_5 && (ButtonMiddleRight != null))
                        ButtonMiddleRight(this, new McuStateEventArgs(prevMcuState, mcuState));

                    if (mcuState.STATE_BUTTON_6 && (ButtonBottomRight != null))
                        ButtonBottomRight(this, new McuStateEventArgs(prevMcuState, mcuState));


                    // If any of the fans stops notify the operator
                    //  There are only two fans in the current system

                    if ((!mcuState.STATE_FAN_1) || (!mcuState.STATE_FAN_2) )
                    {
                        if (FanStopped != null) FanStopped(this, new McuStateEventArgs(prevMcuState, mcuState, 1));
                    }


                }
                catch (Exception except)
                {
                    logError(except);
                }
            }

        }

    }

    //
    //-------------------------------------------------------------------------
    // Extends the MCU IO Container to include the ability to receive
    // AppToMcuIOBuffer and generate corresponding control events.
    //
    // Note: This class allows a Windows App to simulate acting like the
    //       MCU firmware by receiving AppToMcuIOBuffer and sending
    //       McuToAppIOBuffer.
    //-------------------------------------------------------------------------
    //
    public class ChoiceMcuIO_RW : ChoiceMcuIO
    {
        //
        // Private Members
        //

        private McuToAppIOBuffer_RW mMcuState_RW;

        //
        // Constructor
        //
        public ChoiceMcuIO_RW(Form mainForm, string portName=null) :
            base(mainForm, null)
        {
            //
            // Create a RW MCU State object
            //
            mMcuState_RW = new McuToAppIOBuffer_RW();
            mMcuState = mMcuState_RW;

            //
            // Serial port to MCU (receive the MCU control structure)
            //
            mSerialCom = new ChoiceMcuCom(this, mMcuControl, portName);
        }

        //
        // The MCU STATE object (the most recently received MCU state)
        //
        public McuToAppIOBuffer_RW STATE_RW
        {
            get { return mMcuState_RW; }
        }

        //
        // Event types
        //

        // A delegate type for MCU control events
        public delegate void McuControlEventHandler(object sender, McuControlEventArgs e);


        //-----------------------------
        // Subscribable Events
        //-----------------------------

        //
        // MCU State Update
        //

        // Invoked every time an MCU state update is received
        public event McuControlEventHandler McuControlUpdate;

        //
        // Start Fan Control Events
        //

        public event McuControlEventHandler CmdStartFan;

        public event McuControlEventHandler CmdStartFan1;
        public event McuControlEventHandler CmdStartFan2;
        public event McuControlEventHandler CmdStartFan3;
        public event McuControlEventHandler CmdStartFan4;
        public event McuControlEventHandler CmdStartFan5;
        public event McuControlEventHandler CmdStartFan6;

        //
        // Stop Fan Control Events
        //

        public event McuControlEventHandler CmdStopFan;

        public event McuControlEventHandler CmdStopFan1;
        public event McuControlEventHandler CmdStopFan2;
        public event McuControlEventHandler CmdStopFan3;
        public event McuControlEventHandler CmdStopFan4;
        public event McuControlEventHandler CmdStopFan5;
        public event McuControlEventHandler CmdStopFan6;

        //
        // White LED Control Events
        //

        public event McuControlEventHandler CmdWhiteLedOn;
        public event McuControlEventHandler CmdWhiteLedOff;

        //
        // NIR LED Control Events
        //

        public event McuControlEventHandler CmdNirLedOn;
        public event McuControlEventHandler CmdNirLedOff;

        //
        // Receive an updated MCU IO buffer (on a secondary thread)
        //
        public override ChoiceMcuIOBuffer InvokeMcuBufferReceived(ChoiceMcuIOBuffer ioBuffer)
        {
            McuToAppIOBuffer mcuState = ioBuffer as McuToAppIOBuffer;
            if (mcuState != null)
            {
                new Thread(() => mMainForm.Invoke((MethodInvoker)delegate
                {
                    OnMcuStateUpdate(mcuState); // invoke on UI thread
                })).Start();
            }
            else
            {
                AppToMcuIOBuffer mcuControl = ioBuffer as AppToMcuIOBuffer;
                if (mcuControl != null)
                {
                    new Thread(() => mMainForm.Invoke((MethodInvoker)delegate
                    {
                        onMcuControlUpdate(mcuControl); // invoke on UI thread
                    })).Start();

                    return new AppToMcuIOBuffer();
                }
            }

            return new McuToAppIOBuffer();
        }

        //
        // Send the MCU state buffer
        //
        public void SendMcuState(McuToAppIOBuffer ioBuffer = null)
        {
            if (ioBuffer == null)
            {
                ioBuffer = mMcuState;
            }

            // Create a new buffer to send
            McuToAppIOBuffer sendBuffer = new McuToAppIOBuffer();

            // Copy all data
            System.Array.Copy(ioBuffer.IOBuffer, sendBuffer.IOBuffer, ioBuffer.IOBuffer.Length);

            // Write the data to the serial port
            mSerialCom.SendIOBuffer(sendBuffer);
        }

        //
        // Receive an MCU control command
        //
        private void onMcuControlUpdate(AppToMcuIOBuffer mcuControl)
        {
            if (!mcuControl.IsValid())
            {
                logError(new Exception("Invalid AppToMcuIOBuffer buffer received"));
                return;
            }

            lock (this)
            {
                try
                {
                    //
                    // Update the current MCU control
                    //

                    mMcuControl = mcuControl;

                    //
                    // Consolidate the control and state bytes into an int
                    //

                    int controlVal = (mcuControl.CONTROL << 8) | mcuControl.STATE;

                    //
                    // Invoke the generic MCU control update event
                    //

                    if (McuControlUpdate != null)
                    {
                        McuControlUpdate(this, new McuControlEventArgs(mcuControl, controlVal));
                    }

                    if (mcuControl.CONTROL == 0)
                    {
                        // No command bits set.... nothing else to do
                        return;
                    }

                    if (mcuControl.CONTROL_WHITE_LED)
                    {
                        if (mcuControl.STATE_WHITE_LED)
                        {
                            if (CmdWhiteLedOn != null)
                                CmdWhiteLedOn(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdWhiteLedOff != null)
                                CmdWhiteLedOff(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_NIR_LED)
                    {
                        if (mcuControl.STATE_NIR_LED)
                        {
                            if (CmdNirLedOn != null)
                                CmdNirLedOn(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdNirLedOff != null)
                                CmdNirLedOff(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_1)
                    {
                        if (mcuControl.STATE_FAN_1)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 1));
                            if (CmdStartFan1 != null)
                                CmdStartFan1(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 1));
                            if (CmdStopFan1 != null)
                                CmdStopFan1(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_2)
                    {
                        if (mcuControl.STATE_FAN_2)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 2));
                            if (CmdStartFan2 != null)
                                CmdStartFan2(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 2));
                            if (CmdStopFan2 != null)
                                CmdStopFan2(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_3)
                    {
                        if (mcuControl.STATE_FAN_3)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 3));
                            if (CmdStartFan3 != null)
                                CmdStartFan3(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 3));
                            if (CmdStopFan3 != null)
                                CmdStopFan3(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_4)
                    {
                        if (mcuControl.STATE_FAN_4)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 4));
                            if (CmdStartFan4 != null)
                                CmdStartFan4(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 4));
                            if (CmdStopFan4 != null)
                                CmdStopFan4(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_5)
                    {
                        if (mcuControl.STATE_FAN_5)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 5));
                            if (CmdStartFan5 != null)
                                CmdStartFan5(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 5));
                            if (CmdStopFan5 != null)
                                CmdStopFan5(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                    if (mcuControl.CONTROL_FAN_6)
                    {
                        if (mcuControl.STATE_FAN_6)
                        {
                            if (CmdStartFan != null)
                                CmdStartFan(this, new McuControlEventArgs(mcuControl, 6));
                            if (CmdStartFan6 != null)
                                CmdStartFan6(this, new McuControlEventArgs(mcuControl));
                        }
                        else
                        {
                            if (CmdStopFan != null)
                                CmdStopFan(this, new McuControlEventArgs(mcuControl, 6));
                            if (CmdStopFan6 != null)
                                CmdStopFan6(this, new McuControlEventArgs(mcuControl));
                        }
                    }

                }
                catch (Exception except)
                {
                    logError(except);
                }
            }
        }

    }


}
