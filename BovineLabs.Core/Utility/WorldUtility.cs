// <copyright file="WorldUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;

    /// <summary> Utility for worlds. </summary>
    public static class WorldUtility
    {
        public static IEnumerable<World> AllExcludingAdvanced()
        {
            foreach (var world in World.All)
            {
                var flags = world.GetMainFlag();

                if (flags == WorldFlags.Live)
                {
                    yield return world;
                }
            }
        }
    }
}