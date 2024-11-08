// <copyright file="WorldSafeShutdown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;
    using UnityEngine;

    public static class WorldSafeShutdown
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            Application.quitting -= OnQuit;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < World.All.Count; index++)
            {
                var world = World.All[index];
                if (!world.IsCreated || (world.Flags & WorldFlags.Live) == 0)
                {
                    continue;
                }

                // world.EntityManager.CompleteAllTrackedJobs(); // TODO this would be safer but hides potential issues
                TryDisableUpdateSystemGroup<InitializationSystemGroup>(world);
                TryDisableUpdateSystemGroup<SimulationSystemGroup>(world);
                TryDisableUpdateSystemGroup<PresentationSystemGroup>(world);
            }
        }

        private static void TryDisableUpdateSystemGroup<T>(World world)
            where T : ComponentSystemBase
        {
            var system = world.GetExistingSystemManaged<T>();
            if (system != null)
            {
                system.Enabled = false;
                system.Update();
            }
        }
    }
}
