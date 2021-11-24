// <copyright file="NativeArrayExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public interface IPredicate<in T>
    {
        bool Check(T val);
    }

    public interface ISelector<in TInput, out TOutput>
    {
        TOutput Select(TInput val);
    }

    public static class NativeArrayExtensions
    {
        /// <summary> Efficiently sets all values in a NativeArray to a specific value. </summary>
        /// <param name="array"> The array to fill. </param>
        /// <param name="value"> The value that the array elements will be set to. </param>
        /// <typeparam name="T"> The unmanaged type the array holds. </typeparam>
        public static unsafe void SetAll<T>(this NativeArray<T> array, T value)
            where T : unmanaged
        {
            UnsafeUtility.MemCpyReplicate(array.GetUnsafeReadOnlyPtr(), &value, UnsafeUtility.SizeOf<T>(), array.Length);
        }

        /// <summary> Efficiently clears all values in a NativeArray. </summary>
        /// <param name="array"> The array to clear. </param>
        /// <typeparam name="T"> The unmanaged type the array holds. </typeparam>
        public static unsafe void Clear<T>(this NativeArray<T> array)
            where T : unmanaged
        {
            UnsafeUtility.MemClear(array.GetUnsafeReadOnlyPtr(), UnsafeUtility.SizeOf<T>() * array.Length);
        }

        public static NativeArray<T> Clone<T>(this NativeArray<T> array, Allocator allocator = Allocator.Temp)
            where T : struct
        {
            return new NativeArray<T>(array, allocator);
        }

        public static NativeArray<T> Where<T, TPredicate>(this NativeArray<T> array, TPredicate predicate, Allocator allocator = Allocator.Temp)
            where T : struct
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
            where T : struct
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

        public static NativeArray<TOutput> Select<TInput, TOutput, TSelector>(this NativeArray<TInput> array, TSelector selector, Allocator allocator = Allocator.Temp)
            where TInput : struct
            where TOutput : struct
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
            where T : struct
            where TPredicate : IPredicate<T>
        {
            for (var i = 0; i < collection.Length; i++)
            {
                if (!predicate.Check(collection[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Any<T, TPredicate>(this NativeArray<T> collection, TPredicate predicate)
            where T : struct
            where TPredicate : IPredicate<T>
        {
            for (var i = 0; i < collection.Length; i++)
            {
                if (predicate.Check(collection[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static T FirstOrDefault<T, TPredicate>(this NativeArray<T> collection, TPredicate predicate)
            where T : struct
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
            where T : struct
        {
            if (array.Length == 0)
            {
                throw new InvalidOperationException("Array of length 0");
            }
        }
    }
}