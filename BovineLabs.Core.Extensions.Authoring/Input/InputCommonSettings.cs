// <copyright file="InputCommonSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Authoring.Input
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Input;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class InputCommonSettings : SettingsBase
    {
        [SerializeField]
        private InputActionAsset? asset;

        [SerializeField]
        public InputActionReference? cursorPosition;

        /// <inheritdoc />
        public override void Bake(IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            var defaultSettings = new InputDefault
            {
                Asset = this.asset!,
                CursorPosition = this.cursorPosition!,
            };

            baker.AddComponent(entity, defaultSettings);
            baker.AddComponent<InputCommon>(entity);
        }
    }
}
#endif
