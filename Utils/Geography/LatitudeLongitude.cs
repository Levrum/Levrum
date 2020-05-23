using Levrum.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Geography
{
    public class LatitudeLongitude
    {
        public static LatitudeLongitude Default = new LatitudeLongitude(0.0, 0.0);

        private double m_latitude = 0.0;
        private double m_longitude = 0.0;

        public double Latitude 
        { 
            get 
            { 
                return m_latitude; 
            } 
            set 
            {
                if (value > 90.0 || value < -90.0)
                {
                    throw new ArgumentOutOfRangeException("Latitudes must be between -90.0 and 90.0 degrees");
                }

                m_latitude = value;
            } 
        }

        public double Longitude
        {
            get
            {
                return m_longitude;
            }
            set
            {
                if (value > 180.0 || value < -180.0)
                {
                    throw new ArgumentOutOfRangeException("Longitudes must be between -180.0 and 180.0 degrees");
                }

                m_longitude = value;
            }
        }

        public LatitudeLongitude()
        {

        }

        public LatitudeLongitude(double _latitude, double _longitude)
        {
            Latitude = _latitude;
            Longitude = _longitude;
        }

        public override string ToString()
        {
            return string.Format("Lat={0}, Long={1}", m_latitude, m_longitude);
        }

        public override int GetHashCode()
        {
            return m_latitude.GetHashCode() & m_longitude.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LatitudeLongitude))
            {
                return false;
            }

            return this == (obj as LatitudeLongitude);
        }

        public static bool operator ==(LatitudeLongitude point1, LatitudeLongitude point2)
        {
            return point1.Latitude == point2.Latitude && point1.Longitude == point2.Longitude;
        }

        public static bool operator !=(LatitudeLongitude point1, LatitudeLongitude point2)
        {
            return !(point1 == point2);
        }

        public double DistanceFrom(LatitudeLongitude otherPoint)
        {
            double radiusInFeet = 20902464.0;
            double pi180 = (Math.PI / 180);
            double p1 = Latitude * pi180;
            double p2 = otherPoint.Latitude * pi180;
            double deltaP = (otherPoint.Latitude - Latitude) * pi180;
            double deltaL = (otherPoint.Longitude - Longitude) * pi180;
            double a = Math.Sin(deltaP / 2) * Math.Sin(deltaP / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(deltaL / 2) * Math.Sin(deltaL / 2);
            double distanceInRadians = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return radiusInFeet * distanceInRadians;
        }

        public static implicit operator Point2(LatitudeLongitude input)
        {
            return new Point2(input.Longitude, input.Latitude);
        }

        public static implicit operator LatitudeLongitude(Point2 input)
        {
            return new LatitudeLongitude(input.Y, input.X);
        }
    }
}
