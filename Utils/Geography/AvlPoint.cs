using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Geography
{
    public class AvlPoint
    {
        //Fake?
        public string ID { get { return string.Format("{0}{1}{2}", UnitID, TimeStampUTC.ToString("MMddyyyyHHmmss"), Observation); } } //ID = UnitID + Month + Day + Year + Hour + Minute + Second + _observation count that day
        public int Observation { get; set; } = -1;

        //Straight from AVL data
        public string UnitID { get; set; } = string.Empty;
        public string VehicleID { get; set; } = string.Empty;
        public string MDCHexIP { get; set; } = string.Empty; //no clue what this is...
        public string UnitStatus { get; set; } = string.Empty;
        public string IncidentID { get; set; } = string.Empty;
        public LatitudeLongitude Location { get; set; } = new LatitudeLongitude();
        public double Latitude { get { return Location.Latitude; } set { Location.Latitude = value; } }
        public double Longitude { get { return Location.Longitude; } set { Location.Longitude = value; } }
        public double Heading { get; set; } = double.NaN;
        public DateTime TimeStampUTC { get; set; } = DateTime.MinValue;

        //OSRM Nearest Outputs
        public string OSMNodeID { get; set; } = string.Empty; //Really is the two nodes that this is between.
        public string OSMWayID { get; set; } = string.Empty;
        public double OSRMLatitude { get; set; } = double.NaN;
        public double OSRMLongitude { get; set; } = double.NaN;

        //Location Voting
        public int RouteID { get; set; } = -1;
        public string IntersectionSegmentVote { get; set; } = string.Empty;
        public double DistanceFromBlockStartMiles { get; set; } = 0.0;
        public string BlockID { get; set; } = string.Empty;

        //Calculated Fields
        public string TimeBin { get; set; } = string.Empty;
        public string PreviousTopology { get; set; } = "Unknown";
        public string NextTopology { get; set; } = "Unknown";
        public string UnitType { get; set; } = string.Empty;
        public double CalculatedHeading { get; set; } = 0.0;
        public double HeadingDelta { get; set; } = 0.0;
        public double ExpandedHeadingDelta { get; set; } = 0.0; //Adds in the previous and next turn's deltas(more complete picture of turn). Uses calculated heading for simplicity
        public double X { get; set; } = double.NaN;
        public double Y { get; set; } = double.NaN;
        public double Speed { get; set; } = double.NaN;
        public double OSRMSpeed { get; set; } = double.NaN;
        public double SpeedDelta { get; set; } = double.NaN; // = current speed - previous speed
        public double SpeedDeltaPercentage { get; set; } = double.NaN; // = (current speed - previous speed) / previous speed. Positive is acceleration and negative is deceleration
        public bool IsEmergent { get; set; } = false;
        public bool OriginalHeadingIsSuspicious { get; set; }

        // Calculated route context:
        public DateTime TimeStampLocal { get; set; } = DateTime.MinValue;
        public double CumElapsedSec { get; internal set; } = 0.0;
        public double DistanceFromRouteStartFeet { get; set; } = 0.0;

        #region Public Methods
        public static double FeetBetween(AvlPoint oPrev, AvlPoint oCur)
        {
            return oPrev.Location.DistanceFrom(oCur.Location);
        }

        internal string GetPrevIntersectionId()
        {
            string sid = BlockID;
            if (string.IsNullOrEmpty(sid)) { return (""); }
            string[] pieces = sid.Split('_');
            if ((null == pieces) || (pieces.Length < 1) || (null == pieces[0])) { return (""); }
            return (pieces[0]);
        }

        internal string GetNextIntersectionId()
        {
            string sid = BlockID;
            if (string.IsNullOrEmpty(sid)) { return (""); }
            string[] pieces = sid.Split('_');
            if ((null == pieces) || (pieces.Length < 2) || (null == pieces[1])) { return (""); }
            return (pieces[1]);
        }
        #endregion
    }
}
