// <copyright file="SelectedEntities.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    [InternalBufferCapacity(512)] // On a unique chunk, may as well use it
    public struct SelectedEntities : IBufferElementData
    {
        public Entity Value;
    }
}
