// <copyright file="ComponentTypeHandleExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class ComponentTypeHandleExtensions
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetSafety<T>(this ComponentTypeHandle<T> compo)
        {
            return compo.m_Safety;
        }
#endif
    }
}
