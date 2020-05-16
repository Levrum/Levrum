using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Levrum.Utils.Geography;

namespace Levrum.Utils.Osm
{

    public class OSMElement
    {
        public string ID { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public string Visible { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Changeset { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        public string InternalName { get; set; } = string.Empty;

        public string Name
        {
            get
            {
                if (Tags.ContainsKey("name"))
                {
                    return Tags["name"];
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                Tags["name"] = value;
            }
        }

        public void GetElementDetailsFromXElement(XElement xElement)
        {
            ID = xElement.Attribute("id")?.Value;
            User = xElement.Attribute("user")?.Value;
            Uid = xElement.Attribute("uid")?.Value;
            Visible = xElement.Attribute("visible")?.Value;
            Version = xElement.Attribute("version")?.Value;
            Changeset = xElement.Attribute("changeset")?.Value;
            Timestamp = xElement.Attribute("timestamp")?.Value;
        }
    }

    public class OSMNode : OSMElement
    {
        public LatitudeLongitude Location { get; set; } = new LatitudeLongitude();
        public double Latitude { get { return Location.Latitude; } set { Location.Latitude = value; } }
        public double Longitude { get { return Location.Longitude; } set { Location.Longitude = value; } }
        public BoundingBox BoundingBox { get; set; } = new BoundingBox();
        public bool EndNodeFlag { get; set; } = false;
        public bool IsIntersection { get; set; } = false;
        public List<string> WayReferences { get; set; } = new List<string>();
        public int References { get; set; } = 0;
    }

    public class OSMWay : OSMElement
    {
        public string MaxSpeed { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new BoundingBox();
        public List<string> NodeReferences { get; set; } = new List<string>();
        public List<double> NodeDistances { get; set; } = new List<double>();
        public bool Oneway { get; set; } = false;
    }

    public class OSMRelation : OSMElement
    {
        public List<OSMRelationMember> Members { get; set; } = new List<OSMRelationMember>();
    }

    public class OSMRelationMember
    {
        public string Type { get; set; }
        public string Ref { get; set; }
        public string Role { get; set; }
    }

    public class OSMNodeDistance
    {
        public string NodeID { get; set; } = string.Empty;
        public double Distance { get; set; } = 0;

        public OSMNodeDistance(string _nodeId = "", double _distance = 0)
        {
            NodeID = _nodeId;
            Distance = _distance;
        }
    }

    public class OSMNodeWayInfo
    {
        public string WayID = string.Empty;
        public double Distance { get; set; } = 0;
        public int Index { get; set; } = -1;
    }
}
