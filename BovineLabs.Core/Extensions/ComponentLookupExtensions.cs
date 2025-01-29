// <copyright file="ComponentLookupExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Assertions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    internal unsafe struct ComponentLookupInternal
    {
        [NativeDisableUnsafePtrRestriction]
        public readonly EntityDataAccess* m_Access;

        public LookupCache m_Cache;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle m_Safety;
#endif
        public readonly TypeIndex m_TypeIndex;
        public uint m_GlobalSystemVersion;
        public readonly byte m_IsZeroSized; // cache of whether T is zero-sized
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public readonly byte m_IsReadOnly;
#endif
    }

    public static unsafe class ComponentLookupExtensions
    {
        public static T* GetOptionalComponentDataRW<T>(ref this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(lookupInternal.m_Safety);
#endif
            AssertExistsNotZeroSize(ref lookupInternal);

            var ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntitiesExist(&entity, 1);

            return (T*)ecs->GetOptionalComponentDataWithTypeRW(entity, lookupInternal.m_TypeIndex, lookupInternal.m_GlobalSystemVersion,
                ref lookupInternal.m_Cache);
        }

        public static RefRW<T> GetRefRWNoChangeFilter<T>(ref this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(lookupInternal.m_Safety);
#endif
            var ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

            if (lookupInternal.m_IsZeroSized != 0)
            {
                return default;
            }

            void* ptr = ecs->GetComponentDataWithTypeRO(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRW<T>(ptr, lookupInternal.m_Safety);
#else
            return new RefRW<T>(ptr);
#endif
        }

        public static EnabledRefRW<T> GetEnableRefRWNoChangeFilter<T>(ref this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(lookup.m_Safety);
#endif
            var ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

            int indexInBitField;
            int* ptrChunkDisabledCount;
            var ptr = ecs->GetEnabledRawRO(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache, out indexInBitField, out ptrChunkDisabledCount);

            return new EnabledRefRW<T>(MakeSafeBitRef(lookup, ptr, indexInBitField), ptrChunkDisabledCount);
        }

        public static void SetChangeFilter<T>(ref this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(lookupInternal.m_Safety);
#endif
            var ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);

            if (Hint.Unlikely(archetype != lookupInternal.m_Cache.Archetype))
            {
                lookupInternal.m_Cache.Update(archetype, lookupInternal.m_TypeIndex);
            }

            var typeIndexInArchetype = lookupInternal.m_Cache.IndexInArchetype;
            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.ListIndex, lookup.GlobalSystemVersion);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(lookupInternal.m_Access->EntityComponentStore->m_RecordToJournal != 0))
            {
                lookupInternal.m_Access->EntityComponentStore->GetComponentDataWithTypeRW(entity, lookupInternal.m_TypeIndex,
                    lookupInternal.m_GlobalSystemVersion, ref lookupInternal.m_Cache);
            }
#endif
        }

        public static bool TryGetComponent<T>(ref this ComponentLookup<T> lookup, ref EntityCache cache, out T componentData)
            where T : unmanaged, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety);
#endif
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

            if (Hint.Unlikely(!cache.Exists))
            {
                componentData = default;
                return false;
            }

            if (Hint.Unlikely(lookupInternal.m_IsZeroSized != 0))
            {
                componentData = default;
                return cache.HasComponent(ref lookupInternal.m_Cache, lookupInternal.m_TypeIndex);
            }

            void* ptr = cache.GetOptionalComponentDataWithTypeRO(lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);
            if (ptr == null)
            {
                componentData = default;
                return false;
            }

            UnsafeUtility.CopyPtrToStructure(ptr, out componentData);
            return true;
        }

        public static T GetComponentRequired<T>(ref this ComponentLookup<T> lookup, ref EntityCache cache)
            where T : unmanaged, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(lookup.m_Safety);
#endif
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(cache.Exists);
            Assert.IsTrue(cache.HasComponent(ref lookupInternal.m_Cache, lookupInternal.m_TypeIndex));
#endif

            if (Hint.Unlikely(lookupInternal.m_IsZeroSized != 0))
            {
                return default;
            }

            void* ptr = cache.GetComponentDataWithTypeRO(lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);
            UnsafeUtility.CopyPtrToStructure(ptr, out T componentData);
            return componentData;
        }

        public static T GetChunkComponent<T>(ref this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            return GetChunkComponent(ref lookup, entity, out _);
        }

        public static T GetChunkComponent<T>(ref this ComponentLookup<T> lookup, Entity entity, out int indexInChunk)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);
            var ecs = lookupInternal.m_Access->EntityComponentStore;

            var chunk = ecs->GetChunk(entity);
            // var archetype = ecs->GetArchetype(chunk);
            var entityInChunk = ecs->GetEntityInChunk(entity);

            indexInChunk = entityInChunk.IndexInChunk;

            var ptr = ecs->GetComponentDataWithTypeRO(chunk.MetaChunkEntity, lookupInternal.m_TypeIndex);
            UnsafeUtility.CopyPtrToStructure(ptr, out T value);
            return value;
        }

        private static SafeBitRef MakeSafeBitRef<T>(in ComponentLookup<T> lookup, ulong* ptr, int offsetInBits)
            where T : unmanaged, IComponentData, IEnableableComponent
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            =>
                new(ptr, offsetInBits, lookup.m_Safety);
#else
            => new(ptr, offsetInBits);
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void AssertExistsNotZeroSize(ref ComponentLookupInternal lookup)
        {
            if (lookup.m_IsZeroSized != 0)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new ArgumentException("zero sized component");
#endif
            }
        }
    }
}
