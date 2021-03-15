using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{

    /// <summary>
    /// General signature of classes used for retrieving lists of objects.   These are used in auto-generated editing
    /// interfaces, particularly for discovery of dynamic calculations, etc.
    /// </summary>
    public interface IValueEnumerator
    {
        IEnumerable GetValues(params object[] oParams);
    }
}
