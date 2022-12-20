// <copyright file="StringExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;

    /// <summary> Tests for <see cref="StringExtensions" />. </summary>
    public class StringExtensionsTests
    {
        /// <summary> Tests <see cref="ToSentence" />. </summary>
        [TestCase("ThisIsATestString", "This Is A Test String")]
        [TestCase("thisIsATestString", "this Is A Test String")]
        public void ToSentence(string input, string expected)
        {
            Assert.AreEqual(expected, input.ToSentence());
        }

        /// <summary> Tests <see cref="FirstCharToUpper" />. </summary>
        [Test]
        public void FirstCharToUpper()
        {
            const string s1 = "this is a test string";
            const string expected = "This is a test string";

            Assert.AreEqual(expected, s1.FirstCharToUpper());
        }
    }
}
