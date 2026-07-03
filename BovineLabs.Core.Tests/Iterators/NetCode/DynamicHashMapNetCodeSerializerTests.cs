// <copyright file="DynamicHashMapNetCodeSerializerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.NetCode.LowLevel.Unsafe;

    public partial class DynamicHashMapNetCodeSerializerTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;
        private const int SnapshotOffset = 16;
        private const int SnapshotStride = 64;
        private const int MeasurementEntryCount = 1024;
        private const int MeasurementIterations = 64;

        [Test]
        public unsafe void CopyToSnapshotAndCopyFromSnapshot_CoversPrespawnAndPredictedSpawnSnapshotSetup()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillSparse(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
            };

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            AssertCompactRoundTrip(source, rebuilt);
        }

        [Test]
        public void GeneratedRegistration_CoversRootAndChildBufferSelection()
        {
            using var world = new World("DynamicHashMap NetCode Collection Test", WorldFlags.GameServer);
            CreateGeneratedSerializerCollection(world);

            using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostComponentSerializerCollectionData>());
            var data = query.GetSingleton<GhostComponentSerializerCollectionData>();

            AssertSerializedStrategy<DynamicHashMapTestsBuffer>(data, true, true, GhostPrefabType.All, GhostSendType.AllClients);
            AssertSerializedStrategy<DynamicHashMapTestsBuffer>(data, false, true, GhostPrefabType.All, GhostSendType.AllClients);
            AssertSerializedStrategy<DynamicHashMapRawStableModeTestsBuffer>(data, true, true, GhostPrefabType.All, GhostSendType.AllClients);
            AssertSerializedStrategy<DynamicHashMapRootOnlyTestsBuffer>(data, true, false, GhostPrefabType.All, GhostSendType.AllClients);
            AssertRootOnlyChildDefaultsToDontSerialize<DynamicHashMapRootOnlyTestsBuffer>(data);
            AssertSerializedStrategy<DynamicHashMapPredictedOwnerTestsBuffer>(
                data, true, true, GhostPrefabType.PredictedClient, GhostSendType.OnlyPredictedClients);
            AssertSerializedStrategy<DynamicHashMapInterpolatedNonOwnerTestsBuffer>(
                data, true, true, GhostPrefabType.InterpolatedClient, GhostSendType.OnlyInterpolatedClients);
        }

        [Test]
        public void SerializerState_CoversPredictedInterpolatedAndOwnerSendMasks()
        {
            DynamicHashMapPredictedOwnerStateCaptureSystem.State = default;
            using (var predictedWorld = new World("DynamicHashMap Predicted State Test", WorldFlags.GameServer))
            {
                predictedWorld.CreateSystem<DynamicHashMapPredictedOwnerStateCaptureSystem>();
            }

            var predicted = DynamicHashMapPredictedOwnerStateCaptureSystem.State;
            AssertStateBasics<DynamicHashMapPredictedOwnerTestsBuffer>(predicted);
            Assert.AreEqual(GhostPrefabType.PredictedClient, predicted.PrefabType);
            Assert.AreEqual(GhostSendType.OnlyPredictedClients, predicted.SendMask);
            Assert.AreEqual(SendToOwnerType.SendToOwner, predicted.SendToOwner);

            DynamicHashMapInterpolatedNonOwnerStateCaptureSystem.State = default;
            using (var interpolatedWorld = new World("DynamicHashMap Interpolated State Test", WorldFlags.GameServer))
            {
                interpolatedWorld.CreateSystem<DynamicHashMapInterpolatedNonOwnerStateCaptureSystem>();
            }

            var interpolated = DynamicHashMapInterpolatedNonOwnerStateCaptureSystem.State;
            AssertStateBasics<DynamicHashMapInterpolatedNonOwnerTestsBuffer>(interpolated);
            Assert.AreEqual(GhostPrefabType.InterpolatedClient, interpolated.PrefabType);
            Assert.AreEqual(GhostSendType.OnlyInterpolatedClients, interpolated.SendMask);
            Assert.AreEqual(SendToOwnerType.SendToNonOwner, interpolated.SendToOwner);
        }

        [Test]
        public unsafe void GeneratedFieldCodec_UsesFieldSizeInsteadOfUnmanagedSize()
        {
            Assert.AreEqual(
                sizeof(byte) + sizeof(int) + sizeof(ushort),
                DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.EncodedKeySize);
            Assert.Less(DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.EncodedKeySize, UnsafeUtility.SizeOf<GeneratedPaddedKey>());
            Assert.AreEqual(
                sizeof(ushort) + sizeof(byte) + sizeof(byte) + sizeof(char) + sizeof(float) + sizeof(byte),
                DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.EncodedValueSize);
            Assert.AreEqual(sizeof(byte) + sizeof(int), DynamicHashMapGeneratedPaddingTestsBufferDynamicHashMapGhostSerializer.EncodedValueSize);
            Assert.Less(DynamicHashMapGeneratedPaddingTestsBufferDynamicHashMapGhostSerializer.EncodedValueSize, UnsafeUtility.SizeOf<GeneratedPaddedValue>());
        }

        [Test]
        public unsafe void PrimitiveCodec_WritesFloatingPointAsLittleEndianBits()
        {
            var bytes = stackalloc byte[8];

            DynamicGhostPrimitiveCodec.WriteFloat32(bytes, 1.0f);
            Assert.AreEqual((byte)0x00, bytes[0]);
            Assert.AreEqual((byte)0x00, bytes[1]);
            Assert.AreEqual((byte)0x80, bytes[2]);
            Assert.AreEqual((byte)0x3f, bytes[3]);

            DynamicGhostPrimitiveCodec.WriteFloat64(bytes, 1.0);
            Assert.AreEqual((byte)0x00, bytes[0]);
            Assert.AreEqual((byte)0x00, bytes[1]);
            Assert.AreEqual((byte)0x00, bytes[2]);
            Assert.AreEqual((byte)0x00, bytes[3]);
            Assert.AreEqual((byte)0x00, bytes[4]);
            Assert.AreEqual((byte)0x00, bytes[5]);
            Assert.AreEqual((byte)0xf0, bytes[6]);
            Assert.AreEqual((byte)0x3f, bytes[7]);

            DynamicGhostPrimitiveCodec.WriteUInt32(bytes, 0x80000000U);
            Assert.AreEqual(0x80000000U, FloatBits(DynamicGhostPrimitiveCodec.ReadFloat32(bytes)));

            DynamicGhostPrimitiveCodec.WriteUInt32(bytes, 0x7fc12345U);
            Assert.AreEqual(0x7fc12345U, FloatBits(DynamicGhostPrimitiveCodec.ReadFloat32(bytes)));

            DynamicGhostPrimitiveCodec.WriteUInt64(bytes, 0x400921fb54442d18UL);
            Assert.AreEqual(0x400921fb54442d18UL, DoubleBits(DynamicGhostPrimitiveCodec.ReadFloat64(bytes)));
        }

        [Test]
        public unsafe void GeneratedStructCodec_RoundTripsSparseMap()
        {
            var sourceBuffer = this.CreateGeneratedStructHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>();
            var removedKey = new GeneratedPaddedKey { A = 2, B = 20, @event = 200 };
            source.Add(new GeneratedPaddedKey { A = 1, B = 10, @event = 100 }, CreateGeneratedMixedValue(5, true, GeneratedSmallEnum.One, 'a', 1.25f, 9));
            source.Add(removedKey, CreateGeneratedMixedValue(6, false, GeneratedSmallEnum.Two, 'b', 2.5f, 8));
            source.Add(new GeneratedPaddedKey { A = 3, B = 30, @event = 300 }, CreateGeneratedMixedValue(7, true, GeneratedSmallEnum.Two, 'c', 3.75f, 7));
            Assert.IsTrue(source.Remove(removedKey));

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var targetBuffer = this.CreateGeneratedStructHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)snapshot.GetUnsafeReadOnlyPtr(),
            };

            DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>();
            Assert.AreEqual(source.Count, rebuilt.Count);
            Assert.IsTrue(rebuilt.Helper->IsDense);
            AssertGeneratedMixedValue(
                rebuilt, new GeneratedPaddedKey { A = 1, B = 10, @event = 100 }, CreateGeneratedMixedValue(5, true, GeneratedSmallEnum.One, 'a', 1.25f, 9));
            AssertGeneratedMixedValue(
                rebuilt, new GeneratedPaddedKey { A = 3, B = 30, @event = 300 }, CreateGeneratedMixedValue(7, true, GeneratedSmallEnum.Two, 'c', 3.75f, 7));
            Assert.IsFalse(rebuilt.TryGetValue(removedKey, out _));
        }

        [Test]
        public unsafe void GeneratedFieldCodec_IgnoresPaddingBytes()
        {
            var first = this.CreateGeneratedPaddingHashMapBuffer();
            var firstMap = first.AsHashMap<DynamicHashMapGeneratedPaddingTestsBuffer, int, GeneratedPaddedValue>();
            firstMap.Add(7, new GeneratedPaddedValue { A = 11, B = 123456 });
            firstMap.Helper->Values[1] = 0x11;
            firstMap.Helper->Values[2] = 0x22;
            firstMap.Helper->Values[3] = 0x33;

            var second = this.CreateGeneratedPaddingHashMapBuffer();
            var secondMap = second.AsHashMap<DynamicHashMapGeneratedPaddingTestsBuffer, int, GeneratedPaddedValue>();
            secondMap.Add(7, new GeneratedPaddedValue { A = 11, B = 123456 });
            secondMap.Helper->Values[1] = 0xaa;
            secondMap.Helper->Values[2] = 0xbb;
            secondMap.Helper->Values[3] = 0xcc;

            var firstBytes = first.Reinterpret<byte>();
            var secondBytes = second.Reinterpret<byte>();
            var firstPayload = new NativeArray<byte>(firstBytes.Length, Allocator.Temp);
            var secondPayload = new NativeArray<byte>(secondBytes.Length, Allocator.Temp);

            DynamicHashMapGeneratedPaddingTestsBufferDynamicHashMapGhostSerializer.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)firstPayload.GetUnsafePtr(), 0, 1, (IntPtr)firstBytes.GetPtr(), 1, firstBytes.Length);
            DynamicHashMapGeneratedPaddingTestsBufferDynamicHashMapGhostSerializer.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)secondPayload.GetUnsafePtr(), 0, 1, (IntPtr)secondBytes.GetPtr(), 1, secondBytes.Length);

            var firstHeader = DynamicHashMapCompactHeader.Read((byte*)firstPayload.GetUnsafeReadOnlyPtr());
            var secondHeader = DynamicHashMapCompactHeader.Read((byte*)secondPayload.GetUnsafeReadOnlyPtr());
            Assert.IsTrue(firstHeader.IsCurrentFormat);
            Assert.IsTrue(secondHeader.IsCurrentFormat);
            var firstPayloadBytes = (int)firstHeader.PayloadBytes;
            var secondPayloadBytes = (int)secondHeader.PayloadBytes;
            Assert.AreEqual(firstPayloadBytes, secondPayloadBytes);

            for (var i = 0; i < firstPayloadBytes; i++)
            {
                Assert.AreEqual(firstPayload[i], secondPayload[i], $"Payload byte {i} should ignore source struct padding.");
            }
        }

        [Test]
        public unsafe void Deserialize_ConsumesOnlyCompactPayloadBytes()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillSparse(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            Assert.IsTrue(DynamicHashMapNetCodeRawCodec<int, byte>.TryGetPayloadBytes(
                (byte*)snapshot.GetUnsafeReadOnlyPtr(), snapshot.Length, out var payloadBytes));

            var writer = new DataStreamWriter(payloadBytes, Allocator.Temp);
            DynamicHashMapNetCodeRawCodec<int, byte>.WritePayload((byte*)snapshot.GetUnsafeReadOnlyPtr(), payloadBytes, ref writer);

            var reader = new DataStreamReader(writer.AsNativeArray());
            var compressionModel = StreamCompressionModel.Default;
            var deserialized = new NativeArray<byte>(sourceBytes.Length, Allocator.Temp);

            for (var i = 0; i < sourceBytes.Length; i++)
            {
                DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.Deserialize(
                    (IntPtr)((byte*)deserialized.GetUnsafePtr() + i), IntPtr.Zero, ref reader, ref compressionModel, IntPtr.Zero, i);
            }

            Assert.AreEqual(payloadBytes * 8, reader.GetBitsRead());

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(sourceBytes.Length);

            var dataAtTick = new SnapshotData.DataAtTick
            {
                SnapshotBefore = (IntPtr)deserialized.GetUnsafeReadOnlyPtr(),
                SnapshotAfter = (IntPtr)deserialized.GetUnsafeReadOnlyPtr(),
            };

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.CopyFromSnapshot(
                IntPtr.Zero, (IntPtr)(&dataAtTick), 0, 1, (IntPtr)targetBytes.GetPtr(), 1, targetBytes.Length);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            AssertCompactRoundTrip(source, rebuilt);
        }

        [Test]
        public unsafe void PostSerializeBuffer_MatchesDirectSerializationForPreSerializedGhosts()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillSparse(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var dynamicSize = DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicSnapshotSize(1, sourceBytes.Length);
            var maskSize = DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicDataChangeMaskSize(1, sourceBytes.Length);
            var componentData = new NativeArray<IntPtr>(1, Allocator.Temp);
            var componentLengths = new NativeArray<int>(1, Allocator.Temp);
            var directSnapshot = new NativeArray<byte>(SnapshotStride, Allocator.Temp);
            var directDynamic = new NativeArray<byte>(dynamicSize, Allocator.Temp);
            var directEntityStartBits = new NativeArray<int>(2, Allocator.Temp);
            var directDynamicSizePerEntity = new NativeArray<int>(1, Allocator.Temp);
            var directWriterBuffer = new NativeArray<byte>(4096, Allocator.Temp);
            var directCompressionModel = StreamCompressionModel.Default;
            var directDynamicOffset = 0;

            componentData[0] = (IntPtr)sourceBytes.GetPtr();
            componentLengths[0] = sourceBytes.Length;

            var directWriter = new DataStreamWriter(directWriterBuffer);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.SerializeBuffer(
                IntPtr.Zero, (IntPtr)directSnapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1,
                (IntPtr)componentData.GetUnsafePtr(), (IntPtr)componentLengths.GetUnsafePtr(), 1, IntPtr.Zero, ref directWriter,
                ref directCompressionModel, (IntPtr)directEntityStartBits.GetUnsafePtr(), (IntPtr)directDynamic.GetUnsafePtr(), ref directDynamicOffset,
                (IntPtr)directDynamicSizePerEntity.GetUnsafePtr(), directDynamic.Length);

            var preSerializedSnapshot = new NativeArray<byte>(SnapshotStride, Allocator.Temp);
            var preSerializedDynamic = new NativeArray<byte>(dynamicSize, Allocator.Temp);
            var preSerializedEntityStartBits = new NativeArray<int>(2, Allocator.Temp);
            var preSerializedDynamicSizePerEntity = new NativeArray<int>(1, Allocator.Temp);
            var preSerializedWriterBuffer = new NativeArray<byte>(4096, Allocator.Temp);
            var preSerializedCompressionModel = StreamCompressionModel.Default;

            GhostComponentSerializer.TypeCast<uint>((IntPtr)preSerializedSnapshot.GetUnsafePtr(), SnapshotOffset) = (uint)sourceBytes.Length;
            GhostComponentSerializer.TypeCast<uint>((IntPtr)preSerializedSnapshot.GetUnsafePtr(), SnapshotOffset + sizeof(int)) = 0;
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.CopyToSnapshot(
                IntPtr.Zero, (IntPtr)((byte*)preSerializedDynamic.GetUnsafePtr() + maskSize), 0, 1, (IntPtr)sourceBytes.GetPtr(), 1, sourceBytes.Length);

            var preSerializedWriter = new DataStreamWriter(preSerializedWriterBuffer);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.PostSerializeBuffer(
                (IntPtr)preSerializedSnapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1, 1, IntPtr.Zero, ref preSerializedWriter,
                ref preSerializedCompressionModel, (IntPtr)preSerializedEntityStartBits.GetUnsafePtr(), (IntPtr)preSerializedDynamic.GetUnsafePtr(),
                (IntPtr)preSerializedDynamicSizePerEntity.GetUnsafePtr(), preSerializedDynamic.Length);

            Assert.AreEqual(directWriter.LengthInBits, preSerializedWriter.LengthInBits);
            Assert.AreEqual(directDynamicOffset, preSerializedDynamicSizePerEntity[0]);
            Assert.AreEqual(
                GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)directSnapshot.GetUnsafePtr() + sizeof(int)), 0, 2),
                GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)preSerializedSnapshot.GetUnsafePtr() + sizeof(int)), 0, 2));

            var writtenBytes = (directWriter.LengthInBits + 7) / 8;
            for (var i = 0; i < writtenBytes; i++)
            {
                Assert.AreEqual(directWriterBuffer[i], preSerializedWriterBuffer[i], $"Pre-serialized byte {i} differs from direct serialization.");
            }
        }

        [Test]
        public unsafe void SerializeBuffer_WithoutBaselinesWritesFullPayloadForLateJoinAndBaselineLoss()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillSparse(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var snapshot = new NativeArray<byte>(SnapshotStride, Allocator.Temp);
            var dynamicData = new NativeArray<byte>(
                DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicSnapshotSize(1, sourceBytes.Length), Allocator.Temp);
            var componentData = new NativeArray<IntPtr>(1, Allocator.Temp);
            var componentLengths = new NativeArray<int>(1, Allocator.Temp);
            var entityStartBits = new NativeArray<int>(2, Allocator.Temp);
            var dynamicSizePerEntity = new NativeArray<int>(1, Allocator.Temp);
            var writerBuffer = new NativeArray<byte>(4096, Allocator.Temp);
            var compressionModel = StreamCompressionModel.Default;
            var dynamicOffset = 0;

            componentData[0] = (IntPtr)sourceBytes.GetPtr();
            componentLengths[0] = sourceBytes.Length;

            var writer = new DataStreamWriter(writerBuffer);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.SerializeBuffer(
                IntPtr.Zero, (IntPtr)snapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1,
                (IntPtr)componentData.GetUnsafePtr(), (IntPtr)componentLengths.GetUnsafePtr(), 1, IntPtr.Zero, ref writer, ref compressionModel,
                (IntPtr)entityStartBits.GetUnsafePtr(), (IntPtr)dynamicData.GetUnsafePtr(), ref dynamicOffset,
                (IntPtr)dynamicSizePerEntity.GetUnsafePtr(), dynamicData.Length);

            Assert.AreEqual(3u, GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)snapshot.GetUnsafePtr() + sizeof(int)), 0, 2));
            Assert.Greater(writer.LengthInBits, 0);
        }

        [Test]
        public unsafe void RestoreFromBackup_RestoresSinglePhysicalByteForPredictedRollback()
        {
            var componentData = new NativeArray<byte>(3, Allocator.Temp);
            var backupData = new NativeArray<byte>(3, Allocator.Temp);
            componentData[0] = 10;
            componentData[1] = 20;
            componentData[2] = 30;
            backupData[0] = 90;
            backupData[1] = 91;
            backupData[2] = 92;

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.RestoreFromBackup(
                (IntPtr)((byte*)componentData.GetUnsafePtr() + 1), (IntPtr)((byte*)backupData.GetUnsafePtr() + 1));

            Assert.AreEqual(10, componentData[0]);
            Assert.AreEqual(91, componentData[1]);
            Assert.AreEqual(30, componentData[2]);

            var predictor = default(GhostDeltaPredictor);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.PredictDelta(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref predictor);
        }

        [Test]
        public unsafe void SerializeBuffer_SuppressesUnchangedAndForcesFullChangedUpdates()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillSparse(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var baselineSnapshot = new NativeArray<byte>(SnapshotStride, Allocator.Temp);
            var baselineDynamic = new NativeArray<byte>(
                DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicSnapshotSize(1, sourceBytes.Length), Allocator.Temp);
            var componentData = new NativeArray<IntPtr>(1, Allocator.Temp);
            var componentLengths = new NativeArray<int>(1, Allocator.Temp);
            var baselines = new NativeArray<IntPtr>(4, Allocator.Temp);
            var entityStartBits = new NativeArray<int>(2, Allocator.Temp);
            var dynamicSizePerEntity = new NativeArray<int>(1, Allocator.Temp);
            var writerBuffer = new NativeArray<byte>(4096, Allocator.Temp);
            var compressionModel = StreamCompressionModel.Default;
            var dynamicOffset = 0;

            componentData[0] = (IntPtr)sourceBytes.GetPtr();
            componentLengths[0] = sourceBytes.Length;

            var writer = new DataStreamWriter(writerBuffer);
            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.SerializeBuffer(
                IntPtr.Zero, (IntPtr)baselineSnapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1,
                (IntPtr)componentData.GetUnsafePtr(), (IntPtr)componentLengths.GetUnsafePtr(), 1, (IntPtr)baselines.GetUnsafePtr(), ref writer,
                ref compressionModel, (IntPtr)entityStartBits.GetUnsafePtr(), (IntPtr)baselineDynamic.GetUnsafePtr(), ref dynamicOffset,
                (IntPtr)dynamicSizePerEntity.GetUnsafePtr(), baselineDynamic.Length);

            Assert.AreEqual(3u, GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)baselineSnapshot.GetUnsafePtr() + sizeof(int)), 0, 2));

            baselines[0] = (IntPtr)baselineSnapshot.GetUnsafePtr();
            baselines[3] = (IntPtr)baselineDynamic.GetUnsafePtr();

            var unchangedSnapshot = new NativeArray<byte>(SnapshotStride, Allocator.Temp);
            var unchangedDynamic = new NativeArray<byte>(baselineDynamic.Length, Allocator.Temp);
            dynamicSizePerEntity[0] = 0;
            dynamicOffset = 0;
            writer = new DataStreamWriter(writerBuffer);

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.SerializeBuffer(
                IntPtr.Zero, (IntPtr)unchangedSnapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1,
                (IntPtr)componentData.GetUnsafePtr(), (IntPtr)componentLengths.GetUnsafePtr(), 1, (IntPtr)baselines.GetUnsafePtr(), ref writer,
                ref compressionModel, (IntPtr)entityStartBits.GetUnsafePtr(), (IntPtr)unchangedDynamic.GetUnsafePtr(), ref dynamicOffset,
                (IntPtr)dynamicSizePerEntity.GetUnsafePtr(), unchangedDynamic.Length);

            Assert.AreEqual(0u, GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)unchangedSnapshot.GetUnsafePtr() + sizeof(int)), 0, 2));
            Assert.AreEqual(0, writer.LengthInBits);

            source[7] = 220;
            dynamicSizePerEntity[0] = 0;
            dynamicOffset = 0;
            writer = new DataStreamWriter(writerBuffer);

            DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.SerializeBuffer(
                IntPtr.Zero, (IntPtr)unchangedSnapshot.GetUnsafePtr(), SnapshotOffset, SnapshotStride, 0, 1,
                (IntPtr)componentData.GetUnsafePtr(), (IntPtr)componentLengths.GetUnsafePtr(), 1, (IntPtr)baselines.GetUnsafePtr(), ref writer,
                ref compressionModel, (IntPtr)entityStartBits.GetUnsafePtr(), (IntPtr)unchangedDynamic.GetUnsafePtr(), ref dynamicOffset,
                (IntPtr)dynamicSizePerEntity.GetUnsafePtr(), unchangedDynamic.Length);

            Assert.AreEqual(3u, GhostComponentSerializer.CopyFromChangeMask((IntPtr)((byte*)unchangedSnapshot.GetUnsafePtr() + sizeof(int)), 0, 2));
            Assert.Greater(writer.LengthInBits, 0);
        }

        [Test]
        public unsafe void StageA_Measurements_CompareWireSnapshotCpuAndAllocations()
        {
            var sourceBuffer = this.CreateHashMapBuffer();
            var source = sourceBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            FillMeasurementWorkload(ref source);

            var sourceBytes = sourceBuffer.Reinterpret<byte>();
            var physicalReplicationBytes = sourceBytes.Length;
            var compactPayload = new NativeArray<byte>(physicalReplicationBytes, Allocator.Temp);

            Assert.IsTrue(DynamicHashMapNetCodeRawCodec<int, byte>.TryPack(
                source.Helper, (byte*)compactPayload.GetUnsafePtr(), compactPayload.Length, out var header));

            var compactWireBytes = (int)header.PayloadBytes;
            var dynamicMaskBytes = DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicDataChangeMaskSize(1, physicalReplicationBytes);
            var snapshotHistoryBytes = DynamicHashMapNetCodeRawCodec<int, byte>.GetDynamicSnapshotSize(1, physicalReplicationBytes);
            var expectedSnapshotHistoryBytes = GhostComponentSerializer.SnapshotSizeAligned(dynamicMaskBytes + physicalReplicationBytes);

            Assert.Less(compactWireBytes, physicalReplicationBytes);
            Assert.AreEqual(expectedSnapshotHistoryBytes, snapshotHistoryBytes);
            Assert.Greater(snapshotHistoryBytes, compactWireBytes);

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(physicalReplicationBytes);

            var deserialized = new NativeArray<byte>(physicalReplicationBytes, Allocator.Temp);
            var writerBuffer = new NativeArray<byte>(compactWireBytes, Allocator.Temp);

            MeasureStageATicks(
                source.Helper,
                compactPayload,
                deserialized,
                targetBytes,
                writerBuffer,
                MeasurementIterations,
                out var packTicks,
                out var deserializeTicks,
                out var rebuildTicks);

            Assert.Greater(packTicks, 0);
            Assert.Greater(deserializeTicks, 0);
            Assert.Greater(rebuildTicks, 0);

            var allocatedBytes = MeasureStageAAllocatedBytes(
                source.Helper,
                compactPayload,
                deserialized,
                targetBytes,
                writerBuffer,
                MeasurementIterations);

            Assert.AreEqual(0, allocatedBytes);

            TestContext.WriteLine(
                "DynamicHashMap Stage A compact payload: {0} bytes; physical bytes: {1}; saving: {2:P1}.",
                compactWireBytes,
                physicalReplicationBytes,
                1.0 - ((double)compactWireBytes / physicalReplicationBytes));
            TestContext.WriteLine(
                "DynamicHashMap Stage A snapshot history: {0} bytes ({1} mask + {2} payload scratch, aligned).",
                snapshotHistoryBytes,
                dynamicMaskBytes,
                physicalReplicationBytes);
            TestContext.WriteLine(
                "DynamicHashMap Stage A CPU per iteration: pack {0:F3} us; deserialize chunks {1:F3} us; rebuild {2:F3} us.",
                TicksToMicroseconds(packTicks, MeasurementIterations),
                TicksToMicroseconds(deserializeTicks, MeasurementIterations),
                TicksToMicroseconds(rebuildTicks, MeasurementIterations));
            TestContext.WriteLine("DynamicHashMap Stage A steady-state managed allocations: {0} bytes.", allocatedBytes);
        }

        private DynamicBuffer<DynamicHashMapTestsBuffer> CreateHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicHashMapTestsBuffer));
            return this.Manager.GetBuffer<DynamicHashMapTestsBuffer>(entity).InitializeHashMap<DynamicHashMapTestsBuffer, int, byte>(0, MinGrowth);
        }

        private DynamicBuffer<DynamicHashMapGeneratedStructTestsBuffer> CreateGeneratedStructHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicHashMapGeneratedStructTestsBuffer));
            return this.Manager.GetBuffer<DynamicHashMapGeneratedStructTestsBuffer>(entity)
                .InitializeHashMap<DynamicHashMapGeneratedStructTestsBuffer, GeneratedPaddedKey, GeneratedMixedValue>(0, MinGrowth);
        }

        private DynamicBuffer<DynamicHashMapGeneratedPaddingTestsBuffer> CreateGeneratedPaddingHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicHashMapGeneratedPaddingTestsBuffer));
            return this.Manager.GetBuffer<DynamicHashMapGeneratedPaddingTestsBuffer>(entity)
                .InitializeHashMap<DynamicHashMapGeneratedPaddingTestsBuffer, int, GeneratedPaddedValue>(0, MinGrowth);
        }

        private static void CreateGeneratedSerializerCollection(World world)
        {
            world.GetOrCreateSystemManaged<GhostComponentSerializerCollectionSystemGroup>();
            world.CreateSystem<DynamicHashMapNetCodeGeneratedRegistrationSystem>();
            world.GetOrCreateSystemManaged<DefaultVariantSystemGroup>();
        }

        private static void AssertStateBasics<TBuffer>(GhostComponentSerializer.State state)
            where TBuffer : unmanaged, IDynamicHashMap<int, byte>
        {
            Assert.AreEqual(ComponentType.ReadWrite<TBuffer>(), state.ComponentType);
            Assert.AreEqual(1, state.ComponentSize);
            Assert.AreEqual(1, state.SnapshotSize);
            Assert.AreEqual(1, state.ChangeMaskBits);
            Assert.IsTrue(state.HasGhostFields);
            Assert.AreNotEqual(0, state.SerializerHash);
            Assert.AreNotEqual(0, state.GhostFieldsHash);
            Assert.AreNotEqual(0, state.VariantHash);
        }

        private static void AssertSerializedStrategy<TBuffer>(
            GhostComponentSerializerCollectionData data, bool isRoot, bool sendsForChildEntities, GhostPrefabType prefabType, GhostSendType sendType)
            where TBuffer : unmanaged, IDynamicHashMap<int, byte>
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

                Assert.AreEqual(prefabType, strategy.PrefabType);
                Assert.AreEqual(sendType, strategy.SendTypeOptimization);
                Assert.AreEqual(sendsForChildEntities ? 1 : 0, strategy.SendForChildEntities);
                Assert.AreNotEqual(ComponentTypeSerializationStrategy.DefaultType.NotDefault, strategy.DefaultRule);
                return;
            }

            Assert.Fail($"No serialized DynamicHashMap strategy found for {typeof(TBuffer).Name} with isRoot={isRoot}.");
        }

        private static void AssertRootOnlyChildDefaultsToDontSerialize<TBuffer>(GhostComponentSerializerCollectionData data)
            where TBuffer : unmanaged, IDynamicHashMap<int, byte>
        {
            var componentType = ComponentType.ReadWrite<TBuffer>();
            var available = data.GetAllAvailableSerializationStrategiesForType(componentType, 0, false);
            var hasDontSerializeDefault = false;

            for (var i = 0; i < available.Length; i++)
            {
                var strategy = available[i];
                if (strategy.Component != componentType)
                {
                    continue;
                }

                if (strategy.IsSerialized != 0)
                {
                    Assert.AreEqual(0, strategy.SendForChildEntities);
                    continue;
                }

                hasDontSerializeDefault |= strategy.IsDontSerializeVariant &&
                    strategy.DefaultRule != ComponentTypeSerializationStrategy.DefaultType.NotDefault;
            }

            Assert.IsTrue(hasDontSerializeDefault);
        }

        private static void FillSparse(ref DynamicHashMap<int, byte> map)
        {
            for (var i = 0; i < 96; i++)
            {
                map.Add(i, (byte)(i + 7));
            }

            for (var i = 0; i < 96; i += 4)
            {
                Assert.IsTrue(map.Remove(i));
            }
        }

        private static void FillMeasurementWorkload(ref DynamicHashMap<int, byte> map)
        {
            for (var i = 0; i < MeasurementEntryCount; i++)
            {
                map.Add(i, (byte)(i % byte.MaxValue));
            }

            for (var i = 0; i < MeasurementEntryCount; i++)
            {
                if ((i & 3) != 0)
                {
                    Assert.IsTrue(map.Remove(i));
                }
            }
        }

        private static unsafe void MeasureStageATicks(
            DynamicHashMapHelper<int>* source,
            NativeArray<byte> compactPayload,
            NativeArray<byte> deserialized,
            DynamicBuffer<byte> targetBytes,
            NativeArray<byte> writerBuffer,
            int iterations,
            out long packTicks,
            out long deserializeTicks,
            out long rebuildTicks)
        {
            Assert.IsTrue(RunStageASteadyState(source, compactPayload, deserialized, targetBytes, writerBuffer));

            var compactPayloadPtr = (byte*)compactPayload.GetUnsafePtr();
            var deserializedPtr = (byte*)deserialized.GetUnsafePtr();
            var targetPtr = targetBytes.GetPtr();
            Assert.IsTrue(DynamicHashMapNetCodeRawCodec<int, byte>.TryGetPayloadBytes(compactPayloadPtr, compactPayload.Length, out var payloadBytes));

            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < iterations; i++)
            {
                if (!DynamicHashMapNetCodeRawCodec<int, byte>.TryPack(source, compactPayloadPtr, compactPayload.Length, out _))
                {
                    Assert.Fail("DynamicHashMap Stage A pack measurement failed.");
                }
            }

            packTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;

            var writer = new DataStreamWriter(writerBuffer);
            DynamicHashMapNetCodeRawCodec<int, byte>.WritePayload(compactPayloadPtr, payloadBytes, ref writer);
            var serialized = writer.AsNativeArray();
            var compressionModel = StreamCompressionModel.Default;

            start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var reader = new DataStreamReader(serialized);
                for (var offset = 0; offset < deserialized.Length; offset++)
                {
                    DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.Deserialize(
                        (IntPtr)(deserializedPtr + offset), IntPtr.Zero, ref reader, ref compressionModel, IntPtr.Zero, offset);
                }
            }

            deserializeTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;

            start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < iterations; i++)
            {
                if (!DynamicHashMapNetCodeRawCodec<int, byte>.TryRebuild(targetPtr, targetBytes.Length, deserializedPtr, deserialized.Length))
                {
                    Assert.Fail("DynamicHashMap Stage A rebuild measurement failed.");
                }
            }

            rebuildTicks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
        }

        private static unsafe long MeasureStageAAllocatedBytes(
            DynamicHashMapHelper<int>* source,
            NativeArray<byte> compactPayload,
            NativeArray<byte> deserialized,
            DynamicBuffer<byte> targetBytes,
            NativeArray<byte> writerBuffer,
            int iterations)
        {
            Assert.IsTrue(RunStageASteadyState(source, compactPayload, deserialized, targetBytes, writerBuffer));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < iterations; i++)
            {
                if (!RunStageASteadyState(source, compactPayload, deserialized, targetBytes, writerBuffer))
                {
                    Assert.Fail("DynamicHashMap Stage A steady-state measurement failed.");
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static unsafe bool RunStageASteadyState(
            DynamicHashMapHelper<int>* source,
            NativeArray<byte> compactPayload,
            NativeArray<byte> deserialized,
            DynamicBuffer<byte> targetBytes,
            NativeArray<byte> writerBuffer)
        {
            var compactPayloadPtr = (byte*)compactPayload.GetUnsafePtr();
            var deserializedPtr = (byte*)deserialized.GetUnsafePtr();

            if (!DynamicHashMapNetCodeRawCodec<int, byte>.TryPack(source, compactPayloadPtr, compactPayload.Length, out var header))
            {
                return false;
            }

            var payloadBytes = (int)header.PayloadBytes;
            var writer = new DataStreamWriter(writerBuffer);
            DynamicHashMapNetCodeRawCodec<int, byte>.WritePayload(compactPayloadPtr, payloadBytes, ref writer);

            var reader = new DataStreamReader(writer.AsNativeArray());
            var compressionModel = StreamCompressionModel.Default;
            for (var offset = 0; offset < deserialized.Length; offset++)
            {
                DynamicHashMapNetCodeSerializer<DynamicHashMapTestsBuffer, int, byte>.Deserialize(
                    (IntPtr)(deserializedPtr + offset), IntPtr.Zero, ref reader, ref compressionModel, IntPtr.Zero, offset);
            }

            if (reader.GetBitsRead() != payloadBytes * 8)
            {
                return false;
            }

            return DynamicHashMapNetCodeRawCodec<int, byte>.TryRebuild(
                targetBytes.GetPtr(), targetBytes.Length, deserializedPtr, deserialized.Length);
        }

        private static double TicksToMicroseconds(long ticks, int iterations)
        {
            return ticks * 1000000.0 / System.Diagnostics.Stopwatch.Frequency / iterations;
        }

        private static unsafe void AssertCompactRoundTrip(DynamicHashMap<int, byte> source, DynamicHashMap<int, byte> rebuilt)
        {
            Assert.AreEqual(source.Count, rebuilt.Count);
            Assert.AreEqual(source.Capacity, rebuilt.Capacity);
            Assert.IsTrue(rebuilt.Helper->IsDense);

            var found = new HashSet<int>();
            foreach (var pair in source)
            {
                Assert.IsTrue(rebuilt.TryGetValue(pair.Key, out var value), $"Missing key {pair.Key} after compact round-trip.");
                Assert.AreEqual(pair.Value, value);
                found.Add(pair.Key);
            }

            Assert.AreEqual(source.Count, found.Count);
        }

        private static unsafe uint FloatBits(float value)
        {
            return *(uint*)&value;
        }

        private static unsafe ulong DoubleBits(double value)
        {
            return *(ulong*)&value;
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

        private static void AssertGeneratedMixedValue(
            DynamicHashMap<GeneratedPaddedKey, GeneratedMixedValue> map, GeneratedPaddedKey key, GeneratedMixedValue expected)
        {
            Assert.IsTrue(map.TryGetValue(key, out var actual), $"Missing generated-struct key {key.A}:{key.B}.");
            Assert.AreEqual(expected.Nested.Count, actual.Nested.Count);
            Assert.AreEqual(expected.Nested.Flag, actual.Nested.Flag);
            Assert.AreEqual(expected.Mode, actual.Mode);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Weight, actual.Weight);
            Assert.AreEqual(expected.@class, actual.@class);
        }

        private partial struct DynamicHashMapNetCodeGeneratedRegistrationSystem : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
                using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<GhostComponentSerializerCollectionData>();
                using var query = state.EntityManager.CreateEntityQuery(builder);
                ref var data = ref query.GetSingletonRW<GhostComponentSerializerCollectionData>().ValueRW;

                DynamicHashMapTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapRawStableModeTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapRootOnlyTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapPredictedOwnerTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapInterpolatedNonOwnerTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapGeneratedStructTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
                DynamicHashMapGeneratedPaddingTestsBufferDynamicHashMapGhostSerializer.AddToCollection(ref data, ref state);
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }

        private partial struct DynamicHashMapPredictedOwnerStateCaptureSystem : ISystem
        {
            public static GhostComponentSerializer.State State;

            public void OnCreate(ref SystemState state)
            {
                State = DynamicHashMapNetCodeSerializer<DynamicHashMapPredictedOwnerTestsBuffer, int, byte>.GetState(
                    ref state, 0, null, GhostPrefabType.PredictedClient, GhostSendType.OnlyPredictedClients, SendToOwnerType.SendToOwner);
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }

        private partial struct DynamicHashMapInterpolatedNonOwnerStateCaptureSystem : ISystem
        {
            public static GhostComponentSerializer.State State;

            public void OnCreate(ref SystemState state)
            {
                State = DynamicHashMapNetCodeSerializer<DynamicHashMapInterpolatedNonOwnerTestsBuffer, int, byte>.GetState(
                    ref state, 0, null, GhostPrefabType.InterpolatedClient, GhostSendType.OnlyInterpolatedClients, SendToOwnerType.SendToNonOwner);
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }
    }

}
#endif
