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

        public static string GetProjectionName(string _projection)
        {
            CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
            CoordinateSystem system = csFactory.CreateFromWkt(_projection);

            string output = string.Empty;
            if (!string.IsNullOrEmpty(system.Name))
            {
                output = system.Name;
            } else if (!string.IsNullOrEmpty(system.Abbreviation))
            {
                output = system.Abbreviation;
            } else if (!string.IsNullOrEmpty(system.Alias))
            {
                output = system.Alias;
            } else if (!string.IsNullOrEmpty(system.Authority))
            {
                output = string.Format("{0}:{1}", system.Authority, system.AuthorityCode);
            }
            return output;
        }

        public CoordinateConverter(string _projection)
        {
            try
            {
                CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
                CoordinateSystem = csFactory.CreateFromWkt(_projection);

                CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();

                XYToLonLatTransform = ctFactory.CreateFromCoordinateSystems(CoordinateSystem, WGS84).MathTransform;
                LonLatToXYTransform = ctFactory.CreateFromCoordinateSystems(WGS84, CoordinateSystem).MathTransform;

                XYToWebTransform = ctFactory.CreateFromCoordinateSystems(CoordinateSystem, WebMercator).MathTransform;
                WebToXYTransform = ctFactory.CreateFromCoordinateSystems(WebMercator, CoordinateSystem).MathTransform;
            } catch (Exception ex)
            {
                if (_projection.ToLowerInvariant().Contains("web_mercator"))
                {
                    CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();
                    LonLatToXYTransform = ctFactory.CreateFromCoordinateSystems(WGS84, WebMercator).MathTransform;
                    XYToLonLatTransform = LonLatToXYTransform.Inverse();
                } else
                {
                    throw ex;
                }
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
