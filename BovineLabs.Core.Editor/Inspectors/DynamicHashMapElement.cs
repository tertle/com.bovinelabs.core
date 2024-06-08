// <copyright file="DynamicHashMapElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using BovineLabs.Core.Iterators;
    using UnityEngine.UIElements;
#if UNITY_6000_0_OR_NEWER
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using UnityEditor.UIElements;
#endif

    public class DynamicHashMapElement<TBuffer, TKey, TValue> : DynamicHashMapElement<TBuffer, TBuffer, TKey, TValue>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#if UNITY_6000_0_OR_NEWER
        public DynamicHashMapElement(object inspector, List<SearchView.Item>? items = default, TValue defaultValue = default)
            : base(inspector, items, defaultValue)
#else
	    public DynamicHashMapElement(object inspector)
            : base(inspector)
#endif
        {
        }
    }

    public class DynamicHashMapElement<T, TBuffer, TKey, TValue> : VisualElement
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapListElement<T, TBuffer, TKey, TValue> listElement;

#if UNITY_6000_0_OR_NEWER
        private readonly DynamicHashMapSearchElement<T, TBuffer, TKey, TValue>? searchElement;

        private readonly ToolbarToggle listElementToggle;
        private readonly ToolbarToggle searchToggle;
#endif

#if UNITY_6000_0_OR_NEWER
        public DynamicHashMapElement(object inspector, List<SearchView.Item>? items = default, TValue defaultValue = default)
#else
	    public DynamicHashMapElement(object inspector)
#endif
        {
#if UNITY_6000_0_OR_NEWER
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
#endif

            this.listElement = new DynamicHashMapListElement<T, TBuffer, TKey, TValue>(inspector, 0);
            this.Add(this.listElement);

#if UNITY_6000_0_OR_NEWER
            if (hasItems)
            {
                this.searchElement = new DynamicHashMapSearchElement<T, TBuffer, TKey, TValue>(inspector, items!, defaultValue, 0);
            }
#endif

            this.schedule.Execute(this.Update).Every(250);
        }

#if UNITY_6000_0_OR_NEWER
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
#endif

        private void Update()
        {
            if (!this.listElement.IsValid())
            {
                this.RemoveFromHierarchy();
                return;
            }

            // TODO conditional on visible
            this.listElement.Update();
#if UNITY_6000_0_OR_NEWER
            this.searchElement?.Update();
#endif
        }
    }
}