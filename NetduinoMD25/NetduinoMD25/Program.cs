using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using MD2x;

namespace NetduinoMD25
{
    public class Program
    {
        public static void Main()
        {   // http://webge.github.io/MD25/
            // Programme de test de la carte MD25
            // Création d'un objet MotorControl (carte MD25)
            // avec l'adresse 0x58 et la fréquence de bus F = 100kHz
            MotorControlMD2x CarteMD25 = new MotorControlMD2x();

            // Pour info : Lecture des registres de la carte MD2x et affichage de la version du logiciel
            Debug.Print("Vers.=" + CarteMD25.SoftRev.ToString());
            Debug.Print("Tension=" + ((Single)CarteMD25.Battery / 10).ToString("N1") + "V");
            Debug.Print("Acceleration=" + CarteMD25.AccelerationRate.ToString());
            Debug.Print("Mode=" + CarteMD25.Mode.ToString());

            while (true)
            {
                // Essai : Rotation des moteurs jusqu'à ce que la distance recherchée soit atteinte
                // ---------------------------------------------------------------------
                CarteMD25.RazEncoders(); // Remise à zéro des codeurs
                CarteMD25.SetSpeedTurn(140, 140); // Réglage de la vitesse des moteurs

                while (CarteMD25.Encoder1 < 2000)
                {
                    Debug.Print("Codeur 1=" + CarteMD25.Encoder1.ToString() + " " + "Codeur 2=" + CarteMD25.Encoder2.ToString() + " Speed1=" + CarteMD25.Speed1.ToString() + " Speed2=" + CarteMD25.Speed2Turn.ToString());
                }
                CarteMD25.StopMotor();  // Arrêt des moteurs
                Thread.Sleep(5000);
            }          
        }
    }
}
