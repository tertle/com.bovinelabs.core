// <copyright file="PauseUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using Unity.Entities;

    public static class PauseUtility
    {
        public static void UpdateAlwaysSystems(ComponentSystemGroup group)
        {
            foreach (var index in group.m_MasterUpdateList)
            {
                if (!index.IsManaged)
                {
                    continue;
                }

                var system = group.m_managedSystemsToUpdate[index.Index];
                if (system is not ComponentSystemGroup subGroup)
                {
                    continue;
                }

                if (subGroup is IUpdateWhilePaused)
                {
                    subGroup.Update();
                }
                else
                {
                    // Check if any groups inside this group are marked
                    UpdateAlwaysSystems(subGroup);
                }
            }
        }
    }
}
#endif
