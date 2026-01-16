// <copyright file="WorldSafeShutdown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;
#if UNITY_EDITOR
    using UnityEditor;
#else
    using UnityEngine;
#endif

    internal static class WorldSafeShutdown
    {
#if UNITY_EDITOR
        internal static void Initialize()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingPlayMode)
                {
                    OnQuit();
                }
            };
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Application.quitting += OnQuit;
        }
#endif

        private static void OnQuit()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < World.All.Count; index++)
            {
                var world = World.All[index];
                if (!world.IsCreated || (world.Flags & WorldFlags.Live) == 0)
                {
                    continue;
                }

                world.EntityManager.CompleteAllTrackedJobs();

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
