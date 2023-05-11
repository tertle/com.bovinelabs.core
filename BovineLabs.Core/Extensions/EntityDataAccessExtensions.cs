// <copyright file="EntityDataAccessExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Burst.CompilerServices;
    using Unity.Entities;

    public static unsafe class EntityDataAccessExtensions
    {
        internal static byte* GetComponentDataWithTypeRO(ref this EntityDataAccess access, Chunk* chunk, int indexInChunk, TypeIndex typeIndex, ref LookupCache cache)
        {
            return ChunkDataUtility.GetComponentDataWithTypeRO(chunk, chunk->Archetype, indexInChunk, typeIndex, ref cache);
        }

        internal static byte* GetComponentDataWithTypeRW(ref this EntityDataAccess access, Chunk* chunk, int indexInChunk, Entity entity, TypeIndex typeIndex, uint globalVersion, ref LookupCache cache)
        {
            var data = ChunkDataUtility.GetComponentDataWithTypeRW(chunk, chunk->Archetype, indexInChunk, typeIndex, globalVersion, ref cache);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(access.EntityComponentStore->m_RecordToJournal != 0))
            {
                JournalAddRecord(access.EntityComponentStore, entity, typeIndex, globalVersion, data);
            }
#endif

            return data;
        }


#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void JournalAddRecord(EntityComponentStore* store, Entity entity, TypeIndex typeIndex, uint version, void* data)
        {
            EntitiesJournaling.RecordType recordType;
            void* recordData = null;
            int recordDataLength = 0;
            if (TypeManager.IsSharedComponentType(typeIndex))
            {
                // Getting RW data pointer on shared components should not be allowed
                return;
            }
            else if (TypeManager.IsManagedComponent(typeIndex))
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

            EntitiesJournaling.AddRecord(
                recordType: recordType,
                entityComponentStore: store,
                globalSystemVersion: version,
                entities: &entity,
                entityCount: 1,
                types: &typeIndex,
                typeCount: 1,
                data: recordData,
                dataLength: recordDataLength);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetComponentEnabled(ref this EntityDataAccess access, in SystemHandle originSystem, Entity* entities, int entityCount, TypeIndex type, bool value)
        {
            EntitiesJournaling.AddRecord(
                recordType: value ? EntitiesJournaling.RecordType.EnableComponent : EntitiesJournaling.RecordType.DisableComponent,
                worldSequenceNumber: access.m_WorldUnmanaged.SequenceNumber,
                executingSystem: access.m_WorldUnmanaged.ExecutingSystem,
                originSystem: in originSystem,
                entities: entities,
                entityCount: entityCount,
                types: &type,
                typeCount: 1);
        }
#endif
    }
}
