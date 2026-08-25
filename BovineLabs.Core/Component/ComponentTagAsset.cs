// <copyright file="ComponentTagAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Tag", fileName = "Tag")]
    public class ComponentTagAsset : ComponentAsset
    {
        protected override void ValidateType(TypeIndex typeIndex)
        {
            if (!TypeManager.GetTypeInfo(typeIndex).IsZeroSized)
            {
                throw new InvalidCastException(
                    $"Type '{TypeManager.GetType(typeIndex)}' assigned to {nameof(ComponentTagAsset)} '{this.name}' is not zero-sized.");
            }
        }
    }
}
