// <copyright file="SelectedEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    // TODO add support for multi selections?
    public struct SelectedEntity : IComponentData
    {
        public Entity Value;
    }
}
