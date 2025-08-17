// <copyright file="EditorPreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Entities.UI;
    using Unity.Serialization.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using SettingsProvider = UnityEditor.SettingsProvider;

    /// <summary>
    /// Abstract base class for creating editor preferences using attributes.
    /// </summary>
    /// <typeparam name="T">The type of EditorPreferenceAttribute to look for.</typeparam>
    public abstract class EditorPreferences<T> : SettingsProvider
        where T : EditorPreferenceAttribute
    {
        private static readonly Dictionary<string, List<IEditorPreference>> Preferences = new();
        private static readonly List<string> Keywords = new();
        private static readonly string Prefix = $"{typeof(EditorPreferences<T>).FullName}: ";

        static EditorPreferences()
        {
            try
            {
                CachePreferences();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorPreferences{T}"/> class.
        /// </summary>
        /// <param name="path">The path for the preferences in the settings window.</param>
        /// <param name="scope">The scope of the settings (User or Project).</param>
        /// <param name="keywords">Additional keywords for searching.</param>
        protected EditorPreferences(string path, SettingsScope scope, IEnumerable<string>? keywords = null)
            : base(PathForScope(scope) + path, scope, Keywords.Concat(keywords ?? Array.Empty<string>()))
        {
            this.Title = path.Replace("/", " ");
        }

        /// <summary>
        /// Gets a value indicating whether there are any preferences to display.
        /// </summary>
        protected static bool HasAnyPreferences => Preferences.Count > 0;

        /// <summary>
        /// Gets the title to display for this preferences group.
        /// </summary>
        protected virtual string Title { get; }

        /// <inheritdoc/>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Create the main container
            var mainContainer = new VisualElement
            {
                style =
                {
                    paddingTop = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingBottom = 10,
                },
            };

            // Add title
            var titleLabel = new Label(this.Title)
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 15,
                },
            };

            mainContainer.Add(titleLabel);

            // Create scroll view for preferences
            var scrollView = new ScrollView { style = { flexGrow = 1 } };

            foreach (var pair in Preferences)
            {
                // Create section header
                var sectionLabel = new Label(pair.Key)
                {
                    style =
                    {
                        fontSize = 14,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        marginTop = 10,
                        marginBottom = 5,
                    },
                };

                scrollView.Add(sectionLabel);

                // Create container for section content
                var sectionContainer = new VisualElement
                {
                    style =
                    {
                        marginLeft = 10,
                        marginBottom = 10,
                    },
                };

                foreach (var preference in pair.Value)
                {
                    // Create property element for this preference
                    var propertyElement = new PropertyElement();
                    propertyElement.SetTarget(preference);
                    propertyElement.OnChanged += (_, path) => preference.OnPreferenceChanged(path);
                    propertyElement.RegisterCallback<GeometryChangedEvent>(e => StylingUtility.AlignInspectorLabelWidth(propertyElement));

                    sectionContainer.Add(propertyElement);
                }

                scrollView.Add(sectionContainer);
            }

            mainContainer.Add(scrollView);
            rootElement.Add(mainContainer);

            base.OnActivate(searchContext, rootElement);
        }

        private static void CachePreferences()
        {
            if (typeof(T) == typeof(EditorPreferenceAttribute))
            {
                Debug.LogError(
                    $"{Prefix} Constraint of type `{nameof(EditorPreferenceAttribute)}` is not allowed, you must use a derived type of type `{nameof(EditorPreferenceAttribute)}`.");

                return;
            }

            var userSettingsType = typeof(UserSettings<>);

            foreach (var type in TypeCache.GetTypesWithAttribute<T>())
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass)
                {
                    continue;
                }

                if (!typeof(IEditorPreference).IsAssignableFrom(type))
                {
                    Debug.LogError($"{Prefix} type `{type.FullName}` must implement `{typeof(IEditorPreference)}` in order to be used as a preference.");
                    continue;
                }

                var typedUserSettings = userSettingsType.MakeGenericType(type);
                var getOrCreateMethod = typedUserSettings.GetMethod("GetOrCreate", BindingFlags.Static | BindingFlags.Public);
                if (getOrCreateMethod == null)
                {
                    Debug.LogError($"{Prefix} Could not find the `GetOrCreate` method on `{userSettingsType.FullName}` class.");
                    continue;
                }

                var attributes = type.GetCustomAttributes<T>();
                foreach (var attribute in attributes)
                {
                    var preference = (IEditorPreference)getOrCreateMethod.Invoke(null, new object[] { attribute.SectionName });
                    if (!Preferences.TryGetValue(attribute.SectionName, out var list))
                    {
                        Preferences[attribute.SectionName] = list = new List<IEditorPreference>();
                        Keywords.Add(attribute.SectionName);
                    }

                    list.Add(preference);
                    var keywords = preference.GetSearchKeywords();
                    if (keywords.Length > 0)
                    {
                        Keywords.AddRange(keywords);
                    }
                }
            }
        }

        private static string PathForScope(SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.User:
                    return "Preferences/";
                case SettingsScope.Project:
                    return "Project/";
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }
    }
}
