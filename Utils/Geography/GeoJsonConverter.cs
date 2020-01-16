using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Utils.Geometry;
using LevrumPolygon = Levrum.Utils.Geometry.Polygon;

using GeoJSON.Net.Geometry;
using GeoJSONPolygon = GeoJSON.Net.Geometry.Polygon;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;


namespace Levrum.Utils.Geography
{
    public class GeoJsonConverter
    {
        public static LevrumPolygon[] ConvertRegionToPolygons(string geoJSON)
        {
            List<LevrumPolygon> output = new List<LevrumPolygon>();

            try
            {
                ProtoGeoJSON obj = JsonConvert.DeserializeObject<ProtoGeoJSON>(geoJSON);

                switch (obj.Type)
                {
                    case "FeatureCollection":
                        FeatureCollection features = JsonConvert.DeserializeObject<FeatureCollection>(geoJSON);
                        foreach (Feature subFeature in features.Features)
                        {
                            output.AddRange(getLevrumPolygonsFromFeature(subFeature));
                        }
                        break;
                    case "Feature":
                        Feature feature = JsonConvert.DeserializeObject<Feature>(geoJSON);
                        output.AddRange(getLevrumPolygonsFromFeature(feature));
                        break;
                    case "MultiPolygon":
                        MultiPolygon multiPolygon = JsonConvert.DeserializeObject<MultiPolygon>(geoJSON);
                        foreach (GeoJSONPolygon subPolygon in multiPolygon.Coordinates)
                        {
                            output.Add(getLevrumPolygon(subPolygon));
                        }
                        break;
                    case "Polygon":
                        GeoJSONPolygon polygon = JsonConvert.DeserializeObject<GeoJSONPolygon>(geoJSON);
                        output.Add(getLevrumPolygon(polygon));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

            }

            return output.ToArray();
        }

        public static string ConvertPolygonToGeoJSON(LevrumPolygon polygon)
        {
            List<IPosition> positions = new List<IPosition>();
            foreach (Point2 point in polygon.Points)
            {
                positions.Add(new Position(point.Y, point.X));
            }
            LineString lineStr = new LineString(positions);
            List<LineString> list = new List<LineString>();
            list.Add(lineStr);
            GeoJSONPolygon poly = new GeoJSONPolygon(list);

            return JsonConvert.SerializeObject(poly);
        }

        private static LevrumPolygon[] getLevrumPolygonsFromFeature(Feature feature)
        {
            List<LevrumPolygon> output = new List<LevrumPolygon>();

            try
            {
                if (feature.Geometry is GeoJSONPolygon)
                {
                    output.Add(getLevrumPolygon(feature.Geometry as GeoJSONPolygon));
                }
                else if (feature.Geometry is MultiPolygon)
                {
                    foreach (GeoJSONPolygon subPolygon in (feature.Geometry as MultiPolygon).Coordinates)
                    {
                        output.Add(getLevrumPolygon(subPolygon));
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return output.ToArray();
        }

        private static LevrumPolygon getLevrumPolygon(GeoJSONPolygon input)
        {
            LevrumPolygon output = new LevrumPolygon();
            foreach (LineString lineStr in input.Coordinates)
            {
                foreach (IPosition position in lineStr.Coordinates)
                {
                    output.AddPoint(position.Longitude, position.Latitude);
                }
            }

            return output;
        }
    }

    public class ProtoGeoJSON
    {
        public string Type { get; set; }
    }
}

