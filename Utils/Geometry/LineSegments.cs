using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Geometry
{
    public class LineSegment2 : ICloneable
    {
        public Point2 PointA { get; set; } = null;
        public Point2 PointB { get; set; } = null;

        public LineSegment2()
        {

        }

        public LineSegment2(Point2 _a, Point2 _b)
        {
            PointA = _a;
            PointB = _b;
        }

        public LineSegment2(double x1, double y1, double x2, double y2)
        {
            PointA = new Point2(x1, y1);
            PointB = new Point2(x2, y2);
        }

        public Point2 Intersection(LineSegment2 otherSegment)
        {
            return Intersection(this, otherSegment);
        }

        public bool IntersectsWith(LineSegment2 otherSegment)
        {
            return Intersection(otherSegment) != null;
        }

        public static Point2 Intersection(LineSegment2 segment1, LineSegment2 segment2)
        {
            return Intersection(segment1.PointA.X, segment1.PointA.Y, segment1.PointB.X, segment1.PointB.Y, segment2.PointA.X, segment2.PointA.Y, segment2.PointB.X, segment2.PointB.Y);
        }

        public static Point2 Intersection(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            return Intersection(a.X, a.Y, b.X, b.Y, c.X, c.Y, d.X, d.Y);
        }

        public static Point2 Intersection(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double[] output = new double[2] { double.NaN, double.NaN };
            double uA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
            double uB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            {
                output[0] = x1 + (uA * (x2 - x1));
                output[1] = y1 + (uA * (y2 - y1));

                return new Point2(output[0], output[1]);
            }
            else
            {
                return null;
            }
        }

        public object Clone()
        {
            Point2 a_new = (Point2)PointA.Clone();
            Point2 b_new = (Point2)PointB.Clone();
            return new LineSegment2() { PointA = a_new, PointB = b_new };
        }
    }
}
