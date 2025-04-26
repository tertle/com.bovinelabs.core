// <copyright file="BurstUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Burst;
    using Unity.Entities;

    [BurstCompile]
    public static class BurstUtil
    {
        [BurstCompile]
        public static bool IsEmpty(ref EntityQuery query)
        {
            return query.IsEmpty;
        }
    }
}
