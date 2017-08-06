namespace FilesystemWatcher
{

    public class FileSystemWatcha
    {
        public int pid = -1;
        public System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
        private bool errorOccured = false;
        private System.Collections.Generic.List<System.String> _changedFiles = new System.Collections.Generic.List<System.String>();

        public FileSystemWatcha( )
        {
  
        }

        public void init(string watchfolder , string watchfilter)
        {
            this.ProcessDirectory(watchfolder, watchfilter);
            /* Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories. */
            this.watcher.NotifyFilter = System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.DirectoryName;
            this.watcher.Filter = watchfilter;
            this.watcher.Renamed += new System.IO.RenamedEventHandler(this.RenamedEventHandler);
            this.watcher.Created += new System.IO.FileSystemEventHandler(this.CreatedEventHandler);
            this.watcher.Path = watchfolder;
            this.watcher.Error += new System.IO.ErrorEventHandler(this.OnError);
            // Begin watching.
            this.watcher.EnableRaisingEvents = true;

        }


        private byte[] ReadAllBytes(System.IO.FileStream fs)
        {
            byte[] buffer = null;
            buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            return buffer;
        }

        private System.IO.FileStream WaitForFile(string fullPath, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share)
        {
            for (int numTries = 0; numTries < 60; numTries++)
            {
                try
                {
                    System.IO.FileStream fs = new System.IO.FileStream(fullPath, mode, access, share);
                    fs.ReadByte();
                    fs.Seek(0, System.IO.SeekOrigin.Begin);
                    return fs;
                }
                catch (System.IO.IOException e)
                {
                    //sit.shared.Logs.log.Debug(e.Message);
                    //sit.shared.Logs.log.Debug("Retry " + numTries.ToString() + "/" + 60.ToString());
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return null;
        }

 
  

        private void ProcessFile(string filename)
        {
            System.Console.WriteLine("processing filename");
        }

        private void ProcessDirectory(string watchfolder, string watchfilter)
        {
            this.errorOccured = false;
            System.Console.WriteLine("Processing directory " + System.IO.Path.Combine(watchfolder, watchfilter) + " at service startup to import old unprocessed files.");
            string[] fileEntries = System.IO.Directory.GetFiles(watchfolder, watchfilter);
            foreach (string fileName in fileEntries)
            {
                if (System.IO.Path.GetExtension(fileName).ToLower() == System.IO.Path.GetExtension(watchfilter).ToLower()) // work around a bug in getfiles
                {
                    this.ProcessFile(fileName);
                }
            }
      
            System.Console.WriteLine("Processing directory done");
        }
 


        private void RenamedEventHandler(object source, System.IO.FileSystemEventArgs e)
        {
            lock (_changedFiles)
            {
                if (_changedFiles.Contains(e.FullPath))
                {
                    return;
                }
                else
                {
                    _changedFiles.Add(e.FullPath);
                }
            }

            System.Console.WriteLine("Detected new file to import: \"" + e.FullPath + "\"");
            System.Console.WriteLine("waiting 2 seconds to give third party process a chance to cool down ...");
            System.Threading.Thread.Sleep(2000);
            try
            {
                this.ProcessFile(e.FullPath);
                System.Console.WriteLine(e.FullPath + " successfully processed.");
            }
            catch (System.IO.IOException ioException)
            {
                // ignore because a dot net bug in renamed eventhadler which triggers twice
                System.Console.WriteLine(ioException.Message);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            finally
            {

                lock (_changedFiles)
                {
                    _changedFiles.Remove(e.FullPath);
                }

            }
        }

        private void CreatedEventHandler(object source, System.IO.FileSystemEventArgs e)
        {

            System.Console.WriteLine("created file " + e.FullPath);
        }

        private void NotAccessibleError(string watchfolder)
        {
            this.watcher.EnableRaisingEvents = false;
            int iTimeOut = 1000 * 10; /* retry in 10 seconds */
            int iMaxAttempts = 6 /* 1 minute */ * 20 /* 20 minuten */;
            int i = 0;
            while (!this.watcher.EnableRaisingEvents && i < iMaxAttempts)
            {
                i += 1;
                try
                {
                    System.Console.WriteLine("Retrying to watch " + watchfolder + "...");
                    this.watcher.EnableRaisingEvents = true;
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(ex);
                    this.watcher.EnableRaisingEvents = false;
                    System.Threading.Thread.Sleep(iTimeOut);
                }
            }

            if (this.watcher.EnableRaisingEvents)
            {
                System.Console.WriteLine("Watchting " + watchfolder);
                System.Console.WriteLine("Maybe there are files not processed.");
            }
            else
            {
                System.Console.WriteLine("Could not re-bind filewatching to " + watchfolder);
                System.Console.WriteLine("Service need to be restarted!");
            }
        }

        private void OnError(object source, System.IO.ErrorEventArgs e)
        {
            System.Console.WriteLine(e.GetException());
        }
    }

    class Program
    {

        private static System.IO.FileSystemWatcher watcher;

        private static void watch(string path)
        {
            watcher = new System.IO.FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.DirectoryName;
            watcher.Filter = "*.net";
            watcher.Changed += new System.IO.FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private static void OnChanged(object source, System.IO.FileSystemEventArgs e)
        {
            System.Console.WriteLine(e.Name);
        }

        static void Main(string[] args)
        {
            string folder = "c:\\temp\\";
            // string folder = "\\\\192.168.20.80\\d\\test\\";
            
            string filter = "*.net";
            FileSystemWatcha watcher = new FileSystemWatcha();
            watcher.init(folder, filter);
            
            System.Console.ReadKey();
        }
    }
}
