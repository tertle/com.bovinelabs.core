// <copyright file="EntityDataAccessExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityDataAccessExtensions
    {
        internal static byte* GetComponentDataWithTypeRO(
            ref this EntityDataAccess access, ChunkIndex chunk, Archetype* archetype, int indexInChunk, TypeIndex typeIndex, ref LookupCache cache)
        {
            return ChunkDataUtility.GetComponentDataWithTypeRO(chunk, archetype, indexInChunk, typeIndex, ref cache);
        }

        internal static byte* GetComponentDataWithTypeRW(
            ref this EntityDataAccess access, ChunkIndex chunk, Archetype* archetype, int indexInChunk, Entity entity, TypeIndex typeIndex, uint globalVersion,
            ref LookupCache cache)
        {
            var data = ChunkDataUtility.GetComponentDataWithTypeRW(chunk, archetype, indexInChunk, typeIndex, globalVersion, ref cache);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(access.EntityComponentStore->m_RecordToJournal != 0))
            {
                JournalAddRecord(access.EntityComponentStore, entity, typeIndex, globalVersion, data);
            }
#endif

            return data;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal static UntypedDynamicBuffer GetUntypedBuffer(
            ref this EntityDataAccess access, ComponentType componentType, Entity entity, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety,
            bool isReadOnly = false)
#else
        internal static UntypedDynamicBuffer GetUntypedBuffer(
            ref this EntityDataAccess access, ComponentType componentType, Entity entity, bool isReadOnly = false)
#endif
        {
            var typeIndex = componentType.TypeIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            access.EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!TypeManager.IsBuffer(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();

                throw new ArgumentException(
                    $"GetBuffer<{typeName}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo(typeIndex).Category}");
            }
#endif

            if (!access.IsInExclusiveTransaction)
            {
                if (isReadOnly)
                {
                    access.DependencyManager->CompleteWriteDependency(typeIndex);
                }
                else
                {
                    access.DependencyManager->CompleteReadAndWriteDependency(typeIndex);
                }
            }

            BufferHeader* header;
            if (isReadOnly)
            {
                header = (BufferHeader*)access.EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            }
            else
            {
                header = (BufferHeader*)access.EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    access.EntityComponentStore->GlobalSystemVersion);
            }

            var internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            var typeInfo = TypeManager.GetTypeInfo(typeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var useMemoryInit = access.EntityComponentStore->useMemoryInitPattern != 0;
            var memoryInitPattern = access.EntityComponentStore->memoryInitPattern;

            return new UntypedDynamicBuffer(header, safety, arrayInvalidationSafety, isReadOnly, useMemoryInit, memoryInitPattern, internalCapacity,
                typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
#else
            return new UntypedDynamicBuffer(header, internalCapacity, typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
#endif
        }

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void JournalAddRecord(EntityComponentStore* store, Entity entity, TypeIndex typeIndex, uint version, void* data)
        {
            EntitiesJournaling.RecordType recordType;
            void* recordData = null;
            var recordDataLength = 0;
            if (TypeManager.IsSharedComponentType(typeIndex))
            {
                // Getting RW data pointer on shared components should not be allowed
                return;
            }

            if (TypeManager.IsManagedComponent(typeIndex))
            {
                recordType = EntitiesJournaling.RecordType.GetComponentObjectRW;
            }
            else if (TypeManager.IsBuffer(typeIndex))
            {
                recordType = EntitiesJournaling.RecordType.GetBufferRW;
            }
            else
            {
                recordType = EntitiesJournaling.RecordType.GetComponentDataRW;
                recordData = data;
                recordDataLength = TypeManager.GetTypeInfo(typeIndex).TypeSize;
            }

            EntitiesJournaling.AddRecord(recordType, store, version, &entity, 1, types: &typeIndex, typeCount: 1, data: recordData,
                dataLength: recordDataLength);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetComponentEnabled(
            ref this EntityDataAccess access, in SystemHandle originSystem, Entity* entities, int entityCount, TypeIndex type, bool value)
        {
            EntitiesJournaling.AddRecord(value ? EntitiesJournaling.RecordType.EnableComponent : EntitiesJournaling.RecordType.DisableComponent,
                access.m_WorldUnmanaged.SequenceNumber, access.m_WorldUnmanaged.ExecutingSystem, originSystem: in originSystem, entities: entities,
                entityCount: entityCount, types: &type, typeCount: 1);
        }
#endif
    }
}
