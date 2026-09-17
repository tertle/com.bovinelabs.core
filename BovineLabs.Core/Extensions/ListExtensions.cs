namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class ListExtensions
    {
        public static void AddRangeNative<T>(this List<T> list, NativeArray<T> array)
            where T : struct
        {
            AddRangeNative(list, array, array.Length);
        }

        public static unsafe void AddRangeNative<T>(this List<T> list, NativeArray<T> array, int length)
            where T : struct
        {
            list.AddRangeNative(array.GetUnsafeReadOnlyPtr(), length);
        }

        public static unsafe void AddRangeNative<T>(this List<T> list, NativeSlice<T> nativeSlice)
            where T : struct
        {
            list.AddRangeNative(nativeSlice.GetUnsafeReadOnlyPtr(), nativeSlice.Length);
        }

        public static unsafe void AddRangeNative<T>(this List<T> list, void* arrayBuffer, int length)
            where T : struct
        {
            if (length == 0)
            {
                return;
            }

            var index = list.Count;
            var newLength = index + length;

            // Resize our list if we require
            if (list.Capacity < newLength)
            {
                list.Capacity = newLength;
            }

            var items = NoAllocHelpers.ExtractArrayFromList(list);
            var size = UnsafeUtility.SizeOf<T>();

            // Get the pointer to the end of the list
            var bufferStart = (IntPtr)UnsafeUtility.AddressOf(ref items[0]);
            var buffer = (byte*)(bufferStart + (size * index));

            UnsafeUtility.MemCpy(buffer, arrayBuffer, length * (long)size);

            NoAllocHelpers.ResizeList(list, newLength);
        }

        public static void ClearAddRange<T>(this List<T> list, IEnumerable<T> range)
        {
            list.Clear();
            list.AddRange(range);
        }

        public static void Resize<T>(this List<T> list, int size, T element)
        {
            var count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)
                {
                    list.Capacity = size;
                }

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
    }
}
