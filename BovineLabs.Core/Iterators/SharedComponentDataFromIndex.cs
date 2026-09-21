namespace BovineLabs.Core.Iterators
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [NativeContainer]
    public unsafe struct SharedComponentDataFromIndex<T>
        where T : struct, ISharedComponentData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;
        private readonly byte _isReadOnly;
#endif
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* _access;

        private readonly TypeIndex _typeIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal SharedComponentDataFromIndex(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            var safetyHandles = &access->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(typeIndex, isReadOnly);
            _isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            _access = access;
            _typeIndex = typeIndex;
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
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return _access->GetSharedComponentData<T>(index);
            }
        }

        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &_access->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(_typeIndex, _isReadOnly != 0);
#endif
        }
    }
}
