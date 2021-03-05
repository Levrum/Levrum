//--------------------------------------------------------------------------------
// CommonEvents.cs:  definition of essential events, delegates and arguments for
//		CoeloUtils.dll
//
//	Copyright (C) 2007-2008, Coelo Company of Design, Corvallis, OR, USA.
//	Proprietary and confidential;  unauthorized reproduction, transmission or use
//  prohibited.
//
//  History:
//		20080202 CDN: initial version.
//--------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

namespace Levrum.Utils.Infra
{


    public delegate bool FileEventHandler(Object oSrc, FileArgs oArgs);
    public delegate bool StatusEventHandler(Object oSrc, StatusArgs oArgs);
    public delegate void ProgressMessageHandler(String sProgressMessage);


    /// <summary>
    /// Root class for all  event args.   Auto-support for unique, sequential
    /// IDs, plus date-time stamping.
    /// </summary>
    public class CoeloEventArgs : EventArgs
    {
        /// <summary>
        /// Timestamp associated with event.  Automatically generated at
        /// event creation; can be reset from outside.
        /// </summary>
        public DateTime TimeStamp = DateTime.MinValue;

        /// <summary>
        /// Unique sequential ID.
        /// </summary>
        public long SeqUid
        {
            get
            {
                return (this.m_iSeqUid);
            }
        }


        /// <summary>
        /// Construct event arguments.  Default to current Windows date/time;
        /// assign sequential UID.
        /// </summary>
        public CoeloEventArgs()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            lock (typeof(CoeloEventArgs))// jpr-load
            {
                //Util.Benchmark(new object(), fn, "get-lock-24(typeof(CoeloEventArgs))", sw1);

                try
                {
                    m_iCurUid++;
                }
                catch (Exception)   // Assume rollover.
                {
                    m_iCurUid = 0;
                }
            }
            m_iSeqUid = m_iCurUid;
            TimeStamp = DateTime.Now;

        }



        private long m_iSeqUid = 0;			// UID for this instance. 
        private static long m_iCurUid = 0;  // Global UID counter.              // jpr-MT2
    } // End class EventArgs



    /// <summary>
    /// A file event.
    /// </summary>
    public class FileArgs : CoeloEventArgs
    {
        public FileArgs(String sFile, String sStatus)
        {
            Status = sStatus;
            FileName = sFile;
        }

        public String Status = "";
        public String FileName = "";
    }



    public enum TaskStatuses
    {
        Waiting,
        Running,
        Finished,
        Completed,
        PostCleanup,
        Error
    }


    /// <summary>
    /// Simple helper class for creating StatusArgs to go in
    /// events.
    /// </summary>
    public class CoeloStatus
    {

        /// <summary>
        /// Create an app-error event args
        /// </summary>
        /// <param name="oSrc"></param>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static StatusArgs AppErr(Object oSrc, String sContext, String sMessage)
        {
            return (new StatusArgs(oSrc, Sev.AppError, sContext, sMessage));
        }

        /// <summary>
        /// Create a system error event-args.
        /// </summary>
        /// <param name="oSrc"></param>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static StatusArgs SysErr(Object oSrc, String sContext, String sMessage)
        {
            return (new StatusArgs(oSrc, Sev.SysError, sContext, sMessage));
        }

        /// <summary>
        /// Create an informational event-args.
        /// </summary>
        /// <param name="oSrc"></param>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static StatusArgs Info(Object oSrc, String sContext, String sMessage)
        {
            return (new StatusArgs(oSrc, Sev.Info, sContext, sMessage));
        }


        /// <summary>
        /// Create a b_succeeded event-args.
        /// </summary>
        /// <param name="oSrc"></param>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static StatusArgs Ok(Object oSrc, String sContext, String sMessage)
        {
            return (new StatusArgs(oSrc, Sev.Success, sContext, sMessage));
        }


    }

    /// <summary>
    /// Occurrence count for a particular status.
    /// </summary>
    public class StatusCounter
    {
        public StatusCounter(StatusArgs oStatus, int iCount)
        {
            Count = iCount;
            Status = oStatus;
        }

        public int Count = 0;
        public StatusArgs Status = null;
    }


    /// <summary>
    /// </summary>
    public class StatusArgs : CoeloEventArgs
    {

        public StatusArgs(Object oSrc, String sContext, String sMessage)
        {
            Source = oSrc;
            Context = sContext;
            Message = sMessage;
            Severity = Sev.Info;
        }

        public virtual bool IsOK
        {
            get { return (Severity < Sev.UserError); }
        }

        public StatusArgs(Object oSrc, Sev qSev, String sContext, String sMessage)
        {
            Source = oSrc;
            Context = sContext;
            Message = sMessage;
            Severity = qSev;
        }

        public Object Source = null;
        public Sev Severity = Sev.Success;
        public String Context = "";
        public String Message = "";


        public static StatusArgs InternalError(object oSrc, string sContext, string sMessage)
        {
            return (new StatusArgs(oSrc, Sev.SysError, sContext, sMessage));
        }

        public static StatusArgs OK(object oSrc, string sContext, string sMessage)
        {
            return (new StatusArgs(oSrc, Sev.Success, sContext, sMessage));
        }
    }



} // end namespace 