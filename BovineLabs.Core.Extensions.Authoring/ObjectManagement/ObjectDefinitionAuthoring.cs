// <copyright file="ObjectDefinitionAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public class ObjectDefinitionAuthoring : MonoBehaviour
    {
        // TODO readonly
        public ObjectDefinition? Definition;

        private class Baker : Baker<ObjectDefinitionAuthoring>
        {
            private static Dictionary<byte, ComponentType>? categoryToComponentTypes;

            public override void Bake(ObjectDefinitionAuthoring authoring)
            {
                if (authoring.Definition == null)
                {
                    return;
                }

                var entity = this.GetEntity(TransformUsageFlags.None);

                this.DependsOn(authoring.Definition);
                this.AddComponent(entity, (ObjectId)authoring.Definition); // TODO support mods?

                var categoriesMap = GetCategories(this);

                var categories = authoring.Definition.Categories;
                while (categories != 0)
                {
                    var categoryIndex = (byte)math.tzcnt(categories);
                    categories ^= 1U << categoryIndex;

                    if (!categoriesMap.TryGetValue(categoryIndex, out var component))
                    {
                        continue;
                    }

                    this.AddComponent(entity, component);
                }
            }

            private static Dictionary<byte, ComponentType> GetCategories(IBaker baker)
            {
                var objectCategories = AuthoringSettingsUtility.GetSettings<ObjectCategories>();
                baker.DependsOn(objectCategories);

                if (categoryToComponentTypes != null)
                {
                    return categoryToComponentTypes;
                }

                categoryToComponentTypes = new Dictionary<byte, ComponentType>();

                if (objectCategories == null)
                {
                    Debug.LogWarning("Categories missing");
                    return categoryToComponentTypes;
                }

                foreach (var c in objectCategories.Components)
                {
                    var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(c.ComponentType);
                    if (typeIndex == TypeIndex.Null)
                    {
                        continue;
                    }

                    if (!categoryToComponentTypes.TryAdd((byte)c.Value, ComponentType.FromTypeIndex(typeIndex)))
                    {
                        Debug.LogWarning($"Duplicate entries for {c.Value}");
                    }
                }

                return categoryToComponentTypes;
            }
        }
    }
}
#endif
