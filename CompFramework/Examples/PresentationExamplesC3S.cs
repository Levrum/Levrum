using AnalysisFramework.Infrastructure;
using AnalysisFramework.Model.Dashboard;
using AnalysisFramework.Model.Data;
using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;


// This file offers examples of presentation subclasses for Code3 Strategist on Windows.
// It should eventually be moved into the Code3 Strategist environment, which should be refactored
// to use the Levrum classes.

namespace AnalysisFramework.Examples
{
    public class ExampleC3sPresentation : PresentationInfo
    {
        // Common C3S presentation elements here

        public string DisplayCaption = "";


    }

    [Caption("Code3 Strategist Heat Map Presentation")]
    public class ExampleC3sPresentationHeatMap : ExampleC3sPresentation
    {
        [Caption("Lat/Long Coordinates")]
        [Doc("This property determines whether the specified coordinates are in latitude/longitude format (true) or rectangular feet (e.g., state plane; false).\r\n" +
             "In the latitude/longitude case, the X coordinate field specifies the longitude with negative coordinates for the western hemisphere;\r\n" +
             "in the rectangular case, the projection is the one currently configured into the C3S customer license."
            )]
        public bool IsLatLong = true;


        [Caption("X Field")]
        [Doc("*Need to figure out how to do lookups here*.  Please select the field containing the east-west coordinate.")]
        [Editable(SpecialEditType.ObjectLookupCustom)]
        // Need to figure out  [CustomDataSource()] here!
        public string XFieldName = "";


        [Caption("Y Field")]
        [Doc("*Need to figure out how to do lookups here*.  Please select the field containing the north-south coordinate.")]
        [Editable(SpecialEditType.ObjectLookupCustom)]
        // Need to figure out  [CustomDataSource()] here!
        public string YFieldName = "";

        [Caption("Map View Parameters")]
        [Editable]
        public MapViewInfo MapViewParameters = null;


    } // end class


    [Caption("Code3 Strategist Point Map Presentation")]
    public class ExampleC3sPresentationPointMap : ExampleC3sPresentation
    {
        [Caption("Lat/Long Coordinates")]
        [Doc("This property determines whether the specified coordinates are in latitude/longitude format (true) or rectangular feet (e.g., state plane; false).\r\n" +
             "In the latitude/longitude case, the X coordinate field specifies the longitude with negative coordinates for the western hemisphere;\r\n" +
             "in the rectangular case, the projection is the one currently configured into the C3S customer license."
            )]
        public bool IsLatLong = true;


        [Caption("X Field")]
        [Doc("*Need to figure out how to do lookups here*.  Please select the field containing the east-west coordinate.")]
        [Editable(SpecialEditType.ObjectLookupCustom)]
        // Need to figure out  [CustomDataSource()] here!
        public string XFieldName = "";


        [Caption("Y Field")]
        [Doc("*Need to figure out how to do lookups here*.  Please select the field containing the north-south coordinate.")]
        [Editable(SpecialEditType.ObjectLookupCustom)]
        // Need to figure out  [CustomDataSource()] here!
        public string YFieldName = "";

        [Caption("Map View Parameters")]
        [Editable]
        public MapViewInfo MapViewParameters = null;


    } // end class


    /// <summary>
    /// A C3s dashboard chart
    /// </summary>
    [Caption("Code3 Strategist Chart Presentation")]
    public class ExampleC3SPresentationChart : ExampleC3sPresentation
    {
        [Caption("Chart Style")]
        [Editable][StringEnum("Column Chart","Bar Chart","Stacked Columns", "Stacked Bars", "X/Y Plot" )]
        public string ChartStyle = "ColumnChart";

        // TODO:   finish this data layout...   (20210404 CDN)


    }


}
