// <copyright file="EntityDestroyAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_DESTROY
namespace BovineLabs.Core.Authoring.Destroy
{
    using BovineLabs.Core.Destroy;
    using Unity.Entities;
    using UnityEngine;

    public class EntityDestroyAuthoring : MonoBehaviour
    {
    }

    public class EntityDestroyBaker : Baker<EntityDestroyAuthoring>
    {
        public override void Bake(EntityDestroyAuthoring authoring)
        {
            this.AddComponent<EntityDestroy>();
        }
    }
}
#endif
