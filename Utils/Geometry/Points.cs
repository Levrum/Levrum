using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Geometry
{

    public class IntPoint2
    {
        public IntPoint2(int iX, int iY)
        {
            X = iX;
            Y = iY;
        }
        public int X = 0;
        public int Y = 0;
    }

    public class IntPoint2Z : IntPoint2
    {
        public IntPoint2Z(int iX, int iY)
            : base(iX, iY)
        {
            Intensity = 1.0;
        }

        public IntPoint2Z(int iX, int iY, double dIntensity)
            : base(iX, iY)
        {
            Intensity = dIntensity;
        }

        public double Intensity = 1.0;
    }



    /// <summary>
    /// A 2-dimensional point
    /// </summary>
    public class Point2 : ICloneable
    {

        public virtual bool Equals(Point2 oRhs)
        {
            return ((this.X == oRhs.X) && (this.Y == oRhs.Y));
        }

        public virtual double GetRawAngle()
        {
            if ((0.0 == Y) && (X < 0)) { return (Math.PI); }    // special case for "west"...
            return (Math.Atan2(Y, X));
        }

        public Point2()
        {
            X = 0.0;
            Y = 0.0;
        }

        public Point2(double dX, double dY)
        {
            X = dX;
            Y = dY;
        }


        public override string ToString()
        {
            String sret = "<" + Math.Round(X, 2) + "," + Math.Round(Y, 2) + ">";
            return (sret);
        }
        public double X = 0.0;

        public double Y = 0.0;


        internal double DistanceTo(Point2 oDestination)
        {
            double dx = oDestination.X - this.X;
            double dy = oDestination.Y - this.Y;
            double dsquared = ((dx * dx) + (dy * dy));
            double d = Math.Sqrt(dsquared);
            return (d);
        }

        /// <summary>
        /// Vector subtraction.
        /// </summary>
        /// <param name="oRt"></param>
        /// <returns></returns>
        public virtual Point2 Subtract(Point2 oRt)
        {
            return (new Point2(X - oRt.X, Y - oRt.Y));
        }

        /// <summary>
        /// The "right-hand-rule" orthogonal to the current vector.
        /// </summary>
        /// <returns></returns>
        public virtual Point2 OrthogonalRight()
        {
            return (new Point2(Y, -X));
        }

        /// <summary>
        /// Vector addition.
        /// </summary>
        /// <param name="oRt"></param>
        /// <returns></returns>
        public virtual Point2 Add(Point2 oRt)
        {
            return (new Point2(X + oRt.X, Y + oRt.Y));
        }

        /// <summary>
        /// Scalar multiplication of a vector.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public virtual Point2 ScalarMult(double dMultiple)
        {
            return (new Point2(X * dMultiple, Y * dMultiple));
        }

        /// <summary>
        /// Are three points co-linear?
        /// </summary>
        /// <param name="oOrigin"></param>
        /// <param name="oMiddle"></param>
        /// <param name="oDestination"></param>
        /// <returns></returns>
        public static bool CoLinear(Point2 oOrigin, Point2 oMiddle, Point2 oDestination, double dToleranceRatio)
        {
            if ((oOrigin.X == oMiddle.X) && (oMiddle.X == oDestination.X)) { return (true); }   // true if they're on the same horizontal line

            double slope1 = (oMiddle.Y - oOrigin.Y) / (oMiddle.X - oOrigin.X);
            double slope2 = (oDestination.Y - oMiddle.Y) / (oDestination.X - oMiddle.X);

            double avgslope = (slope1 + slope2) / 2.0;
            if (0.0 == avgslope)
            {
                if (Math.Abs(slope1 + slope2) < dToleranceRatio) { return (true); }
                return (false);
            }

            double diff_ratio = Math.Abs((slope2 - slope1) / avgslope);

            return (diff_ratio < dToleranceRatio);
        }

        /// <summary>
        /// Rotate a vector by an angle given in radians.
        /// </summary>
        /// <param name="dThetaRadians"></param>
        /// <returns></returns>
        public virtual Point2 Rotate(double dThetaRadians)
        {
            double cur_theta = this.GetRawAngle();
            double norm = this.GetNorm();

            double new_theta = cur_theta + dThetaRadians;
            double new_x = norm * Math.Cos(new_theta);
            double new_y = norm * Math.Sin(new_theta);
            Point2 newpt = new Point2(new_x, new_y);
            return (newpt);
        }

        /// <summary>
        /// Calculate the Cartesian norm of the vector.
        /// </summary>
        /// <returns></returns>
        public virtual double GetNorm()
        {
            return (Math.Sqrt((X * X) + (Y * Y)));
        }

        public object Clone()
        {
            Point2 p2_new = new Point2(X, Y);
            return (p2_new);
        }
    }

    /// <summary>
    /// A Point2 that has an independent Z value.  The Z value is not part of the geometry, but is a scalar intensity value.
    /// </summary>
    public class Point2Z : Point2
    {
        public Point2Z(double dX, double dY)
        {
            X = dX;
            Y = dY;
            Intensity = 1.0;
        }


        public Point2Z(double dX, double dY, double dIntensity)
        {
            X = dX;
            Y = dY;
            Intensity = dIntensity;
        }

        public double Intensity = 1.0;
    }
}
