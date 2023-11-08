// <copyright file="VirtualEntityTypeHandle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A handle to the <see cref="Entity" /> component type, used to access an <see cref="ArchetypeChunk" />'s entities in a
    /// job.
    /// </summary>
    /// <remarks>
    /// Passing a type handle to a job automatically registers the job as a reader or writer of that type, which allows
    /// the DOTS safety system to detect potential race conditions between concurrent jobs which access the same component
    /// type.
    /// To create a EntityTypeHandle, use <see cref="ComponentSystemBase.GetEntityTypeHandle" />. While type handles
    /// can be created just in time before they're used, it is more efficient to create them once during system creation,
    /// cache them in a private field on the system, and incrementally update them with
    /// <see cref="UnityEngine.PlayerLoop.Update" /> just before use.
    /// </remarks>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct VirtualEntityTypeHandle
    {
        internal readonly TypeIndex ChunkLinksTypeIndex;
        internal LookupCache ChunkLinksLookupCache;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal VirtualEntityTypeHandle(AtomicSafetyHandle safety)
#else
        internal VirtualEntityTypeHandle(bool unused)
#endif
        {
            this.ChunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.ChunkLinksLookupCache = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = safety;
#endif
        }

        /// <summary>
        /// When a EntityTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system"> The system on which this type handle is cached. </param>
        public unsafe void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a EntityTypeHandle is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="state"> The SystemState of the system on which this type handle is cached. </param>
        public unsafe void Update(ref SystemState state)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = state.m_DependencyManager->Safety.GetSafetyHandleForEntityTypeHandle();
#endif
        }

        /// <summary>
        /// Returns "EntityTypeHandle".
        /// </summary>
        /// <returns> Returns "VirtualEntityTypeHandle". </returns>
        public FixedString128Bytes ToFixedString()
        {
            return "VirtualEntityTypeHandle";
        }
    }
}
#endif
