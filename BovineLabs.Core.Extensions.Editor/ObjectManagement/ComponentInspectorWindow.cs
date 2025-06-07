// <copyright file="ComponentInspectorWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ComponentInspectorWindow : EditorWindow
    {
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/ComponentInspectorWindow/";
        private static readonly UITemplate Template = new(RootUIPath + "ComponentInspectorWindow");

        private DropdownField? categoryDropdown;
        private DropdownField? componentDropdown;
        private MultiColumnListView? listView;

        private ObjectDefinition[] definitions = Array.Empty<ObjectDefinition>();
        private Dictionary<string, Type> components = new();

        [MenuItem(EditorMenus.RootMenuTools + "Component Inspector")]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = (ComponentInspectorWindow)GetWindow(typeof(ComponentInspectorWindow));
            window.titleContent = new GUIContent("Component Inspector");
            window.Show();
        }

        private static ObjectCategories GetObjectCategories()
        {
            return Resources.Load<ObjectCategories>($"{KSettingsBase.KResourceDirectory}/{nameof(ObjectCategories)}");
        }

        private void CreateGUI()
        {
            // Reference to the root of the window.
            var root = this.rootVisualElement;
            Template.Clone(root);

            this.categoryDropdown = this.rootVisualElement.Q<DropdownField>("Category");
            this.categoryDropdown.choices = GetObjectCategories().Keys.Select(s => s.Name).ToList();
            this.categoryDropdown.RegisterValueChangedCallback(this.CategoryChanged);

            this.componentDropdown = this.rootVisualElement.Q<DropdownField>("Components");
            this.componentDropdown.RegisterValueChangedCallback(this.ComponentChanged);

            this.listView = this.rootVisualElement.Q<MultiColumnListView>();
        }

        private void CategoryChanged(ChangeEvent<string> evt)
        {
            // evt.
            var categories = GetObjectCategories();

            var keys = categories.Keys.ToArray();
            var index = keys.IndexOf(i => i.Name == evt.newValue);
            if (index == -1)
            {
                this.componentDropdown!.choices = new List<string>();
                return;
            }

            var value = keys[index].Value;
            var settings = EditorSettingsUtility.GetSettings<ObjectManagementSettings>();

            this.definitions = settings.ObjectDefinitions.Where(o => (o.Categories & (1 << value)) != 0).Where(o => o.Prefab != null).ToArray();
            this.components =
                this.definitions.SelectMany(o => o.Prefab!.GetComponents<Component>()).Select(c => c.GetType()).Distinct().ToDictionary(c => c.Name, c => c);

            this.componentDropdown!.choices = this.components.Keys.ToList();
        }

        private void ComponentChanged(ChangeEvent<string> evt)
        {
            if (!this.components.TryGetValue(evt.newValue, out var type))
            {
                return;
            }

            this.listView!.columns.Clear();

            var selectedDefinitions = this.definitions.Select(d => (d.Prefab, d.Prefab!.GetComponent(type))).Where(d => d.Item2 != null).ToArray();

            var objects = new List<ObjectData>();

            foreach (var d in selectedDefinitions)
            {
                var od = new ObjectData(d.Prefab!);
                var so = new SerializedObject(d.Item2);

                foreach (var sp in SerializedHelper.IterateAllChildrenAndFlatten(so))
                {
                    od.Properties.Add(sp.propertyPath, sp);
                }

                objects.Add(od);
            }

            this.listView.itemsSource = objects;
            var first = selectedDefinitions.First();

            this.listView.columns.primaryColumnName = "bl-definition";

            var primary = new Column
            {
                name = "bl-definition",
                title = "Definition",
                width = 100,
                makeCell = () => new ObjectField(),
                bindCell = (element, index) =>
                {
                    var of = (ObjectField)element;
                    of.SetEnabled(false);
                    of.value = objects[index].ObjectDefinition;
                    of.label = string.Empty;
                },
            };

            this.listView.columns.Add(primary);

            foreach (var sp in SerializedHelper.IterateAllChildrenAndFlatten(new SerializedObject(first.Item2)))
            {
                var column = new Column
                {
                    name = sp.propertyPath,
                    title = sp.propertyPath,
                    width = 100,
                    makeCell = () => new PropertyField(null, string.Empty),
                    bindCell = (element, index) =>
                    {
                        var e = objects[index];
                        var property = e.Properties[sp.propertyPath];

                        var pf = (PropertyField)element;
                        pf.bindingPath = property.propertyPath;
                        pf.name = "PropertyField:" + property.propertyPath;
                        pf.Bind(property.serializedObject);
                    },
                };

                this.listView.columns.Add(column);
            }
        }

        private class ObjectData
        {
            public readonly GameObject ObjectDefinition;
            public readonly Dictionary<string, SerializedProperty> Properties = new();

            public ObjectData(GameObject objectDefinition)
            {
                this.ObjectDefinition = objectDefinition;
            }
        }
    }
}
#endif
