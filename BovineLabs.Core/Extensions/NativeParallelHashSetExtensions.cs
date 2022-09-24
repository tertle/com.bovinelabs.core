// <copyright file="NativeParallelHashSetExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class NativeParallelHashSetExtensions
    {
        public static unsafe void CopyToNativeList<T>(this NativeParallelHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.m_Data.m_HashMapData;
            var count = data.Count();
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(data.m_Buffer, list.AsArray());
        }

        public static unsafe void CopyToNativeList<T>(this UnsafeParallelHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.m_Data;
            var count = data.Count();
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(data.m_Buffer, list.AsArray());
        }

        public static unsafe void AddBatchUnsafe<T>([NoAlias] this NativeParallelHashSet<T> hashMap, [NoAlias] NativeArray<T> keys)
            where T : unmanaged, IEquatable<T>
        {
            hashMap.m_Data.AddBatchUnsafe((T*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static unsafe void AddBatchUnsafe<T>([NoAlias] this NativeParallelHashSet<T> hashMap, [NoAlias] T* values, int length)
            where T : unmanaged, IEquatable<T>
        {
            hashMap.m_Data.AddBatchUnsafe(values, length);
        }

        public static unsafe T FirstKey<T>(this NativeParallelHashSet<T> map)
            where T : unmanaged, IEquatable<T>
        {
            return map.m_Data.m_HashMapData.m_Buffer->FirstKey<T>();
        }

        public static unsafe bool TryGetFirstKey<T>(this NativeParallelHashSet<T> map, out T key)
            where T : unmanaged, IEquatable<T>
        {
            var startIndex = 0;
            return map.m_Data.m_HashMapData.m_Buffer->TryGetFirstKey<T>(out key, ref startIndex);
        }

        public static unsafe bool TryGetFirstKey<T>(this NativeParallelHashSet<T> map, out T key, ref int index)
            where T : unmanaged, IEquatable<T>
        {
            return map.m_Data.m_HashMapData.m_Buffer->TryGetFirstKey<T>(out key, ref index);
        }
    }
}
