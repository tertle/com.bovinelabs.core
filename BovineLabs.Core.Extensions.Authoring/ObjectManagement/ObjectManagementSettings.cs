// <copyright file="ObjectManagementSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Core")]
    public sealed class ObjectManagementSettings : ObjectManagementSettingsBase
    {
        [SerializeField]
        private ObjectDefinition[] objectDefinitions = Array.Empty<ObjectDefinition>();

        [SerializeField]
        private ObjectGroup[] objectGroups = Array.Empty<ObjectGroup>();

        public override int Mod => 0;

        public override IReadOnlyCollection<ObjectDefinition> ObjectDefinitions => this.objectDefinitions;

        public override IReadOnlyCollection<ObjectGroup> ObjectGroups => this.objectGroups;

        protected override void CustomBake(Baker<SettingsAuthoring> baker, Entity entity)
        {
            // Setup categories
            var objectCategories = Resources.Load<ObjectCategories>($"{KSettingsBase.KResourceDirectory}/{nameof(ObjectCategories)}");
            baker.DependsOn(objectCategories);

            var components = baker.AddBuffer<ObjectCategoryComponents>(entity);

            if (objectCategories == null)
            {
                Debug.LogWarning("Categories missing");
                return;
            }

            var unique = new HashSet<int>();

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

                if (c.Value >= ObjectCategory.MaxBits)
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
    }
}
#endif
