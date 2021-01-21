using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using Levrum.Data.Classes;
using Levrum.Data.Sources;
using Levrum.Data.Map;

using Levrum.Utils.Messaging;
using Levrum.Utils.Geography;

using Levrum.Utils.Osm;
using Levrum.Utils.Geometry;

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

        public static string quack(List<string> args)
        {
            try
            {
                double lat = Convert.ToDouble(args[0]);
                double lon = Convert.ToDouble(args[1]);
                
                if(args.Count > 2)
                {
                    string unit = args[2];
                    string projection = AutoProjection.GetProjection(lat, lon, unit);
                    return projection;
                }
                else
                {
                    string projection = AutoProjection.GetProjection(lat, lon);
                    return projection;
                }
            }
            catch
            {
                return "What the quack? You didn't quack right. Enter 'quack latitude longitude unit(optional)'. QUACK!";
            }
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

        private static RabbitMQProducer<IncidentData> producer;
        private static RabbitMQConsumer<IncidentData> consumer;

        public static string rmqprod(List<string> args)
        {
            try
            {
                producer = new RabbitMQProducer<IncidentData>("localhost", 5672, "test");
                producer.Connect();
                return "Producer Connected";
            } catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string rmqsend(List<string> args)
        {
            if (args.Count == 0)
            {
                return "rmqsend requires 1 argument";
            }

            producer.Connect();
            IncidentData data = new IncidentData();
            data.Data["Message"] = args[0];
            producer.SendObject(data);

            return "Message sent";
        }

        public static string rmqconsume(List<string> args)
        {
            try
            {
                consumer = new RabbitMQConsumer<IncidentData>("localhost", 5672, "test");
                consumer.Connect();
                consumer.MessageReceived += onReceive;
                return "Consumer Connected";
            } catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static void onReceive(object sender, string message, IncidentData obj)
        {
            Console.WriteLine(string.Format("Received Message: {0} Object: {1}", message, obj));
        }

        public static string getproj(List<string> args)
        {
            if (args.Count < 0)
            {
                return "getproj requires at least 1 argument";
            }

            string authCode = args[0];
            string projection = AutoProjection.GetProjection(authCode);

            return projection;
        }

        public static OsmFile OsmFile { get; set; } = null;
        public static string loadosm(List<string> args)
        {
            if (args.Count == 0)
                return "Usage: loadosm <filename>";

            FileInfo file = new FileInfo(args[0]);
            if (!file.Exists)
                return string.Format("File {0} does not exist!", args[0]);

            DateTime loadStart = DateTime.Now;
            OsmFile = new OsmFile(file);
            OsmFile.Load(true, true);
            DateTime loadEnd = DateTime.Now;

            return string.Format("Loaded {0} nodes, {1} ways, and {2} relations from {3} in {4} seconds.", OsmFile.Nodes.Count, OsmFile.Ways.Count, OsmFile.Relations.Count, file.Name, (loadEnd - loadStart).TotalSeconds);
        }

        public static string saveosm(List<string> args)
        {
            if (args.Count == 0)
                return "Usage: saveosm <filename>";

            if (OsmFile == null)
                return "You must first load an OSM file with the loadosm command.";

            DateTime saveStart = DateTime.Now;
            OsmFile.Save(args[0]);
            DateTime saveEnd = DateTime.Now;

            return string.Format("Saved {0} nodes, {1} ways, and {2} relations to {3} in {4} seconds.", OsmFile.Nodes.Count, OsmFile.Ways.Count, OsmFile.Relations.Count, args[0], (saveEnd - saveStart).TotalSeconds);
        }

        public static string makeintersections(List<string> args)
        {
            if (OsmFile == null)
                return "You must first load an osm file with the loadosm command.";

            DateTime startTime = DateTime.Now;
            OsmFile.GenerateIntersections();
            DateTime endTime = DateTime.Now;

            return string.Format("Generated {0} intersections in {1} seconds.", OsmFile.Intersections.Count, (endTime - startTime).TotalSeconds);
        }

        public static string saveintersections(List<string> args)
        {
            if (OsmFile == null)
                return "You must first load an osm file with the loadosm command and run 'makeintersections'.";

            if (OsmFile.IntersectionsGenerated == false)
                return "You must first run 'makeintersections'.";

            if (args.Count != 1)
            {
                return "Usage: saveintersections <filename>";
            }

            if (File.Exists(args[0]))
            {
                File.Delete(args[0]);
            }

            using (StreamWriter sw = new StreamWriter(File.OpenWrite(args[0])))
            {
                sw.WriteLine("IntersectionID,Latitude,Longitude,ConnectedIntersection_Distance");
                foreach (var intersection in OsmFile.Intersections.Values)
                {
                    string lineToWrite = string.Format("{0},{1},{2},", intersection.ID, intersection.Latitude, intersection.Longitude);
                    bool firstConnection = true;
                    foreach (var connectedIntersection in intersection.ConnectedIntersectionDistances)
                    {
                        if (firstConnection)
                        {
                            firstConnection = false;
                            lineToWrite = string.Format("{0}{1}_{2}", lineToWrite, connectedIntersection.Key, connectedIntersection.Value);
                        } else
                        {
                            lineToWrite = string.Format("{0}#{1}_{2}", lineToWrite, connectedIntersection.Key, connectedIntersection.Value);
                        }
                    }
                    sw.WriteLine(lineToWrite);
                }
                //JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
                //serializer.Serialize(sw, OsmFile.Intersections);   
            }

            return string.Format("Saved {0} intersections to {1} in CSV format.", OsmFile.Intersections.Count, args[0]);
        }

        public static string addroadcut(List<string> args)
        {
            if (OsmFile == null)
                return "You must first load an osm file with the loadosm command.";

            double x1, y1, x2, y2;
            if (args.Count != 4 || !double.TryParse(args[0], out x1) || !double.TryParse(args[1], out y1) || !double.TryParse(args[2], out x2) || !double.TryParse(args[3], out y2))
            {
                return "Usage: addroadcut <x1> <y1> <x2> <y2>";
            }

            LatitudeLongitude a = new LatitudeLongitude(y1, x1);
            LatitudeLongitude b = new LatitudeLongitude(y2, x2);

            if (!OsmFile.SplitWaysByLine(a, b))
            {
                return "Unable to split way!";
            }

            return "Ways split successfully!";
        }

        static string s_testjson = "{  \"$id\": \"1\",  \"Parent\": null,  \"Contents\": {    \"$id\": \"2\",    \"$values\": [      {        \"$id\": \"3\",        \"Time\": \"2021-01-19T23:03:40.088235-06:00\",        \"Location\": \"\",        \"Longitude\": \"NaN\",        \"Latitude\": \"NaN\",        \"Responses\": {          \"$id\": \"4\",          \"Parent\": {            \"$ref\": \"3\"          },          \"Contents\": {            \"$id\": \"5\",            \"$values\": [              {                \"$id\": \"6\",                \"Id\": \"Moo\",                \"Benchmarks\": {                  \"$id\": \"7\",                  \"Parent\": {                    \"$ref\": \"6\"                  },                  \"Contents\": {                    \"$id\": \"8\",                    \"$values\": []                  },                  \"Data\": {                    \"$id\": \"9\"                  }                },                \"Data\": {                  \"Id\": \"Moo\",                  \"Unit\": \"Also Moo\",                  \"UnitType\": \"Cow\",                  \"Shift\": \"Night Shift\",                  \"Benchmarks\": {                   \"$ref\": \"7\"                  }                }              }            ]          },          \"Data\": {            \"$id\": \"10\"          }        },        \"Data\": {          \"Id\": \"Moo\",          \"Time\": \"2021-01-19T23:03:40.088235-06:00\",          \"Responses\": {            \"$ref\": \"4\"          }        }      }    ]  },  \"Data\": {    \"$id\": \"11\",    \"Test\": \"Test\"  }}";

        public static string testserialize(List<string> args)
        {
            DataSet<IncidentData> test = new DataSet<IncidentData>();

            test.Data["Test"] = "Test";

            IncidentData incident = new IncidentData();
            incident.Id = "Moo";
            incident.Time = DateTime.Now;

            ResponseData response = new ResponseData();
            response.Id = "Moo";
            response.Data.Add("Unit", "Also Moo");
            response.Data.Add("UnitType", "Cow");
            response.Data.Add("Shift", "Night Shift");
            incident.Responses.Add(response);

            test.Add(incident);

            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, PreserveReferencesHandling = PreserveReferencesHandling.All };

            string json = JsonConvert.SerializeObject(test, test.GetType(), settings);
            Console.WriteLine(json);

            return "Test passed.";
        }

        public static string tests016(List<string> args)
        {
            DataSet016<IncidentData016> test = new DataSet016<IncidentData016>();

            IncidentData016 incident = new IncidentData016();
            incident.Id = "d479dd53-77c7-4440-b8c6-ac4001435f9a";
            incident.Time = DateTime.Now;
            incident.Longitude = 0.0;
            incident.Latitude = 0.0;
            incident.Location = string.Empty;

            ResponseData016 response = new ResponseData016();
            response.Id = "Moo";
            response.Data.Add("Unit", "Also Moo");
            response.Data.Add("UnitType", "Cow");
            response.Data.Add("Shift", "Night Shift");
            incident.Responses.Add(response);

            test.Add(incident);

            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Auto, PreserveReferencesHandling = PreserveReferencesHandling.Objects };// PreserveReferencesHandling = PreserveReferencesHandling.All };

            string json = JsonConvert.SerializeObject(test, settings);
            Console.WriteLine(json);

            json = JsonConvert.SerializeObject(incident, settings);
            Console.WriteLine(json);

            return "Test passed.";
        }

        public static string testds016(List<string> args)
        {
            string json = "[  {    \"$id\": \"1\",    \"Time\": \"2021-01-20T21:55:41.5579044-06:00\",    \"Location\": \"\",    \"Longitude\": 0.0,    \"Latitude\": 0.0,    \"Responses\": [      {        \"$id\": \"2\",        \"Id\": \"Moo\",        \"Benchmarks\": [],        \"Data\": {          \"Id\": \"Moo\",          \"Unit\": \"Also Moo\",          \"UnitType\": \"Cow\",          \"Shift\": \"Night Shift\",          \"Benchmarks\": {            \"$type\": \"Levrum.Data.Classes.DataSet016`1[[Levrum.Data.Classes.TimingData, Levrum.Data.Classes]], Levrum.Data.Classes\",            \"$values\": []          }        }      }    ],    \"Data\": {      \"Id\": \"d479dd53-77c7-4440-b8c6-ac4001435f9a\",      \"Time\": \"2021-01-20T21:55:41.5579044-06:00\",      \"Longitude\": 0.0,      \"Latitude\": 0.0,      \"Location\": \"\",      \"Responses\": {        \"$type\": \"Levrum.Data.Classes.DataSet016`1[[Levrum.Data.Classes.ResponseData016, Levrum.Data.Classes]], Levrum.Data.Classes\",        \"$values\": [          {            \"$ref\": \"2\"          }        ]      }    }  }]";
            DataSet016<IncidentData016> test = JsonConvert.DeserializeObject<DataSet016<IncidentData016>>(json);

            json = "{  \"$id\": \"1\",  \"Time\": \"2021-01-20T16:14:49.6769059-06:00\",  \"Location\": \"\",  \"Longitude\": 0.0,  \"Latitude\": 0.0,  \"Responses\": [],  \"Data\": {    \"Id\": \"d479dd53-77c7-4440-b8c6-ac4001435f9a\",    \"Time\": \"2021-01-20T16:14:49.6769059-06:00\",    \"Longitude\": 0.0,    \"Latitude\": 0.0,    \"Location\": \"\",    \"Responses\": {      \"$type\": \"Levrum.Data.Classes.DataSet016`1[[Levrum.Data.Classes.ResponseData016, Levrum.Data.Classes]], Levrum.Data.Classes\",      \"$values\": []    }  }}";
            IncidentData016 test2 = JsonConvert.DeserializeObject<IncidentData016>(json);

            return "Test passed.";
        }

        public static string testds(List<string> args)
        {
            if (args.Count == 0)
            {
                DataSet<IncidentData> test = JsonConvert.DeserializeObject<DataSet<IncidentData>>(s_testjson);

                return "Test passed.";
            } else
            {
                DataSet<IncidentData> test = DataSet<IncidentData>.Deserialize(args[0]);

                return "Test passed.";
            }
        }
    }
}