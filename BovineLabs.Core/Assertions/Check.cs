// <copyright file="Check.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Assertions
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Unity.Burst.CompilerServices;
    using Debug = UnityEngine.Debug;

    [DebuggerStepThrough]
    public static class Check
    {
        [AssertionMethod]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume(bool assumption)
        {
            IsTrue(assumption);
            Hint.Assume(assumption);
        }

        [AssertionMethod]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume(bool assumption, string message)
        {
            IsTrue(assumption, message);
            Hint.Assume(assumption);
        }

        [AssertionMethod]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void IsTrue(bool condition)
        {
            Debug.Assert(condition);
        }

        [AssertionMethod]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void IsTrue(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }
    }
}
