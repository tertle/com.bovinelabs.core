namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public interface IPredicate<in T>
    {
        bool Check(T other);
    }

    public interface ISelector<in TInput, out TOutput>
    {
        TOutput Select(TInput val);
    }

    public struct Equals<T> : IPredicate<T>
        where T : IEquatable<T>
    {
        private readonly T _value;

        public Equals(T value)
        {
            _value = value;
        }

        public bool Check(T other)
        {
            return _value.Equals(other);
        }
    }

    public static unsafe class NativeArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this NativeArray<T> array, int index)
            where T : unmanaged
        {
            CheckElementWriteAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ptr<T> ElementAtAsPtr<T>(this NativeArray<T> array, int index)
            where T : unmanaged
        {
            CheckElementWriteAccess(array, index);
            return new Ptr<T>((T*)array.GetUnsafePtr() + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this NativeArray<T> array, int index)
            where T : unmanaged
        {
            CheckElementReadAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this NativeArray<T>.ReadOnly array, int index)
            where T : unmanaged
        {
            CheckElementReadAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtROUnsafe<T>(this NativeArray<T> array, int index)
            where T : unmanaged
        {
            CheckElementReadAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtROUnsafe<T>(this NativeArray<T>.ReadOnly array, int index)
            where T : unmanaged
        {
            CheckElementReadAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }


        public static void Fill<T>(this NativeArray<T> array, T value)
            where T : unmanaged
        {
            UnsafeUtility.MemCpyReplicate(array.GetUnsafePtr(), &value, UnsafeUtility.SizeOf<T>(), array.Length);
        }

        public static void Clear<T>(this NativeArray<T> array)
            where T : unmanaged
        {
            UnsafeUtility.MemClear(array.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * array.Length);
        }

        public static void Reverse<T>(this NativeArray<T> array)
            where T : unmanaged
        {
            var halfLength = array.Length / 2;
            var i = 0;
            var j = array.Length - 1;

            for (; i < halfLength; i++, j--)
            {
                var value = array[i];
                array[i] = array[j];
                array[j] = value;
            }
        }

        public static NativeArray<T> Clone<T>(this NativeArray<T> array, Allocator allocator = Allocator.Temp)
            where T : unmanaged
        {
            return new NativeArray<T>(array, allocator);
        }

        public static NativeArray<T> Where<T, TPredicate>(this NativeArray<T> array, TPredicate predicate, Allocator allocator = Allocator.Temp)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            var output = new NativeArray<T>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);

            var c = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (predicate.Check(array[i]))
                {
                    output[c++] = array[i];
                }
            }

            return output.GetSubArray(0, c);
        }

        public static NativeArray<T> WhereNoAlloc<T, TPredicate>(this NativeArray<T> array, TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            var c = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (predicate.Check(array[i]))
                {
                    array[c++] = array[i];
                }
            }

            return array.GetSubArray(0, c);
        }

        public static NativeArray<TOutput> Select<TInput, TOutput, TSelector>(
            this NativeArray<TInput> array, TSelector selector, Allocator allocator = Allocator.Temp)
            where TInput : unmanaged
            where TOutput : unmanaged
            where TSelector : ISelector<TInput, TOutput>
        {
            var output = new NativeArray<TOutput>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < array.Length; i++)
            {
                output[i] = selector.Select(array[i]);
            }

            return output;
        }

        public static bool All<T, TPredicate>(this NativeArray<T> collection, TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            foreach (var value in collection)
            {
                if (!predicate.Check(value))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Any<T, TPredicate>(this in NativeArray<T> collection, in TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            foreach (var value in collection)
            {
                if (predicate.Check(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static T FirstOrDefault<T, TPredicate>(this NativeArray<T> collection, TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            for (var i = 0; i < collection.Length; i++)
            {
                if (predicate.Check(collection[i]))
                {
                    return collection[i];
                }
            }

            return default;
        }

        public static int IndexOf<T, TPredicate>(this NativeArray<T> collection, TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            for (var i = 0; i < collection.Length; i++)
            {
                if (predicate.Check(collection[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int Min(this NativeArray<int> collection)
        {
            ElementCheck(collection);

            var min = collection[0];

            for (var i = 1; i < collection.Length; i++)
            {
                min = math.min(min, collection[i]);
            }

            return min;
        }

        public static float Min(this NativeArray<float> collection)
        {
            ElementCheck(collection);

            var min = collection[0];

            for (var i = 1; i < collection.Length; i++)
            {
                min = math.min(min, collection[i]);
            }

            return min;
        }

        public static int Max(this NativeArray<int> collection)
        {
            ElementCheck(collection);

            var max = collection[0];

            for (var i = 1; i < collection.Length; i++)
            {
                max = math.max(max, collection[i]);
            }

            return max;
        }

        public static float Max(this NativeArray<float> collection)
        {
            ElementCheck(collection);

            var max = collection[0];

            for (var i = 1; i < collection.Length; i++)
            {
                max = math.max(max, collection[i]);
            }

            return max;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ElementCheck<T>(NativeArray<T> array)
            where T : unmanaged
        {
            if (array.Length == 0)
            {
                throw new InvalidOperationException("Array of length 0");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementReadAccess<T>(NativeArray<T> array, int index)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= array.Length)
            {
                FailOutOfRangeError(index, array.Length);
            }

            AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementReadAccess<T>(NativeArray<T>.ReadOnly array, int index)
            where T : unmanaged
        {
            // TODO we need a better approach here
            var a = array[index];
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementWriteAccess<T>(NativeArray<T> array, int index)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= array.Length)
            {
                FailOutOfRangeError(index, array.Length);
            }

            AtomicSafetyHandle.CheckWriteAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void FailOutOfRangeError(int index, int length)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
        }
    }
}
