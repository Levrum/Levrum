using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Levrum.Utils;
using Levrum.Utils.Messaging;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] StartupFileNames { get; set; } = new string[0];
        public bool DebugMode { get; set; }
        public static string AppIdentifier { get; } = "Levrum_DataBridge";

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
                        MessageServer = new IPCNamedPipeServer(AppIdentifier);
                        MessageServer.OnMessageReceived += messageServer_OnMessageReceived;
                    }
                } else
                {
                    MessageServer?.Dispose();
                    MessageServer = null;
                }   
            } 
        }

        private Task GainMutexTask { get; set; }
        private bool AbortMutex { get; set; } = false;
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            List<string> fileNames = new List<string>();
            foreach (string arg in args)
            {
                if (arg == "-d" || arg == "--debug")
                {
                    DebugMode = true;
                } else if (arg.EndsWith(".dmap"))
                {
                    fileNames.Add(arg);
                    StartupFileNames = fileNames.ToArray();
                }
            }

            bool newMutex;
            Mutex mutex = new Mutex(true, AppIdentifier, out newMutex);

            if (!newMutex)
            {
                if (StartupFileNames.Length > 0)
                {
                    using (IPCNamedPipeClient client = new IPCNamedPipeClient(AppIdentifier))
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
                            mutex = new Mutex(true, AppIdentifier, out isNewMutex);
                            if (isNewMutex)
                            {
                                s_mutex = mutex;
                            } else
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
            } else
            {
                s_mutex = mutex;
                HasMutex = true;
            }
            
#if DEBUG
            DebugMode = true;
#endif
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            LogHelper.OnMessageBox += LogHelper_OnMessageBox;
            LogHelper.OnFatalError += LogHelper_OnFatalError;
            LogHelper.LogMessage(LogLevel.Info, string.Format("Started Application {0}", Assembly.GetExecutingAssembly().FullName));
            Exit += App_Exit;
        }

        private void LogHelper_OnFatalError()
        {
            Current.Shutdown();
        }

        private void LogHelper_OnMessageBox(string message, string title)
        {
            MessageBox.Show(message, title);
        }

        private void App_Exit(object sender, ExitEventArgs e)
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
                LogHelper.LogMessage(LogLevel.Info, string.Format("DataBridge shutdown"));
            }
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogHelper.LogMessage(LogLevel.Fatal, string.Format("Unhandled Exception: {0} {1}", e.Exception.Message, e.Exception.StackTrace), e.Exception);
            e.Handled = true;
        }

        private void messageServer_OnMessageReceived(IPCMessage message)
        {
            OnMessageReceived(message);
        }
    }
}
