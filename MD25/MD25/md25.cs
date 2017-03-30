using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace testMicrotoolsKit
{
    namespace Hardware
    {
        namespace MotorDrivers
        {
            /// <summary>
            /// MD25 - 12v 2.8A dual H-bridge motor driver 
            /// </summary>
            /// <remarks>
            /// You may have some additional information about this class on https://github.com/WebGE/MD25
            /// </remarks>
            public class MotorControlMD2x
            {
                /// <summary>
                /// Transaction time out = 1s before throwing System.IO.IOException 
                /// </summary>
                private UInt16 transactionTimeOut = 1000;

                /// <summary>
                /// Slave Adress and frequency configuration
                /// </summary>
                private I2CDevice.Configuration config;

                private I2CDevice i2CBus;

                /// <summary>
                /// 7-bit Slave Adress
                /// </summary>
                private UInt16 sla;

                /// <summary>
                /// Writable Registers
                /// </summary>
                private enum WRegister
                {
                    Speed1 = 0,
                    Speed2Turn = 1,
                    AccelerationRate = 14,
                    Mode = 15,
                    Command = 16
                }

                public enum ModeRegister
                {
                    /// <summary>
                    /// (Default Setting) - The speed registers is literal speeds in the range of (see below)
                    /// </summary>
                    /// <remarks>   
                    /// Speed registers :  0 (Full Reverse) 128 (Stop) 255 (Full Forward)
                    /// </remarks>
                    Mode0,
                    /// <summary>
                    ///  Mode 1 is similar to Mode 0, except that the speed values are interpreted as signed values. 
                    /// </summary>
                    /// <remarks>
                    ///  Speed registers : -128 (Full Reverse) 0 (Stop) 127 (Full Forward).
                    /// </remarks>
                    Mode1,
                    /// <summary>
                    /// Speed1 control both motors speed, and speed2 becomes the
                    /// turn value. 
                    /// </summary>
                    /// <remarks>
                    /// Data is in the range of 0 (Full Reverse) 128 (Stop) 255 (Full Forward)
                    /// </remarks>
                    Mode2,
                    /// <summary>
                    /// Mode 3 is similar to Mode 2, except that the speed values are interpreted as signed values. 
                    /// </summary>
                    /// <remarks>
                    /// Data is in the range of -128 (Full Reverse) 0 (Stop) 127 (Full Forward) 
                    /// </remarks>
                    Mode3
                }

                // MD2x Registers
                private Byte speed1 = 0, speed2Turn = 0;
                private Byte softrev = 0;
                private Byte battery = 0;
                private Byte current1 = 0, current2 = 0;
                private Byte acceleration = 0;
                private Byte mode = 0;
                private Byte command = 0;
                private Int32 encoder1 = 0, encoder2 = 0;

                /// <summary>
                /// This constructor assumes the default factory Slave Address = 0x58 and default bus frequency = 100Khz
                /// </summary>
                public MotorControlMD2x()
                {
                    sla = 0x58;
                    config = new I2CDevice.Configuration(0x58, 100);
                }

                /// <summary>
                /// This constructor allows user to specify the Slave Address (bus frequency = 100Khz)
                /// </summary>
                /// <param name="SLA">MD25(@=0x58 to 0x5F), 0x58 by default (see datasheet)</param>
                public MotorControlMD2x(UInt16 SLA)
                {
                    sla = SLA;
                    config = new I2CDevice.Configuration(SLA, 100);
                }

                /// <summary>
                /// This constructor allows user to specify the Slave Address and the bus frequency
                /// </summary>
                /// <param name="SLA">MD25(@=0x58 to 0x5F), 0x58 by default (see datasheet)</param>
                /// <param name="Frequency">100khz to 400kHz, 100kHz by default</param>
                public MotorControlMD2x(UInt16 SLA, UInt16 Frequency)
                {
                    sla = SLA;
                    config = new I2CDevice.Configuration(SLA, Frequency);
                }

                /// <summary>
                /// Return motor1 speed1.
                /// </summary>
                public Byte Speed1
                {
                    get { GetAllRegisters(); return speed1; }
                }

                /// <summary>
                /// Return motor2 speed (mode 0,1) or turn (mode 2,3).
                /// </summary>
                public Byte Speed2Turn
                {
                    get { GetAllRegisters(); return speed2Turn; }
                }

                /// <summary>
                /// Return Encoder1 value (32-bit).
                /// </summary>
                public Int32 Encoder1
                {
                    get
                    { GetAllRegisters(); return encoder1; }
                }

                /// <summary>
                /// Return Encoder2 value (32-bit).
                /// </summary>
                public Int32 Encoder2
                {
                    get { GetAllRegisters(); return encoder2; }
                }

                /// <summary>
                /// Return as 10 times the voltage (121 for 12.1V)
                /// </summary>
                public Byte Battery
                {
                    get { GetAllRegisters(); return battery; }
                }

                /// <summary>
                /// Return the current through motor 1.
                /// </summary>
                public Byte Current1
                {
                    get { GetAllRegisters(); return current1; }
                }

                /// <summary>
                /// Return the current through motor 2.
                /// </summary>         
                public Byte Current2
                {
                    get { GetAllRegisters(); return current2; }
                }

                /// <summary>
                /// Return the software revision number.
                /// </summary>
                public Byte SoftRev
                {
                    get { GetAllRegisters(); return softrev; }
                }

                /// <summary>
                /// Set or return optional acceleration register (1, 2, 3, 5(default), 10). See datasheet to calculate the time (in seconds) for the acceleration to complete.
                /// </summary>
                /// <remarks>
                /// If you require a controlled acceleration period for the attached motors to reach there ultimate speed,
                /// the MD25 has the ability to provide this. It works by using a sent acceleration value and incrementing
                /// the power by that value. 
                /// </remarks>
                public Byte AccelerationRate
                {
                    set
                    {
                        if (value == 1 || value == 2 || value == 3 || value == 5 || value == 10)
                        {
                            SetRegister(WRegister.AccelerationRate, value);
                        }
                    }
                    get { GetAllRegisters(); return acceleration; }
                }

                /// <summary>
                /// Return the mode of operation among Mode0, Mode1, Mode2 or Mode3 
                /// </summary>
                public Byte Mode
                {
                    get { GetAllRegisters(); return mode; }
                }

                /// <summary>
                /// Set or return the command register value. 
                /// </summary>
                /// <remarks>
                /// 0x20(32) Resets the encoder registers to zero. 
                /// 0x30(48) Disables automatic speed regulation. 
                /// 0x31(49) Enables automatic speed regulation (default). 
                /// 0x32(50) Disables 2 second timeout of motors (Version 2 onwards only). 
                /// 0x33(51) Enables 2 second timeout of motors when no I2C comms (default) (Version 2 onwards only)
                /// See datasheet for more informations (changing the I2C Bus Address).
                /// </remarks>
                public Byte Command
                {
                    set { SetRegister(WRegister.Command, value); }
                    get { GetAllRegisters(); return command; }
                }

                /// <summary>
                /// Set or return time before System IO Exception if transaction failed (in ms).
                /// </summary>
                /// <remarks>
                /// 1000ms by default
                /// </remarks>
                public UInt16 TransactionTimeOut
                {
                    get
                    {
                        return transactionTimeOut;
                    }
                    
                    set
                    {
                        transactionTimeOut = value;
                    }
                }

                /// <summary>
                /// Set value on register
                /// </summary>
                /// <param name="Register">Register number (0 to 16)</param>
                /// <param name="value">Register value</param>
                private void SetRegister(WRegister Register, byte value)
                {
                    byte[] outbuffer = new byte[] { (byte)Register, value };
                    I2CDevice.I2CTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outbuffer);
                    I2CDevice.I2CTransaction[] T_WriteByte = new I2CDevice.I2CTransaction[] { writeTransaction };
                    i2CBus = new I2CDevice(config);
                    int transferred = i2CBus.Execute(T_WriteByte, transactionTimeOut);
                    i2CBus.Dispose();
                    if (transferred < outbuffer.Length)
                        throw new System.IO.IOException("I2CBus error:" + sla.ToString());
                }

                /// <summary>
                /// Read all registers and calculate Encoder1 and Encoder2 value.
                /// </summary>
                public void GetAllRegisters()
                {
                    byte[] outBuffer = new byte[] { 0 };
                    I2CDevice.I2CWriteTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outBuffer);
                    byte[] inBuffer = new byte[17];
                    I2CDevice.I2CReadTransaction readTransaction = I2CDevice.CreateReadTransaction(inBuffer);
                    I2CDevice.I2CTransaction[] XAction = new I2CDevice.I2CTransaction[] { writeTransaction, readTransaction };
                    i2CBus = new I2CDevice(config); // Connexion virtuelle de la carte MD2x au bus I2C
                    int transferred = i2CBus.Execute(XAction, transactionTimeOut);
                    i2CBus.Dispose();
                    if (transferred != 0)
                    {
                        speed1 = inBuffer[0]; speed2Turn = inBuffer[1]; battery = inBuffer[10]; current1 = inBuffer[11]; current2 = inBuffer[12];
                        softrev = inBuffer[13]; acceleration = inBuffer[14]; mode = inBuffer[15]; command = inBuffer[16];

                        encoder1 = (Int32)(inBuffer[2] << 24) | (Int32)(inBuffer[3] << 16) | (Int32)(inBuffer[4] << 8) | (Int32)(inBuffer[5]);
                        encoder2 = (Int32)(inBuffer[6] << 24) | (Int32)(inBuffer[7] << 16) | (Int32)(inBuffer[8] << 8) | (Int32)(inBuffer[9]);
                    }
                    else
                    {
                        throw new System.IO.IOException("I2CBus error:" + sla.ToString());
                    }
                }

                /// <summary>
                /// The mode register selects which mode of operation and I2C data input type the user requires. 
                /// </summary>
                /// <param name="valmode">Mode0, Mode1, Mode2 or Mode3 (see MotorControlMD2x.ModeRegister)</param>
                public void SetMode(ModeRegister valmode)
                {
                    SetRegister(WRegister.Mode, (byte)valmode);
                }

                /// <summary>
                /// Set encoder registers value (Enc1a,b,c,d and Enc2a,b,c,d) to 0.
                /// </summary>
                public void RazEncoders()
                {
                    SetRegister(WRegister.Command, 32);
                }

                /// <summary>
                /// Stop motors in all modes.
                /// </summary>
                public void StopMotor()
                {
                    GetAllRegisters();
                    if ((mode == (byte)ModeRegister.Mode0) || (mode == (byte)ModeRegister.Mode2))
                    {
                        speed1 = 128; speed2Turn = 128;
                    }
                    else if ((mode == (byte)ModeRegister.Mode1) || (mode == (byte)ModeRegister.Mode3))
                    {
                        speed1 = 0; speed2Turn = 0;
                    }
                    SetRegister(WRegister.Speed1, speed1);
                    SetRegister(WRegister.Speed2Turn, speed2Turn);
                }

                /// <summary>
                /// Adjusts the speed of the motors in mode 0 and 1. Speed and Turn in mode 2 and 3
                /// </summary>
                /// <remarks>
                /// Turn mode looks at the speed register to decide if the direction is forward or reverse. 
                /// Then it applies a subtraction or addition of the turn value on either motor.
                /// If the direction is forward motor speed1 = speed - turn, motor speed2 = speed + turn
                /// else the direction is reverse so motor speed1 = speed + turn, motor speed2 = speed - turn
                /// </remarks>
                /// <param name="ValSpeed1">Speed motor1 value</param>
                /// <param name="ValSpeed2Turn">Speed motor2 value (mode0 and 1). Turn value (mode 2 and 3) </param>
                public void SetSpeedTurn(byte ValSpeed1, byte ValSpeed2Turn)
                {
                    SetRegister(WRegister.Speed1, ValSpeed1);
                    SetRegister(WRegister.Speed2Turn, ValSpeed2Turn);
                }
            }
        }
    }
}
