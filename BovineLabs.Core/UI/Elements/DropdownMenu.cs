// <copyright file="DropdownMenu.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Collections;
    using UnityEngine.UIElements;

    public class DropdownMenu
    {
        private const string MenuUssClassName = ""; //UssClassName + "__menu";

        private ObjectPool<ListView> listPool = new(() =>
        {
            var listView = new ListView
            {
                pickingMode = PickingMode.Position,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                focusable = true,
                selectionType = SelectionType.None,
                // classList = { containerInnerUssClassName },
                makeItem = () => new VisualElement { focusable = false, pickingMode = PickingMode.Ignore },
                unbindItem = (v, i) => v.Clear(),
                style = { flexGrow = 1, },
            };
            listView.bindItem = (v, i) => v.Add((listView.itemsSource as List<MenuItem>)?[i].element);
            listView.horizontalScrollingEnabled = false;
            listView.itemsSourceChanged += () =>
            {
                // We don't allow separators at the top or bottom
                if (listView.itemsSource is List<MenuItem> items)
                {
                    while (items.Count > 0 && items[0].isSeparator)
                    {
                        items.RemoveAt(0);
                    }

                    while (items.Count > 0 && items[^1].isSeparator)
                    {
                        items.Remove(items[^1]);
                    }
                }
            };

            listView.AddToClassList(MenuUssClassName);

            // void BindItem(VisualElement e, int i)
            // {
            //     ((Label)e).text = displayNames[i];
            // }
            //
            // displayNames.Add("Default");
            //
            // menu.itemsSource = displayNames;
            // menu.makeItem = MakeItem;
            // menu.bindItem = BindItem;
            // menu.selectionType = multiSelect ? SelectionType.Multiple : SelectionType.Single;

            listView.style.position = Position.Absolute;
            listView.style.maxHeight = 200;
            listView.style.minHeight = 0;

            return listView;
        });

        public DropdownMenu()
        {

        }

        internal class MenuItem
        {
            public string name;
            public VisualElement element;
            public Action action;
            public Action<object> actionUserData;
            public bool isCustomContent;
            internal readonly Guid guid = Guid.NewGuid();

            public MenuItem parent;
            public List<MenuItem> children = new();
            public List<MenuItem> headerActions = new();

            // Accelerates child search
            Dictionary<int, MenuItem> childrenDictionary = new();

            public bool isSubmenu => this.children.Count > 0;

            public bool isSeparator => string.IsNullOrEmpty(this.name) || this.name[^1] == '/';

            public bool isActionValid => this.action != null || this.actionUserData != null;

            public void PerformAction()
            {
                if (this.actionUserData != null)
                    this.actionUserData.Invoke(this.element.userData);
                else
                    this.action?.Invoke();
            }

            public void AddChild(MenuItem item)
            {
                this.children.Add(item);

                if (string.IsNullOrEmpty(item.name) || item.name[^1] == '/')
                    return;

                this.childrenDictionary.Add(item.name.GetHashCode(), item);
            }

            public bool HasChild(string name) => this.childrenDictionary.ContainsKey(name.GetHashCode());

            public MenuItem GetChild(string name)
            {
                this.childrenDictionary.TryGetValue(name.GetHashCode(), out var child);
                return child;
            }
        }
    }
}
