using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Environment;

namespace Levrum.Utils.Infra
{

    public delegate void GeneralThreadDel(params object[] oParams);

    /// <summary>
    /// Severity codes
    /// </summary>
    public enum Sev
    {
        Success,        // Operation succeeded
        Info,           // Information or warning
        UserError,      // Improper data from user
        AppError,       // Invalid condition in application
        SysError,       // Error in called API, or exception
        Fatal           // Error causing application termination
    }

    /// <summary>
    /// General form of something that can run as a safe thread.
    /// Add this interface to your class's derivation list, and implement
    /// the two obligatory methods -- then call SafeThread.Launch() on 
    /// an instance of your class to run your class's ThreadMain in 
    /// a separate thread.  Optionally subscribe to SafeThread events
    /// to be notified of thread start, completion and abnormal termination.
    /// Implement ISafeThread.Stop() in your class to enable graceful 
    /// shutdowns.
    /// </summary>
    public interface ISafeThread
    {
        /// <summary>
        /// Thread main routine to be inmplemented in your class.
        /// Should pass back true for normal and false for abnormal
        /// termination.
        /// </summary>
        bool ThreadMain(Object oParms);

        /// <summary>
        /// Your class's ThreadMain should respond nicely to this request!
        /// </summary>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        bool Shutdown(String sMessage);
    }

    /// <summary>
    /// This is a simple queue that can be shared among threads.
    /// It should be safe to insert objects in one thread, and
    /// remove them in another.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThreadSafeQueue<T>
    {
        public ThreadSafeQueue()
        {
            m_oQueue = new Queue<T>();
        }

        /// <summary>
        /// This event fires whenever someone puts something into the queue.
        /// A typical event consumer would have, say ThreadSafeEventQueue<mytype> mq,
        /// and might loop on WaitOne(mq.NewData,1000).
        /// </summary>
        public EventWaitHandle NewData = new EventWaitHandle(false, EventResetMode.AutoReset);

        /// <summary>
        /// Number of items on this queue.
        /// </summary>
        public virtual int Count
        {
            get
            {
                return (m_oQueue.Count);
            }
        }

        #region Public Methods

        /// <summary>
        /// Push an object onto the queue.   Should not interfere
        /// with another thread's attempts to insert or remove.
        /// </summary>
        /// <param name="tObj"></param>
        /// <returns></returns>
        public virtual bool Push(T tObj)
        {
            lock (this)
            {
                m_oQueue.Enqueue(tObj);
            }
            NewData.Set();
            return (true);
        }

        /// <summary>
        /// Pull an object from the queue.   Should not interfere
        /// with another thread's attempts to insert or remove.
        /// </summary>
        /// <returns></returns>
        public virtual T Pull()
        {
            T tval = default(T);
            lock (this)
            {
                if (m_oQueue.Count > 0)
                {
                    tval = m_oQueue.Dequeue();
                }
            }
            return (tval);
        }

        /// <summary>
        /// Non-destructive "peek" at first element.
        /// </summary>
        /// <returns></returns>
        public virtual T Peek()
        {
            T tval = default(T);
            lock (this)
            {
                if (m_oQueue.Count > 0)
                {
                    tval = m_oQueue.Peek();
                }
            }
            return (tval);
        }
        #endregion // Public Methods


        #region Data Members
        private Queue<T> m_oQueue = null;
        #endregion // Data Members

    }






    /// <summary>
    /// Standard signature for something that takes and returns nothing.
    /// </summary>
    public delegate void VoidDel();


    /// <summary>
    /// Standard signature for something that takes nothing and returns bool.
    /// </summary>
    public delegate bool BoolDel();

    /// <summary>
    /// Standard signature for debugging information events.
    /// </summary>
    /// <param name="iLevel"></param>
    /// <param name="iThread"></param>
    /// <param name="oSrc"></param>
    /// <param name="sContext"></param>
    /// <param name="sMsg"></param>
    public delegate void DebugInfoDel(int iLevel, int iThread, Object oSrc, String sContext, String sMsg);





    /// <summary>
    /// Static class for performing unit conversions.
    /// </summary>
    public static class UnitConversions
    {
        public static double MetersToFeet(double dMeters)
        {
            double inches = dMeters * 39.37;
            double feet = inches / 12.0;
            return (feet);
        }

        public static double MetersToMiles(double dMeters)
        {
            double miles = MetersToFeet(dMeters) / 5280.0;
            return (miles);
        }

        public static double FeetToMeters(double dFeet)
        {
            double meters = (dFeet * 12.0) / 39.37;
            return (meters);
        }

    }




    /// <summary>
    /// This class helps recursive functions detect whether they're headed for infinite recursion.
    /// To use, pass an instance of the class to the recursive function at level 0, then push a comparable unique identifier
    /// for the recursion argument(s) and test for duplication at entry;  don't forget to pop the argument UIDs at 
    /// return time (typically in a finally{} block).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RecursionStack<T>
        where T : IComparable
    {


        /// <summary>
        /// Has this argument key been seen in the call stack?
        /// </summary>
        /// <param name="oArg"></param>
        /// <returns></returns>
        public bool HasBeenSeen(T oArg)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (null == oArg) { return (false); }
                if (m_oDict.ContainsKey(oArg)) { return (true); }
                return (false);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }

        }

        /// <summary>
        /// Look for an argument key in the call stack.  If found, return false.  If not, push and return true.
        /// </summary>
        /// <param name="oArg"></param>
        /// <returns></returns>
        public bool TestAndPush(T oArg)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (HasBeenSeen(oArg)) { return (false); }
                m_oStack.Push(oArg);
                m_oDict.Add(oArg, m_oDict.Count);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }

        }


        /// <summary>
        /// Pop the top of the stack.
        /// </summary>
        /// <returns></returns>
        public bool Pop()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                T poppee = m_oStack.Peek();
                if (!m_oDict.ContainsKey(poppee)) { return (Util.HandleAppErr(this, fn, "Key not found: " + poppee.ToString())); }
                m_oDict.Remove(poppee);
                m_oStack.Pop();
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }

        }

        /// <summary>
        /// Clear the entire structure.
        /// </summary>
        /// <returns></returns>
        public bool Clear()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                m_oDict.Clear();
                m_oStack.Clear();
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            }
        }


        public int Depth
        {
            get
            {
                if (m_oStack.Count != m_oDict.Count)
                {
                    string fn = MethodBase.GetCurrentMethod().Name;
                    Util.HandleAppErrOnce(this, fn, "Mismatched stack/dictionary counts - " + m_oDict.Count + " / " + m_oStack.Count);
                }
                return (m_oStack.Count);
            }
        }

        /// <summary>
        /// Stack.
        /// </summary>
        private Stack<T> m_oStack = new Stack<T>();

        /// <summary>
        /// De-references items to stack depth (0-based).   Quick lookup of items.
        /// </summary>
        private Dictionary<T, int> m_oDict = new Dictionary<T, int>();



        /// <summary>
        /// ASCII print of stack, bottom to top.
        /// </summary>
        /// <returns></returns>
        public string Prettyprint()
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (T item in m_oStack)
                {
                    string skey = item.ToString();
                    string sdepth = "";
                    if (!m_oDict.ContainsKey(item)) { sdepth = "[unknown item]"; }
                    else { sdepth = m_oDict[item].ToString().PadLeft(14, ' '); }
                    sb.AppendLine(" " + sdepth + ": " + skey);
                }
                return (sb.ToString());
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("");
            }

        }
    }



    /// <summary>
    /// Useful utility functions on colors.
    /// </summary>
    public static class ColorUtil
    {
        /// <summary>
        /// Get the "distance" between two 32-bit colors, irrespective of alpha.
        /// </summary>
        /// <param name="oColor1"></param>
        /// <param name="oColor2"></param>
        /// <returns></returns>
        public static double ColorDistance(Color oColor1, Color oColor2)
        {
            int rdist = (oColor1.R - oColor2.R) * (oColor1.R - oColor2.R);
            int gdist = (oColor1.
                G - oColor2.G) * (oColor1.G - oColor2.G);
            int bdist = (oColor1.B - oColor2.B) * (oColor1.B - oColor2.B);
            double distance = Math.Sqrt(rdist + gdist + bdist);
            return (distance);
        }


    }





    /// <summary>
    /// Utility class for persisting static classes to and from INI files.
    /// 
    /// </summary>
    public static class StaticIniSaver
    {


        /// <summary>
        /// Save all static fields of a specific type to a specified
        /// INI file.
        /// </summary>
        /// <param name="oType"></param>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public static bool SaveTypeToIni(Type oType, String sFile)
        {
            const String fn = "AppSettings.Save()";

            try
            {

                StreamWriter sw = new StreamWriter(sFile);
                FieldInfo[] finfos = oType.GetFields(
                            BindingFlags.NonPublic | BindingFlags.Public
                            | BindingFlags.Static);
                foreach (FieldInfo fi in finfos)
                {
                    if (!fi.IsPublic) { continue; }  // 20101219 CDN - bugfix; ignore private data members.
                    String sfldname = fi.Name;
                    Object oval = fi.GetValue(null);
                    string sval = (null == oval) ? "" : oval.ToString();
                    sw.WriteLine(sfldname + "=" + sval);
                }
                sw.Close();
                return (true);
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(StaticIniSaver), fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        }


        /// <summary>
        /// Loads a static type from a specified INI file.
        /// </summary>
        /// <param name="oType"></param>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public static bool LoadTypeFromIni(Type oType, String sFile)
        {
            const String fn = "AppSettings.LoadTypeFromIni()";
            StreamReader sr = null;
            try
            {
                if (!File.Exists(sFile))
                {
                    Util.HandleAppErr(typeof(AppSettings), fn, "Unable to find file '" + sFile + "'");
                    return (false);
                }
                sr = new StreamReader(sFile);
                String srec = "";
                while (null != (srec = sr.ReadLine()))
                {
                    int ix = srec.IndexOf('=');
                    if ((ix < 0) || (ix >= (srec.Length + 1)))
                    {
                        Util.HandleAppErr(typeof(StaticIniSaver), fn,
                                "Invalid .INI line: " + srec);
                        return (false);
                    }

                    String sname = srec.Substring(0, ix);
                    String sval = srec.Substring(ix + 1, srec.Length - (ix + 1));
                    FieldInfo fi = oType.GetField(sname,
                                    BindingFlags.NonPublic | BindingFlags.Public
                                  | BindingFlags.Static);

                    // If field not found, it's undefined:
                    if (null == fi)
                    {
                        Util.HandleAppErr(typeof(StaticIniSaver),
                                            fn, "Undefined field: " + sname);
                        return (false);
                    }

                    // 20101219 CDN bugfix - ignore any non-public members:
                    if (!fi.IsPublic) { continue; }

                    Type fitype = fi.FieldType;
                    if (fitype.IsAssignableFrom(typeof(String)))
                    {
                        fi.SetValue(null, sval);
                    }
                    else if (fitype.IsAssignableFrom(typeof(Int32)))
                    {
                        fi.SetValue(null, Int32.Parse(sval));
                    }
                    else if (fitype.IsAssignableFrom(typeof(Double)))
                    {
                        fi.SetValue(null, Double.Parse(sval));
                    }
                    else if (typeof(Enum).IsAssignableFrom(fitype))  // 20140910 CDN - bugfix #1737 ... support enums in .ini files
                    {
                        object oval = Enum.Parse(fitype, sval);
                        fi.SetValue(null, oval);
                    }
                    else if (fitype.IsAssignableFrom(typeof(Boolean)))
                    {
                        fi.SetValue(null, Boolean.Parse(sval));
                    }
                    else
                    {
                        Util.HandleAppErr(typeof(StaticIniSaver), fn,
                                "Invalid data type in ini file: "
                                + sname + " (" + fitype + ")");
                        return (false);
                    }

                } // end while(reading)

                return (true);
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(StaticIniSaver), fn, exc);
                return (false);
            } // end catch()

            finally
            {
                if (null != sr) sr.Close();
                sr = null;
            } // end finally { }
        }



    } // end class StaticIniSaver



    /// <summary>
    /// Selector for describing how application files (data files, config files, log files, etc) are stored.
    /// </summary>
    public enum FileLocationMode
    {
        Default,                // root directory is {drive}:\_coelo\
        Relative                // root directory is parent ("..") of entry assembly launch directory
    }

    /// <summary>
    /// Global application settings
    /// </summary>
    public static class AppSettings
    {

        /// <summary>
        /// Where are files located?
        /// </summary>
        public static FileLocationMode FileLocation = FileLocationMode.Default;


        /// <summary>
        /// Root directory for this application.  Defaults to c:\_coelo\{AppName}\
        /// </summary>
		public static String RootDir
        {
            get
            {
                Assembly entry_assy = Assembly.GetEntryAssembly();

                switch (FileLocation)
                {
                    case FileLocationMode.Relative:
                        if (null != entry_assy)
                        {
                            string launch_loc = entry_assy.Location;
                            FileInfo launch_file = new FileInfo(launch_loc);
                            DirectoryInfo launch_dir = launch_file.Directory;
                            DirectoryInfo parent_dir = launch_dir.Parent;

                            m_sRootDir = parent_dir.FullName;
                        }
                        break;
                    case FileLocationMode.Default:
                    default:
                        if (String.IsNullOrEmpty(m_sRootDir))
                        {
                            String sdrive = "c:";
                            if (UseLaunchDriveAsRoot)
                            {
                                if (null != entry_assy)
                                {
                                    String spath = entry_assy.Location;
                                    sdrive = spath.Substring(0, 1) + ":";
                                }
                            }
                            m_sRootDir = AppSettings.DefaultRootDir;  // KMK review 20190417
                        }
                        break;
                }

                return (Util.SafeDir(m_sRootDir));
            }
            set
            {
                m_sRootDir = value;
                if (!m_sRootDir.EndsWith("\\")) { m_sRootDir += "\\"; }
            }
        }



        /// <summary>
        /// Default root directory.    Set on first reference to property RootDir, from 
        /// combination of true root plus appname.   Can be overridden by setting RootDir.
        /// </summary>
        private static String m_sRootDir = "";



        /// <summary>
        /// Temporary file directory.
        /// </summary>
        public static String TempDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "Temp\\"));
            }
        }

        public static string LocalTempDir
        {
            get
            {
                return (Util.SafeDir(LocalDir + "Temp\\"));
            }
        }

        public static String LocalDir
        {
            get
            {
                return Util.SafeDir(DefaultRootDir);
            }
        }

        public static String DefaultRootDir
        {
            get
            {
                // KMK review 20190417   #1 - path stuff needs to be platform independent.  Check my work?
                //    https://stackoverflow.com/questions/867485/c-sharp-getting-the-path-of-appdata
                //    https://stackoverflow.com/questions/6041332/best-way-to-get-application-folder-path/35295609
                //    https://stackoverflow.com/questions/42708939/get-the-windows-system-directory-from-a-net-standard-library
                string sappdata_folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return(sappdata_folder);
                //String sdrive = "c:";
                //if (UseLaunchDriveAsRoot)
                //{
                //    if (null != entry_assy)
                //    {
                //        String spath = entry_assy.Location;
                //        sdrive = spath.Substring(0, 1) + ":";
                //    }
                //}
                //return sdrive + "\\_coelo\\" + AppName + "\\";
            }
        }


        /// <summary>
        /// Is the application running in demo mode (suppresses error messages, etc).
        /// </summary>
        public static string DemoMode = "N";
        /// <summary>
        /// Is the application running in demo mode?  (High-perf encapsulation of string DemoMode).
        /// </summary>
        public static bool IsInDemoMode
        {
            get
            {
                if (!m_bDemoModeInit)
                {
                    m_bDemoModeInit = true;
                    m_bDemoMode = (DemoMode.Trim().ToUpper().StartsWith("Y"));
                }
                return (m_bDemoMode);
            }
        }
        private static bool m_bDemoModeInit = false;
        private static bool m_bDemoMode = false;



        /// <summary>
        /// 
        /// </summary>
        public static String AppVersion
        {
            get
            {
                Assembly entry_assembly = Assembly.GetEntryAssembly();
                String version = "2.0.0.0";
                if (null != entry_assembly) { version = entry_assembly.GetName().Version.ToString(); }
                return (version);
            }
        }

        public static String DebugDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "debug\\"));
            }
        }

        public static String DocDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "docs\\"));
            }
        }

        public static String ImageDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "images\\"));
            }
        }

        /// <summary>
        /// List of month names in chronological order.
        /// </summary>
        public static List<string> OrderedMonthNameList
        {
            get
            {
                if (null == m_oMonthNames)
                {
                    m_oMonthNames = new List<string>();
                    m_oMonthNames.AddRange(MonthNamesInOrder.Split(",".ToCharArray()));

                }
                return (m_oMonthNames);
            }
        }
        private static List<string> m_oMonthNames = null;

        /// <summary>
        /// Should the "launch drive" be used to define the root directory?
        /// The launch drive is the drive letter of the directory containing the entry 
        /// assembly -- typically the executable image running the application.
        /// Defaults to false;  use true, for example, to run the application off
        /// of a jump drive and persist data to the same drive.
        /// Does not affect the standard directory structure.
        /// </summary>
        public static bool UseLaunchDriveAsRoot
        {
            get { return (m_bUseLaunchDriveAsRoot); }
            set { m_bUseLaunchDriveAsRoot = value; }
        }
        private static bool m_bUseLaunchDriveAsRoot = true;


        /// <summary>
        /// Maximum number of items to be displayed in RapidObjectGridCtl instances.
        /// </summary>
        public static int MaxGridItems = 1000;


        /// <summary>
        /// Minimum interval between error sounds.   If lots of errors get generated, the sounds can 
        /// get quite annoying.   This setting allows the developer to control that experience.
        /// </summary>
        public static double MinErrorSoundIntervalSec = 5.0;


        public static int ErrorBlinkDuration = 10;

        /// <summary>
        /// Turn Simulation increment & completed sounds on or off
        /// </summary>
        public static int SimulationSoundsOnOff = 1; // 1 = On 0 = off

        /// <summary>
        /// Max width of grids
        /// </summary>
        public static int MaxGridWidth = 20;

        /// <summary>
        /// Debugging log verbosity.   Higher numbers ==> more verbose.  
        /// General idea:
        /// 0 = No debug data written at all.
        /// 1 = application startup/shutdown, exceptions
        /// 2 = key lifecycle events
        /// 5 = routine-level diagnostics (no more than 1k/minute)
        /// 10 = the gloves are off.
        /// </summary>
        public static int DebugLevel = 5;

        /// <summary>
        /// Definition of ordered month names for localization.
        /// </summary>
        public static string
            MonthNamesInOrder =
                "January,February,March,April,May,June,July,August,September,October,November,December";


        /// <summary>
        /// Are debugging assertions active?   Set to 1 or higher to enable, 0 to disable.
        /// </summary>
        public static int AssertsActive = 1;


        /// <summary>
        /// Is special formatting (e.g., in grids) enabled?   Enables the [DisplayFormat] attribute.
        /// Expected values are "Y" and "N".
        /// </summary>
        public static string EnableSpecialFormatting = "N";

        /// <summary>
        /// Number of days to keep logfiles.   Files older than this range are automatically deleted
        /// each time a new logfile is initialized (up to a specific time limit).
        /// </summary>
        public static double KeepLogfilesDays = 5.0;

        /// <summary>
        /// Application name.   Set this prior to calling AppSettings.Initialize(), to determine
        /// the application's root directory, and other characteristics.
        /// </summary>
		public static String AppName = "Unknown";

        public static String LogDir
        {
            get { return (Util.SafeDir(LocalDir + "log\\")); }
        }

        public static String DataDir
        {
            get { return (Util.SafeDir(RootDir + "data\\")); }
        }

        public static String CfgDir
        {
            get { return (Util.SafeDir(RootDir + "config\\")); }
        }

        public static string LocalCfgDir
        {
            get { return (Util.SafeDir(LocalDir + "config\\")); }
        }

        /// <summary>
        /// Perform application initialization, but fail if another instance of the same application
        /// is already running.
        /// </summary>
        /// <param name="sAppName"></param>
        /// <returns></returns>
        public static bool InitSingleInstance(String sAppName)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(Util);
            try
            {
                if (null != m_oSingleInstanceMUtex)
                {
                    Util.HandleAppErr(type, fn, "You can only call InitSingleInstance() once within an application lifetime");
                    return (false);
                }

                m_oSingleInstanceMUtex = new Mutex(false, "CoeloUtils.Util.SingleInstanceMutex." + sAppName);
                if (!m_oSingleInstanceMUtex.WaitOne(5)) { return (false); }

                Init(sAppName);

                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Mutex for enforcing single app instance rule.
        /// </summary>
        private static Mutex m_oSingleInstanceMUtex = null;


        public static void Init(String sAppName)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            AppSettings.AppName = sAppName;
            String sload_err = "Unable to confirm AppSettings load";

            if (!StaticIniSaver.LoadTypeFromIni(typeof(AppSettings), IniFile))
            {
                if (!StaticIniSaver.SaveTypeToIni(typeof(AppSettings), IniFile))
                {
                    sload_err = "Unable to save default AppSettings";
                }
                else
                {
                    if (!StaticIniSaver.LoadTypeFromIni(typeof(AppSettings), IniFile))
                    {
                        sload_err = "Saved default AppSettings, but unable to reload...";
                    }
                    else
                    {
                        sload_err = "AppSettings default values have been set up.";
                    }
                }
            }
            else
            {
                sload_err = "";
            }

            Assembly exec_assembly = Assembly.GetExecutingAssembly();
            String sexever = "{unknown - IIS?}";
            if (null != exec_assembly)
            {
                String sfile = exec_assembly.Location;
                FileInfo fi = new FileInfo(sfile);
                sexever = fi.Name + ", " + fi.LastWriteTime.ToShortDateString() + ", " + fi.Length + " bytes";
            }
            String scwd = Directory.GetCurrentDirectory();

            Util.Dbg(1, typeof(AppSettings), fn, "***********************************************************************************");
            Util.Dbg(1, typeof(AppSettings), fn, "***********************************************************************************");
            Util.Dbg(1, typeof(AppSettings), fn, "**** Initializing application: " + sAppName + "; Version=" + AppSettings.AppVersion +
                                                 "; Executable: " + sexever + "; current directory = " + scwd);
            if (String.IsNullOrEmpty(sload_err))
            {
                Util.Dbg(1, typeof(AppSettings), fn,
                        "Successfully loaded " + IniFile + "; debug=" + AppSettings.DebugLevel);
            }
            else
            {
                Util.HandleAppErr(typeof(AppSettings), fn, sload_err);
            }

        }

        public static String IniFile
        {
            get
            {
                String sfile = CfgDir + "AppSettings.ini";
                return (sfile);
            }
        }


        /// <summary>
        /// Load AppSettings from AppSettings.ini in the config directory.   Assumes AppName has
        /// been set correctly prior to execution.
        /// </summary>
        /// <returns></returns>
        public static bool Load()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (!File.Exists(IniFile))
                {
                    Save();
                }
                bool ok = StaticIniSaver.LoadTypeFromIni(typeof(AppSettings), IniFile);
                return (ok);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(AppSettings), fn, exc);
                return (false);
            }
        }


        /// <summary>
        /// Save AppSettings to AppSettings.ini.
        /// </summary>
        /// <returns></returns>
        public static bool Save()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                bool ok = StaticIniSaver.SaveTypeToIni(typeof(AppSettings), IniFile);
                return (ok);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(AppSettings), fn, exc);
                return (false);
            }
        }


        public static string DefaultColorNames =
            "DarkGoldenrod,DarkBlue,DarkGreen,Orange,Brown,Cyan,Magenta,Teal,DarkSalmon,LightGreen,BlueViolet,DarkOrange,Indigo";



        public static List<Color> GetDefaultColors()
        {
            const string fn = "AppSettings.GetDefaultColors()";
            List<Color> colors = new List<Color>();
            try
            {
                string[] color_names = DefaultColorNames.Split(",".ToCharArray());
                foreach (string color_name in color_names)
                {
                    PropertyInfo pi = typeof(Color).GetProperty(color_name);
                    if (null == pi)
                    {
                        Util.HandleAppErrOnce(typeof(AppSettings), fn, "Color '" + color_name + "' defined in AppSettings.DefaultColorNames is not recognized.");
                        continue;
                    }
                    Color cur_color = (Color)pi.GetValue(typeof(Color), null);
                    colors.Add(cur_color);
                }
                return (colors);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(Color), fn, exc);
                return (colors);
            }
        } // end method()
    }



    /// <summary>
    /// Static class for audit-logging of events.
    /// </summary>
    public static class Audit
    {


        /// <summary>
        /// Get the filename for the audit file.
        /// </summary>
        public static String FileName
        {
            get
            {
                Init();
                return (m_sFileName);
            }
        }

        /// <summary>
        /// Initialize the audit log.
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            if (null != m_swAuditFile) return (true);
            const String fn = "Audit.Init()";
            try
            {
                m_sFileName = C3InfraSettings.AuditDir + "CoeloAudit.log";
                m_swAuditFile = new StreamWriter(m_sFileName, true);
                return (true);
            }

            catch (Exception exc)
            {
                Util.HandleExc(typeof(Audit), fn, exc);
                return (false);
            }
        }

        /// <summary>
        /// Finish the audit file at the end of the executable image.
        /// </summary>
        /// <returns></returns>
        public static bool Finish()
        {
            if (null != m_swAuditFile)
            {
                m_swAuditFile.Close();
                m_swAuditFile = null;
            }
            return (true);
        }


        /// <summary>
        /// Log a single event to the logfile.
        /// </summary>
        /// <param name="sContext"></param>
        /// <param name="qSev"></param>
        /// <param name="sMessage"></param>
        /// <returns></returns>
        public static bool LogEvent(String sContext, Sev qSev, String sMessage)
        {
            if (null == m_swAuditFile)
            {
                if (!Init()) return (false);
            }
            DateTime now = DateTime.Now;
            String sdt = String.Format("{0:D4}/{1:D2}/{2:D2}", now.Year, now.Month, now.Day);
            String stm =
                String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", now.Hour, now.Minute,
                              now.Second, now.Millisecond);
            String srec =
                sdt + Delim +
                stm + Delim +
                C3InfraSettings.CurrentUserName + Delim +
                qSev.ToString() + Delim +
                sMessage + Delim +
                sContext + Delim;

            m_swAuditFile.WriteLine(srec);
            m_swAuditFile.Flush();
            return (true);

        }

        /// <summary>
        /// Delimiter for audit file.   Useful for loading into Excel, etc.
        /// </summary>
        public static String Delim = "|";

        private static String m_sFileName = "";
        private static StreamWriter m_swAuditFile = null;
    } // end class Audit


    /// <summary>
    /// Configuration info for utilities   
    /// </summary>
    public static class C3InfraSettings
    {
        /// <summary>
        /// Root path for CoeloUtils settings relative to current application.
        /// </summary>
        public static String RootDir
        {
            get
            {

                // KMK review 20190417 CDN - 
                //return (Util.SafeDir(Application.CommonAppDataPath + "Config\\"));
                return(AppSettings.RootDir);
            }
        }

        /// <summary>
        /// CoeloUtils log directory.
        /// </summary>
        public static String LogDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "Log\\"));
            }
        }

        /// <summary>
        /// CoeloUtils audit directory.
        /// </summary>
        public static String AuditDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "Audit\\"));
            }
        }



        /// <summary>
        /// CoeloUtils config directory.
        /// </summary>
        public static String ConfigDir
        {
            get
            {
                return (Util.SafeDir(RootDir + "Config\\"));
            }
        }


        /// <summary>
        /// Current user ID.   Set/retrieved by client application.
        /// </summary>
        public static String CurrentUserName = "Unknown";

    }


    /// <summary>
    /// Any instance of a class derived from this class can have
    /// all its nonstatic, public fields written to or read from an
    /// ASCII file by the Save() and Load() methods, respectively.
    /// Rules for derived classes:
    /// -- all public fields must be int, double, String or a class
    ///    derived from PersistentObj
    /// -- no referential circularities!  (E.g., t1->t2->t1 is illegal,
    ///    where t1 and t2 are derived from PersistentObj).
    /// -- must implement a default constructor.
    /// </summary>
    public class PersistentObj
    {


        /// <summary>
        /// Determines whether or not an instance belongs to a class
        /// that follows the rules for PersistentObj.
        /// </summary>
        /// <param name="bLogErrors"></param>
        /// <param name="bRecurse"></param>
        /// <returns></returns>
        public virtual bool IsValid(bool bLogErrors, bool bRecurse)
        {
            return (IsValid(bLogErrors, bRecurse, null));
        }

        public virtual bool IsValid(bool bLogErrors, bool bRecurse,
                    List<Type> TypesSeen)
        {
            const String fn = "PersistentObj.IsValid()";

            try
            {
                PersistentObj hpo = this as PersistentObj;
                if (null == hpo)
                {
                    if (bLogErrors) Util.HandleAppErr(this, fn, "Object of wrong type: " + this.GetType());
                    return (false);
                }

                // Check for circularities:
                Type curtype = this.GetType();
                if (null == TypesSeen) TypesSeen = new List<Type>();
                String circlist = "";
                foreach (Type typeseen in TypesSeen)
                {
                    circlist += typeseen.ToString() + "==>";
                    if (typeseen.Equals(curtype))
                    {
                        circlist += curtype.ToString();
                        if (bLogErrors) Util.HandleAppErr(this, fn, "Circularity detected: " + circlist);
                        return (false);
                    }
                }
                TypesSeen.Add(curtype);

                // Examine each field:
                FieldInfo[] fis = curtype.GetFields();
                foreach (FieldInfo fi in fis)
                {
                    if (fi.IsStatic) continue;
                    Type ftype = fi.FieldType;
                    String fname = fi.Name;
                    bool atomic = ValidAtomicType(ftype);
                    if (!atomic)
                    {
                        if (IsValidVectorType(ftype, bRecurse, TypesSeen)) return (true);
                        bool molecular = (typeof(PersistentObj).IsAssignableFrom(ftype));
                        if (!molecular)
                        {
                            if (bLogErrors) Util.HandleAppErr(this, fn, "Field " + fname + " is of invalid type: " + ftype);
                            return (false);
                        }
                        ConstructorInfo ci = fi.FieldType.GetConstructor(new Type[] { });
                        if (null == ci)
                        {
                            if (bLogErrors) Util.HandleAppErr(this, fn, "Field " + fname + " (type " + ftype + ") has no default constructor");
                            return (false);
                        }
                        Object odummy = ci.Invoke(new Object[] { });
                        if (null == odummy)
                        {
                            if (bLogErrors) Util.HandleAppErr(this, fn, "Field " + fname + ": unable to create a " + ftype);
                            return (false);
                        }
                        if (bRecurse)
                        {
                            PersistentObj hpochild = odummy as PersistentObj;
                            if (null == hpochild)
                            {
                                if (bLogErrors) Util.HandleAppErr(this, fn, "Field " + fname + " could not be converted to PersistentObj");
                                return (false);
                            }
                            if (!hpochild.IsValid(bLogErrors, true, TypesSeen))
                            {
                                if (bLogErrors)
                                {
                                    Util.HandleAppErr(this, fn, "Error(s) in child field " + fname + ", as described above...");
                                }
                                return (false);
                            } // endif(recursion fails)
                        } // endif(recursion on)
                    } // endif(not atomic)

                } // end foreach(field)

                // If we get here, it's all good:
                if ((null != TypesSeen) && (TypesSeen.Contains(this.GetType())))
                {
                    TypesSeen.Remove(this.GetType());
                }
                return (true);
            }// end main try()

            catch (Exception exc)
            {
                if (bLogErrors) Util.HandleExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {


            } // end finally { }
        }


        /// <summary>
        /// Determines whether or not a type is a valid vector of PersistentObj
        /// -derived types.
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsValidVectorType(Type oType, bool bRecurse, List<Type> TypesSeen)
        {
            const String fn = "PersistentObj.IsValidVectorType()";

            try
            {
                // The type must be IList<T> where T : PersistentObj
                if (!IsValidListType(oType))
                {
                    //Util.HandleAppErr(typeof(PersistentObj),fn,"Not a valid list type: " + oType);
                    return (false);
                }

                // Must be a single type parameter:
                Type[] typeparms =
                    oType.GetGenericArguments();
                if ((null == typeparms) | (1 != typeparms.Length)) return (false);
                Type eltype = typeparms[0];


                // And a dummy instance should be recursively valid:
                ConstructorInfo ci = eltype.GetConstructor(new Type[] { });
                if (null == ci) return (false);
                PersistentObj hpo = ci.Invoke(new Object[] { }) as PersistentObj;
                if (null == hpo) return (false);
                return (hpo.IsValid(true, bRecurse, TypesSeen));
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersistentObj), fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        }

        /// <summary>
        /// Check whether this adheres to:
        ///		IList[T] where T : PersistentObj // and square=angle brackets
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsValidListType(Type oType)
        {
            const String fn = "PersistentObj.IsvalidListType()";

            try
            {
                if (!oType.IsGenericType) return (false);

                // Need to make sure the containing type is a list here...
                // there is probably a better way to do this:
                //if (!Util.LssMatch("List",oType.Name)) return(false);
                if (null == oType.GetInterface("IList")) return (false);


                // Should have a single, PersistentObj-derived type parm:
                Type[] typeparms =
                    oType.GetGenericArguments();
                if ((null == typeparms) | (1 != typeparms.Length)) return (false);
                Type eltype = typeparms[0];
                if (!typeof(PersistentObj).IsAssignableFrom(eltype))
                {
                    return (false);
                }

                return (true);

            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersistentObj), fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        }


        /// <summary>
        /// Save the current instance to the specified ASCII file.
        /// If you supply a filename, the file will be created/overwritten,
        /// then closed.   If you supply an open stream, the current instance
        /// will be appended to the stream.
        /// NOTE (20061026 CDN): currently *ignores* any fields that
        /// aren't String, int or double.
        /// </summary>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public virtual bool Save(String sFile)
        {
            const String fn = "PersistentObj.Save(String)";
            StreamWriter sw = null;

            try
            {
                sw = new StreamWriter(sFile);
                return (Save(sw));
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {
                if (null != sw) sw.Close();
            } // end finally { }
        }


        public virtual bool Save(StreamWriter oFile)
        {
            return (Save(oFile, 0));
        }
        public virtual bool Save(StreamWriter oFile, int iIndent)
        {
            const String fn = "PersistentObj.Save()";

            try
            {

                // First, make sure this class follows the rules:
                if (!this.IsValid(true, true)) return (false);

                String sindent = "";
                for (int i = 0; i < iIndent; i++) sindent += " ";
                Type otype = this.GetType();
                FieldInfo[] finfos =
                    otype.GetFields();
                List<PersistentObj> curlist = null;
                // Start with the type:
                oFile.WriteLine(sindent + "%" + this.GetType().AssemblyQualifiedName);
                // Dump fields in "name=value" format:
                foreach (FieldInfo fi in finfos)
                {
                    if (fi.IsStatic) continue;
                    Type ftype = fi.FieldType;
                    String sfldname = fi.Name;

                    Object oval = fi.GetValue(this);
                    if (ValidAtomicType(ftype))
                    {
                        oFile.WriteLine(sindent + sfldname + "=" + EncodeCrlfs(oval.ToString()));
                    }
                    else if (null != (curlist = ToValidList(fi, ftype)))
                    {
                        oFile.WriteLine(sindent + sfldname + "=" + KList);
                        if (!SaveList(oFile, curlist, iIndent + 1)) return (false);
                    }
                    else // molecular:
                    {

                        String fname = fi.Name;
                        PersistentObj hpo = oval as PersistentObj;
                        if (null == hpo)
                        {
                            Util.HandleAppErr(this, fn, "Can't save field " + fname +
                                "; type incompatible with PersistentObj: " +
                                ftype.ToString());
                            return (false);
                        }
                        oFile.WriteLine(sindent + sfldname + "=" + KMolec);
                        if (!hpo.Save(oFile, iIndent + 1)) return (false);
                    }
                }
                oFile.WriteLine(sindent);   // terminate with empty line.
                return (true);
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(StaticIniSaver), fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        } // end Save(stream)




        /// <summary>
        /// Saves a list of PersistentObj's to the specified stream.
        /// </summary>
        /// <param name="oList"></param>
        /// <param name="iIndent"></param>
        /// <returns></returns>
        protected virtual bool SaveList(StreamWriter oFile, List<PersistentObj> oList, int iIndent)
        {
            const String fn = "PersistentObj.SaveList()";

            try
            {
                String sindent = "";
                for (int i = 0; i < iIndent; i++) sindent += " ";

                bool wroteheader = false;
                foreach (PersistentObj hpo in oList)
                {
                    if (!wroteheader)
                    {
                        Type containedtype = hpo.GetType();
                        wroteheader = true;
                        oFile.WriteLine(sindent + oList.Count + "|"
                            + containedtype.AssemblyQualifiedName);
                    }
                    if (!hpo.Save(oFile, iIndent + 1)) return (false);
                }
                return (true);
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        }



        /// <summary>
        /// Safe cast of the value of a field specified by a FieldInfo as
        /// a generic list of PersistentObj instances.
        /// </summary>
        /// <param name="oFi"></param>
        /// <param name="oFldType"></param>
        /// <returns></returns>
        protected virtual List<PersistentObj>
            ToValidList(FieldInfo oFi, Type oFldType)
        {
            const String fn = "PersistentObj.ToValidList()";

            try
            {

                if (!IsValidListType(oFldType))
                {
                    return (null);
                }
                Object oval = oFi.GetValue(this);
                if (null == oval) return (null);
                IList ilist = oval as IList;
                if (null == ilist)
                {
                    Util.HandleAppErr(this, fn, "Can't cast to IList: " + oval.GetType());
                    return (null);
                }
                List<PersistentObj> retlist = new List<PersistentObj>();
                foreach (Object obj in ilist)
                {
                    PersistentObj hpo = obj as PersistentObj;
                    if (null == hpo)
                    {
                        Util.HandleAppErr(this, fn, "Can't convert to PersistentObj: " + obj.GetType());
                        return (null);
                    }
                    retlist.Add(hpo);
                }
                return (retlist);

            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (null);
            } // end catch()

            finally
            {
            } // end finally { }
        }



        /// <summary>
        /// Prettyprint the current object to a string.
        /// </summary>
        /// <returns></returns>
        public virtual String Prettyprint(int iIndent)
        {
            const String fn = "PersistentObj.Prettyprint()";
            const String crlf = "\r\n";
            const String sindentchar = " ";

            try
            {
                String sindent = "";
                for (int i = 0; i < iIndent; i++) sindent += sindentchar;
                Type type = this.GetType();
                String saccum = sindent + type.ToString() + ": " + crlf;
                FieldInfo[] finfos = type.GetFields();
                foreach (FieldInfo fi in finfos)
                {
                    if (fi.IsStatic) continue;
                    String slocalindent = sindent + sindentchar;
                    Type ftype = fi.FieldType;
                    String fname = fi.Name;
                    if (ValidAtomicType(ftype))
                    {
                        saccum += slocalindent + fname + " = " + fi.GetValue(this).ToString();
                        saccum += crlf;
                    }
                    else if (typeof(PersistentObj).IsAssignableFrom(ftype))
                    {
                        saccum += slocalindent + fname + ": " + crlf;
                        PersistentObj hpochild = fi.GetValue(this) as PersistentObj;
                        saccum += hpochild.Prettyprint(iIndent + 1) + crlf;
                    }
                    else if (IsValidListType(ftype))
                    {
                        IList ilist = fi.GetValue(this) as IList;
                        int count = ilist.Count;
                        saccum += slocalindent + fname + "[count=" + count + "]: ";
                        foreach (PersistentObj hpochild in ilist)
                        {
                            saccum += hpochild.Prettyprint(iIndent + 1) + crlf;
                            saccum += slocalindent + "============" + crlf;
                        }
                    }

                } // end foreach(field)
                return (saccum);

            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return ("");
            } // end catch()

            finally
            {
            } // end finally { }
        }

        /// <summary>
        /// Load the current instance from an ASCII file.  Populates all
        /// public fields whose names are found in the file.  If you 
        /// supply a filename, the file is opened, read and closed.  If
        /// you supply an open stream, the current instance is read from
        /// the current file position, and the file is NOT closed.
        /// </summary>
        /// <param name="sFile"></param>
        /// <returns></returns>
        public virtual bool Load(String sFile)
        {
            const String fn = "PersistentObj.Load()";
            StreamReader sr = null;

            try
            {
                sr = new StreamReader(sFile);
                return (Load(sr));
            }// end main try()

            catch (Exception exc)
            {
                HandleLoadExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {
                if (null != sr) sr.Close();

            } // end finally { }
        } // end Load(filename)


        public virtual bool Load(StreamReader oFile)
        {
            return (Load(oFile, 0));
        }
        /// <summary>
        /// Load from an open stream.   Indent level indicates
        /// how many spaces at the beginning of each line are to be discarded.
        /// </summary>
        /// <param name="oFile"></param>
        /// <param name="iIndent"></param>
        /// <returns></returns>
        public virtual bool Load(StreamReader oFile, int iIndent)
        {
            const String fn = "PersistentObj.Load()";

            try
            {
                m_iLineNo = 0;
                String srec = "";

                // First make sure this instance's class follows the rules:
                if (!this.IsValid(true, true)) return (false);

                // Check the type:
                String stypeline = GetData(oFile, iIndent);
                if (string.IsNullOrEmpty(stypeline))
                {
                    HandleLoadErr(this, fn, "Invalid persistence data: no type line");
                    return (false);
                }
                if (("%" != stypeline.Substring(0, 1)) || (stypeline.Length < 2))
                {
                    HandleLoadErr(this, fn, "Invalid type line: " + stypeline);
                    return (false);
                }
                String sfiletype = stypeline.Substring(1, stypeline.Length - 1);
                Type filetype = Type.GetType(sfiletype, false, true);
                if (!this.GetType().IsAssignableFrom(filetype))
                {
                    HandleLoadErr(this, fn,
                        "Type in persistence file (" + sfiletype +
                        ") does not match instance type: " + this.GetType().ToString());
                    return (false);
                }

                // Load fields until we encounter a blank line:
                Type otype = this.GetType();
                while (!string.IsNullOrEmpty(srec = GetData(oFile, iIndent)))
                {
                    int ix = srec.IndexOf('=');
                    if ((ix < 0) || (ix >= (srec.Length + 1)))
                    {
                        HandleLoadErr(typeof(StaticIniSaver), fn,
                                "Invalid .INI line: " + srec);
                        return (false);
                    }

                    String sname = srec.Substring(0, ix);
                    String sval = srec.Substring(ix + 1, srec.Length - (ix + 1));
                    FieldInfo fi = otype.GetField(sname);
                    if (null == fi)
                    {
                        HandleLoadErr(typeof(StaticIniSaver),
                                            fn, "Undefined field: " + sname);
                        return (false);
                    }
                    Type fitype = fi.FieldType;
                    List<Type> types_seen = new List<Type>();
                    if (fitype.IsAssignableFrom(typeof(String)))
                    {
                        fi.SetValue(this, DecodeCrlfs(sval));
                    }
                    else if (fitype.IsAssignableFrom(typeof(Int32)))
                    {
                        fi.SetValue(this, Int32.Parse(sval));
                    }
                    else if (fitype.IsAssignableFrom(typeof(Double)))
                    {
                        fi.SetValue(this, Double.Parse(sval));
                    }
                    else if (IsValidVectorType(fitype, true, types_seen))
                    {
                        if (KList != sval)
                        {
                            HandleLoadErr(this, fn,
                                "Invalid persistence file; list field '" + fi.Name +
                                "' is ill-formed.");
                            return (false);
                        }
                        if (!LoadList(fi, otype, oFile, iIndent + 1)) return (false);
                    }
                    else if (typeof(PersistentObj).IsAssignableFrom(fitype))
                    {
                        if (KMolec != sval)
                        {
                            HandleLoadErr(this, fn,
                                "Invalid persistence file; molecular field '" + fi.Name +
                                "' is ill-formed.");
                            return (false);
                        }
                        ConstructorInfo ci = fitype.GetConstructor(new Type[] { });
                        if (null == ci)
                        {
                            HandleLoadErr(this, fn, "Cannot load field " + fi.Name
                                + ": no default constructor for type " + fitype.Name);
                            return (false);
                        }
                        Object oval = ci.Invoke(new object[] { });
                        if (null == oval)
                        {
                            HandleLoadErr(this, fn, "Unable to populate field "
                                + fi.Name + "; unable to create " + fitype.Name);
                            return (false);
                        }
                        PersistentObj hpo = oval as PersistentObj;
                        if (null == hpo)
                        {
                            HandleLoadErr(this, fn, "Cannot convert " + oval.GetType()
                                + " to PersistentObj");
                        }
                        if (!hpo.Load(oFile, iIndent + 1)) return (false);
                        fi.SetValue(this, hpo);
                    }
                    else
                    {
                        HandleLoadErr(typeof(StaticIniSaver), fn,
                                "Invalid data type in field: "
                                + sname + " (" + fitype + ")");
                        return (false);
                    }

                } // end while(reading)
                return (true);


            }// end main try()

            catch (Exception exc)
            {
                HandleLoadExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        } // end Load(Stream)

        /// <summary>
        /// Encode any CRLFs in a string in a form that will not mess up
        /// the persistence file.
        /// </summary>
        /// <param name="sOrig">String that may contain CRLFs</param>
        /// <returns>String with CRLFs safely encoded</returns>
        protected virtual String EncodeCrlfs(String sOrig)
        {
            List<String> scrlf_pcs = Util.SplitOnString(sOrig, "\r\n");
            String saccum = "";
            foreach (String sline in scrlf_pcs)
            {
                if (saccum.Length > 0) saccum += "\x07\x07";
                saccum += sline;
            }
            return (saccum);
        }


        /// <summary>
        /// Decode the CRLFs in a string that have been encoded by EncodeCrlfs().
        /// </summary>
        /// <param name="sOrig">Encoded string</param>
        /// <returns>Decoded string, containing CRLFs</returns>
        protected virtual String DecodeCrlfs(String sOrig)
        {
            List<String> spcs = Util.SplitOnString(sOrig, "\x07\x07");
            String saccum = "";
            foreach (String sline in spcs)
            {
                if (saccum.Length > 0) saccum += "\r\n";
                saccum += sline;
            }
            return (saccum);
        }

        protected virtual bool LoadList(FieldInfo oFI, Type oType, StreamReader oFile, int iIndent)
        {
            String fname = "";
            if (null != oFI) fname = oFI.Name;
            String fn = "PersistentObj.LoadList(" + fname + ")";
            try
            {
                String scountline = GetData(oFile, iIndent);
                String[] scountpcs = scountline.Split("|".ToCharArray());
                int count = Int32.Parse(scountpcs[0]);
                String svaltype = scountpcs[1];
                Type valtype = Type.GetType(svaltype, false, true);
                if (null == valtype)
                {
                    HandleLoadErr(this, fn, "Unable to find type " + svaltype);
                    return (false);
                }
                ConstructorInfo cival = valtype.GetConstructor(new Type[] { });
                if (null == cival)
                {
                    return (HandleLoadErr(this, fn, "No default c'tor for " + svaltype));
                }
                Type listtype = typeof(List<>).MakeGenericType(new Type[] { valtype });
                ConstructorInfo cilist = listtype.GetConstructor(new Type[] { });
                IList ilist = cilist.Invoke(new object[] { }) as IList;
                if (null == ilist)
                {
                    HandleLoadErr(this, fn, "Unable to create instance of " + listtype);
                    return (false);
                }

                for (int i = 0; i < count; i++)
                {
                    PersistentObj hpo = cival.Invoke(new object[] { }) as PersistentObj;
                    if (null == hpo)
                    {
                        return (HandleLoadErr(this, fn, "Unable to create a " + svaltype));
                    }
                    if (!hpo.Load(oFile, iIndent + 1))
                    {
                        HandleLoadErr(this, fn, "Previous error occurred loading list");
                        return (false);
                    }
                    if (hpo.GetType() != valtype)
                    {
                        HandleLoadErr(this, fn, "List value of wrong type: expected " + valtype + ", found " + hpo.GetType());
                        return (false);
                    }
                    ilist.Add(hpo);

                } // end for(i)

                // at this point, our list have the right type and
                // contain the right objects:
                oFI.SetValue(this, ilist);
                return (true);


            }// end main try()

            catch (Exception exc)
            {
                HandleLoadExc(this, fn, exc);
                return (false);
            } // end catch()

            finally
            {
            } // end finally { }
        }


        /// <summary>
        /// Get data from a line in a persistence file, ignoring
        /// "indent characters."
        /// </summary>
        /// <param name="oFile"></param>
        /// <param name="iIndent"></param>
        /// <returns></returns>
        protected virtual String GetData(StreamReader oFile, int iIndent)
        {
            const String fn = "PersistentObj.GetData()";

            try
            {
                String srec = oFile.ReadLine();
                m_iLineNo++;
                m_sLastLine = srec;
                if (null == srec) return (null);
                int l = srec.Length;
                if (iIndent >= l) return ("");
                String sdata = srec.Substring(iIndent, l - iIndent);
                return (sdata);
            }// end main try()

            catch (Exception exc)
            {
                HandleLoadExc(this, fn, exc);
                return (null);
            } // end catch()

            finally
            {
            } // end finally { }
        } // end GetData()


        protected virtual bool HandleLoadErr(Object oSrc, String sContext, String sMsg)
        {
            sMsg = "Error loading file at line " + m_iLineNo + ": " + sMsg + "; Last line: " + m_sLastLine;
            return (Util.HandleAppErr(oSrc, sContext, sMsg));
        }
        protected virtual bool HandleLoadExc(Object oSrc, String sContext, Exception oExc)
        {
            String smsg = "Exception loading file at line " + m_iLineNo + ": " + oExc.Message + "; Last line: " + m_sLastLine;
            return (Util.HandleAppErr(oSrc, sContext, smsg));
        }

        public virtual bool ValidAtomicType(Type oType)
        {
            bool atomic = (typeof(String).IsAssignableFrom(oType));
            atomic |= (typeof(int).IsAssignableFrom(oType));
            atomic |= (typeof(double).IsAssignableFrom(oType));
            return (atomic);

        }


        #region Static Constants
        public static String KMolec = "*MOLECULE*"; // indicates molecular data value
        public static String KList = "*LIST* {";
        public static String KEndList = "} *LIST*";
        #endregion

        #region Data Members
        private int m_iLineNo = 0;
        private String m_sLastLine = "";
        #endregion
    } // end class PersistentObj


    public class CommandExecutor
    {
        public bool ExecWithOutput(String sExecutable, String sArgs, int iTimeoutMs, out String rsOutput)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            rsOutput = "";
            try
            {
                if (null != m_oProc)
                {
                    Util.HandleAppErr(this, fn, "A process is already running.");
                    return (false);
                }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = sExecutable;
                psi.Arguments = sArgs;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;



                m_oProc = new Process();
                m_oProc.StartInfo = psi;
                //m_oProc.Exited += new EventHandler(HandleProcessExit);
                bool return_code = true;
                if (!m_oProc.Start())
                {
                    Util.HandleAppErr(this, fn, "Unable to start process.");
                    return_code = false;
                }

                //if (!m_hProcExit.WaitOne(iTimeoutMs))
                //{
                //    ok = false;
                //}
                //m_oProc.Exited -= new EventHandler(HandleProcessExit);
                rsOutput = m_oProc.StandardOutput.ReadToEnd();
                bool exited = m_oProc.WaitForExit(iTimeoutMs);
                //Util.HandleErr(Sev.oFieldInfo,this,fn,"Cmd=" + sExecutable +"; response=" + rsOutput.Length + " bytes");
                if (!exited)
                {
                    Util.HandleAppErr(this, fn, "Could not confirm child process exit.");
                    return_code = false;
                }

                return (return_code);
            }
            catch (Exception exc)
            {
                Util.HandleExc(this, fn, exc);
                return (false);

            }
            finally
            {
                m_oProc = null;
            }
        }

        void HandleProcessExit(object sender, EventArgs e)
        {
            m_oData = new StringBuilder();
            String srec = "";
            while (null != (srec = m_oProc.StandardOutput.ReadLine()))
            {
                m_oData.AppendLine(srec);
            }
            m_hProcExit.Set();
        }

        void HandleOutputReceived(object sender, DataReceivedEventArgs e)
        {
            m_oData.Append(e.Data);
        }

        private Process m_oProc = null;
        private StringBuilder m_oData = new StringBuilder();
        private EventWaitHandle m_hProcExit = new EventWaitHandle(false, EventResetMode.AutoReset);


    }


    /// <summary>
    /// Used for representing any DAG.
    /// </summary>
    public class HierarchyNode<T>
    {
        public T Tag = default(T);

        public List<HierarchyNode<T>> Children = new List<HierarchyNode<T>>();
    }



    /// <summary>
    /// Attribute [Invisible], indicating that a tagged instance is not to be displayed in prototyping
    /// tools such as RapidObjectGridControl.
    /// </summary>
    public class InvisibleAttribute : Attribute
    {
        public InvisibleAttribute()
        {
        }

        public static bool Set(Type type, string fieldName, bool invisible)
        {
            try
            {
                FieldInfo field = type.GetField(fieldName);
                if (null == field)
                    return Util.HandleAppErrOnce(typeof(InvisibleAttribute), MethodBase.GetCurrentMethod().Name, "Field '" + fieldName + "' is not defined on type " + type.Name);

                return Set(field, invisible);
            }
            catch (Exception ex)
            {
                Util.HandleExc(typeof(InvisibleAttribute), MethodBase.GetCurrentMethod().Name, ex);
                return false;
            }
        }

        /// <summary>
        /// Dynamically make a field invisible at runtime. Also hopefully.
        /// </summary>
        /// <param name="oField"></param>
        /// <param name="bInvisible"></param>
        /// <returns></returns>
        public static bool Set(FieldInfo field, bool invisible)
        {
            try
            {
                string fieldName = string.Format("{0}.{1}", field.DeclaringType.Name, field.Name);
                lock (s_invisibleFields)
                {
                    if (invisible)
                        s_invisibleFields.Add(fieldName);
                    else if (s_invisibleFields.Contains(fieldName))
                        s_invisibleFields.Remove(fieldName);
                }
                return (true);
            }
            catch (Exception exc)
            {
                string fn = MethodBase.GetCurrentMethod().Name;
                Type type = typeof(InvisibleAttribute);
                Util.HandleExc(type, fn, exc);
                return (false);
            }
        }

        private static HashSet<string> s_invisibleFields = new HashSet<string>();

        public static bool Get(FieldInfo field)
        {
            string fieldName = string.Format("{0}.{1}", field.DeclaringType.Name, field.Name);

            return s_invisibleFields.Contains(fieldName);
        }
    }



}