// <copyright file="ComponentFieldAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private ComponentAssetBase component;

        [SerializeField]
        private string fieldName = string.Empty;

        // We use a non-serialized cache to stop multiple calls triggering reflection
        private Cache cache;

        public ushort GetOffset()
        {
            if (this.TryGetOffsetFromCache(out var cachedOffset))
            {
                return cachedOffset;
            }

            if (this.component == null)
            {
                throw new NullReferenceException($"{nameof(this.component)} not set");
            }

            if (string.IsNullOrWhiteSpace(this.fieldName))
            {
                throw new NullReferenceException($"{nameof(this.fieldName)} not set");
            }

            var type = this.component.GetComponentType();

            var field = type.GetField(this.fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                throw new InvalidOperationException($"FieldInfo not found for field {this.fieldName} on {this.name}");
            }

            var offset = (ushort)UnsafeUtility.GetFieldOffset(field);
            this.cache = new Cache(this, offset);
            return offset;
        }

        private bool TryGetOffsetFromCache(out ushort offset)
        {
            if (this.cache.Component != this.component || this.cache.FieldName != this.fieldName)
            {
                offset = 0;
                return false;
            }

            offset = this.cache.Offset;
            return true;
        }

        private readonly struct Cache
        {
            public readonly ComponentAssetBase Component;
            public readonly string FieldName;
            public readonly ushort Offset;

            public Cache(ComponentFieldAsset asset, ushort offset)
            {
                this.Component = asset.component;
                this.FieldName = asset.fieldName;
                this.Offset = offset;
            }
        }
    }
}
