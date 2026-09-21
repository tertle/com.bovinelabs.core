namespace BovineLabs.Core
{
    using System;
    using System.Reflection;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Component Field", fileName = "ComponentField")]
    public class ComponentFieldAsset : ScriptableObject
    {
        [SerializeField]
        private ComponentAsset _component;

        [SerializeField]
        private string _fieldName = string.Empty;

        // We use a non-serialized cache to stop multiple calls triggering reflection
        private Cache _cache;

        public ulong GetStableTypeHash()
        {
            return GetComponentAsset().GetStableTypeHash();
        }

        public Type GetComponentType()
        {
            return GetComponentAsset().ResolveType();
        }

        public Type GetFieldType()
        {
            return GetField().FieldType;
        }

        public ushort GetOffset()
        {
            if (TryGetOffsetFromCache(out var cachedOffset))
            {
                return cachedOffset;
            }

            var field = GetField();
            var offset = (ushort)UnsafeUtility.GetFieldOffset(field);
            _cache = new Cache(this, offset);
            return offset;
        }

        private ComponentAsset GetComponentAsset()
        {
            if (_component == null)
            {
                throw new NullReferenceException($"{nameof(_component)} not set");
            }

            return _component;
        }

        private FieldInfo GetField()
        {
            if (string.IsNullOrWhiteSpace(_fieldName))
            {
                throw new NullReferenceException($"{nameof(_fieldName)} not set");
            }

            var field = GetComponentType().GetField(_fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                throw new InvalidOperationException($"FieldInfo not found for field {_fieldName} on {name}");
            }

            return field;
        }

        private bool TryGetOffsetFromCache(out ushort offset)
        {
            if (_cache.Component != _component || _cache.FieldName != _fieldName)
            {
                offset = 0;
                return false;
            }

            offset = _cache.Offset;
            return true;
        }

        private readonly struct Cache
        {
            public readonly ComponentAsset Component;
            public readonly string FieldName;
            public readonly ushort Offset;

            public Cache(ComponentFieldAsset asset, ushort offset)
            {
                Component = asset._component;
                FieldName = asset._fieldName;
                Offset = offset;
            }
        }
    }
}
