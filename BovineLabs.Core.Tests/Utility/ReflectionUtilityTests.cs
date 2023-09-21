// <copyright file="ReflectionUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;

    /// <summary> Tests for <see cref="ReflectionUtility" />. </summary>
    public class ReflectionUtilityTests
    {
        /// <summary>
        /// Tests for <see cref="ReflectionUtility.GetCustomImplementation{T}" /> and <see cref="ReflectionUtility.GetCustomImplementation{T, TD}" />.
        /// </summary>
        public class GetCustomImplementation
        {
            /// <summary> Tests needs interface. </summary>
            [Test]
            public void ExceptionIfNotInterface()
            {
                Assert.Throws<ArgumentException>(() => ReflectionUtility.GetCustomImplementation<TestImplementation2>());
            }

            /// <summary> Tests requires only 1 implementation. </summary>
            [Test]
            public void ExceptionMoreThanOneImplementation()
            {
                Assert.Throws<InvalidOperationException>(() => ReflectionUtility.GetCustomImplementation<ITestInterface2>());
            }

            /// <summary> Tests with no implementation. </summary>
            [Test]
            public void NoImplementationReturnsNull()
            {
                Assert.IsNull(ReflectionUtility.GetCustomImplementation<ITestInterface0>());
            }

            /// <summary> Tests returns expected implementation. </summary>
            [Test]
            public void ReturnsExpectedImplementation()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface1>()!;
                Assert.AreSame(typeof(TestImplementation2), result.GetType());
            }

            /// <summary> Tests returns expected implementation when ignoring. </summary>
            [Test]
            public void ReturnsExpectedImplementationWhenIgnoring()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface2, TestImplementation2>()!;
                Assert.AreSame(typeof(TestImplementation1), result.GetType());
            }

            /// <summary> Tests returns default implementation. </summary>
            [Test]
            public void NoImplementationReturnsDefaultImplementation()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface1, TestImplementation2>()!;
                Assert.AreEqual(typeof(TestImplementation2), result.GetType());
            }
        }
    }
}
