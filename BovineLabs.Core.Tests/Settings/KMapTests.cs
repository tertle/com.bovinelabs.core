// <copyright file="KMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Settings
{
    using System;
    using BovineLabs.Core.Variables;
    using NUnit.Framework;

    public class KMapTests
    {
        [Test]
        public void CapacityMax()
        {
            var kvp = new NameValue[KMap.MaxCapacity];

            for (byte i = 0; i < kvp.Length; i++)
            {
                kvp[i] = new NameValue
                {
                    Name = i.ToString(),
                    Value = new ByteReference(i),
                };
            }

            new KMap(kvp);
        }

        [Test]
        public void CapacityFail()
        {
            var kvp = new NameValue[KMap.MaxCapacity + 1];

            for (int i = 0; i < kvp.Length; i++)
            {
                kvp[i] = new NameValue
                {
                    Name = i.ToString(),
                    Value = new ByteReference((byte)i),
                };
            }

            Assert.Throws<ArgumentException>(() => new KMap(kvp));
        }

        [Test]
        public void MaxString()
        {
            var kvp = new NameValue[1];
            kvp[0] = new NameValue { Name = "abcdefghijklmno", Value = new ByteReference(0) }; // 15
            new KMap(kvp);
        }

        [Test]
        public void MaxStringInvalid()
        {
            const string testString = "abcdefghijklmnop";

            var kvp = new NameValue[1];

            kvp[0] = new NameValue { Name = testString, Value = new ByteReference(0) }; // 16
            Assert.Throws<ArgumentException>(() => new KMap(kvp));
        }
    }
}
