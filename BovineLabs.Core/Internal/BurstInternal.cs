// <copyright file="BurstInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Burst;

    public static class BurstInternal
    {
        public static int HashStringWithFNV1A32(string text)
        {
            return BurstRuntime.HashStringWithFNV1A32(text);
        }
    }
}
