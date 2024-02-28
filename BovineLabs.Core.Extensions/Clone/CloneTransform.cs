// <copyright file="CloneTransform.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Clone
{
    using Unity.Entities;

    public struct CloneTransform : IComponentData
    {
        public Entity Value;
    }
}
