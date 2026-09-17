namespace BovineLabs.Core.Extensions
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeParallelHashSetExtensions
    {
        public static int Reserve<TKey>([NoAlias] this NativeParallelHashSet<TKey>.ParallelWriter hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return hashMap.GetData().Reserve(length);
        }

        public static UnsafeParallelHashMapBucketData GetUnsafeBucketData<TKey>([NoAlias] this NativeParallelHashSet<TKey>.ParallelWriter hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return hashMap.GetData().GetUnsafeBucketData();
        }

        public static void ClearLengthBuckets<TKey>([NoAlias] this NativeParallelHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.GetData().ClearLengthBuckets();
        }

        public static void CopyToNativeList<T>(this NativeParallelHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.GetData().GetHashMapStorage();
            var count = hashSet.Count(); // Writers must be complete before counting and copying.
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(data.GetBuffer(), list.AsArray());
        }

        public static void CopyToNativeList<T>(this UnsafeParallelHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.GetData();
            var count = hashSet.Count(); // Writers must be complete before counting and copying.
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(data.GetBuffer(), list.AsArray());
        }

        public static void AddBatchUnsafe<T>([NoAlias] this NativeParallelHashSet<T> hashMap, [NoAlias] NativeArray<T> keys)
            where T : unmanaged, IEquatable<T>
        {
            hashMap.GetData().AddBatchUnsafe((T*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<T>([NoAlias] this NativeParallelHashSet<T> hashMap, [NoAlias] T* values, int length)
            where T : unmanaged, IEquatable<T>
        {
            hashMap.GetData().AddBatchUnsafe(values, length);
        }

        public static void RecalculateBuckets<TKey>([NoAlias] this NativeParallelHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.GetData().RecalculateBuckets();
        }

        public static T FirstKey<T>(this NativeParallelHashSet<T> map)
            where T : unmanaged, IEquatable<T>
        {
            return map.GetData().GetHashMapStorage().GetBuffer()->FirstKey<T>();
        }

        public static bool TryGetFirstKey<T>(this NativeParallelHashSet<T> map, out T key)
            where T : unmanaged, IEquatable<T>
        {
            var startIndex = 0;
            return map.GetData().GetHashMapStorage().GetBuffer()->TryGetFirstKey<T>(out key, ref startIndex);
        }

        public static bool TryGetFirstKey<T>(this NativeParallelHashSet<T> map, out T key, ref int index)
            where T : unmanaged, IEquatable<T>
        {
            return map.GetData().GetHashMapStorage().GetBuffer()->TryGetFirstKey<T>(out key, ref index);
        }
    }
}
