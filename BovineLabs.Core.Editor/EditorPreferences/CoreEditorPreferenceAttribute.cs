namespace BovineLabs.Core.Editor.EditorPreferences
{
    public sealed class CoreEditorPreferenceAttribute : EditorPreferenceAttribute
    {
        public CoreEditorPreferenceAttribute(string sectionName)
            : base(sectionName)
        {
        }
    }
}
