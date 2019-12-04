﻿using System;
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

                m_latitude = Math.Round(value, 6); 
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

                m_longitude = Math.Round(value, 6);
            }
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
    }
}