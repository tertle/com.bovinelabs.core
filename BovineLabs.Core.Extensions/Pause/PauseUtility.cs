// <copyright file="PauseUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using System;
    using System.Collections.Generic;
    using Unity.Entities;

    public static class PauseUtility
    {
        /// <summary>
        /// A list of systems, including system groups, that should still be updated. Only use this for 3rd party apps. Prefer the IUpdateWhilePaused interface.
        /// </summary>
        public static readonly HashSet<Type> UpdateWhilePaused = new();

        public static void UpdateAlwaysSystems(ComponentSystemGroup group)
        {
            foreach (var index in group.m_MasterUpdateList)
            {
                if (!index.IsManaged)
                {
                    var unmanagedSystem = group.UnmanagedSystems[index.Index];
                    CheckIfUnmanagedSystemShouldUpdate(group.World.Unmanaged, unmanagedSystem);
                    continue;
                }

                var system = group.m_managedSystemsToUpdate[index.Index];

                if (!TryUpdateSystem(system))
                {
                    // If we didn't update and we're a system group
                    if (system is ComponentSystemGroup subGroup)
                    {
                        // Check if anything inside this group is marked
                        UpdateAlwaysSystems(subGroup);
                    }
                }
            }
        }

        private static void CheckIfUnmanagedSystemShouldUpdate(WorldUnmanaged world, SystemHandle system)
        {
            ref var state = ref world.ResolveSystemStateRef(system);
            var type = SystemBaseRegistry.GetStructType(state.UnmanagedMetaIndex);

            if (typeof(IUpdateWhilePaused).IsAssignableFrom(type) || UpdateWhilePaused.Contains(type))
            {
                system.Update(world);
            }
        }

        private static bool TryUpdateSystem(ComponentSystemBase system)
        {
            if (system is EntityCommandBufferSystem || system is IUpdateWhilePaused || UpdateWhilePaused.Contains(system.GetType()))
            {
                system.Update();
                return true;
            }

            return false;
        }
    }
}
#endif
