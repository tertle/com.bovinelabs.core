// <copyright file="ComponentEnableableAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Enableable", fileName = "Enableable")]
    public class ComponentEnableableAsset : ComponentAsset
    {
        protected override void ValidateType(TypeIndex typeIndex)
        {
            if (!TypeManager.IsEnableable(typeIndex))
            {
                throw new InvalidCastException(
                    $"Type '{TypeManager.GetType(typeIndex)}' assigned to {nameof(ComponentEnableableAsset)} '{this.name}' is not enableable.");
            }
        }
    }
}
