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
    public struct BlobCurve2 : IBlobCurve<float2>
    {
        internal BlobCurveHeader m_Header;
        internal BlobArray<BlobCurveSegment2> Segments;

        public unsafe ref BlobCurveHeader Header => ref UnsafeUtility.AsRef<BlobCurveHeader>(UnsafeUtility.AddressOf(ref this.m_Header));
        public unsafe ref BlobArray<float> Times => ref UnsafeUtility.AsRef<BlobArray<float>>(UnsafeUtility.AddressOf(ref this.m_Header.Times));
        public BlobCurveHeader.WrapMode WrapModePrev => this.m_Header.WrapModePrev;
        public BlobCurveHeader.WrapMode WrapModePost => this.m_Header.WrapModePost;
        public int SegmentCount => this.m_Header.SegmentCount;
        public float StartTime => this.m_Header.StartTime;
        public float EndTime => this.m_Header.EndTime;
        public float Duration => this.m_Header.Duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.m_Header.SearchIgnoreWrapMode(time, ref cache, out var t);
            return this.Segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapMode(in float time)
        {
            var i = this.m_Header.SearchIgnoreWrapMode(time, out var t);
            return this.Segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = this.m_Header.Search(time, ref cache, out var t);
            return this.Segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(in float time)
        {
            var i = this.m_Header.Search(time, out var t);
            return this.Segments[i].Sample(BlobShared.PowerSerial(t));
        }

        public static BlobAssetReference<BlobCurve2> Create(AnimationCurve curveX, AnimationCurve curveY, Allocator allocator = Allocator.Persistent)
        {
            InputCurveCheck(curveX, curveY);
            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            int keyFrameCount = xKeys.Length;
            bool hasOnlyOneKeyframe = keyFrameCount == 1;
            int segmentCount = math.select(keyFrameCount - 1, 1, hasOnlyOneKeyframe);
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            data.m_Header.SegmentCount = segmentCount;
            data.m_Header.WrapModePrev = BlobShared.ConvertWrapMode(curveX.preWrapMode);
            data.m_Header.WrapModePost = BlobShared.ConvertWrapMode(curveX.postWrapMode);

            if (hasOnlyOneKeyframe)
            {
                var key0x = xKeys[0];
                var key0y = yKeys[0];
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = key0x.time;
                builder.Allocate(ref data.Segments, 1)[0] = new BlobCurveSegment2(key0x, key0y, key0x, key0y);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, keyFrameCount + 2);
                var segBuilder = builder.Allocate(ref data.Segments, segmentCount);
                for (int i = 0, j = 1; j < keyFrameCount; i = j++)
                {
                    timeBuilder[j] = xKeys[i].time;
                    segBuilder[i] = new BlobCurveSegment2(xKeys[i], yKeys[i], xKeys[j], yKeys[j]);
                }
                data.m_Header.StartTime = xKeys[0].time;
                data.m_Header.EndTime = timeBuilder[keyFrameCount] = xKeys[segmentCount].time;
                timeBuilder[0] = float.MaxValue;
                timeBuilder[keyFrameCount + 1] = float.MinValue;
            }
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }

        public static BlobAssetReference<BlobCurve2> Create(
          List<float2> vertices, List<float> times,
          BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
          Allocator allocator = Allocator.Persistent)
        {
            int vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");

            bool hasOnlyOneKeyframe = vertCount == 1;
            int segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            data.m_Header.SegmentCount = segmentCount;
            data.m_Header.WrapModePrev = preWrapMode;
            data.m_Header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                builder.Allocate(ref data.Segments, 1)[0] = BlobCurveSegment2.Linear2(v0, v0);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref data.Segments, segmentCount);

                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment2.Linear2(vertices[i], vertices[j]);
                }
                data.m_Header.StartTime = times[0];
                data.m_Header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }


        public static BlobAssetReference<BlobCurve2> Create(
          List<float2> vertices, List<float2x2> cvs, List<float> times,
          BlobCurveHeader.WrapMode preWrapMode, BlobCurveHeader.WrapMode postWrapMode,
          Allocator allocator = Allocator.Persistent)
        {
            int vertCount = vertices.Count;
            Assert.IsTrue(vertCount > 0, "No vertices");
            Assert.IsTrue(vertCount == times.Count, $"Vertex Count{vertCount} and Time count{times.Count} not sync");
            Assert.IsTrue(cvs.Count == vertCount, $"Vertex Count{vertCount} and Control vertex count{cvs.Count} not sync");

            bool hasOnlyOneKeyframe = vertCount == 1;
            int segmentCount = math.select(vertCount - 1, 1, hasOnlyOneKeyframe);
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve2>();
            data.m_Header.SegmentCount = segmentCount;
            data.m_Header.WrapModePrev = preWrapMode;
            data.m_Header.WrapModePost = postWrapMode;
            if (hasOnlyOneKeyframe)
            {
                var v0 = vertices[0];
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = times[0];
                builder.Allocate(ref data.Segments, 1)[0] = BlobCurveSegment2.Bezier2(v0, v0, v0, v0);
            }
            else
            {
                var timeBuilder = builder.Allocate(ref data.m_Header.Times, vertCount + 2);
                var segBuilder = builder.Allocate(ref data.Segments, segmentCount);
                for (int i = 0, j = 1; j < segmentCount; i = j++)
                {
                    timeBuilder[j] = times[i];
                    segBuilder[i] = BlobCurveSegment2.Bezier2(vertices[i], cvs[i].c1, cvs[j].c0, vertices[j]);
                }
                data.m_Header.StartTime = times[0];
                data.m_Header.EndTime = timeBuilder[vertCount] = times[segmentCount];
                timeBuilder[0] = float.MaxValue;
                timeBuilder[vertCount + 1] = float.MinValue;
            }
            return builder.CreateBlobAssetReference<BlobCurve2>(allocator);
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void InputCurveCheck(AnimationCurve curveX, AnimationCurve curveY)
        {
            if (curveX == null || curveY == null) throw new NullReferenceException("Input curve is null");
            if (curveX.length != curveY.length) throw new NullReferenceException($"Curve X[{curveX.length}]/Y[{curveY.length}] length not sync");
            if (curveX.length == 0) throw new ArgumentException("Input curve is empty (no keyframe)");
            var xKeys = curveX.keys;
            var yKeys = curveY.keys;
            for (int i = 0, len = xKeys.Length; i < len; i++)
            {
                var kx = xKeys[i];
                var ky = yKeys[i];
                if (!Mathf.Approximately(kx.time, ky.time)) throw new ArgumentException($"Time not sync Key[{i}, X time={kx.time}, Y time={ky.time}]");
                if (kx.weightedMode != WeightedMode.None)
                    Debug.LogWarning($"Weight Not Supported! X Key[{i},Weight[{kx.weightedMode},In{kx.inWeight},Out{kx.outWeight}],Time{kx.time},Value{kx.value}]");
                if (ky.weightedMode != WeightedMode.None)
                    Debug.LogWarning($"Weight Not Supported! Y Key[{i},Weight[{ky.weightedMode},In{ky.inWeight},Out{ky.outWeight}],Time{ky.time},Value{ky.value}]");
            }
        }
    }
}
