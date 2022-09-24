// <copyright file="NativeArrayProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [DebuggerTypeProxy(typeof(NativeArrayProxyDebugView<>))]
    public readonly unsafe struct NativeArrayProxy<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly void* Ptr;
        internal readonly int Length;

        public NativeArrayProxy(NativeArray<T> nativeArray)
        {
            this.Ptr = nativeArray.GetUnsafeReadOnlyPtr();
            this.Length = nativeArray.Length;
        }

        public NativeArray<T> AsArray(AtomicSafetyManager* safetyManager)
        {
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(this.Ptr, this.Length, Allocator.Invalid);
            safetyManager->MarkNativeArrayAsReadOnly(ref nativeArray);
            return nativeArray;
        }
    }

    public sealed class NativeArrayProxyDebugView<T>
        where T : unmanaged
    {
        private readonly NativeArrayProxy<T> array;

        public NativeArrayProxyDebugView(NativeArrayProxy<T> array)
        {
            this.array = array;
        }

        public unsafe T[] Items
        {
            get
            {
                var items = new T[this.array.Length];
                var gcHandle = GCHandle.Alloc(items, GCHandleType.Pinned);
                UnsafeUtility.MemCpy(
                    (void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject()),
                    (void*)((IntPtr)array.Ptr),
                    UnsafeUtility.SizeOf<T>() * this.array.Length);

                gcHandle.Free();
                return items;
            }
        }
    }
}
