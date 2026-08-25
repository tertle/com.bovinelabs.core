// <copyright file="ComponentAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Component", fileName = "Component")]
    public class ComponentAsset : TypeAsset
    {
        public ulong GetStableTypeHash()
        {
            var typeIndex = TypeManager.GetTypeIndex(this.ResolveType());
            return TypeManager.GetTypeInfo(typeIndex).StableTypeHash;
        }

        public override Type ResolveType()
        {
            var type = base.ResolveType();
            this.ValidateType(TypeManager.GetTypeIndex(type));
            return type;
        }

        protected virtual void ValidateType(TypeIndex typeIndex)
        {
        }
    }
}
