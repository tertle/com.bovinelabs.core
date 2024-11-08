// <copyright file="SelectedEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    public struct SelectedEntity : IComponentData
    {
        public Entity Value;
    }
}
