// <copyright file="UnsafeComponentLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A container that provides access to all instances of components of type T, indexed by <see cref="Entity" />. </summary>
    /// <typeparam name="T"> The type of <see cref="IComponentData" /> to access. </typeparam>
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

        /// <summary>
        /// Gets the <see cref="IComponentData" /> instance of type T for the specified entity.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns> An <see cref="IComponentData" /> type. </returns>
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

        /// <summary>
        /// Gets the <see cref="IComponentData" /> instance of type T for the specified system's associated entity.
        /// </summary>
        /// <param name="system"> The system handle. </param>
        /// <returns> An <see cref="IComponentData" /> type. </returns>
        public T this[SystemHandle system]
        {
            get => this[system.m_Entity];
            set => this[system.m_Entity] = value;
        }

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system"> The system on which this type handle is cached. </param>
        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="systemState"> The SystemState of the system on which this type handle is cached. </param>
        public void Update(ref SystemState systemState)
        {
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity" /> instance still refers to a valid entity and that it has a
        /// component of type T.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns>
        /// True if the entity has a component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.
        /// </returns>
        public bool HasComponent(Entity entity)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(entity, this.typeIndex, ref this.cache, out _);
        }

        /// <summary>
        /// Reports whether the specified <see cref="SystemHandle" /> associated <see cref="Entity" /> is valid and contains a
        /// component of type T.
        /// </summary>
        /// <param name="system"> The system handle. </param>
        /// <returns>
        /// True if the entity associated with the system has a component of type T, and false if it does not. Also returns false if
        /// the system handle refers to a system that has been destroyed.
        /// </returns>
        public bool HasComponent(SystemHandle system)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(system.m_Entity, this.typeIndex, ref this.cache, out _);
        }

        /// <summary>
        /// Retrieves the component associated with the specified <see cref="Entity" />, if it exists. Then reports if the instance still refers to a valid entity and that
        /// it has a
        /// component of type T.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// ///
        /// <param name="componentData"> The component of type T for the given entity, if it exists. </param>
        /// <returns> True if the entity has a component of type T, and false if it does not. </returns>
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
        /// Reports whether any of IComponentData components of the type T, in the chunk containing the
        /// specified <see cref="Entity" />, could have changed.
        /// </summary>
        /// <remarks>
        /// Note that for efficiency, the change version applies to whole chunks not individual entities. The change
        /// version is incremented even when another job or system that has declared write access to a component does
        /// not actually change the component value.
        /// </remarks>
        /// <param name="entity"> The entity. </param>
        /// <param name="version">
        /// The version to compare. In a system, this parameter should be set to the
        /// current <see cref="Unity.Entities.ComponentSystemBase.LastSystemVersion" /> at the time the job is run or
        /// scheduled.
        /// </param>
        /// <returns>
        /// True, if the version number stored in the chunk for this component is more recent than the version
        /// passed to the <paramref name="version" /> parameter.
        /// </returns>
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

        /// <summary>
        /// Checks whether the <see cref="IComponentData" /> of type T is enabled on the specified <see cref="Entity" />.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="Entity" /> does not exist. </exception>
        /// <param name="entity"> The entity whose component should be checked. </param>
        /// <returns> True if the specified component is enabled, or false if it is disabled. </returns>
        /// <seealso cref="SetComponentEnabled(Entity, bool)" />
        public bool IsComponentEnabled(Entity entity)
        {
            return this.access->IsComponentEnabled(entity, this.typeIndex, ref this.cache);
        }

        /// <summary>
        /// Checks whether the <see cref="IComponentData" /> of type T is enabled on the specified system using a <see cref="SystemHandle" />.
        /// For the purposes of EntityQuery matching, a system with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="SystemHandle" /> does not exist. </exception>
        /// <param name="systemHandle"> The system whose component should be checked. </param>
        /// <returns> True if the specified component is enabled, or false if it is disabled. </returns>
        /// <seealso cref="SetComponentEnabled(SystemHandle, bool)" />
        public bool IsComponentEnabled(SystemHandle systemHandle)
        {
            return this.access->IsComponentEnabled(systemHandle.m_Entity, this.typeIndex, ref this.cache);
        }

        /// <summary>
        /// Enable or disable the <see cref="IComponentData" /> of type T on the specified system using a <see cref="SystemHandle" />. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, a system with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="SystemHandle" /> does not exist. </exception>
        /// <param name="systemHandle"> The system whose component should be enabled or disabled. </param>
        /// <param name="value"> True if the specified component should be enabled, or false if it should be disabled. </param>
        /// <seealso cref="IsComponentEnabled(SystemHandle)" />
        public void SetComponentEnabled(SystemHandle systemHandle, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckWriteAndThrow(this);
#endif
            this.access->SetComponentEnabled(systemHandle.m_Entity, this.typeIndex, value, ref this.cache);
        }

        /// <summary>
        /// Enable or disable the <see cref="IComponentData" /> of type T on the specified <see cref="Entity" />. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="Entity" /> does not exist. </exception>
        /// <param name="entity"> The entity whose component should be enabled or disabled. </param>
        /// <param name="value"> True if the specified component should be enabled, or false if it should be disabled. </param>
        /// <seealso cref="IsComponentEnabled(Entity)" />
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
