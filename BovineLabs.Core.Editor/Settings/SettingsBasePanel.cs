// <copyright file="SettingsBasePanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Base class for implementing the settings UI. </summary>
    /// <typeparam name="T"> The settings type the panel draws. </typeparam>
    public abstract class SettingsBasePanel<T> : ISettingsPanel
        where T : ScriptableObject, ISettings
    {
        private readonly Dictionary<string, List<string>> keywordList = new();

        /// <summary> Initializes a new instance of the <see cref="SettingsBasePanel{T}" /> class. </summary>
        protected SettingsBasePanel()
        {
            this.Settings = EditorSettingsUtility.GetSettings<T>();
            this.GroupName = typeof(T).GetCustomAttribute<SettingsGroupAttribute>()?.Group ?? this.Settings.DisplayName();

            this.SerializedObject = new SerializedObject(this.Settings);

            // ReSharper disable once VirtualMemberCallInConstructor, Justification: GetKeyWords marked with a warning
            this.IsEmpty = !this.GetKeyWords(this.keywordList);
            if (this.IsEmpty)
            {
                if (typeof(T).GetCustomAttribute<AlwaysShowSettingsAttribute>() != null)
                {
                    this.IsEmpty = false;
                }
            }
        }

        /// <inheritdoc />
        public string DisplayName => this.Settings.DisplayName();

        public string GroupName { get; }

        public bool IsEmpty { get; }

        /// <summary> Gets the settings that the panel is drawing. </summary>
        protected T Settings { get; }

        /// <summary> Gets the <see cref="SerializedObject" /> of the <see cref="Settings" />. </summary>
        protected SerializedObject SerializedObject { get; }

        /// <summary> Executed when activate is called from the settings window. Can be used to draw using UIElements. </summary>
        /// <remarks>
        ///     <para> If UIElements is used then the OnGUI drawer will be disabled. </para>
        /// </remarks>
        /// <param name="searchContext"> The search context to provide filtering. </param>
        /// <param name="rootElement"> The UI root element. </param>
        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            var inspectorElement = new InspectorElement(this.SerializedObject);
            rootElement.Add(inspectorElement);

            if (!string.IsNullOrWhiteSpace(searchContext))
            {
                var parents = new HashSet<string>();

                foreach (var c in this.keywordList)
                {
                    if (!MatchesSearchContext(c.Key, searchContext))
                    {
                        continue;
                    }

                    parents.UnionWith(c.Value);
                }

                foreach (var p in parents)
                {
                    inspectorElement.Q<PropertyField>($"PropertyField:{p}")?.AddToClassList("search");
                }
            }
        }

        /// <summary> Executed when deactivate is called from the settings window. </summary>
        public virtual void OnDeactivate()
        {
        }

        /// <inheritdoc />
        public bool MatchesFilter(string searchContext, bool allowEmpty)
        {
            if (!allowEmpty && this.IsEmpty)
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchContext))
            {
                return true;
            }

            return this.keywordList.Any(s => MatchesSearchContext(s.Key, searchContext));
        }

        /// <summary> Populates all the keywords associated with the settings. </summary>
        /// <remarks> Do not populate this in constructor. </remarks>
        /// <param name="keywords"> The list to populate. </param>
        /// <returns> If there were any children. </returns>
        protected virtual bool GetKeyWords(Dictionary<string, List<string>> keywords)
        {
            foreach (var c in this.Settings.DisplayName().Split(' '))
            {
                AddToKeyWord(keywords, c, null);
            }

            var groups = IterateAllChildren(this.SerializedObject);
            var anyChildren = false;

            foreach (var g in groups)
            {
                AddToKeyWord(keywords, g.Parent.name, g.Parent.name);

                foreach (var c in g.Children)
                {
                    AddToKeyWord(keywords, c.name, g.Parent.name);
                }

                anyChildren = true;
            }

            return anyChildren;
        }

        private static void AddToKeyWord(IDictionary<string, List<string>> keywords, string keyword, string? parent)
        {
            if (!keywords.TryGetValue(keyword, out var parents))
            {
                keywords[keyword] = parents = new List<string>();
            }

            if (!string.IsNullOrEmpty(parent))
            {
                parents.Add(parent!);
            }
        }

        private static bool MatchesSearchContext(string s, string searchContext)
        {
            return s.IndexOf(searchContext, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private static IEnumerable<PropertyGroup> IterateAllChildren(SerializedObject root)
        {
            var iterator = root.GetIterator();

            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath != "m_Script")
                {
                    yield return new PropertyGroup
                    {
                        Parent = iterator.Copy(),
                        Children = GetChildren(iterator).ToArray(),
                    };
                }
            }
        }

        private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            var currentProperty = property.Copy();
            var nextSiblingProperty = property.Copy();
            nextSiblingProperty.Next(false);

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                    {
                        yield break;
                    }

                    yield return currentProperty.Copy();
                }
                while (currentProperty.Next(false));
            }
        }

        private struct PropertyGroup
        {
            public SerializedProperty Parent;
            public SerializedProperty[] Children;
        }
    }
}
