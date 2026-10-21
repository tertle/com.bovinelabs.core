namespace BovineLabs.Core.Internal
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;

    public static class EntityInternal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsAllocated(Entity entity)
        {
            return EntityComponentStore.s_entityStore.Data.Exists(entity);
        }
    }
}
