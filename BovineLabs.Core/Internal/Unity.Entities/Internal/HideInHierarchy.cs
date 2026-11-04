// <copyright file="HideInHierarchy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR
namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class HideInHierarchy
    {
        public static ComponentType Type => ComponentType.ReadWrite<Unity.Entities.HideInHierarchy>();
    }
}
#endif
