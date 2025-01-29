// <copyright file="TestLeakDetectionAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Testing
{
    using System;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Attribute to test leaks during a Unit Test. </summary>
    /// <remarks> From Ribitta https://discord.com/channels/489222168727519232/1064581837055348857/1244548367623782452. </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestLeakDetectionAttribute : TestActionAttribute
    {
        public override ActionTargets Targets => ActionTargets.Test;

        /// <inheritdoc />
        public override void BeforeTest(ITest test)
        {
            if (test.IsSuite)
            {
                return;
            }

            UnsafeUtility.ForgiveLeaks();

            if (UnsafeUtility.GetLeakDetectionMode() == NativeLeakDetectionMode.Disabled)
            {
                UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.Enabled);
            }
        }

        /// <inheritdoc />
        public override void AfterTest(ITest test)
        {
            if (test.IsSuite)
            {
                return;
            }

            var leakCount = UnsafeUtility.CheckForLeaks();
            Assert.That(leakCount, Is.Zero, "Expected no leaks");
        }
    }
}
