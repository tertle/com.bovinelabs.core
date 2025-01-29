// <copyright file="EntityCache.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public readonly unsafe struct EntityCache
    {
        public readonly Entity Entity;
        internal readonly bool Exists;
        internal readonly Archetype* Archetype;
        internal readonly EntityInChunk EntityInChunk;

        internal EntityCache(EntityComponentStore* ecs, Entity entity)
        {
            this.Entity = entity;

            if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
            {
                this.Exists = false;
                this.Archetype = default;
                this.EntityInChunk = default;
            }
            else
            {
                var chunk = ecs->GetChunk(entity);
                this.Archetype = ecs->GetArchetype(chunk);
                this.Exists = ecs->WorldSequenceNumber == this.Archetype->EntityComponentStore->WorldSequenceNumber;

                if (this.Exists)
                {
                    this.EntityInChunk = ecs->GetEntityInChunk(entity);
                }
                else
                {
                    this.EntityInChunk = default;
                }
            }
        }

        public int Chunk => this.EntityInChunk.Chunk;

        public static EntityCache Create<T>(ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);
            return new EntityCache(lookupInternal.m_Access->EntityComponentStore, entity);
        }

        public static EntityCache Create<T>(BufferLookup<T> lookup, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            ref var lookupInternal = ref UnsafeUtility.As<BufferLookup<T>, BufferLookupInternal<T>>(ref lookup);
            return new EntityCache(lookupInternal.m_Access->EntityComponentStore, entity);
        }

        internal bool HasComponent(ref LookupCache lookupCache, TypeIndex type)
        {
            if (Hint.Unlikely(this.Archetype != lookupCache.Archetype))
            {
                lookupCache.Update(this.Archetype, type);
            }

            return lookupCache.IndexInArchetype != -1;
        }

        internal byte* GetOptionalComponentDataWithTypeRO(TypeIndex typeIndex, ref LookupCache cache)
        {
            return ChunkDataUtility.GetOptionalComponentDataWithTypeRO(this.EntityInChunk.Chunk, this.Archetype, this.EntityInChunk.IndexInChunk, typeIndex,
                ref cache);
        }

        internal byte* GetOptionalComponentDataWithTypeRW(TypeIndex typeIndex, uint globalVersion, ref LookupCache cache)
        {
            var data = ChunkDataUtility.GetOptionalComponentDataWithTypeRW(this.EntityInChunk.Chunk, this.Archetype, this.EntityInChunk.IndexInChunk, typeIndex,
                globalVersion, ref cache);

            // TODO SUPPORT?
// #if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
//             if (data != null && Hint.Unlikely(m_RecordToJournal != 0))
//             {
//                 JournalAddRecordGetRW(entity, typeIndex, globalVersion, data);
//             }
// #endif

            return data;
        }

        internal byte* GetComponentDataWithTypeRO(TypeIndex typeIndex, ref LookupCache cache)
        {
            return ChunkDataUtility.GetComponentDataWithTypeRO(this.EntityInChunk.Chunk, this.Archetype, this.EntityInChunk.IndexInChunk, typeIndex, ref cache);
        }

        internal byte* GetComponentDataWithTypeRW(TypeIndex typeIndex, uint globalVersion, ref LookupCache cache)
        {
            var data = ChunkDataUtility.GetComponentDataWithTypeRW(this.EntityInChunk.Chunk, this.Archetype, this.EntityInChunk.IndexInChunk, typeIndex,
                globalVersion, ref cache);

            // TODO SUPPORT?
// #if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
//             if (Burst.CompilerServices.Hint.Unlikely(m_RecordToJournal != 0))
//                 JournalAddRecordGetRW(entity, typeIndex, globalVersion, data);
// #endif

            return data;
        }
    }
}
