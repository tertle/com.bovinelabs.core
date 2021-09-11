// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private Settings[] settings;

        /// <inheritdoc/>
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (this.settings == null)
            {
                return;
            }

            foreach (var setting in this.settings)
            {
                if (setting == null)
                {
                    Debug.LogWarning("Setting is null");
                    continue;
                }

                setting.Convert(entity, dstManager, conversionSystem);
            }
        }
    }
}
