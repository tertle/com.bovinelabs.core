// <copyright file="WorldExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class WorldExtensionsTests : ECSTestsFixture
    {
        [Test]
        public void IsClientOnlyWorld_ReturnsExpectedResults()
        {
            var cases = new[]
            {
                (WorldFlags.GameClient, true),
                (WorldFlags.GameThinClient, true),
                (WorldFlags.GameServer, false),
                (WorldFlags.GameClient | WorldFlags.GameServer, false),
                (WorldFlags.Game, false),
            };

            foreach (var (flags, expected) in cases)
            {
                using var world = new World("Client Only World Test", flags);
                Assert.AreEqual(expected, world.IsClientOnlyWorld());
                Assert.AreEqual(expected, world.Unmanaged.IsClientOnlyWorld());
            }
        }
    }
}
