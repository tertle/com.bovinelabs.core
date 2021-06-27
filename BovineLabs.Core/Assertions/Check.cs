// <copyright file="Check.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Assertions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Debug = UnityEngine.Debug;

    [DebuggerStepThrough]
    public static class Check
    {
        public static void Assume(bool assumption)
        {
            IsTrue(assumption);
            Hint.Assume(assumption);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsTrue(bool condition)
        {
            Debug.Assert(condition);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsTrue(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsFalse(bool condition)
        {
            Debug.Assert(!condition);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void IsFalse(bool condition, string message)
        {
            Debug.Assert(!condition, message);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual<T>(T expected, T actual)
            where T : IEquatable<T>
        {
            Debug.Assert(expected.Equals(actual));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreEqual<T>(T expected, T actual, string message)
            where T : IEquatable<T>
        {
            Debug.Assert(expected.Equals(actual), message);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreNotEqual<T>(T expected, T actual)
        {
            Debug.Assert(!expected.Equals(actual));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AreNotEqual<T>(T expected, T actual, string message)
        {
            Debug.Assert(!expected.Equals(actual), message);
        }
    }
}
