// <copyright file="InputActionMapSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;

    [UpdateInGroup(typeof(InputSystemGroup), OrderFirst = true)]
    public partial class InputActionMapSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            var inputAsset = SystemAPI.GetSingleton<InputDefault>().Asset;

            // Disable all action maps by default
            foreach (var am in inputAsset.Value.actionMaps)
            {
                am.Disable();
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var enables = SystemAPI.GetSingletonBuffer<InputActionMapEnable>();
            if (enables.Length == 0)
            {
                return;
            }

            var inputAsset = SystemAPI.GetSingleton<InputDefault>().Asset;
            foreach (var s in enables.AsNativeArray())
            {
                var actionMap = inputAsset.Value.FindActionMap(s.Input.ToString(), true);
                if (s.Enable)
                {
                    actionMap.Enable();
                }
                else
                {
                    actionMap.Disable();
                }
            }

            enables.Clear();
        }
    }
}
#endif
