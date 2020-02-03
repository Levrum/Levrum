using System;
using System.Collections.Generic;
using System.Text;

using ClipperLib;

// using Levrum.Data.Classes;


namespace Levrum.Utils.Geometry
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class ComplexPolygon
    {
        public Polygon Polygon = new Polygon();

        public List<Polygon> SubPolygons = new List<Polygon>();

        public List<Polygon> Polygons
        {
            get
            {
                List<Polygon> outList = new List<Polygon>();
                outList.Add(Polygon);
                outList.AddRange(SubPolygons);

                return outList;
            }
        }

        private double m_area = double.NaN;

        public double Area { 
            get
            {
                if (m_area == double.NaN)
                {
                    m_area = ComputeArea();
                }
                return m_area;
            } 
        }

        /// <summary>
        /// Add a point to the polygon.
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="dY"></param>
        public virtual void AddPoint(double dX, double dY)
        {
            Polygon.Points.Add(new Point2(dX, dY));
        }

        public double ComputeArea()
        {
            if (SubPolygons.Count == 0)
            {
                return Polygon.GetArea();
            }

            Clipper c = new Clipper();
            Path polyPath = new Path();
            foreach (Point2 point in Polygon.Points)
                polyPath.Add(new IntPoint(point.X, point.Y));

            c.AddPath(polyPath, PolyType.ptSubject, true);

            foreach (Polygon poly in SubPolygons)
            {
                polyPath = new Path();
                foreach (Point2 point in poly.Points)
                    polyPath.Add(new IntPoint(point.X, point.Y));

                c.AddPath(polyPath, PolyType.ptSubject, true);
            }

            Paths solution = new Paths();
            c.Execute(ClipType.ctXor, solution, PolyFillType.pftEvenOdd);

            double clipperArea = 0.0;
            foreach (Path p in solution)
                clipperArea += Clipper.Area(p);

            return clipperArea;
        }

        public Point2 GenerateRandomLoc()
        {
            List<Point2> points = Polygon.Points;

            int maxTries = 100;
            // Up to N tries to get a point on the connector between two random vertices.
            for (int i = 0; i < maxTries; i++)
            {
                int index1 = m_oRandom.Next(points.Count);
                if (1 >= points.Count)
                {
                    return points[index1];
                }

                int index2 = index1;
                while (index2 == index1) index2 = m_oRandom.Next(points.Count);

                double fraction = m_oRandom.NextDouble();
                double rx = points[index1].X + (fraction * (points[index2].X - points[index1].X));
                double ry = points[index1].Y + (fraction * (points[index2].Y - points[index1].Y));
                Point2 point = new Point2(rx, ry);

                bool goodLocation = Polygon.Contains(point);

                foreach (Polygon poly in SubPolygons)
                {
                    goodLocation = goodLocation ^ poly.Contains(rx, ry); // If an odd number of polygons contains the location, it's good, so use XOR.
                }
                if (goodLocation)
                    return point;
            }

            return (null);
        }

        Random m_oRandom = new Random();

        public virtual bool Contains(double dX, double dY)
        {
            Point2 p2 = new Point2(dX, dY);
            if (null == this.Polygon) { return (false); }

            bool containsPoint = Polygon.Contains(p2);
            foreach (Polygon subPoly in SubPolygons)
                containsPoint = containsPoint ^ subPoly.Contains(p2); // If an odd number of polygons contains the location, it's good, so use XOR.

            return containsPoint;
        }

        /// <summary>
        /// Get the centroid of a polygon.
        /// </summary>
        /// <returns></returns>
        public virtual Point2 GetCentroid()
        {
            try
            {
                Point2 default_point = new Point2(0.0, 0.0);
                //Stats xstats = new Stats();
                //Stats ystats = new Stats();
                //foreach (Point2 p2 in this.Polygon.Points)
                //{
                //    xstats.AddObs(p2.X);
                //    ystats.AddObs(p2.Y);
                //}
                //Point2 centroid = new Point2(xstats.Mean, ystats.Mean);
                //return (centroid);
                double area = this.Polygon.GetArea();
                if (0.0 == area) { return (default_point); }
                double xsum = 0.0, ysum = 0.0;
                List<Point2> dps = new List<Point2>();
                dps.AddRange(Polygon.Points);
                dps.Add(Polygon.Points[0]);
                for (int i = 0; i < dps.Count - 1; i++)
                {
                    Point2 p2cur = dps[i];
                    Point2 p2nxt = dps[i + 1];
                    double crossprod = (p2cur.X * p2nxt.Y) - (p2nxt.X * p2cur.Y);
                    double xfactor = (p2cur.X + p2nxt.X);
                    double yfactor = (p2cur.Y + p2nxt.Y);
                    double xterm = crossprod * xfactor;
                    double yterm = crossprod * yfactor;
                    xsum += xterm;
                    ysum += yterm;
                }
                double finalx = Math.Abs(xsum / (6.0 * area));
                double finaly = Math.Abs(ysum / (6.0 * area));

                Point2 retpt = new Point2(finalx, finaly);
                return (retpt);
            }
            catch (Exception exc)
            {
                return (null);
            }
        }

        /// <summary>
        /// Get the maximum distance from the centroid to an edge vertex in miles.
        /// </summary>
        /// <returns></returns>
        public double GetMaxRadiusMiles()
        {
            try
            {
                Point2 centroid = this.GetCentroid();
                double max_dist2 = 0.0;
                foreach (Point2 p in this.Polygon.Points)
                {
                    double dx = p.X - centroid.X;
                    double dy = p.Y - centroid.Y;
                    double dist2 = (dx * dx) + (dy * dy);
                    if (dist2 > max_dist2) { max_dist2 = dist2; }

                }
                double retdist = Math.Sqrt(max_dist2) / 5280.0;  // assumes polygons are in feet
                return (retdist);
            }
            catch (Exception exc)
            {
                return -1.0;
            }

        }
    }

    public class ComplexPolygonAreaComparer : IComparer<ComplexPolygon>
    {
        public int Compare(ComplexPolygon p1, ComplexPolygon p2)
        {
            if (p1.Area == p2.Area)
                return 0;
            if (p1.Area < p2.Area)
                return -1;

            return 1;
        }
    }
}
