// <copyright file="ObjectDefinitionSceneBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;

    // TODO is this slow? can we add it another way
    public class ObjectDefinitionSceneBaker : Baker<Transform>
    {
        public override void Bake(Transform authoring)
        {
            if (authoring.gameObject.IsPrefab())
            {
                return;
            }

            var objectManagementSettings = AuthoringSettingsUtility.GetSettings<ObjectManagementSettings>();

            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(authoring.gameObject);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            if (!objectManagementSettings.ObjectDefinitionMap.TryGetValue(prefab, out var id))
            {
                return;
            }

            var entity = this.GetEntity(TransformUsageFlags.None);
            this.AddComponent(entity, new ObjectId(0, id)); // TODO support mods?
        }
    }
}
#endif
