// <copyright file="Worlds.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

    public static class Worlds
    {
        public const WorldSystemFilterFlags Service = (WorldSystemFilterFlags)(1 << 21);
        public const WorldFlags ServiceWorld = (WorldFlags)(1 << 16) | WorldFlags.Live;
    }
}
