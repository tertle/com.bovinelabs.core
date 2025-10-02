// <copyright file="ComponentAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using BovineLabs.Core.PropertyDrawers;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Component", fileName = "Component")]
    public class ComponentAsset : ComponentAssetBase
    {
        [SerializeField]
        [StableTypeHash(
            StableTypeHashAttribute.TypeCategory.BufferData | StableTypeHashAttribute.TypeCategory.ComponentData, AllowEditorAssemblies = false)]
        private ulong component;

        protected override ulong Component => this.component;
    }
}
