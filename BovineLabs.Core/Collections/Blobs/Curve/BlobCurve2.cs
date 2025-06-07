// <copyright file="BlobCurve2.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
    public struct BlobCurve2 : IBlobCurve<float2>
    {
        private BlobCurveHeader header;
        private BlobArray<BlobCurveSegment2> segments;

        public unsafe ref BlobCurveHeader Header => ref UnsafeUtility.AsRef<BlobCurveHeader>(UnsafeUtility.AddressOf(ref this.header));

        public unsafe ref BlobArray<float> Times => ref UnsafeUtility.AsRef<BlobArray<float>>(UnsafeUtility.AddressOf(ref this.header.Times));

        public BlobCurveHeader.WrapMode WrapModePrev => this.header.WrapModePrev;

        public BlobCurveHeader.WrapMode WrapModePost => this.header.WrapModePost;

        public int SegmentCount => this.header.SegmentCount;

        public float StartTime => this.header.StartTime;

        public float EndTime => this.header.EndTime;

        public float Duration => this.header.Duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.header.SearchIgnoreWrapMode(time, ref cache, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapMode(in float time)
        {
            var i = this.header.SearchIgnoreWrapMode(time, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.header.Search(time, ref cache, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(in float time)
        {
            var i = this.header.Search(time, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        public static BlobAssetReference<BlobCurve2> Create(AnimationCurve curveX, AnimationCurve curveY, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            Construct(ref builder, ref data, curveX, curveY);
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }

        public static BlobAssetReference<BlobCurve2> Create(
            List<float2> vertices, List<float> times, BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
            Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            Construct(ref builder, ref data, vertices, times, preWrapMode, postWrapMode);
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }

        public static BlobAssetReference<BlobCurve2> Create(
            List<float2> vertices, List<float2x2> cvs, List<float> times, BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
            Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            Construct(ref builder, ref data, vertices, cvs, times, preWrapMode, postWrapMode);
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }

        public static void Construct(ref BlobBuilder builder, ref BlobCurve2 blobCurve, AnimationCurve curveX, AnimationCurve curveY)
        {
            InputCurveCheck(curveX, curveY);
            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            var keyFrameCount = xKeys.Length;
            var hasOnlyOneKeyframe = keyFrameCount == 1;
            var segmentCount = math.select(keyFrameCount - 1, 1, hasOnlyOneKeyframe);

            blobCurve.header.SegmentCount = segmentCount;
            blobCurve.header.WrapModePrev = BlobShared.ConvertWrapMode(curveX.preWrapMode);
            blobCurve.header.WrapModePost = BlobShared.ConvertWrapMode(curveX.postWrapMode);

            if (hasOnlyOneKeyframe)
            {
                var key0X = xKeys[0];
                var key0Y = yKeys[0];
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = key0X.time;
                builder.Allocate(ref blobCurve.segments, 1)[0] = new BlobCurveSegment2(key0X, key0Y, key0X, key0Y);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, keyFrameCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve.segments, segmentCount);
                for (int i = 0, j = 1; j < keyFrameCount; i = j++)
                {
                    timeBuilder[j] = xKeys[i].time;
                    segBuilder[i] = new BlobCurveSegment2(xKeys[i], yKeys[i], xKeys[j], yKeys[j]);
                }

                blobCurve.header.StartTime = xKeys[0].time;
                blobCurve.header.EndTime = timeBuilder[keyFrameCount] = xKeys[segmentCount].time;
                timeBuilder[0] = float.MaxValue;
                timeBuilder[keyFrameCount + 1] = float.MinValue;
            }
        }

        public static void Construct(
            ref BlobBuilder builder, ref BlobCurve2 blobCurve, List<float2> vertices, List<float> times, BlobCurveHeader.WrapMode preWrapMode,
            BlobCurveHeader.WrapMode postWrapMode)
        {
            var vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");

            var hasOnlyOneKeyframe = vertCount == 1;
            var segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            blobCurve.header.SegmentCount = segmentCount;
            blobCurve.header.WrapModePrev = preWrapMode;
            blobCurve.header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                builder.Allocate(ref blobCurve.segments, 1)[0] = BlobCurveSegment2.Linear2(v0, v0);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve.segments, segmentCount);

                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment2.Linear2(vertices[i], vertices[j]);
                }

                blobCurve.header.StartTime = times[0];
                blobCurve.header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
        }

        public static void Construct(
            ref BlobBuilder builder, ref BlobCurve2 blobCurve, List<float2> vertices, List<float2x2> cvs, List<float> times,
            BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode)
        {
            var vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");
            Assert.IsTrue(cvs.Count == vertCount, $"Vertex Count{vertCount} and Control vertex count{cvs.Count} not sync");

            var hasOnlyOneKeyframe = vertCount == 1;
            var segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            blobCurve.header.SegmentCount = segmentCount;
            blobCurve.header.WrapModePrev = preWrapMode;
            blobCurve.header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                builder.Allocate(ref blobCurve.segments, 1)[0] = BlobCurveSegment2.Bezier2(v0, v0, v0, v0);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve.segments, segmentCount);
                for (int i = 0, j = 1; j < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment2.Bezier2(vertices[i], cvs[i].c1, cvs[j].c0, vertices[j]);
                }

                blobCurve.header.StartTime = times[0];
                blobCurve.header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputCurveCheck(AnimationCurve curveX, AnimationCurve curveY)
        {
            if (curveX == null || curveY == null)
            {
                throw new NullReferenceException("Input curve is null");
            }

            if (curveX.length != curveY.length)
            {
                throw new NullReferenceException($"Curve X[{curveX.length}]/Y[{curveY.length}] length not sync");
            }

            if (curveX.length == 0)
            {
                throw new ArgumentException("Input curve is empty (no keyframe)");
            }

            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            for (int i = 0, len = xKeys.Length; i < len; i++)
            {
                var kx = xKeys[i];
                var ky = yKeys[i];
                if (!Mathf.Approximately(kx.time, ky.time))
                {
                    throw new ArgumentException($"Time not sync Key[{i}, X time={kx.time}, Y time={ky.time}]");
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
            }
        }
    }
}
