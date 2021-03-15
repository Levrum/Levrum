using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Dashboard
{
    /// <summary>
    /// This class defines layout information for a single display.
    /// </summary>
    public class LayoutInfo
    {
        public double LeftEdgePercent { set; get; } = 0.0;      // Not sure about the best way to do this.  This is a placeholder 
        public double WidthPercent { set; get; } = 100.0;       // for initial implementation.
        public double TopEdgePercent { set; get; } = 0.0;
        public double HeightPercent { set; get; } = 100.0;

    }
}
