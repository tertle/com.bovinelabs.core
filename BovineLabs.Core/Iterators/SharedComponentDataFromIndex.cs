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
        private readonly AtomicSafetyHandle m_Safety;
#endif
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* m_Access;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal SharedComponentDataFromIndex(EntityDataAccess* access, AtomicSafetyHandle safety)
        {
            m_Safety = safety;
            m_Access = access;
        }

#else
        internal SharedComponentDataFromIndex(EntityDataAccess* access)
        {
            m_Access = access;
        }
#endif

        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Access->GetSharedComponentData<T>(index);
            }
        }
    }
}
