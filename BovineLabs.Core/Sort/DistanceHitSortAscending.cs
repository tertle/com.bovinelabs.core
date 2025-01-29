// <copyright file="DistanceHitSortAscending.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Sort
{
    using System.Collections.Generic;
    using Unity.Physics;

    public struct DistanceHitSortAscending : IComparer<DistanceHit>
    {
        /// <inheritdoc />
        public int Compare(DistanceHit x, DistanceHit y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }
}
#endif
