// <copyright file="NativeHashSetExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeHashSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearLengthBuckets<TKey>(this ref NativeHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.CheckWrite();
            hashMap.m_Data->ClearLengthBuckets();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateBuckets<TKey>(this ref NativeHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.CheckWrite();
            hashMap.m_Data->RecalculateBuckets();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReserveAtomicNoResize<TKey>(this ref NativeHashSet<TKey> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return hashMap.m_Data->ReserveAtomicNoResize(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCount<TKey>(this ref NativeHashSet<TKey> hashMap, int count)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.m_Data->SetCount(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey* GetKeys<TKey>(this in NativeHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return hashMap.m_Data->Keys;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWrite<TKey>(this ref NativeHashSet<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.m_Safety);
#endif
        }
    }
}
