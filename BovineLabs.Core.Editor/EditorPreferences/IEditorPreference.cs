// <copyright file="IEditorPreference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Properties;
    using UnityEditor;

    /// <summary>
    /// Interface that allows to declare a class type as an editor preference.
    /// </summary>
    public interface IEditorPreference
    {
        /// <summary>
        /// Method called when a change is detected in the UI.
        /// </summary>
        /// <param name="path">Path to the changed property.</param>
        void OnPreferenceChanged(PropertyPath path)
        {
        }

        /// <summary>
        /// Get the searchable keywords in this preferences group.
        /// </summary>
        /// <returns>Array of search keywords.</returns>
        string[] GetSearchKeywords();

        /// <summary>
        /// Helper method to get search keywords from properties of a type.
        /// </summary>
        /// <param name="type">The type to extract keywords from.</param>
        /// <returns>Enumerable of search keywords from properties.</returns>
        static IEnumerable<string> GetSearchKeywordsFromProperties(System.Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(prop => ObjectNames.NicifyVariableName(prop.Name));
        }

        /// <summary>
        /// Helper method to get search keywords from fields of a type.
        /// </summary>
        /// <param name="type">The type to extract keywords from.</param>
        /// <returns>Enumerable of search keywords from fields.</returns>
        static IEnumerable<string> GetSearchKeywordsFromFields(System.Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => ObjectNames.NicifyVariableName(field.Name));
        }

        /// <summary>
        /// Helper method to get all search keywords from a type (properties and fields).
        /// </summary>
        /// <param name="type">The type to extract keywords from.</param>
        /// <returns>Array of all search keywords from the type.</returns>
        static string[] GetSearchKeywordsFromType(System.Type type)
        {
            return GetSearchKeywordsFromProperties(type)
                .Concat(GetSearchKeywordsFromFields(type))
                .ToArray();
        }
    }
}