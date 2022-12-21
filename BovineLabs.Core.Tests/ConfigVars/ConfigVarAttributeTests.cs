// <copyright file="ConfigVarAttributeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.ConfigVars
{
    using System.Collections.Generic;
    using BovineLabs.Core.ConfigVars;
    using NUnit.Framework;
    using Assert = Unity.Assertions.Assert;

    /// <summary> Tests for <see cref="ConfigVarAttribute" />. </summary>
    public class ConfigVarAttributeTests
    {
        /// <summary> Tests implicit conversion of string to config var. </summary>
        [Test]
        public void StringEquals()
        {
            var attribute = new ConfigVarAttribute("a", 3, "abc");
            var attribute2 = (ConfigVarAttribute)"a";

            Assert.AreEqual(attribute, attribute2);
        }

        /// <summary> Tests hash is based off the <see cref="ConfigVarAttribute.Name" /> field only. </summary>
        [Test]
        public void StringHashEquals()
        {
            var attribute = new ConfigVarAttribute("a", 3, "abc");
            var hashSet = new HashSet<ConfigVarAttribute> { attribute };
            Assert.IsTrue(hashSet.Contains("a"));
        }
    }
}
