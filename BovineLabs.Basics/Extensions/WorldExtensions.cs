// <copyright file="WorldExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    public static class WorldExtensions
    {
        public static WorldFlags GetMainFlag(this World world)
        {
            if ((world.Flags & WorldFlags.Shadow) != 0)
            {
                return WorldFlags.Shadow;
            }

            if ((world.Flags & WorldFlags.Conversion) != 0)
            {
                return WorldFlags.Conversion;
            }

            if ((world.Flags & WorldFlags.Live) != 0)
            {
                return WorldFlags.Live;
            }

            if ((world.Flags & WorldFlags.Streaming) != 0)
            {
                return WorldFlags.Streaming;
            }

            if ((world.Flags & WorldFlags.Staging) != 0)
            {
                return WorldFlags.Staging;
            }

            return WorldFlags.None;
        }
    }
}