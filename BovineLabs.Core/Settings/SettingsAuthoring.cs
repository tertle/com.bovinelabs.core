// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System.Collections.Generic;
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField]
        private Settings[] settings;

        /// <inheritdoc/>
        void IDeclareReferencedPrefabs.DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
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

                setting.DeclareReferencedPrefabs(referencedPrefabs);
            }
        }

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

                // TODO this only works on game objects
                // conversionSystem.DeclareAssetDependency(this.gameObject, setting);

                setting.Convert(entity, dstManager, conversionSystem);
            }
        }
    }
}
