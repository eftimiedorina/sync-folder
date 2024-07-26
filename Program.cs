using System.Security.Cryptography;
namespace sync_folder
{    
    public class Program
    {
        public static StreamWriter log;

        public static void Main(string[] args)
        {
            string sourceFolder = null;
            string destinationFolder = null;
            int interval = 600;
            string logFile = "sync_log.txt";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i + 1 < args.Length)
                {
                    sourceFolder = args[++i];
                }
                else if (args[i] == "--destination" && i + 1 < args.Length)
                {
                    destinationFolder = args[++i];
                }
                else if (args[i] == "--interval" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int parsedInterval))
                    {
                        interval = parsedInterval;
                    }
                }
                else if (args[i] == "--file" && i + 1 < args.Length)
                {
                    logFile = args[++i];
                }
            }

            if (sourceFolder == null || destinationFolder == null)
            {
                Console.WriteLine("Usage: <program> --source <source_folder> --destination <destination_folder> [--interval <interval_in_seconds>] [--file <log_file>]");
                return;
            }

            log = new StreamWriter(logFile, true);

            while (true)
            {
                try
                {
                    CopyFolder(sourceFolder, destinationFolder);
                }
                catch (Exception e)
                {
                    LogMessage($"An error occurred: {e.Message}");
                    break;
                }
                Thread.Sleep(interval * 1000);
            }

            log.Close();
        }

        public static void LogMessage(string message)
        {
            string currentTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            log.WriteLine($"[{currentTime}] {message}");
            log.Flush();
            Console.WriteLine(message);
        }

        public static string ComputeFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void CopyFile(string sourcePath, string destinationFolder)
        {
            try
            {
                bool notDup = true;
                string sourceHash = ComputeFileHash(sourcePath);

                foreach (var file in Directory.GetFiles(destinationFolder, "*", SearchOption.AllDirectories))
                {
                    string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(file));
                    if (sourceHash == ComputeFileHash(destinationPath) && !File.Exists(Path.Combine(destinationFolder, Path.GetFileName(sourcePath))))
                    {
                        LogMessage($"File '{sourcePath.Substring(sourcePath.IndexOf(Path.DirectorySeparatorChar) + 1)}' is a duplicate of '{destinationPath.Substring(destinationPath.IndexOf(Path.DirectorySeparatorChar) + 1)}'");
                        notDup = false;
                    }
                }

                string destinationPathNew = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destinationPathNew, true);

                if (notDup)
                {
                    LogMessage($"{sourcePath.Substring(sourcePath.IndexOf(Path.DirectorySeparatorChar) + 1)} added/updated");
                }
                else
                {
                    LogMessage($"{sourcePath.Substring(sourcePath.IndexOf(Path.DirectorySeparatorChar) + 1)} added as a copy of {destinationPathNew.Substring(destinationPathNew.IndexOf(Path.DirectorySeparatorChar) + 1)}");
                }
            }
            catch (Exception e)
            {
                LogMessage($"Error copying file: {e.Message}");
            }
        }

        public static void CopyFolder(string source, string destination)
        {
            try
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }

                var sourceFiles = new HashSet<string>(Directory.GetFiles(source));
                var destinationFiles = new HashSet<string>(Directory.GetFiles(destination));

                var toDel = new List<string>();

                foreach (var f in destinationFiles)
                {
                    if (!sourceFiles.Contains(f))
                    {
                        toDel.Add(f);
                    }
                }

                foreach (var fileToDelete in toDel)
                {
                    File.Delete(fileToDelete);
                    LogMessage($"{fileToDelete.Substring(fileToDelete.IndexOf(Path.DirectorySeparatorChar) + 1)} has been deleted");
                }

                foreach (var item in Directory.GetFileSystemEntries(source))
                {
                    string sourceItem = Path.Combine(source, Path.GetFileName(item));
                    string destinationItem = Path.Combine(destination, Path.GetFileName(item));

                    if (Directory.Exists(sourceItem))
                    {
                        CopyFolder(sourceItem, destinationItem);
                    }
                    else
                    {
                        CopyFile(sourceItem, destination);
                    }
                }

                LogMessage($"Folder synced: {source} to {destination}");
            }
            catch (Exception e)
            {
                LogMessage($"An error occurred: {e.Message}");
            }
        }

    }
}