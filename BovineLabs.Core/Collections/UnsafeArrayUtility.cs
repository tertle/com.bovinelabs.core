namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;

    public static class UnsafeArrayUtility
    {
        public static unsafe UnsafeArray<T> ConvertExistingDataToUnsafeArray<T>(void* dataPointer, int length, Allocator allocator)
            where T : struct
        {
            CheckConvertArguments<T>(length, allocator);
            return new UnsafeArray<T>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,
                m_MinIndex = 0,
                m_MaxIndex = length - 1,
            };
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments<T>(int length, Allocator allocator)
            where T : struct
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }
    }
}