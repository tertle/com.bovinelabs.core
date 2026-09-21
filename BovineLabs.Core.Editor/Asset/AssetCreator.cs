namespace BovineLabs.Core.Editor.Asset
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core;
    using BovineLabs.Core.Asset;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class AssetCreator
    {
        private readonly string _path;
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _serializedProperty;
        private readonly Type _type;
        private readonly bool _isAbstract;
        private readonly List<SearchView.Item> _items = new();

        private ListView _listView;

        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            var attr = TryGetAttribute(serializedObject, serializedProperty, type);

            _serializedObject = serializedObject;
            _serializedProperty = serializedProperty;
            _type = type;
            _serializedProperty.isExpanded = false;

            _isAbstract = _type.IsAbstract;

            if (_isAbstract)
            {
                foreach (var i in ReflectionUtility.GetAllImplementations(type))
                {
                    _items.Add(new SearchView.Item
                    {
                        Path = i.Name,
                        Data = i,
                    });
                }
            }

            Element = PropertyUtil.CreateProperty(serializedProperty, _serializedObject);

            if (attr != null)
            {
                Element.RegisterCallback<GeometryChangedEvent>(Init);
                Element.AddManipulator(new ContextualMenuManipulator(MenuBuilder));
                _path = _isAbstract ? AssetUtility.GetDefaultPathWithoutFileName(attr) : AssetUtility.GetDefaultPath(attr);
            }
        }

        public PropertyField Element { get; }

        private static AutoRefAttribute TryGetAttribute(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            var attributes = type.GetCustomAttributes<AutoRefAttribute>(true).ToArray();
            var attribute = attributes.FirstOrDefault(a => a.ManagerType == serializedObject.targetObject.name && a.FieldName == serializedProperty.name);

            if (attribute == null && attributes.Length == 0)
            {
                BLGlobalLogger.LogErrorString(
                    $"Type {type} is using AssetCreator but without {nameof(AutoRefAttribute)} so the item will not be added to the object.");
            }
            else if (attribute == null)
            {
                BLGlobalLogger.LogErrorString(
                    $"Type {type} is using AssetCreator but no {nameof(AutoRefAttribute)} targets {serializedObject.targetObject.name}.{serializedProperty.name}.");
            }

            return attribute;
        }

        private void Init(GeometryChangedEvent evt)
        {
            _listView = Element.Q<ListView>();
            if (_listView == null)
            {
                return;
            }

            Element.UnregisterCallback<GeometryChangedEvent>(Init);

            var removeButton = _listView.Q<Button>("unity-list-view__remove-button");
            removeButton.parent.Remove(removeButton);

            _listView.showBoundCollectionSize = false;

            _listView.itemsAdded += ints =>
            {
                var count = ints.Count();

                if (_isAbstract)
                {
                    // Remove the elements unity just force added, they will be added back properly via autoref
                    _serializedObject.Update();
                    _serializedProperty.arraySize -= count;
                    _serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    var searchWindow = SearchWindow.Create();

                    searchWindow.Items = _items;
                    searchWindow.OnSelection += item =>
                    {
                        var t = (Type)item.Data;
                        var p = Path.Combine(_path!, item.Name + ".asset");
                        Create(count, t, p);
                    };

                    var button = _listView.Q<Button>("unity-list-view__add-button");

                    var screenPosition = VisualElementUtil.GetScreenPosition(button);
                    var size = new Rect(screenPosition.x, screenPosition.y + button.worldBound.height, 400, 400);
                    searchWindow.Position = size;
                    searchWindow.Show();
                }
                else
                {
                    Create(count, _type, _path!);
                }

                return;

                static void Create(int count, Type selectedType, string path)
                {
                    for (var i = 0; i < count; i++)
                    {
                        AssetUtility.CreateInstance(selectedType, path);
                    }
                }
            };

            _listView.Q<VisualElement>("unity-content-container").SetEnabled(false);
        }

        private void MenuBuilder(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Remove Missing", _ =>
            {
                for (var i = _serializedProperty.arraySize - 1; i >= 0; i--)
                {
                    if (_serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        _serializedProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                _serializedObject.ApplyModifiedProperties();
            });
        }
    }

    public class AssetCreator<T> : AssetCreator
        where T : ScriptableObject
    {
        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty)
            : base(serializedObject, serializedProperty, typeof(T))
        {
        }
    }
}
