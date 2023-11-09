// <copyright file="ObjectManagementSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Objects")]
    public sealed partial class ObjectManagementSettings : ObjectManagementSettingsBase
    {
        [SerializeField]
        private ObjectDefinition[] objectDefinitions = Array.Empty<ObjectDefinition>();

        [SerializeField]
        private ObjectGroup[] objectGroups = Array.Empty<ObjectGroup>();

        public override int Mod => 0;

        public override IReadOnlyCollection<ObjectDefinition> ObjectDefinitions => this.objectDefinitions;

        public override IReadOnlyCollection<ObjectGroup> ObjectGroups => this.objectGroups;

        public override void Bake(IBaker baker)
        {
            base.Bake(baker);

            this.SetupCategories(baker);
        }

        private void SetupCategories(IBaker baker)
        {
            var objectCategories = Resources.Load<ObjectCategories>("K/" + nameof(ObjectCategories));
            baker.DependsOn(objectCategories);

            var entity = baker.GetEntity(TransformUsageFlags.None);
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

                if (c.Value is < 0 or >= ObjectCategory.MaxBits)
                {
                    Debug.LogWarning($"Value outside bit field range {c.Value}");
                    continue;
                }

                components.Add(new ObjectCategoryComponents
                {
                    CategoryBit = (byte)c.Value,
                    StableTypeHash = c.ComponentType,
                });
            }
        }
    }
}
#endif
