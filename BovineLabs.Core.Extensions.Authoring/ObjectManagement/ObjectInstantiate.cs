// <copyright file="ObjectInstantiate.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;
    using UnityEngine;

    [RequireComponent(typeof(LinkedEntityGroupAuthoring))]
    public partial class ObjectInstantiate : MonoBehaviour
    {
        [SerializeField]
        private ObjectDefinition? definition;

        private class InstantiateObjectBaker : Baker<ObjectInstantiate>
        {
            public override void Bake(ObjectInstantiate authoring)
            {
                if (!authoring.definition || !authoring.definition.Prefab)
                {
                    return;
                }

                var entity = this.GetEntity(authoring, TransformUsageFlags.WorldSpace);
                var prefab = this.GetEntity(authoring.definition.Prefab, TransformUsageFlags.Dynamic);

                this.AddComponent(entity, new Core.ObjectManagement.ObjectInstantiate { Prefab = prefab });
            }
        }
    }
}
#endif
