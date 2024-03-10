// <copyright file="IBlobCurve.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Burst;

    public interface IBlobCurve<out T>
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T EvaluateIgnoreWrapMode(in float time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Evaluate(in float time, [NoAlias] ref BlobCurveCache cache);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Evaluate(in float time);
    }
}
