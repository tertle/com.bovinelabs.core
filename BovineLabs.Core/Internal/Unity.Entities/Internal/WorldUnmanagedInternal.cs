namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Entities;

    public static class WorldUnmanagedInternal
    {
        public static readonly ComponentType SystemInstanceComponentType = ComponentType.ReadWrite<SystemInstance>();

        public static unsafe SystemState* GetSystemState(this WorldUnmanaged world, SystemHandle system)
        {
            return world.ResolveSystemStateChecked(system);
        }

        public static Type GetSystemType(this WorldUnmanaged world, SystemHandle system)
        {
            return world.GetTypeOfSystem(system);
        }

        public static ref WorldFlags FlagsRW(this WorldUnmanaged world)
        {
            return ref world.GetImpl().Flags;
        }
    }
}
