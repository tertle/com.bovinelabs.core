// <copyright file="WorldUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Collections.Generic;
    using Unity.Entities;

    /// <summary> Utility for worlds. </summary>
    public static class WorldUtility
    {
        public static IEnumerable<World> AllExcludingAdvanced()
        {
            foreach (var world in World.All)
            {
                if ((world.Flags & WorldFlags.Live) == WorldFlags.Live)
                {
                    yield return world;
                }
            }
        }
    }
}
