using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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
        public string Action { get; set; } = string.Empty;
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
            Action = xElement.Attribute("action")?.Value;
            Visible = xElement.Attribute("visible")?.Value;
            Version = xElement.Attribute("version")?.Value;
            Changeset = xElement.Attribute("changeset")?.Value;
            Timestamp = xElement.Attribute("timestamp")?.Value;

            List<XElement> tags = xElement.Descendants("tag").ToList();
            foreach (XElement tag in tags)
            {
                string key = tag.Attribute("k").Value;
                string value = tag.Attribute("v").Value;
                Tags[key] = value;
            }
        }

        public static void AddElementDetailsToXElement(OSMElement element, XElement xElement)
        {
            if (!string.IsNullOrEmpty(element.ID)) { xElement.SetAttributeValue("id", element.ID); }
            if (!string.IsNullOrEmpty(element.User)) { xElement.SetAttributeValue("user", SecurityElement.Escape(element.User)); }
            if (!string.IsNullOrEmpty(element.Uid)) { xElement.SetAttributeValue("uid", element.Uid); }
            if (!string.IsNullOrEmpty(element.Action)) { xElement.SetAttributeValue("action", element.Action); }
            if (!string.IsNullOrEmpty(element.Visible)) { xElement.SetAttributeValue("visible", element.Visible); }
            if (!string.IsNullOrEmpty(element.Version)) { xElement.SetAttributeValue("version", element.Version); }
            if (!string.IsNullOrEmpty(element.Changeset)) { xElement.SetAttributeValue("changeset", element.Changeset); }
            if (!string.IsNullOrEmpty(element.Timestamp)) { xElement.SetAttributeValue("timestamp", element.Timestamp); }
        }

        public static void AddElementTagsToXElement(OSMElement element, XElement xElement)
        {
            foreach (KeyValuePair<string, string> kvp in element.Tags)
            {
                XElement tag = new XElement("tag");
                tag.SetAttributeValue("k", kvp.Key);
                tag.SetAttributeValue("v", kvp.Value);
                xElement.Add(tag);
            }
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

        public XElement ToXElement()
        {
            return ToXElement(this);
        }

        public static XElement ToXElement(OSMNode node)
        {
            XElement output = new XElement("node");
            AddElementDetailsToXElement(node, output);
            output.SetAttributeValue("lat", node.Latitude);
            output.SetAttributeValue("lon", node.Longitude);
            AddElementTagsToXElement(node, output);

            return output;
        }
    }

    public class OSMWay : OSMElement
    {
        public string MaxSpeed { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new BoundingBox();
        public List<string> NodeReferences { get; set; } = new List<string>();
        public List<double> NodeDistances { get; set; } = new List<double>();
        public bool Oneway { get; set; } = false;
        
        public XElement ToXElement()
        {
            return ToXElement(this);
        }

        public static XElement ToXElement(OSMWay way)
        {
            XElement output = new XElement("way");
            AddElementDetailsToXElement(way, output);
            foreach (string nodeRef in way.NodeReferences)
            {
                XElement node = new XElement("nd");
                node.SetAttributeValue("ref", nodeRef);

                output.Add(node);
            }
            AddElementTagsToXElement(way, output);

            return output;
        }
    }

    public class OSMRelation : OSMElement
    {
        public List<OSMRelationMember> Members { get; set; } = new List<OSMRelationMember>();

        public XElement ToXElement()
        {
            return ToXElement(this);
        }

        public static XElement ToXElement(OSMRelation relation)
        {
            XElement output = new XElement("relation");
            AddElementDetailsToXElement(relation, output);
            foreach (OSMRelationMember member in relation.Members)
            {
                XElement memberElement = new XElement("member");
                if (!string.IsNullOrWhiteSpace(member.Type)) { memberElement.SetAttributeValue("type", member.Type); }
                if (!string.IsNullOrWhiteSpace(member.Ref)) { memberElement.SetAttributeValue("ref", member.Ref); }
                if (!string.IsNullOrWhiteSpace(member.Role)) { memberElement.SetAttributeValue("role", member.Role); }

                output.Add(memberElement);
            }
            AddElementTagsToXElement(relation, output);

            return output;
        }
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
