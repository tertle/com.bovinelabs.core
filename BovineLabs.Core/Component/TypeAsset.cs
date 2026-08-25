// <copyright file="TypeAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Type", fileName = "Type")]
    public class TypeAsset : ScriptableObject
    {
        public const string SearchProviderType = "types";

        [SerializeField]
        private string typeName;

        public virtual Type ResolveType()
        {
            if (string.IsNullOrWhiteSpace(this.typeName))
            {
                throw new InvalidOperationException($"{this.GetType().Name} '{this.name}' does not have a type assigned.");
            }

            return Type.GetType(this.typeName) ?? throw new TypeLoadException(
                $"{this.GetType().Name} '{this.name}' could not resolve type '{this.typeName}'. " +
                "The type may have been renamed, moved to another assembly, or removed.");
        }
    }
}
