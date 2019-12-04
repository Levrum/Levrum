﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Sources;
using Levrum.Data.Map;

namespace Sandbox
{
    public static class Program
    {
        public static ConsoleApp App { get; set; }

        static void Main(string[] args)
        {
            App = new ConsoleApp();
            App.CommandNamespaces.Add("Sandbox.DefaultCommands");

            App.LoadCommands();

            if (args.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
            } else
            {
                App.Run();
            }

        }
    }
}

namespace Sandbox.DefaultCommands
{
    public static class ConsoleCommands
    {
        public static string quit(List<string> args)
        {
            Program.App.End();

            return "Goodbye!";
        }

        public static string weirdloop(List<string> args)
        {
            for (int i = 1; i < 10; i++)
            {
                Console.WriteLine("Moo");
            }

            return "";
        }

        public static string maploader(List<string> args)
        {
            if (args.Count < 1)
            {
                return "Usage: maploader <datamap path>";
            }

            FileInfo file = new FileInfo(args[0]);
            if (!file.Exists)
            {
                return string.Format("File not found: {0}", args[0]);
            }

            try
            {
                DataMap map = JsonConvert.DeserializeObject<DataMap>(File.ReadAllText(args[0]));

                MapLoader loader = new MapLoader();
                loader.LoadMap(map);
                Console.WriteLine("Loaded {0} incidents", loader.Incidents.Count);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.PreserveReferencesHandling = PreserveReferencesHandling.All;
                settings.Formatting = Formatting.Indented;
                string incidentJson = JsonConvert.SerializeObject(loader.Incidents, settings);

                if (args.Count > 1)
                {
                    File.WriteAllText(args[1], incidentJson);
                } else
                {
                    File.WriteAllText("incidents.json", incidentJson);
                }
            } catch (Exception ex)
            {
                return string.Format("Exception loading map: {0}\r\r\t{1}\r--------", ex.Message, ex.StackTrace);
            }

            return "Loading complete";
        }

        public static string recordtest(List<string> args)
        {
            try
            {
                Record record = new Record();
                record.AddValue("Test", null);
            } catch (Exception ex)
            {
                return "Test failed.";
            }
            return "Test passed.";
        }

        public static string causetest(List<string> args)
        {
            try
            {
                List<CauseData> causeDataList = new List<CauseData>();
                CauseData causeDataOne = new CauseData();
                causeDataOne.Name = "Test";
                causeDataOne.Description = "Testing";
                CauseData causeDataTwo = new CauseData();
                causeDataTwo.Name = "Test 2";
                causeDataTwo.Description = "Testing 2";
                causeDataOne.Children.Add(causeDataTwo);
                NatureCode code = new NatureCode();
                code.Description = "Code";
                code.Value = "Code";
                causeDataTwo.NatureCodes.Add(code);

                causeDataList.Add(causeDataOne);
                string json = JsonConvert.SerializeObject(causeDataList, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented, PreserveReferencesHandling = PreserveReferencesHandling.All });

                List<CauseData> output = JsonConvert.DeserializeObject<List<CauseData>>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented, PreserveReferencesHandling = PreserveReferencesHandling.All });
            } catch (Exception ex)
            {
                return "Test failed.";
            }
            return "Test passed.";
        }
    }
}