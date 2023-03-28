// <copyright file="ObjectDefinitionRegistryAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.AssetManagement
{
    using System.Linq;
    using BovineLabs.Core.AssetManagement;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;

    public class ObjectDefinitionRegistryAuthoring : MonoBehaviour
    {
    }

    public class ObjectDefinitionRegistryBaker : Baker<ObjectDefinitionRegistryAuthoring>
    {
        public override void Bake(ObjectDefinitionRegistryAuthoring authoring)
        {
            var registry = this.AddBuffer<ObjectDefinitionRegistry>(this.GetEntity(TransformUsageFlags.None));

            var definitions = AssetDatabase.FindAssets($"t:{nameof(ObjectDefinition)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ObjectDefinition>)
                .ToList();

            if (definitions.Count == 0)
            {
                return;
            }

            // Get the largest ID and resize the array to fit
            // The importer should keep this close to the number of prefabs
            definitions.Sort((d1, d2) => d1.ID.CompareTo(d2.ID));
            var length = definitions[^1].ID + 1;
            registry.ResizeInitialized(length);

            foreach (var asset in definitions)
            {
                this.DependsOn(asset);
                if (asset.ID != 0 && asset.Prefab == null)
                {
                    Debug.LogWarning($"Missing Prefab on {asset}");
                    continue;
                }

                var prefab = this.GetEntity(asset.Prefab, TransformUsageFlags.None);
                registry[asset.ID] = new ObjectDefinitionRegistry { Prefab = prefab };
            }
        }
    }
}
#endif
