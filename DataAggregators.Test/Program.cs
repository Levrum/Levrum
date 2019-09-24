using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Levrum.DataClasses;

namespace Levrum.DataAggregators.Test
{
    class Program : ConsoleApp
    {
        static Program s_program;

        public static Program Instance { get { return s_program; } }

        static Program()
        {
            s_program = new Program();
            s_program.CommandNamespaces.Add("Levrum.DataAggregators.Test.Commands");
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

namespace Levrum.DataAggregators.Test.Commands
{
    public static class ConsoleCommands
    {
        public static string quit(List<string> args)
        {
            Program.Instance.End();
            return "Bye!";
        }

        public static string runtest(List<string> args)
        {
            Test test = new Test();
            test.BuildDemoData();
            test.RunAggregatorTests();

            return "";
        }

        public static string runtesttwo(List<string> args)
        {
            Test test = new Test();
            test.BuildDemoData();
            test.RunComboAggregatorTest();

            return "";
        }

        public static string runtimeseries(List<string> args)
        {
            TimeSeriesExampleOne test = new TimeSeriesExampleOne();
            test.BuildDemoData();

            return JsonConvert.SerializeObject(test.RunAggregatorTest());
        }

        public static string runtimeseriestwo(List<string> args)
        {
            TimeSeriesExampleOne test = new TimeSeriesExampleOne();
            test.BuildDemoData();
            List<AggregatedData<IncidentData>> data = (List<AggregatedData<IncidentData>>)test.RunGetAggregatedDataTest();
            data.Sort();
            return JsonConvert.SerializeObject(data);
        }

        public static string runtimeseriesthree(List<string> args)
        {
            TimeSeriesExampleOne test = new TimeSeriesExampleOne();
            test.BuildDemoData();
            var data = test.RunGroupByTest();
            return JsonConvert.SerializeObject(data);
        }
    }
}