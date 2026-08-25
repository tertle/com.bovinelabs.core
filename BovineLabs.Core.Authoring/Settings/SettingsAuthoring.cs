// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;

    public class SettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private SettingsBase[] settings = Array.Empty<SettingsBase>();

        /// <summary> Gets the asset GUID of the prefab rooted at <paramref name="authoring" />. </summary>
        /// <param name="authoring"> The settings authoring on the prefab root. </param>
        /// <returns> The prefab asset GUID. </returns>
        /// <exception cref="ArgumentNullException"> The authoring is null. </exception>
        /// <exception cref="InvalidOperationException"> The authoring is not on a valid prefab root. </exception>
        internal static Hash128 GetPrefabGuid(SettingsAuthoring authoring)
        {
            if (!authoring)
            {
                throw new ArgumentNullException(nameof(authoring));
            }

            if (!IsPrefabRoot(authoring))
            {
                throw new InvalidOperationException($"{nameof(SettingsAuthoring)} '{authoring.name}' must be on a prefab root.");
            }

            var path = EditorUtility.IsPersistent(authoring) ?
                AssetDatabase.GetAssetPath(authoring) :
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(authoring);

            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{nameof(SettingsAuthoring)} '{authoring.name}' must belong to a prefab asset.");
            }

            var prefabGuid = new Hash128(AssetDatabase.AssetPathToGUID(path));
            if (!prefabGuid.IsValid)
            {
                throw new InvalidOperationException($"Unable to resolve the prefab GUID for {nameof(SettingsAuthoring)} '{authoring.name}'.");
            }

            return prefabGuid;
        }

        /// <inheritdoc />
        private class Baker : Baker<SettingsAuthoring>
        {
            /// <inheritdoc />
            public override void Bake(SettingsAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<SettingsTag>(entity);

                var prefabGuid = GetPrefabGuid(authoring);
                if (this.IsBakingForEditor())
                {
                    this.AddComponent(entity, new SettingsPrefabIdentity { PrefabGuid = prefabGuid });
                }

                foreach (var setting in authoring.settings.Distinct())
                {
                    if (!setting)
                    {
                        BLGlobalLogger.LogWarning512($"Setting is not set on {authoring.gameObject.name} in {authoring.gameObject.scene.name}");
                        continue;
                    }

                    this.DependsOn(setting);
                    setting.Bake(this);
                }
            }
        }

        private static bool IsPrefabRoot(SettingsAuthoring authoring)
        {
            return PrefabUtility.IsAnyPrefabInstanceRoot(authoring.gameObject) ||
                (PrefabUtility.IsPartOfPrefabAsset(authoring) && !authoring.transform.parent);
        }
    }
}
