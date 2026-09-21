namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.InteropServices;
    using AOT;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Scripting.LifecycleManagement;

    public unsafe readonly struct BurstTrampoline
    {
        [NoAutoStaticsCleanup]
        private static GCHandle cachedWrapperHandle;

        [NoAutoStaticsCleanup]
        private static IntPtr cachedWrapperPtr;

        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr _managedFunctionPtr;

        [NativeDisableUnsafePtrRestriction]
        private readonly IntPtr _wrapperPtr;

        public BurstTrampoline(delegate*<void*, int, void> managedFunctionPtr)
        {
            Initialize();
            _wrapperPtr = cachedWrapperPtr;
            _managedFunctionPtr = new IntPtr(managedFunctionPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void WrapperDelegate(void* managedFunctionPtr, void* argumentsPtr, int argumentsSize);

        public bool IsCreated => _managedFunctionPtr != default;

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
            if (_managedFunctionPtr == default)
            {
                throw new NullReferenceException("Trying to invoke a null function pointer.");
            }

            ((delegate* unmanaged[Cdecl]<void*, void*, int, void>)_wrapperPtr)(
                (void*)_managedFunctionPtr,
                argumentsPtr,
                argumentsSize);
        }

        public void Invoke<T>(ref T arguments)
            where T : unmanaged
        {
            fixed (T* argumentsPtr = &arguments)
            {
                Invoke(argumentsPtr, UnsafeUtility.SizeOf<T>());
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
