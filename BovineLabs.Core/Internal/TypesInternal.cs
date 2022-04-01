// <copyright file="TypesInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_DISABLE_MANAGED_COMPONENTS && !DOTS_HYBRID_COMPONENTS_DEBUG
namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class TypesInternal
    {
        public static ComponentType CompanionLink => typeof(Unity.Entities.CompanionLink);
    }
}
#endif