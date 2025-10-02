// <copyright file="ComponentAssetBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.PropertyDrawers;
    using Unity.Entities;
    using UnityEngine;

    public abstract class ComponentAssetBase : ScriptableObject
    {
        [SerializeField]
        [InspectorReadOnly]
        private string componentName;

        protected abstract ulong Component { get; }

        public ulong GetStableTypeHash()
        {
#if UNITY_EDITOR
            this.GetTypeIndexWithValidation();
#endif
            return this.Component;
        }

        public Type GetComponentType()
        {
            var typeIndex = this.GetTypeIndexWithValidation();
            return TypeManager.GetType(typeIndex);
        }

        protected virtual void CustomValidation(TypeIndex typeIndex)
        {
        }

        private TypeIndex GetTypeIndexWithValidation()
        {
            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(this.Component);
            if (typeIndex == TypeIndex.Null)
            {
                throw new InvalidCastException($"Type not found for stable type hash {this.Component} on {this.name}");
            }

            this.CustomValidation(typeIndex);
            return typeIndex;
        }
    }
}
