﻿// <copyright file="LifeCycleAuthoring.cs" company="BovineLabs">
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
        public bool AddInitialize = true;
        public bool AddDestroy = true;

        public static void AddComponents(IBaker baker, Entity entity, bool isPrefab, bool addInitialize, bool addDestroy)
        {
            if (addInitialize)
            {
                if (isPrefab)
                {
                    baker.AddComponent<InitializeEntity>(entity);
                    baker.SetComponentEnabled<InitializeEntity>(entity, true);
                }
                else
                {
                    baker.AddComponent<InitializeSubSceneEntity>(entity);
                    baker.SetComponentEnabled<InitializeSubSceneEntity>(entity, true);
                }
            }

            if (addDestroy)
            {
                baker.AddComponent<DestroyEntity>(entity);
                baker.SetComponentEnabled<DestroyEntity>(entity, false);
            }
        }

        private class Baker : Baker<LifeCycleAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(LifeCycleAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                var isPrefab = authoring.IsPrefab();

                AddComponents(this, entity, isPrefab, authoring.AddInitialize, authoring.AddDestroy);
            }
        }
    }
}
#endif
