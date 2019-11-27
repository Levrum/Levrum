using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Levrum.Data.Classes
{
    public class InternedDictionary<T1, T2> : Dictionary<T1, T2> 
    {
        public new void Add(T1 key, T2 value)
        {
            T1 realKey = key;
            if (key is string)
            {
                realKey = (T1)Convert.ChangeType(string.Intern(key as string), typeof(T1));
            }

            T2 realValue = value;
            if (value is string)
            {
                realValue = (T2)Convert.ChangeType(string.Intern(value as string), typeof(T2));
            }

            base.Add(realKey, realValue);
        }

        public void InternKeys()
        {
            List<T1> keys = Keys.ToList();
            T2 value;
            T1 newKey;
            foreach (T1 key in keys)
            {
                if (key is string)
                {
                    value = this[key];
                    Remove(key);
                    newKey = (T1)Convert.ChangeType(string.Intern(key as string), typeof(T1));
                    Add(newKey, value);
                }
            }
        }

        public void InternValues()
        {
            List<T1> keys = Keys.ToList();
            T2 value;
            T2 newValue;
            foreach (T1 key in keys)
            {
                value = this[key];
                if (value is string)
                {
                    newValue = (T2)Convert.ChangeType(string.Intern(value as string), typeof(T2));
                    Remove(key);
                    Add(key, newValue);
                }
            }
        }

        public void Intern()
        {
            T1 newKey;
            T2 newValue;
            bool updateRequired;

            List<KeyValuePair<T1, T2>> kvps = this.ToList();
            foreach (KeyValuePair<T1, T2> kvp in kvps)
            {
                updateRequired = false;
                newKey = kvp.Key;
                newValue = kvp.Value;

                if (kvp.Key is string)
                {
                    newKey = (T1)Convert.ChangeType(string.Intern(kvp.Key as string), typeof(T1));
                    updateRequired = true;
                }

                if (kvp.Value is string)
                {
                    newValue = (T2)Convert.ChangeType(string.Intern(kvp.Value as string), typeof(T2));
                    updateRequired = true;
                }

                if (updateRequired)
                {
                    Remove(kvp.Key);
                    Add(newKey, newValue);
                }
            }
        }
    }
}
