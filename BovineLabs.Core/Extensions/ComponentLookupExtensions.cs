// <copyright file="ComponentLookupExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

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

            return (T*)ecs->GetOptionalComponentDataWithTypeRW(entity, lookupInternal.m_TypeIndex, lookupInternal.m_GlobalSystemVersion, ref lookupInternal.m_Cache);
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
            EntityComponentStore* ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache);

            int indexInBitField;
            int* ptrChunkDisabledCount;
            var ptr = ecs->GetEnabledRawRO(
                entity, lookupInternal.m_TypeIndex, ref lookupInternal.m_Cache, out indexInBitField, out ptrChunkDisabledCount);

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
        }

        private static SafeBitRef MakeSafeBitRef<T>(in ComponentLookup<T> lookup, ulong* ptr, int offsetInBits)
            where T : unmanaged, IComponentData, IEnableableComponent
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            => new(ptr, offsetInBits, lookup.m_Safety);
#else
            => new(ptr, offsetInBits);
#endif

        private struct ComponentLookupInternal
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
