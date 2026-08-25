// <copyright file="DynamicMultiHashMapNetCodeSerializerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.NetCode.LowLevel.Unsafe;

    public partial class DynamicMultiHashMapNetCodeSerializerTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;

        [Test]
        public unsafe void GeneratedSerializer_RoundTripsDuplicatesAndPreservesValueOrder()
        {
            var sourceBuffer = this.CreateMultiHashMapBuffer();
            var source = sourceBuffer.AsMultiHashMap<DynamicMultiHashMapTestsBuffer, int, byte>();
            FillOrderedDuplicateWorkload(ref source);

            var targetBuffer = this.RoundTripGenerated(sourceBuffer);
            var rebuilt = targetBuffer.AsMultiHashMap<DynamicMultiHashMapTestsBuffer, int, byte>();

            AssertOrderedDuplicateWorkload(rebuilt);
            AssertDenseRebuild(rebuilt.Helper, source.Count);
        }

        [Test]
        public unsafe void RawSerializer_RoundTripsDuplicateIdenticalPairs()
        {
            var sourceBuffer = this.CreateRawMultiHashMapBuffer();
            var source = sourceBuffer.AsMultiHashMap<DynamicMultiHashMapRawStableModeTestsBuffer, int, byte>();
            source.Add(5, 9);
            source.Add(5, 9);
            source.Add(5, 9);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            DynamicMultiHashMapNetCodeSerializer<DynamicMultiHashMapRawStableModeTestsBuffer, int, byte>.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var targetBuffer = this.CreateRawMultiHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
            };

            DynamicMultiHashMapNetCodeSerializer<DynamicMultiHashMapRawStableModeTestsBuffer, int, byte>.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsMultiHashMap<DynamicMultiHashMapRawStableModeTestsBuffer, int, byte>();
            AssertValues(rebuilt, 5, 9, 9, 9);
            AssertDenseRebuild(rebuilt.Helper, source.Count);
        }

        [Test]
        public unsafe void RawStructSerializer_RoundTripsStructKeyAndBLId()
        {
            var sourceBuffer = this.CreateRawStructMultiHashMapBuffer();
            var source = sourceBuffer.AsMultiHashMap<DynamicMultiHashMapRawStableObjectIdTestsBuffer, GeneratedStableKey, BLId>();
            var firstTarget = new GeneratedStableKey { Id = 5, Version = 1 };
            var secondTarget = new GeneratedStableKey { Id = 6, Version = 1 };
            source.Add(firstTarget, new BLId(7, 2));
            source.Add(firstTarget, new BLId(8, 2));
            source.Add(secondTarget, new BLId(9, 2));

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            DynamicMultiHashMapRawStableObjectIdTestsBufferDynamicMultiHashMapGhostSerializer.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var targetBuffer = this.CreateRawStructMultiHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
            };

            DynamicMultiHashMapRawStableObjectIdTestsBufferDynamicMultiHashMapGhostSerializer.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsMultiHashMap<DynamicMultiHashMapRawStableObjectIdTestsBuffer, GeneratedStableKey, BLId>();
            Assert.AreEqual(
                UnsafeUtility.SizeOf<GeneratedStableKey>(),
                DynamicMultiHashMapRawStableObjectIdTestsBufferDynamicMultiHashMapGhostSerializer.EncodedKeySize);
            Assert.AreEqual(UnsafeUtility.SizeOf<BLId>(), DynamicMultiHashMapRawStableObjectIdTestsBufferDynamicMultiHashMapGhostSerializer.EncodedValueSize);
            AssertValues(rebuilt, firstTarget, new BLId(8, 2), new BLId(7, 2));
            AssertValues(rebuilt, secondTarget, new BLId(9, 2));
            AssertDenseRebuild(rebuilt.Helper, source.Count);
        }

        [Test]
        public unsafe void GeneratedStructCodec_RoundTripsThroughEmittedSerializer()
        {
            var sourceBuffer = this.CreateGeneratedStructMultiHashMapBuffer();
            var source = sourceBuffer.AsMultiHashMap<DynamicMultiHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>();
            var key = new GeneratedPaddedKey { A = 1, B = 10, @event = 100 };
            var collidingKey = new GeneratedPaddedKey { A = 2, B = 10, @event = 99 };
            source.Add(key, CreateGeneratedMixedValue(1, true, GeneratedSmallEnum.One, 'a', 1.25f, 7));
            source.Add(collidingKey, CreateGeneratedMixedValue(2, false, GeneratedSmallEnum.Two, 'b', 2.5f, 8));
            source.Add(key, CreateGeneratedMixedValue(3, true, GeneratedSmallEnum.Two, 'c', 3.75f, 9));

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            DynamicMultiHashMapGeneratedStructTestsBufferDynamicMultiHashMapGhostSerializer.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var targetBuffer = this.CreateGeneratedStructMultiHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
            };

            DynamicMultiHashMapGeneratedStructTestsBufferDynamicMultiHashMapGhostSerializer.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsMultiHashMap<DynamicMultiHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>();
            AssertGeneratedValues(
                rebuilt,
                key,
                CreateGeneratedMixedValue(3, true, GeneratedSmallEnum.Two, 'c', 3.75f, 9),
                CreateGeneratedMixedValue(1, true, GeneratedSmallEnum.One, 'a', 1.25f, 7));
            AssertGeneratedValues(rebuilt, collidingKey, CreateGeneratedMixedValue(2, false, GeneratedSmallEnum.Two, 'b', 2.5f, 8));
            AssertDenseRebuild(rebuilt.Helper, source.Count);
        }

        [Test]
        public void GeneratedRegistration_CoversRootAndChildBufferSelection()
        {
            using var world = new World("DynamicMultiHashMap NetCode Collection Test", WorldFlags.GameServer);
            CreateGeneratedSerializerCollection(world);

            using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostComponentSerializerCollectionData>());
            var data = query.GetSingleton<GhostComponentSerializerCollectionData>();

            AssertSerializedStrategy<DynamicMultiHashMapTestsBuffer>(data, true);
            AssertSerializedStrategy<DynamicMultiHashMapTestsBuffer>(data, false);
            AssertSerializedStrategy<DynamicMultiHashMapRawStableModeTestsBuffer>(data, true);
            AssertSerializedStrategy<DynamicMultiHashMapRawStableObjectIdTestsBuffer>(data, true);
            AssertSerializedStrategy<DynamicMultiHashMapGeneratedStructTestsBuffer>(data, true);
        }

        [Test]
        public void SerializerState_HasDistinctMapAndMultiHashMapHashes()
        {
            using var world = new World("DynamicMultiHashMap State Test", WorldFlags.GameServer);
            CreateGeneratedSerializerCollection(world);
            var states = CreateSerializerStates(world);

            var mapState = FindSerializerState<DynamicHashMapTestsBuffer>(states);
            var multiState = FindSerializerState<DynamicMultiHashMapTestsBuffer>(states);

            Assert.AreEqual(ComponentType.ReadWrite<DynamicMultiHashMapTestsBuffer>(), multiState.ComponentType);
            Assert.AreEqual(1, multiState.ComponentSize);
            Assert.AreEqual(1, multiState.SnapshotSize);
            Assert.AreEqual(1, multiState.ChangeMaskBits);
            Assert.AreNotEqual(mapState.SerializerHash, multiState.SerializerHash);
            Assert.AreNotEqual(mapState.GhostFieldsHash, multiState.GhostFieldsHash);
            AssertFunctionPointersInitialized(mapState);
            AssertFunctionPointersInitialized(multiState);
#if UNITY_EDITOR || NETCODE_DEBUG
            Assert.AreEqual(
                DynamicGhostPrimitiveCodec.Hash64("BovineLabs.Core.Iterators.DynamicHashMapNetCodeGeneratedCompactVariant"),
                mapState.VariantTypeFullNameHash);
            Assert.AreEqual(
                DynamicGhostPrimitiveCodec.Hash64("BovineLabs.Core.Iterators.DynamicMultiHashMapNetCodeGeneratedCompactVariant"),
                multiState.VariantTypeFullNameHash);
#endif
        }

        private DynamicBuffer<DynamicMultiHashMapTestsBuffer> RoundTripGenerated(DynamicBuffer<DynamicMultiHashMapTestsBuffer> sourceBuffer)
        {
            unsafe
            {
                var sourceBytes = sourceBuffer.Reinterpret<byte>();
                var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

                DynamicMultiHashMapTestsBufferDynamicMultiHashMapGhostSerializer.CopyToSnapshot(
                    IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

                var targetBuffer = this.CreateMultiHashMapBuffer();
                var targetBytes = targetBuffer.Reinterpret<byte>();
                targetBytes.ResizeUninitialized(sourceBytes.Length);

                var dataAtTick = new SnapshotData.DataAtTick
                {
                    SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                    SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                };

                DynamicMultiHashMapTestsBufferDynamicMultiHashMapGhostSerializer.CopyFromSnapshot(
                    IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

                return targetBuffer;
            }
        }

        private DynamicBuffer<DynamicMultiHashMapTestsBuffer> CreateMultiHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapTestsBuffer));
            return this.Manager.GetBuffer<DynamicMultiHashMapTestsBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapTestsBuffer, int, byte>(0, MinGrowth);
        }

        private DynamicBuffer<DynamicMultiHashMapRawStableModeTestsBuffer> CreateRawMultiHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapRawStableModeTestsBuffer));
            return this.Manager.GetBuffer<DynamicMultiHashMapRawStableModeTestsBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapRawStableModeTestsBuffer, int, byte>(0, MinGrowth);
        }

        private DynamicBuffer<DynamicMultiHashMapRawStableObjectIdTestsBuffer> CreateRawStructMultiHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapRawStableObjectIdTestsBuffer));
            return this.Manager.GetBuffer<DynamicMultiHashMapRawStableObjectIdTestsBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapRawStableObjectIdTestsBuffer, GeneratedStableKey, BLId>(0, MinGrowth);
        }

        private DynamicBuffer<DynamicMultiHashMapGeneratedStructTestsBuffer> CreateGeneratedStructMultiHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapGeneratedStructTestsBuffer));
            return this.Manager.GetBuffer<DynamicMultiHashMapGeneratedStructTestsBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>(0, MinGrowth);
        }

        private static void FillOrderedDuplicateWorkload(ref DynamicMultiHashMap<int, byte> map)
        {
            map.Add(7, 1);
            map.Add(7, 2);
            map.Add(8, 20);
            map.Add(7, 3);

            Assert.IsTrue(map.TryGetFirstValue(7, out _, out var iterator));
            Assert.IsTrue(map.TryGetNextValue(out _, ref iterator));
            map.Remove(iterator);

            map.Add(7, 4);
            map.Add(8, 21);
        }

        private static void AssertOrderedDuplicateWorkload(DynamicMultiHashMap<int, byte> map)
        {
            AssertValues(map, 7, 4, 3, 1);
            AssertValues(map, 8, 21, 20);
        }

        private static unsafe void AssertDenseRebuild<TKey>(DynamicHashMapHelper<TKey>* helper, int expectedCount)
            where TKey : unmanaged, IEquatable<TKey>
        {
            Assert.AreEqual(expectedCount, helper->Count);
            Assert.AreEqual(expectedCount, helper->AllocatedIndex);
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.IsTrue(helper->IsDense);
        }

        private static void CreateGeneratedSerializerCollection(World world)
        {
            world.GetOrCreateSystemManaged<GhostComponentSerializerCollectionSystemGroup>();
            world.CreateSystem<DynamicMultiHashMapNetCodeGeneratedRegistrationSystem>();
            world.GetOrCreateSystemManaged<DefaultVariantSystemGroup>();
        }

        private static DynamicBuffer<GhostComponentSerializer.State> CreateSerializerStates(World world)
        {
            world.CreateSystem<NetDebugSystem>();
            var ghostCollectionSystem = world.CreateSystem<GhostCollectionSystem>();
            ghostCollectionSystem.Update(world.Unmanaged);

            using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostComponentSerializer.State>());
            return query.GetSingletonBuffer<GhostComponentSerializer.State>();
        }

        private static GhostComponentSerializer.State FindSerializerState<TBuffer>(DynamicBuffer<GhostComponentSerializer.State> states)
            where TBuffer : unmanaged, IBufferElementData
        {
            var componentType = ComponentType.ReadWrite<TBuffer>();
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i].ComponentType == componentType)
                {
                    return states[i];
                }
            }

            Assert.Fail($"No dynamic hash collection serializer state found for {typeof(TBuffer).Name}.");
            return default;
        }

        private static void AssertFunctionPointersInitialized(GhostComponentSerializer.State state)
        {
            AssertPointerInitialized(state.PostSerializeBuffer, nameof(state.PostSerializeBuffer));
            AssertPointerInitialized(state.SerializeBuffer, nameof(state.SerializeBuffer));
            AssertPointerInitialized(state.CopyFromSnapshot, nameof(state.CopyFromSnapshot));
            AssertPointerInitialized(state.CopyToSnapshot, nameof(state.CopyToSnapshot));
            AssertPointerInitialized(state.RestoreFromBackup, nameof(state.RestoreFromBackup));
            AssertPointerInitialized(state.PredictDelta, nameof(state.PredictDelta));
            AssertPointerInitialized(state.Deserialize, nameof(state.Deserialize));
#if UNITY_EDITOR || NETCODE_DEBUG
            AssertPointerInitialized(state.ReportPredictionErrors, nameof(state.ReportPredictionErrors));
#endif
        }

        private static void AssertPointerInitialized<TDelegate>(PortableFunctionPointer<TDelegate> pointer, string name)
            where TDelegate : Delegate
        {
            Assert.AreNotEqual(default(PortableFunctionPointer<TDelegate>), pointer, $"{name} was not initialized.");
        }

        private static void AssertSerializedStrategy<TBuffer>(GhostComponentSerializerCollectionData data, bool isRoot)
            where TBuffer : unmanaged, IBufferElementData
        {
            var componentType = ComponentType.ReadWrite<TBuffer>();
            var available = data.GetAllAvailableSerializationStrategiesForType(componentType, 0, isRoot);
            for (var i = 0; i < available.Length; i++)
            {
                var strategy = available[i];
                if (strategy.Component != componentType || strategy.IsSerialized == 0 || strategy.IsDontSerializeVariant)
                {
                    continue;
                }

                Assert.AreEqual(GhostPrefabType.All, strategy.PrefabType);
                Assert.AreEqual(GhostSendType.AllClients, strategy.SendTypeOptimization);
                Assert.AreEqual(1, strategy.SendForChildEntities);
                Assert.AreNotEqual(ComponentTypeSerializationStrategy.DefaultType.NotDefault, strategy.DefaultRule);
                return;
            }

            Assert.Fail($"No serialized DynamicMultiHashMap strategy found for {typeof(TBuffer).Name} with isRoot={isRoot}.");
        }

        private static void AssertValues(DynamicMultiHashMap<int, byte> map, int key, params byte[] expected)
        {
            var values = map.GetValuesForKey(key);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(values.MoveNext());
                Assert.AreEqual(expected[i], values.Current);
            }

            Assert.IsFalse(values.MoveNext());
        }

        private static void AssertValues(DynamicMultiHashMap<GeneratedStableKey, BLId> map, GeneratedStableKey key, params BLId[] expected)
        {
            var values = map.GetValuesForKey(key);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(values.MoveNext());
                Assert.AreEqual(expected[i], values.Current);
            }

            Assert.IsFalse(values.MoveNext());
        }

        private static void AssertGeneratedValues(
            DynamicMultiHashMap<GeneratedPaddedKey, GeneratedMixedValue> map, GeneratedPaddedKey key, params GeneratedMixedValue[] expected)
        {
            var values = map.GetValuesForKey(key);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(values.MoveNext());
                AssertGeneratedMixedValue(expected[i], values.Current);
            }

            Assert.IsFalse(values.MoveNext());
        }

        private static void AssertGeneratedMixedValue(GeneratedMixedValue expected, GeneratedMixedValue actual)
        {
            Assert.AreEqual(expected.Nested.Count, actual.Nested.Count);
            Assert.AreEqual(expected.Nested.Flag, actual.Nested.Flag);
            Assert.AreEqual(expected.Mode, actual.Mode);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Weight, actual.Weight);
            Assert.AreEqual(expected.@class, actual.@class);
        }

        private static GeneratedMixedValue CreateGeneratedMixedValue(
            ushort count, bool flag, GeneratedSmallEnum mode, char symbol, float weight, byte keywordClass)
        {
            return new GeneratedMixedValue
            {
                Nested = new GeneratedNestedValue
                {
                    Count = count,
                    Flag = flag,
                },
                Mode = mode,
                Symbol = symbol,
                Weight = weight,
                @class = keywordClass,
            };
        }

        private partial struct DynamicMultiHashMapNetCodeGeneratedRegistrationSystem : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
                using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<GhostComponentSerializerCollectionData>();
                using var query = state.EntityManager.CreateEntityQuery(builder);
                ref var data = ref query.GetSingletonRW<GhostComponentSerializerCollectionData>().ValueRW;

                DynamicHashMapTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicMultiHashMapTestsBufferDynamicMultiHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicMultiHashMapRawStableModeTestsBufferDynamicMultiHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicMultiHashMapRawStableObjectIdTestsBufferDynamicMultiHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicMultiHashMapGeneratedStructTestsBufferDynamicMultiHashMapGhostSerializer.AddToCollection(ref data, ref state);
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }

    }
}
#endif
