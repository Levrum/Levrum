using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Levrum.Utils.Infra
{

    /// <summary>
    /// Encapsulates JSON persistence.    Currently uses Newtonsoft.Json;   may not preserve
    /// object topology.   E.g.,   ((A->B)&&(C->B)) may rehydrate into ((A->B)&&(C->B'))
    /// </summary>
    public class PersisterJson
    {
        public static bool SaveToFile<T>(string sFile, T oSubj)
        {
            const string fn = "PersisterJson.SaveToFile()";
            try
            {
                
                string sjson = JsonConvert.SerializeObject(oSubj);
                File.WriteAllText(sFile, sjson);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersisterJson), fn, exc);
                return (false);
            }
        } // end method()

        public static bool LoadFromFile<T>(string sFile, ref T roSubj)
        {
            const string fn = "PersisterJson.SaveToFile()";
            try
            {
                string sjson = File.ReadAllText(sFile);
                roSubj = JsonConvert.DeserializeObject<T>(sjson);
                return (true);
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(PersisterJson), fn, exc);
                return (false);
            }

        }
    } // end class
}
