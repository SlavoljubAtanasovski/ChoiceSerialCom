# handle serial communication with firmware
import serial.tools.list_ports
import serial
import time

import numpy as np

       
class serialCom:
    def __init__(self, baud=115200, port="COM3"):
        self.baud=baud
        self.port=port
        self.serial=None
        
    def connect(self):
        self.serial=serial.Serial(self.port, self.baud, serial.EIGHTBITS, serial.PARITY_EVEN)
        
    def print_ports(self):
        comlist = serial.tools.list_ports.comports()
        connected = []
        for element in comlist:
            connected.append(element.device)
        print("Connected COM ports: " + str(connected))
    
    def flush(self):
        self.serial.flushInput()
        self.serial.flushOutput()
        
    def send(self, buffer):
        print('sending:',buffer)
        self.serial.write(buffer)
        
    def receive(self):
        bytes_available = self.serial.inWaiting()
        if bytes_available > 0:
            buffer=self.serial.read(bytes_available)
            print('received: ',bytes_available,' bytes',buffer)
            return buffer
        
    def close(self):
        if self.serial is not None:
            self.serial.close()
    


       
        
class MCU:
    AppToMcuIOBufferSize = 16
    McuToAppIOBufferSize = 32
    
    BYTE_HEADER0 = 0xc3
    BYTE_HEADER1 = 0xa5
    BIT_WHITE_LED = 0x01
    BIT_NIR_LED = 0x02
    BIT_FAN_1 = 0x04
    BIT_FAN_2 = 0x08
    BIT_FAN_3 = 0x10
    BIT_FAN_4 = 0x20
    BIT_FAN_5 = 0x40
    BIT_FAN_6 = 0x80

    BIT_WHITE_LED_DUTY_CYCLE = 0x01
    BIT_NIR_LED_DUTY_CYCLE = 0x02

    BIT_CUVETTE_EMPTY = 0x01
    BIT_CUVETTE_FULL = 0x02

    BYTE_HEADER_0_STATE = 0x5a
    BYTE_HEADER_1_STATE = 0x3c
    BYTE_PROTOCOL_VERSION_STATE = 0x01

    BIT_WHITE_LED_STATE = 0x01
    BIT_NIR_LED_STATE = 0x02
    BIT_RESERVED1_STATE = 0x04
    BIT_RESERVED2_STATE = 0x08
    BIT_CUVETTE_1_STATE = 0x10
    BIT_CUVETTE_2_STATE = 0x20
    BIT_CUVETTE_3_STATE = 0x40
    BIT_LID_SWITCH_STATE = 0x80

    BIT_BUTTON_1_STATE = 0x01 #top, left
    BIT_BUTTON_2_STATE = 0x02 #middle, left
    BIT_BUTTON_3_STATE = 0x04 #bottom, left
    BIT_BUTTON_4_STATE = 0x08 #top, right
    BIT_BUTTON_5_STATE = 0x10 #middle, right
    BIT_BUTTON_6_STATE = 0x20 #bottom, right
    BIT_RESERVED3_STATE = 0x40
    BIT_RESERVED4_STATE = 0x80

    BIT_FAN_1_STATE = 0x01
    BIT_FAN_2_STATE = 0x02
    BIT_FAN_3_STATE = 0x04
    BIT_FAN_4_STATE = 0x08
    BIT_FAN_5_STATE = 0x10
    BIT_FAN_6_STATE = 0x20
    BIT_RESERVED5_STATE = 0x40
    BIT_RESERVED6_STATE = 0x80
    
    def __init__(self, baud=115200, port="COM3"):
        
        self.serial = None
        self.baud = baud
        self.port = port
        self.error = False
        self.retry = 3  # number of retry in error
        
        #Create buffers
        self.in_buffer = bytearray(self.McuToAppIOBufferSize)
        self.out_buffer = bytearray(self.AppToMcuIOBufferSize)
        self.out_buffer[0] = self.BYTE_HEADER0
        self.out_buffer[1] = self.BYTE_HEADER1
        
        
        self.connect()
        if not self.error:
            self.flush()
            self.reset()

    # serial commands
    
    def connect(self):
        self.serial=serial.Serial(self.port, 
                                  self.baud, 
                                  serial.EIGHTBITS, 
                                  serial.PARITY_EVEN,
                                  timeout = 0.1,
                                  write_timeout = 0.1)
        
        self.error = not self.serial.is_open
        
    def close(self):
        if self.serial is not None:
            self.serial.close()
            
    def reset(self):
        # power up reset
        self.resetOutBuffer()
        self.send()
        time.sleep(0.05)
        self.clearState()
        self.receive()
        time.sleep(0.05)
    
    def print_ports(self):
        comlist = serial.tools.list_ports.comports()
        connected = []
        for element in comlist:
            connected.append(element.device)
        print("Connected COM ports: " + str(connected))
    
    def flush(self):
        self.serial.flushInput()
        self.serial.flushOutput()
        
    def read(self):
        result = False
        bytes_available = self.serial.inWaiting()
        if bytes_available > 0:
            try:
                self.in_buffer=self.serial.read(bytes_available)
                # print('received: ',bytes_available,' bytes',self.in_buffer)
                result = True
            except:
                print('serial read exception')
        
        return result
    
    def write(self):
        result = False
        try:
            self.serial.write(self.out_buffer)
            result = True
        except:
            print('serial write exception')
            
        return result

    def clearState(self):
        self.in_buffer = [0 for i in range(self.McuToAppIOBufferSize)]

    def PROTOCOL_VERSION(self):
        return self.in_buffer[2]
  

    def getUShort(self, offset):
        return self.in_buffer[offset+1]<<8|self.in_buffer[offset]
    
    def setUShort(self, offset, data):
        self.out_buffer[offset+1] = data>>8
        self.out_buffer[offset] = data & 0xff
        
    def isChecksumMatch(self):
        return self.in_buffer[-1] == self.computeChecksum(self.in_buffer)

    def isValidState(self):
        return (self.in_buffer[0]==self.BYTE_HEADER_0_STATE and
                self.in_buffer[1]==self.BYTE_HEADER_1_STATE and
                self.isChecksumMatch())
    
    def isValid(self):
        valid = (self.out_buffer[0]==self.BYTE_HEADER0 and 
                 self.out_buffer[1]==self.BYTE_HEADER1 and 
                 self.out_buffer[-1]==self.computeChecksum())
            
        return valid
    
    def resetOutBuffer(self):
        self.out_buffer = [0 for i in range(self.AppToMcuIOBufferSize)]
        self.out_buffer[0] = self.BYTE_HEADER0
        self.out_buffer[1] = self.BYTE_HEADER1

    def computeChecksum(self, buffer):
        checksum=0
        for byte in buffer[:-1]:
            checksum+=byte
        return checksum % 256

    def set_checksum(self):
        checksum_i = len(self.out_buffer)-1
        self.out_buffer[checksum_i]=0
       
        checksum=0
        for byte in self.out_buffer:
            checksum+=byte
        self.out_buffer[checksum_i]=checksum % 256
    
    # high level send/receive for MCU    
    def send(self):
        result = False
        self.set_checksum()
        # skip isValid()
        i = self.retry
        while i>0:
            if self.write():
                result = True
                break
            i -= 1
            time.sleep(0.05)
        
        return result
        
    def receive(self):
        result = False
        self.clearState()

        i = self.retry
        while i>0:
            if self.read():
                if self.isValidState():
                    result = True
                    break
            i -= 1
            time.sleep(0.05)
            
        return result
    
    # in_buffer functions
    def thermistor_1(self):
        return self.getUShort(6)*0.06-156.58

    def thermistor_2(self):
        return self.getUShort(8)*0.0058-281.5

    def thermistor_3(self):
        return self.getUShort(10)*0.0058-281.5

    def thermistor_4(self):
        return self.getUShort(12)*0.06-156.58

    def thermopile_1(self):
        return (self.getUShort(14)- 0x2DE4)*0.02-38.2

    def thermopile_2(self):
        return (self.getUShort(16)- 0x2DE4)*0.02-38.2

    def photodiode_1(self):
        return self.getUShort(18)

    def photodiode_2(self):
        return self.getUShort(20)

    def get_white_led_duty_cycle(self):
        return self.in_buffer[22]

    def get_nir_led_duty_cycle(self):
        return self.in_buffer[23]

    def cuvette1_sensor(self):
        return self.getUShort(24)

    def cuvette2_sensor(self):
        return self.getUShort(26)

    def cuvette3_sensor(self):
        return self.getUShort(28)

    def check_lid(self):
        return (self.in_buffer[3]&self.BIT_LID_SWITCH_STATE)==self.BIT_LID_SWITCH_STATE

    def check_button1(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_1_STATE)==self.BIT_BUTTON_1_STATE

    def check_button2(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_2_STATE)==self.BIT_BUTTON_2_STATE

    def check_button3(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_3_STATE)==self.BIT_BUTTON_3_STATE

    def check_button4(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_4_STATE)==self.BIT_BUTTON_4_STATE

    def check_button5(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_5_STATE)==self.BIT_BUTTON_5_STATE

    def check_button6(self):
        return (self.in_buffer[4]&self.BIT_BUTTON_6_STATE)==self.BIT_BUTTON_6_STATE
    
    def check_white_led(self):
        return (self.in_buffer[3]&self.BIT_WHITE_LED==self.BIT_WHITE_LED)

    def check_nir_led(self):
        return (self.in_buffer[3]&self.BIT_NIR_LED==self.BIT_NIR_LED)

    def check_fan1(self):
        return (self.in_buffer[5]&self.BIT_FAN_1_STATE==self.BIT_FAN_1_STATE)

    def check_fan2(self):
        return (self.in_buffer[5]&self.BIT_FAN_2_STATE==self.BIT_FAN_2_STATE)

    def check_fan3(self):
        return (self.in_buffer[5]&self.BIT_FAN_3_STATE==self.BIT_FAN_3_STATE)

    def check_fan4(self):
        return (self.in_buffer[5]&self.BIT_FAN_4_STATE==self.BIT_FAN_4_STATE)

    def check_fan5(self):
        return (self.in_buffer[5]&self.BIT_FAN_5_STATE==self.BIT_FAN_5_STATE)

    def check_fan6(self):
        return (self.in_buffer[5]&self.BIT_FAN_6_STATE==self.BIT_FAN_6_STATE)


    # out_buffer functions
    def control_white_led(self, onoff):
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_WHITE_LED
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_WHITE_LED or 
                              self.out_buffer[4]&~self.BIT_WHITE_LED&0xff)
            
        self.set_checksum()
        
    def control_nir_led(self, onoff):
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_NIR_LED
        self.out_buffer[4] = (onoff and 
                          self.out_buffer[4]|self.BIT_NIR_LED or 
                          self.out_buffer[4]&~self.BIT_NIR_LED&0xff)
        self.set_checksum()

    def control_fan1(self, onoff):
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_1
        self.out_buffer[4] = (onoff and 
                          self.out_buffer[4]|self.BIT_FAN_1 or 
                          self.out_buffer[4]&~self.BIT_FAN_1&0xff)
        self.set_checksum()

    def control_fan2(self, onoff):
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_2
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_FAN_2 or 
                              self.out_buffer[4]&~self.BIT_FAN_2&0xff)
        self.set_checksum()

    def control_fan3(self, onoff):   
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_3
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_FAN_3 or 
                              self.out_buffer[4]&~self.BIT_FAN_3&0xff)
        self.set_checksum()

    def control_fan4(self, onoff):   
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_4
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_FAN_4 or 
                              self.out_buffer[4]&~self.BIT_FAN_4&0xff)
        self.set_checksum()

    def control_fan5(self, onoff):   
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_5
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_FAN_5 or 
                              self.out_buffer[4]&~self.BIT_FAN_5&0xff)
        self.set_checksum()

    def control_fan6(self, onoff):   
        self.out_buffer[3] = self.out_buffer[3]|self.BIT_FAN_6
        self.out_buffer[4] = (onoff and 
                              self.out_buffer[4]|self.BIT_FAN_6 or 
                              self.out_buffer[4]&~self.BIT_FAN_6&0xff)
        self.set_checksum()

    def control_white_led_duty_cycle(self, dutyCycle):
        self.out_buffer[5] = self.out_buffer[5]|self.BIT_WHITE_LED_DUTY_CYCLE
        self.out_buffer[6] = dutyCycle

    def control_nir_led_duty_cycle(self, dutyCycle):
        self.out_buffer[5] = self.out_buffer[5]|self.BIT_NIR_LED_DUTY_CYCLE
        self.out_buffer[7] = dutyCycle
        
    def test(self, fname, num=10, t=1.0, nir=0, white=0):
        if nir>0:
            self.control_nir_led_duty_cycle(nir)
            self.control_nir_led(True)
        else:
            self.control_nir_led_duty_cycle(0)
            self.control_nir_led(False)
            
        if white>0:
            self.control_white_led_duty_cycle(white)
            self.control_white_led(True)
        else:
            self.control_white_led_duty_cycle(0)
            self.control_white_led(False)
            
        self.send()
        time.sleep(1)
        self.receive()
    
        i = 0
        while i<num:
            self.receive()
            T1 = self.thermistor_1()
            T2 = self.thermistor_2()
            T3 = self.thermistor_3()
            T4 = self.thermistor_4()
            T5 = self.thermopile_1()
            T6 = self.thermopile_2()
            V1 = self.photodiode_1()
            V2 = self.photodiode_2()
            S1 = self.cuvette1_sensor()
            S2 = self.cuvette2_sensor()
            S3 = self.cuvette3_sensor()
            L1 = self.get_nir_led_duty_cycle()
            L2 = self.get_white_led_duty_cycle()
        
            data_list = [i*t, T1, T2, T3, T4, T5, T6, V1, V2, S1, S2, S3, L1, L2]
        
            print(fname, data_list)
        
            if i==0:
                data = np.array([data_list])
            else:
                data = np.concatenate((data,[data_list]), axis=0)
       
            time.sleep(t)
            i+=1
            
            self.send()
        
    

        self.control_nir_led(False)
        self.control_white_led(False)
        self.send()

        np.savetxt(fname, data, delimiter=',', 
               header='i, T1(Ambient), T2 (Transmission), T3 (Scatter), T4 (Chamber), T5 (Thermopile Ambient), T6 (Sample), V1 (Transmission), V2 (Scatter), S1, S2, S3, L1 (NIR), L2 (White)')

        

if __name__ == "__main__":
    # from live_plot import *
    
    mcu = MCU()
    mcu.print_ports()
    
    state = True
    # mcu.resetOutBuffer()
    # mcu.control_white_led(state)
    # mcu.send()
    # time.sleep(0.05)
    # mcu.clearState()
    # mcu.receive()
    # time.sleep(0.05)
    
    data = []
    for i in range(20):
        t0 = time.time()
        mcu.resetOutBuffer()
        if i%5==0:
            state = not state
            mcu.control_white_led(state)
        if not mcu.send():
            print('write error')
        time.sleep(0.05)
        t1 = time.time()
        if not mcu.receive():
            print('read error')
        print(state, mcu.check_white_led())
        t2 = time.time()
        data.append((i, 
                     (t1-t0)*1000, 
                     (t2-t1)*1000, 
                     (t2-t0)*1000))
    
    print(*data, sep='\n')
    
    # mcu.control_nir_led_duty_cycle(30)

    # mcu.control_nir_led(True)
    # mcu.send()
    # time.sleep(1)
    
    # mcu.receive()
    # print('in buffer:', mcu.in_buffer)
    # print(mcu.get_nir_led_duty_cycle())
    # # time.sleep(1)

    # n = 100
    # t = 10.0
    # x = np.array([])
    # y = np.array([])
    # data = np.array([])
    # line1 = []
    
    # i = 0
    # while i<n:
    #     mcu.receive()
    #     T1 = mcu.thermistor_1()
    #     T2 = mcu.thermistor_2()
    #     T3 = mcu.thermistor_3()
    #     T4 = mcu.thermistor_4()
    #     T5 = mcu.thermopile_1()
    #     T6 = mcu.thermopile_2()
    #     V1 = mcu.photodiode_1()
    #     V2 = mcu.photodiode_2()
    #     S1 = mcu.cuvette1_sensor()
    #     S2 = mcu.cuvette2_sensor()
    #     S3 = mcu.cuvette3_sensor()
    #     L1 = mcu.get_nir_led_duty_cycle()
    #     L2 = mcu.get_white_led_duty_cycle()
        
    #     data_list = [i, T1, T2, T3, T4, T5, T6, V1, V2, S1, S2, S3, L1, L2]
        
    #     print('Values', data_list)
        
    #     if i==0:
    #         data = np.array([data_list])
    #     else:
    #         data = np.concatenate((data,[data_list]), axis=0)
       
    #     x = np.append(x,i)
    #     y = np.append(y,V1)
    #     #line1 = live_plotter_xy(x,y,line1,pause_time=t)
    #     time.sleep(t)
        
    #     i+=1
    #     # mcu.control_nir_led_duty_cycle(45+i)
    #     mcu.send()
        
    

    # mcu.control_nir_led(False)
    # mcu.send()
    # mcu.close()

    # np.savetxt('data_01.csv', data, delimiter=',', 
    #             header='i, T1(Ambient), T2 (Transmission), T3 (Scatter), T4 (Chamber), T5 (Thermopile Ambient), T6 (Sample), V1 (Transmission), V2 (Scatter), S1, S2, S3, L1 (NIR), L2 (White)')

        
        
    
