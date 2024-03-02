// <copyright file="LifeCycleAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.Authoring.LifeCycle
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.LifeCycle;
    using Unity.Entities;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class LifeCycleAuthoring : MonoBehaviour
    {
        public bool AddInstantiate = true;
        public bool DisableInstantiateOnInstance = true;
        public bool AddDestroy = true;

        private class Baker : Baker<LifeCycleAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(LifeCycleAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                if (authoring.AddInstantiate)
                {
                    this.AddComponent<InitializeEntity>(entity);

                    if (!authoring.IsPrefab() && authoring.DisableInstantiateOnInstance)
                    {
                        this.SetComponentEnabled<InitializeEntity>(entity, false);
                    }
                }

                if (authoring.AddDestroy)
                {
                    this.AddComponent<DestroyEntity>(entity);
                    this.SetComponentEnabled<DestroyEntity>(entity, false);
                }
            }
        }
    }
}
#endif
