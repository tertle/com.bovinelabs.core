// <copyright file="CoreEditorPreferenceAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorPreferences
{
    /// <summary>
    /// Attribute to tag a type to be included as a preference for the BovineLabs Core editor preferences.
    /// </summary>
    public sealed class CoreEditorPreferenceAttribute : EditorPreferenceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreEditorPreferenceAttribute"/> class with the provided section name.
        /// </summary>
        /// <param name="sectionName">The name of the section where the preference should appear.</param>
        public CoreEditorPreferenceAttribute(string sectionName)
            : base(sectionName)
        {
        }
    }
}
