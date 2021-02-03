using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Levrum.Utils;
using Levrum.Utils.Messaging;

namespace Levrum.UI.WPF
{
    public class WPFApp : Application
    {
        public List<string> StartupFileNames { get; set; } = new List<string>();
        public List<string> StartupArguments { get; set; } = new List<string>();
        public string[] FileTypes { get; protected set; } = new string[0];

        public bool DebugMode { get; set; }

        public string AppId { get; protected set; } = "Levrum";
        public bool ShutdownOnFatalError { get; protected set; } = true;
        public bool UseDefaultMessageBox { get; protected set; } = true;
        public bool SingleInstanceApplication { get; protected set; } = false;
        public bool OpenDocsInFirstInstance { get; protected set; } = true;

        private bool m_saveWindowPosition = false;
        public bool SaveWindowPosition 
        { 
            get 
            { 
                return m_saveWindowPosition; 
            } 
            set 
            { 
                m_saveWindowPosition = value; 
            } 
        }

        public IPCNamedPipeServer MessageServer { get; set; } = null;
        public event IPCMessageDelegate OnMessageReceived;

        private static Mutex s_mutex;

        private bool m_hasMutex = false;
        public bool HasMutex
        {
            get
            {
                return m_hasMutex;
            }
            set
            {
                m_hasMutex = value;
                if (value == true)
                {
                    if (MessageServer == null)
                    {
                        MessageServer = new IPCNamedPipeServer(AppId);
                        MessageServer.OnMessageReceived += messageServer_OnMessageReceived;
                    }
                }
                else
                {
                    MessageServer?.Dispose();
                    MessageServer = null;
                }
            }
        }

        private Task GainMutexTask { get; set; }
        private bool AbortMutex { get; set; } = false;

        protected void InitializeApp()
        {
            string[] args = Environment.GetCommandLineArgs();
            List<string> fileTypes = FileTypes.ToList();
            foreach (string arg in args)
            {
                if (arg == "-d" || arg == "--debug")
                {
                    DebugMode = true;
                    StartupArguments.Add(arg);
                } else if (arg.StartsWith("-") || arg.StartsWith("--"))
                {
                    StartupArguments.Add(arg);
                }
                else
                {
                    FileInfo file = new FileInfo(arg);
                    if (file.Exists && fileTypes.Contains(file.Extension))
                    {
                        StartupFileNames.Add(arg);
                    }
                }
            }

#if DEBUG
            DebugMode = true;
#endif

            bool newMutex;
            Mutex mutex = new Mutex(true, AppId, out newMutex);

            if (!newMutex)
            {
                if (SingleInstanceApplication == true)
                {
                    using (IPCNamedPipeClient client = new IPCNamedPipeClient(AppId))
                    {
                        IPCMessage message = new IPCMessage();
                        message.Type = IPCMessageType.BringToFront;
                        client.SendMessage(message);
                    }
                    Current.Shutdown();
                    return;
                } else if (OpenDocsInFirstInstance && StartupFileNames.Count > 0)
                {
                    using (IPCNamedPipeClient client = new IPCNamedPipeClient(AppId))
                    {
                        IPCMessage message = new IPCMessage();
                        message.Type = IPCMessageType.OpenDocument;
                        message.Data = StartupFileNames;
                        client.SendMessage(message);
                    }
                    Current.Shutdown();
                }

                GainMutexTask = new Task(() => {
                    try
                    {
                        bool isNewMutex = false;
                        while (isNewMutex == false && !AbortMutex)
                        {
                            Thread.Sleep(100);
                            mutex = new Mutex(true, AppId, out isNewMutex);
                            if (isNewMutex)
                            {
                                s_mutex = mutex;
                            }
                            else
                            {
                                mutex.Close();
                                mutex.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, ex.Message, false);
                    }
                    HasMutex = true;
                });

                GainMutexTask.Start();
            }
            else
            {
                s_mutex = mutex;
                HasMutex = true;
            }

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            if (UseDefaultMessageBox)
            {
                LogHelper.OnMessageBox += LogHelper_OnMessageBox;
            }

            if (ShutdownOnFatalError)
            {
                LogHelper.OnFatalError += LogHelper_OnFatalError;
            }
            LogHelper.LogMessage(LogLevel.Info, string.Format("Started Application {0}", Assembly.GetExecutingAssembly().FullName));
            Exit += wpfApp_Exit;
        }

        private void messageServer_OnMessageReceived(IPCMessage message)
        {
            OnMessageReceived(message);
        }

        private void LogHelper_OnFatalError()
        {
            Current.Shutdown();
        }

        private void LogHelper_OnMessageBox(string message, string title)
        {
            MessageBox.Show(message, title);
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogHelper.LogMessage(LogLevel.Fatal, string.Format("Unhandled Exception: {0} {1}", e.Exception.Message, e.Exception.StackTrace), e.Exception);
            e.Handled = true;
        }

        private void wpfApp_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                Task mutexCleanup = new Task(() =>
                {
                    if (HasMutex)
                    {
                        s_mutex?.Close();
                        s_mutex?.Dispose();
                        s_mutex = null;
                    }
                    else if (!GainMutexTask.IsCompleted)
                    {
                        AbortMutex = true;
                        GainMutexTask.Wait();
                    }
                });
                mutexCleanup.Start();
                mutexCleanup.Wait();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, ex.Message);
            }
            finally
            {
                LogHelper.LogMessage(LogLevel.Info, string.Format("Shutdown Application {0}", Assembly.GetExecutingAssembly().FullName));
            }
        }
    }
}
