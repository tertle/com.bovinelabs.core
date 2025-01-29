// <copyright file="DynamicHashMapSearchElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Iterators;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities.UI;
    using Unity.Properties;
    using UnityEngine;
    using SearchElement = BovineLabs.Core.Editor.UI.SearchElement;

    public class DynamicHashMapSearchElement<T, TBuffer, TKey, TValue> : EntityInspector<T>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly TValue defaultValue;
        private readonly PropertyElement content;

        private TKey? current;

        public DynamicHashMapSearchElement(object inspector, List<SearchView.Item> items, TValue defaultValue = default, int refreshRate = 250)
            : base(inspector)
        {
            this.defaultValue = defaultValue;
            var popup = new SearchElement(items, "Stat");

            this.content = new PropertyElement();
            this.content.AddContext(this.Context.Context);
            this.content.OnChanged += this.OnComponentChanged;

            popup.OnSelection += evt => this.UpdateValue(evt.Data);

            if (items.Count > 0)
            {
                popup.SetValue(0);
            }

            if (items.Count > 0)
            {
                this.UpdateValue(items[0].Data);
            }

            this.Add(popup);
            this.Add(this.content);

            if (refreshRate > 0)
            {
                this.schedule.Execute(this.Update).Every(refreshRate);
            }
        }

        private DynamicHashMap<TKey, TValue> GetMap()
        {
            return this.Context.EntityManager.GetBuffer<TBuffer>(this.Context.Entity).AsHashMap<TBuffer, TKey, TValue>();
        }

        public unsafe void Update()
        {
            if (!this.IsValid())
            {
                return;
            }

            if (this.current == null)
            {
                return;
            }

            var oldValue = this.content.GetTarget<TValue>();

            var map = this.GetMap();
            var value = this.TryGetValue(map, this.current.Value);

            if (UnsafeUtility.MemCmp(&oldValue, &value, UnsafeUtility.SizeOf<TValue>()) != 0)
            {
                this.content.SetTarget(value);
            }
        }

        private void OnComponentChanged(BindingContextElement element, PropertyPath path)
        {
            if (!this.IsValid() || this.current == null)
            {
                return;
            }

            if (this.Context.IsReadOnly)
            {
                return;
            }

            var value = element.GetTarget<TValue>();
            var map = this.GetMap();
            map[this.current.Value] = value;
        }

        private void UpdateValue(object data)
        {
            if (!this.IsValid())
            {
                return;
            }

            if (data is not TKey key)
            {
                Debug.LogError($"List item {data} was not type of {typeof(TKey)}");
                return;
            }

            var map = this.GetMap();

            this.current = key;
            var value = this.TryGetValue(map, key);
            this.content.SetTarget(value);
        }

        private TValue TryGetValue(DynamicHashMap<TKey, TValue> map, TKey key)
        {
            if (!map.TryGetValue(key, out var value))
            {
                value = this.defaultValue;
            }

            return value;
        }
    }
}
