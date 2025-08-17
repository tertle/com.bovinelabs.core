// <copyright file="BlobSpline.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES
namespace BovineLabs.Core.Collections
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Splines;

    public struct BlobSpline
    {
        private const int SegmentResolution = 30;
        private const int NormalsPerCurve = 16;

        /// <summary> A BlobArray of <see cref="BezierKnot"/> that form this Spline. </summary>
        /// <value> Returns a reference to the knots array. </value>
        [ReadOnly]
        public BlobArray<BezierKnot> Knots;

        /// <summary> A BlobArray of <see cref="BezierCurve"/> that form this Spline. </summary>
        /// <value> Returns a reference to the curves array. </value>
        [ReadOnly]
        public BlobArray<BezierCurve> Curves;

        // TODO we totally can in a blob
        // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
        // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
        [ReadOnly]
        public BlobArray<DistanceToInterpolation> SegmentLengthsLookupTable;

        // TODO we totally can in a blob
        // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
        // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
        [ReadOnly]
        public BlobArray<float3> UpVectorsLookupTable;

        /// <summary> Whether the spline is open (has a start and end point) or closed (forms an unbroken loop). </summary>
        public bool Closed;

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// Note that this value is affected by the transform used to create this NativeSpline.
        /// </summary>
        /// <returns> Returns the sum length of all curves composing this spline, accounting for closed state. </returns>
        public float Length;

        /// <summary> Gets the number of knots. </summary>
        public int Count => this.Knots.Length;

        /// <summary> Get the knot at <paramref name="index"/>. </summary>
        /// <param name="index">The zero-based index of the knot.</param>
        public BezierKnot this[int index] => this.Knots[index];

        public static BlobAssetReference<BlobSpline> Create(ISpline spline, float4x4 transform, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobSpline>();
            Construct(ref builder, ref data, spline, transform);
            return builder.CreateBlobAssetReference<BlobSpline>(allocator);
        }

        public static BlobAssetReference<BlobArray<BlobSpline>> Create<T>(IReadOnlyList<T> splines, float4x4 transform, Allocator allocator = Allocator.Persistent)
            where T : ISpline
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var data = ref builder.ConstructRoot<BlobArray<BlobSpline>>();

            var blobSplines = builder.Allocate(ref data, splines.Count);

            for (var i = 0; i < splines.Count; i++)
            {
                Construct(ref builder, ref blobSplines[i], splines[i], transform);
            }

            return builder.CreateBlobAssetReference<BlobArray<BlobSpline>>(allocator);
        }

        public static void Construct(ref BlobBuilder builder, ref BlobSpline blobSpline, ISpline spline, float4x4 transform)
        {
            var knotCount = spline.Count;

            var knots = builder.Allocate(ref blobSpline.Knots, knotCount);
            var curves = builder.Allocate(ref blobSpline.Curves, knotCount);
            var segmentLengthsLookupTable = builder.Allocate(ref blobSpline.SegmentLengthsLookupTable, knotCount * SegmentResolution);

            blobSpline.Closed = spline.Closed;
            blobSpline.Length = 0f;

            // Costly to do this for temporary NativeSpline that does not require to access/compute up vectors
            var upVectorsLookupTable = builder.Allocate(ref blobSpline.UpVectorsLookupTable, knotCount * SegmentResolution);

            // TODO we totally can for blobs
            // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
            // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
            var distanceToTimes = new NativeArray<DistanceToInterpolation>(SegmentResolution, Allocator.Temp);
            var upVectors = new NativeArray<float3>(SegmentResolution, Allocator.Temp);

            if (knotCount > 0)
            {
                var cur = spline[0].Transform(transform);
                for (var i = 0; i < knotCount; ++i)
                {
                    var next = spline[(i + 1) % knotCount].Transform(transform);
                    knots[i] = cur;

                    curves[i] = new BezierCurve(cur, next);
                    CurveUtility.CalculateCurveLengths(curves[i], distanceToTimes);

                    var curveStartUp = math.rotate(cur.Rotation, math.up());
                    var curveEndUp = math.rotate(next.Rotation, math.up());
                    EvaluateUpVectors(curves[i], curveStartUp, curveEndUp, upVectors);

                    if (blobSpline.Closed || i < knotCount - 1)
                    {
                        blobSpline.Length += distanceToTimes[SegmentResolution - 1].Distance;
                    }

                    for (var index = 0; index < SegmentResolution; index++)
                    {
                        segmentLengthsLookupTable[(i * SegmentResolution) + index] = distanceToTimes[index];

                        upVectorsLookupTable[(i * SegmentResolution) + index] = upVectors[index];
                    }

                    cur = next;
                }
            }
        }

        /// <summary> Get a <see cref="BezierCurve"/> from a knot index. </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns> A <see cref="BezierCurve"/> formed by the knot at index and the next knot. </returns>
        public BezierCurve GetCurve(int index) => this.Curves[index];

        /// <summary> Get the length of a <see cref="BezierCurve"/>. </summary>
        /// <param name="curveIndex">The 0 based index of the curve to find length for.</param>
        /// <returns>The length of the bezier curve at index.</returns>
        public float GetCurveLength(int curveIndex)
        {
            return this.SegmentLengthsLookupTable[(curveIndex * SegmentResolution) + SegmentResolution - 1].Distance;
        }

        /// <summary>
        /// Return the up vector for a t ratio on the curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the spline.</param>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t)
        {
            // Value  is not cached, compute the value directly on demand
            if (this.UpVectorsLookupTable.Length == 0)
            {
                return this.CalculateUpVector(index, t);
            }

            var curveIndex = index * SegmentResolution;
            var offset = 1f / (SegmentResolution - 1);
            var curveT = 0f;
            for (var i = 0; i < SegmentResolution; i++)
            {
                if (t <= curveT + offset)
                {
                    var value = math.lerp(this.UpVectorsLookupTable[curveIndex + i], this.UpVectorsLookupTable[curveIndex + i + 1], (t - curveT) / offset);

                    return value;
                }

                curveT += offset;
            }

            // Otherwise, no value has been found, return the one at the end of the segment
            return this.UpVectorsLookupTable[curveIndex + SegmentResolution - 1];
        }

        public float GetCurveInterpolation(int curveIndex, float curveDistance)
        {
            if (curveIndex < 0 || curveIndex >= this.SegmentLengthsLookupTable.Length || curveDistance <= 0)
            {
                return 0f;
            }

            var curveLength = this.GetCurveLength(curveIndex);
            if (curveDistance >= curveLength)
            {
                return 1f;
            }

            var startIndex = curveIndex * SegmentResolution;

            if (SegmentResolution < 1 || curveDistance <= 0)
            {
                return 0f;
            }

            var cl = this.SegmentLengthsLookupTable[startIndex + SegmentResolution - 1].Distance;

            if (curveDistance >= cl)
            {
                return 1f;
            }

            var prev = this.SegmentLengthsLookupTable[startIndex];

            for (var i = 1; i < SegmentResolution; i++)
            {
                var current = this.SegmentLengthsLookupTable[startIndex + i];
                if (curveDistance < current.Distance)
                {
                    return math.lerp(prev.T, current.T, (curveDistance - prev.Distance) / (current.Distance - prev.Distance));
                }

                prev = current;
            }

            return 1f;
        }

        private float3 CalculateUpVector(int curveIndex, float curveT)
        {
            if (this.Count < 1)
            {
                return float3.zero;
            }

            var curve = this.GetCurve(curveIndex);

            var curveStartRotation = this[curveIndex].Rotation;
            var curveStartUp = math.rotate(curveStartRotation, math.up());
            if (curveT == 0f)
            {
                return curveStartUp;
            }

            var endKnotIndex = this.NextIndex(curveIndex);
            var curveEndRotation = this[endKnotIndex].Rotation;
            var curveEndUp = math.rotate(curveEndRotation, math.up());
            if (math.abs(curveT - 1f) < math.EPSILON)
            {
                return curveEndUp;
            }

            var up = EvaluateUpVector(curve, curveT, curveStartUp, curveEndUp);

            return up;
        }

        private static void EvaluateUpVectors(BezierCurve curve, float3 startUp, float3 endUp, NativeArray<float3> upVectors)
        {
            upVectors[0] = startUp;
            upVectors[^1] = endUp;

            for (var i = 1; i < upVectors.Length - 1; i++)
            {
                var curveT = i / (float)(upVectors.Length - 1);
                upVectors[i] = EvaluateUpVector(curve, curveT, upVectors[0], endUp);
            }
        }

        private static float3 EvaluateUpVector(BezierCurve curve, float t, float3 startUp, float3 endUp, bool fixEndUpMismatch = true)
        {
            // Ensure we have workable tangents by linearizing ones that are of zero length
            var linearTangentLen = math.length(GetExplicitLinearTangent(curve.P0, curve.P3));
            var linearTangentOut = math.normalize(curve.P3 - curve.P0) * linearTangentLen;
            if (Approximately(math.length(curve.P1 - curve.P0), 0f))
            {
                curve.P1 = curve.P0 + linearTangentOut;
            }

            if (Approximately(math.length(curve.P2 - curve.P3), 0f))
            {
                curve.P2 = curve.P3 - linearTangentOut;
            }

            var normalBuffer = new NativeArray<float3>(NormalsPerCurve, Allocator.Temp);

            // Construct initial frenet frame
            FrenetFrame frame;
            frame.Origin = curve.P0;
            frame.Tangent = curve.P1 - curve.P0;
            frame.Normal = startUp;
            frame.Binormal = math.normalize(math.cross(frame.Tangent, frame.Normal));

            // SPLB-185 : If the tangent and normal are parallel, we can't construct a valid frame
            // rather than returning a value based on startUp and endUp, we return a zero vector
            // to indicate that this is not a valid up vector.
            if (float.IsNaN(frame.Binormal.x))
            {
                return float3.zero;
            }

            normalBuffer[0] = frame.Normal;

            // Continue building remaining rotation minimizing frames
            var stepSize = 1f / (NormalsPerCurve - 1);
            var currentT = stepSize;
            var prevT = 0f;
            var upVector = float3.zero;
            for (var i = 1; i < NormalsPerCurve; ++i)
            {
                var prevFrame = frame;
                frame = GetNextRotationMinimizingFrame(curve, prevFrame, currentT);

                normalBuffer[i] = frame.Normal;

                if (prevT <= t && currentT >= t)
                {
                    var lerpT = (t - prevT) / stepSize;
                    upVector = Vector3.Slerp(prevFrame.Normal, frame.Normal, lerpT);
                }

                prevT = currentT;
                currentT += stepSize;
            }

            if (!fixEndUpMismatch)
            {
                return upVector;
            }

            if (prevT <= t && currentT >= t)
            {
                upVector = endUp;
            }

            var lastFrameNormal = normalBuffer[NormalsPerCurve - 1];

            var angleBetweenNormals = math.acos(math.clamp(math.dot(lastFrameNormal, endUp), -1f, 1f));
            if (angleBetweenNormals == 0f)
            {
                return upVector;
            }

            // Since there's an angle difference between the end knot's normal and the last evaluated frenet frame's normal,
            // the remaining code gradually applies the angle delta across the evaluated frames' normals.
            var lastNormalTangent = math.normalize(frame.Tangent);
            var positiveRotation = quaternion.AxisAngle(lastNormalTangent, angleBetweenNormals);
            var negativeRotation = quaternion.AxisAngle(lastNormalTangent, -angleBetweenNormals);
            var positiveRotationResult = math.acos(math.clamp(math.dot(math.rotate(positiveRotation, endUp), lastFrameNormal), -1f, 1f));
            var negativeRotationResult = math.acos(math.clamp(math.dot(math.rotate(negativeRotation, endUp), lastFrameNormal), -1f, 1f));

            if (positiveRotationResult > negativeRotationResult)
            {
                angleBetweenNormals *= -1f;
            }

            currentT = stepSize;
            prevT = 0f;

            for (var i = 1; i < normalBuffer.Length; i++)
            {
                var normal = normalBuffer[i];
                var adjustmentAngle = math.lerp(0f, angleBetweenNormals, currentT);
                var tangent = math.normalize(EvaluateTangent(curve, currentT));
                var adjustedNormal = math.rotate(quaternion.AxisAngle(tangent, -adjustmentAngle), normal);

                normalBuffer[i] = adjustedNormal;

                // Early exit if we've already adjusted the normals at offsets that curveT is in between
                if (prevT <= t && currentT >= t)
                {
                    var lerpT = (t - prevT) / stepSize;
                    upVector = Vector3.Slerp(normalBuffer[i - 1], normalBuffer[i], lerpT);

                    return upVector;
                }

                prevT = currentT;
                currentT += stepSize;
            }

            return endUp;
        }

        /// <summary>
        /// Mathf.Approximately is not working when using BurstCompile, causing NaN values in the EvaluateUpVector
        /// method when tangents have a 0 length. Using this method instead fixes that.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Approximately(float a, float b)
        {
            const float kEpsilon = 0.0001f;

            // Reusing Mathf.Approximately code
            return math.abs(b - a) < math.max(0.000001f * math.max(math.abs(a), math.abs(b)), kEpsilon * 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetExplicitLinearTangent(float3 point, float3 to)
        {
            return (to - point) / 3.0f;
        }

        private static FrenetFrame GetNextRotationMinimizingFrame(BezierCurve curve, FrenetFrame previousRMFrame, float nextRMFrameT)
        {
            // Evaluate position and tangent for next RM frame
            var nextRMFrame = new FrenetFrame
            {
                Origin = EvaluatePosition(curve, nextRMFrameT),
                Tangent = EvaluateTangent(curve, nextRMFrameT),
            };

            // Mirror the rotational axis and tangent
            var toCurrentFrame = nextRMFrame.Origin - previousRMFrame.Origin;
            var c1 = math.dot(toCurrentFrame, toCurrentFrame);
            var riL = previousRMFrame.Binormal - (toCurrentFrame * 2f / c1 * math.dot(toCurrentFrame, previousRMFrame.Binormal));
            var tiL = previousRMFrame.Tangent - (toCurrentFrame * 2f / c1 * math.dot(toCurrentFrame, previousRMFrame.Tangent));

            // Compute a more stable binormal
            var v2 = nextRMFrame.Tangent - tiL;
            var c2 = math.dot(v2, v2);

            // Fix binormal's axis
            nextRMFrame.Binormal = math.normalize(riL - (v2 * 2f / c2 * math.dot(v2, riL)));
            nextRMFrame.Normal = math.normalize(math.cross(nextRMFrame.Binormal, nextRMFrame.Tangent));

            return nextRMFrame;
        }

        /// <summary> Given a Bezier curve, return an interpolated position at ratio t. </summary>
        /// <param name="curve">A cubic Bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A position on the curve.</returns>
        private static float3 EvaluatePosition(BezierCurve curve, float t)
        {
            t = math.clamp(t, 0, 1);
            var t2 = t * t;
            var t3 = t2 * t;
            var position = (curve.P0 * ((-1f * t3) + (3f * t2) - (3f * t) + 1f)) + (curve.P1 * ((3f * t3) - (6f * t2) + (3f * t))) +
                (curve.P2 * ((-3f * t3) + (3f * t2))) + (curve.P3 * t3);

            return position;
        }

        /// <summary> Given a Bezier curve, return an interpolated tangent at ratio t. </summary>
        /// <param name="curve">A cubic Bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A tangent on the curve.</returns>
        private static float3 EvaluateTangent(BezierCurve curve, float t)
        {
            t = math.clamp(t, 0, 1);
            var t2 = t * t;

            var tangent = (curve.P0 * ((-3f * t2) + (6f * t) - 3f)) + (curve.P1 * ((9f * t2) - (12f * t) + 3f)) + (curve.P2 * ((-9f * t2) + (6f * t))) +
                (curve.P3 * (3f * t2));

            return tangent;
        }

        public int NextIndex(int index)
        {
            return NextIndex(index, this.Count, this.Closed);
        }

        private static int NextIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + 1) % count : math.min(index + 1, count - 1);
        }

        private struct FrenetFrame
        {
            public float3 Origin;
            public float3 Tangent;
            public float3 Normal;
            public float3 Binormal;
        }
    }
}
#endif