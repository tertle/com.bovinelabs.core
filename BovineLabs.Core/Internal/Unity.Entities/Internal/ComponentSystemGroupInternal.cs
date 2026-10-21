namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class ComponentSystemGroupInternal
    {
        public static int GetUpdateSystemCount(this ComponentSystemGroup group)
        {
            return group.m_MasterUpdateList.Length;
        }

        public static bool IsManagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.m_MasterUpdateList[index].IsManaged;
        }

        public static ComponentSystemBase GetManagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.m_managedSystemsToUpdate[group.m_MasterUpdateList[index].Index];
        }

        public static SystemHandle GetUnmanagedUpdateSystem(this ComponentSystemGroup group, int index)
        {
            return group.UnmanagedSystems[group.m_MasterUpdateList[index].Index];
        }
    }
}
