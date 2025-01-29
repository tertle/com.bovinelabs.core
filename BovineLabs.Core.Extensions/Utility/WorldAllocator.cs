// <copyright file="WorldAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    public static class WorldAllocator
    {
        internal static readonly SharedStatic<NativeHashMap<ulong, AllocatorHelper<AutoFreeAllocator>>> Allocators =
            SharedStatic<NativeHashMap<ulong, AllocatorHelper<AutoFreeAllocator>>>.GetOrCreate<AllocatorData>();

        public static void Initialize()
        {
            if (Allocators.Data.IsCreated)
            {
                Debug.LogError("allocators weren't cleaned up");
                Dispose();
            }

            Allocators.Data = new NativeHashMap<ulong, AllocatorHelper<AutoFreeAllocator>>(1, Allocator.Persistent);

            DefaultWorldInitializationInternal.DefaultWorldDestroyed += Dispose;
        }

        public static void Dispose()
        {
            foreach (var d in Allocators.Data)
            {
                d.Value.Allocator.Dispose();
                d.Value.Dispose();
            }

            Allocators.Data.Dispose();
            Allocators.Data = default;

            DefaultWorldInitializationInternal.DefaultWorldDestroyed -= Dispose;
        }

        public static void CreateAllocator(ulong sequenceNumber)
        {
            var helper = new AllocatorHelper<AutoFreeAllocator>(Allocator.Persistent);
            helper.Allocator.Initialize(Allocator.Persistent);
            Allocators.Data[sequenceNumber] = helper;
        }

        public static void DisposeAllocator(ulong sequenceNumber)
        {
            Allocators.Data.Remove(sequenceNumber, out var allocator);
            allocator.Allocator.Dispose();
            allocator.Dispose();
        }

        private struct AllocatorData
        {
        }
    }

    public static class WorldAllocatorExtensions
    {
        public static AllocatorManager.AllocatorHandle WorldAllocator(this in WorldUnmanaged worldUnmanaged)
        {
            return GetHelper(worldUnmanaged).Allocator.Handle;
        }

        public static AllocatorManager.AllocatorHandle WorldAllocator(this SystemBase systemBase)
        {
            return WorldAllocator(systemBase.World.Unmanaged);
        }

        public static AllocatorManager.AllocatorHandle WorldAllocator(this ref SystemState systemState)
        {
            return WorldAllocator(systemState.WorldUnmanaged);
        }

        private static AllocatorHelper<AutoFreeAllocator> GetHelper(in WorldUnmanaged worldUnmanaged)
        {
            Assert.IsTrue(Core.WorldAllocator.Allocators.Data.IsCreated,
                "Allocators not setup. WorldAllocator must be used with BovineLabsBootstrap or initialized yourself");

            var result = Core.WorldAllocator.Allocators.Data.TryGetValue(worldUnmanaged.SequenceNumber, out var helper);
            Assert.IsTrue(result, "Allocator for world not setup. WorldAllocator must be used with BovineLabsBootstrap or initialized yourself");

            return helper;
        }
    }
}
