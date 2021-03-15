
using AnalysisFramework.Infrastructure;
using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Dashboard
{

    /// <summary>
    /// Graphical information about graphic properties of data to be rendered in a dashboard display.
    /// </summary>
    public class VisualInfo
    {
        [Editable(SpecialEditType.Color)] [Caption("Color")] public int ColorValue { set; get; }

        [Caption("Display Field Name")]
        public string DisplayFieldName { set; get; } = "";     // Name of the field to be displayed from the output data.   This will be set by the client, based on the output type of the selected display.


        [Editable] [Caption("Legend Text")] public string LegendText { set; get; } = "";


    }
}
