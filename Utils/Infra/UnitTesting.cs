using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Infra
{

    /// <summary>
    /// This attribute flags a method for running unit tests.
    /// Methods should return bool (success/failure), and take a list of strings,
    /// which should be updated with any error messages.
    /// </summary>
    public class UnitTestAttribute : Attribute
    {
        public UnitTestAttribute()
        {
        }
    }
}
