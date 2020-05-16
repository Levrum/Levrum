using System;
using System.Collections.Generic;
using System.Text;

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
            MinLat = Math.Min(MinLat, lat);
            MinLon = Math.Min(MinLon, lon);
            MaxLat = Math.Max(MaxLat, lat);
            MaxLon = Math.Min(MaxLon, lon);
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
    }
}
