using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using Levrum.Utils.Geography;

namespace Levrum.Utils.Osm
{
    public static class OsmFileExtensions
    {
        public static bool SplitWay(this OsmFile osmFile, OSMWay wayToSplit, LatitudeLongitude splitStart, LatitudeLongitude splitEnd)
        {
            try
            {
                // Find where split path intersects way
                List<OSMNode> nodesForWay = new List<OSMNode>();
                foreach (string nodeId in wayToSplit.NodeReferences)
                {
                    OSMNode node;
                    if (osmFile.NodesById.TryGetValue(nodeId, out node)) {
                        nodesForWay.Add(node);
                    }
                }

                // Create separate ways
                // Remove intersection connections
                return true;
            } catch (Exception ex)
            {
                
            }
            return false;
        }
    }
}
