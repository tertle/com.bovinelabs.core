// <copyright file="NativeArrayProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe struct NativeArrayProxy<T>
        where T : struct
    {
        private readonly void* ptr;
        private readonly int length;

        public NativeArrayProxy(NativeArray<T> nativeArray)
        {
            this.ptr = nativeArray.GetUnsafeReadOnlyPtr();
            this.length = nativeArray.Length;
        }

        public NativeArray<T> ToArray(AtomicSafetyManager* safetyManager)
        {
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(this.ptr, this.length, Allocator.Invalid);
            safetyManager->MarkNativeArrayAsReadOnly(ref nativeArray);
            return nativeArray;
        }
    }
}