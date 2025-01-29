// <copyright file="UnsafeBufferLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A container that provides access to all instances of DynamicBuffer components with elements of type T,
    /// indexed by <see cref="Entity" />.
    /// </summary>
    /// <typeparam name="T"> The type of <see cref="IBufferElementData" /> to access. </typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeBufferLookup<T>
        where T : unmanaged, IBufferElementData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        private readonly TypeIndex typeIndex;
        private readonly byte isReadOnly;

        private LookupCache cache;
        private uint globalSystemVersion;
        private int internalCapacity;

        internal UnsafeBufferLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.internalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}" /> instance of type <typeparamref name="T" /> for the specified entity.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns> A <see cref="DynamicBuffer{T}" /> type. </returns>
        /// <remarks>
        /// Normally, you cannot write to buffers accessed using a BufferLookup instance
        /// in a parallel Job. This restriction is in place because multiple threads could write to the same buffer,
        /// leading to a race condition and nondeterministic results. However, when you are certain that your algorithm
        /// cannot write to the same buffer from different threads, you can manually disable this safety check
        /// by putting the [NativeDisableParallelForRestriction] attribute on the BufferLookup field in the Job.
        /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html.
        /// </remarks>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="entity" /> does not have a buffer
        /// component of type <typeparamref name="T" />.
        /// </exception>
        public UnsafeDynamicBuffer<T> this[Entity entity]
        {
            get
            {
                var ecs = this.access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                ecs->AssertEntityHasComponent(entity, this.typeIndex, ref this.cache);
#endif

                var header = this.isReadOnly != 0
                    ? (BufferHeader*)ecs->GetComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache)
                    : (BufferHeader*)ecs->GetComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

                return new UnsafeDynamicBuffer<T>(header, this.internalCapacity);
            }
        }

        /// <summary>
        /// Retrieves the buffer components associated with the specified <see cref="Entity" />, if it exists. Then reports if the instance still refers to a
        /// valid entity and that it has a buffer component of type T.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <param name="bufferData"> The buffer component of type T for the given entity, if it exists. </param>
        /// <returns> True if the entity has a buffer component of type T, and false if it does not. </returns>
        public bool TryGetBuffer(Entity entity, out UnsafeDynamicBuffer<T> bufferData)
        {
            var ecs = this.access->EntityComponentStore;
            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                bufferData = default;
                return false;
            }

            var header = this.isReadOnly != 0
                ? (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache)
                : (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

            if (header != null)
            {
                bufferData = new UnsafeDynamicBuffer<T>(header, this.internalCapacity);
                return true;
            }

            bufferData = default;
            return false;
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity" /> instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns>
        /// True if the entity has a buffer component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.
        /// </returns>
        public bool HasBuffer(Entity entity)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(entity, this.typeIndex, ref this.cache, out _);
        }

        /// <summary>
        /// Reports whether any of IBufferElementData components of the type T, in the chunk containing the
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
        /// Checks whether the <see cref="IBufferElementData" /> of type T is enabled on the specified <see cref="Entity" />.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="Entity" /> does not exist. </exception>
        /// <param name="entity"> The entity whose component should be checked. </param>
        /// <returns> True if the specified component is enabled, or false if it is disabled. </returns>
        /// <seealso cref="SetBufferEnabled" />
        public bool IsBufferEnabled(Entity entity)
        {
            return this.access->IsComponentEnabled(entity, this.typeIndex, ref this.cache);
        }

        /// <summary>
        /// Enable or disable the <see cref="IBufferElementData" /> of type T on the specified <see cref="Entity" />. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent" /> interface.
        /// </summary>
        /// <exception cref="ArgumentException"> The <see cref="Entity" /> does not exist. </exception>
        /// <param name="entity"> The entity whose component should be enabled or disabled. </param>
        /// <param name="value"> True if the specified component should be enabled, or false if it should be disabled. </param>
        /// <seealso cref="IsBufferEnabled" />
        public void SetBufferEnabled(Entity entity, bool value)
        {
            this.access->SetComponentEnabled(entity, this.typeIndex, value, ref this.cache);
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system"> The system on which this type handle is cached. </param>
        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="systemState"> The SystemState of the system on which this type handle is cached. </param>
        public void Update(ref SystemState systemState)
        {
            // NOTE: We could in theory fetch all this data from m_Access.EntityComponentStore and void the SystemState from being passed in.
            //       That would unfortunately allow this API to be called from a job. So we use the required system parameter as a way of signifying to the user that this can only be invoked from main thread system code.
            //       Additionally this makes the API symmetric to ComponentTypeHandle.
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }
    }
}
