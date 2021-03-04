using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Levrum.Utils.Geography;
using Levrum.Utils.Geometry;

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
                    if (double.TryParse(nodeElement.Attribute("lat").Value, out lat))
                    {
                        node.Latitude = lat;
                    }

                    if (double.TryParse(nodeElement.Attribute("lon").Value, out lon))
                    {
                        node.Longitude = lon;
                    }
                    node.Latitude = double.TryParse(nodeElement.Attribute("lat").Value, out lat) ? lat : double.NaN;
                    node.Longitude = double.TryParse(nodeElement.Attribute("lon").Value, out lon) ? lon : double.NaN;

                    Bounds.ExtendBounds(node.Latitude, node.Longitude);

                    NodesById[node.ID] = node;
                    //WaysForNodes[node.ID] = new List<OSMNodeWayInfo>();
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

                    XElement oneWayTag = wayElement.Descendants("tag").Where(t => t.Attribute("k").Value == "oneway").SingleOrDefault();
                    if (oneWayTag != null && (oneWayTag.Attribute("v").Value == "yes" || oneWayTag.Attribute("v").Value == "true"))
                    {
                        way.Oneway = true;
                    }

                    XElement maxSpeedTag = wayElement.Descendants("tag").Where(t => t.Attribute("k").Value == "maxspeed").SingleOrDefault();
                    if (maxSpeedTag != null)
                    {
                        way.MaxSpeed = maxSpeedTag.Attribute("v").Value;
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
                    calculateNodeDistancesForWay(way);
                }

                return DistancesCalculated = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error calculating node distances", true);
                return DistancesCalculated = false;
            }
        }

        private void calculateNodeDistancesForWay(OSMWay way)
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

        public bool CalculateWayBounds()
        {
            try
            {
                foreach (OSMWay way in WaysById.Values)
                {
                    calculateBoundsForWay(way);
                }

                return BoundsCalculated = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error calculating way bounds", true);
                return BoundsCalculated = false;
            }
        }

        private void calculateBoundsForWay(OSMWay way)
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
                    XElement osmElement = new XElement("osm");
                    osmElement.SetAttributeValue("version", "0.6");
                    osmElement.SetAttributeValue("encoding", "UTF-8");

                    UpdateBounds();
                    XElement boundsElement = new XElement("bounds");
                    boundsElement.SetAttributeValue("minlat", Bounds.MinLat);
                    boundsElement.SetAttributeValue("minlon", Bounds.MinLon);
                    boundsElement.SetAttributeValue("maxlat", Bounds.MaxLat);
                    boundsElement.SetAttributeValue("maxlon", Bounds.MaxLon);
                    boundsElement.SetAttributeValue("origin", "Levrum OSM Writer");
                    osmElement.Add(boundsElement);

                    foreach (OSMNode node in Nodes)
                    {
                        writeNode(osmElement, node);
                    }
                    foreach (OSMWay way in Ways)
                    {
                        writeWay(osmElement, way);
                    }
                    foreach (OSMRelation relation in Relations)
                    {
                        writeRelation(osmElement, relation);
                    }

                    osmElement.Save(writer);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error saving OSM file", true);
                return false;
            }
        }

        private void writeNode(XElement parent, OSMNode node)
        {
            XElement nodeElement = node.ToXElement();
            parent.Add(nodeElement);
        }

        private void writeWay(XElement parent, OSMWay way)
        {
            XElement wayElement = way.ToXElement();
            parent.Add(wayElement);
        }

        private void writeRelation(XElement parent, OSMRelation relation)
        {
            XElement relationElement = relation.ToXElement();
            parent.Add(relationElement);
        }

        public static bool Download(string fileName, BoundingBox region)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("https://overpass-api.de/api/map?bbox={0},{1},{2},{3}", region.MinLon, region.MinLat, region.MaxLon, region.MaxLat));
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string xml = reader.ReadToEnd();
                        System.IO.File.WriteAllText(fileName, xml);
                        return true;
                    }
                }
            } 
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Unable to downlolad requested OSM file", false);
            }
            return false;
        }

        #endregion

        #region Operations

        public List<OSMWay> GetWaysIntersectingLine(LatitudeLongitude point1, LatitudeLongitude point2)
        {
            List<OSMWay> output = new List<OSMWay>();
            try
            {
                foreach (OSMWay way in Ways)
                {
                    if (way.BoundingBox.IntersectsLine(point1, point2))
                    {
                        for (int i = 0; i < way.NodeReferences.Count - 1; i++)
                        {
                            OSMNode thisNode, thatNode;
                            if (!NodesById.TryGetValue(way.NodeReferences[i], out thisNode) || !NodesById.TryGetValue(way.NodeReferences[i + 1], out thatNode))
                            {
                                continue;
                            }

                            Point2 intersectionPoint = LineSegment2.Intersection(point1, point2, thisNode.Location, thatNode.Location);
                            if (intersectionPoint != null)
                            {
                                output.Add(way);
                            }
                        }
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error getting intersecting ways", true);
            }
            return output;
        }

        public bool SplitWaysByLine(LineSegment2 line)
        {
            return SplitWaysByLine(line.PointA, line.PointB);
        }

        public bool SplitWaysByLine(LatitudeLongitude point1, LatitudeLongitude point2)
        {
            try
            {
                List<OSMWay> originalWays = Ways;
                foreach (OSMWay way in originalWays)
                {
                    if (way.Tags.ContainsKey("highway") && AcceptedRoadTypes.Contains(way.Tags["highway"]))
                    {
                        searchWayForIntersectionAndSplit(way, point1, point2);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error getting intersecting ways", true);
            }
            return false;
        }

        private void searchWayForIntersectionAndSplit(OSMWay way, LatitudeLongitude point1, LatitudeLongitude point2)
        {
            if (way.BoundingBox.IntersectsLine(point1, point2))
            {
                for (int i = 0; i < way.NodeReferences.Count - 1; i++)
                {
                    OSMNode thisNode, thatNode;
                    if (!NodesById.TryGetValue(way.NodeReferences[i], out thisNode) || !NodesById.TryGetValue(way.NodeReferences[i + 1], out thatNode))
                    {
                        continue;
                    }

                    Point2 intersectionPoint = LineSegment2.Intersection(point1, point2, thisNode.Location, thatNode.Location);
                    if (intersectionPoint != null)
                    {
                        splitWayByLine(way, point1, point2, thisNode.Location, thatNode.Location, i, intersectionPoint);
                    }
                }
            }
        }

        private void splitWayByLine(OSMWay way, LatitudeLongitude point1, LatitudeLongitude point2, LatitudeLongitude nodeLoc1, LatitudeLongitude nodeLoc2, int nodeIndex, Point2 intersectionPoint)
        {
            // Make new way
            OSMWay newWay = new OSMWay();
            newWay.ID = getValidOsmId();
            newWay.Name = way.Name;
            newWay.MaxSpeed = way.MaxSpeed;
            newWay.Oneway = way.Oneway;
            newWay.Version = "1";
            newWay.Changeset = DateTime.Now.ToString("yyMMddHHmm");
            foreach (KeyValuePair<string, string> kvp in way.Tags)
            {
                newWay.Tags[kvp.Key] = kvp.Value;
            }
            claimOsmObjectAsLevrums(newWay);
            WaysById.Add(newWay.ID, newWay);

            claimOsmObjectAsLevrums(way);

            // Make new nodes
            OSMNode oldWayTerminator = new OSMNode(), newWayTerminator = new OSMNode();

            // Move the terminators towards their nodes slightly to prevent infinite recursion
            double oldWayDistance = Math.Sqrt(Math.Pow(nodeLoc1.Latitude - intersectionPoint.Y, 2) + Math.Pow(nodeLoc1.Longitude - intersectionPoint.X, 2));
            double oldWayTerminatorDistance = oldWayDistance - .00002;
            double ratio = oldWayTerminatorDistance / oldWayDistance;
            oldWayTerminator.Location = new LatitudeLongitude(((1 - ratio) * nodeLoc1.Latitude) + (ratio * intersectionPoint.Y), ((1 - ratio) * nodeLoc1.Longitude) + (ratio * intersectionPoint.X));

            double newWayDistance = Math.Sqrt(Math.Pow(nodeLoc2.Latitude - intersectionPoint.Y, 2) + Math.Pow(nodeLoc2.Longitude - intersectionPoint.X, 2));
            double newWayTerminatorDistance = newWayDistance - .00002;
            ratio = newWayTerminatorDistance / newWayDistance;
            newWayTerminator.Location = new LatitudeLongitude(((1 - ratio) * nodeLoc2.Latitude) + (ratio * intersectionPoint.Y), ((1 - ratio) * nodeLoc2.Longitude) + (ratio * intersectionPoint.X));

            newWayTerminator.EndNodeFlag = oldWayTerminator.EndNodeFlag = true;
            
            newWayTerminator.ID = getValidOsmId();
            NodesById.Add(newWayTerminator.ID, newWayTerminator);

            oldWayTerminator.ID = getValidOsmId();
            NodesById.Add(oldWayTerminator.ID, oldWayTerminator);

            newWayTerminator.Tags.Add("roadCut", oldWayTerminator.ID);
            oldWayTerminator.Tags.Add("roadCut", newWayTerminator.ID);

            claimOsmObjectAsLevrums(newWayTerminator);
            claimOsmObjectAsLevrums(oldWayTerminator);
            newWayTerminator.Version = "1";
            oldWayTerminator.Version = "1";

            // Not exactly true but this is somewhat hard to fake reasonably?
            newWayTerminator.Changeset = newWay.Changeset;
            oldWayTerminator.Changeset = newWay.Changeset;

            // Cleanup node references
            newWay.NodeReferences.Add(newWayTerminator.ID);
            for (int j = nodeIndex + 1; j < way.NodeReferences.Count; j++)
            {
                newWay.NodeReferences.Add(way.NodeReferences[j]);
            }

            for (int j = way.NodeReferences.Count - 1; j > nodeIndex; j--) 
            {
                way.NodeReferences.RemoveAt(j);
            }

            way.NodeReferences.Add(oldWayTerminator.ID);
            way.NodeDistances.Clear();
            calculateNodeDistancesForWay(way);
            calculateNodeDistancesForWay(newWay);

            calculateBoundsForWay(newWay);
            searchWayForIntersectionAndSplit(newWay, point1, point2);
        }

        Random m_rand = new Random((int)DateTime.Now.Ticks >> 10);

        private string getValidOsmId()
        {
            // Gives a random number between 9 and 10 billion. At the time of writing (05-2020) there were only 6 billion OSM nodes. Should be safe for a year or two.
            string output = string.Empty;
            while (string.IsNullOrEmpty(output))
            {   
                double variance = m_rand.NextDouble() * Math.Pow(10, 9);
                long nodeId = (long)Math.Floor((Math.Pow(10, 9) * 9) + variance);
                output = nodeId.ToString();
                if (NodesById.ContainsKey(output) || WaysById.ContainsKey(output))
                    output = string.Empty;
            }
            
            return output;
        }

        private void claimOsmObjectAsLevrums(OSMElement element)
        {
            element.User = "Levrum";
            element.Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            element.Uid = "11158813";
            int version;
            element.Version = int.TryParse(element.Version, out version) ? (version + 1).ToString() : element.Version;
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
                    claimOsmObjectAsLevrums(megaWay);
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
