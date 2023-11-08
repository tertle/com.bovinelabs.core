// <copyright file="VirtualBufferTypeHandle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A handle to a specific buffer component type, used to access an <see cref="ArchetypeChunk"/>'s component data in a job.
    /// </summary>
    /// <remarks>
    /// Passing a type handle to a job automatically registers the job as a reader or writer of that type, which allows
    /// the DOTS safety system to detect potential race conditions between concurrent jobs which access the same component type.
    ///
    /// To create a BufferTypeHandle, use <see cref="ComponentSystemBase.GetBufferTypeHandle{T}"/>. While type handles
    /// can be created just in time before they're used, it is more efficient to create them once during system creation,
    /// cache them in a private field on the system, and incrementally update them with
    /// <see cref="UnityEngine.PlayerLoop.Update"/> just before use.
    ///
    /// If the component type is not known at compile time, use <seealso cref="DynamicComponentTypeHandle"/>.
    /// </remarks>
    /// <typeparam name="T">The buffer element type</typeparam>
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    public struct VirtualBufferTypeHandle<T>
        where T : unmanaged, IBufferElementData
    {
        internal readonly TypeIndex TypeIndex;
        internal readonly TypeIndex ChunkLinksTypeIndex;
        internal readonly byte GroupIndex;

        internal LookupCache LookupCache;
        internal LookupCache ChunkLinksLookupCache;

        private readonly byte isReadOnly;
        private uint globalSystemVersion;

        /// <summary>The global system version for which this handle is valid.</summary>
        /// <remarks>Attempting to use this type handle with a different
        /// system version indicates that the handle is no longer valid; use the <see cref="Update(Unity.Entities.SystemBase)"/>
        /// method to incrementally update the version just before use.
        /// </remarks>
        public uint GlobalSystemVersion => this.globalSystemVersion;

        /// <summary>
        /// Reports whether this type handle was created in read-only mode.
        /// </summary>
        public bool IsReadOnly => this.isReadOnly == 1;

#pragma warning disable 0414
        private readonly int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly int m_MinIndex;
        private readonly int m_MaxIndex;

        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_Safety1;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;
#endif
#pragma warning restore 0414

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal VirtualBufferTypeHandle(AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly, uint globalSystemVersion)
#else
        internal VirtualBufferTypeHandle(bool isReadOnly, uint globalSystemVersion)
#endif
        {
            this.m_Length = 1;
            this.TypeIndex = TypeManager.GetTypeIndex<T>();
            this.globalSystemVersion = globalSystemVersion;
            this.isReadOnly = isReadOnly ? (byte) 1u : (byte) 0u;

            this.ChunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.GroupIndex = TypeMangerEx.GetGroupIndex<T>();

            this.LookupCache = default;
            this.ChunkLinksLookupCache = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_MinIndex = 0;
            this.m_MaxIndex = 0;
            this.m_Safety0 = safety;
            this.m_Safety1 = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
#endif
        }

        /// <summary>
        /// When a BufferTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system">The system on which this type handle is cached.</param>
        public unsafe void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a BufferTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="state">The SystemState of the system on which this type handle is cached.</param>
        public unsafe void Update(ref SystemState state)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety0 = state.m_DependencyManager->Safety.GetSafetyHandleForBufferTypeHandle(this.TypeIndex, this.IsReadOnly);
            this.m_Safety1 = state.m_DependencyManager->Safety.GetBufferHandleForBufferTypeHandle(this.TypeIndex);
#endif
            this.globalSystemVersion = state.m_EntityComponentStore->GlobalSystemVersion;
        }

        /// <summary>
        /// Returns the formatted FixedString "BufferTypeHandle[type_name_here]".
        /// </summary>
        /// <returns>Returns the formatted FixedString "VirtualBufferTypeHandle[type_name_here]".</returns>
        public FixedString512Bytes ToFixedString()
        {
            var fs = new FixedString128Bytes((FixedString32Bytes)"VirtualBufferTypeHandle[");
            fs.Append(TypeManager.GetTypeNameFixed(this.TypeIndex));
            fs.Append(']');
            return fs;
        }
    }
}
#endif
