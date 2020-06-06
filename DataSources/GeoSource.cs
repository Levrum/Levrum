using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using GeoJSON.Net.CoordinateReferenceSystem;
using GFeature = GeoJSON.Net.Feature.Feature;
using GFeatureCollection = GeoJSON.Net.Feature.FeatureCollection;
using GPolygon = GeoJSON.Net.Geometry.Polygon;
using GMultiPolygon = GeoJSON.Net.Geometry.MultiPolygon;

using Newtonsoft.Json;

using Levrum.Data.Classes;

using Levrum.Utils;
using Levrum.Utils.Geography;
using Levrum.Utils.Geometry;

namespace Levrum.Data.Sources
{
    public class GeoSource : IDataSource
    {
        public string Name { get; set; } = string.Empty;
        public DataSourceType Type { get; } = DataSourceType.GeoSource;

        [JsonIgnore]
        public string Info { get { return string.Format("Geo Source '{0}': {1}", Name, Parameters["File"]); } }

        public string IDColumn { get; set; }
        public string ResponseIDColumn { get; set; }

        private FileInfo s_file = null;

        public string ErrorMessage {  get { return ("Error messages not implemented for type GeoSource"); } }

        [JsonIgnore]
        public FileInfo GeoFile
        {
            get
            {
                if (s_file == null && (Parameters.ContainsKey("File") && !string.IsNullOrWhiteSpace(Parameters["File"])))
                {
                    s_file = new FileInfo(Parameters["File"]);
                }
                return s_file;
            }
            set
            {
                s_file = value;
                Parameters["File"] = s_file.FullName;
            }
        }

        static readonly string[] s_requiredParameters = new string[] { "File" };

        [JsonIgnore]
        public List<string> RequiredParameters { get { return new List<string>(s_requiredParameters); } }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        private CoordinateConverter m_converter = null;
        private string m_projection = string.Empty;
        private string m_projectionName = string.Empty;


        [JsonIgnore]
        public CoordinateConverter Converter
        {
            get
            {
                if (m_converter == null)
                {
                    string projection;
                    if (Parameters.TryGetValue("Projection", out projection))
                    {
                        m_converter = new CoordinateConverter(projection);
                    }
                }
                return m_converter;
            }
            set
            {
                m_converter = value;
            }
        }

        public bool Connect()
        {
            return true;
        }

        public void Disconnect()
        {

        }

        private HashSet<string> m_columns = null;

        public List<string> GetColumns()
        {
            if (m_columns != null)
            {
                return m_columns.ToList();
            }

            List<AnnotatedObject<Geometry>> geoms = GetGeomsFromFile();
            m_columns = new HashSet<string>();
            foreach (AnnotatedObject<Geometry> geom in geoms)
            {
                foreach (string key in geom.Data.Keys)
                {
                    m_columns.Add(key);
                }
            }

            return m_columns.ToList();
        }

        public List<string> GetColumnValues(string column)
        {
            return new List<string>();
        }

        public List<Record> GetRecords()
        {
            return new List<Record>();
        }

        public void Dispose()
        {

        }

        public object Clone()
        {
            GeoSource clone = new GeoSource();
            clone.Name = Name;
            foreach (KeyValuePair<string, string> kvp in Parameters)
            {
                clone.Parameters.Add(kvp.Key, kvp.Value);
            }
            return clone;
        }

        public Dictionary<string, object> GetPropertiesForLatLon(double latitude, double longitude)
        {
            return GetPropertiesForLatLon(new LatitudeLongitude(latitude, longitude));
        }

        public Dictionary<string, object> GetPropertiesForLatLon(LatitudeLongitude latLon)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();

            List<AnnotatedObject<ComplexPolygon>> cPolys = GetComplexPolysFromFile();
            double[] xyPoint = Converter.ConvertLatLonToXY(latLon);
            ComplexPolygon lastContainingPoly = null;
            foreach (AnnotatedObject<ComplexPolygon> cPoly in cPolys)
            {
                if (cPoly.Object.Contains(xyPoint[0], xyPoint[1]))
                {
                    foreach (KeyValuePair<string, object> kvp in cPoly.Data)
                    {
                        if (output.ContainsKey(kvp.Key))
                        {
                            LogHelper.LogMessage(LogLevel.Warn, string.Format("Discarding data {0}={1} at coordinates {2},{3}", kvp.Key, output[kvp.Key], latLon.Latitude, latLon.Longitude));
                        }
                        else
                        {
                            output[kvp.Key] = kvp.Value;
                        }
                    }
                    lastContainingPoly = cPoly.Object;
                }
            }

            return output;
        }

        private void rationalizeCoordinate(Coordinate coord)
        {
            long maxPrecision = 10000000000;
            long value = (long)(coord[0] * maxPrecision);
            coord[0] = (double)value / maxPrecision;
            value = (long)(coord[1] * maxPrecision);
            coord[1] = (double)value / maxPrecision;
        }

        private List<AnnotatedObject<Geometry>> m_annotatedGeoms = null;

        public void Load()
        {
            string str1, str2;
            GetProjectionFromFile(out str1, out str2);
            GetComplexPolysFromFile();
        }

        public bool GetProjectionFromFile(out string projectionName, out string projection)
        {
            bool result = true;
            if (m_projection == string.Empty || m_projectionName == string.Empty)
            {
                result = GetProjectionFromFile(GeoFile.FullName, out m_projectionName, out m_projection);
            }

            projectionName = m_projectionName;
            projection = m_projection;
            return result;
        }

        public static bool GetProjectionFromFile(string fileName, out string projectionName, out string projection)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            try
            {
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(fileName);
                }
                else if (fileInfo.Extension == ".shp" || fileInfo.Extension == ".zip")
                {
                    return GetProjectionFromShpFile(fileName, out projectionName, out projection);
                }
                else if (fileInfo.Extension == ".geojson")
                {
                    return GetProjectionFromGeoJson(fileName, out projectionName, out projection);
                }
                else
                {
                    throw new NotImplementedException(string.Format("Unable to parse file '{0}': invalid extension {1}", fileInfo.FullName, fileInfo.Extension));
                }
            }
            catch (Exception ex)
            {
                projectionName = string.Empty;
                projection = string.Empty;
                return false;
            }
        }

        public static bool GetProjectionFromShpFile(string fileName, out string projectionName, out string projection)
        {
            FileInfo shpFile = new FileInfo(fileName);
            projection = string.Empty;
            projectionName = string.Empty;
            try
            {
                if (shpFile.Extension == ".zip")
                {
                    DirectoryInfo tempDir = new DirectoryInfo(string.Format("{0}\\Levrum\\Temp\\{1}",
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        shpFile.Name));

                    ZipFile.ExtractToDirectory(fileName, tempDir.FullName);
                    FileInfo[] prjFiles = tempDir.GetFiles("*.prj");
                    if (prjFiles.Length > 0)
                    {
                        projection = File.ReadAllText(prjFiles[0].FullName);
                    }
                    tempDir.Delete(true);
                }
                else
                {
                    FileInfo prjFile = new FileInfo(string.Format("{0}.prj", fileName.Substring(0, fileName.Length - shpFile.Extension.Length)));
                    if (prjFile.Exists)
                    {
                        projection = File.ReadAllText(prjFile.FullName);
                    }
                }
                if (projection != null)
                {
                    projectionName = CoordinateConverter.GetProjectionName(projection);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public static bool GetProjectionFromGeoJson(string fileName, out string projectionName, out string projection)
        {
            string geoJson = File.ReadAllText(fileName);
            ProtoGeoJSON obj = JsonConvert.DeserializeObject<ProtoGeoJSON>(geoJson);
            projectionName = string.Empty;
            projection = string.Empty;
            ICRSObject crs = null;
            switch (obj.Type)
            {
                case "FeatureCollection":
                    GFeatureCollection features = JsonConvert.DeserializeObject<GFeatureCollection>(geoJson);
                    crs = features.CRS;
                    break;
                case "Feature":
                    GFeature feature = JsonConvert.DeserializeObject<GFeature>(geoJson);
                    crs = feature.CRS;
                    break;
                case "MultiPolygon":
                    GMultiPolygon multiPoly = JsonConvert.DeserializeObject<GMultiPolygon>(geoJson);
                    crs = multiPoly.CRS;
                    break;
                case "Polygon":
                    GPolygon poly = JsonConvert.DeserializeObject<GPolygon>(geoJson);
                    crs = poly.CRS;
                    break;
                default:
                    break;
            }

            if (crs is NamedCRS)
            {
                NamedCRS nCrs = crs as NamedCRS;
                object value;
                if (nCrs.Properties.TryGetValue("name", out value))
                {
                    string name = value as string;
                    if (name != null)
                    {
                        name = name.ToLower();
                        if (name.EndsWith("3857"))
                        {
                            projectionName = "EPSG:3857 Pseudo-Mercator";
                            projection = CoordinateConverter.WebMercator.WKT;
                            return true;
                        }
                        else
                        {
                            int index = name.IndexOf("epsg::");
                            string epsgStr = name.Substring(index);
                            string authNumber = epsgStr.Substring(epsgStr.IndexOf("::") + 2);
                            projection = AutoProjection.GetProjection(authNumber);
                            if (projection != string.Empty)
                            {
                                projectionName = string.Format("EPSG:{0}", authNumber);
                                return true;
                            }
                        }
                    }
                }
            }

            projectionName = "EPSG:4326 Lat-Long";
            projection = CoordinateConverter.WGS84.WKT;
            return true;
        }

        List<AnnotatedObject<ComplexPolygon>> m_complexPolygons = null;
        AnnotatedComplexPolygonComparer m_comparer = new AnnotatedComplexPolygonComparer();

        private class AnnotatedComplexPolygonComparer : IComparer<AnnotatedObject<ComplexPolygon>>
        {
            public int Compare(AnnotatedObject<ComplexPolygon> p1, AnnotatedObject<ComplexPolygon> p2)
            {
                if (p1.Object.Area == p2.Object.Area)
                    return 0;
                if (p1.Object.Area < p2.Object.Area)
                    return -1;

                return 1;
            }
        }

        public List<AnnotatedObject<ComplexPolygon>> GetComplexPolysFromFile()
        {
            if (m_complexPolygons != null)
            {
                return m_complexPolygons;
            }
            List<AnnotatedObject<ComplexPolygon>> output = new List<AnnotatedObject<ComplexPolygon>>();
            
            List<AnnotatedObject<Geometry>> geoms = GetGeomsFromFile();
            foreach (AnnotatedObject<Geometry> geom in geoms)
            {
                AnnotatedObject<ComplexPolygon> poly = new AnnotatedObject<ComplexPolygon>(GetComplexPolygonFromGeometry(geom.Object));
                foreach(KeyValuePair<string, object> kvp in geom.Data)
                {
                    poly.Data.Add(kvp.Key, kvp.Value);
                }
                poly.Object.ComputeArea();
                output.Add(poly);
            }

            output.Sort(m_comparer);

            return m_complexPolygons = output;
        }

        public ComplexPolygon GetComplexPolygonFromGeometry(Geometry geom)
        {
            ComplexPolygon output = new ComplexPolygon();
            if (geom is NetTopologySuite.Geometries.Polygon)
            {
                NetTopologySuite.Geometries.Polygon p = geom as NetTopologySuite.Geometries.Polygon;
                if (p.ExteriorRing == null)
                {
                    foreach (Coordinate coord in p.Coordinates)
                    {
                        output.Polygon.AddPoint(coord.X, coord.Y);
                    }
                } else
                {
                    foreach (Coordinate coord in p.ExteriorRing.Coordinates)
                    {
                        output.Polygon.AddPoint(coord.X, coord.Y);
                    }
                }
                
                foreach (LineString lstr in p.InteriorRings)
                {
                    Utils.Geometry.Polygon poly = new Utils.Geometry.Polygon();
                    foreach (Coordinate coord in lstr.Coordinates)
                    {
                        poly.AddPoint(coord.X, coord.Y);
                    }
                    output.SubPolygons.Add(poly);
                }
            } else if (geom is MultiPolygon)
            {
                MultiPolygon mp = geom as MultiPolygon;
                int geomIndex = 0;
                foreach (Geometry subGeom in mp.Geometries)
                {
                    ComplexPolygon cp = GetComplexPolygonFromGeometry(subGeom);
                    if (geomIndex == 0)
                    {
                        output.Polygon = cp.Polygon;
                        output.SubPolygons.AddRange(cp.SubPolygons);
                    } else
                    {
                        output.SubPolygons.AddRange(cp.Polygons);
                    }
                    geomIndex++;
                }
            }

            return output;
        }

        public List<AnnotatedObject<Geometry>> GetGeomsFromFile()
        {
            if (m_annotatedGeoms != null)
                return m_annotatedGeoms;
            if (GeoFile == null)
            {
                return m_annotatedGeoms = new List<AnnotatedObject<Geometry>>();
            }
            else if (GeoFile.Extension == ".shp" || GeoFile.Extension == ".zip")
            {
                return m_annotatedGeoms = GetGeomsFromShpFile(GeoFile.FullName).ToList();
            }
            else if (GeoFile.Extension == ".geojson")
            {
                string geoJson = File.ReadAllText(GeoFile.FullName);
                return m_annotatedGeoms = GetGeomsFromGeoJson(geoJson).ToList();
            }
            else
            {
                throw new NotImplementedException(string.Format("Unable to parse file '{0}': invalid extension {1}", GeoFile.FullName, GeoFile.Extension));
            }
        }

        public static AnnotatedObject<Geometry>[] GetGeomsFromShpFile(string fileName)
        {
            List<AnnotatedObject<Geometry>> output = new List<AnnotatedObject<Geometry>>();
            FileInfo file = new FileInfo(fileName);
            if (file.Extension == ".zip")
            {
                DirectoryInfo tempDir = new DirectoryInfo(string.Format("{0}\\Levrum\\Temp\\{1}",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    file.Name));

                ZipFile.ExtractToDirectory(fileName, tempDir.FullName);
                FileInfo[] shpFiles = tempDir.GetFiles("*.shp");
                foreach (FileInfo shpFile in shpFiles)
                {
                    output.AddRange(getGeomsFromShpFile(shpFile.FullName));
                }
                tempDir.Delete(true);
            }
            else
            {
                output.AddRange(getGeomsFromShpFile(fileName));
            }

            return output.ToArray();
        }

        private static AnnotatedObject<Geometry>[] getGeomsFromShpFile(string fileName)
        {
            List<AnnotatedObject<Geometry>> output = new List<AnnotatedObject<Geometry>>();
            using (var reader = new ShapefileDataReader(fileName, GeometryFactory.Default))
            {
                while (reader.Read())
                {
                    DbaseFileHeader h = reader.DbaseHeader;
                    int fieldCount = h.NumFields;
                    var geom = reader.Geometry;
                    geom.Normalize();
                    var obj = new AnnotatedObject<Geometry>(geom);

                    for (int i = 1; i <= fieldCount; i++)
                    {
                        try
                        {
                            obj.Data[h.Fields[i - 1].Name] = reader.GetValue(i);
                        }
                        catch
                        {

                        }
                    }

                    output.Add(obj);
                }
            }

            return output.ToArray();
        }

        public static AnnotatedObject<Geometry>[] GetGeomsFromGeoJson(string geoJson)
        {
            List<AnnotatedObject<Geometry>> output = new List<AnnotatedObject<Geometry>>();
            GeoJsonReader reader = new GeoJsonReader(GeometryFactory.Default, new JsonSerializerSettings());
            ProtoGeoJSON obj = reader.Read<ProtoGeoJSON>(geoJson);
            AnnotatedObject<Geometry> annotatedGeom = new AnnotatedObject<Geometry>(null);
            Geometry geom;
            bool isFeatureCollection = false;
            switch (obj.Type)
            {
                case "Point":
                    annotatedGeom.Object = reader.Read<Point>(geoJson);
                    break;
                case "LineString":
                    annotatedGeom.Object = reader.Read<LineString>(geoJson);
                    break;
                case "Polygon":
                    annotatedGeom.Object = reader.Read<NetTopologySuite.Geometries.Polygon>(geoJson);
                    break;
                case "MultiPoint":
                    annotatedGeom.Object = reader.Read<MultiPoint>(geoJson);
                    break;
                case "MultiLineString":
                    annotatedGeom.Object = reader.Read<MultiLineString>(geoJson);
                    break;
                case "MultiPolygon":
                    annotatedGeom.Object = reader.Read<MultiPolygon>(geoJson);
                    break;
                case "Feature":
                    Feature feature = reader.Read<Feature>(geoJson);
                    annotatedGeom.Object = feature.Geometry;
                    foreach (string attribute in feature.Attributes.GetNames())
                    {
                        annotatedGeom.Data[attribute] = feature.Attributes[attribute];
                    }
                    break;
                case "FeatureCollection":
                    FeatureCollection collection = reader.Read<FeatureCollection>(geoJson);
                    foreach (Feature item in collection)
                    {
                        annotatedGeom = new AnnotatedObject<Geometry>(item.Geometry);
                        annotatedGeom.Object.Normalize();
                        foreach (string attribute in item.Attributes.GetNames())
                        {
                            annotatedGeom.Data[attribute] = item.Attributes[attribute];
                        }
                        output.Add(annotatedGeom);
                    }
                    isFeatureCollection = true;
                    break;
                default:
                    throw new NotImplementedException(string.Format("No handler found for type {0}", obj.Type));
            }

            if (!isFeatureCollection)
            {
                annotatedGeom.Object.Normalize();
                output.Add(annotatedGeom);
            }

            return output.ToArray();
        }
    }
}
