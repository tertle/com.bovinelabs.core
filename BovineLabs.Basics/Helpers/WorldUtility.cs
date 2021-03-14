// <copyright file="WorldUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Helpers
{
    using System.Collections.Generic;
    using Unity.Entities;

    /// <summary> Utility for worlds. </summary>
    public static class WorldUtility
    {
        /// <summary> Get all worlds excluding the built in loading ones. </summary>
        /// <returns> The worlds. </returns>
        public static IEnumerable<World> AllExcludingLoading()
        {
            foreach (var world in World.All)
            {
                if (world.Name.StartsWith("LoadingWorld"))
                {
                    continue;
                }

                yield return world;
            }
        }
    }
}