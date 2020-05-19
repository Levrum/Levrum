using System;
using System.Collections.Generic;
using System.Text;

using Levrum.Utils.Geometry;

namespace Levrum.Utils.Geography
{

    public class BoundingBox
    {
        public double MinLat { get; set; } = double.MaxValue;
        public double MinLon { get; set; } = double.MaxValue;
        public double MaxLat { get; set; } = double.MinValue;
        public double MaxLon { get; set; } = double.MinValue;

        public BoundingBox()
        {

        }

        public BoundingBox(double _minLat, double _minLon, double _maxLat, double _maxLon)
        {
            MinLat = _minLat;
            MinLon = _minLon;
            MaxLat = _maxLat;
            MaxLon = _maxLon;
        }

        public void ExtendBounds(LatitudeLongitude latLon)
        {
            ExtendBounds(latLon.Latitude, latLon.Longitude);
        }

        public void ExtendBounds(double[] latLon)
        {
            ExtendBounds(latLon[0], latLon[1]);
        }

        public void ExtendBounds(double lat, double lon)
        {
            MinLat = MinLat == double.MaxValue ? lat : Math.Min(MinLat, lat);
            MinLon = MinLon == double.MaxValue ? lon : Math.Min(MinLon, lon);
            MaxLat = MaxLat == double.MinValue ? lat : Math.Max(MaxLat, lat);
            MaxLon = MaxLon == double.MinValue ? lon : Math.Max(MaxLon, lon);
        }

        public bool Contains(LatitudeLongitude latLon)
        {
            return Contains(latLon.Latitude, latLon.Longitude);
        }

        public bool Contains(double[] latLon)
        {
            return Contains(latLon[0], latLon[1]);
        }

        public bool Contains(double lat, double lon)
        {
            return (lat >= MinLat && lat <= MaxLat) && (lon >= MinLon && lon <= MaxLon);
        }

        public static explicit operator double[](BoundingBox input)
        {
            double[] output = new double[4];
            output[0] = input.MinLat;
            output[1] = input.MinLon;
            output[2] = input.MaxLat;
            output[3] = input.MaxLon;

            return output;
        }

        public static explicit operator BoundingBox(double[] input)
        {
            if (input.Length != 4)
            {
                throw new ArgumentException("Invalid input array");
            }

            BoundingBox output = new BoundingBox(input[0], input[1], input[2], input[3]);
            return output;
        }

        public bool IntersectsLine(LatitudeLongitude point1, LatitudeLongitude point2)
        {
            try
            {
                Point2 westIntersection = LineSegment2.Intersection(point1.Longitude, point1.Latitude, point2.Longitude, point2.Latitude, MinLon, MaxLat, MinLon, MinLat);
                Point2 southIntersection = LineSegment2.Intersection(point1.Longitude, point1.Latitude, point2.Longitude, point2.Latitude, MinLon, MinLat, MaxLon, MinLat);
                Point2 eastIntersection = LineSegment2.Intersection(point1.Longitude, point1.Latitude, point2.Longitude, point2.Latitude, MaxLon, MaxLat, MaxLon, MinLat);
                Point2 northIntersection = LineSegment2.Intersection(point1.Longitude, point1.Latitude, point2.Longitude, point2.Latitude, MinLon, MaxLat, MaxLon, MaxLat);

                return (westIntersection != null || southIntersection != null || eastIntersection != null || northIntersection != null);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error testing line intersection", false);
                return false;
            }
        }
    }
}
