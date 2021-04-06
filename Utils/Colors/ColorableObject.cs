using System.Drawing;

namespace Levrum.Utils.Colors
{
    public class ColorableObject
    {
        /*      Overview:
         *      
         *          Each ColorableObject acts as a link between a named object and the colors you want to use for it.
         *          They go into Dictionaries used by the ColorManager
         *          
         *          Fields:
         *          Name - Uhhh.... does your object have a name?
         *          Type - The category of object that the dictionaries ColorManager is using.
         *          TypeGrouping - The pretty pring version of the type. I mostly use if for UI elements.
         *          Parent - The ColorableObject which this will defualt the color to if it doesn't have its own.
         *          ParentType - The type/category of the parent.
         *          FillColor - The back color. AKA main color.
         *          BorderColor - The accent color usually used around edges.
         */

        public string Name { get; set; } //For example, Stucture Fire
        public string Type { get; set; }//For example, Cause
        public string TypeGrouping { get; set; } //Optional. For example, Level 2 Cause
        public string Parent { get; set; } //Optional. For example, Fire
        public string ParentType { get; set; } //Optional. For example, Cause. Does not need to match parent type. For example, E2's parent could be Unit Role
        public Color FillColor { get; set; } //The main object color
        public Color BorderColor { get; set; } //The secondary color that shows around the edge
        public ColorableObject(string name, string type, string typeGrouping = "", string parent = "", string parentType = "", Color? fillColor = null, Color? borderColor = null)
        {
            Name = name;
            Type = type;
            TypeGrouping = typeGrouping;
            Parent = parent;
            ParentType = parentType;
            FillColor = fillColor.GetValueOrDefault(Color.Empty);
            BorderColor = borderColor.GetValueOrDefault(Color.Empty);
        }

        public void RemoveColor()
        {
            FillColor = Color.Empty;
            BorderColor = Color.Empty;
        }

        public bool HasFillColor()
        {
            if (FillColor != Color.Empty)
            {
                return true;
            }
            return false;
        }

        public bool HasBorderColor()
        {
            if (BorderColor != Color.Empty)
            {
                return true;
            }
            return false;
        }
    }
}
