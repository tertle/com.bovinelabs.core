namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Iterators;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities.UI;
    using Unity.Properties;
    using SearchElement = BovineLabs.Core.Editor.UI.SearchElement;

    public class DynamicHashMapSearchElement<T, TBuffer, TKey, TValue> : EntityInspector<T>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly TValue _defaultValue;
        private readonly PropertyElement _content;

        private TKey? _current;

        public DynamicHashMapSearchElement(object inspector, List<SearchView.Item> items, TValue defaultValue = default, int refreshRate = 250)
            : base(inspector)
        {
            _defaultValue = defaultValue;
            SetValue = SetValueDirect; // Default Value
            var popup = new SearchElement(items, string.Empty);

            _content = new PropertyElement();
            _content.AddContext(Context.Context);
            _content.OnChanged += OnComponentChanged;

            popup.OnSelection += evt => UpdateValue(evt.Data);

            if (items.Count > 0)
            {
                popup.SetValue(0);
            }

            if (items.Count > 0)
            {
                UpdateValue(items[0].Data);
            }

            Add(popup);
            Add(_content);

            if (refreshRate > 0)
            {
                schedule.Execute(Update).Every(refreshRate);
            }
        }

        public Action<IEntityContext, TKey, TValue> SetValue { get; set; }

        private DynamicHashMap<TKey, TValue> GetMap(bool isReadOnly = true)
        {
            return Context.EntityManager.GetBuffer<TBuffer>(Context.Entity, isReadOnly).AsHashMap<TBuffer, TKey, TValue>();
        }

        public unsafe void Update()
        {
            if (!IsValid())
            {
                return;
            }

            if (_current == null)
            {
                return;
            }

            var oldValue = _content.GetTarget<ValueStruct>();

            var map = GetMap();
            var value = TryGetValue(map, _current.Value);

            if (UnsafeUtility.MemCmp(&oldValue.Value, &value, UnsafeUtility.SizeOf<TValue>()) != 0)
            {
                _content.SetTarget(new ValueStruct { Value = value });
            }
        }

        private void OnComponentChanged(BindingContextElement element, PropertyPath path)
        {
            if (!IsValid() || _current == null)
            {
                return;
            }

            if (Context.IsReadOnly)
            {
                return;
            }

            var value = element.GetTarget<ValueStruct>();
            SetValue(Context, _current!.Value, value.Value);
        }

        private void SetValueDirect(IEntityContext context, TKey key, TValue value)
        {
            var map = GetMap(false);
            map[key] = value;
        }

        private void UpdateValue(object data)
        {
            if (!IsValid())
            {
                return;
            }

            if (data is not TKey key)
            {
                BLGlobalLogger.LogErrorString($"List item {data} was not type of {typeof(TKey)}");
                return;
            }

            var map = GetMap();

            _current = key;
            var value = TryGetValue(map, key);
            _content.SetTarget(new ValueStruct { Value = value });
        }

        private TValue TryGetValue(DynamicHashMap<TKey, TValue> map, TKey key)
        {
            if (!map.TryGetValue(key, out var value))
            {
                value = _defaultValue;
            }

            return value;
        }

        public struct ValueStruct
        {
            public TValue Value;
        }
    }
}