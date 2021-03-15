using AnalysisFramework.Infrastructure;
using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Dashboard
{




    public enum PresentationStyles
    {
        Table,
        [Caption("Scatter Plot")] ScatterPlot,                          // This may need to be a class.
        [Caption("Column Chart")] ColumnChart,                          // Different types may require additional/different
        [Caption("Stacked Column Chart")] StackedColumnChart,           // parameters, which may need to be defined
        [Caption("Bar Chart")] BarChart,                                // on a per-chart-type basis.
        [Caption("Stacked Bar Chart")] StackedBarChart,
        [Caption("Line Chart")] LineChart,
        [Caption("Stacked Line Chart")] StackedLineChart,
        [Caption("Stacked Area Chart")] StackedAreaChart,
    };


    /// <summary>
    /// This class defines the presentation parameters for a single display.
    /// </summary>
    public class PresentationInfo
    {

        /// <summary>
        /// The display style for the display.
        /// </summary>
        [Editable] public  PresentationStyles Style { set; get; } = PresentationStyles.Table;

        /// <summary>
        /// How the display is to be laid out within the dashboard.
        /// </summary>
        [Editable] public LayoutInfo Layout { set; get; } = new LayoutInfo();  // defaults to 1 display at 100% horiz/vert

        [Editable] public VisualInfo SeriesInfo { set; get; } = new VisualInfo();   

        


    }
}
