using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Levrum.Utils.Infra
{

    /// <summary>
    /// This class serves as the base for various documentation attributes that support text that can be 
    /// be retrieved dynamically according to the individual type.   This permits tagging of various
    /// elements with multiple types of documentation tags ... e.g., 
    /// [Comment("blah")][DocLink("https://help.levrum.com")][Usage("my_util -{a|b|c} <input-file>  <output-file>")]
    /// </summary>
    public abstract class DocBaseAttribute : Attribute
    {

        public DocBaseAttribute(string sText)
        {
            Text = sText;
        }
        public string Text = "";


    } // end class




    /// <summary>
    /// Attribute for providing documentation about items.  This is intended for medium-sized commentary,
    /// around a paragraph in length.
    /// </summary>
    public class DocAttribute : DocBaseAttribute
    {
        public DocAttribute(string sDoc)
            : base(sDoc)
        {
        }
    }


    /// <summary>
    /// Attribute for providing descriptive text about items.  This is intended forshort commentary, 3-12 words-ish.
    /// </summary>
    public class DescAttribute : DocBaseAttribute
    {
        public DescAttribute(string sDoc)
            : base(sDoc)
        {
        }
    }


    /// <summary>
    /// Attribute for providing a help link.    E.g., [HelpLink("https://help.levrum.com/myTopic")]
    /// </summary>
    public class HelpLinkAttribute : DocBaseAttribute
    {
        public HelpLinkAttribute(string sLink)
            : base(sLink)
        {

        }
    }
         






    /// <summary>
    /// Documentation utilities.
    /// </summary>
    public static class DocUtil
    {
        /// <summary>
        /// Ability to get text of a documentation elenent (by specific type) for any attribute-taggable code element.
        /// Example usages:
        ///    - DocUtil.GetText<DocAttribute>(myComputationMethodInfo)   // get documentation for the MethodInfo for MyComputation()
        ///    - DocUtil.GetText<HelpLinkAttribute>(myClass)              // get the help link for class myClass
        /// </summary>
        /// <param name="oAttribute"></param>
        /// <param name="oTargetInfo"></param>
        /// <returns></returns>
        public static string GetText<T>(ICustomAttributeProvider oTargetInfo)
            where T : DocBaseAttribute
        {
            const string fn = "DocUtil.GetText()";
            try
            {
                if (null == oTargetInfo) { return (""); }
                Type info_object_type = typeof(T);
                object[] atts = oTargetInfo.GetCustomAttributes(info_object_type, true);
                if (null == atts) { return (""); }
                if (0 == atts.Length) { return (""); }
                //string sdefault = (oTargetInfo is MemberInfo) ? ((oTargetInfo as MemberInfo).Name) : oTargetInfo.ToString();
                object obj0 = atts[0];
                T att0 = obj0 as T;
                if (null == att0) { return (""); }
                return (att0.Text);

                
            }
            catch (Exception exc)
            {
                Util.HandleExc(typeof(DocBaseAttribute), fn, exc);
                return ("");
            }
        }

    }
}
