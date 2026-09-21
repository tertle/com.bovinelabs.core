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
        public DynamicHashMapElement(object inspector, List<SearchView.Item> items = null, TValue defaultValue = default)
            : base(inspector, items, defaultValue)
        {
        }
    }

    public class DynamicHashMapElement<T, TBuffer, TKey, TValue> : VisualElement
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapListElement<T, TBuffer, TKey, TValue> _listElement;

        private readonly DynamicHashMapSearchElement<T, TBuffer, TKey, TValue> _searchElement;

        private readonly ToolbarToggle _listElementToggle;
        private readonly ToolbarToggle _searchToggle;

        public DynamicHashMapElement(object inspector, List<SearchView.Item> items = null, TValue defaultValue = default)
        {
            var hasItems = items is { Count: > 0 };
            _listElementToggle = new ToolbarToggle();
            _searchToggle = new ToolbarToggle();

            if (hasItems)
            {
                var toolbar = new Toolbar();
                Add(toolbar);

                toolbar.Add(_listElementToggle);
                _listElementToggle.style.flexGrow = 1;
                _listElementToggle.text = "List";
                _listElementToggle.value = true;
                _listElementToggle.RegisterValueChangedCallback(ListValueChanged);

                toolbar.Add(_searchToggle);
                _searchToggle.style.flexGrow = 1;
                _searchToggle.text = "Search";
                _searchToggle.RegisterValueChangedCallback(SearchValueChanged);
            }

            _listElement = new DynamicHashMapListElement<T, TBuffer, TKey, TValue>(inspector, 0);
            Add(_listElement);

            if (hasItems)
            {
                _searchElement = new DynamicHashMapSearchElement<T, TBuffer, TKey, TValue>(inspector, items!, defaultValue, 0);
            }

            schedule.Execute(Update).Every(250);
        }

        public Action<IEntityContext, TKey, TValue> SearchSetValue
        {
            get
            {
                if (_searchElement == null)
                {
                    return (_, _, _) =>
                    {
                    };
                }

                return _searchElement.SetValue;
            }

            set
            {
                if (_searchElement != null)
                {
                    _searchElement.SetValue = value;
                }
            }
        }

        private void ListValueChanged(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                // Don't allow it to toggle off
                _listElementToggle.SetValueWithoutNotify(true);
                return;
            }

            _searchToggle.SetValueWithoutNotify(false);
            Remove(_searchElement);
            Add(_listElement);

            _listElement.ForceUpdate();
        }

        private void SearchValueChanged(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                // Don't allow it to toggle off
                _searchToggle.SetValueWithoutNotify(true);
                return;
            }

            _listElementToggle.SetValueWithoutNotify(false);
            Remove(_listElement);
            Add(_searchElement);
        }

        private void Update()
        {
            if (!_listElement.IsValid())
            {
                RemoveFromHierarchy();
                return;
            }

            _listElement.Update();
            _searchElement?.Update();
        }
    }
}