using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Levrum.Data.Classes
{
    public interface ICategoryData
    {
        string Name { get; set; }
        string Description { get; set; }
        List<ICategoryData> Children { get; set; }
        List<ICategorizedValue> Values { get; set; }
    }

    public interface ICategorizedValue
    {
        string Value { get; set; }
        string Description { get; set; }
    }

    public class CategorizedValue : AnnotatedData, ICategorizedValue
    {
        public virtual string Value { get; set; }
        public virtual string Description { get; set; }
    }

    public class CategoryData : AnnotatedData, ICategoryData
    {
        public virtual string Name { get; set; } = "";
        public virtual string Description { get; set; } = "";
        public virtual List<ICategoryData> Children { get; set; } = new List<ICategoryData>();
        public virtual List<ICategorizedValue> Values { get; set; } = new List<ICategorizedValue>();
    }
}
