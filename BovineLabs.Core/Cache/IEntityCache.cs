// <copyright file="IEntityCache.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Cache
{
    using Unity.Entities;

    public interface IEntityCache
    {
        Entity Entity { get; set; }
    }
}
