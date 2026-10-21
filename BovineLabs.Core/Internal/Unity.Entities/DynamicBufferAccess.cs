namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicBufferAccess
    {
        public static void* GetPtr<T>(DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            var pointer = UnsafeUtility.As<DynamicBuffer<T>, IntPtr>(ref buffer);
            return BufferHeader.GetElementPointer((BufferHeader*)pointer);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetSafety0<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            return buffer.m_Safety0;
        }

        public static AtomicSafetyHandle GetSafety1<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            return buffer.m_Safety1;
        }

        public static bool IsReadOnly<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            return buffer.m_IsReadOnly != 0;
        }
#endif
    }
}
