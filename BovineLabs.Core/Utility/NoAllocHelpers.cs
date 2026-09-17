namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    public static class NoAllocHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ExtractArrayFromList<T>(List<T> list)
        {
            return UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list).Items;
        }

        public static void ResizeList<T>(List<T> list, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("invalid size to resize.", nameof(list));
            }

            list.Clear();
            if (list.Capacity < count)
            {
                list.Capacity = count;
            }

            if (count == list.Count)
            {
                return;
            }

            var privateFieldAccess = UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list);
            privateFieldAccess.Size = count;
            ++privateFieldAccess.Version;
        }

        private class ListPrivateFieldAccess<T>
        {
            internal readonly T[] Items = null!;
            internal int Size;
            internal int Version;
        }
    }
}
