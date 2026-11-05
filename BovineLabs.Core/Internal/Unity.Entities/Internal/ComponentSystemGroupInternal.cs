namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class ComponentSystemGroupInternal
    {
        public static int GetUpdateSystemCount(this ComponentSystemGroup group)
        {
            return group.m_UpdateList.Length;
        }

        public static bool IsManagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.World.Unmanaged.GetSystemTypeIndex(group.m_UpdateList[index]).IsManaged;
        }

        public static ComponentSystemBase GetManagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.World.Unmanaged.ResolveSystemStateRef(group.m_UpdateList[index]).ManagedSystem;
        }

        public static SystemHandle GetUnmanagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.m_UpdateList[index];
        }
    }
}
