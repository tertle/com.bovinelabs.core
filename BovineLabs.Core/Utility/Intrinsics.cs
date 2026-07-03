// <copyright file="Intrinsics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    public static class Intrinsics
    {
        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static int InterlockedAnd(ref int location, int value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedAnd(ref location, value);
#else
            var currentValue = System.Threading.Interlocked.Add(ref location, 0);

            while (true)
            {
                var updatedValue = currentValue & value;

                // If nothing would change by and'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
#endif
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static uint InterlockedAnd(ref uint location, uint value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedAnd(ref location, value);
#else
            ref int locationAsInt = ref UnsafeUtility.As<uint, int>(ref location);
            int valueAsInt = (int)value;

            return (uint)InterlockedAnd(ref locationAsInt, valueAsInt);

#endif
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static long InterlockedAnd(ref long location, long value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedAnd(ref location, value);
#else
            var currentValue = System.Threading.Interlocked.Read(ref location);

            while (true)
            {
                var updatedValue = currentValue & value;

                // If nothing would change by and'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
#endif
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static ulong InterlockedAnd(ref ulong location, ulong value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedAnd(ref location, value);
#else
            ref long locationAsInt = ref UnsafeUtility.As<ulong, long>(ref location);
            long valueAsInt = (long)value;

            return (ulong)InterlockedAnd(ref locationAsInt, valueAsInt);

#endif
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static int InterlockedOr(ref int location, int value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedOr(ref location, value);
#else
            var currentValue = System.Threading.Interlocked.Add(ref location, 0);

            while (true)
            {
                var updatedValue = currentValue | value;

                // If nothing would change by or'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
#endif
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static uint InterlockedOr(ref uint location, uint value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedOr(ref location, value);
#else
            ref int locationAsInt = ref UnsafeUtility.As<uint, int>(ref location);
            int valueAsInt = (int)value;

            return (uint)InterlockedOr(ref locationAsInt, valueAsInt);

#endif
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static long InterlockedOr(ref long location, long value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedOr(ref location, value);
#else
            var currentValue = System.Threading.Interlocked.Read(ref location);

            while (true)
            {
                var updatedValue = currentValue | value;

                // If nothing would change by or'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
#endif
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static ulong InterlockedOr(ref ulong location, ulong value)
        {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS && !UNITY_6000_0_OR_NEWER // currently broken in 6.6
            Unity.Burst.Intrinsics.Common.InterlockedOr(ref location, value);
#else
            ref long locationAsInt = ref UnsafeUtility.As<ulong, long>(ref location);
            long valueAsInt = (long)value;

            return (ulong)InterlockedOr(ref locationAsInt, valueAsInt);

#endif
        }
    }
}
