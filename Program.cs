using System;
using System.Threading;
using nanoFramework.Hardware.Esp32;

namespace nfSDStorage
{
    public class Program
    {

        // Set SD public to use in other librarys 
        public static NfStorage SD;

        public static void Main()
        {
            Console.WriteLine("Hello Esp32 storage! \n" );
            
            int MISOPin = Configuration.GetFunctionPin(DeviceFunction.SPI1_MISO);
            Console.WriteLine("SPI1 MISO Pin " + MISOPin);

            int MOSIPin = Configuration.GetFunctionPin(DeviceFunction.SPI1_MOSI);
            Console.WriteLine("SPI1 MOSI Pin " + MOSIPin);

            int CLKPin = Configuration.GetFunctionPin(DeviceFunction.SPI1_CLOCK);
            Console.WriteLine("SPI1 Clock Pin " + CLKPin);
                  
           
            // Set constructor to SDCard defaults
            // Internal storage example: SD = new NFStorage(false);
            SD = new NfStorage();


            // Internal storage does not support directories
            // Get the files in the starting directory
            // Example: SD.GetFiles("folder1")
            string[] Files = SD.GetFiles();

            foreach (var File in Files)
            {
                Console.WriteLine("File -> " + File);

            }


            // Internal storage does not support directories
            // Get the directoriess in the starting directory
            // Example: SD.GetDirectories("folder1")
            //  string[] Directories = SD.GetDirectories();

            // foreach (var dir in Directories)
            //  {
            //      Console.WriteLine("Directory -> " + dir);
            //  }


            // Write text in starting directory
            // Write over the current file if it exists (default is append the current file)
            // Example: SD.WriteText("FOLDER1\\FOLDER2\\TEST2.TXT", "Hello World! \n");
            SD.WriteText("Test1.txt", "Hello internal storage!", false);

            Boolean Exists = SD.FileExists("Test1.txt");
            if (Exists)
                Console.WriteLine("Test1.txt exists");

            // Delete file
            // SD.DeleteFile("test1.txt");

            // Rename file
            // SD.RenameFile("test1.txt", "test2.txt");

            // Read a text file from starting directory
            // Example: string textreturned = SD.ReadText("FOLDER1\\FOLDER2\\TEST2.TXT");

            //Write binary file 
            byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 100 };

            SD.WriteBuffer("BITES.BIN", bytes, false);

            //Read binary file
            byte[] rb = SD.ReadBuffer("BITES.BIN");

            Console.WriteLine("Bytes read from BITES.BIN");

            foreach (byte b in rb)
                Console.Write($"{b:X},");
            Console.WriteLine($"");


            SD.SDCardUnmount();

            Thread.Sleep(Timeout.Infinite);

        }
    }
}


           
