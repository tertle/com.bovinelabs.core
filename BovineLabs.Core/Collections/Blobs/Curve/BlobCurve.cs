namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential)]
    public struct BlobCurve : IBlobCurve<float>
    {
        private BlobCurveHeader _header;
        private BlobArray<BlobCurveSegment> _segments;

        public unsafe ref BlobCurveHeader Header => ref UnsafeUtility.AsRef<BlobCurveHeader>(UnsafeUtility.AddressOf(ref _header));

        public unsafe ref BlobArray<float> Times => ref UnsafeUtility.AsRef<BlobArray<float>>(UnsafeUtility.AddressOf(ref _header.Times));

        public BlobCurveHeader.WrapMode WrapModePrev => _header.WrapModePrev;

        public BlobCurveHeader.WrapMode WrapModePost => _header.WrapModePost;

        public int SegmentCount => _header.SegmentCount;

        public float StartTime => _header.StartTime;

        public float EndTime => _header.EndTime;

        public float Duration => _header.Duration;

        public bool IsCreated => SegmentCount > 0;

        public static BlobAssetReference<BlobCurve> Create(AnimationCurve curve, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobCurve>();
            Construct(ref builder, ref data, curve);
            return builder.CreateBlobAssetReference<BlobCurve>(allocator);
        }

        public static void Construct(ref BlobBuilder builder, ref BlobCurve blobCurve, AnimationCurve curve)
        {
            InputCurveCheck(curve);
            var keyFrameCount = curve.length;
            using var keyBuffer = new NativeArray<Keyframe>(keyFrameCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var keys = keyBuffer.AsSpan();
            curve.GetKeys(keys);
            InputKeyCheck(keys);

            var hasOnlyOneKeyframe = keyFrameCount == 1;
            var segmentCount = math.select(keyFrameCount - 1, 1, hasOnlyOneKeyframe);
            blobCurve._header.SegmentCount = segmentCount;
            blobCurve._header.WrapModePrev = BlobShared.ConvertWrapMode(curve.preWrapMode);
            blobCurve._header.WrapModePost = BlobShared.ConvertWrapMode(curve.postWrapMode);

            if (hasOnlyOneKeyframe)
            {
                var key0 = keys[0];
                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, 4);
                timeBuilder[0] = timeBuilder[1] = timeBuilder[2] = timeBuilder[3] = key0.time;
                builder.Allocate(ref blobCurve._segments, 1)[0] = new BlobCurveSegment(key0, key0);
                blobCurve._header.StartTime = key0.time;
                blobCurve._header.EndTime = key0.time;
            }
            else
            {
                var timeBuilder = builder.Allocate(ref blobCurve._header.Times, keyFrameCount + 2);
                var segBuilder = builder.Allocate(ref blobCurve._segments, segmentCount);
                for (int i = 0, j = 1; i < segmentCount; i = j++)
                {
                    var keyI = keys[i];
                    timeBuilder[j] = keyI.time;
                    segBuilder[i] = new BlobCurveSegment(keyI, keys[j]);
                }

                blobCurve._header.StartTime = keys[0].time;
                blobCurve._header.EndTime = timeBuilder[keyFrameCount] = keys[segmentCount].time;
                timeBuilder[0] = float.MaxValue;
                timeBuilder[keyFrameCount + 1] = float.MinValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = _header.SearchIgnoreWrapMode(time, ref cache, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapMode(in float time)
        {
            var i = _header.SearchIgnoreWrapMode(time, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(in float time, [NoAlias] ref BlobCurveCache cache)
        {
            var i = _header.Search(time, ref cache, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(in float time)
        {
            var i = _header.Search(time, out var t);
            return _segments[i].Sample(BlobShared.PowerSerial(t));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputCurveCheck(AnimationCurve curve)
        {
            if (curve == null)
            {
                throw new NullReferenceException("Input curve is null");
            }

            if (curve.length == 0)
            {
                throw new ArgumentException("Input curve is empty (no keyframe)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void InputKeyCheck(ReadOnlySpan<Keyframe> keys)
        {
            for (int i = 0, len = keys.Length; i < len; i++)
            {
                var k = keys[i];
                if (k.weightedMode != WeightedMode.None)
                {
                    BLGlobalLogger.LogWarningString(
                        $"Weight Not Supported! Key[{i}, Weight[{k.weightedMode}, In{k.inWeight}, Out{k.outWeight}], Time{k.time}, Value{k.value}]");
                }
            }
        }
    }
}
