using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public static List<LogEntry> LogEntries = new List<LogEntry>();


        static LogHelper()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public static string PrettyprintLogEntries(int nMax = -1)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int lastIndex = LogEntries.Count - 1;
                int firstIndex = nMax == -1 ? 0 : LogEntries.Count - nMax;
                for (int i = firstIndex; i < LogEntries.Count; i++)
                {
                    LogEntry entry = LogEntries[i];

                    if (entry.Exception == null)
                    {
                        sb.AppendLine(string.Format("{0}.) {1} {2}: {3}", i.ToString().PadLeft(3, ' '), entry.Timestamp.ToLongTimeString(), entry.Level.ToString(), entry.Message));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("{0}.) {1} {2}: {3} EXCEPTION: {4}", i.ToString().PadLeft(3, ' '), entry.Timestamp.ToLongTimeString(), entry.Level.ToString(), entry.Message, entry.Exception.Message));
                    }
                }
                return sb.ToString();
            }
            catch (Exception exc)
            {
                LogException(exc);  // hopefully this doesn't cause infinite recursion
                return "Error retrieving log entries ... please see event log";
            }
        }

        public static void LogMessage(LogLevel level, string message = "", Exception ex = null)
        {
            LogEntry entry = new LogEntry(default, level, message, ex);
            lock (LogEntries)
            {
                LogEntries.Add(entry);
            }

            if (ex != null && level == LogLevel.Error)
            {
                LogException(ex, message, true);
            }
            else
            {
                LogMessage(GetNLogLevel(level), message, ex);
            }
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
                    OnMessageBox?.Invoke(string.Format("{0}: {1}", message, ex.Message), "Exception Occured");
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

        public static void LogErrOnce(string context, string message)
        {
            string smsg_hash = context + "|" + message;
            if (!m_oMessageTab.ContainsKey(smsg_hash))
            {
                m_oMessageTab.Add(smsg_hash, 1);
                LogMessage(LogLevel.Error, smsg_hash);
                return;
            }
            else
            {
                m_oMessageTab[smsg_hash]++;
            }
        }

        private static Dictionary<string, int> m_oMessageTab = new Dictionary<string, int>();

        public static void LogMessage(NLogLevel level, string message)
        {
            LogMessage(level, message, null);
        }

        public static void LogMessage(NLogLevel level, string message = "", Exception ex = null)
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

        #region Legacy Helper Functions
        public static bool HandleAppErr(object oSrc, string sContext, string sMsg)
        {
            LogMessage(LogLevel.Error, "Source: " + oSrc.ToString() + ";  Context: " + sContext + ";  Message: " + sMsg);
            return (false);
        }

        public static bool HandleExc(Object obj, string str, Exception ex)
        {
            LogException(ex, "Context: " + obj?.ToString() + "; " + str, true);
            return (false);
        }
        #endregion

        public static void DisplayMessageIfPossible(string sMsg)
        {
            try
            {
                if (null != OnMessageBox) { OnMessageBox(sMsg, ""); }
            }
            catch (Exception exc)
            {
                LogException(exc, "Error attempting in message box handler");
            }
        }
    } // end class{}

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; } = LogLevel.Info;
        public Exception Exception { get; set; } = null;
        public string Message { get; set; } = string.Empty;

        public LogEntry()
        {

        }

        public LogEntry(DateTime timeStamp = default, LogLevel level = LogLevel.Info, string message = "", Exception exception = null)
        {
            Timestamp = timeStamp == default ? DateTime.Now : timeStamp;
            Level = level;
            Exception = exception;
            Message = message;
        }
    }

}
