// <copyright file="WorldSpaceAuthoring.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Entities;
    using UnityEngine;

    public class WorldSpaceAuthoring : MonoBehaviour
    {
    }

    public class WorldSpaceBaker : Baker<WorldSpaceAuthoring>
    {
        public override void Bake(WorldSpaceAuthoring authoring)
        {
            this.GetEntity(TransformUsageFlags.WorldSpace);
        }
    }
}
