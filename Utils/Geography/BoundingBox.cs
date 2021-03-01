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

        public LatitudeLongitude GetCenterPoint()
        {
            try
            {
                LatitudeLongitude output = new LatitudeLongitude();
                output.Latitude = MinLat + (MaxLat - MinLat) / 2;
                output.Longitude = MinLon + (MaxLon - MinLon) / 2;

                return output;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Error computing bounding box centroid", false);
                return null;
            }
        }

        // Semi-axes of WGS-84 geoidal reference
        private const double WGS84_a = 6378137.0; // Major semiaxis [m]
        private const double WGS84_b = 6356752.3; // Minor semiaxis [m]

        // 'halfSideInKm' is the half length of the bounding box you want in kilometers.
        public static BoundingBox GetBoundingBox(Point2 point, double halfSideInMiles)
        {
            double halfSideInKm = halfSideInMiles / 0.62137;
            // Bounding box surrounding the point at given coordinates,
            // assuming local approximation of Earth surface as a sphere
            // of radius given by WGS84
            var lat = Deg2rad(point.Y);
            var lon = Deg2rad(point.X);
            var halfSide = 1000 * halfSideInKm;

            // Radius of Earth at given latitude
            var radius = WGS84EarthRadius(lat);
            // Radius of the parallel at given latitude
            var pradius = radius * Math.Cos(lat);

            var latMin = lat - halfSide / radius;
            var latMax = lat + halfSide / radius;
            var lonMin = lon - halfSide / pradius;
            var lonMax = lon + halfSide / pradius;

            return new BoundingBox
            {
                MinLat = Rad2deg(latMin),
                MinLon = Rad2deg(lonMin),
                MaxLat = Rad2deg(latMax),
                MaxLon = Rad2deg(lonMax)
            };
        }

        // degrees to radians
        private static double Deg2rad(double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

        // radians to degrees
        private static double Rad2deg(double radians)
        {
            return 180.0 * radians / Math.PI;
        }

        // Earth radius at a given latitude, according to the WGS-84 ellipsoid [m]
        private static double WGS84EarthRadius(double lat)
        {
            // http://en.wikipedia.org/wiki/Earth_radius
            var An = WGS84_a * WGS84_a * Math.Cos(lat);
            var Bn = WGS84_b * WGS84_b * Math.Sin(lat);
            var Ad = WGS84_a * Math.Cos(lat);
            var Bd = WGS84_b * Math.Sin(lat);
            return Math.Sqrt((An * An + Bn * Bn) / (Ad * Ad + Bd * Bd));
        }
    }
}
