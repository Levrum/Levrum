using CoeloUtils.UiControls;
using Levrum.Utils.Infra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RandD.PumpAndPipeSketch
{


    public abstract class PnpBase
    {
        [Editable]
        public string Name = "";
        [Editable]
        public string Description = "";
    }



    [CoeloUtils.UiControls.Caption("Dashboard")]
    public class Dashboard : PnpBase
    {
        
        [Editable(SpecialEditType.DynamicSequenceNew)]
        public List<Display> Displays = new List<Display>();
    }


    public class Display : PnpBase
    {

        [Editable(SpecialEditType.ObjectLookupCustom)]
        [CoeloUtils.UiControls.CustomDataSource(typeof(RetrievalEnumerator))]
        DynamicCalcInfo<IEnumerable> DataRetriever = null;
    }

    public class RetrievalEnumerator : CoeloUtils.UiControls.IValueEnumerator
    {
        public IEnumerable GetValues(params object[] oParams)
        {
            IEnumerable<DynamicCalcInfo<IEnumerable>> retlist = DynamicCalcInfo<IEnumerable>.GetAvailableCalcs();
            return (retlist);
        }
    }

    [DynamicCalc]
    public class ExampleCalcs
    {
        [DynamicCalc]
        [CoeloUtils.UiControls.Caption("Count by Region")]
        public IEnumerable CountByRegion()
        {
            return (null);
        }

        [DynamicCalc]
        [CoeloUtils.UiControls.Caption("90th Percentile ERF by Region")]
        public IEnumerable Pct90ErfByRegion()
        {
            return (null);
        }

    }
}
