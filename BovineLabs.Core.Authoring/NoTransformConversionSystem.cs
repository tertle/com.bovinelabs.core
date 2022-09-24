// <copyright file="NoTransformConversionSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    [ConverterVersion("tim", 1)]
    internal class NoTransformConversionSystem : GameObjectConversionSystem
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.Entities
                .WithAll<StaticOptimizeEntity>()
                .ForEach((NoTransform noTransform) =>
                {
                    if (noTransform.RemoveFromChildren)
                    {
                        var children = noTransform.GetComponentsInChildren<Transform>();

                        foreach (var child in children)
                        {
                            RemoveComponents(child);
                        }
                    }
                    else
                    {
                        RemoveComponents(noTransform.transform);
                    }

                    void RemoveComponents(Component component)
                    {
                        var entity = this.GetPrimaryEntity(component);

                        // This check ensures avoiding breaking cases where something like Physics has added components back that are required
                        if (!this.DstEntityManager.HasComponent<Translation>(entity))
                        {
                            this.DstEntityManager.RemoveComponent<Static>(entity);
                            this.DstEntityManager.RemoveComponent<LocalToWorld>(entity);
                        }
                    }
                });
        }
    }
}
