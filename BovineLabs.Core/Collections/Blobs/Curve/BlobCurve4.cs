// <copyright file="BlobCurve4.cs" company="BovineLabs">
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
    using Debug = UnityEngine.Debug;

    [StructLayout(LayoutKind.Sequential)]
    public struct BlobCurve4 : IBlobCurve<float4>
    {
        private BlobCurveHeader header;
        private BlobArray<BlobCurveSegment4> segments;

        public unsafe ref BlobCurveHeader Header => ref UnsafeUtility.AsRef<BlobCurveHeader>(UnsafeUtility.AddressOf(ref this.header));

        public unsafe ref BlobArray<float> Times => ref UnsafeUtility.AsRef<BlobArray<float>>(UnsafeUtility.AddressOf(ref this.header.Times));

        public BlobCurveHeader.WrapMode WrapModePrev => this.header.WrapModePrev;

        public BlobCurveHeader.WrapMode WrapModePost => this.header.WrapModePost;

        public int SegmentCount => this.header.SegmentCount;

        public float StartTime => this.header.StartTime;

        public float EndTime => this.header.EndTime;

        public float Duration => this.header.Duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.header.SearchIgnoreWrapMode(time, ref cache, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time)
        {
            var i = this.header.SearchIgnoreWrapMode(time, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.header.Search(time, ref cache, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time)
        {
            var i = this.header.Search(time, out var t);
            return this.segments[i].Sample(BlobShared.PowerSerial(t));
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
            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            var zKeys = curveZ.keys;
            var wKeys = curveW.keys;
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
                var key0Z = zKeys[0];
                var key0W = zKeys[0];
                builder.Allocate(ref blobCurve.segments, 1)[0] = new BlobCurveSegment4(key0X, key0Y, key0Z, key0W, key0X, key0Y, key0Z, key0W);

                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = key0X.time;
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, keyFrameCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve.segments, segmentCount);
                for (int i = 0, j = 1; j < keyFrameCount; i = j++)
                {
                    timeBuilder[j] = xKeys[i].time;
                    segBuilder[i] = new BlobCurveSegment4(xKeys[i], yKeys[i], zKeys[i], wKeys[i], xKeys[j], yKeys[j], zKeys[j], wKeys[j]);
                }

                blobCurve.header.StartTime = xKeys[0].time;
                blobCurve.header.EndTime = timeBuilder[keyFrameCount] = xKeys[segmentCount].time;
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
            blobCurve.header.SegmentCount = segmentCount;
            blobCurve.header.WrapModePrev = preWrapMode;
            blobCurve.header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                builder.Allocate(ref blobCurve.segments, 1)[0] = BlobCurveSegment4.Linear4(v0, v0);
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve.header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve.segments, segmentCount);
                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment4.Linear4(vertices[i], vertices[j]);
                }

                blobCurve.header.StartTime = times[0];
                blobCurve.header.EndTime = timeBuilder[vertCount] = times[segmentCount];
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
            data.header.SegmentCount = segmentCount;
            data.header.WrapModePrev = preWrapMode;
            data.header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                builder.Allocate(ref data.segments, 1)[0] = BlobCurveSegment4.Bezier4(v0, v0, v0, v0);
                var timeBuilder = builder.Allocate(ref data.header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
            }
            else
            {
                var timeBuilder = builder.Allocate(ref data.header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref data.segments, segmentCount);
                for (int i = 0, j = 1; j < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment4.Bezier4(vertices[i], cvs[i].c1, cvs[j].c0, vertices[j]);
                }

                data.header.StartTime = times[0];
                data.header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputCurveCheck(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (curveX == null || curveY == null || curveZ == null)
            {
                throw new NullReferenceException("Input curve is null");
            }

            if (curveX.length != curveY.length || curveX.length != curveZ.length)
            {
                throw new NullReferenceException($"Curve X[{curveX.length}]/Y[{curveY.length}]/Z[{curveZ.length}] length not sync");
            }

            if (curveX.length == 0)
            {
                throw new ArgumentException("Input curve is empty (no keyframe)");
            }

            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            var zKeys = curveZ.keys;
            var wKeys = curveW.keys;
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
                    Debug.LogWarning(
                        $"Weight Not Supported! X Key[{i},Weight[{kx.weightedMode},In{kx.inWeight},Out{kx.outWeight}],Time{kx.time},Value{kx.value}]");
                }

                if (ky.weightedMode != WeightedMode.None)
                {
                    Debug.LogWarning(
                        $"Weight Not Supported! Y Key[{i},Weight[{ky.weightedMode},In{ky.inWeight},Out{ky.outWeight}],Time{ky.time},Value{ky.value}]");
                }

                if (kz.weightedMode != WeightedMode.None)
                {
                    Debug.LogWarning(
                        $"Weight Not Supported! Z Key[{i},Weight[{kz.weightedMode},In{kz.inWeight},Out{kz.outWeight}],Time{kz.time},Value{kz.value}]");
                }

                if (kw.weightedMode != WeightedMode.None)
                {
                    Debug.LogWarning(
                        $"Weight Not Supported! Z Key[{i},Weight[{kw.weightedMode},In{kw.inWeight},Out{kw.outWeight}],Time{kw.time},Value{kw.value}]");
                }
            }
        }
    }
}
