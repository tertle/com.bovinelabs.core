// <copyright file="RefRWExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    public static unsafe class RefRWExtensions
    {
        public static RefRW<T> Create<T>(T* ptr, int index, ComponentTypeHandle<T> handle)
            where T : unmanaged, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRW<T>((byte*)(ptr + index), handle.GetSafety());
#else
            return new RefRW<T>((byte*)(ptr + index));
#endif
        }
    }
}
