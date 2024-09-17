// <copyright file="PauseGame.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Entities;

    public struct PauseGame : IComponentData
    {
        public bool PausePresentation;

        public static void Pause(ref SystemState systemState, bool pausePresentation = false)
        {
            var isPaused = systemState.EntityManager.HasComponent<PauseGame>(systemState.SystemHandle);

            // We still set component even if we were already paused as the presentation part might change
            systemState.EntityManager.AddComponentData(systemState.SystemHandle, new PauseGame { PausePresentation = pausePresentation });

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

        public static void Unpause(ref SystemState systemState)
        {
            // Not paused
            if (!systemState.EntityManager.HasComponent<PauseGame>(systemState.SystemHandle))
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
