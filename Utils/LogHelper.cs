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
        public static List<LogEntry> LogEntries = new List<LogEntry>();


        static LogHelper()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public static string PrettyprintLogEntries(int nMax)
        {
            const string fn = "LogHelper.PrettyprintLogEnries()";
            try
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; (i < LogEntries.Count) && (i < (nMax - 1)); i++)
                {
                    LogEntry entry = LogEntries[i];

                    sb.Append(i.ToString().PadLeft(3, ' ') + ".) ");
                    sb.Append(entry.Timestamp.ToLongTimeString() + " ");
                    sb.Append(entry.Level.ToString() + ": ");
                    sb.Append(entry.Message + "  ");
                    if (null!=entry.Exc)
                    {
                        sb.Append(" EXCEPTION: " + entry.Exc.Message);
                    }
                    sb.AppendLine();
                }
                return (sb.ToString());

            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);  // hopefully this doesn't cause infinite recursion
                return ("Error retrieving log entries ... please see event log");
            }
        }

        public static void LogMessage(LogLevel level, string message = "", Exception ex = null)
        {

            LogEntry entry = new LogEntry();
            entry.Message = message;
            entry.Level = level;
            entry.Exc = ex;
            lock (LogEntries)
            {
                LogEntries.Add(entry);
            }

            if (ex != null && level == LogLevel.Error)
            {
                LogException(ex, message, true);
            } else {
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

        public  static void LogErrOnce(string context, string message)
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

        public static void DisplayMessageIfPossible(string sMsg)
        {
            try
            {
                if (null != OnMessageBox) { OnMessageBox(sMsg, ""); }
            }
            catch(Exception exc)
            {
                LogException(exc, "Error attempting in message box handler");
            }
        }
    } // end class{}

    public class LogEntry
    {
        public DateTime Timestamp = DateTime.Now;
        public LogLevel Level = LogLevel.Info;
        public Exception Exc = null;
        public string Message = "";
    }

}
