namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeComponentLookup<T>
        where T : unmanaged, IComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        private readonly TypeIndex typeIndex;
        private readonly byte isZeroSized;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly byte isReadOnly;
#endif

        private LookupCache cache;
        private uint globalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal UnsafeComponentLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.typeIndex = typeIndex;
            this.access = access;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }
#else
        internal UnsafeComponentLookup(int typeIndex, EntityDataAccess* access)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }
#endif

        public T this[Entity entity]
        {
            get
            {
                var ecs = this.access->EntityComponentStore;
                ecs->AssertEntityHasComponent(entity, this.typeIndex, ref this.cache);

                if (this.isZeroSized != 0)
                {
                    return default;
                }

                void* ptr = ecs->GetComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache);
                UnsafeUtility.CopyPtrToStructure(ptr, out T data);

                return data;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                CheckWriteAndThrow(this);
#endif
                var ecs = this.access->EntityComponentStore;
                ecs->AssertEntityHasComponent(entity, this.typeIndex, ref this.cache);

                if (this.isZeroSized != 0)
                {
                    return;
                }

                void* ptr = ecs->GetComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);
                UnsafeUtility.CopyStructureToPtr(ref value, ptr);
            }
        }

        public T this[SystemHandle system]
        {
            get => this[system.m_Entity];
            set => this[system.m_Entity] = value;
        }

        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

        public bool HasComponent(Entity entity)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(entity, this.typeIndex, ref this.cache, out _);
        }

        public bool HasComponent(SystemHandle system)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(system.m_Entity, this.typeIndex, ref this.cache, out _);
        }

        public bool TryGetComponent(Entity entity, out T componentData)
        {
            var ecs = this.access->EntityComponentStore;

            if (this.isZeroSized != 0)
            {
                componentData = default;
                return ecs->HasComponent(entity, this.typeIndex, ref this.cache, out _);
            }

            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                componentData = default;
                return false;
            }

            void* ptr = ecs->GetOptionalComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache);
            if (ptr == null)
            {
                componentData = default;
                return false;
            }

            UnsafeUtility.CopyPtrToStructure(ptr, out componentData);
            return true;
        }

        /// <summary>
        /// Tracks possible writes to the whole chunk, including declared writes that leave values unchanged.
        /// </summary>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = this.access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            var typeIndexInArchetype = this.cache.IndexInArchetype;
            if (typeIndexInArchetype == -1)
            {
                return false;
            }

            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        public bool IsComponentEnabled(Entity entity)
        {
            return this.access->IsComponentEnabled(entity, this.typeIndex, ref this.cache);
        }

        public bool IsComponentEnabled(SystemHandle systemHandle)
        {
            return this.access->IsComponentEnabled(systemHandle.m_Entity, this.typeIndex, ref this.cache);
        }

        public void SetComponentEnabled(SystemHandle systemHandle, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckWriteAndThrow(this);
#endif
            this.access->SetComponentEnabled(systemHandle.m_Entity, this.typeIndex, value, ref this.cache);
        }

        public void SetComponentEnabled(Entity entity, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckWriteAndThrow(this);
#endif
            this.access->SetComponentEnabled(entity, this.typeIndex, value, ref this.cache);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckWriteAndThrow(in UnsafeComponentLookup<T> componentLookup)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentLookup.isReadOnly != 0)
            {
                throw new InvalidOperationException("Writing when read only");
            }
#endif
        }
    }
}
