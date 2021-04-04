using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Model.Data
{
    /// <summary>
    /// This class specifies a map view geometry - center and scale for now, though we could
    /// subclass it to handle upper-left/lower-right window settings.
    /// In practice, this will probably be captured "in one piece" by the UI from a visual display.  (Use case:
    /// user sees a map display, re-adjusts the view and says "save view settings," which updates and saves
    /// an instance of this class.
    /// </summary>
    public class MapViewInfo
    {

        [Caption("Center Longitude")]
        public double CenterLongitude = 0.0;

        [Caption("Center Latitude")]
        public double CenterLatitude = 0.0;

        [Caption("Scale")]
        public double Scale = 0.0;


    }
}
