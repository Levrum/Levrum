using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Dashboard
{
    /// <summary>
    /// A dashboard:  i.e., a collection of displays that (hoepfully) can be rendered on the Web or in Windows or potentially other display
    /// media.   Displays, themselves, contain their own data retrieval definitions, the basics of which are defined in the
    /// computational model.
    /// </summary>
    public class  Dashboard
    {

        /// <summary>
        /// Displays that make up this dashboard.
        /// </summary>
        public List<Display> Displays = new List<Display>();    /// 20210327 OPENQ: should the dashboard, or the display, know the layout info?


    }
}
