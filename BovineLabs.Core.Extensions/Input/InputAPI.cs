// <copyright file="InputAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;

    public static class InputAPI
    {
        /// <summary> Enables a specific input state without disabling other states. </summary>
        /// <remarks> Not burst compilable. </remarks>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="input"> The input state to enable. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InputEnable(ref SystemState systemState, FixedString32Bytes input)
        {
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"Input enabled {input}");
            InputEnable(ref systemState, input, true);
        }

        /// <summary> Disables a specific input state. </summary>
        /// <remarks> Not burst compilable. </remarks>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="input"> The input state to disable. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InputDisable(ref SystemState systemState, FixedString32Bytes input)
        {
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"Input disabled {input}");
            InputEnable(ref systemState, input, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InputEnable(ref SystemState systemState, FixedString32Bytes input, bool enabled)
        {
            systemState
            .EntityManager
            .GetSingletonBuffer<InputActionMapEnable>()
            .Add(new InputActionMapEnable
            {
                Input = input,
                Enable = enabled,
            });
        }
    }
}
#endif
