namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class WorldUnmanagedInternal
    {
        public static readonly ComponentType SystemInstanceComponentType = ComponentType.ReadWrite<SystemInstance>();

        public static ref WorldFlags FlagsRW(this WorldUnmanaged world)
        {
            return ref world.GetImpl().Flags;
        }
    }
}
