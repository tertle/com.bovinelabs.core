// <copyright file="KMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Keys
{
    using System;
    using BovineLabs.Core.Keys;
    using NUnit.Framework;

    public class KMapTests
    {
        [Test]
        public void CapacityMax()
        {
            var kvp = new NameValue[KMap.MaxCapacity];

            for (var i = 0; i < kvp.Length; i++)
            {
                kvp[i] = new NameValue
                {
                    Name = i.ToString(),
                    Value = i,
                };
            }

            new KMap(kvp);
        }

        [Test]
        public void CapacityFail()
        {
            var kvp = new NameValue[KMap.MaxCapacity + 1];

            for (var i = 0; i < kvp.Length; i++)
            {
                kvp[i] = new NameValue
                {
                    Name = i.ToString(),
                    Value = (byte)i,
                };
            }

            Assert.Throws<ArgumentException>(() => new KMap(kvp));
        }

        [Test]
        public void MaxString()
        {
            var kvp = new NameValue[1];
            kvp[0] = new NameValue
            {
                Name = "abcdefghijklmno",
                Value = 0,
            }; // 15

            new KMap(kvp);
        }

        [Test]
        public void MaxStringInvalid()
        {
            const string testString = "abcdefghijklmnop";

            var kvp = new NameValue[1];

            kvp[0] = new NameValue
            {
                Name = testString,
                Value = 0,
            }; // 16

            Assert.Throws<ArgumentException>(() => new KMap(kvp));
        }
    }
}
