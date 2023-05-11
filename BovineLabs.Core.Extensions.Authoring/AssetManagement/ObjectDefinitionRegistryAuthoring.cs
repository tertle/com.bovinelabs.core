// <copyright file="ObjectDefinitionRegistryAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.AssetManagement
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using BovineLabs.Core.AssetManagement;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;

    public partial class ObjectDefinitionRegistryAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ObjectDefinitionRegistryAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(ObjectDefinitionRegistryAuthoring authoring)
            {
                this.SetupRegistry();
                this.SetupCategories();
            }

            private void SetupCategories()
            {
                var objectCategories = Resources.Load<ObjectCategories>("ObjectCategories");
                if (objectCategories == null)
                {
                    Debug.LogWarning("Categories missing");
                    return;
                }

                var entity = this.GetEntity(TransformUsageFlags.None);
                var components = this.AddBuffer<ObjectCategoryComponents>(entity);

                var unique = new HashSet<byte>();

                foreach (var c in objectCategories.Components)
                {
                    var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(c.ComponentType);
                    if (typeIndex == TypeIndex.Null)
                    {
                        continue;
                    }

                    if (!unique.Add(c.Value))
                    {
                        Debug.LogWarning($"Duplicate entries for {c.Value}");
                        continue;
                    }

                    if (c.Value > ObjectCategory.MaxBits)
                    {
                        Debug.LogWarning($"Value outside bit field range {c.Value}");
                        continue;
                    }

                    components.Add(new ObjectCategoryComponents
                    {
                        CategoryBit = c.Value,
                        StableTypeHash = c.ComponentType,
                    });
                }
            }

            private void SetupRegistry()
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                var registry = this.AddBuffer<ObjectDefinitionRegistryBakingData>(entity);

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
                    registry[asset.ID] = new ObjectDefinitionRegistryBakingData
                    {
                        Prefab = prefab,
                        ObjectId = asset.ID,
                        ObjectCategory = asset.Categories,
                    };
                }
            }
        }

        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Baking")]
        public partial struct System : ISystem
        {
            private NativeHashMap<byte, ComponentType> categoryToComponentTypes;

            /// <inheritdoc/>
            public void OnCreate(ref SystemState state)
            {
                this.categoryToComponentTypes = new NativeHashMap<byte, ComponentType>(0, Allocator.Persistent);

                var objectCategories = Resources.Load<ObjectCategories>("ObjectCategories");
                if (objectCategories == null)
                {
                    Debug.LogWarning("Categories missing");
                }
                else
                {
                    foreach (var c in objectCategories.Components)
                    {
                        var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(c.ComponentType);
                        if (typeIndex == TypeIndex.Null)
                        {
                            continue;
                        }

                        if (!this.categoryToComponentTypes.TryAdd(c.Value, ComponentType.FromTypeIndex(typeIndex)))
                        {
                            Debug.LogWarning($"Duplicate entries for {c.Value}");
                        }
                    }
                }
            }

            /// <inheritdoc/>
            public void OnDestroy(ref SystemState state)
            {
                this.categoryToComponentTypes.Dispose();
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

                foreach (var (registryData, entity) in SystemAPI.Query<DynamicBuffer<ObjectDefinitionRegistryBakingData>>().WithEntityAccess())
                {
                    var registry = ecb.AddBuffer<ObjectDefinitionRegistry>(entity);

                    foreach (var definition in registryData)
                    {
                        registry.Add(new ObjectDefinitionRegistry { Prefab = definition.Prefab });

                        if (definition.Prefab == Entity.Null)
                        {
                            continue;
                        }

                        ecb.AddComponent(definition.Prefab, definition.ObjectId);
                        // ecb.AddComponent(definition.Prefab, definition.ObjectCategory);

                        var categories = definition.ObjectCategory.Value;
                        while (categories != 0)
                        {
                            var categoryIndex = (byte)math.tzcnt(categories);
                            categories ^= 1UL << categoryIndex;

                            if (!this.categoryToComponentTypes.TryGetValue(categoryIndex, out var component))
                            {
                                continue;
                            }

                            ecb.AddComponent(definition.Prefab, component);
                        }
                    }
                }

                ecb.Playback(state.EntityManager);
            }
        }

        [BakingType]
        private struct ObjectDefinitionRegistryBakingData : IBufferElementData
        {
            public Entity Prefab;
            public ObjectId ObjectId;
            public ObjectCategory ObjectCategory;
        }
    }
}
#endif
