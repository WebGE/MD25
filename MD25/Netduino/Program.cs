using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

using testMicrotoolsKit.Hardware.MotorDrivers;

namespace Netduino
{
    public class Program
    {
        public static void Main()
        {
            MotorControlMD2x MD25 = new MotorControlMD2x();

            try
            {
                MD25.TransactionTimeOut = 100;
                Debug.Print("Vers.=" + MD25.SoftRev.ToString());
                Debug.Print("Voltage=" + ((Single)MD25.Battery / 10).ToString("N1") + "V");
                Debug.Print("Acceleration=" + MD25.AccelerationRate.ToString());
                Debug.Print("Mode=" + MD25.Mode.ToString());
            }
            catch (System.IO.IOException ex)
            {
                Debug.Print(ex.Message);
            }

            while (true)
            {
                try
                {
                    UInt32 i = 0;
                    MD25.RazEncoders(); MD25.AccelerationRate = 5;
                    MD25.SetSpeedTurn(140, 140);
                    while (MD25.Encoder1 < 2000)
                    {
                        i += 1;
                        Debug.Print(i + " " + "Codeur 1=" + MD25.Encoder1.ToString() + " " + "Codeur 2=" + MD25.Encoder2.ToString() +
                            " Speed1=" + MD25.Speed1.ToString() + " Speed2=" + MD25.Speed2Turn.ToString());
                    }
                    MD25.StopMotor();
                }
                catch (System.IO.IOException ex)
                {
                    Debug.Print(ex.Message);
                }

                try
                {
                    UInt32 i = 0;
                    MD25.RazEncoders(); MD25.AccelerationRate = 10;
                    MD25.SetSpeedTurn(255, 128);
                    while (MD25.Encoder1 < 2000)
                    {
                        i += 1;
                        Debug.Print(i + " " + "Codeur 1=" + MD25.Encoder1.ToString() + " " + "Codeur 2=" + MD25.Encoder2.ToString() +
                            " Speed1=" + MD25.Speed1.ToString() + " Speed2=" + MD25.Speed2Turn.ToString());
                    }
                    MD25.StopMotor();
                }
                catch (System.IO.IOException ex)
                {
                    Debug.Print(ex.Message);
                }
                finally
                {
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
