namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public struct BlobCurve4 : IBlobCurve<float4>
    {
        private BlobCurveHeader _header;
        private BlobArray<BlobCurveSegment4> _segments;

        public unsafe ref BlobCurveHeader Header => ref UnsafeUtility.AsRef<BlobCurveHeader>(UnsafeUtility.AddressOf(ref _header));

        public unsafe ref BlobArray<float> Times => ref UnsafeUtility.AsRef<BlobArray<float>>(UnsafeUtility.AddressOf(ref _header.Times));

        public BlobCurveHeader.WrapMode WrapModePrev => _header.WrapModePrev;

        public BlobCurveHeader.WrapMode WrapModePost => _header.WrapModePost;

        public int SegmentCount => _header.SegmentCount;

        public float StartTime => _header.StartTime;

        public float EndTime => _header.EndTime;

        public float Duration => _header.Duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = _header.SearchIgnoreWrapMode(time, ref cache, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time)
        {
            var i = _header.SearchIgnoreWrapMode(time, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = _header.Search(time, ref cache, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time)
        {
            var i = _header.Search(time, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        public static BlobAssetReference<BlobCurve4> Create(
            AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve4>();
            Construct(ref builder, ref data, curveX, curveY, curveZ, curveW);
            return builder.CreateBlobAssetReference<BlobCurve4>(allocator);
        }

        public static BlobAssetReference<BlobCurve4> Create(
            List<float4> vertices, List<float> times, BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
            Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve4>();
            Construct(ref builder, ref data, vertices, times, preWrapMode, postWrapMode);
            return builder.CreateBlobAssetReference<BlobCurve4>(allocator);
        }

        public static BlobAssetReference<BlobCurve4> Create(
            List<float4> vertices, List<float4x2> cvs, List<float> times, BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
            Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve4>();
            Construct(ref builder, ref data, vertices, cvs, times, preWrapMode, postWrapMode);
            return builder.CreateBlobAssetReference<BlobCurve4>(allocator);
        }

        public static void Construct(
            ref BlobBuilder builder, ref BlobCurve4 blobCurve, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            InputCurveCheck(curveX, curveY, curveZ, curveW);
            var keyFrameCount = curveX.length;
            using var xKeyBuffer = new NativeArray<Keyframe>(keyFrameCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            using var yKeyBuffer = new NativeArray<Keyframe>(curveY.length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            using var zKeyBuffer = new NativeArray<Keyframe>(curveZ.length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            using var wKeyBuffer = new NativeArray<Keyframe>(curveW.length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var xKeys = xKeyBuffer.AsSpan();
            var yKeys = yKeyBuffer.AsSpan();
            var zKeys = zKeyBuffer.AsSpan();
            var wKeys = wKeyBuffer.AsSpan();
            curveX.GetKeys(xKeys);
            curveY.GetKeys(yKeys);
            curveZ.GetKeys(zKeys);
            curveW.GetKeys(wKeys);
            InputKeyCheck(xKeys, yKeys, zKeys, wKeys);

            var hasOnlyOneKeyframe = keyFrameCount == 1;
            var segmentCount = math.select(keyFrameCount - 1, 1, hasOnlyOneKeyframe);
            blobCurve._header.SegmentCount = segmentCount;
            blobCurve._header.WrapModePrev = BlobShared.ConvertWrapMode(curveX.preWrapMode);
            blobCurve._header.WrapModePost = BlobShared.ConvertWrapMode(curveX.postWrapMode);

            if (hasOnlyOneKeyframe)
            {
                var key0X = xKeys[0];
                var key0Y = yKeys[0];
                var key0Z = zKeys[0];
                var key0W = wKeys[0];
                builder.Allocate(ref blobCurve._segments, 1)[0] = new BlobCurveSegment4(key0X, key0Y, key0Z, key0W, key0X, key0Y, key0Z, key0W);

                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = key0X.time;
                blobCurve._header.StartTime = key0X.time;
                blobCurve._header.EndTime = key0X.time;
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, keyFrameCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve._segments, segmentCount);
                for (int i = 0, j = 1; j < keyFrameCount; i = j++)
                {
                    timeBuilder[j] = xKeys[i].time;
                    segBuilder[i] = new BlobCurveSegment4(xKeys[i], yKeys[i], zKeys[i], wKeys[i], xKeys[j], yKeys[j], zKeys[j], wKeys[j]);
                }

                blobCurve._header.StartTime = xKeys[0].time;
                blobCurve._header.EndTime = timeBuilder[keyFrameCount] = xKeys[segmentCount].time;
                timeBuilder[0] = float.MaxValue;
                timeBuilder[keyFrameCount + 1] = float.MinValue;
            }
        }

        public static void Construct(
            ref BlobBuilder builder, ref BlobCurve4 blobCurve, List<float4> vertices, List<float> times, BlobCurveHeader.WrapMode preWrapMode,
            BlobCurveHeader.WrapMode postWrapMode)
        {
            var vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");

            var hasOnlyOneKeyframe = vertCount == 1;
            var segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            blobCurve._header.SegmentCount = segmentCount;
            blobCurve._header.WrapModePrev = preWrapMode;
            blobCurve._header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                builder.Allocate(ref blobCurve._segments, 1)[0] = BlobCurveSegment4.Linear4(v0, v0);
                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                blobCurve._header.StartTime = times[0];
                blobCurve._header.EndTime = times[0];
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve._segments, segmentCount);
                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment4.Linear4(vertices[i], vertices[j]);
                }

                blobCurve._header.StartTime = times[0];
                blobCurve._header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
        }

        public static void Construct(
            ref BlobBuilder builder, ref BlobCurve4 data, List<float4> vertices, List<float4x2> cvs, List<float> times, BlobCurveHeader.WrapMode preWrapMode,
            BlobCurveHeader.WrapMode postWrapMode)
        {
            var vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");
            Assert.IsTrue(cvs.Count == vertCount, $"Vertex Count{vertCount} and Control vertex count{cvs.Count} not sync");

            var hasOnlyOneKeyframe = vertCount == 1;
            var segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            data._header.SegmentCount = segmentCount;
            data._header.WrapModePrev = preWrapMode;
            data._header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                builder.Allocate(ref data._segments, 1)[0] = BlobCurveSegment4.Bezier4(v0, v0, v0, v0);
                var timeBuilder = builder.Allocate(ref data._header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                data._header.StartTime = times[0];
                data._header.EndTime = times[0];
            }
            else
            {
                var timeBuilder = builder.Allocate(ref data._header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref data._segments, segmentCount);
                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment4.Bezier4(vertices[i], cvs[i].c1, cvs[j].c0, vertices[j]);
                }

                data._header.StartTime = times[0];
                data._header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputCurveCheck(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (curveX == null || curveY == null || curveZ == null || curveW == null)
            {
                throw new NullReferenceException("Input curve is null");
            }

            if (curveX.length != curveY.length || curveX.length != curveZ.length || curveX.length != curveW.length)
            {
                throw new NullReferenceException(
                    $"Curve X[{curveX.length}]/Y[{curveY.length}]/Z[{curveZ.length}]/W[{curveW.length}] length not sync");
            }

            if (curveX.length == 0)
            {
                throw new ArgumentException("Input curve is empty (no keyframe)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputKeyCheck(
            ReadOnlySpan<Keyframe> xKeys, ReadOnlySpan<Keyframe> yKeys, ReadOnlySpan<Keyframe> zKeys, ReadOnlySpan<Keyframe> wKeys)
        {
            for (int i = 0, len = xKeys.Length; i < len; i++)
            {
                var kx = xKeys[i];
                var ky = yKeys[i];
                var kz = zKeys[i];
                var kw = wKeys[i];
                if (!Mathf.Approximately(kx.time, ky.time) || !Mathf.Approximately(kx.time, kz.time) || !Mathf.Approximately(kx.time, kw.time))
                {
                    throw new ArgumentException($"Time not sync Key[{i}, X time={kx.time}, Y time={ky.time}], Z time={kz.time}], W time={kw.time}]");
                }

                if (kx.weightedMode != WeightedMode.None)
                {
                    BLGlobalLogger.LogWarningString(
                        $"Weight Not Supported! X Key[{i},Weight[{kx.weightedMode},In{kx.inWeight},Out{kx.outWeight}],Time{kx.time},Value{kx.value}]");
                }

                if (ky.weightedMode != WeightedMode.None)
                {
                    BLGlobalLogger.LogWarningString(
                        $"Weight Not Supported! Y Key[{i},Weight[{ky.weightedMode},In{ky.inWeight},Out{ky.outWeight}],Time{ky.time},Value{ky.value}]");
                }

                if (kz.weightedMode != WeightedMode.None)
                {
                    BLGlobalLogger.LogWarningString(
                        $"Weight Not Supported! Z Key[{i},Weight[{kz.weightedMode},In{kz.inWeight},Out{kz.outWeight}],Time{kz.time},Value{kz.value}]");
                }

                if (kw.weightedMode != WeightedMode.None)
                {
                    BLGlobalLogger.LogWarningString(
                        $"Weight Not Supported! W Key[{i},Weight[{kw.weightedMode},In{kw.inWeight},Out{kw.outWeight}],Time{kw.time},Value{kw.value}]");
                }
            }
        }
    }
}
