// <copyright file="ObjectManagementSettingsBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public abstract class ObjectManagementSettingsBase : SettingsBase
    {
        public abstract int Mod { get; }

        public abstract IReadOnlyCollection<ObjectDefinition> ObjectDefinitions { get; }

        public abstract IReadOnlyCollection<ObjectGroup> ObjectGroups { get; }

        public override void Bake(IBaker baker)
        {
            this.SetupRegistry(baker);
            this.SetupGroups(baker);
        }

        private void SetupRegistry(IBaker baker)
        {
            if (this.ObjectDefinitions.Count == 0)
            {
                return;
            }

            var entity = baker.GetEntity(TransformUsageFlags.None);
            baker.AddComponent(entity, new Mod(this.Mod));
            var registry = baker.AddBuffer<ObjectDefinitionSetupRegistry>(entity);
            var bakingData = baker.AddBuffer<ObjectDefinitionRegistryBakingData>(entity);

            var definitions = this.ObjectDefinitions.Where(o => o != null).ToList();

            // Get the largest ID and resize the array to fit
            // The importer should keep this close to the number of prefabs
            definitions.Sort((d1, d2) => d1.ID.CompareTo(d2.ID));
            var length = definitions[^1].ID + 1;
            registry.ResizeInitialized(length);

            bakingData.Capacity = length;

            foreach (var asset in definitions)
            {
                baker.DependsOn(asset);
                if (asset.ID != 0 && asset.Prefab == null)
                {
                    Debug.LogWarning($"Missing Prefab on {asset}");
                    continue;
                }

                var prefab = baker.GetEntity(asset.Prefab, TransformUsageFlags.None);
                registry[asset.ID] = new ObjectDefinitionSetupRegistry { Prefab = prefab };

                if (prefab != Entity.Null)
                {
                    bakingData.Add(new ObjectDefinitionRegistryBakingData
                    {
                        Prefab = prefab,
                        ObjectId = new ObjectId(this.Mod, asset.ID),
                        ObjectCategory = asset.Categories,
                    });
                }
            }
        }

        private void SetupGroups(IBaker baker)
        {
            // TODO runtime
            if (this.ObjectGroups.Count == 0)
            {
                return;
            }

            var entity = baker.GetEntity(TransformUsageFlags.None);
            var objectGroupRegistry = baker.AddBuffer<ObjectGroupRegistry>(entity).Initialize().AsMap();
            var objectGroupMatcher = baker.AddBuffer<ObjectGroupMatcher>(entity).Initialize().AsHashSet();

            foreach (var group in this.ObjectGroups)
            {
                if (group == null)
                {
                    continue;
                }

                baker.DependsOn(group);
                var definitions = group.GetAllDefinitions();

                foreach (var definition in definitions)
                {
                    var id = new ObjectId(this.Mod, definition.ID);

                    objectGroupMatcher.Add((group, id));
                    objectGroupRegistry.Add(group, id);

                    Check.Assume(objectGroupMatcher.Contains((group, id)));
                }
            }
        }
    }

    [TemporaryBakingType]
    internal struct ObjectDefinitionRegistryBakingData : IBufferElementData
    {
        public Entity Prefab;
        public ObjectId ObjectId;
        public ObjectCategory ObjectCategory;
    }

    /// <summary> Applies the ObjectID component as well as any category components that have been defined. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Baking")]
    internal partial struct ObjectManagementSettingsBaseSystem : ISystem
    {
        private NativeHashMap<byte, ComponentType> categoryToComponentTypes;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.categoryToComponentTypes = new NativeHashMap<byte, ComponentType>(0, Allocator.Persistent);

            var objectCategories = Resources.Load<ObjectCategories>($"{KSettings.KResourceDirectory}/{nameof(ObjectCategories)}");
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

                    if (!this.categoryToComponentTypes.TryAdd((byte)c.Value, ComponentType.FromTypeIndex(typeIndex)))
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

            foreach (var registryData in SystemAPI.Query<DynamicBuffer<ObjectDefinitionRegistryBakingData>>())
            {
                foreach (var definition in registryData)
                {
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
}
#endif
