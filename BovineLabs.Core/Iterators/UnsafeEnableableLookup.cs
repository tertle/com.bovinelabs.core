namespace BovineLabs.Core.Iterators
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeEnableableLookup
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        internal UnsafeEnableableLookup(EntityDataAccess* access)
        {
            this.access = access;
        }

        public bool HasComponent(Entity entity, ComponentType componentType)
        {
            return this.access->HasComponent(entity, componentType);
        }

        public bool IsComponentEnabled(Entity entity, ComponentType componentType)
        {
            return this.access->IsComponentEnabled(entity, componentType.TypeIndex);
        }

        public void SetComponentEnabled(Entity entity, ComponentType componentType, bool value)
        {
            this.access->SetComponentEnabled(entity, componentType.TypeIndex, value);
        }
    }
}
