// <copyright file="SettingsBasePanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Base class for implementing the settings UI. </summary>
    /// <typeparam name="T"> The settings type the panel draws. </typeparam>
    public abstract class SettingsBasePanel<T> : ISettingsPanel
        where T : ScriptableObject, ISettings
    {
        private readonly List<string> keywordList = new List<string>();

        /// <summary> Initializes a new instance of the <see cref="SettingsBasePanel{T}"/> class. </summary>
        protected SettingsBasePanel()
        {
            this.Settings = EditorSettingsUtility.GetSettings<T>();
            this.SerializedObject = new SerializedObject(this.Settings);

            // ReSharper disable once VirtualMemberCallInConstructor, Justification: GetKeyWords marked with a warning
            this.GetKeyWords(this.keywordList);
        }

        /// <inheritdoc/>
        public string DisplayName => this.Settings.DisplayName();

        /// <summary> Gets the settings that the panel is drawing. </summary>
        protected T Settings { get; }

        /// <summary> Gets the <see cref="SerializedObject"/> of the <see cref="Settings"/>. </summary>
        protected SerializedObject SerializedObject { get; }

        /// <summary> Executed when activate is called from the settings window. Can be used to draw using UIElements. </summary>
        /// <remarks><para> If UIElements is used then the OnGUI drawer will be disabled. </para></remarks>
        /// <param name="searchContext"> The search context to provide filtering. </param>
        /// <param name="rootElement"> The UI root element. </param>
        public virtual void OnActivate(string searchContext, VisualElement rootElement)
        {
            Editor editor = Editor.CreateEditor(this.SerializedObject.targetObject);
            //editor.DrawDefaultInspector();


            var imguiContainer = new IMGUIContainer(() =>
            {
                editor.DrawDefaultInspector();
            });
            rootElement.Add(imguiContainer);

            return;

            // var allMatch = string.IsNullOrWhiteSpace(searchContext);
            //
            // foreach (var group in IterateAllChildren(this.SerializedObject))
            // {
            //     var parentGroup = new VisualElement();
            //     rootElement.Add(parentGroup);
            //
            //     var parentLabel = new Label(group.Parent.displayName);
            //     parentGroup.Add(parentLabel);
            //
            //     foreach (var child in group.Children)
            //     {
            //         var e = allMatch || MatchesSearchContext(child.name, searchContext);
            //
            //         var property = new PropertyField(child, child.displayName);
            //         property.name = "PropertyField:" + child.propertyPath;
            //         property.Bind(this.SerializedObject);
            //
            //         var field = ScriptAttributeUtility.GetFieldInfoFromProperty(child, out _);
            //
            //         // property.AddManipulator(new ContextualMenuManipulator(evt =>
            //         // {
            //         //     evt.menu.AppendAction("ActionA", (x) => { });
            //         //     evt.menu.AppendAction("ActionB", (x) => { });
            //         // }));
            //
            //         if (field != null)
            //         {
            //             var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            //             if (tooltip != null)
            //             {
            //                 property.tooltip = tooltip.tooltip;
            //             }
            //         }
            //
            //         parentGroup.Add(property);
            //
            //         property.SetEnabled(e);
            //         // property.RegisterValueChangeCallback(evt => this.SerializedObject.ApplyModifiedProperties());
            //         property.RegisterCallback((EventCallback<ChangeEvent<T>>)(evt => this.SerializedObject.ApplyModifiedProperties()));
            //     }
            // }
        }


        /// <summary> Executed when deactivate is called from teh settings window. </summary>
        public virtual void OnDeactivate()
        {
        }

        /// <inheritdoc/>
        public bool MatchesFilter(string searchContext)
        {
            return this.keywordList.Any(s => MatchesSearchContext(s, searchContext));
        }

        /// <summary> Populates all the keywords associated with the settings. </summary>
        /// <remarks> Do not populate this in constructor. </remarks>
        /// <param name="keywords"> The list to populate. </param>
        protected virtual void GetKeyWords(List<string> keywords)
        {
            keywords.AddRange(this.Settings.DisplayName().Split(' '));

            var groups = IterateAllChildren(this.SerializedObject);

            foreach (var g in groups)
            {
                keywords.Add(g.Parent.name);
                keywords.AddRange(g.Children.Select(c => c.name));
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