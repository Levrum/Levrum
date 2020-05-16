using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using Levrum.Utils.Geography;

namespace Levrum.Utils.Osm
{
    public class OSMIntersection
    {
        public string ID { get; set; } = string.Empty;
        public HashSet<string> TrafficLightIDs { get; set; } = new HashSet<string>();
        public string Name { get; set; } = string.Empty;
        
        [JsonIgnore]
        public LatitudeLongitude Location { get; set; } = new LatitudeLongitude(0.0, 0.0);
        public double Latitude { get { return Location.Latitude; } set { Location.Latitude = value; } }
        public double Longitude { get { return Location.Longitude; } set { Location.Longitude = value; } }
        
        [JsonIgnore]
        public double[] XYCoordinates { get; set; } = new double[2] { 0.0, 0.0 };
        public double X { get { return XYCoordinates[0]; } set { XYCoordinates[0] = value; } }
        public double Y { get { return XYCoordinates[1]; } set { XYCoordinates[1] = value; } }
        
        public bool HasTrafficLight { get; set; } = false;
        public bool HasOpticom { get; set; } = false;
        public int Traversals { get; set; } = 0;
        public int EmergencyTraverals { get; set; } = 0;

        public Dictionary<string, double> ConnectedIntersectionDistances = new Dictionary<string, double>();

        [JsonIgnore]
        public List<AvlPoint> AccelerationDecelerationPoints { get; set; } = new List<AvlPoint>();

        [JsonIgnore]
        public HashSet<OsmBlock> NeighboringBlocks { get; set; }
    }
}
