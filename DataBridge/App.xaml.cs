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

using Levrum.Utils;

namespace Levrum.DataBridge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] StartupFileNames { get; set; } = new string[0];
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
            LogHelper.LogMessage(LogLevel.Info, string.Format("DataBridge shutdown"));
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogHelper.LogMessage(LogLevel.Fatal, string.Format("Unhandled Exception: {0} {1}", e.Exception.Message, e.Exception.StackTrace), e.Exception);
            e.Handled = true;
        }
    }
}
