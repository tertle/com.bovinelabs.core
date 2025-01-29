// <copyright file="BlobCurveHeader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveHeader
    {
        public enum WrapMode : short
        {
            Clamp = 0,
            Loop = 1,
            PingPong = 2,
        }

        public WrapMode WrapModePrev;
        public WrapMode WrapModePost;
        public int SegmentCount;
        public float StartTime;
        public float EndTime;

        internal BlobArray<float> Times;

        public float Duration => this.EndTime - this.StartTime;

        public unsafe int SearchIgnoreWrapMode(in float time, [NoAlias] ref BlobCurveCache cache, [NoAlias] out float t)
        {
            var wrappedTime = math.clamp(time, this.StartTime, this.EndTime);
            var isPrev = wrappedTime < cache.NeighborhoodTimes.x;
            var isPost = wrappedTime > cache.NeighborhoodTimes.y;
            if ((cache.Index >= 0) & !(isPrev | isPost))
            {
                var d = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
                t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / d, 0, d == 0);
                return cache.Index;
            }

            var times = (float*)this.Times.GetUnsafePtr();
            int lo = 0, hi = this.SegmentCount - 1;
            cache.Index = math.clamp(cache.Index, lo, hi);
            var neighborhoodIDs = new int2(cache.Index - 1, cache.Index + 1);
            var neighborhoodTimes = *(float4*)(times + cache.Index);
            isPrev &= neighborhoodTimes.x <= wrappedTime;
            isPost &= wrappedTime <= neighborhoodTimes.w;
            if (isPrev | isPost)
            {
                cache.NeighborhoodTimes = isPrev ? neighborhoodTimes.xy : neighborhoodTimes.zw;
                var d = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
                t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / d, 0, d == 0);
                cache.Index = isPrev ? neighborhoodIDs.x : neighborhoodIDs.y;
                return cache.Index;
            }

            bool notFound;
            do
            {
                cache.NeighborhoodTimes = *(float2*)(times + (cache.Index + 1));
                var goLow = wrappedTime < cache.NeighborhoodTimes.x;
                var goHigh = wrappedTime > cache.NeighborhoodTimes.y;
                notFound = goLow | goHigh;
                lo = math.select(lo, cache.Index + 1, goHigh);
                hi = math.select(hi, cache.Index - 1, goLow);
                cache.Index = math.select(cache.Index, lo + ((hi - lo) >> 1), notFound);
            }
            while (notFound & (lo <= hi));

            var duration = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
            t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / duration, 0, Approximately(duration, 0));
            return cache.Index;
        }

        public unsafe int SearchIgnoreWrapMode(in float time, [NoAlias] out float t)
        {
            var wrappedTime = math.clamp(time, this.StartTime, this.EndTime);
            var times = (float*)this.Times.GetUnsafePtr();
            var timeRange = *(float2*)(times + 1);
            if (wrappedTime <= timeRange.y)
            {
                var d = timeRange.y - timeRange.x;
                t = math.select((wrappedTime - timeRange.x) / d, 0, d == 0);
                return 0;
            }

            int lo = 0, hi = this.SegmentCount - 1, i = 0;
            bool notFound;
            do
            {
                timeRange = *(float2*)(times + (i + 1));
                var goLow = wrappedTime < timeRange.x;
                var goHi = wrappedTime > timeRange.y;
                notFound = goLow | goHi;
                lo = math.select(lo, i + 1, goHi);
                hi = math.select(hi, i - 1, goLow);
                i = math.select(i, lo + ((hi - lo) >> 1), notFound);
            }
            while (notFound & (lo <= hi));

            var duration = timeRange.y - timeRange.x;
            t = math.select((wrappedTime - timeRange.x) / duration, 0, duration == 0);
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int Search(in float time, [NoAlias] ref BlobCurveCache cache, [NoAlias] out float t)
        {
            float wrappedTime, duration;
            var preClamp = this.WrapModePrev == WrapMode.Clamp;
            var postClamp = this.WrapModePost == WrapMode.Clamp;
            if (preClamp & postClamp)
            {
                wrappedTime = math.clamp(time, this.StartTime, this.EndTime);
            }
            else
            {
                var left = time < this.StartTime;
                var right = time > this.EndTime;
                if (left | right)
                {
                    var wrapMode = left ? this.WrapModePrev : this.WrapModePost;
                    switch (wrapMode)
                    {
                        default:
                        case WrapMode.Clamp:
                            wrappedTime = left ? this.StartTime : this.EndTime;
                            break;
                        case WrapMode.Loop:
                            wrappedTime = ModPlus(time - this.StartTime, this.Duration) + this.StartTime;
                            break;
                        case WrapMode.PingPong:
                            duration = this.Duration;
                            var offset = ModPlus(time - this.StartTime, duration);
                            var loopCounter = (int)math.floor((time - this.StartTime) / this.Duration);
                            var isMirror = (loopCounter & 1) == 1;
                            wrappedTime = this.StartTime + (isMirror ? this.Duration - offset : offset);
                            break;
                    }
                }
                else
                {
                    wrappedTime = time;
                }
            }

            var isPrev = wrappedTime < cache.NeighborhoodTimes.x;
            var isPost = wrappedTime > cache.NeighborhoodTimes.y;
            if ((cache.Index >= 0) & !(isPrev | isPost))
            {
                duration = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
                t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / duration, 0, duration == 0);
                return cache.Index;
            }

            var times = (float*)this.Times.GetUnsafePtr();
            int lo = 0, hi = this.SegmentCount - 1;
            cache.Index = math.clamp(cache.Index, lo, hi);
            var neighborhoodIDs = new int2(cache.Index - 1, cache.Index + 1);
            var neighborhoodTimes = *(float4*)(times + cache.Index);
            isPrev &= neighborhoodTimes.x <= wrappedTime;
            isPost &= wrappedTime <= neighborhoodTimes.w;
            if (isPrev | isPost)
            {
                cache.NeighborhoodTimes = isPrev ? neighborhoodTimes.xy : neighborhoodTimes.zw;
                duration = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
                t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / duration, 0, duration == 0);
                cache.Index = isPrev ? neighborhoodIDs.x : neighborhoodIDs.y;
                return cache.Index;
            }

            bool notFound;
            do
            {
                cache.NeighborhoodTimes = *(float2*)(times + (cache.Index + 1));
                var goLow = wrappedTime < cache.NeighborhoodTimes.x;
                var goHigh = wrappedTime > cache.NeighborhoodTimes.y;
                notFound = goLow | goHigh;
                lo = math.select(lo, cache.Index + 1, goHigh);
                hi = math.select(hi, cache.Index - 1, goLow);
                cache.Index = math.select(cache.Index, lo + ((hi - lo) >> 1), notFound);
            }
            while (notFound & (lo <= hi));

            duration = cache.NeighborhoodTimes.y - cache.NeighborhoodTimes.x;
            t = math.select((wrappedTime - cache.NeighborhoodTimes.x) / duration, 0, duration == 0);
            return cache.Index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int Search(in float time, [NoAlias] out float t)
        {
            float wrappedTime, duration;
            var preClamp = this.WrapModePrev == WrapMode.Clamp;
            var postClamp = this.WrapModePost == WrapMode.Clamp;
            if (preClamp & postClamp)
            {
                wrappedTime = math.clamp(time, this.StartTime, this.EndTime);
            }
            else
            {
                var left = time < this.StartTime;
                var right = time > this.EndTime;
                if (left | right)
                {
                    var wrapMode = left ? this.WrapModePrev : this.WrapModePost;
                    switch (wrapMode)
                    {
                        default:
                        case WrapMode.Clamp:
                            wrappedTime = left ? this.StartTime : this.EndTime;
                            break;
                        case WrapMode.Loop:
                            wrappedTime = ModPlus(time - this.StartTime, this.Duration) + this.StartTime;
                            break;
                        case WrapMode.PingPong:
                            duration = this.Duration;
                            var offset = ModPlus(time - this.StartTime, duration);
                            var loopCounter = (int)math.floor((time - this.StartTime) / this.Duration);
                            var isMirror = (loopCounter & 1) == 1;
                            wrappedTime = this.StartTime + (isMirror ? this.Duration - offset : offset);
                            break;
                    }
                }
                else
                {
                    wrappedTime = time;
                }
            }

            var times = (float*)this.Times.GetUnsafePtr();
            var timeRange = *(float2*)(times + 1);
            if (wrappedTime <= timeRange.y)
            {
                duration = timeRange.y - timeRange.x;
                t = math.select((wrappedTime - timeRange.x) / duration, 0, duration == 0);
                return 0;
            }

            var lo = 0;
            var hi = this.SegmentCount - 1;
            var i = 0;

            bool notFound;
            do
            {
                timeRange = *(float2*)(times + (i + 1));
                var goLow = wrappedTime < timeRange.x;
                var goHigh = wrappedTime > timeRange.y;
                notFound = goLow | goHigh;
                lo = math.select(lo, i + 1, goHigh);
                hi = math.select(hi, i - 1, goLow);
                i = math.select(i, lo + ((hi - lo) >> 1), notFound);
            }
            while (notFound & (lo <= hi));

            duration = timeRange.y - timeRange.x;
            t = math.select((wrappedTime - timeRange.x) / duration, 0, duration == 0);
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ModPlus(float value, float range)
        {
            Check.Assume(range > 0);
            var mod = value % range;
            return math.select(mod + range, mod, mod >= 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Approximately(float value, float equals)
        {
            return math.abs(value - equals) < 1.1921e-07F;
        }
    }
}
