namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class EditorPreferenceAttribute : Attribute
    {
        public string SectionName { get; }

        protected EditorPreferenceAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }
}