// <copyright file="StringExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Core
{
    using BovineLabs.Basics.Extensions;
    using NUnit.Framework;

    /// <summary> Tests for <see cref="StringExtensions"/>. </summary>
    public class StringExtensionsTests
    {
        /// <summary> Tests <see cref="ToSentence"/>. </summary>
        [Test]
        public void ToSentence()
        {
            const string s1 = "ThisIsATestString";
            const string s2 = "thisIsATestString";
            const string expected = "This Is A Test String";

            Assert.AreEqual(expected, s1.ToSentence());
            Assert.AreEqual(expected, s2.ToSentence());
        }

        /// <summary> Tests <see cref="FirstCharToUpper"/>. </summary>
        [Test]
        public void FirstCharToUpper()
        {
            const string s1 = "this is a test string";
            const string expected = "This is a test string";

            Assert.AreEqual(expected, s1.FirstCharToUpper());
        }
    }
}