using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace Levrum.Utils.Infra
{
    public class Util
    {
        public static bool HandleAppErrOnce(object oSrc, object oContext, string sMessage)
        {
            string skey = oSrc?.ToString() + "|" + oContext?.ToString() + "|" + sMessage;
            if (m_oErrors.ContainsKey(skey)) { return (false); }
            m_oErrors.Add(skey, "");
            AddEvent(oSrc, Sev.AppError, oContext, sMessage);
            return (false);

        }

        private static Dictionary<string,string> m_oErrors = new Dictionary<string, string>();

        public static bool HandleExc(object oSrc, object oContext, Exception oExc)
        {
            AddEvent(oSrc,Sev.SysError,oContext,oExc.Message);
            return(false);
        }

        public static bool HandleAppErr(object oSrc, object oContext, string sMessage)
        {
            AddEvent(oSrc,Sev.AppError,oContext,sMessage);
            return(false);
        }

        public static bool HandleAppWarningOnce(object oSrc, object oContext, string sMessage)
        {
            string skey = oSrc?.ToString() + "|" + oContext?.ToString() + "|" + sMessage;
            if (m_oWarnings.ContainsKey(skey)) { return (false); }
            m_oWarnings.Add(skey,"");
            AddEvent(oSrc,Sev.UserError,oContext,sMessage);
            return(false);
        }

        private static Dictionary<string,string> m_oWarnings = new Dictionary<string, string>();

        /// <summary>
        /// Turn a string into a field name.
        /// </summary>
        /// <param name="sRawName"></param>
        /// <returns></returns>
        public static string MakeFieldName(string sRawName)
        {
            StringBuilder sb_field = new StringBuilder();
            foreach (char c in sRawName)
            {
                if ((char.IsLetterOrDigit(c)) || ('_' == c)) { sb_field.Append(c); }
                else if (' ' == c) { sb_field.Append('_'); }
                else if (char.IsPunctuation(c)) { sb_field.Append('_'); }
            }
            string sret = sb_field.ToString();
            if (sret.Length > 254) { sret = sret.Substring(0, 254); }
            return(sret);
        }

        public static string LeftMaxChars(string sText, int nMaxchars, bool bUseEllipsisIfTruncated)
        {
            if (string.IsNullOrEmpty(sText)) { return ("");  }
            if (nMaxchars<=0) { return ("");  }
            if (sText.Length<=nMaxchars) { return (sText);  }
            string sret = sText.Substring(0, nMaxchars);
            if (bUseEllipsisIfTruncated) { sret += "...";  }
            return (sret);
        }

        /// <summary>
        /// Handle a debug message with a source object and a context.
        /// </summary>
        /// <param name="sContext"></param>
        /// <param name="oSrc"></param>
        /// <param name="sMessage"></param>
        public static void Dbg(int iLevel, Object oSrc, String sContext, String sMessage)       // jpr-MT
        {
            const String fn = "Util.Dbg()";
            try
            {
                if (iLevel > AppSettings.DebugLevel) { return; }
                int threadid = Thread.CurrentThread.ManagedThreadId;

                LogMsg(string.Format("***Dbg|{0}|{1}|{2}|{3}|{4}|{5}", iLevel, threadid, Sev.Info, oSrc, sContext, sMessage));
            }
            catch (Exception Exc)
            {
                HandleExc(typeof(Util), fn, Exc);

            }
        }


        /// <summary>
        /// Write a message to the general logfile, CoeloGeneral.log
        /// </summary>
        /// <param name="sMsg"></param>
        public static void LogMsg(String sMsg) // jpr-MT // jpr-MT2
        {
            //const string fn = "Util.LogMsg()";
            try
            {
                InitLogFile();
                Stopwatch sw1 = new Stopwatch();
                sw1.Start();


                lock (m_oLockLogfile)
                {
                    DateTime n = DateTime.Now;
                    String sts = String.Format("{0:D4}/{1:D2}/{2:D2}|{3:D2}:{4:D2}:{5:D2}.{6:D3}|",
                            n.Year, n.Month, n.Day, n.Hour, n.Minute, n.Second, n.Millisecond);


                    String sfinalmsg = sts + sMsg;

                    m_swGeneralLogfile.WriteLine(sfinalmsg);
                    m_iWritesSinceFlush++;
                    if (m_iWritesSinceFlush >= 1)
                    {
                        m_swGeneralLogfile.Flush();
                        m_iWritesSinceFlush = 0;
                    }
                } // end lock(logfile-lock)

            } // end main try {}

            catch (Exception exc)
            {
                AddEvent(new StatusArgs(typeof(Util), Sev.Fatal, "Util.LogMsg()",
                                                "Exception: " + exc.Message +
                                                "\r\n\t" + exc.StackTrace)); // jpr-MT2
            }
        } // end LogMsg()


        private static void InitLogFile()
        {
            String fn = MethodBase.GetCurrentMethod().Name;

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            lock (m_oLockLogfile) // jpr-load
            {

                if (null == m_swGeneralLogfile)
                {
                    DateTime n = DateTime.Now;
                    String sdate = String.Format("{0:D4}{1:D2}{2:D3}", n.Year, n.Month, n.Day);
                    String sfile = AppSettings.LogDir + AppSettings.AppName + "_" + sdate + ".log";
                    m_swGeneralLogfile = new StreamWriter(sfile, true);

                    // 20101216 CDN - clean up old logfiles when opening a new one.
                    ThreadStart ts = new ThreadStart(CleanupLogDirThread);
                    Thread cleanup_thread = new Thread(ts);

                    Util.Dbg(3, typeof(AppSettings), "Starting Thread CleanupLogDirThread", "***********************************************************************************");

                    cleanup_thread.Start();
                } // endif(logfile object is null)
            } // end lock

        } // end method()

        /// <summary>
        /// Spend up to 5 seconds cleaning up the log directory of files older than
        /// AppSettings.DaysToKeepLogfiles days.
        /// </summary>
        private static void CleanupLogDirThread()
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            Type type = typeof(Util);
            try
            {
                Thread.Sleep(1500);     // wait for main app (whatever it is) to get constructed,
                                        // so any events we generate here will show up in event display.

                String[] files = Directory.GetFiles(AppSettings.LogDir);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int files_deleted = 0;
                foreach (String sfile in files)
                {
                    FileInfo fi = new FileInfo(sfile);
                    double age_days = DateTime.Now.Subtract(fi.LastWriteTime).TotalDays;
                    if (age_days > AppSettings.KeepLogfilesDays)
                    {
                        //Util.AddEvent(new StatusArgs(type,Sev.oFieldInfo,fn,"Deleted logfile " + fi.Name + " " + fi.LastWriteTime.ToShortDateString()));
                        fi.Delete();
                        files_deleted++;
                    }
                    if (sw.Elapsed.TotalSeconds > 5.0)
                    {
                        Util.AddEvent(new StatusArgs(type, Sev.UserError, fn, "Time allotted to logfile cleanup expired; cleanup may not have completed.")); // jpr-MT2
                        break;
                    }
                }
                if (files_deleted > 0)
                {
                    Util.AddEvent(new StatusArgs(type, Sev.Info, fn, "Deleted " + files_deleted +
                                    " logfiles from " + AppSettings.LogDir)); // jpr-MT2
                }
            }
            catch (Exception exc)
            {
                Util.AddEvent(new StatusArgs(type, Sev.SysError, fn, "Exception: " + exc.Message + "\r\n" + exc.StackTrace)); // jpr-MT2
                // No file logging here!   Logging exceptions is chancy, because this is most likely
                // running while the logfile is being initialized...
            }
        }


        public static bool AddEvent(object oSrc, Sev qSev, object oContext, string sMessage)
        {
            string scontext = "";
            if (null != oContext) { scontext = oContext.ToString(); }
            StatusArgs args = new StatusArgs(oSrc,qSev,scontext,sMessage);
            return(AddEvent(args));
        }

        /// <summary>
        /// Add an event to the queue, and notify any listeners.
        /// </summary>
        /// <param name="oArgs"></param>
        /// <returns></returns>
        public static bool AddEvent(StatusArgs oArgs)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                lock (Events)
                {
                    Events.Add(oArgs);                    
                    if (null != EventLogChanged)
                    {
                        EventLogChanged();
                    }

                    Console.WriteLine();
                    Console.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + DateTime.Now.Millisecond
                                        + " " + oArgs.Severity.ToString());
                    Console.WriteLine("  Context: " + oArgs.Context);
                    Console.WriteLine("  Subject: " + oArgs.Source.ToString());
                    Console.WriteLine("  Message: " + oArgs.Message);
                } // end lock(Events)
                return (true);
            }
            catch (Exception)
            {
                return (false);
            }
        }

        /// <summary>
        /// Clear the current event queue.
        /// </summary>
        public void ClearEvents()
        {
            if (null == Events) { return; }
            lock (Events)
            {
                Events.Clear();
            }
        }

        /// <summary>
        /// This event gets fired when the event log changes.
        /// </summary>
        public static event VoidDel EventLogChanged;


        /// <summary>
        /// Send a debugging message at priority level 1.
        /// </summary>
        /// <param name="oSrc"></param>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
		public static void Dbg(Object oSrc, String sContext, String sMessage)
        {
            Dbg(1, oSrc, sContext, sMessage);
        }



        /// <summary>
        /// Make sure a directory exists, make sure the name ends with "\\" and return the clean name.
        /// </summary>
        /// <param name="sDir"></param>
        /// <returns></returns>
        public static String SafeDir(String sDir)
        {
            const String fn = "Util.SafeDir()";

            try
            {
                String sdir = sDir;
                if (!Directory.Exists(sdir))
                {
                    if (null == Directory.CreateDirectory(sdir))
                    {
                        sdir = "";
                    }
                }
                if (!sdir.EndsWith("\\")) sdir += "\\";
                return (sdir);

            }// end main try()

            catch (Exception exc)
            {
                HandleExc(typeof(Util), fn, exc);
                return ("");
            } // end catch()

            finally
            {
            } // end finally { }
        }


        /// <summary>
        /// Turn a datum into a string suitable for writing to a CSV file.
        /// </summary>
        /// <param name="oData"></param>
        /// <returns></returns>
        public static string Csvify(Object oData)
        {
            if (null == oData) return ("");
            Type t = oData.GetType();
            if (typeof(int).IsAssignableFrom(t)) return (oData.ToString());
            if (typeof(double).IsAssignableFrom(t)) return (oData.ToString());
            String sdata = oData.ToString().TrimEnd();
            string sdata2 = "";
            foreach (char c in sdata)
            {
                if ('\"' == c) { sdata2 += "\"\""; } // escaping double quotes per RFC
                else { sdata2 += c; }        // otherwise copy the character verbatim
            }
            String sret = "\"" + sdata2 + "\"";
            return (sret);
        }



        /// <summary>
        /// Parse a CSV record into a list of strings representing field contents, in field order.
        /// </summary>
        /// <param name="sCsvRecord"></param>
        /// <returns></returns>
        public static List<String> ParseCsv(String sCsvRecord)
        {
            String fn = "Util.ParseCsv";
            List<String> retlist = new List<string>();
            try
            {
                char[] chars = sCsvRecord.ToCharArray();
                bool in_quote = false;
                String scurpiece = "";
                for (int i = 0; i < sCsvRecord.Length; i++)
                {
                    char c = chars[i];

                    // If it's a quote, we toggle the in-quote state:
                    if ('\"' == c)
                    {
                        in_quote = !in_quote;
                    }

                    // If it's an unquoted comma, we add the current column value to the list and reset; quoted
                    // commas get added to the current column value.
                    else if (',' == c)
                    {
                        if (in_quote) scurpiece += c;
                        else
                        {
                            retlist.Add(scurpiece);
                            scurpiece = "";
                        }
                    }

                    // Otherwise, we just add the character to the current piece and keep going.
                    else
                    {
                        scurpiece += c;
                    }
                } // end for(i)
                if (!String.IsNullOrEmpty(scurpiece))
                {
                    retlist.Add(scurpiece);
                    scurpiece = "";
                } // endif(a dangling piece)

                // 20180325 CDN - Special case for terminal commas: add an empty piece.
                // I think this could be accomplished by removing the condition on the
                // clause above, but worry about consequences for corner cases.
                if (sCsvRecord.EndsWith(",")) { retlist.Add(""); }


                return (retlist);
            } // end main try()
            catch (Exception exc)
            {
                Util.HandleExc(typeof(Util), fn, exc);
                return (retlist);
            }
        } // end ParseCsv()

        /// <summary>
        /// Encode or decode a single character.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="iXform"></param>
        /// <param name="bEncode"></param>
        /// <returns></returns>
        public static Char CodeChar(Char c, int iXform, bool bEncode)
        {
            const int imin = 29;    // smallest character to be encoded
            const int imax = 127;   // highest character to be encoded
            int ichar = (int)c;
            if ((ichar < imin) || (ichar > imax)) return (c);

            int icount = (imax - imin) + 1;
            int iwarp = iXform % icount;
            int ioffset = ichar - imin;     // s/b in [0,imax-1]
            int inewoffset = ioffset;
            if (bEncode) inewoffset = (inewoffset + iwarp) % icount;
            else inewoffset = (inewoffset - iwarp) % icount;
            if (inewoffset < 0) inewoffset += icount;

            int inewchar = imin + inewoffset;
            Char cret = (Char)inewchar;
            return (cret);

        }

        /// <summary>
        /// Split a string on a delimiter string.   E.g.,
        /// SplitOnString("Gallia%%est%omnis%%divisa","%%") will return
        /// { "Gallia", "est%omnis", "divisa" }.
        /// </summary>
        /// <param name="sVictim">String to be split</param>
        /// <param name="sDelim">Delimiter</param>
        /// <returns>List of string pieces</returns>
        public static List<String> SplitOnString(String sVictim, String sDelim)
        {
            const String fn = "Util.SplitOnString()";
            List<String> retlist = new List<String>();

            try
            {
                int dlen = sDelim.Length;
                int vlen = sVictim.Length;
                String saccum = "";
                int ichar = 0;
                while (ichar < vlen)
                {
                    // If no more delimiters will fit, stop:
                    if ((ichar + dlen) >= vlen)
                    {
                        saccum += sVictim.Substring(ichar, (vlen - ichar));
                        retlist.Add(saccum);
                        saccum = "";
                        ichar = vlen;
                        break;
                    }

                    // Else if match a delimiter, bump and flush:
                    else if (sVictim.Substring(ichar, dlen) == sDelim)
                    {
                        ichar += dlen;
                        retlist.Add(saccum);
                        saccum = "";
                    }

                    // Else - normal text; append to buffer:
                    else
                    {
                        saccum += sVictim[ichar];
                        ichar++;
                    }
                } // end while(chars to do)
                return (retlist);
            }// end main try()

            catch (Exception exc)
            {
                Util.HandleExc(typeof(Util), fn, exc);
                return (retlist);
            } // end catch()

            finally
            {
            } // end finally { }
        }

        public static Color RandomColorByName(int iSat, String sName)
        {
            if (iSat < 0) iSat = 0;
            if (iSat > 255) iSat = 255;
            int iseed = 0;
            for (int i = 0; i < sName.Length; i++)
            {
                iseed += ((int)sName[i]);
            }
            int r = 127 - ((iseed * 13579) % 100);
            int g = 127 - ((iseed * 10001) % 100);
            int b = 127 - ((iseed * 15721) % 100);
            Color c = Color.FromArgb(iSat, r, g, b);
            return (c);
        }


        /// <summary>
        /// Generate a random string from a palette of uppercase alpha characters.
        /// </summary>
        /// <param name="iLen"></param>
        /// <returns></returns>
        public static String RandomStr(int iLen)
        {
            String spalette = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return (RandomStr(iLen, spalette));
        }



        /// <summary>
        /// Generate a random string from the caller-supplied character palette.
        /// </summary>
        /// <param name="iLen"></param>
        /// <returns></returns>
        public static String RandomStr(int iLen, String sCharacterPalette)
        {
            String saccum = "";

            for (int i = 0; i < iLen; i++)
            {
                int ix = RandomGenerator.Next(65536) % sCharacterPalette.Length;
                char c = sCharacterPalette[ix];
                saccum += c;
            }
            return (saccum);
        } // end function


        /// <summary>
        /// Pick a random string from an array of strings.
        /// </summary>
        /// <param name="oPickList"></param>
        /// <returns></returns>
        public static string RandomItem(params String[] oPickList)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (null == oPickList) return ("");
                String s = oPickList[m_oItemRandomizer.Next() % oPickList.Length];
                return (s);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(Util), fn, exc);
                return ("");
            }

        }

        /// <summary>
        /// get Dynamic ResponseRecord Field Value 
        /// </summary>
        /// <param name="oRow"></param>
        /// <param name="response string"></param>
        /// <param name="fieldname"></param>
        /// <param name="bParsedOk">Did it parse ok?</param>
        /// <param name="reporterrors">Should absent fields be reported</param>
        /// <returns> object </returns>
        /// 
        public static object GetFieldDynamic(object oRow, out string resp, string fieldname, ref bool bParsedOk, bool isrequired, string sCsvFile)
        {
            string fn = "Util.getDynamicResponseRecordFieldValue";
            try
            {
                resp = "";

                object odata = oRow.GetFieldDynamic(fieldname, true, isrequired);
                if (null != odata) { resp = odata.ToString(); }
                else
                {
                    if (isrequired)
                    {
                        Util.HandleAppErrOnce(new object(), fn, "Required field '" + fieldname + "' not found in data:" + sCsvFile);
                        bParsedOk = false;
                        return (null);
                    }
                }
                return oRow;
            }
            catch (Exception exc)
            {
                resp = "";
                bParsedOk = false;
                Util.HandleExc(new object(), fn, exc);
                return (null);
            }
        }

        /// <summary>
        /// get Dynamic ResponseRecord Field Value 
        /// </summary>
        /// <param name="oRow"></param>
        /// <param name="response int"></param>
        /// <param name="fieldname"></param>
        /// <param name="bParsedOk">Did it parse ok?</param>
        /// <returns> object </returns>
        /// 
        public static object GetFieldDynamic(object oRow, out int resp, string fieldname, ref bool bParsedOk, bool isrequired, string sCsvFile)
        {
            string fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                resp = -999999;

                object odata = oRow.GetFieldDynamic(fieldname, true, isrequired);
                if (null != odata) { resp = (int)odata; }
                else
                {
                    if (isrequired)
                    {
                        Util.HandleAppErrOnce(new object(), fn, "Required field '" + fieldname + "' not found in data:" + sCsvFile);
                        bParsedOk = false;
                        return (null);
                    }
                }
                return oRow;
            }
            catch (Exception exc)
            {
                resp = -999999;
                bParsedOk = false;
                Util.HandleExc(new object(), fn, exc);
                return (null);
            }
        }

        public static object GetFieldDynamic(object oRow, out DateTime resp, string fieldname, ref bool bParsedOk, bool isrequired, string sCsvFile)
        {
            string fn = "Util.getDynamicResponseRecordFieldValue";
            try
            {
                resp = DateTime.MinValue;

                object odata = oRow.GetFieldDynamic(fieldname, true, isrequired);
                if (null != odata) { bParsedOk &= DateTime.TryParse(odata.ToString(), out resp); }
                else
                {
                    if (isrequired)
                    {
                        Util.HandleAppErrOnce(new object(), fn, "Required field '" + fieldname + "' not found in data:" + sCsvFile);
                        bParsedOk = false;
                        return (null);
                    }
                }
                return oRow;
            }
            catch (Exception exc)
            {
                resp = DateTime.MinValue;
                bParsedOk = false;
                Util.HandleExc(new object(), fn, exc);
                return (null);
            }
        }


        /// <summary>
        /// Is this type a scalar?
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsScalarType(Type oType)
        {
            if (null == oType) { return (false); }
            if (typeof(int).IsAssignableFrom(oType)) { return (true); }
            if (typeof(double).IsAssignableFrom(oType)) { return (true); }
            if (typeof(Decimal).IsAssignableFrom(oType)) { return (true); }
            if (typeof(String).IsAssignableFrom(oType)) { return (true); }
            if (typeof(Enum).IsAssignableFrom(oType)) { return (true); }
            if (typeof(DateTime).IsAssignableFrom(oType)) { return (true); }
            return (false);
        }

        public static string MilitaryTime(DateTime oTime)
        {
            String sfinal = String.Format("{0:D02}:{1:D02}:{2:D02}", oTime.Hour, oTime.Minute, oTime.Second);
            return (sfinal);
        }

        /// <summary>
        /// Is this an aggregate type?
        /// </summary>
        /// <param name="oType"></param>
        /// <returns></returns>
        public static bool IsAggregateType(Type oType)
        {
            try
            {
                FieldInfo[] fields = oType.GetFields();
                foreach (FieldInfo fi in fields)
                {
                    if ((fi.IsPublic) && (!fi.IsStatic)) { return (true); }  // if it has one or more public, non-static fields, we've got a hit
                }
                return (false);     // if no hits, not an aggregate...
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(Util), "Util.IsAggregateType()", exc);
                return (false);
            }
        }

        /// <summary>
        /// Prettyprint an XML document to a string, with indenting.
        /// </summary>
        /// <param name="oDoc"></param>
        /// <returns></returns>
        public static String PpXml(XmlDocument oDoc)
        {
            String fn = MethodBase.GetCurrentMethod().Name;
            try
            {
                lock (m_oPpLock)
                {
                    StringBuilder sb = new StringBuilder("");
                    StringWriter sw = new StringWriter(sb);
                    XmlTextWriter xtw = new XmlTextWriter(sw);
                    xtw.Formatting = Formatting.Indented;
                    xtw.Indentation = 2;
                    xtw.IndentChar = ' ';
                    oDoc.WriteTo(xtw);
                    xtw.Flush();
                    sw.Flush();
                    String sxml = sb.ToString();
                    sw.Close();
                    xtw.Close();
                    return (sxml);
                }
            }
            catch (Exception exc)
            {
                HandleExc(typeof(Util), fn, exc);
                return ("");
            } // end function
        }

        static Object m_oPpLock = new Object(); // lock object for prettyprinting

        /// <summary>
        /// Decode XML numeric character references into actual data.
        /// </summary>
        /// <param name="sval"></param>
        /// <returns></returns>
        public static string XmlDecode(string sval)
        {
            Type type = typeof(Util);
            try
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                while (i < sval.Length)
                {
                    char c = ' ';
                    int offset = 0;
                    if (helpParseCharRef(sval, i, out c, out offset))
                    {
                        sb.Append(c);
                        i += offset;
                    }
                    else
                    {
                        c = sval[i];
                        sb.Append(c);
                        i++;
                    } // end else(regular character)

                } // end whle(i / chars)

                String sret = sb.ToString();
                return (sret);
            } // end main try()
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(type, fn, exc);
                return ("");
            }
        }

        private static bool helpParseCharRef(string sval, int i, out char c, out int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encode any embedded control characters as valid XML.
        /// </summary>
        /// <param name="sValue"></param>
        /// <returns></returns>
        internal static string XmlEncode(string sValue)
        {
            Type type = typeof(Util);
            try
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < sValue.Length; i++)
                {
                    char c = sValue[i];
                    int ic = (int)c;
                    if (ic <= 27)
                    {
                        // For control characters, insert an XML numeric character reference, per
                        // http://en.wikipedia.org/wiki/List_of_XML_and_HTML_character_entity_references  20110927 CDN:
                        String numcharref = String.Format("&#{0:D03};", ic);
                        sb.Append(numcharref);
                    }
                    else
                    {
                        sb.Append(c);
                    }

                } // end for(i=char index)
                String sresult = sb.ToString();
                return (sresult);
            } // end main try()
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(type, fn, exc);
                return ("");
            }
        } // end method()


        public static bool DebugLevelIs(int level)
        {
            return DebugLevel >= level;
        }

        /// <summary>
        /// Get the elapsed time in seconds past midnight for a date time value that has significant
        /// fields only for the time of day.
        /// </summary>
        /// <param name="attStartTimeOfDay"></param>
        /// <returns></returns>
        public static double SecondsPastMidnight(DateTime oTimeOfDay)
        {
            try
            {
                double dsec =
                    (3600.0 * oTimeOfDay.Hour) +
                    (60.0 * oTimeOfDay.Minute) +
                    (oTimeOfDay.Second) +
                    (oTimeOfDay.Millisecond / 1000.0);
                return (dsec);
            }
            catch (Exception exc)
            {
                String fn = MethodBase.GetCurrentMethod().Name;
                Util.HandleExc(typeof(Util), fn, exc);
                return (0.0);
            }
        }


        /// <summary>
        /// Provide a timestamp string of the form YYYYMMDD_HHMMSS_TTT.
        /// </summary>
        public static String TsYmdhmst
        {
            get
            {
                try
                {
                    DateTime now = DateTime.Now;
                    String sret =
                        String.Format("{0:D4}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}_{6:D3}",
                                        now.Year, now.Month, now.Day,
                                        now.Hour, now.Minute, now.Second,
                                        now.Millisecond);
                    return (sret);

                }
                catch (Exception)
                {
                    return ("Util_TsError");
                }
            }
        }



        #region Public State

        public static int DebugLevel { get { return AppSettings.DebugLevel; } }


        /// <summary>
        /// List of all events generated by logging functions.
        /// </summary>
        public static List<StatusArgs> Events = new List<StatusArgs>();

        /// <summary>
        /// Random number generator.
        /// </summary>
        public static Random RandomGenerator = new Random();

        #endregion


        #region Private State
        public static object m_oLockLogfile = new object();     // Locking for the log file

        private static StreamWriter m_swGeneralLogfile = null;                      // jpr-MT (CDN fixed)
        private static Random m_oItemRandomizer = new Random();                     // low concurrency risk 20140222 CDN
        public static Dictionary<String, String> m_oErrorsSeen = new Dictionary<string, string>();               // jpr-MT (CDN fixed)
        private static DateTime m_dtmLastErrorSound = DateTime.Now;
        private static int m_iWritesSinceFlush = 0;// jpr-MT (CDN OK)
        private static Random m_Random = new Random();


        #endregion // private state

    } // end class Util
} 
