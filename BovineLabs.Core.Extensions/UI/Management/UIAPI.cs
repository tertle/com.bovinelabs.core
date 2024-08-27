// <copyright file="UIAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Keys;
    using Unity.Collections;
    using Unity.Entities;

    public static class UIAPI
    {
        /// <summary> Gets the current UI state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <returns> The current UI state. </returns>
        public static BitArray256 UICurrent(ref SystemState systemState)
        {
            return systemState.EntityManager.GetSingleton<UIState>().Value;
        }

        /// <summary> Checks if a UI state is currently enabled. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// /// <param name="name"> The UI state to check. </param>
        /// <returns> True if the state is enabled. </returns>
        public static bool UIIsEnabled(ref SystemState systemState, FixedString32Bytes name)
        {
            var state = (byte)K<UIStates>.NameToKey(name);
            return systemState.EntityManager.GetSingleton<UIState>().Value[state];
        }

        /// <summary> Sets the state of the UI exclusively to <see cref="name" />. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The UI state to switch to. </param>
        public static void UISet(ref SystemState systemState, FixedString32Bytes name)
        {
            var state = (byte)K<UIStates>.NameToKey(name);
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"UIState set to {name}");
            systemState.EntityManager.SetSingleton(new UIState { Value = new BitArray256 { [state] = true } });
        }

        /// <summary> Enables a specific UI state without disabling other states. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The UI state to enable. </param>
        public static void UIEnable(ref SystemState systemState, FixedString32Bytes name)
        {
            var state = (byte)K<UIStates>.NameToKey(name);
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"UIState enabled {name}");
            UIEnable(ref systemState, state, true);
        }

        /// <summary> Disables a specific UI state. </summary>
        /// <param name="systemState"> The owning system state. </param>
        /// <param name="name"> The UI state to disable. </param>
        public static void UIDisable(ref SystemState systemState, FixedString32Bytes name)
        {
            var state = (byte)K<UIStates>.NameToKey(name);
            systemState.EntityManager.GetSingleton<BLDebug>(false).Debug($"UIState disabled {name}");
            UIEnable(ref systemState, state, false);
        }

        public static void UIPop(ref SystemState systemState)
        {
            var back = systemState.EntityManager.GetSingletonBuffer<UIStateBack>();
            if (back.Length == 0)
            {
                return;
            }

            systemState.EntityManager.SetSingleton(new UIState { Value = back[^1].Value });
        }

        public static void UICloseAllPopups(ref SystemState systemState)
        {
            var back = systemState.EntityManager.GetSingletonBuffer<UIStateBack>();

            for (var i = back.Length - 1; i >= 0; i--)
            {
                if (back[i].Popup)
                {
                    back.RemoveAt(i);
                }
                else
                {
                    systemState.EntityManager.SetSingleton(new UIState { Value = back[i].Value });
                    return;
                }
            }
        }

        public static void UICloseAllPopupsOrPop(ref SystemState systemState)
        {
            var back = systemState.EntityManager.GetSingletonBuffer<UIStateBack>();
            if (back.Length == 0)
            {
                return;
            }

            var previous = back[^1];
            if (previous.Popup)
            {
                UICloseAllPopups(ref systemState);
            }
            else
            {
                UIPop(ref systemState);
            }
        }

        public static void UIHideAll(ref SystemState systemState)
        {
            systemState.EntityManager.SetSingleton(default(UIState));
        }

        private static void UIEnable(ref SystemState systemState, byte state, bool enabled)
        {
            var clientState = systemState.EntityManager.GetSingletonRW<UIState>();
            clientState.ValueRW.Value[state] = enabled;
        }
    }
}
#endif
