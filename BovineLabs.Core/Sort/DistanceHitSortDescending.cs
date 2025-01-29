// <copyright file="DistanceHitSortDescending.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Sort
{
    using System.Collections.Generic;
    using Unity.Physics;

    public struct DistanceHitSortDescending : IComparer<DistanceHit>
    {
        /// <inheritdoc />
        public int Compare(DistanceHit x, DistanceHit y)
        {
            return y.Distance.CompareTo(x.Distance);
        }
    }
}
#endif
