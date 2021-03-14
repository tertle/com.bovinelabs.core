// <copyright file="SharedSafety.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> This is based off ExportPhysicsWorld.SharedData but uses a custom <see cref="AtomicSafetyManager"/>. </summary>
    public unsafe struct SharedSafety : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        public AtomicSafetyManager* SafetyManager;

        public static SharedSafety Create()
        {
            var sharedData = default(SharedSafety);
            sharedData.SafetyManager = (AtomicSafetyManager*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<AtomicSafetyManager>(), 16, Allocator.Persistent);
            *sharedData.SafetyManager = AtomicSafetyManager.Create();

            return sharedData;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.SafetyManager->Dispose();
        }

        public void Sync()
        {
            this.SafetyManager->BumpTemporaryHandleVersions();
        }
    }
}