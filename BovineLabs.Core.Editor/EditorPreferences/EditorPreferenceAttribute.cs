// <copyright file="EditorPreferenceAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System;

    /// <summary>
    /// Base attribute to tag a <see cref="IEditorPreference"/> derived class as an editor preference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class EditorPreferenceAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the section to use for the preference.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorPreferenceAttribute"/> class with the provided section name.
        /// </summary>
        /// <param name="sectionName">The name of the section where the preference should appear.</param>
        protected EditorPreferenceAttribute(string sectionName)
        {
            this.SectionName = sectionName;
        }
    }
}