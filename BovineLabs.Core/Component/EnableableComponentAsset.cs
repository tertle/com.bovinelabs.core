// <copyright file="EnableableComponentAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.PropertyDrawers;
    using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Enableable", fileName = "Enableable")]
    public class EnableableComponentAsset : ComponentAssetBase
    {
        [SerializeField]
        [StableTypeHash(StableTypeHashAttribute.TypeCategory.BufferData | StableTypeHashAttribute.TypeCategory.ComponentData, AllowEditorAssemblies = false,
            OnlyEnableable = true)]
        private ulong component;

        protected override ulong Component => this.component;

        protected override void CustomValidation(TypeIndex typeIndex)
        {
            if (!TypeManager.IsEnableable(typeIndex))
            {
                throw new InvalidCastException($"Type {this.Component} on {this.name} is no longer enableable");
            }
        }
    }
}