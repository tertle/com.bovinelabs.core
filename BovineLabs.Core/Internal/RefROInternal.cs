// <copyright file="RefROInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class RefROInternal<T>
        where T : unmanaged, IComponentData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static RefRO<T> Create(void* ptr, AtomicSafetyHandle safety)
        {
            return new RefRO<T>(ptr, safety);
        }
#else
        public static RefRO<T> Create(void* ptr)
        {
            return new RefRO<T>(ptr);
        }
#endif
    }
}
