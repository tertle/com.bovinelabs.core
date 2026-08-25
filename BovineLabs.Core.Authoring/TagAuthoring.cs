// <copyright file="TagAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> A simple authoring script that lets you tag any entity. </summary>
    public class TagAuthoring : MonoBehaviour
    {
        public ComponentAsset[] Components = Array.Empty<ComponentAsset>();

        private class TagBaker : Baker<TagAuthoring>
        {
            public override void Bake(TagAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                foreach (var c in authoring.Components)
                {
                    if (c == null)
                    {
                        continue;
                    }

                    this.DependsOn(c);

                    this.AddComponent(entity, c.ResolveType());
                }
            }
        }
    }
}
