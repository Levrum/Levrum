using Levrum.Utils.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Levrum.Utils.Infra
{
    /// <summary>
    /// Utility platform for timing various operations ... code blocks, locks, etc.
    /// </summary>
    public static class TimeThis
    {
        public enum Kinds { CodeBlock, Lock, Custom }


        /// <summary>
        /// Log the elapsed time to complete a block of code.
        /// </summary>
        /// <param name="sLabel"></param>
        /// <param name="sSubCat"></param>
        /// <param name="dArg1"></param>
        /// <param name="dArg2"></param>
        /// <param name="fnCode"></param>
        /// <returns></returns>
        public static bool Code(string sLabel, double ttime, VoidDel fnCode)
        {
            return (Code(sLabel, "", "", 0.0, 0.0, ttime, fnCode));
        }

        static object m_oTimeThisLock = new object();

        public static bool Code(string sLabel, string sSubCat, string sDependencies, double dArg1, double dArg2, double ttime, VoidDel fnCode)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(TimeThis);
            try
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                DateTime TaskStart = DateTime.Now;
                fnCode();
                sw.Stop();

                Record(TaskStart, Kinds.CodeBlock, sLabel, sSubCat, sDependencies, dArg1, dArg2, sw.Elapsed.TotalMilliseconds, 0.0F, 0.0F, ttime);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }

        public static bool Code(string sLabel, string sSubCat, string sDependencies, double dArg1, double dArg2, double ttime, BoolDel fnCode)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(TimeThis);
            try
            {

                if (null == sLabel)
                {
                    StackTrace st = new StackTrace();
                    StackFrame[] frames = st.GetFrames();
                    if (frames.Length > 0) { sLabel = frames[0].GetMethod().Name + "()"; }
                    if (frames.Length > 1) { sSubCat = frames[1].GetMethod().Name + "()"; }
                }
                Stopwatch sw = new Stopwatch();
                sw.Start();
                DateTime TaskStart = DateTime.Now;
                fnCode();
                sw.Stop();
                Record(TaskStart, Kinds.CodeBlock, sLabel, sSubCat, sDependencies, dArg1, dArg2, sw.Elapsed.TotalMilliseconds, 0,0, ttime);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Record statistics for a code block categorically ... i.e., create a Stats object for the
        /// code label, that will be written to the file TimeThisCodeCategories.txt, when TimeThis.Flush() is called.
        /// </summary>
        /// <param name="sLabel"></param>
        /// <param name="fnCode"></param>
        /// <returns></returns>
        public static bool ByCategory(string sLabel, VoidDel fnCode)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(TimeThis);
            try
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                DateTime TaskStart = DateTime.Now;
                fnCode();
                sw.Stop();

                if (!m_oTimingStats.ContainsKey(sLabel))
                {
                    m_oTimingStats.Add(sLabel, new Stats());
                }
                Stats stats = m_oTimingStats[sLabel];
                stats.AddObs(sw.ElapsedMilliseconds);

                //Record(TaskStart, Kinds.CodeBlock, sLabel, sSubCat, sDependencies, dArg1, dArg2, sw.Elapsed.TotalMilliseconds, 0.0F, 0.0F, ttime);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Flush all data cached in current executable lifetime.
        /// </summary>
        /// <returns></returns>
        public static bool Flush()
        {
            string fn = "TimeThis.Flush()";
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(AppSettings.LogDir + "TimeThis_Categories_" + Util.TsYmdhmst + ".csv", false);
                sw.WriteLine("BenchmarkName,N,MeanMs,StDevMs,MinMs,MaxMs");
                SortedDictionary<string, Stats> sorted_lut = new SortedDictionary<string, Stats>();
                foreach (string skey in m_oTimingStats.Keys)
                {
                    sorted_lut.Add(skey, m_oTimingStats[skey]);
                }
                foreach (string skey in sorted_lut.Keys)
                {
                    Stats stats = sorted_lut[skey];
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Util.Csvify(skey));
                    sb.Append("," + stats.Count);
                    sb.Append("," + stats.Mean);
                    sb.Append("," + stats.StdDev);
                    sb.Append("," + stats.Min);
                    sb.Append("," + stats.Max);
                    sw.WriteLine(sb.ToString());

                }
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(TimeThis), fn, exc);
                return (false);
            }
            finally
            {
                if (null != sw) { sw.Close(); }
            }
        }

        /// <summary>
        ///  Lookup table of timing statistics.
        /// </summary>
        private static Dictionary<string, Stats> m_oTimingStats = new Dictionary<string, Stats>();


        /// <summary>
        /// Perform a locking operation and record timing information for it.
        /// If the lock object is invalid, logs an error and does NOT perform the lock
        /// or execute the protected code.
        /// </summary>
        /// <param name="oLockObj"></param>
        /// <param name="sLabel"></param>
        /// <param name="sSubCat"></param>
        /// <param name="dArg1"></param>
        /// <param name="dArg2"></param>
        /// <param name="fnCode"></param>
        /// <returns></returns>
        public static bool Lock(object oLockObject, string sLabel, VoidDel fnCode, double ttime)
        {
            return (Lock(oLockObject, sLabel, "", "", 0.0, 0.0, fnCode, ttime));
        }
        public static bool Lock(object oLockObj, string sLabel, string sDependencies, string sSubCat, double dArg1, double dArg2, VoidDel fnCode, double ttime)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(TimeThis);
            try
            {
                if (null == oLockObj)
                {
                    Util.HandleAppErr(type, fn, "Unable to lock null object for label/category " + sLabel + "/" + sSubCat);
                    Record(DateTime.Now, Kinds.Lock, sLabel, "**ERROR**", sDependencies, 0, 0, -1000.0, 0.0F, 0.0F, ttime);
                    return (false);
                }

                Stopwatch sw = new Stopwatch();
                sw.Start();

                lock (oLockObj)
                {
                    sw.Stop();
                    Record(DateTime.Now, Kinds.Lock, sLabel, sSubCat, sDependencies, dArg1, dArg2, sw.Elapsed.TotalMilliseconds, 0.0F, 0.0F, ttime);

                    fnCode();
                }

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }

        }


        public static List<TimeThisInfo> Data = new List<TimeThisInfo>();

        /// <summary>
        /// Record a single timing information item.
        /// </summary>
        /// <param name="qKind"></param>
        /// <param name="sLabel"></param>
        /// <param name="sCat"></param>
        /// <param name="dArg1"></param>
        /// <param name="dArg2"></param>
        /// <param name="dElapsedMs"></param>
        /// <returns></returns>
        public static bool Record(DateTime TaskStart, Kinds qKind, string sLabel, string sCat, string sDependencies, double dArg1, double dArg2, double dElapsedMs, float beforeRam, float afterRam, double ttime)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                TimeThisInfo tti = new TimeThisInfo();
                tti.AppName = AppSettings.AppName;
                tti.AppVersion = AppSettings.AppVersion;
                tti.TaskStartTime = TaskStart;
                tti.TaskEndTime = DateTime.Now;
                tti.Kind = qKind;
                tti.Label = sLabel;
                tti.Category = sCat;
                tti.Dependencies = sDependencies;
                tti.Arg1 = dArg1;
                tti.Arg2 = dArg2;
                tti.ElapsedMs = dElapsedMs;
                tti.MemoryAtStart = beforeRam;
                tti.MemoryAtEnd = afterRam;
                tti.TstampUid = Convert.ToInt64(ttime);
                Data.Add(tti);



                //Util.BenchmarkXy(typeof(TimeThis).Name + "." + qKind.ToString(), sLabel, sCat, dArg1, dArg2, dElapsedMs, beforeRam, afterRam );


                // Write the record to the persistence file (initializing the file if necessary):
                lock (m_oPersister)
                {
                    if (null == m_oDataFile)
                    {
                        string sfile = AppSettings.LocalDir + "TimeThis_Code.txt";
                        bool exists = File.Exists(sfile);
                        m_oDataFile = new StreamWriter(sfile, true);
                        if (!exists)
                        {
                            string styperec = m_oPersister.SerializeType(typeof(TimeThisInfo));
                            m_oDataFile.WriteLine(styperec);
                        }
                    }
                    string srec = m_oPersister.SerializeInst(typeof(TimeThisInfo), tti);
                    m_oDataFile.WriteLine(srec);
                    m_oDataFile.Flush();        //  this is inefficient!

                }


                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(TimeThisInfo), fn, exc);
                return (false);
            }

        }

        /// <summary>
        /// Persister for serializing/deserializing data.
        /// </summary>
        private static PersisterAscii m_oPersister = new PersisterAscii();

        /// <summary>
        /// Data file for persisting benchmark data.   Initialized on first reference by TimeThis.Record().
        /// </summary>
        private static StreamWriter m_oDataFile = null;

    }

    public class TimeThisInfo
    {

        public TimeThis.Kinds Kind = TimeThis.Kinds.CodeBlock;
        public long TstampUid = 0;
        public DateTime TaskStartTime = DateTime.MinValue;
        public DateTime TaskEndTime = DateTime.MinValue;
        public string AppName = "";
        public string AppVersion = "";
        public string Label = "";
        public string Category = "";
        public string Dependencies = "";
        public double Arg1 = 0.0;
        public double Arg2 = 0.0;
        public double ElapsedMs = 0.0;
        public double MemoryAtStart = 0.0;
        public double MemoryAtEnd = 0.0;

    }
}
