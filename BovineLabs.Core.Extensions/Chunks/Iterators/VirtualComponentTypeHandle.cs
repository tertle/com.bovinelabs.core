// <copyright file="VirtualComponentTypeHandle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A handle to a specific component type, used to access an <see cref="ArchetypeChunk"/>'s component data in a job.
    /// </summary>
    /// <remarks>
    /// Passing a type handle to a job automatically registers the job as a reader
    /// or writer of that type, which allows the DOTS safety system to detect potential race conditions between concurrent
    /// jobs which access the same component type.
    ///
    /// To create a ComponentTypeHandle, use <see cref="ComponentSystemBase.GetComponentTypeHandle"/>. While type handles
    /// can be created just in time before they're used, it is more efficient to create them once during system creation,
    /// cache them in a private field on the system, and incrementally update them with
    /// <see cref="ComponentTypeHandle{T}.Update"/> just before use.
    ///
    /// If the component type is not known at compile time, use <seealso cref="DynamicComponentTypeHandle"/>.
    /// </remarks>
    /// <typeparam name="T">The component type</typeparam>
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    public struct VirtualComponentTypeHandle<T>
    {
        internal readonly TypeIndex TypeIndex;
        internal readonly int SizeInChunk;
        internal readonly TypeIndex ChunkLinksTypeIndex;
        internal readonly byte GroupIndex;

        internal LookupCache LookupCache;
        internal LookupCache ChunkLinksLookupCache;

        private readonly byte isReadOnly;
        private readonly byte isZeroSized;
        private uint globalSystemVersion;

        /// <summary>The global system version for which this handle is valid.</summary>
        /// <remarks>Attempting to use this type handle with a different
        /// system version indicates that the handle is no longer valid; use the <see cref="Update(Unity.Entities.SystemBase)"/>
        /// method to incrementally update the version just before use.
        /// </remarks>
        public readonly uint GlobalSystemVersion => this.globalSystemVersion;

        /// <summary>
        /// Reports whether this type handle was created in read-only mode.
        /// </summary>
        public readonly bool IsReadOnly => this.isReadOnly == 1;

        /// <summary>
        /// Reports whether this type will consume chunk space when used in an archetype.
        /// </summary>
        internal readonly bool IsZeroSized => this.isZeroSized == 1;

#pragma warning disable 0414
        private readonly int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;
        public AtomicSafetyHandle m_Safety;
#endif
#pragma warning restore 0414

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal VirtualComponentTypeHandle(AtomicSafetyHandle safety, bool isReadOnly, uint globalSystemVersion)
#else
        internal VirtualComponentTypeHandle(bool isReadOnly, uint globalSystemVersion)
#endif
        {
            this.TypeIndex = TypeManager.GetTypeIndex<T>();
            var typeInfo = TypeManager.GetTypeInfo(this.TypeIndex);

            this.ChunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.GroupIndex = TypeMangerEx.GetGroupIndex<T>();

            this.m_Length = 1;
            this.SizeInChunk = typeInfo.SizeInChunk;
            this.isZeroSized = typeInfo.IsZeroSized ? (byte)1u : (byte)0u;
            this.globalSystemVersion = globalSystemVersion;
            this.isReadOnly = isReadOnly ? (byte)1u : (byte)0u;
            this.LookupCache = default;
            this.ChunkLinksLookupCache = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_MinIndex = 0;
            this.m_MaxIndex = 0;
            this.m_Safety = safety;
#endif
        }

        /// <summary>
        /// When a ComponentTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="state">The SystemState of the system on which this type handle is cached.</param>
        public unsafe void Update(ref SystemState state)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = state.m_DependencyManager->Safety.GetSafetyHandleForComponentTypeHandle(this.TypeIndex, IsReadOnly);
#endif
            this.globalSystemVersion = state.GlobalSystemVersion;
        }

        /// <summary>
        /// When a ComponentTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system">The system on which this type handle is cached.</param>
        public unsafe void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// Returns the formatted FixedString "ComponentTypeHandle[type_name_here]".
        /// </summary>
        /// <returns>Returns the formatted FixedString "VirtualComponentTypeHandle[type_name_here]".</returns>
        public FixedString128Bytes ToFixedString()
        {
            var fs = new FixedString128Bytes((FixedString32Bytes)"VirtualComponentTypeHandle[");
            fs.Append(TypeManager.GetTypeNameFixed(this.TypeIndex));
            fs.Append(']');
            return fs;
        }
    }
}
#endif
