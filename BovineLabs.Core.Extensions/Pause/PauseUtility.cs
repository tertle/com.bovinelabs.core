// <copyright file="PauseUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    public static class PauseUtility
    {
        /// <summary>
        /// Gets a list of systems, including system groups, that should still be updated even though they're in a pause group.
        /// Only use this for 3rd party apps, prefer the <see cref="IUpdateWhilePaused"/> interfaces.
        /// </summary>
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "For 3rd party libraries")]
        public static HashSet<Type> UpdateWhilePaused { get; } = new();

        /// <summary>
        /// Gets a list of root systems, including system groups, that should be disabled when paused.
        /// Only use this for 3rd party systems, prefer the <see cref="IDisableWhilePaused"/> interfaces.
        /// </summary>
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "For 3rd party libraries")]
        public static HashSet<Type> DisableWhilePaused { get; } = new()
        {
            typeof(FixedStepSimulationSystemGroup),
            typeof(LateSimulationSystemGroup),
            typeof(VariableRateSimulationSystemGroup),
        };

        // The root update
        public static void UpdateAlwaysSystems(ComponentSystemGroup group)
        {
            foreach (var index in group.m_MasterUpdateList)
            {
                if (!index.IsManaged)
                {
                    var unmanagedSystem = group.UnmanagedSystems[index.Index];
                    CheckIfUnmanagedRootSystemShouldUpdate(group.World.Unmanaged, unmanagedSystem);
                    continue;
                }

                var system = group.m_managedSystemsToUpdate[index.Index];

                if (!TryUpdateRootSystem(system))
                {
                    // If we didn't update, and we're a system group
                    if (system is ComponentSystemGroup subGroup)
                    {
                        // Check if anything inside this group is marked
                        TryUpdateChildSystems(subGroup);
                    }
                }
            }
        }

        private static void TryUpdateChildSystems(ComponentSystemGroup group)
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
                        TryUpdateChildSystems(subGroup);
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

        private static void CheckIfUnmanagedRootSystemShouldUpdate(WorldUnmanaged world, SystemHandle system)
        {
            ref var state = ref world.ResolveSystemStateRef(system);
            var type = SystemBaseRegistry.GetStructType(state.UnmanagedMetaIndex);

            if (!typeof(IDisableWhilePaused).IsAssignableFrom(type) && !DisableWhilePaused.Contains(system.GetType()))
            {
                system.Update(world);
            }
        }

        private static bool TryUpdateSystem(ComponentSystemBase system)
        {
            if (system is IUpdateWhilePaused || UpdateWhilePaused.Contains(system.GetType()))
            {
                system.Update();
                return true;
            }

            return false;
        }

        private static bool TryUpdateRootSystem(ComponentSystemBase system)
        {
            if (system is not IDisableWhilePaused && !DisableWhilePaused.Contains(system.GetType()))
            {
                system.Update();
                return true;
            }

            return false;
        }
    }
}
#endif
