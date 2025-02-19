// <copyright file="PauseGame.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Component that pauses the world by disabling system updates. </summary>
    public struct PauseGame : IComponentData
    {
        /// <summary>
        /// If true, all systems will be disabled unless tagged with <see cref="IUpdateWhilePaused"/> or in <see cref="PauseUtility.UpdateWhilePaused"/>.
        /// If false, all root systems will update unless tagged with <see cref="IDisableWhilePaused"/> or in <see cref="PauseUtility.DisableWhilePaused"/>.
        /// Generally this is marked as true only during the initial world creation (waiting on subscenes etc.) and thereafter is left as false.
        /// </summary>
        public bool PauseAll;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPaused(ref SystemState systemState)
        {
            return systemState.EntityManager.HasComponent<PauseGame>(systemState.SystemHandle);
        }

        /// <summary> Pause the world by disabling system updates. </summary>
        /// <param name="systemState"> The system state. </param>
        /// <param name="pauseAll"><see cref="PauseAll"/>.</param>
        public static void Pause(ref SystemState systemState, bool pauseAll = false)
        {
            var isPaused = IsPaused(ref systemState);

            // We still set component even if we were already paused as the presentation part might change
            systemState.EntityManager.AddComponentData(systemState.SystemHandle, new PauseGame { PauseAll = pauseAll });

            // We were already paused, we don't bother logging
            if (isPaused)
            {
                return;
            }

            var text = default(FixedString512Bytes);
            text.Append(TypeManagerEx.GetSystemName(systemState.m_SystemTypeIndex));
            text.Append((FixedString32Bytes)" | Paused: true");
            systemState.EntityManager.GetSingleton<BLDebug>(false).DebugLong512(text);
        }

        /// <summary> Unpause the world. </summary>
        /// <param name="systemState"> The system state. </param>
        public static void Unpause(ref SystemState systemState)
        {
            // Not paused
            if (!IsPaused(ref systemState))
            {
                return;
            }

            systemState.EntityManager.RemoveComponent<PauseGame>(systemState.SystemHandle);

            var text = default(FixedString512Bytes);
            text.Append(TypeManagerEx.GetSystemName(systemState.m_SystemTypeIndex));
            text.Append((FixedString32Bytes)" | Paused: false");
            systemState.EntityManager.GetSingleton<BLDebug>(false).DebugLong512(text);
        }
    }
}
#endif
