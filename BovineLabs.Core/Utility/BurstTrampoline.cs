// <copyright file="BurstTrampoline.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.InteropServices;
    using AOT;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Packs managed callback arguments into a single pointer and size payload so the same unmanaged wrapper can dispatch any signature.
    /// </summary>
    public unsafe readonly struct BurstTrampoline
    {
        private static GCHandle cachedWrapperHandle;
        private static IntPtr cachedWrapperPtr;

        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr managedFunctionPtr;

        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr wrapperPtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="BurstTrampoline"/> struct.
        /// </summary>
        /// <param name="managedFunctionPtr">
        /// Callback with a single payload pointer and payload size.
        /// </param>
        public BurstTrampoline(delegate*<void*, int, void> managedFunctionPtr)
        {
            Initialize();
            this.wrapperPtr = cachedWrapperPtr;
            this.managedFunctionPtr = new IntPtr(managedFunctionPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WrapperDelegate(void* managedFunctionPtr, void* argumentsPtr, int argumentsSize);

        public bool IsCreated => this.managedFunctionPtr != default;

        [MonoPInvokeCallback(typeof(WrapperDelegate))]
        private static void Wrapper(void* managedFunctionPtr, void* argumentsPtr, int argumentsSize)
        {
            ((delegate*<void*, int, void>)managedFunctionPtr)(argumentsPtr, argumentsSize);
        }

        private static void Initialize()
        {
            if (cachedWrapperPtr != default)
            {
                return;
            }

            WrapperDelegate wrapperDelegate = Wrapper;
            cachedWrapperHandle = GCHandle.Alloc(wrapperDelegate);
            cachedWrapperPtr = Marshal.GetFunctionPointerForDelegate(wrapperDelegate);
        }

        public void Invoke(void* argumentsPtr, int argumentsSize)
        {
            if (this.managedFunctionPtr == default)
            {
                throw new NullReferenceException("Trying to invoke a null function pointer.");
            }

            ((delegate* unmanaged[Cdecl]<void*, void*, int, void>)this.wrapperPtr)(
                (void*)this.managedFunctionPtr,
                argumentsPtr,
                argumentsSize);
        }

        public void Invoke<T>(ref T arguments)
            where T : unmanaged
        {
            fixed (T* argumentsPtr = &arguments)
            {
                this.Invoke(argumentsPtr, UnsafeUtility.SizeOf<T>());
            }
        }

        public static ref T ArgumentsFromPtr<T>(void* argumentsPtr, int size)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (size != UnsafeUtility.SizeOf<T>())
            {
                throw new InvalidOperationException("The requested argument type size does not match the provided one.");
            }
#endif
            return ref *(T*)argumentsPtr;
        }
    }
}
