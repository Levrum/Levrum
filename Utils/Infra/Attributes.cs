using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Levrum.Utils.Infra
{

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



        public static IEnumerable GetValues(CustomDataSourceAttribute oAtt, FieldInfo oFieldInfo)
        {
            string fn = "CustomDataSourceAttribute.GetValues()";
            Type type = typeof(CustomDataSourceAttribute);
            List<object> errval = new List<object>();
            try
            {
                Type ivetype = oAtt.EnumeratorType;
                if (!typeof(IValueEnumerator).IsAssignableFrom(ivetype))
                {
                    Util.HandleAppErr(type, fn, "Unable to retrieve IValueEnumerator for field " + oFieldInfo.Name);
                    return (errval);
                }

                ConstructorInfo ci = ivetype.GetConstructor(new Type[] { });
                if (null == ci)
                {
                    Util.HandleAppErr(type, fn, "Type '" + ivetype.Name + "' has no default constructor, but requires one for this operation");
                    return (errval);
                }

                IValueEnumerator enumerator = ci.Invoke(new object[] { }) as IValueEnumerator;

                IEnumerable values = null;
                if ((null != oAtt.AdditionalParams) && (oAtt.AdditionalParams.Length > 0)) { values = enumerator.GetValues(oAtt.AdditionalParams); }

                return (values);

            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(CustomDataSourceAttribute), fn, exc);
                return (new List<object>());
            }
        }
    } // end class { }

    /// <summary>
    /// Attribute tagging caption of a field or class.  Synax:  [Caption("my caption goes here")].  Captions
    /// often show up in the UI, so be clear, be nice and be brief!
    /// </summary>
    public class BriefTitleAttribute : Attribute
    {
        public BriefTitleAttribute(String sText)
        {
            Text = sText;
        }
        public String Text = "";
    } // end class { }



    /// <summary>
    /// The ability to create an enumeration list of strings.
    /// </summary>
    public class StringEnumAttribute : Attribute
    {
        public StringEnumAttribute(params String[] sEnumVals)
        {
            EnumValues.AddRange(sEnumVals);
        }

        public List<String> EnumValues = new List<string>();
    }


} // end namespace