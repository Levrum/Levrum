using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Osm
{
    public class OsmBlock
    {
        public string ID { get; set; } = string.Empty;
        public string WayID { get; set; } = string.Empty;
        public double Length { get; set; } = 0.0;
        public double WayLength { get; set; } = 0.0;
        public OSMIntersection FromIntersection { get; set; } = null;
        public OSMIntersection ToIntersection { get; set; } = null;
        public object Tag { get; set; } = null;
    }
}
