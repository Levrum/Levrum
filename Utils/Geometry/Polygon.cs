﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using ClipperLib;

namespace Levrum.Utils.Geometry
{
    /// <summary>
    /// General polygon operations.
    /// </summary>
    public class Polygon : ICloneable
    {

        /// <summary>
        /// Possible directions for traversing edge nodes in order.
        /// </summary>
        public enum TraversalDirections
        {
            Clockwise, CounterClockwise, Unknown
        }

        /// <summary>
        /// List of points comprising the polygon.
        /// </summary>
        public List<Point2> Points = new List<Point2>();



        /// <summary>
        /// List of points generated by various operations (for debugging purposes).
        /// </summary>
        public List<Point2> DebugPoints = new List<Point2>();

		// Algorithm courtesy of http://alienryderflex.com/polygon/

        private double[] m_constant;
        private double[] m_multiple;
        private bool m_precalcDone = false;

        private long[] m_longConstant;
        private long[] m_longMultiple;

        List<IntPoint> m_path = new List<IntPoint>();

        private void precalcValues()
        {
            lock (m_path)
            {
                if (m_precalcDone)
                    return;

                for (int i = 0; i < Points.Count; i++)
                {
                    long pX = (long)(Points[i].X * 10000000.0);
                    long pY = (long)(Points[i].Y * 10000000.0);
                    m_path.Add(new IntPoint(pX, pY));
                }

                m_precalcDone = true;
            }
        }

        private bool pointInPolygon(Point2 point)
        {
            if (!m_precalcDone)
                precalcValues();

            long pointLongY = (long)(point.Y * 10000000.0);
            long pointLongX = (long)(point.X * 10000000.0);
            IntPoint intPoint = new IntPoint(pointLongX, pointLongY);
            return Clipper.PointInPolygon(intPoint, m_path) != 0;
        }

        /// <summary>
        /// Clear the point list for this polygon.
        /// </summary>
        /// <returns></returns>
        public virtual bool ClearPoints()
        {
            if (null == Points) { Points = new List<Point2>(); }
            Points.Clear();
            return (true);
        }


        /// <summary>
        /// Add the point with the specified coordinates to the polygon.
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="dY"></param>
        /// <returns></returns>
        public virtual bool AddPoint(double dX, double dY)
        {
            Point2 p2 = new Point2(dX, dY);
            Points.Add(p2);
            return (true);
        }

        /// <summary>
        /// Does this polygon contain the point whose coordinates are specified?
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="dY"></param>
        /// <returns></returns>
        public virtual bool Contains(double dX, double dY)
        {
            Point2 p2 = new Point2(dX, dY);
            return (Contains(p2));
        }

        /// <summary>
        /// Does the polygon contain the point?
        /// </summary>
        /// <param name="oPoint"></param>
        /// <returns></returns>
        public virtual bool Contains(Point2 oPoint)
        {
            return pointInPolygon(oPoint);            
        }  // end Contains()
        
        /// <summary>
        /// Get the bounding rectangle for this polygon.
        /// </summary>
        /// <returns></returns>
        public virtual Rect2 GetBoundingRectangle()
        {
            double xmax = Double.MinValue;
            double xmin = Double.MaxValue;
            double ymax = Double.MinValue;
            double ymin = Double.MaxValue;

            foreach (Point2 point in Points)
            {
                xmax = Math.Max(point.X,xmax);
                xmin = Math.Min(point.X,xmin);
                ymax = Math.Max(point.Y,ymax);
                ymin = Math.Min(point.Y,ymin);
            }
            Rect2 bounder = new Rect2();
            bounder.UpperLeft = new Point2(xmin,ymax);
            bounder.LowerRight = new Point2(xmax,ymin);
            return(bounder);
        }

        public virtual Point2 GetApproxCenter()
        {
            Rect2 bounder = GetBoundingRectangle();
            return(bounder.Center);
        }

        /// <summary>
        /// This property is true if the polygon's boundary is simple ... i.e., does not cross itself.
        /// </summary>
        public virtual bool HasSimpleBoundary(out string invalidedges)
        {
            invalidedges = "";

            List<LineSegment2> edges = GetEdges();
            int n = edges.Count;
            bool bl_polygon_is_not_intersecting = true;
                
            // Loop through the edges and compare all pairs that are not adjacent:
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 2; j < n; j++)
                {
                    // Skip adjacent edge pairs at list boundary:
                    if ((0 == i) && (j == (n - 1))) { continue; }
                    if (edges[i].IntersectsWith(edges[j])) 
                    {
                        invalidedges = invalidedges + "(" + (i + 1).ToString() + ", " + (j).ToString() + ")";

                        bl_polygon_is_not_intersecting = false; 
                    }
                }
            }

            if (bl_polygon_is_not_intersecting == true)
            {
                // If we get through all the edge pairs, we're safe:
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Get the line segments consisting of all the edges of this polygon.
        /// </summary>
        /// <returns></returns>
        private List<LineSegment2> GetEdges()
        {
            List<LineSegment2> retlist = new List<LineSegment2>();
            if ((null == Points) || (2 > Points.Count)) { return (retlist); }

            Point2 firstpoint = Points[0];
            Point2 lastpoint = Points[Points.Count - 1];
            Point2 curpoint = firstpoint;


            for (int i = 1; i < Points.Count; i++)
            {
                Point2 nextpoint = Points[i];
                LineSegment2 seg = new LineSegment2(curpoint, nextpoint);
                retlist.Add(seg);
                curpoint = nextpoint;
            }

            retlist.Add(new LineSegment2(lastpoint, firstpoint));

            return (retlist);
        }




        /// <summary>
        /// Calculate the area of the polygon using the algorithm published by
        /// Darel Rex Finley at: http://alienryderflex.com/polygon_area/
        /// as of 1/3/2010.    Don't understand the algorithm, but I expect it has to
        /// do with vector dot products ... I think I remember something like this from
        /// linear algebra.
        /// </summary>
        /// <returns></returns>
        public virtual double GetArea()
        {
            double area = 0.0;
            for (int i=0; i<Points.Count; i++)
            {
                Point2 p1 = Points[i];
                Point2 p2 = Points[(i+1)%Points.Count];
                double increment = 
                    (p1.X+p2.X) * (p1.Y-p2.Y);
                area += increment;
            }

            double final_area = Math.Abs(area/2.0);
            return(final_area);
        }

        /// <summary>
        /// Does this polygon contain another polygon?
        /// </summary>
        /// <param name="oOtherPolygon"></param>
        /// <returns></returns>
        public virtual bool Contains(Polygon oOtherPolygon)
        {
            DebugPoints.Clear();

            foreach(Point2 otherpoint in oOtherPolygon.Points)
            {
                if (!Contains(otherpoint)) 
                {
                    DebugPoints.Add(otherpoint);
                    return(false);
                }
            }
            return(true);
        } // end Contains(polygon)

        /// <summary>
        /// Find the point (if any) at which a horizontal line with a specified Y intersects a 
        /// line segment.
        /// </summary>
        /// <param name="iHorizY"></param>
        /// <param name="oPoint1"></param>
        /// <param name="oPoint2"></param>
        /// <param name="bIsOnSegment">Returns a value telling whether or not the intersection is on the specified segment.</param>
        /// <returns></returns>
        protected virtual Point2 GetHorizontalIntersect(double dHorizY,Point2 oPoint1,Point2 oPoint2, out bool bIsOnSegment)
        {
            double y = dHorizY;
            double a = oPoint1.X;
            double b = oPoint1.Y;
            double c = oPoint2.X;
            double d = oPoint2.Y;

            bIsOnSegment = false;

            // If Y components are equal, segment is horizontal;  check X.
            if (d==b)
            {
                if (d==dHorizY) // if the user actually hit the exact latitude of the segment, return its midpoint...
                {
                    Point2 retpt1 = new Point2((a+c)/2.0,dHorizY);
                    return(retpt1);
                }
                else  // ... otherwise, it's a miss.
                {
                    return(null);
                }
            }

            double ratio = (y-b)/(d-b);

            double xi = a + (ratio * (c-a));
            double yi = b + (ratio * (d-b));

            bIsOnSegment = false;
            if (a<c) bIsOnSegment = ((a<=xi)&&(xi<=c));
            else if (a>c) bIsOnSegment = ((a>=xi)&&(xi>=c));
            
            // If the X components are equal, we special-case for a vertical segment:
            else 
            {

                bIsOnSegment = (a==xi) && areInOrder(b,yi,d);
            }

            Point2 retpt = new Point2(xi,yi);
            return(retpt);
        } // end GetHorizontalIntersect()

        private static bool areInOrder(double dLeft, double dMiddle, double dRight)
        {
            if (dLeft <= dRight) return ((dLeft <= dMiddle) && (dMiddle <= dRight));
            else return ((dLeft >= dMiddle) && (dMiddle >= dRight));
        }

        /// <summary>
        /// Find the vertex closest to the specified target.
        /// </summary>
        /// <param name="oTarget"></param>
        /// <param name="roDistance"></param>
        /// <returns></returns>
        public Point2 FindClosestVertex(Point2 oTarget, out double roDistance)
        {
            roDistance = 0.0;
            double min_dsquared = double.MaxValue;
            Point2 sel_point = null;
            foreach (Point2 p2 in this.Points)
            {
                double dx = p2.X - oTarget.X;
                double dy = p2.Y - oTarget.Y;
                double dsquared = ((dx * dx) + (dy * dy));
                if (min_dsquared > dsquared)
                {
                    sel_point = p2;
                    min_dsquared = dsquared;
                }
            } // end foreach(p2)

            if (null != sel_point)
            {
                roDistance = Math.Sqrt(min_dsquared);
            }
            else
            {
                roDistance = double.MaxValue;
                return (null);
            }

            Point2 retpoint = new Point2(sel_point.X, sel_point.Y);
            return (retpoint);
        }


        /// <summary>
        /// Given a target vertex on the boundary set of the polygon, find the two adjacent vertices.
        /// The "left" neighbor is defined as the predecessor in clockwise traversal;  the "right" neighbor
        /// is the clockwise successor.
        /// </summary>
        /// <param name="oTargetVertex"></param>
        /// <param name="roLeftNeighbor"></param>
        /// <param name="roRightNeighbor"></param>
        /// <returns></returns>
        public bool GetAdjacentVertices(Point2 oTargetVertex, out Point2 roLeftNeighbor, out Point2 roRightNeighbor)
        {
            roLeftNeighbor = roRightNeighbor = null;
            double tgtx = oTargetVertex.X, tgty = oTargetVertex.Y;
            int curix = -1, prvix = -1, nxtix = -1;
            for (int i = 0; i < Points.Count; i++)
            {
                Point2 curpoint = Points[i];
                if ((curpoint.X == tgtx) && (curpoint.Y == tgty))
                {
                    curix = i;
                    nxtix = i + 1;
                    if (nxtix >= Points.Count) { nxtix = 0; }
                    prvix = i - 1;
                    if (prvix < 0) { prvix = Points.Count - 1; }
                    Point2 prev = Points[prvix];
                    Point2 next = Points[nxtix];

                    // Figure it out by traversal direction:
                    TraversalDirections qdir = GetTraversalDirection(prev, curpoint, next);
                    if (TraversalDirections.Clockwise == qdir)
                    {
                        roLeftNeighbor = new Point2(next.X, next.Y);
                        roRightNeighbor = new Point2(prev.X, prev.Y);
                        return (true);
                    }
                    else if (TraversalDirections.CounterClockwise == qdir)
                    {
                        roLeftNeighbor = new Point2(prev.X, prev.Y);
                        roRightNeighbor = new Point2(next.X, next.Y);
                        return (true);
                    }
                    else
                    {
                        return (false);
                    }



                    // Other option: angles from polygon center.
                    //Point2 ctr = GetApproxCenter();
                    //double theta_p = CalcVectorAngle(ctr, prev);
                    //double theta_n = CalcVectorAngle(ctr, next);
                    //if (theta_p < theta_n)
                    //{
                    //    roLeftNeighbor = new DoublePoint(next.X, next.Y);
                    //    roRightNeighbor = new DoublePoint(prev.X, prev.Y);
                    //}
                    //else
                    //{
                    //    roLeftNeighbor = new DoublePoint(prev.X, prev.Y);
                    //    roRightNeighbor = new DoublePoint(next.X, next.Y);
                    //}
                    //return (true);
                } // endif(hit target point)
            } // end for(i=point index)

            // if we get here, we failed to find the target point:
            return (false);
        }

        /// <summary>
        /// Determine the direction of traversal betwen an origin and destination vertex.   The vertices are assumed
        /// to lie on the perimeter of the polygon.
        /// </summary>
        /// <param name="oOrigin"></param>
        /// <param name="oDestination"></param>
        /// <returns></returns>
        private TraversalDirections GetTraversalDirection(Point2 oOrigin, Point2 oMiddle, Point2 oDestination)
        {
            double adj_multiple = 0.01;      // scalar multiple for tiny vector offsets

            // Figure out the bisector of the angle made from (origin,middle,destination):
            Point2 half_travel_vector = oDestination.Subtract(oOrigin).ScalarMult(0.5);
            Point2 halfway_point = oOrigin.Add(half_travel_vector);
            Point2 bisector = halfway_point.Subtract(oMiddle);

            // If the midpoint is in line with origin and destination, make the bisector orthogonal:
            if (Point2.CoLinear(oOrigin,oMiddle,oDestination,0.001))
            {
                bisector = oMiddle.Subtract(oOrigin).OrthogonalRight().ScalarMult(0.5);
            }

            // Add a tiny fraction of that vector to the middle point, and also a tiny fraction
            // of the inverse of that vector:
            Point2 offset1 = bisector.ScalarMult(adj_multiple);
            Point2 offset2 = offset1.ScalarMult(-1.0);
            Point2 testpoint1 = oMiddle.Add(offset1);
            Point2 testpoint2 = oMiddle.Add(offset2);
            Point2 interior_pt = null;

            // Test both points to see which is in the polygon:
            bool b1interior = this.Contains(testpoint1);
            bool b2interior = this.Contains(testpoint2);
            if (b1interior && b2interior)
            {
                return (TraversalDirections.Unknown);
            }
            else if ((!b1interior) && (!b2interior))
            {
                return (TraversalDirections.Unknown);
            }
            else if (b1interior) { interior_pt = testpoint1;    }
            else if (b2interior) { interior_pt = testpoint2; }
            else
            {
                return (TraversalDirections.Unknown);
            }

            // Get the vectors from origin to middle and origin to interior point.  
            // Then calculate the angles of each vector.   If the interior point is
            // "right" of the middle point, relative to the original point, we're going counter-clockwise.
            Point2 vectori = interior_pt.Subtract(oMiddle);
            Point2 vectorm = oMiddle.Subtract(oOrigin);
            double anglem = vectorm.GetRawAngle();
            Point2 vectori_rotated = vectori.Rotate(-anglem);   // Align M' along +X axis, shift I' accordingly
            Point2 vectorm_rotated = vectorm.Rotate(-anglem);
                
            // The rotated vector should have a 0 X component, and the Y component should discriminate
            // the traversal direction; if Y<0, it's a right turn, else a left turn.
            if (vectori_rotated.Y < 0) { return (TraversalDirections.CounterClockwise); }
            else { return (TraversalDirections.Clockwise); }
        } // end method()

        /// <summary>
        /// Calculate the vector angle -- in the range [0,2*PI] -- of the vector described by two endpoints.
        /// Angle is measured in radians, counter-clockwise from positive X axis.
        /// </summary>
        /// <param name="oOrigin"></param>
        /// <param name="oDestination"></param>
        /// <returns></returns>
        private double CalcVectorAngle(Point2 oOrigin, Point2 oDestination)
        {
            double deltay = oDestination.Y - oOrigin.Y;
            double norm = oOrigin.DistanceTo(oDestination);
            if (0.0 == norm) { return (0.0); }
            double sine = deltay / norm;
            double arcsin = Math.Asin(sine);
            if (arcsin < 0) { arcsin += (2.0 * Math.PI); }
            return (arcsin);
        }

        /// <summary>
        /// Add the specified vertex between two other vertices.
        /// </summary>
        /// <param name="oNewVertex"></param>
        /// <param name="oNeighbor1"></param>
        /// <param name="oNeighbor2"></param>
        /// <returns></returns>
        public bool InsertVertex(Point2 oNewVertex, Point2 oNeighbor1, Point2 oNeighbor2)
        {
            Point2 n1 = new Point2(oNeighbor1.X, oNeighbor1.Y);
            Point2 n2 = new Point2(oNeighbor2.X, oNeighbor2.Y);
            Point2 pnew = new Point2(oNewVertex.X, oNewVertex.Y);
            int [] indices = new int[2];
            int index_index = 0;
            for (int i = 0; i < Points.Count; i++)
            {
                Point2 p = Points[i];
                if (p.Equals(n1)) { indices[index_index++] = i; }
                else if (p.Equals(n2)) { indices[index_index++] = i;    }
                if (index_index > 1 )  // If we've found both neighbors, we can figure out the index at which to insert:
                {
                    int minix = Math.Min(indices[0],indices[1]);
                    int maxix = Math.Max(indices[0],indices[1]);
                    if (1 == (maxix-minix))
                    {
                        Points.Insert(maxix,pnew);
                        return(true);
                    }
                    else if (0==minix)
                    {
                        Points.Insert(0,pnew);
                        return(true);
                    }
                } // endif(index_index>1 - found both)
            } // end for(i - point index)

            return (false);     // both neighbors not found
        }

        public bool RemoveVertex(Point2 oVertex)
        {
            Point2 tgt = new Point2(oVertex.X, oVertex.Y);
            int victim_index = -1;
            for (int i = 0; i < Points.Count; i++)
            {
                Point2 p = Points[i];
                if (tgt.Equals(p))
                {
                    victim_index = i;
                    break;
                } // endif(found target)
            } // end for(i)
            if (victim_index < 0) { return (false); } // not found
            Points.RemoveAt(victim_index);
            return (true);
        }

        public object Clone()
        {
            Polygon newpoly = new Polygon();
            foreach (Point2 dp in this.Points)
            {
                Point2 dpnew = dp.Clone() as Point2;
                newpoly.Points.Add(dpnew);
            }
            foreach (Point2 dbgp in this.DebugPoints)
            {
                Point2 dbgnew = dbgp.Clone() as Point2;
                newpoly.DebugPoints.Add(dbgnew);
            }
            return (newpoly);
        }
    } // end class Polygon



    /// <summary>
    /// A rectangle.   Upper left and lower right.
    /// </summary>
    public class Rect2
    {
        public Rect2()
        {
        }

        public Rect2(double dUlcX, double dUlcY, double dLrcX, double dLrcY)
        {
            UpperLeft = new Point2(dUlcX, dUlcY);
            LowerRight = new Point2(dLrcX, dLrcY);
        }

        public Point2 UpperLeft= new Point2();
        public Point2 LowerRight = new Point2();

        /// <summary>
        /// Center of the rectangle.
        /// </summary>
        public Point2 Center
        {
            get
            {
                double x = (UpperLeft.X + LowerRight.X) / 2.0;
                double y = (UpperLeft.Y + LowerRight.Y) / 2.0;
                Point2 center = new Point2(x,y);
                return(center);
            }
        }

        public Rect2 MergeBoundingBox(Rect2 that)
        {
            double xmin = Math.Min(this.UpperLeft.X, that.UpperLeft.X);
            double ymin = Math.Min(this.UpperLeft.Y, that.UpperLeft.Y);
            double xmax = Math.Max(this.LowerRight.X, that.LowerRight.X);
            double ymax = Math.Max(this.LowerRight.Y, this.LowerRight.Y);
            Rect2 bb = new Rect2(xmin, ymin, xmax, ymax);
            return (bb);
        }
    }
}
