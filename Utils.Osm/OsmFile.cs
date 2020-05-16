using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Levrum.Utils.Geography;

namespace Levrum.Utils.Osm
{
    public class OsmFile
    {
        public FileInfo File { get; set; }
        public XDocument Document { get; protected set; }

        public Dictionary<string, OSMNode> NodesById { get; set; } = new Dictionary<string, OSMNode>();
        public List<OSMNode> Nodes { get { return NodesById.Values.ToList(); } }

        public Dictionary<string, OSMWay> WaysById { get; set; } = new Dictionary<string, OSMWay>();
        public List<OSMWay> Ways { get { return WaysById.Values.ToList(); } }

        public Dictionary<string, OSMRelation> RelationsById { get; set; } = new Dictionary<string, OSMRelation>();
        public List<OSMRelation> Relations { get { return RelationsById.Values.ToList(); } }

        public Dictionary<string, OSMIntersection> Intersections { get; set; } = new Dictionary<string, OSMIntersection>();

        public BoundingBox Bounds { get; set; } = new BoundingBox();

        public bool DistancesCalculated { get; protected set; } = false;
        public bool BoundsCalculated { get; protected set; } = false;
        public bool IntersectionsGenerated { get; protected set; } = false;

        public static HashSet<string> AcceptedRoadTypes = new HashSet<string>() { "residential", "living_street", "motorway", "motorway_link", "primary", "primary_link", "secondary", "secondary_link", "tertiary", "tertiary_link", "trunk", "trunk_link", "pedestrian", "service", "unclassified" };

        public OsmFile(OsmFileInfo osmFileInfo)
        {
            File = osmFileInfo.OsmFile;
        }

        public OsmFile(FileInfo _file)
        {
            File = _file;
        }

        public OsmFile(string path)
        {
            File = new FileInfo(path);
        }

        #region Loading and Saving

        private void clearValues()
        {
            NodesById.Clear();
            WaysById.Clear();
            RelationsById.Clear();
            Intersections.Clear();
            Bounds = new BoundingBox();
        }

        /// <summary>
        /// Load OSM objects from an OSM XML file
        /// </summary>
        /// <param name="calculateDistances">Calculate distances between nodes on load</param>
        /// <param name="calculateBounds">Calculate way bounding boxes on load</param>
        /// <param name="strictMode">Discard node references from ways if the nodes to not exist</param>
        /// <returns></returns>
        public bool Load(bool calculateDistances = false, bool calculateBounds = false, bool strictMode = false)
        {
            try
            {
                clearValues();

                XDocument doc = XDocument.Load(File.FullName);
                List<XElement> nodeElements = doc.Descendants("node").ToList();
                List<XElement> wayElements = doc.Descendants("way").ToList();
                List<XElement> relationElements = doc.Descendants("relation").ToList();

                double lat, lon;
                foreach (XElement nodeElement in nodeElements)
                {
                    OSMNode node = new OSMNode();
                    node.GetElementDetailsFromXElement(nodeElement);
                    node.Latitude = double.TryParse(nodeElement.Attribute("lat").Value, out lat) ? lat : double.NaN;
                    node.Longitude = double.TryParse(nodeElement.Attribute("lon").Value, out lon) ? lon : double.NaN;

                    Bounds.ExtendBounds(node.Latitude, node.Longitude);

                    NodesById[node.ID] = node;
                    //WaysForNodes[node.ID] = new List<OSMNodeWayInfo>();

                    List<XElement> tags = nodeElement.Descendants("tag").ToList();
                    foreach (XElement tag in tags)
                    {
                        string key = tag.Attribute("k").Value;
                        string value = tag.Attribute("v").Value;
                        node.Tags[key] = value;
                    }
                }

                foreach (XElement wayElement in wayElements)
                {
                    OSMWay way = new OSMWay();
                    way.GetElementDetailsFromXElement(wayElement);
                    if (way.ID == "15200801")
                    {
                        var str = "str";
                    }

                    List<XElement> nodes = wayElement.Descendants("nd").ToList();
                    foreach (XElement node in nodes)
                    {
                        string refValue = node.Attribute("ref").Value;
                        if (!string.IsNullOrEmpty(refValue))
                        {
                            OSMNode osmNode;
                            if (!NodesById.TryGetValue(refValue, out osmNode))
                            {
                                if (strictMode)
                                {
                                    continue;
                                } else
                                {
                                    osmNode = new OSMNode() { ID = refValue };
                                    NodesById.Add(refValue, osmNode);
                                }
                            }
                            osmNode.WayReferences.Add(way.ID);
                            way.NodeReferences.Add(osmNode.ID);
                        }
                    }

                    List<XElement> tags = wayElement.Descendants("tag").ToList();
                    foreach (XElement tag in tags)
                    {
                        string key = tag.Attribute("k").Value;
                        string value = tag.Attribute("v").Value;

                        switch (key)
                        {
                            case "oneway":
                                way.Oneway = (value == "yes" || value == "true");
                                break;
                            case "maxspeed":
                                way.MaxSpeed = value;
                                break;
                        }

                        way.Tags[key] = value;
                    }

                    WaysById[way.ID] = way;
                }

                foreach (XElement relationElement in relationElements)
                {
                    OSMRelation relation = new OSMRelation();
                    relation.GetElementDetailsFromXElement(relationElement);

                    List<XElement> members = relationElement.Descendants("member").ToList();
                    foreach (XElement member in members)
                    {
                        OSMRelationMember rm = new OSMRelationMember();
                        rm.Type = member.Attribute("type")?.Value;
                        rm.Ref = member.Attribute("ref")?.Value;
                        rm.Role = member.Attribute("role")?.Value;

                        relation.Members.Add(rm);
                    }

                    RelationsById[relation.ID] = relation;
                }

                if (calculateDistances)
                {
                    CalculateNodeDistances();
                }

                if (calculateBounds)
                {
                    CalculateWayBounds();
                }


                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error loading OSM file", true);
                return false;
            }
        }

        public bool CalculateNodeDistances()
        {
            try
            {
                foreach (OSMWay way in WaysById.Values) 
                {
                    OSMNode previousNode = null;
                    double previousDistance = 0, newDistance = 0;
                    foreach (string nodeRef in way.NodeReferences)
                    {
                        OSMNode currentNode;
                        if (NodesById.TryGetValue(nodeRef, out currentNode))
                        {
                            if (previousNode != null)
                            {
                                double distance = previousNode.Location.DistanceFrom(currentNode.Location);
                                newDistance = previousDistance + distance;
                            }
                            previousDistance = newDistance;
                            previousNode = currentNode;
                        }
                        way.NodeDistances.Add(previousDistance);
                    }
                }

                return DistancesCalculated = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error calculating node distances", true);
                return DistancesCalculated = false;
            }
        }

        public bool CalculateWayBounds()
        {
            try
            {
                foreach (OSMWay way in WaysById.Values)
                {
                    foreach (string nodeId in way.NodeReferences)
                    {
                        OSMNode node;
                        if (NodesById.TryGetValue(nodeId, out node))
                        {
                            way.BoundingBox.ExtendBounds(node.Location);
                        }
                    }
                }

                return BoundsCalculated = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error calculating way bounds", true);
                return BoundsCalculated = false;
            }
        }

        public void UpdateBounds()
        {
            foreach (OSMNode node in Nodes)
            {
                Bounds.ExtendBounds(node.Latitude, node.Longitude);
            }
        }

        public bool Save()
        {
            return Save(File);
        }

        public bool Save(FileInfo file)
        {
            return Save(file.FullName);
        }

        public bool Save(string path)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    writer.WriteLine("<osm version=\"0.6\" generator=\"Levrum\">");
                    UpdateBounds();
                    writer.WriteLine(string.Format("  <bounds minlat=\"{0}\" minlon=\"{1}\" maxlat=\"{2}\" maxlon=\"{3}\" origin=\"Levrum OSM Writer\" />", Bounds.MinLat, Bounds.MinLon, Bounds.MaxLat, Bounds.MaxLon));
                    foreach (OSMNode node in Nodes)
                    {
                        writeNode(writer, node);
                    }
                    foreach (OSMWay way in Ways)
                    {
                        writeWay(writer, way);
                    }
                    foreach (OSMRelation relation in Relations)
                    {
                        writeRelation(writer, relation);
                    }
                    writer.WriteLine("</osm>");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error saving OSM file", true);
                return false;
            }
        }

        const string c_nodeNoTagFormat = "  <node id=\"{0}\" timestamp=\"{1}\" uid=\"{2}\" user=\"{3}\" version=\"{4}\" changeset=\"{5}\" visible=\"{6}\" lat=\"{7}\" lon=\"{8}\" />";
        const string c_nodeWithTagFormat = "  <node id=\"{0}\" timestamp=\"{1}\" uid=\"{2}\" user=\"{3}\" version=\"{4}\" changeset=\"{5}\" visible=\"{6}\" lat=\"{7}\" lon=\"{8}\" >";
        const string c_tagFormat = "    <tag k=\"{0}\" v=\"{1}\" />";

        private void writeNode(StreamWriter writer, OSMNode node)
        {

            var format = node.Tags.Count == 0 ? c_nodeNoTagFormat : c_nodeWithTagFormat;
            writer.WriteLine(string.Format(format, node.ID, node.Timestamp, node.Uid, SecurityElement.Escape(node.User), node.Version, node.Changeset, node.Visible, node.Latitude, node.Longitude));

            foreach (KeyValuePair<string, string> keyValuePair in node.Tags)
            {
                writer.WriteLine(string.Format(c_tagFormat, SecurityElement.Escape(keyValuePair.Key), SecurityElement.Escape(keyValuePair.Value)));
            }
            if (node.Tags.Count > 0)
            {
                writer.WriteLine("  </node>");
            }
        }

        const string c_wayFormat = "  <way id=\"{0}\" timestamp=\"{1}\" uid=\"{2}\" user=\"{3}\" version=\"{4}\" changeset=\"{5}\" visible=\"{6}\">";
        const string c_nodeRefFormat = "    <nd ref=\"{0}\" />";

        private void writeWay(StreamWriter writer, OSMWay way)
        {
            writer.WriteLine(string.Format(c_wayFormat, way.ID, way.Timestamp, way.Uid, SecurityElement.Escape(way.User), way.Version, way.Changeset, way.Visible));
            List<string> nodeIds = way.NodeReferences;
            foreach (string node in nodeIds)
            {
                writer.WriteLine(string.Format(c_nodeRefFormat, node));
            }

            foreach (KeyValuePair<string, string> keyValuePair in way.Tags)
            {
                writer.WriteLine(string.Format(c_tagFormat, SecurityElement.Escape(keyValuePair.Key), SecurityElement.Escape(keyValuePair.Value)));
            }
            writer.WriteLine("  </way>");
        }

        const string c_relationFormat = "  <relation id=\"{0}\" timestamp=\"{1}\" uid=\"{2}\" user=\"{3}\" version=\"{4}\" changeset=\"{5}\" visible=\"{6}\">";
        const string c_relationMemberFormat = "    <member type=\"{0}\" ref=\"{1}\" role=\"{2}\" />";

        private void writeRelation(StreamWriter writer, OSMRelation relation)
        {
            writer.WriteLine(string.Format(c_relationFormat, relation.ID, relation.Timestamp, relation.Uid, SecurityElement.Escape(relation.User), relation.Version, relation.Changeset, relation.Visible));
            foreach (OSMRelationMember member in relation.Members)
            {
                writer.WriteLine(string.Format(c_relationMemberFormat, member.Type, member.Ref, member.Role));
            }
            writer.WriteLine("  </relation>");
        }

        #endregion

        #region Intersections

        public void GenerateIntersections()
        {
            Intersections.Clear();
            try
            {
                if (!DistancesCalculated)
                {
                    CalculateNodeDistances();
                }

                generateIntersections();
                cleanSplitWays();
                createEndIntersections();
                findConnectedIntersections();
                flagIntersectionInNodes();
                IntersectionsGenerated = true;
            }
            catch (Exception ex)
            {
                IntersectionsGenerated = false;
            }
        }

        private void generateIntersections()
        {
            foreach (OSMWay way in Ways)
            {
                List<string> nodeIdsForWay = way.NodeReferences;
                if (nodeIdsForWay.Count < 2)
                {
                    continue;
                }

                if (!way.Tags.ContainsKey("highway") || !AcceptedRoadTypes.Contains(way.Tags["highway"])) 
                {
                    continue;
                }

                string firstNodeId = way.NodeReferences[0];
                string lastNodeId = way.NodeReferences[way.NodeReferences.Count - 1];

                if (NodesById.ContainsKey(firstNodeId)) { NodesById[firstNodeId].EndNodeFlag = true; }
                if (NodesById.ContainsKey(lastNodeId)) { NodesById[lastNodeId].EndNodeFlag = true; }

                foreach (string nodeId in nodeIdsForWay)
                {
                    OSMNode node;
                    if (!NodesById.TryGetValue(nodeId, out node))
                    {
                        continue;
                    }

                    node.References++;
                    if (firstNodeId != nodeId && lastNodeId != nodeId)
                    {
                        node.References++;
                    }
                    else
                    {
                        int nodeIndex = nodeIdsForWay.IndexOf(nodeId);
                        if (nodeIndex != 0 && nodeIndex < nodeIdsForWay.Count - 1) // Handles instances where a node appears twice in a way, once in the middle and once at the end
                        {
                            node.References++;
                        }
                    }

                    if (node.References >= 3 && !Intersections.ContainsKey(nodeId))
                    {
                        OSMIntersection intersection = new OSMIntersection();
                        intersection.ID = nodeId;
                        intersection.Location = node.Location;
                        Intersections[nodeId] = intersection;
                    }
                    else if (way.Name != null && !node.InternalName.Contains(way.Name))
                    {
                        node.InternalName = string.IsNullOrWhiteSpace(node.Name) ? way.Name : string.Format("{0} / {1}", node.Name, way.Name);
                    }
                }
            }
        }

        private void cleanSplitWays()
        {
            // This function is a doozy! Hopefully I translated its functionality okay, but if you're running into a problem with intersections it probably lies here.
            Dictionary<string, string> removedWays = new Dictionary<string, string>();
            List<OSMNode> nodes = Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                OSMNode node = nodes[i];
                List<OSMWay> filteredWaysForNode = new List<OSMWay>();

                // Need to create a mega way of the two ways the node is splitting. Otherwise multiple things get screwed up down the line.
                foreach (string usedInWay in node.WayReferences.ToList())
                {
                    string newID = usedInWay;
                    while (removedWays.ContainsKey(newID))
                    {
                        node.WayReferences.Remove(newID);
                        newID = removedWays[newID];
                        node.WayReferences.Add(newID);
                    }
                }

                foreach (string wayId in node.WayReferences) 
                {
                    OSMWay way;
                    if (WaysById.TryGetValue(wayId, out way))
                    {
                        if (way.Tags.ContainsKey("highway") && AcceptedRoadTypes.Contains(way.Tags["highway"]))
                        {
                            filteredWaysForNode.Add(way);
                        }
                    }
                }

                if (node.References == 2 && filteredWaysForNode.Count == 2)
                {
                    OSMWay megaWay, dyingWay;

                    bool reversedDyingWay = false;

                    OSMWay way1 = filteredWaysForNode[0];
                    OSMWay way2 = filteredWaysForNode[1];

                    bool isLastNodeForWayOne = way1.NodeReferences[way1.NodeReferences.Count - 1] == node.ID;
                    bool isLastNodeForWayTwo = way2.NodeReferences[way2.NodeReferences.Count - 1] == node.ID;

                    if (isLastNodeForWayOne)
                    {
                        megaWay = way1;
                        dyingWay = way2;

                        if (isLastNodeForWayTwo)
                        {
                            // Situation where they are both facing towards the split way node
                            reversedDyingWay = true;
                        }
                    }
                    else if (isLastNodeForWayTwo)
                    {
                        megaWay = way2;
                        dyingWay = way1;
                    }
                    else
                    {
                        // Weird scenario where they are both facing away from the split way node
                        OSMIntersection intersection = new OSMIntersection();
                        intersection.ID = node.ID;
                        intersection.Location = node.Location;
                        Intersections[node.ID] = intersection;
                        continue;
                    }

                    // This is when one way loops around and connects to the other way more than once
                    bool multipleConnections = false;
                    foreach (string nodeId in dyingWay.NodeReferences)
                    {
                        if (nodeId != node.ID && megaWay.NodeReferences.Contains(nodeId))
                        {
                            multipleConnections = true;
                            if (!Intersections.ContainsKey(nodeId))
                            {
                                OSMNode multiNode = NodesById[nodeId];
                                multiNode.IsIntersection = true;

                                OSMIntersection intersection = new OSMIntersection();
                                intersection.ID = nodeId;
                                intersection.Location = multiNode.Location;
                                Intersections[nodeId] = intersection;
                            }

                            break;
                        }
                    }

                    if (multipleConnections)
                    {
                        continue;
                    }

                    if (megaWay.ID == dyingWay.ID)
                    {
                        node.References--;
                        node.WayReferences.RemoveAt(1);
                        continue;
                    }

                    if ((megaWay.Oneway || dyingWay.Oneway) ^ (megaWay.Oneway && dyingWay.Oneway))
                    {
                        //If there is just one one-way street, add an intersection. If they both are, ...way murder time!
                        if (!Intersections.ContainsKey(node.ID))
                        {
                            OSMIntersection intersection = new OSMIntersection();
                            intersection.ID = node.ID;
                            intersection.Location = node.Location;
                            Intersections[node.ID] = intersection;
                        }
                        continue;
                    }

                    // Identity theft
                    megaWay.Name = string.IsNullOrWhiteSpace(megaWay.Name) ? dyingWay.Name : megaWay.Name;
                    
                    // Combine max speeds
                    double mwMaxSpeed, dwMaxSpeed;
                    if (double.TryParse(megaWay.MaxSpeed, out mwMaxSpeed) && double.TryParse(dyingWay.MaxSpeed, out dwMaxSpeed))
                    {
                        megaWay.MaxSpeed = Math.Max(mwMaxSpeed, dwMaxSpeed).ToString();
                    }
                    else if (string.IsNullOrWhiteSpace(megaWay.MaxSpeed))
                    {
                        megaWay.MaxSpeed = dyingWay.MaxSpeed;
                    }

                    if (!reversedDyingWay)
                    {
                        foreach (string nodeId in dyingWay.NodeReferences)
                        {
                            if (nodeId != node.ID)
                            {
                                megaWay.NodeReferences.Add(nodeId);
                            }
                        }
                    } else
                    {
                        for (int j = dyingWay.NodeReferences.Count - 1; j >= 0; j--) 
                        { 
                            if (dyingWay.NodeReferences[j] != node.ID)
                            {
                                megaWay.NodeReferences.Add(dyingWay.NodeReferences[j]);
                            }
                        }
                    }

                    double previousLength = megaWay.NodeDistances[megaWay.NodeDistances.Count - 1];
                    if (!reversedDyingWay)
                    {
                        for (int h = 0; h < dyingWay.NodeReferences.Count; h++)
                        {
                            string nodeId = dyingWay.NodeReferences[h];
                            if (nodeId != node.ID)
                            {
                                megaWay.NodeDistances.Add(previousLength + dyingWay.NodeDistances[h]);
                            }
                        }
                    } else
                    {
                        for (int j = dyingWay.NodeReferences.Count - 1; j >= 0; j--)
                        {
                            string nodeId = dyingWay.NodeReferences[j];
                            if (nodeId != node.ID)
                            {
                                int inverseIndex = dyingWay.NodeReferences.Count - 1 - j;
                                megaWay.NodeDistances.Add(dyingWay.NodeDistances[inverseIndex] + previousLength);
                            }
                        }
                    }

                    //megaWay uses mega drain...
                    WaysById.Remove(dyingWay.ID);
                    node.WayReferences.Remove(dyingWay.ID);
                    node.References--;
                    removedWays[dyingWay.ID] = megaWay.ID;
                    foreach (var murderedWay in removedWays.ToList())
                    {
                        if (murderedWay.Value == dyingWay.ID)
                        {
                            removedWays[murderedWay.Key] = megaWay.ID;
                        }
                    }
                    //Its super effective!
                    //megaWay regains some HP
                    //dyingWay has fainted!

                    //Levrum used ULTRA BALL!
                    //Gotcha! megaWay was caught!
                    //megaWay's data was added to the POKEDEX
                    //Give a nickname to the captured megaWay?
                    megaWay.User = "Levrum";
                    megaWay.Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    megaWay.Uid = "11158813";
                    int version;
                    megaWay.Version = int.TryParse(megaWay.Version, out version) ? (version + 1).ToString() : megaWay.Version;
                }
            }

            // Fix any references that may have changed after the last loop
            foreach (OSMNode node in NodesById.Values.ToList())
            {
                foreach (string usedInWay in node.WayReferences.ToList())
                {
                    string newID = usedInWay;
                    while (removedWays.ContainsKey(newID))
                    {
                        node.WayReferences.Remove(newID);
                        newID = removedWays[newID];
                        node.WayReferences.Add(newID);
                    }
                }
            }
        }

        private void createEndIntersections()
        {
            foreach (OSMWay way in Ways)
            {
                if (!way.Tags.ContainsKey("highway") || !AcceptedRoadTypes.Contains(way.Tags["highway"]) || way.NodeReferences.Count < 2)
                {
                    continue;
                }

                string firstNodeId = way.NodeReferences[0];
                string lastNodeId = way.NodeReferences[way.NodeReferences.Count - 1];

                OSMNode firstNode, lastNode;

                if (NodesById.TryGetValue(firstNodeId, out firstNode) && firstNode.References == 1 && !Intersections.ContainsKey(firstNode.ID))
                {
                    OSMIntersection intersection = new OSMIntersection();
                    intersection.ID = firstNode.ID;
                    intersection.Location = firstNode.Location;
                    Intersections[firstNode.ID] = intersection;
                }

                if (NodesById.TryGetValue(lastNodeId, out lastNode) && lastNode.References == 1 && !Intersections.ContainsKey(lastNode.ID)) 
                {
                    OSMIntersection intersection = new OSMIntersection();
                    intersection.ID = lastNode.ID;
                    intersection.Location = lastNode.Location;
                    Intersections[lastNode.ID] = intersection;
                }
            }
        }

        private void findConnectedIntersections()
        {
            foreach (OSMWay way in Ways)
            {
                if (way.NodeReferences.Count < 2)
                {
                    continue;
                }

                if (!way.Tags.ContainsKey("highway") || !AcceptedRoadTypes.Contains(way.Tags["highway"]))
                {
                    continue;
                }

                bool firstLoop = true;
                double distSinceMatch = 0;
                LatitudeLongitude previousLoc = null;
                OSMIntersection intersection = null, previousIntersection = null;
                foreach (string nodeId in way.NodeReferences)
                {
                    if (nodeId == "149232745" && previousIntersection != null && previousIntersection.ID == "149161225")
                    {
                        var str = "str";
                    }
                    OSMNode node;
                    if (!NodesById.TryGetValue(nodeId, out node))
                    {
                        int index = way.NodeReferences.IndexOf(nodeId);
                        way.NodeReferences.RemoveAt(index);
                        way.NodeDistances.RemoveAt(index);
                        continue;
                    }

                    if (!firstLoop)
                    {
                        distSinceMatch += previousLoc.DistanceFrom(node.Location);
                    }

                    previousLoc = node.Location;
                    firstLoop = false;
                    if (Intersections.TryGetValue(nodeId, out intersection))
                    {
                        if (previousIntersection != null)
                        {
                            previousIntersection.ConnectedIntersectionDistances[nodeId] = distSinceMatch;
                            if (!way.Oneway)
                            {
                                intersection.ConnectedIntersectionDistances[previousIntersection.ID] = distSinceMatch;
                            }
                            distSinceMatch = 0;
                        }
                        previousIntersection = intersection;
                    }
                }
            }
        }

        private void flagIntersectionInNodes()
        {
            foreach (string nodeId in Intersections.Keys)
            {
                OSMNode node;
                if (NodesById.TryGetValue(nodeId, out node))
                {
                    node.IsIntersection = true;
                }
            }
        }

        #endregion
    }
}
