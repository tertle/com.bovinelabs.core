// <copyright file="InputSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Authoring.Input
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Input;
    using Unity.Entities;
    using UnityEngine;

    public class InputSettings : SettingsBase
    {
        [SerializeField]
        private InputDefault core = new();

        /// <inheritdoc />
        public override void Bake(IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            baker.AddComponentObject(entity, this.core);
            baker.AddComponent<InputCommon>(entity);
        }
    }
}
#endif
