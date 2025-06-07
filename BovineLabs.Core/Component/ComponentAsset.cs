// <copyright file="ComponentAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.PropertyDrawers;
    using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Component", fileName = "Component")]
    public class ComponentAsset : ScriptableObject
    {
        [SerializeField]
        [InspectorReadOnly]
        private string componentName;

        [StableTypeHash(
            StableTypeHashAttribute.TypeCategory.BufferData | StableTypeHashAttribute.TypeCategory.ComponentData, AllowEditorAssemblies = false)]
        [SerializeField]
        private ulong component;

        public ulong GetStableTypeHash()
        {
#if UNITY_EDITOR
            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(this.component);
            if (typeIndex == TypeIndex.Null)
            {
                throw new InvalidCastException($"Type not found for stable type hash {this.component} on {this.name}");
            }
#endif

            return this.component;
        }

        public Type GetComponentType()
        {
            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(this.component);
            if (typeIndex == TypeIndex.Null)
            {
                throw new InvalidCastException($"Type not found for stable type hash {this.component} on {this.name}");
            }

            return TypeManager.GetType(typeIndex);
        }
    }
}
