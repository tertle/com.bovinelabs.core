// <copyright file="ICacheComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Cache
{
    using Unity.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe interface ICacheComponent<T> : IComponentData
        where T : unmanaged, IEntityCache
    {
        UnsafeList<T>* Cache { get; set; }
    }

    public static unsafe class CacheComponentExtensions
    {
        public static ref T GetRef<T, TC>(this TC cacheComponent, int index)
            where TC : unmanaged, ICacheComponent<T>
            where T : unmanaged, IEntityCache
        {
            Assert.IsFalse(cacheComponent.Cache == null, "Cache == null which means you are accessing it before the cache system has updated.");
            Assert.IsTrue(index >= 0 && index < cacheComponent.Cache->Length, "Out of range");

            return ref UnsafeUtility.AsRef<T>(cacheComponent.Cache->Ptr + index);
        }
    }
}
