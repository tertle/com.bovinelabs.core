// <copyright file="NativeReferenceExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeReferenceExtensions
    {
        public static ref T ValueRef<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
            return ref UnsafeUtility.AsRef<T>(reference.GetUnsafePtr());
        }
    }
}
