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

    public class PresentationInfo
    {

        [Editable] public  PresentationStyles Style { set; get; } = PresentationStyles.Table;
        


    }
}
