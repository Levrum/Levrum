using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Levrum.DataClasses;

namespace Levrum.DataClasses.Test
{
    class Program : ConsoleApp
    {
        static Program s_program;

        public static Program Instance { get { return s_program; } }

        static Program()
        {
            s_program = new Program();
            s_program.CommandNamespaces.Add("Levrum.DataClasses.Test.Commands");
            s_program.LoadCommands();
        }

        static void Main(string[] args)
        {
            string inputStr = "";

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                        inputStr = inputStr + " ";

                    inputStr = inputStr + args[i];
                }
            }

            s_program.Run(inputStr);
        }
    }
}

namespace Levrum.DataClasses.Test.Commands
{
    public static class ConsoleCommands
    {
        static DataSet<IncidentData> incidents;

        public static string quit(List<string> args)
        {
            Program.Instance.End();
            return "Bye!";
        }

        public static string load(List<string> args)
        {
            GC.Collect();
            try
            {
                string jsonText = File.ReadAllText(args[0]);
                incidents = JsonConvert.DeserializeObject<DataSet<IncidentData>>(jsonText);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            foreach (IncidentData incident in incidents)
            {
                incident.Intern();
            }
            GC.Collect();
            return string.Format("Load complete. Loaded {0} incidents. Press enter to exit function.", incidents.Count);
        }

        public static string collect(List<string> args)
        {
            GC.Collect();
            return "Pay the man.";
        }
    }
}
