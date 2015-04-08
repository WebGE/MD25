// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// 01/07/2014
// Philippe Mariano
// Lycée Pierre Emile Martin
// 18000 Bourges
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
using System;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;


namespace ToolBoxes
{
    public class MotorControlMD2x
    {
        // Constantes
        private const Int16 TRANSACTIONEXECUTETIMEOUT = 1000;

        // Attributs
        private I2CDevice busI2C;
        private I2CDevice.Configuration configMD2x;

        private enum WRegister
        {
            Speed1=0,
            Speed2Turn=1,
            AccelerationRate=14,
            Mode=15,
            Command=16
        }
        public enum ModeRegister
	    {
            Mode0, 
            Mode1, 
            Mode2,
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

        // Constructors
        // this constructor assumes the default factory Slave Address = 0x58
        public MotorControlMD2x()
        {
            configMD2x = new I2CDevice.Configuration(0x58, 100);
        }
        // This constructor allows user to specify the Slave Address and bus frequency = 100khz
        public MotorControlMD2x(byte I2C_Add_7bits)
        {
            configMD2x = new I2CDevice.Configuration(I2C_Add_7bits, 100);
        }
        // This constructor allows user to specify the Slave Address and bus frequency
        public MotorControlMD2x(byte I2C_Add_7bits, UInt16 Frequency)
        {
            configMD2x = new I2CDevice.Configuration(I2C_Add_7bits, Frequency);
        }
        
        // Propriétés
        public Byte Speed1
        {
            get { GetAllRegisters(); return speed1; }
        }
        public Byte Speed2Turn
        {
            get { GetAllRegisters(); return speed2Turn; }
        }
        public Int32 Encoder1
        {
            get 
                { GetAllRegisters(); return encoder1; }
        }     // Mots de 32 bits, signés contenant la valeur des codeurs
        public Int32 Encoder2
        {
            get { GetAllRegisters(); return encoder2; }
        }
        public Byte Battery
        {
            get { GetAllRegisters(); return battery; }
        }        // Tension d'allimentaion de la carte
        public Byte Current1
        {
            get { GetAllRegisters(); return current1; }
        }       // Intensité du courant consommée par les moteurs
        public Byte Current2
        {
            get { GetAllRegisters(); return current2; }
        }
        public Byte SoftRev
        {
            get { GetAllRegisters(); return softrev; }
        }        // Version du soft
        public Byte AccelerationRate
        {
            get { GetAllRegisters(); return acceleration; }
        }   // Rampe d'accélération
        public Byte Mode
        {
            get { GetAllRegisters(); return mode; }
        }               // Mode de fonctionnement de la carte
        public Byte Command               // Divers fonctionnalités (Raz encodeur etc...)
        {
            get { GetAllRegisters(); return command; }
        }

        // Private methode
        private int SetRegister(WRegister Register, byte value)
        {
            // Création d'un buffer et d'une transaction pour l'accès au circuit en écriture
            byte[] outbuffer = new byte[] { (byte)Register, value };
            I2CDevice.I2CTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outbuffer);
            // Tableaux des transactions 
            I2CDevice.I2CTransaction[] T_WriteByte = new I2CDevice.I2CTransaction[] { writeTransaction };
            busI2C = new I2CDevice(configMD2x); // Connexion virtuelle de l'objet MD2x au bus I2C 
            busI2C.Execute(T_WriteByte, TRANSACTIONEXECUTETIMEOUT); // Exécution de la transaction
            busI2C.Dispose(); // Déconnexion virtuelle de l'objet MD2x du bus I2C
            return 1;
        }

        // Public methode
        /// <summary>
        /// Lecture des 17 registres de la carte MD2x
        /// et calcul de la valeur des encodeurs
        /// </summary>
        /// <returns></returns>
        public int GetAllRegisters()
        {
            // Buffer d'écriture
            byte[] outBuffer = new byte[] { 0 };
            I2CDevice.I2CWriteTransaction writeTransaction = I2CDevice.CreateWriteTransaction(outBuffer);

            // Buffer de lecture
            byte[] inBuffer = new byte[17];
            I2CDevice.I2CReadTransaction readTransaction = I2CDevice.CreateReadTransaction(inBuffer);

            // Tableau des transactions
            I2CDevice.I2CTransaction[] transactions = new I2CDevice.I2CTransaction[] { writeTransaction, readTransaction };
            // Exécution des transactions
            busI2C = new I2CDevice(configMD2x); // Connexion virtuelle de la carte MD2x au bus I2C

            if (busI2C.Execute(transactions, TRANSACTIONEXECUTETIMEOUT) != 0)
            {
                // Success
                //Debug.Print("Received the first data from at device " + busI2C.Config.Address + ": " + ((int)inBuffer[0]).ToString());
                // Sauvegarde de la valeur contenue dans les registres
                speed1 = inBuffer[0]; speed2Turn = inBuffer[1]; battery = inBuffer[10]; current1 = inBuffer[11]; current2 = inBuffer[12];
                softrev = inBuffer[13]; acceleration = inBuffer[14]; mode = inBuffer[15]; command = inBuffer[16];
                // Calcul de la valeur contenue dans les codeurs 32 bits signée
                encoder1 = (Int32)(inBuffer[2] << 24) | (Int32)(inBuffer[3] << 16) | (Int32)(inBuffer[4] << 8) | (Int32)(inBuffer[5]);
                encoder2 = (Int32)(inBuffer[6] << 24) | (Int32)(inBuffer[7] << 16) | (Int32)(inBuffer[8] << 8) | (Int32)(inBuffer[9]);
            }
            else
            {
                // Failed
                //Debug.Print("Failed to execute transaction at device: " + busI2C.Config.Address + ".");
            }
            busI2C.Dispose(); // Déconnexion virtuelle de l'objet Lcd du bus I2C
            return 1;
        }

        public int SetMode(ModeRegister valmode)
        {
            SetRegister(WRegister.Mode, (byte)valmode);
            return 1;
        }      

        public int RazEncoders()
        {
            SetRegister(WRegister.Command, 32);
            return 1;
        }

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

        public int SetSpeedTurn(byte ValSpeed1, byte ValSpeed2Turn)
        {
            SetRegister(WRegister.Speed1, ValSpeed1);
            SetRegister(WRegister.Speed2Turn, ValSpeed2Turn);
            return 1;
        }
        
    }
 }
