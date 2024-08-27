// <copyright file="GameAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.States;
    using BovineLabs.Core.UI;
    using BovineLabs.Sample.Extension.Data;
    using Unity.Collections;
    using Unity.Entities;

    public static class GameAPI
    {
        /// <inheritdoc cref="AppAPI.StateCurrent{GameState, BitArray256}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray256 StateCurrent(ref SystemState systemState)
        {
            return AppAPI.StateCurrent<GameState, BitArray256>(ref systemState);
        }

        /// <inheritdoc cref="AppAPI.StateIsEnabled{GameState, BitArray256, GameStates}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StateIsEnabled(ref SystemState systemState, FixedString32Bytes name)
        {
            return AppAPI.StateIsEnabled<GameState, BitArray256, GameStates>(ref systemState, name);
        }

        /// <inheritdoc cref="AppAPI.StateSet{GameState, BitArray256, GameStates}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateSet(ref SystemState systemState, FixedString32Bytes name)
        {
            AppAPI.StateSet<GameState, BitArray256, GameStates>(ref systemState, name);
        }

        /// <inheritdoc cref="AppAPI.StateSet{GameState, BitArray256}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateSet(ref SystemState systemState, byte state)
        {
            AppAPI.StateSet<GameState, BitArray256>(ref systemState, state);
        }

        /// <inheritdoc cref="AppAPI.StateEnable{GameState, BitArray256, GameStates}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateEnable(ref SystemState systemState, FixedString32Bytes name)
        {
            AppAPI.StateEnable<GameState, BitArray256, GameStates>(ref systemState, name);
        }

        /// <inheritdoc cref="AppAPI.StateEnable{GameState, BitArray256}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateEnable(ref SystemState systemState, byte state)
        {
            AppAPI.StateEnable<GameState, BitArray256>(ref systemState, state);
        }

        /// <inheritdoc cref="AppAPI.StateDisable{GameState, BitArray256, GameStates}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateDisable(ref SystemState systemState, FixedString32Bytes name)
        {
            AppAPI.StateDisable<GameState, BitArray256, GameStates>(ref systemState, name);
        }

        /// <inheritdoc cref="AppAPI.StateDisable{GameState, BitArray256}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StateDisable(ref SystemState systemState, byte state)
        {
            AppAPI.StateDisable<GameState, BitArray256>(ref systemState, state);
        }

        /// <inheritdoc cref="UIAPI.UICurrent"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray256 UICurrent(ref SystemState systemState)
        {
            return UIAPI.UICurrent(ref systemState);
        }

        /// <inheritdoc cref="UIAPI.UIIsEnabled"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UIIsEnabled(ref SystemState systemState, FixedString32Bytes name)
        {
            return UIAPI.UIIsEnabled(ref systemState, name);
        }

        /// <inheritdoc cref="UIAPI.UISet"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UISet(ref SystemState systemState, FixedString32Bytes name)
        {
            UIAPI.UISet(ref systemState, name);
        }

        /// <inheritdoc cref="UIAPI.UIEnable"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UIEnable(ref SystemState systemState, FixedString32Bytes name)
        {
            UIAPI.UIEnable(ref systemState, name);
        }

        /// <inheritdoc cref="UIAPI.UIDisable"/>
        public static void UIDisable(ref SystemState systemState, FixedString32Bytes name)
        {
            UIAPI.UIDisable(ref systemState, name);
        }

        /// <inheritdoc cref="UIAPI.UIPop"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UIPop(ref SystemState systemState)
        {
            UIAPI.UIPop(ref systemState);
        }

        /// <inheritdoc cref="UIAPI.UICloseAllPopups"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UICloseAllPopups(ref SystemState systemState)
        {
            UIAPI.UICloseAllPopups(ref systemState);
        }

        /// <inheritdoc cref="UIAPI.UICloseAllPopupsOrPop"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UICloseAllPopupsOrPop(ref SystemState systemState)
        {
            UIAPI.UICloseAllPopupsOrPop(ref systemState);
        }

        /// <inheritdoc cref="UIAPI.UIHideAll"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UIHideAll(ref SystemState systemState)
        {
            UIAPI.UIHideAll(ref systemState);
        }
    }
}
