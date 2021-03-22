using Levrum.Utils.Infra;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnalysisFramework.Infrastructure
{



    /// <summary>
    /// This attribute marks fields/properties that are editable in the UI.   The attribute marker can be
    /// parameterized with the SpecialEditType enum to provide information about specific editing/metatadata
    /// patterns.
    /// </summary>
    public class EditableAttribute : Attribute
    {
        public EditableAttribute()
        {

        }


        public EditableAttribute(SpecialEditType qEditType)
        {
            EditType = qEditType;
            if (SpecialEditType.CustomEditor == qEditType)
            {
                Util.HandleAppErrOnce(this, "EditableAttribute.c'tor()", "SpecialEditType." + qEditType.ToString() + " must specify instance and user interface types");
            }
        }


        /// <summary>
        /// Special edit flag.   See SpecialEditType definition for further info and special cases.
        /// </summary>
        public SpecialEditType EditType = SpecialEditType.None;


    } // end class





    /// <summary>
    /// Attribute used to specify a custom data source for objects to be selected for either scalar or list elements.
    /// This enables the client to specify items to be made available for selection in the generic editor, without having to
    /// put them explicitly into a repository along with the other contents, first.
    /// </summary>
    public class CustomDataSourceAttribute : Attribute
    {
        public CustomDataSourceAttribute(Type oEnumeratorType, params object[] oAdditionalParams)
        {

            if (!typeof(IValueEnumerator).IsAssignableFrom(oEnumeratorType))
            {
                Util.HandleAppErr(this, "C'tor()", "Type argument " + oEnumeratorType.Name + " must inherit IValueEnumerator.");
                return;
            }

            EnumeratorType = oEnumeratorType;
            AdditionalParams = oAdditionalParams;
        }

        /// <summary>
        /// Type specifying the enumerator that will generate the balues.
        /// </summary>
        public Type EnumeratorType = null;

        /// <summary>
        /// Additional parameters for the attribute instance.   Used, for example, by SpecialEditType.DynamicSeqnenceCustom.
        /// </summary>
        public object[] AdditionalParams = new object[] { };

    }
    }
