// <copyright file="AppAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define DEBUG_LOG
#endif

namespace BovineLabs.Core.States
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> Main thread state API for managing game state, UI and input. </summary>
    public static class AppAPI
    {
        /// <summary> Gets the current state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        /// <returns> The current state. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TA StateCurrent<T, TA>(ref SystemState systemState)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
            return systemState.EntityManager.GetSingleton<T>().Value;
        }

        /// <summary> Checks if a state is currently enabled. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The client state to check. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        /// <typeparam name="TS"> The state type. </typeparam>
        /// <returns> True if the state is enabled. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StateIsEnabled<T, TA, TS>(ref SystemState systemState, FixedString32Bytes name)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
            where TS : KSettings
        {
            var state = (byte)K<TS>.NameToKey(name);
            return systemState.EntityManager.GetSingleton<T>().Value[state];
        }

        /// <summary> Disables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The client state to set. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        /// <typeparam name="TS"> The state type. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateSet<T, TA, TS>(ref SystemState systemState, FixedString32Bytes name)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
            where TS : KSettings
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} set to {name}");
#endif

            var state = (byte)K<TS>.NameToKey(name);
            systemState.EntityManager.SetSingleton(new T { Value = new TA { [state] = true } });
        }

        /// <summary> Enables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="state"> The client state to set. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateSet<T, TA>(ref SystemState systemState, byte state)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} set to {state}");
#endif

            systemState.EntityManager.SetSingleton(new T { Value = new TA { [state] = true } });
        }

        /// <summary> Enables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The client state to disable. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        /// <typeparam name="TS"> The state type. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateEnable<T, TA, TS>(ref SystemState systemState, FixedString32Bytes name)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
            where TS : KSettings
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} enabled {name}");
#endif

            var state = (byte)K<TS>.NameToKey(name);
            StateEnable<T, TA>(ref systemState, state, true);
        }

        /// <summary> Enables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="state"> The client state to disable. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateEnable<T, TA>(ref SystemState systemState, byte state)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} enable {state}");
#endif
            StateEnable<T, TA>(ref systemState, state, true);
        }

        /// <summary> Disables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The client state to disable. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        /// <typeparam name="TS"> The state type. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateDisable<T, TA, TS>(ref SystemState systemState, FixedString32Bytes name)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
            where TS : KSettings
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} disabled {name}");
#endif

            var state = (byte)K<TS>.NameToKey(name);
            StateEnable<T, TA>(ref systemState, state, false);
        }

        /// <summary> Disables a specific client state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="state"> The client state to disable. </param>
        /// <typeparam name="T"> The state. </typeparam>
        /// <typeparam name="TA"> The bit array size. </typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateDisable<T, TA>(ref SystemState systemState, byte state)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
#if DEBUG_LOG
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"{GetName<T, TA>()} disable {state}");
#endif

            StateEnable<T, TA>(ref systemState, state, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void StateEnable<T, TA>(ref SystemState systemState, byte state, bool enabled)
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
            var gameState = systemState.EntityManager.GetSingletonRW<T>();

            ref var r = ref UnsafeUtility.As<T, BitArray256>(ref gameState.ValueRW);
            r[state] = enabled;
        }

        private static FixedString128Bytes GetName<T, TA>()
            where T : unmanaged, IState<TA>
            where TA : unmanaged, IBitArray<TA>
        {
            return TypeManagerEx.GetTypeName(TypeManager.GetTypeIndex<T>());
        }
    }
}
