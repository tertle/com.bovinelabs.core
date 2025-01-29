// <copyright file="ObjectManagementSettingsBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using UnityEngine;

    public abstract class ObjectManagementSettingsBase : SettingsBase
    {
        private Dictionary<GameObject, int>? objectDefinitionMap;

        public abstract int Mod { get; }

        public abstract IReadOnlyCollection<ObjectDefinition> ObjectDefinitions { get; }

        public abstract IReadOnlyCollection<ObjectGroup> ObjectGroups { get; }

        public Dictionary<GameObject, int> ObjectDefinitionMap
        {
            get
            {
                this.OnValidate();

                if (this.objectDefinitionMap == null)
                {
                    var notNull = this.ObjectDefinitions.Where(o => o.Prefab != null).ToArray();
                    var distinct = notNull.Distinct(default(PrefabDistinct)).ToArray();
                    if (distinct.Length != notNull.Length)
                    {
                        Debug.LogError("Non-unique object definitions. Make a prefab instance if you need to duplicate one");
                    }

                    this.objectDefinitionMap = distinct.ToDictionary(o => o.Prefab!, o => o.ID);
                }

                return this.objectDefinitionMap;
            }
        }

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            this.SetupRegistry(baker);
            this.SetupGroups(baker);

#if !BL_DISABLE_LIFECYCLE
            this.SetupLookups(baker);
#endif
        }

        private void OnValidate()
        {
            if (this.objectDefinitionMap == null)
            {
                return;
            }

            if (this.ObjectDefinitions.Count != this.objectDefinitionMap.Count)
            {
                this.objectDefinitionMap = null;
            }
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

            var definitions = this.ObjectDefinitions.Where(o => o != null).ToList();

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
                registry[asset.ID] = new ObjectDefinitionSetupRegistry { Prefab = prefab };
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

#if !BL_DISABLE_LIFECYCLE
        private void SetupLookups(IBaker baker)
        {
            if (this.ObjectDefinitions.Count == 0)
            {
                return;
            }

            var entity = baker.GetEntity(TransformUsageFlags.None);
            var maps = new Dictionary<Type, object>();

            foreach (var d in this.ObjectDefinitions)
            {
                if (d == null || d.Prefab == null)
                {
                    continue;
                }

                foreach (var c in d.Prefab.GetComponents<ILookupAuthoring>())
                {
                    c.Bake(baker, entity, d, maps);
                }
            }
        }
#endif

        private struct PrefabDistinct : IEqualityComparer<ObjectDefinition>
        {
            public bool Equals(ObjectDefinition x, ObjectDefinition y)
            {
                return x.Prefab == y.Prefab;
            }

            public int GetHashCode(ObjectDefinition obj)
            {
                return obj.Prefab!.GetHashCode();
            }
        }
    }
}
#endif
