// <copyright file="SharedComponentDataFromIndex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [NativeContainer]
    public unsafe struct SharedComponentDataFromIndex<T>
        where T : struct, ISharedComponentData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private readonly byte m_IsReadOnly;
#endif
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* m_Access;

        private readonly TypeIndex m_TypeIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal SharedComponentDataFromIndex(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            var safetyHandles = &access->DependencyManager->Safety;
            this.m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(typeIndex, isReadOnly);
            this.m_IsReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.m_Access = access;
            this.m_TypeIndex = typeIndex;
        }

#else
        internal SharedComponentDataFromIndex(TypeIndex typeIndex, EntityDataAccess* access)
        {
            this.m_Access = access;
            this.m_TypeIndex = typeIndex;
        }
#endif

        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                return this.m_Access->GetSharedComponentData<T>(index);
            }
        }

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's Update() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system"> The system on which this type handle is cached. </param>
        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's Update() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="systemState"> The SystemState of the system on which this type handle is cached. </param>
        public void Update(ref SystemState systemState)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &this.m_Access->DependencyManager->Safety;
            this.m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(this.m_TypeIndex, this.m_IsReadOnly != 0);
#endif
        }
    }
}
