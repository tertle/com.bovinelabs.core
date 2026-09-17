namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;

    public class ReflectionUtilityTests
    {
        public class GetCustomImplementation
        {
            [Test]
            public void ExceptionIfNotInterface()
            {
                Assert.Throws<ArgumentException>(() => ReflectionUtility.GetCustomImplementation<TestImplementation2>());
            }

            [Test]
            public void ExceptionMoreThanOneImplementation()
            {
                Assert.Throws<InvalidOperationException>(() => ReflectionUtility.GetCustomImplementation<ITestInterface2>());
            }

            [Test]
            public void NoImplementationReturnsNull()
            {
                Assert.IsNull(ReflectionUtility.GetCustomImplementation<ITestInterface0>());
            }

            [Test]
            public void ReturnsExpectedImplementation()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface1>()!;
                Assert.AreSame(typeof(TestImplementation2), result.GetType());
            }

            [Test]
            public void ReturnsExpectedImplementationWhenIgnoring()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface2, TestImplementation2>()!;
                Assert.AreSame(typeof(TestImplementation1), result.GetType());
            }

            [Test]
            public void NoImplementationReturnsDefaultImplementation()
            {
                var result = ReflectionUtility.GetCustomImplementation<ITestInterface1, TestImplementation2>()!;
                Assert.AreEqual(typeof(TestImplementation2), result.GetType());
            }
        }
    }
}
