﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

using GeoJSON.Net.Geometry;
using GeoJSONPolygon = GeoJSON.Net.Geometry.Polygon;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;

namespace Levrum.Utils.Geography
{
    public class AutoProjection
    {
        public static string GetProjection(double lat, double lon, string unit = "US survey foot")
        {
            //supports US survey foot, metre, and foot. Not sure what the diffence between US survey foot and foot is...
            string projection = "";

            if (!IsUnitSupported(unit))
            {
                return unit + " is not supported. Please try again with either US survey foot, metre, or foot(or nothing and it will default to US survey foot)";
            }

            if (StatePlaneOrLatLon(lat, lon) == "LatLon")
            {
                ExtractZipFiles();
                List<Projection> projectionList = GetSRID();
                string projName = GetProjectionName(lat, lon);
                if (projName == "")
                {
                    RemoveTempFiles();
                    return "Unable to find projection";
                }
                projection = FindMatchingProjection(projName, unit, projectionList);
                RemoveTempFiles();
            }
            else
            {
                return "Your coordinates appear to be state plane. That isn't supported right now but I have plans...";
            }

            return projection;
        }

        public static string StatePlaneOrLatLon(double yLat, double xLon)
        {
            if (xLon < 0 && yLat < 100 && Math.Abs(xLon + yLat) < 200)
            {
                return "LatLon";
            }
            else
            {
                return "StatePlane";
            }
        }

        private static bool IsUnitSupported(string unit)
        {
            unit = unit.ToLower();
            if(unit == "us survey foot" || unit == "metre" || unit == "foot")
            {
                return true;
            }

            return false;
        }

        private static void ExtractZipFiles()
        {
            string dir = Directory.GetCurrentDirectory();
            dir = Path.GetFullPath(Path.Combine(dir, @"..\..\..\..\"));
            try
            {
                if (File.Exists(dir + @"Utils\Geography\SRID\SRID.csv"))
                {
                    Directory.Delete(dir + @"Utils\Geography\SRID\", true);
                }
                ZipFile.ExtractToDirectory(dir + @"Utils\Geography\SRID.zip", dir + @"Utils\Geography\SRID\");
            }
            catch
            {
                if (File.Exists(dir + @"Utils\Geography\SRID\SRID.csv"))
                {
                    Directory.Delete(dir + @"Utils\Geography\SRID\", true);
                }
            }
        }

        private static void RemoveTempFiles()
        {
            string dir = Directory.GetCurrentDirectory();
            dir = Path.GetFullPath(Path.Combine(dir, @"..\..\..\..\"));
            if (File.Exists(dir + @"Utils\Geography\SRID\SRID.csv"))
            {
                Directory.Delete(dir + @"Utils\Geography\SRID\", true);
            }
        }

        private static List<Projection> GetSRID()
        {
            List<Projection> projectionList = new List<Projection>();
            string dir = Directory.GetCurrentDirectory();
            dir = Path.GetFullPath(Path.Combine(dir, @"..\..\..\..\"));
            try
            {
                using (StreamReader reader = new StreamReader(dir + @"Utils\Geography\SRID\SRID.csv"))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        Projection p = ProcessSRID(line.Split(';')[1]);
                        projectionList.Add(p);
                        line = reader.ReadLine();
                    }
                }

                return projectionList;
            }
            catch
            {
                return projectionList;
            }
        }

        private static string GetProjectionName(double latitude, double longitude)
        {
            List<StatePlanePolygon> polygons = LoadStatePlanePolygons();

            foreach (StatePlanePolygon p in polygons)
            {
                if (latitude >= p.minLat && latitude <= p.maxLat && longitude >= p.minLon && longitude <= p.maxLon)
                {
                    int intersections = 0;
                    StatePlaneNode previousNode = null;
                    foreach (StatePlaneNode n in p.Nodes)
                    {
                        if (previousNode != null && DoLinesIntersect(previousNode, n, latitude, longitude))
                        {
                            intersections++;
                        }
                        previousNode = n;
                    }
                    if (DoLinesIntersect(p.Nodes[0], p.Nodes.Last(), latitude, longitude))
                    {
                        intersections++;
                    }

                    if (intersections % 2 == 1)
                    {
                        return p.ID;
                    }
                }
            }
            return "";
        }

        private static List<StatePlanePolygon> LoadStatePlanePolygons()
        {
            List<StatePlanePolygon> polygons = new List<StatePlanePolygon>();
            string dir = Directory.GetCurrentDirectory();
            dir = Path.GetFullPath(Path.Combine(dir, @"..\..\..\..\"));

            using (StreamReader reader = new StreamReader(dir + @"Utils\Geography\SRID\StateProjectionShapes.geojson"))
            {
                string geoJSON = reader.ReadToEnd();
                int nodeCount = 0;

                FeatureCollection features = JsonConvert.DeserializeObject<FeatureCollection>(geoJSON);
                foreach (Feature subFeature in features.Features)
                {
                    if (subFeature.Geometry is MultiPolygon)
                    {
                        foreach (GeoJSONPolygon subPolygon in (subFeature.Geometry as MultiPolygon).Coordinates)
                        {
                            StatePlanePolygon statePlanePolygon = new StatePlanePolygon();
                            foreach (var property in subFeature.Properties)
                            {
                                if (property.Key == "ZONENAME")
                                {
                                    statePlanePolygon.ID = Convert.ToString(property.Value);
                                }
                            }
                            foreach (var coordinate in subPolygon.Coordinates)
                            {
                                foreach (var actualCoordinate in coordinate.Coordinates)
                                {
                                    nodeCount++;
                                    StatePlaneNode node = new StatePlaneNode(Convert.ToString(nodeCount), actualCoordinate.Latitude, actualCoordinate.Longitude);

                                    if (node.Latitude < statePlanePolygon.minLat)
                                    {
                                        statePlanePolygon.minLat = node.Latitude;
                                    }
                                    if (node.Latitude > statePlanePolygon.maxLat)
                                    {
                                        statePlanePolygon.maxLat = node.Latitude;
                                    }
                                    if (node.Longitude < statePlanePolygon.minLon)
                                    {
                                        statePlanePolygon.minLon = node.Longitude;
                                    }
                                    if (node.Longitude > statePlanePolygon.maxLon)
                                    {
                                        statePlanePolygon.maxLon = node.Longitude;
                                    }

                                    statePlanePolygon.Nodes.Add(node);
                                }
                            }
                            polygons.Add(statePlanePolygon);
                        }
                    }
                }
            }
            return polygons;
        }

        private static string FindMatchingProjection(string projName, string unit, List<Projection> projectionList)
        {
            unit = unit.ToLower();
            foreach (Projection proj in projectionList)
            {
                if (proj.Unit.ToLower() != unit)
                {
                    continue;
                }

                if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(proj.RawProjectionString, projName, CompareOptions.IgnoreCase) >= 0)
                {
                    return proj.RawProjectionString;
                }
            }
            return "Unable to find projection";
        }

        private static Projection ProcessSRID(string srid)
        {
            Projection p = new Projection();
            p.RawProjectionString = srid;
            Tuple<string, string> keyValueTuple = SeperateKeyAndValue(srid);
            string key = keyValueTuple.Item1;
            List<string> valueList = SeperateItems(keyValueTuple.Item2);

            foreach (string value in valueList.ToList())
            {
                if (value.Contains('['))
                {
                    keyValueTuple = SeperateKeyAndValue(value);

                    if (keyValueTuple.Item1 == "PARAMETER")
                    {
                        List<string> parameterValues = SeperateItems(keyValueTuple.Item2);
                        p.ParameterDict[parameterValues[0]] = parameterValues[1];
                    }
                    else if (keyValueTuple.Item1 == "UNIT")
                    {
                        List<string> unitValues = SeperateItems(keyValueTuple.Item2);
                        p.Unit = unitValues[0];
                    }
                }
            }
            return p;
        }

        private static Tuple<string, string> SeperateKeyAndValue(string layer)
        {
            string key = "";
            string value = "";
            int bracketCounter = 0;
            foreach (char c in layer)
            {
                if (c == '[')
                {
                    bracketCounter++;
                }
                else if (c == ']')
                {
                    bracketCounter--;
                }
                else if (bracketCounter == 0)
                {
                    key += c;
                }

                if (c != '"' && bracketCounter > 0)
                {
                    value += c;
                }
            }
            if (value.Length > 0 && value[0] == '[')
            {
                value = value.Remove(0, 1);
                //value = value.Remove(value.Length - 1);
            }
            return new Tuple<string, string>(key, value);
        }

        private static List<string> SeperateItems(string contents)
        {
            List<string> itemList = new List<string>();
            int bracketCounter = 0;
            string item = "";
            foreach (char c in contents)
            {
                if (c == '[')
                {
                    bracketCounter++;
                }
                else if (c == ']')
                {
                    bracketCounter--;
                }

                if (c == ',' && bracketCounter == 0)
                {
                    itemList.Add(item);
                    item = "";
                }
                else
                {
                    item += c;
                }
            }
            itemList.Add(item);
            return itemList;
        }

        private static double SolveM(StatePlaneNode v1, StatePlaneNode v2)
        {
            return (v2.Latitude - v1.Latitude) / (v2.Longitude - v1.Longitude);
        }

        private static double SolveB(StatePlaneNode v1, double m)
        {
            return v1.Latitude - (m * v1.Longitude);
        }

        private static bool DoLinesIntersect(StatePlaneNode v1, StatePlaneNode v2, double lat, double lon)
        {
            StatePlaneNode v3 = new StatePlaneNode("whatev", lat, lon);
            StatePlaneNode v4 = new StatePlaneNode("whatev", lat, double.MaxValue);
            double m = SolveM(v1, v2);
            double b = SolveB(v1, m);

            double m2 = SolveM(v3, v4);
            double b2 = SolveB(v3, m2);


            double x = (b2 - b) / (m - m2);
            double y = (m * x) + b;

            if (double.IsNaN(x) || double.IsNaN(y))
            {
                return false;
            }

            return (IsBetween(v1, v2, y, x) && IsBetween(v3, v4, y, x));
        }

        private static bool IsBetween(StatePlaneNode v1, StatePlaneNode v2, double lat, double lon)
        {
            //Handling perfectly straight in one direction lines...
            if (v1.Latitude == v2.Latitude)
            {
                if (v1.Longitude >= v2.Longitude)
                {
                    if (lon > v2.Longitude && lon < v1.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (lon < v2.Longitude && lon > v1.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            if (v1.Longitude == v2.Longitude)
            {
                if (v1.Latitude >= v2.Latitude)
                {
                    if (lat > v2.Latitude && lat < v1.Latitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (lat < v2.Latitude && lat > v1.Latitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }


            if (v1.Latitude > v2.Latitude)
            {
                if (v1.Longitude > v2.Longitude)
                {
                    if (lat < v1.Latitude && lat > v2.Latitude && lon > v2.Longitude && lon < v1.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (lat < v1.Latitude && lat > v2.Latitude && lon > v1.Longitude && lon < v2.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (v1.Longitude > v2.Longitude)
                {
                    if (lat < v2.Latitude && lat > v1.Latitude && lon > v2.Longitude && lon < v1.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (lat < v2.Latitude && lat > v1.Latitude && lon > v1.Longitude && lon < v2.Longitude)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
    class Projection
    {
        public string Unit { get; set; }
        public Dictionary<string, string> ParameterDict { get; set; } = new Dictionary<string, string>();
        public string RawProjectionString { get; set; }
    }

    class StatePlaneNode
    {
        public string ID { get; }
        public double Latitude { get; }
        public double Longitude { get; }

        public StatePlaneNode(string id, double lat, double lon)
        {
            this.ID = id;
            this.Latitude = lat;
            this.Longitude = lon;
        }
    }

    class StatePlanePolygon
    {
        public string ID { get; set; }
        public List<StatePlaneNode> Nodes { get; set; } = new List<StatePlaneNode>();
        public double minLat { get; set; } = double.MinValue;
        public double maxLat { get; set; } = double.MaxValue;
        public double minLon { get; set; } = double.MinValue;
        public double maxLon { get; set; } = double.MaxValue;
    }
}