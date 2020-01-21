using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

using NLog;
using NLogLevel = NLog.LogLevel;
using NLog.Config;
using NLog.Targets;

namespace Levrum.Utils
{
    public enum LogLevel { Fatal, Error, Warn, Info, Debug, Trace };
    public delegate void LogHelperMessageBoxDelegate(string message, string title);
    public delegate void LogHelperFatalErrorDelegate();

    public static class LogHelper
    {
        public static Logger Logger { get; set; }
        public static bool DebugMode { get; set; } = false;

        public static event LogHelperMessageBoxDelegate OnMessageBox;
        public static event LogHelperFatalErrorDelegate OnFatalError;

        static LogHelper()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public static void LogMessage(LogLevel level, string message = "", Exception ex = null)
        {
            LogMessage(GetNLogLevel(level), ex, message);
        }

        public static NLogLevel GetNLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return NLogLevel.Fatal;
                case LogLevel.Error:
                    return NLogLevel.Error;
                case LogLevel.Warn:
                    return NLogLevel.Warn;
                case LogLevel.Info:
                    return NLogLevel.Info;
                case LogLevel.Debug:
                    return NLogLevel.Debug;
                default:
                    return NLogLevel.Trace;
            }
        }


        public static void LogException(Exception ex, string message = "", bool showMessageBox = false)
        {
            try
            {
                Logger.Error(ex, message);
                if (showMessageBox)
                {
                    OnMessageBox?.Invoke(message, "Exception Occured");
                }
            }
            catch (Exception loggingException)
            {
                if (DebugMode == true)
                {
                    OnMessageBox?.Invoke(string.Format("Exception writing to log: {0}", loggingException.ToString()), "Logging Exception");
                }
            }
        }

        public static void LogMessage(NLogLevel level, string message)
        {
            LogMessage(level, null, message);
        }

        public static void LogMessage(NLogLevel level, Exception ex = null, string message = "")
        {
            try
            {
                if (level == NLogLevel.Fatal)
                {
                    Logger.Fatal(ex, message);
                    OnMessageBox?.Invoke(string.Format("DataBridge must exit due to a fatal error: {0}.", message), "Fatal Error");
                    OnFatalError?.Invoke();
                }
                else if (level == NLogLevel.Error)
                {
                    Logger.Error(ex, message);
                }
                else if (level == NLogLevel.Warn)
                {
                    Logger.Warn(ex, message);
                }
                else if (level == NLogLevel.Debug)
                {
                    Logger.Debug(ex, message);
                }
                else if (level == NLogLevel.Trace)
                {
                    Logger.Trace(ex, message);
                }
                else
                {
                    Logger.Info(ex, message);
                }
            }
            catch (Exception loggingException)
            {
                if (DebugMode == true)
                {
                    OnMessageBox?.Invoke(string.Format("Exception writing to log: {0}", loggingException.ToString()), "Logging Exception");
                }
            }
        }

        public static void ReceiveLogMessage(string time, string level, string message, string exception)
        {
            OnLogMessage?.Invoke(time, level, message, exception);
        }

        public static OnLogMessageDelegate OnLogMessage;

        public static string GetLogEvents()
        {
            StringBuilder output = new StringBuilder();
            var target = LogManager.Configuration.FindTargetByName<MemoryTarget>("memory");
            var logEvents = target.Logs;
            foreach (string str in logEvents)
            {
                output.AppendLine(str);
            }

            return output.ToString();
        }

        public delegate void OnLogMessageDelegate(string time, string level, string message, string exception);
    }
}
