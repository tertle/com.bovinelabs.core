// <copyright file="MinMaxAABBExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using MinMaxAABB = Unity.Mathematics.MinMaxAABB;

    public static class MinMaxAABBExtensions
    {
        /// <summary> Tests if the input AABB overlaps this AABB. </summary>
        /// <param name="a"> AABB to test from. </param>
        /// <param name="b"> AABB to test. </param>
        /// <returns> True if input AABB overlaps with this AABB. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps(this MinMaxAABB a, MinMaxAABB b)
        {
            return math.all((a.Max >= b.Min) & (a.Min <= b.Max));
        }
    }
}
