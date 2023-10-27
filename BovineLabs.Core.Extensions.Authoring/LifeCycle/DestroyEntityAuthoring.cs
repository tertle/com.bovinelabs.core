// <copyright file="DestroyEntityAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.Authoring.LifeCycle
{
    using BovineLabs.Core.LifeCycle;
    using Unity.Entities;
    using UnityEngine;

    public class DestroyEntityAuthoring : MonoBehaviour
    {
        private class Baker : Baker<DestroyEntityAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(DestroyEntityAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<DestroyEntity>(entity);
                this.SetComponentEnabled<DestroyEntity>(entity, false);
            }
        }
    }
}
#endif
