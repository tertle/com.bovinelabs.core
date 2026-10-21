namespace BovineLabs.Core.Iterators
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeEnableableLookup
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* _access;

        internal UnsafeEnableableLookup(EntityDataAccess* access)
        {
            _access = access;
        }

        public bool HasComponent(Entity entity, ComponentType componentType)
        {
            return _access->HasComponent(entity, componentType);
        }

        public bool IsComponentEnabled(Entity entity, ComponentType componentType)
        {
            return _access->IsComponentEnabled(entity, componentType.TypeIndex);
        }

        public void SetComponentEnabled(Entity entity, ComponentType componentType, bool value)
        {
            _access->SetComponentEnabled(entity, componentType.TypeIndex, value);
        }
    }
}
