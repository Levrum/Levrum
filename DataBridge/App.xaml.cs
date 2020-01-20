using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using NLog;
using NLog.Config;
using NLog.Targets;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] StartupFileNames { get; set; } = new string[0];
        public ILogger Logger { get; protected set; }
        public bool DebugMode { get; set; }

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

#if DEBUG
            DebugMode = true;
#endif

            Logger = LogManager.GetCurrentClassLogger();
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            LogMessage(LogLevel.Debug, string.Format("Started DataBridge: Assembly {0}", Assembly.GetExecutingAssembly().FullName));
            Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            LogMessage(LogLevel.Debug, string.Format("DataBridge shutdown"));
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogMessage(LogLevel.Fatal, e.Exception, "Unhandled Exception");
            e.Handled = true;
        }

        public void LogException(Exception ex, string message = "", bool showMessageBox = false)
        {
            try
            {
                Logger.Error(ex, message);
                if (showMessageBox)
                {
                    MessageBox.Show(string.Format("{0}: {1}\n{2}", message, ex.Message, ex.StackTrace));
                }
            } catch (Exception loggingException)
            {
                if (DebugMode == true)
                {
                    MessageBox.Show("Exception writing to log: {0}", loggingException.ToString());
                }
            }
        }

        public void LogMessage(LogLevel level, string message)
        {
            LogMessage(level, null, message);
        }

        public void LogMessage(LogLevel level, Exception ex = null, string message = "")
        {
            try
            {
                if (level == LogLevel.Fatal)
                {
                    Logger.Fatal(ex, message);
                    MessageBox.Show(string.Format("DataBridge must exit due to a fatal error: {0}.", message));
                    Current.Shutdown();
                } else if (level == LogLevel.Error) {
                    Logger.Error(ex, message);
                } else if (level == LogLevel.Warn) { 
                    Logger.Warn(ex, message);    
                } else if (level == LogLevel.Debug) {
                    Logger.Debug(ex, message);
                } else if (level == LogLevel.Trace) {
                    Logger.Trace(ex, message);
                } else {
                    Logger.Info(ex, message);
                }
            } catch (Exception loggingException)
            {
                if (DebugMode == true)
                {
                    MessageBox.Show("Exception writing to log: {0}", loggingException.ToString());
                }
            }
        }

        public static void ReceiveLogMessage(string time, string level, string message, string exception)
        {
            OnLogMessage?.Invoke(time, level, message, exception);
        }

        public static OnLogMessageDelegate OnLogMessage;

        public string GetLogEvents()
        {
            StringBuilder output = new StringBuilder();
            var target = LogManager.Configuration.FindTargetByName<MemoryTarget>("memory");
            var logEvents = target.Logs;
            foreach(string str in logEvents)
            {
                output.AppendLine(str);
            }

            return output.ToString();
        }

        public delegate void OnLogMessageDelegate(string time, string level, string message, string exception);
    }
}
