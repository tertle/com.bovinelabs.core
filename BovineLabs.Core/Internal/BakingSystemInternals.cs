// <copyright file="BakingSystemInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class BakingSystemInternals
    {
        public static Hash128 SceneGUID(this BakingSystem bakingSystem)
        {
            return bakingSystem.BakingSettings.SceneGUID;
        }
    }
}
