// <copyright file="Check.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Assertions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Unity.Burst.CompilerServices;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    [DebuggerStepThrough]
    public static class Check
    {
        [AssertionMethod]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume([AssertionCondition(AssertionConditionType.IS_TRUE)]bool assumption)
        {
            IsTrue(assumption);
            Hint.Assume(assumption);
        }

        [AssertionMethod]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume([AssertionCondition(AssertionConditionType.IS_TRUE)]bool assumption, string message)
        {
            IsTrue(assumption, message);
            Hint.Assume(assumption);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void IsTrue(bool condition)
        {
            if (condition)
            {
                return;
            }

            Debug.unityLogger.Log(LogType.Assert);
            throw new Exception();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void IsTrue(bool condition, string message)
        {
            if (condition)
            {
                return;
            }

            Debug.unityLogger.Log(LogType.Assert, message);
            throw new Exception(message);
        }
    }
}
