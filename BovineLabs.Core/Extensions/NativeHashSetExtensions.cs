namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class NativeHashSetExtensions
    {
        public static unsafe void ToNativeArray<T>(this NativeHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.m_Data.m_HashMapData;
            var count = data.Count();
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(data.m_Buffer, list.AsArray());
        }

        public static unsafe void ToNativeArray<T>(this UnsafeHashSet<T> hashSet, NativeList<T> list)
            where T : unmanaged, IEquatable<T>
        {
            var data = hashSet.m_Data;
            var count = data.Count();
            list.Resize(count, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(data.m_Buffer, list.AsArray());
        }
    }
}
