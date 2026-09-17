namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Properties;
    using UnityEditor;

    public interface IEditorPreference
    {
        void OnPreferenceChanged(PropertyPath path)
        {
        }

        string[] GetSearchKeywords();

        static IEnumerable<string> GetSearchKeywordsFromProperties(System.Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(prop => ObjectNames.NicifyVariableName(prop.Name));
        }

        static IEnumerable<string> GetSearchKeywordsFromFields(System.Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => ObjectNames.NicifyVariableName(field.Name));
        }

        static string[] GetSearchKeywordsFromType(System.Type type)
        {
            return GetSearchKeywordsFromProperties(type)
                .Concat(GetSearchKeywordsFromFields(type))
                .ToArray();
        }
    }
}