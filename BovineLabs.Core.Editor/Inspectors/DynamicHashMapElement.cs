// <copyright file="DynamicHashMapElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Iterators;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public class DynamicHashMapElement<TBuffer, TKey, TValue> : DynamicHashMapElement<TBuffer, TBuffer, TKey, TValue>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public DynamicHashMapElement(object inspector, List<SearchView.Item>? items = default, TValue defaultValue = default)
            : base(inspector, items, defaultValue)
        {
        }
    }

    public class DynamicHashMapElement<T, TBuffer, TKey, TValue> : VisualElement
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapListElement<T, TBuffer, TKey, TValue> listElement;

        private readonly DynamicHashMapSearchElement<T, TBuffer, TKey, TValue>? searchElement;

        private readonly ToolbarToggle listElementToggle;
        private readonly ToolbarToggle searchToggle;

        public DynamicHashMapElement(object inspector, List<SearchView.Item>? items = default, TValue defaultValue = default)
        {
            var hasItems = items is { Count: > 0 };
            this.listElementToggle = new ToolbarToggle();
            this.searchToggle = new ToolbarToggle();

            if (hasItems)
            {
                var toolbar = new Toolbar();
                this.Add(toolbar);

                toolbar.Add(this.listElementToggle);
                this.listElementToggle.style.flexGrow = 1;
                this.listElementToggle.text = "List";
                this.listElementToggle.value = true;
                this.listElementToggle.RegisterValueChangedCallback(this.ListValueChanged);

                toolbar.Add(this.searchToggle);
                this.searchToggle.style.flexGrow = 1;
                this.searchToggle.text = "Search";
                this.searchToggle.RegisterValueChangedCallback(this.SearchValueChanged);
            }

            this.listElement = new DynamicHashMapListElement<T, TBuffer, TKey, TValue>(inspector, 0);
            this.Add(this.listElement);

            if (hasItems)
            {
                this.searchElement = new DynamicHashMapSearchElement<T, TBuffer, TKey, TValue>(inspector, items!, defaultValue, 0);
            }

            this.schedule.Execute(this.Update).Every(250);
        }

        private void ListValueChanged(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                // Don't allow it to toggle off
                this.listElementToggle.SetValueWithoutNotify(true);
                return;
            }

            this.searchToggle.SetValueWithoutNotify(false);
            this.Remove(this.searchElement);
            this.Add(this.listElement);

            this.listElement.ForceUpdate();
        }

        private void SearchValueChanged(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                // Don't allow it to toggle off
                this.searchToggle.SetValueWithoutNotify(true);
                return;
            }

            this.listElementToggle.SetValueWithoutNotify(false);
            this.Remove(this.listElement);
            this.Add(this.searchElement);
        }

        private void Update()
        {
            if (!this.listElement.IsValid())
            {
                this.RemoveFromHierarchy();
                return;
            }

            // TODO conditional on visible
            this.listElement.Update();
            this.searchElement?.Update();
        }
    }
}
