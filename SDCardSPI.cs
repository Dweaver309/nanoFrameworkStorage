using System;
using nanoFramework.Hardware.Esp32;
using Windows.Storage;
using Windows.Storage.Devices;
using Windows.Storage.Streams;


// Thanks to Adrian Soundly and Jose Somoes
// and everyone helped make "Storage" possible for nanoFramework

// Storage is still under construction
// Check the samples here https://github.com/nanoframework/Samples/tree/master/samples/Storage
// for updates and changes

// Other Sdcard options not tested
//
// Mount a MMC sdcard using 4 bit data ( e.g Wrover boards )
// SDCard.MountMMC(false);
//
// Mount a MMC sdcard using 1 bit data ( e.g Olimex EVB boards )
// SDCard.MountMMC(true);

// You may need a separate 3.3v power source for the SD card reader 


namespace nfSDStorage
{

    /// <summary>
    /// Class library for SD card or internal storage (SPIFFS)
   /// </summary>
    public class NfStorage
    {
       
        public StorageFolder SDevice;
        private StorageFolder Sfolder;
        private string Fname;
        private readonly Boolean IsInternalStorage = false;


        /// <summary>
        /// Constructor for mounting SD card or internal storage (SPIFFS)
        /// Serial Peripheral Interface Flash File System (SPIFFS) 
        /// SPIFFS is a lightweight filesystem connected by SPI bus
        /// Config data for Network, Wireless, certificates, user data  256K
        /// Config, data, spiffs,  0x2D0000, 0x40000
        /// Example NfStorage SD = new SDCardSPI(false)
        /// Example: NfStorage SD = new SDCardSPI(true, 23, 25, 19, 26)
        /// One or more SPI pins can be changed
        /// </summary>
        public NfStorage(Boolean DeviceIsSDCard = true, int MOSIPin = 23, int MISOPin = 25, int CLOCKPin = 19, int CSPin = 26)
        {

            if (DeviceIsSDCard == false)
            {
               
                StorageFolder internalDevices = Windows.Storage.KnownFolders.InternalDevices;

                var internalDrives = internalDevices.GetFolders();

                //Set the Storage variable
                SDevice = internalDrives[0];

                IsInternalStorage = true;

            }

            else
            {
                // Change pins if not default for ESP32

                // Master out slave in
                // Connect to DI (digital in)
                if (MOSIPin != 23)
                {
                    Configuration.SetPinFunction(MOSIPin, DeviceFunction.SPI1_MOSI);

                }


                // Master in slave out
                // Connect to DO (digital out)
                if (MISOPin != 25)
                {
                    Configuration.SetPinFunction(MISOPin, DeviceFunction.SPI1_MISI);

                }

                if (CLOCKPin != 19)
                {
                    Configuration.SetPinFunction(CLOCKPin, DeviceFunction.SPI1_CLOCK);

                }

                try
                {

                    // Mount a SPI connected SDCard passing the SPI bus and the Chip select pin
                    SDCard.MountSpi("SPI1", CSPin);

                    if (SDCard.IsMounted)
                    {
                        Console.WriteLine("Success SDCard is mounted");

                        StorageFolder externalDevices = Windows.Storage.KnownFolders.RemovableDevices;

                        var removableDevices = externalDevices.GetFolders();

                        //Set the Storage variable
                        SDevice = removableDevices[0];

                    }

                }

                catch (Exception ex)
                {
                    Console.WriteLine("Failed to mount SDCard \n" + ex.Message);
                }

            }
        }

        /// <summary>
        /// Get the files in the starting directory
        /// </summary>
        /// <returns>String array of files</returns>
        public String[] GetFiles(string StartingDirectory = "Root")
        {

            if (StartingDirectory == "Root")
            {
                Sfolder = SDevice;

            }
            else
            {
                StartingDirectory = StartingDirectory.ToUpper();

                SetDirectory(StartingDirectory);

            }

            var FilesInDevice = Sfolder.GetFiles();

            int fl = FilesInDevice.Length;

            string[] rs = new string[fl];

            int i = 0;

            foreach (StorageFile file in FilesInDevice)
            {
                rs[i] = file.Name;

                i += 1;
                
                Console.WriteLine("Files -> " + file.Path);

            }

            return rs;

        }

        /// <summary>
        /// Read text file
        /// </summary>
        /// <returns>Text from file</returns>
        public string ReadText(string FilePath)
        {

            // Not capitalized in internal storage
            if (IsInternalStorage == false)
                FilePath = FilePath.ToUpper();
          
            try
            {
               
             //  Rem directory and filename set in FileExists
             if (FileExists(FilePath))
             {

              var File = Sfolder.CreateFile(Fname, CreationCollisionOption.OpenIfExists);
                                   
              return FileIO.ReadText(File);

             }

              return string.Empty;    

            }

            catch (Exception ex)
            {
                return "Error: Reading file " + ex.Message ;
            }

        }

        /// <summary>
        /// Read binary file
        /// </summary>
        /// <returns>Byte array from file</returns>
        public byte[] ReadBuffer(string FilePath)
        {
            byte[] ErrorByte = { 0x0 };

            // Not capitalized in internal storage
            if (IsInternalStorage == false)
                FilePath = FilePath.ToUpper();


            try
            {

                // Directory and file name set in FileExists
                if (FileExists(FilePath))
                {
                 
                     var File = Sfolder.CreateFile(Fname, CreationCollisionOption.OpenIfExists);

                    IBuffer readBuffer = FileIO.ReadBuffer(File);
                    
                    using (DataReader dataReader = DataReader.FromBuffer(readBuffer))
                    {
                        byte[] cBuf = new byte[readBuffer.Length];
                      
                       dataReader.ReadBytes(cBuf);
                       
                        Console.WriteLine("Buffer length" + cBuf.Length);
                       
                        return cBuf;

                    }
                    
                }
                
                return ErrorByte;

            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                return ErrorByte;
              
            }

        }

        /// <summary>
        /// Write to binary file
        /// Default is to append the existing file if the file exists
        /// </summary>
        public void WriteBuffer(string FilePath, byte[] Buffer, Boolean Append = true)
        {
           
            try
            {
                
                // Not capitalized in internal storage
                if (IsInternalStorage == false)
                    FilePath = FilePath.ToUpper();

                SetDirectoryandFilename(FilePath);
               
                    if (Append)
                    {
    
                        byte[] rBuffer = ReadBuffer(FilePath);

                        byte[] writeBuffer = new byte[rBuffer.Length + Buffer.Length];

                        Array.Copy(rBuffer, 0, writeBuffer, 0, rBuffer.Length - 1);

                        Array.Copy(Buffer, 0, writeBuffer, rBuffer.Length, Buffer.Length - 1);

                        var File = Sfolder.CreateFile(Fname, CreationCollisionOption.ReplaceExisting);

                        FileIO.WriteBytes(File, writeBuffer);

                        Console.WriteLine("Wrote " + writeBuffer.Length + " bytes to " + FilePath + " for append");

                    }

                    else
                    { 
                        var File = Sfolder.CreateFile(Fname, CreationCollisionOption.ReplaceExisting);

                        FileIO.WriteBytes(File, Buffer);

                        Console.WriteLine("Wrote " + Buffer.Length + " bytes to " + FilePath);

                    }
             
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing binary file: " + ex.Message);

            }
        }

        /// <summary>
        /// Get directories from a starting directory
        /// Example: If StartingDirectory =  "folder1\\folder2"
        /// Directories in folder2 are found
        /// </summary>
        /// <returns>String array of directories</returns>
        public String[] GetDirectories(string StartingDirectory = "Root")
        {

            try
            {
           
            if (IsInternalStorage)
            {
                Console.WriteLine("\n Directories are currently not supported for internal storage.\n");
                
            }
           
            if (StartingDirectory == "Root")
            {
                Sfolder = SDevice;

            }
            else
            {
                StartingDirectory = StartingDirectory.ToUpper();

                SetDirectory(StartingDirectory);

            }

            var foldersInDevice = Sfolder.GetFolders();

            int fl = foldersInDevice.Length;

            string[] rs = new string[fl];

            int i = 0;

            foreach (StorageFolder folder in foldersInDevice)
            {
                rs[i] = folder.Path;

                i += 1;

                Console.WriteLine($"Folder ->{folder.Name}");

            }

            return rs;

            }

            catch (Exception)
            {
                Console.WriteLine("\n Directories are currently not supported for internal storage.\n");

                string[] emptyStringArray = new string[0];

                return emptyStringArray;

            }

        }

        /// <summary>
        /// Sets the private varable Sfolder to the current directory 
        /// Sets the private varable Fname to the curent file name
        /// </summary>
        /// <param name="FilePath"></param>
        private void SetDirectoryandFilename(string FilePath)
        {
            try
            {
          
            char slash = '\\';

            Sfolder = SDevice;

            if (FilePath.IndexOf(slash) == -1)
            {

                Console.WriteLine("File path -> " + FilePath);
                Fname = FilePath;

            }

            else
            {
                string[] str = FilePath.Split(slash);
               
                for (int i = 0; i < str.Length - 1; i++)
                {

                   Sfolder = Sfolder.CreateFolder(str[i], CreationCollisionOption.ReplaceExisting);

                   Console.WriteLine("Successfully created folder: " + Sfolder.Path);

                }

                Fname = str[str.Length - 1];

                Console.WriteLine("File name -> " + Fname);

              }

            }
            catch (Exception)
            {
                Console.WriteLine("Directories are not supported for internal storage");
                
            }

        }

        /// <summary>
        /// Sets the private varable Sfolder to the current directory 
        /// Example: If StartingDirectory = folder1\\folder2
        /// Directories under folder2 are found
        /// </summary>
        private void SetDirectory(string StartingDirectory)
        {
            try
            {

                char slash = '\\';

                Sfolder = SDevice;

                if (StartingDirectory.IndexOf(slash) == -1)
                {
                 
                    if(StartingDirectory != "Root")
                    {
                        Sfolder = Sfolder.CreateFolder(StartingDirectory, CreationCollisionOption.ReplaceExisting);

                        Console.WriteLine("Successfully created folder: " + Sfolder.Path);
                    }

                }

                else
                {
                    string[] str = StartingDirectory.Split(slash);

                    for (int i = 0; i < str.Length ; i++)
                    {

                        Sfolder = Sfolder.CreateFolder(str[i], CreationCollisionOption.ReplaceExisting);

                        Console.WriteLine("Successfully created folder: " + Sfolder.Path);

                    }

                }

            }
            catch (Exception)
            {
                Console.WriteLine("Directories are not supported for internal storage");

            }

        }

        /// <summary>
        /// Write text to file
        /// Default is to append if file exists
        /// </summary>
        public void WriteText(string FilePath, string Text, Boolean Append = true)
        {

            try
            {
                //Not capitalized in internal storage
                if (IsInternalStorage == false)
                    FilePath = FilePath.ToUpper();


                SetDirectoryandFilename(FilePath);

                    if (Append)
                    {

                        string st = ReadText(FilePath);

                        var File = Sfolder.CreateFile(Fname, CreationCollisionOption.ReplaceExisting);

                        st = st + Text;

                        FileIO.WriteText(File, st);

                        Console.WriteLine("Wrote " + st.Length + " bytes to " + FilePath + " for append");

                    }

                    else
                    {
                        var File = Sfolder.CreateFile(Fname, CreationCollisionOption.ReplaceExisting);

                        FileIO.WriteText(File, Text);

                        Console.WriteLine("Wrote " + Text.Length + " bytes to " + FilePath);
                    }
             
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error writing text: " + ex.Message);

            }
        }

        /// <summary>
        /// Delete file if exists
        /// </summary>
        public void DeleteFile(string FilePath)
        {
            //Not capitalized in internal storage
            if (IsInternalStorage == false)
                FilePath = FilePath.ToUpper();

            SetDirectoryandFilename(FilePath);

            var Files = Sfolder.GetFiles();

            foreach (var File in Files)
            {

                if (Fname == File.Name)
                {
                    File.Delete();

                    Console.WriteLine(File.Name + " was deleted");
                }

            }
        } 

        /// <summary>
        /// Delete directory from the starting directory
        /// </summary>
        public void DeleteDirectory( string DirectoryToDelete , string StartingDirectory = "Root")
        {

            try
            {

                DirectoryToDelete = DirectoryToDelete.ToUpper();

                if (StartingDirectory == "Root")
                {
                    Sfolder = SDevice;
                }
                else
                {
                    StartingDirectory = StartingDirectory.ToUpper();

                    SetDirectory(StartingDirectory);

                }

            Boolean found = false;

            var Folders = Sfolder.GetFolders();

            foreach (var Folder in Folders)
            {
                Console.WriteLine("Folder Name -> " + Folder.Name);

                if (DirectoryToDelete == Folder.Name)
                {


                    Folder.Delete();

                    Console.WriteLine(Folder.Path + " deleted");

                    found = true;

                }

            }

            if (found == false)
                Console.WriteLine(DirectoryToDelete + " not found");

            }
            catch (Exception ex )
            {

                Console.WriteLine("Error deleting directory the directory must be empty: " + ex.Message);

            }


        }

        /// <summary>
        /// Rename file if FilePath exists and NewFilename does not exist
        /// </summary>
        public void RenameFile(string FilePath, string NewFilename)
        {
            //Not capitalized in internal storage
            if (IsInternalStorage == false)
                FilePath = FilePath.ToUpper();

            SetDirectoryandFilename(FilePath);

            Boolean found = false;

            var Files = Sfolder.GetFiles();

            Console.WriteLine("Fname " + Fname);

            foreach (var File in Files)
            {

                Console.WriteLine("Filename " + File.Name);

                if (Fname == File.Name)
                {
                    Console.WriteLine("Found " + File.Name);
                    if (FileExists(NewFilename))
                    {
                        Console.WriteLine(FilePath + " can't be renamed " + NewFilename + " exists");

                    }

                    else
                    {
                        File.Rename(NewFilename);
                     
                        Console.WriteLine(File.Name + " renamed " + NewFilename);

                        found = true;

                    }

                }

            }

            if (found == false)
                Console.WriteLine(FilePath + " not found");

        }

        /// <summary>
        /// Returns true if FilePath exists
        /// </summary>
        public Boolean FileExists(string FilePath)
        {
            // Internal storage doesn't capitalize files
            if (IsInternalStorage == false)
                FilePath = FilePath.ToUpper();

            SetDirectoryandFilename(FilePath);

            try
            {
                var Files = Sfolder.GetFiles();

                foreach (var File in Files)
                {
                 
                    if (Fname == File.Name)
                    {
                        Console.WriteLine(Fname + " exists");

                        return true;

                    }

                }

                Console.WriteLine(Fname + " not found");

                return false;

            }


            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                return false;
            }

        }

        /// <summary>
        /// If SDcard is mounted Unmount the SDCard
        /// </summary>
        public void  SDCardUnmount()
        {
            //  Currently the mount card class only allows for 1 device to be mounted
            if (SDCard.IsMounted)
            {
                SDCard.Unmount();
                Console.WriteLine("SDCard successfully unmounted");
            }
        }
    
    }

}
