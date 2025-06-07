// <copyright file="InputActionMapSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    [UpdateInGroup(typeof(InputSystemGroup), OrderFirst = true)]
    public partial class InputActionMapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            this.RequireForUpdate<InputDefault>();
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            // Disable all action maps by default
            var inputAsset = SystemAPI.GetSingleton<InputDefault>().Asset;
            inputAsset.Value.Disable();

            // Enable defaults
            var map = SystemAPI.GetSingletonBuffer<InputDefaultEnabled>();
            foreach (var m in map)
            {
                this.SetInputEnable(inputAsset, m.ActionMap, true);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var enables = SystemAPI.GetSingletonBuffer<InputActionMapEnable>();
            if (enables.Length == 0)
            {
                return;
            }

            var inputAsset = SystemAPI.GetSingleton<InputDefault>().Asset;
            foreach (var state in enables.AsNativeArray())
            {
                this.SetInputEnable(inputAsset, state.Input, state.Enable);
            }

            enables.Clear();
        }

        private void SetInputEnable(UnityObjectRef<InputActionAsset> inputAsset, FixedString32Bytes input, bool enable)
        {
            var actionMap = inputAsset.Value.FindActionMap(input.ToString());
            if (actionMap == null)
            {
                SystemAPI.GetSingleton<BLLogger>().LogWarning($"Unable to find action map of name {input}");
                return;
            }

            if (enable)
            {
                actionMap.Enable();
            }
            else
            {
                actionMap.Disable();
            }
        }
    }
}
