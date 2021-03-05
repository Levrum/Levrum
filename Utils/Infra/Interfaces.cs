// This file is a home for all simple interfaces used in the standard model library.

using System.Collections;
using System.Reflection;

namespace Levrum.Utils.Infra
{

    /// <summary>
    /// General form of something capable of retrieving a list of values, e.g., for
    /// user selection or validation.
    /// </summary>
    public interface IValueEnumerator
    {
        IEnumerable GetValues(params object[] oParams);
    }


    public interface IC3cNamedObj
    {
        string Name { get; }
    }

} // end namespace