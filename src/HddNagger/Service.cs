namespace HddNagger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.ServiceProcess;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Win32;

    /// <summary>
    /// Service which always nags the given drive so the HDD doesn't fall asleep.
    /// (It may happen if you change your DVD drive in your laptop to a secodary HDD
    /// and your laptop - as mine - always shuts it down and the access time
    /// becomes long.)
    /// </summary>
    public partial class Service : ServiceBase
    {
        private readonly List<Task> tasks = new List<Task>();
        private TaskState state = TaskState.Working;
        private int secondsBetweenWrites = 10;

        public Service()
        {
            this.InitializeComponent();

            var registryKey = Registry.CurrentUser.OpenSubKey("Software\\hddnagger", true) ?? Registry.CurrentUser.CreateSubKey("Software\\hddnagger");

            if (registryKey == null)
            {
                throw new InvalidOperationException("Couldn't load or create registry node for the HddNagger service. Exiting.");
            }

            var driveParameters = registryKey.GetValue("parameters", string.Empty);

            if (string.IsNullOrWhiteSpace((string)driveParameters))
            {
                registryKey.SetValue("parameters", "D:\\");
            }
            registryKey.Close();

            if (!EventLog.SourceExists(this.eventLog1.Source))
            {
                EventLog.CreateEventSource(this.eventLog1.Source, this.eventLog1.Log);
            }

            this.Log("HDD Nagger service initialized...", false);
        }

        // ReSharper disable once UnusedParameter.Local
        private void Log(string message, bool debug = true)
        {
#if DEBUG
            Debug.WriteLine(message);
            this.eventLog1.WriteEntry(message);
#else
            if (debug)
            {
                return;
            }

            Debug.WriteLine(message);
            this.eventLog1.WriteEntry(message);
#endif
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif

            var registryKey = Registry.CurrentUser.OpenSubKey("Software\\hddnagger");
            if (registryKey == null)
            {
                throw new InvalidOperationException("Couldn't load or create registry node for the HddNagger service. Exiting.");
            }

            var parameters = ((string)registryKey.GetValue("parameters", string.Empty)).Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            registryKey.Close();

            if (parameters.Length < 1)
            {
                this.Log("No root paths were given. Exiting...", false);
                this.Stop();
                return;
            }

            this.state = TaskState.Working;
            this.secondsBetweenWrites = 10;

            for (var i = 0; i < parameters.Length; i++)
            {
                var id = i;
                var rootPath = parameters[i];

                this.tasks.Add(Task.Factory.StartNew(() => DoWork(id, this.secondsBetweenWrites, rootPath, ref this.state)));
                this.Log($"Launched task number {id} with root path {rootPath}.", false);
                Thread.Sleep(1000);
            }
        }

        protected override void OnStop()
        {
            this.Log("Service OnStop...", false);

            this.state = TaskState.Stopping;
            Task.WaitAll(this.tasks.ToArray());

            this.Log("All tasks stopped.", false);
        }

        protected override void OnContinue()
        {
            this.state = TaskState.Working;
            this.Log("Service OnContinue...", false);
        }

        protected override void OnPause()
        {
            this.state = TaskState.Paused;
            this.Log("Service OnPause...");
        }

        protected override void OnShutdown()
        {
            this.Log("Service OnShutdown...", false);

            this.state = TaskState.Stopping;
            Task.WaitAll(this.tasks.ToArray());

            this.Log("All tasks stopped.", false);
        }

        static void DoWork(int id, int secondsBetweenWrites, string rootPath, ref TaskState state)
        {
            string path = String.Empty;
            var second = -1;

            do
            {
                switch (state)
                {
                    case TaskState.Paused:
                        Debug.WriteLine($"[{id}][Status]HDD Nagger service paused.");
                        second = -1;
                        break;

                    case TaskState.Working:
                        second = (second + 1) % secondsBetweenWrites;
                        if (second != 0)
                        {
                            Debug.WriteLine($"[{id}][Status]Working and waiting {second}.");
                        }
                        else
                        {
                            var filename = Guid.NewGuid().ToString();
                            path = Path.Combine(new[] { rootPath, filename });
                            if (!File.Exists(path))
                            {
                                try
                                {
                                    var message = $"[{id}][Status]Writing random content to the file: '{path}'.";
                                    Debug.WriteLine(message);
                                    File.WriteAllText(path, $@"{Guid.NewGuid().ToString()}");
                                }
                                catch (Exception ex)
                                {
                                    var message = $"[{id}][Error]Couldn't write file '{path}'.\r\nError message:{ex.Message}\r\nStack:\r\n{ex.StackTrace}";
                                    Debug.WriteLine(message);
                                }

                                Thread.Sleep(100);

                                if (File.Exists(path))
                                {
                                    try
                                    {
                                        File.Delete(path);
                                    }
                                    catch (Exception ex)
                                    {
                                        var message = $"[{id}][Error]Couldn't delete file '{path}'.\r\nError message:{ex.Message}\r\nStack:\r\n{ex.StackTrace}";
                                        Debug.WriteLine(message);
                                    }
                                }
                            }
                        }
                        break;
                }
                Thread.Sleep(1000);
            } while (state != TaskState.Stopping);

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    var message = $"[{id}][Error]Couldn't delete file '{path}'.\r\nError message:{ex.Message}\r\nStack:\r\n{ex.StackTrace}";
                    Debug.WriteLine(message);
                }
            }

            Debug.WriteLine($"[{id}][Status]Finalized");
        }

        static void Main()
        {
            var servicesToRun = new ServiceBase[] { new Service() };
            Run(servicesToRun);
        }
    }
}
