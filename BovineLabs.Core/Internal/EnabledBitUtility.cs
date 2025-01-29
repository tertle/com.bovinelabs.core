// <copyright file="EnabledBitUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System.Runtime.CompilerServices;
    using Unity.Burst.Intrinsics;

    public static class EnabledBitUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetNextRange(v128 mask, int firstIndexToCheck, out int nextRangeBegin, out int nextRangeEnd)
        {
            return Unity.Entities.EnabledBitUtility.TryGetNextRange(mask, firstIndexToCheck, out nextRangeBegin, out nextRangeEnd);
        }
    }
}
