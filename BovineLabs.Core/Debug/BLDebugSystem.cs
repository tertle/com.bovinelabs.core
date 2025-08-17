// <copyright file="BLDebugSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define BL_DEBUG_UPDATE
#endif

namespace BovineLabs.Core
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    [Configurable]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial class BLDebugSystem : InitSystemBase
    {
        private const int DefaultMinLength = 0;

        [ConfigVar("debug.loglevel.min-world-length", DefaultMinLength, "The min length of the world name, useful for alignment.")]
        private static readonly SharedStatic<int> MinWorldLength = SharedStatic<int>.GetOrCreate<BLDebugSystem>();

        /// <inheritdoc />
        protected override void OnCreate()
        {
            Create(this.World);
        }

        internal static void Create(World world)
        {
            var netDebugEntity = world.EntityManager.CreateSingleton<BLLogger>();
            world.EntityManager.SetName(netDebugEntity, "DBDebug");

            var worldName = world.Name.TrimEnd("World").TrimEnd();

            var maxLength = FixedString32Bytes.UTF8MaxLengthInBytes;
            var minLength = math.min(MinWorldLength.Data, maxLength);

            // Apply size limits
            worldName = worldName.Length > maxLength ? worldName[..maxLength] : worldName;
            worldName = worldName.PadRight(minLength);

            world.EntityManager.SetComponentData(netDebugEntity, new BLLogger { World = worldName });
        }
    }
}
