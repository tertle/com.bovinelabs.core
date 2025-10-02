// <copyright file="EditorSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(EditorSettings))]
    public class EditorSettingsEditor : ElementEditor
    {
        private readonly List<string> scriptingDefineSymbols = new();
        private readonly List<string> scriptingDefineSymbolsOriginal = new();

        private readonly List<string> removed = new();


        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "scriptingDefineSymbols" => this.CreateScriptingDefine(property),
                _ => base.CreateElement(property),
            };
        }

        private VisualElement CreateScriptingDefine(SerializedProperty property)
        {
            this.scriptingDefineSymbolsOriginal.Clear();
            this.scriptingDefineSymbols.Clear();

            for (var i = 0; i < property.arraySize; i++)
            {
                this.scriptingDefineSymbols.Add(property.GetArrayElementAtIndex(i).stringValue);
            }

            this.scriptingDefineSymbolsOriginal.AddRange(this.scriptingDefineSymbols);

            var ve = CreateFoldout(property.displayName, property.isExpanded);

            ve.RegisterValueChangedCallback(evt =>
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            var listView = new ListView(this.scriptingDefineSymbols)
            {
                selectionType = SelectionType.None,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                makeItem = MakeItem,
                bindItem = BindItem,
                unbindItem = UnbindItem,
            };

            ve.Add(listView);

            var hor = new VisualElement { style = { flexDirection = FlexDirection.RowReverse } };
            ve.Add(hor);

            var revert = new Button(Revert) { text = "Revert" };
            var apply = new Button(Apply) { text = "Apply" };

            hor.Add(apply);
            hor.Add(revert);

            return ve;

            TextField MakeItem() => new() { label = string.Empty, style = { flexGrow = 1f } };
            void BindItem(VisualElement element, int index)
            {
                var tf = (TextField)element;
                tf.value = this.scriptingDefineSymbols[index];
                tf.RegisterValueChangedCallback(evt => this.scriptingDefineSymbols[index] = evt.newValue);
            }

            void UnbindItem(VisualElement element, int index)
            {
                var tf = (TextField)element;
                tf.UnregisterValueChangedCallback(evt => this.scriptingDefineSymbols[index] = evt.newValue);
            }

            void Revert()
            {
                this.scriptingDefineSymbols.Clear();
                this.scriptingDefineSymbols.AddRange(this.scriptingDefineSymbolsOriginal);
                listView.Rebuild();
            }

            void Apply()
            {
                this.removed.Clear();

                foreach (var c in this.scriptingDefineSymbolsOriginal)
                {
                    if (!this.scriptingDefineSymbols.Contains(c))
                    {
                        this.removed.Add(c);
                    }
                }

                this.scriptingDefineSymbolsOriginal.Clear();
                this.scriptingDefineSymbolsOriginal.AddRange(this.scriptingDefineSymbols);

                property.arraySize = this.scriptingDefineSymbols.Count;

                for (var i = 0; i < this.scriptingDefineSymbols.Count; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = this.scriptingDefineSymbols[i];
                }

                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                ScriptingDefineSymbolsEditor.ApplyDefinesToAll(this.scriptingDefineSymbols, this.removed);
            }
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var editorSettings = (EditorSettings)this.target;

            var button = new Button(() => UpdateSettings(editorSettings))
            {
                text = "Update Settings",
                style = { maxWidth = 200 },
            };

            root.Add(button);
        }

        private static void UpdateSettings(EditorSettings editorSettings)
        {
            if (editorSettings.DefaultSettingsAuthoring == null)
            {
                return;
            }

            // Clear all our existing settings
            ClearSettings(editorSettings.DefaultSettingsAuthoring);
            foreach (var i in editorSettings.SettingsAuthorings)
            {
                ClearSettings(i.Authoring);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:SettingsBase"))
            {
                var settingsBase = AssetDatabase.LoadAssetAtPath<SettingsBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (settingsBase == null)
                {
                    continue;
                }

                EditorSettingsUtility.AddSettingsToAuthoring(editorSettings, settingsBase);
            }

            return;

            static void ClearSettings(SettingsAuthoring? authoring)
            {
                if (authoring == null)
                {
                    return;
                }

                var so = new SerializedObject(authoring);
                var settingsProperty = so.FindProperty("settings");
                settingsProperty.arraySize = 0;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(authoring);
            }
        }
    }
}
