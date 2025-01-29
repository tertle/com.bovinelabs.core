// <copyright file="BufferLookupExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    // Must match BufferLookup<T> layout
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BufferLookupInternal<T>
        where T : unmanaged, IBufferElementData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;

#endif
        [NativeDisableUnsafePtrRestriction]
        internal readonly EntityDataAccess* m_Access;

        internal LookupCache m_Cache;
        internal readonly TypeIndex m_TypeIndex;

        internal uint m_GlobalSystemVersion;
        internal int m_InternalCapacity;
        internal readonly byte m_IsReadOnly;
    }

    public static class BufferLookupExtensions
    {
        public static unsafe DynamicBuffer<T> GetRO<T>(ref this BufferLookup<T> lookup, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            ref var lookupInternal = ref UnsafeUtility.As<BufferLookup<T>, BufferLookupInternal<T>>(ref lookup);

            var ecs = lookupInternal.m_Access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety0);
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);
#endif

            var header = (BufferHeader*)ecs->GetComponentDataWithTypeRO(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(header, lookup.m_Safety0, lookup.m_ArrayInvalidationSafety, lookupInternal.m_IsReadOnly != 0, false, 0,
                lookupInternal.m_InternalCapacity);
#else
            return new DynamicBuffer<T>(header, lookupInternal.m_InternalCapacity);
#endif
        }

        public static unsafe (DynamicBuffer<T> Buffer, int ChunkIndex) GetROAndChunk<T>(ref this BufferLookup<T> lookup, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            ref var lookupInternal = ref UnsafeUtility.As<BufferLookup<T>, BufferLookupInternal<T>>(ref lookup);

            var ecs = lookupInternal.m_Access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety0);
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);
#endif

            var entityInChunk = ecs->GetEntityInChunk(entity);

            var header = (BufferHeader*)ChunkDataUtility.GetComponentDataWithTypeRO(entityInChunk.Chunk, ecs->GetArchetype(entityInChunk.Chunk),
                entityInChunk.IndexInChunk, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var buffer = new DynamicBuffer<T>(header, lookup.m_Safety0, lookup.m_ArrayInvalidationSafety, lookupInternal.m_IsReadOnly != 0, false, 0,
                lookupInternal.m_InternalCapacity);
#else
            var buffer = new DynamicBuffer<T>(header, lookupInternal.m_InternalCapacity);
#endif

            return (buffer, entityInChunk.Chunk);
        }

        public static unsafe bool TryGetBuffer<T>(ref this BufferLookup<T> lookup, ref EntityCache cache, out DynamicBuffer<T> bufferData)
            where T : unmanaged, IBufferElementData
        {
            ref var lookupInternal = ref UnsafeUtility.As<BufferLookup<T>, BufferLookupInternal<T>>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety0);
#endif
            if (Hint.Unlikely(!cache.Exists))
            {
                bufferData = default;
                return false;
            }

            var header = lookupInternal.m_IsReadOnly != 0
                ? (BufferHeader*)cache.GetOptionalComponentDataWithTypeRO(lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache)
                : (BufferHeader*)cache.GetOptionalComponentDataWithTypeRW(lookupInternal.m_TypeIndex, lookupInternal.m_GlobalSystemVersion,
                    ref lookupInternal.m_Cache);

            if (header != null)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                bufferData = new DynamicBuffer<T>(header, lookupInternal.m_Safety0, lookupInternal.m_ArrayInvalidationSafety, lookupInternal.m_IsReadOnly != 0,
                    false, 0, lookupInternal.m_InternalCapacity);
#else
                bufferData = new DynamicBuffer<T>(header, lookupInternal.m_InternalCapacity);
#endif
                return true;
            }

            bufferData = default;
            return false;
        }

        public static unsafe DynamicBuffer<T> GetBufferRequired<T>(ref this BufferLookup<T> lookup, ref EntityCache cache)
            where T : unmanaged, IBufferElementData
        {
            ref var lookupInternal = ref UnsafeUtility.As<BufferLookup<T>, BufferLookupInternal<T>>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety0);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(cache.Exists);
            Assert.IsTrue(cache.HasComponent(ref lookupInternal.m_Cache, lookupInternal.m_TypeIndex));
#endif

            var header = lookupInternal.m_IsReadOnly != 0
                ? (BufferHeader*)cache.GetComponentDataWithTypeRO(lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache)
                : (BufferHeader*)cache.GetComponentDataWithTypeRW(lookupInternal.m_TypeIndex, lookupInternal.m_GlobalSystemVersion, ref lookupInternal.m_Cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(header, lookupInternal.m_Safety0, lookupInternal.m_ArrayInvalidationSafety, lookupInternal.m_IsReadOnly != 0, false, 0,
                lookupInternal.m_InternalCapacity);
#else
            return new DynamicBuffer<T>(header, lookupInternal.m_InternalCapacity);
#endif
        }
    }
}
