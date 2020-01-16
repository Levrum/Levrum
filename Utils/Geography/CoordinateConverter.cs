using System;
using System.Collections.Generic;
using System.Text;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Levrum.Utils.Geography
{
    public class CoordinateConverter
    {
        public static CoordinateSystem WGS84 { get; protected set; } = GeographicCoordinateSystem.WGS84;
        public static CoordinateSystem WebMercator { get; protected set; } = ProjectedCoordinateSystem.WebMercator;

        public static MathTransform LonLatToWebTransform { get; set; }
        public static MathTransform WebToLonLatTransform { get; set; }

        public CoordinateSystem CoordinateSystem { get; protected set; }

        public MathTransform XYToLonLatTransform { get; protected set; }
        public MathTransform XYToWebTransform { get; protected set; }

        public MathTransform LonLatToXYTransform { get; protected set; }
        public MathTransform WebToXYTransform { get; protected set; }

        static CoordinateConverter()
        {
            CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();
            LonLatToWebTransform = ctFactory.CreateFromCoordinateSystems(WGS84, WebMercator).MathTransform;
            WebToLonLatTransform = LonLatToWebTransform.Inverse();
        }

        public CoordinateConverter(string _projection)
        {
            CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
            CoordinateSystem = csFactory.CreateFromWkt(_projection);

            CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();
            
            XYToLonLatTransform = ctFactory.CreateFromCoordinateSystems(CoordinateSystem, WGS84).MathTransform;
            if (CoordinateSystem.WKT != WGS84.WKT)
            {
                LonLatToXYTransform = XYToLonLatTransform.Inverse();
            } else
            {
                LonLatToXYTransform = XYToLonLatTransform;
            }

            XYToWebTransform = ctFactory.CreateFromCoordinateSystems(CoordinateSystem, WebMercator).MathTransform;
            if (CoordinateSystem.WKT != WebMercator.WKT)
            {
                WebToXYTransform = XYToWebTransform.Inverse();
            } else
            {
                WebToXYTransform = XYToWebTransform;
            }
        }

        public double[] ConvertLatLonToXY(LatitudeLongitude latLon)
        {
            try
            {
                double[] lonLatPoint = { latLon.Longitude, latLon.Latitude };

                return LonLatToXYTransform.Transform(lonLatPoint);
            } catch (Exception ex)
            {
                return new double[] { double.NaN, double.NaN };
            }
        }

        public LatitudeLongitude ConvertXYToLatLon(double[] xy)
        {
            try
            {
                double[] lonLatPoint = XYToLonLatTransform.Transform(xy);

                return new LatitudeLongitude(lonLatPoint[1], lonLatPoint[0]);
            } catch (Exception ex)
            {
                return LatitudeLongitude.Default;
            }
        }

        public LatitudeLongitude ConvertXYToLatLon(double x, double y)
        {
            double[] xyPoint = { x, y };
            return ConvertXYToLatLon(xyPoint);
        }

        public double[] ConvertXYToWebMercator(double[] xy)
        {
            try
            {
                return XYToWebTransform.Transform(xy);
            } catch (Exception ex)
            {
                return new double[] { double.NaN, double.NaN };
            }
        }

        public double[] ConvertXYToWebMercator(double x, double y)
        {
            double[] xyPoint = { x, y };
            return ConvertXYToWebMercator(xyPoint);
        }

        public double[] ConvertWebMercatorToXY(double[] webMercatorXY)
        {
            try
            {
                return WebToXYTransform.Transform(webMercatorXY);
            } catch (Exception ex)
            {
                return new double[] { double.NaN, double.NaN };
            }
        }

        public double[] ConvertWebMercatorToXY(double webMercatorX, double webMercatorY)
        {
            double[] webMercatorPoint = { webMercatorX, webMercatorY };
            return ConvertWebMercatorToXY(webMercatorPoint);
        }

        public static double[] ConvertLatLonToWebMercator(LatitudeLongitude latLon)
        {
            try
            {
                double[] lonLatPoint = { latLon.Longitude, latLon.Latitude };
                return LonLatToWebTransform.Transform(lonLatPoint);
            } catch (Exception ex)
            {
                return new double[] { double.NaN, double.NaN };
            }

        }

        public static LatitudeLongitude ConvertWebMercatorToLatLon(double[] webMercatorXY)
        {
            try
            {
                double[] lonLatPoint = WebToLonLatTransform.Transform(webMercatorXY);
                return new LatitudeLongitude(lonLatPoint[1], lonLatPoint[0]);
            } catch (Exception ex)
            {
                return LatitudeLongitude.Default;
            }
        }

        public LatitudeLongitude ConvertWebMercatorToLatLon(double webMercatorX, double webMercatorY)
        {
            double[] webMercatorPoint = { webMercatorX, webMercatorY };
            return ConvertWebMercatorToLatLon(webMercatorPoint);
        }
    }
}
