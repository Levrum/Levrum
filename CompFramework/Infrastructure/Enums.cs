
using System;
using System.Collections.Generic;
using System.Text;

namespace AnalysisFramework.Infrastructure
{
    /// <summary>
    /// Flag enum to be used with [EditableAttribute] ctor to indicate special editing considerations.
    /// </summary>
    public enum SpecialEditType
    {
        None,
        FileNameInput,                  // Input filename (look for existing file)
        FileNameOutput,                 // Output filename (warn on collision)
        DirPath,                        // directory path
        Checklist,                      // Checklist from all available instances
        DynamicSequenceNew,             // List whose elements must be created dynamically
        DynamicSequenceExisting,        // List whose elements are chosen dynamically, in order, by user, from existing instances of a type
        DynamicSequenceCustom,          // Dynamic list, chosen dynamically in order, from a custom value enumerator.  Requires a [CustomDataSource]
                                        // attribute to explain how to get the lookup values.
        ObjectLookup,                   // Single object lookup ([Editable] c'tor must specify repository and name field)
        ObjectLookupCustom,             // Single object custom lookup.  Requires a [CustomDataSource] attribute to specify lookup method.
        SettingsData,                   // An object whose fields should be edited in place.  (Issue #2809 20160517 CDN)
        CustomEditor,                   // Specify a custom editor by type
        Password,                       // Property is a password and should be protected in UI

        Color,                          // Value should be captured as a color, and returned as an ARGB integer with 8-bit fields

        Unknown,                        // Default special edit type
        Invalid,                        // Disallowed
        NoDerived                       // Edit types that can be edited in this class, but not derived classes (20190830 CDN #4049)
    }

}
