// <copyright file="TimeScaleSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input.Debug
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    internal partial struct TimeScaleSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            const float multi = 8f;

            SystemAPI.TryGetSingleton<InputCoreDebug>(out var inputDebug);
            if (inputDebug.TimeScaleDouble)
            {
                Time.timeScale = math.clamp(math.ceilpow2((int)(Time.timeScale * multi * 2)) / multi, 1 / multi, 100);
            }
            else if (inputDebug.TimeScaleHalve)
            {
                Time.timeScale = math.max(math.ceilpow2((int)((Time.timeScale * multi) / 2)) / multi, 0);
            }
        }
    }
}
