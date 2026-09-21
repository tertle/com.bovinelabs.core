namespace BovineLabs.Core.Editor.Settings
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(EditorSettings))]
    public class EditorSettingsEditor : ElementEditor
    {
        private readonly List<string> _scriptingDefineSymbols = new();
        private readonly List<string> _scriptingDefineSymbolsOriginal = new();

        private readonly List<string> _removed = new();


        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "_scriptingDefineSymbols" => CreateScriptingDefine(property),
                _ => base.CreateElement(property),
            };
        }

        private VisualElement CreateScriptingDefine(SerializedProperty property)
        {
            _scriptingDefineSymbolsOriginal.Clear();
            _scriptingDefineSymbols.Clear();

            for (var i = 0; i < property.arraySize; i++)
            {
                _scriptingDefineSymbols.Add(property.GetArrayElementAtIndex(i).stringValue);
            }

            _scriptingDefineSymbolsOriginal.AddRange(_scriptingDefineSymbols);

            var ve = CreateFoldout(property.displayName, property.isExpanded);

            ve.RegisterValueChangedCallback(evt =>
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            EventCallback<ChangeEvent<string>> changeCallback = null;

            var listView = new ListView(_scriptingDefineSymbols)
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
                tf.value = _scriptingDefineSymbols[index];

                changeCallback = evt => _scriptingDefineSymbols[index] = evt.newValue;
                tf.RegisterValueChangedCallback(changeCallback);
            }

            void UnbindItem(VisualElement element, int index)
            {
                var tf = (TextField)element;
                tf.UnregisterValueChangedCallback(changeCallback);
            }

            void Revert()
            {
                _scriptingDefineSymbols.Clear();
                _scriptingDefineSymbols.AddRange(_scriptingDefineSymbolsOriginal);
                listView.Rebuild();
            }

            void Apply()
            {
                _removed.Clear();

                foreach (var c in _scriptingDefineSymbolsOriginal)
                {
                    if (!_scriptingDefineSymbols.Contains(c))
                    {
                        _removed.Add(c);
                    }
                }

                _scriptingDefineSymbolsOriginal.Clear();
                _scriptingDefineSymbolsOriginal.AddRange(_scriptingDefineSymbols);

                property.arraySize = _scriptingDefineSymbols.Count;

                for (var i = 0; i < _scriptingDefineSymbols.Count; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = _scriptingDefineSymbols[i];
                }

                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                ScriptingDefineSymbolsEditor.ApplyDefinesToAll(_scriptingDefineSymbols, _removed);
            }
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var editorSettings = (EditorSettings)target;

            var button = new Button(() => EditorSettingsUtility.UpdateSettings(editorSettings))
            {
                text = "Update Settings",
                style = { maxWidth = 200 },
            };

            root.Add(button);
        }
    }
}
