// <copyright file="ObjectManagementSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Settings;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [SettingsGroup("Objects")]
    public partial class ObjectManagementSettings : SettingsBase
    {
        [SerializeField]
        private ObjectDefinition[] objectDefinitions = Array.Empty<ObjectDefinition>();

        [SerializeField]
        private ObjectGroup[] objectGroups = Array.Empty<ObjectGroup>();

        public override void Bake(IBaker baker)
        {
            this.SetupRegistry(baker);
            this.SetupCategories(baker);
            this.SetupGroups(baker);
        }

        private void SetupRegistry(IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            var registry = baker.AddBuffer<ObjectDefinitionRegistryBakingData>(entity);

            if (this.objectDefinitions.Length == 0)
            {
                return;
            }

            var definitions = this.objectDefinitions.Where(o => o != null).ToList();

            // Get the largest ID and resize the array to fit
            // The importer should keep this close to the number of prefabs
            definitions.Sort((d1, d2) => d1.ID.CompareTo(d2.ID));
            var length = definitions[^1].ID + 1;
            registry.ResizeInitialized(length);

            foreach (var asset in definitions)
            {
                baker.DependsOn(asset);
                if (asset.ID != 0 && asset.Prefab == null)
                {
                    Debug.LogWarning($"Missing Prefab on {asset}");
                    continue;
                }

                var prefab = baker.GetEntity(asset.Prefab, TransformUsageFlags.None);
                registry[asset.ID] = new ObjectDefinitionRegistryBakingData
                {
                    Prefab = prefab,
                    ObjectId = asset.ID,
                    ObjectCategory = asset.Categories,
                };
            }
        }

        private void SetupCategories(IBaker baker)
        {
            var objectCategories = Resources.Load<ObjectCategories>(nameof(ObjectCategories));
            if (objectCategories == null)
            {
                Debug.LogWarning("Categories missing");
                return;
            }

            baker.DependsOn(objectCategories);

            var entity = baker.GetEntity(TransformUsageFlags.None);
            var components = baker.AddBuffer<ObjectCategoryComponents>(entity);

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

        private void SetupGroups(IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            var objectGroupRegistry = baker.AddBuffer<ObjectGroupMatcher>(entity).Initialize().AsHashSet();

            foreach (var group in this.objectGroups)
            {
                if (group == null)
                {
                    continue;
                }

                baker.DependsOn(group);
                var definitions = group.GetAllDefinitions();

                foreach (var definition in definitions)
                {
                    objectGroupRegistry.Add((group.ID, definition.ID));

                    Check.Assume(objectGroupRegistry.Contains((group.ID, definition.ID)));
                }
            }
        }

        /// <summary> Applies the ObjectID component as well as any category components that have been defined. </summary>
        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Baking")]
        private partial struct System : ISystem
        {
            private NativeHashMap<byte, ComponentType> categoryToComponentTypes;

            /// <inheritdoc/>
            public void OnCreate(ref SystemState state)
            {
                this.categoryToComponentTypes = new NativeHashMap<byte, ComponentType>(0, Allocator.Persistent);

                var objectCategories = Resources.Load<ObjectCategories>(nameof(ObjectCategories));
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

                        var categories = definition.ObjectCategory.Value;
                        while (categories != 0)
                        {
                            var categoryIndex = (byte)math.tzcnt(categories);
                            categories ^= 1U << categoryIndex;

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
