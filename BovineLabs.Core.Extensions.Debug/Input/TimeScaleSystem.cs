// <copyright file="TimeScaleSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    internal partial struct TimeScaleSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            const float multi = 8f;

            SystemAPI.TryGetSingleton<InputCoreDebug>(out var inputDebug);
            if (inputDebug.TimeScaleDouble.Down)
            {
                UnityEngine.Time.timeScale = math.clamp(math.ceilpow2((int)(UnityEngine.Time.timeScale * multi * 2)) / multi, 1 / multi, 100);
            }
            else if (inputDebug.TimeScaleHalve.Up)
            {
                UnityEngine.Time.timeScale = math.max(math.ceilpow2((int)(UnityEngine.Time.timeScale * multi / 2)) / multi, 0);
            }
        }
    }
}
#endif
