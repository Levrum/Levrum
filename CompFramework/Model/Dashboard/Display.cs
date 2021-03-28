using System;
using System.Collections.Generic;
using System.Text;
using AnalysisFramework.Infrastructure;
using AnalysisFramework.Model.Computation;
using Levrum.Utils.Infra;

namespace AnalysisFramework.Model.Dashboard
{
    /// <summary>
    /// A single display within a dashboard.
    /// </summary>
    public class Display
    {
        [Caption("Data Supplier")]
        [Doc("Specify the computation that provides the data here.")]
        // Need attributes that identify the source/editor
        public Computable Computation = null;


        [Caption("Presentation")]
        public PresentationInfo Presentation = null;



    }
}
