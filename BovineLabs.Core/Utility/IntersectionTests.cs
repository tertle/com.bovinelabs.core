// <copyright file="IntersectionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Mathematics;

    public static class IntersectionTests
    {
        // https://github.com/juj/MathGeoLib/blob/master/src/Geometry/Triangle.cpp#L630
        public static bool AABBTriangle(MinMaxAABB aabb, float3 a, float3 b, float3 c)
        {
            var tMin = math.min(a, math.min(b, c));
            var tMax = math.max(a, math.max(b, c));

            if (tMin.x >= aabb.Max.x || tMax.x <= aabb.Min.x || tMin.y >= aabb.Max.y || tMax.y <= aabb.Min.y || tMin.z >= aabb.Max.z || tMax.z <= aabb.Min.z)
            {
                return false;
            }

            var center = (aabb.Min + aabb.Max) * 0.5f;
            var h = aabb.Max - center;

            Span<float3> t = stackalloc float3[3] { b - a, c - a, c - b };

            var ac = a - center;

            var n = math.cross(t[0], t[1]);
            var s = math.dot(n, ac);
            var r = math.abs(math.dot(h, math.abs(n)));

            if (math.abs(s) >= r)
            {
                return false;
            }

            Span<float3> at = stackalloc float3[3] { math.abs(t[0]), math.abs(t[1]), math.abs(t[2]) };

            var bc = b - center;
            var cc = c - center;

            // eX <cross> t[0]
            var d1 = (t[0].y * ac.z) - (t[0].z * ac.y);
            var d2 = (t[0].y * cc.z) - (t[0].z * cc.y);
            var tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[0].z) + (h.z * at[0].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eX <cross> t[1]
            d1 = (t[1].y * ac.z) - (t[1].z * ac.y);
            d2 = (t[1].y * bc.z) - (t[1].z * bc.y);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[1].z) + (h.z * at[1].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eX <cross> t[2]
            d1 = (t[2].y * ac.z) - (t[2].z * ac.y);
            d2 = (t[2].y * bc.z) - (t[2].z * bc.y);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[2].z) + (h.z * at[2].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[0]
            d1 = (t[0].z * ac.x) - (t[0].x * ac.z);
            d2 = (t[0].z * cc.x) - (t[0].x * cc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[0].z) + (h.z * at[0].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[1]
            d1 = (t[1].z * ac.x) - (t[1].x * ac.z);
            d2 = (t[1].z * bc.x) - (t[1].x * bc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[1].z) + (h.z * at[1].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[2]
            d1 = (t[2].z * ac.x) - (t[2].x * ac.z);
            d2 = (t[2].z * bc.x) - (t[2].x * bc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[2].z) + (h.z * at[2].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[0]
            d1 = (t[0].x * ac.y) - (t[0].y * ac.x);
            d2 = (t[0].x * cc.y) - (t[0].y * cc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[0].x) + (h.x * at[0].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[1]
            d1 = (t[1].x * ac.y) - (t[1].y * ac.x);
            d2 = (t[1].x * bc.y) - (t[1].y * bc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[1].x) + (h.x * at[1].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[2]
            d1 = (t[2].x * ac.y) - (t[2].y * ac.x);
            d2 = (t[2].x * bc.y) - (t[2].y * bc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[2].x) + (h.x * at[2].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // No separating axis exists, the AABB and triangle intersect.
            return true;
        }

        // // https://github.com/juj/MathGeoLib/blob/master/src/Geometry/Triangle.cpp#L630
        // public static bool AABBTriangleSse(MinMaxAABB aabb, float3 a, float3 b, float3 c)
        // {
        //     if (!X86.Sse.IsSseSupported)
        //     {
        //         return AABBTriangle(aabb, a, b, c);
        //     }
        //
        //     var min = Load(aabb.Min);
        //     var max = Load(aabb.Max);
        //
        //     var av = Load(a);
        //     var bv = Load(b);
        //     var cv = Load(c);
        //
        //     var tMin = X86.Sse.min_ps(av, X86.Sse.min_ps(bv, cv));
        //     var tMax = X86.Sse.max_ps(av, X86.Sse.max_ps(bv, cv));
        //
        //     var cmp = X86.Sse.or_ps(X86.Sse.cmpge_ps(tMin, max), X86.Sse.cmple_ps(tMax, min));
        //     if ((X86.Sse.movemask_ps(cmp) & 0x7) != 0)
        //     {
        //         return false;
        //     }
        //
        //     var half = X86.Sse.set1_ps(0.5f);
        //     var center = X86.Sse.mul_ps(X86.Sse.add_ps(min, max), half);
        //     var h = X86.Sse.sub_ps(max, center);
        //
        //     var t0 = X86.Sse.sub_ps(bv, av);
        //     var t1 = X86.Sse.sub_ps(cv, av);
        //     var t2 = X86.Sse.sub_ps(cv, bv);
        //
        //     var ac = X86.Sse.sub_ps(av, center);
        //     var bc = X86.Sse.sub_ps(bv, center);
        //     var cc = X86.Sse.sub_ps(cv, center);
        //
        //     var n = Cross3(t0, t1);
        //     var s = Dot3(n, ac);
        //     var rScalar = math.abs(Dot3(h, Abs(n)));
        //
        //     if (math.abs(s) >= rScalar)
        //     {
        //         return false;
        //     }
        //
        //     var acZxyw = ShuffleZxyw(ac);
        //     var acYzxw = ShuffleYzxw(ac);
        //     var hZxyw = ShuffleZxyw(h);
        //     var hYzxw = ShuffleYzxw(h);
        //
        //     var bcZxyw = ShuffleZxyw(bc);
        //     var bcYzxw = ShuffleYzxw(bc);
        //     var ccZxyw = ShuffleZxyw(cc);
        //     var ccYzxw = ShuffleYzxw(cc);
        //
        //     var t1Zxyw = ShuffleZxyw(t1);
        //     var t1Yzxw = ShuffleYzxw(t1);
        //     var at1 = Abs(t1);
        //     var at1Zxyw = ShuffleZxyw(at1);
        //     var at1Yzxw = ShuffleYzxw(at1);
        //
        //     var d1 = Msub(t1Yzxw, acZxyw, X86.Sse.mul_ps(t1Zxyw, acYzxw));
        //     var d2 = Msub(t1Yzxw, bcZxyw, X86.Sse.mul_ps(t1Zxyw, bcYzxw));
        //     var tc = X86.Sse.mul_ps(X86.Sse.add_ps(d1, d2), half);
        //     var r = Abs(Madd(hZxyw, at1Yzxw, X86.Sse.mul_ps(hYzxw, at1Zxyw)));
        //
        //     if (AxisSeparates(r, tc, d1))
        //     {
        //         return false;
        //     }
        //
        //     var t2Zxyw = ShuffleZxyw(t2);
        //     var t2Yzxw = ShuffleYzxw(t2);
        //     var at2 = Abs(t2);
        //     var at2Zxyw = ShuffleZxyw(at2);
        //     var at2Yzxw = ShuffleYzxw(at2);
        //
        //     d1 = Msub(t2Yzxw, acZxyw, X86.Sse.mul_ps(t2Zxyw, acYzxw));
        //     d2 = Msub(t2Yzxw, bcZxyw, X86.Sse.mul_ps(t2Zxyw, bcYzxw));
        //     tc = X86.Sse.mul_ps(X86.Sse.add_ps(d1, d2), half);
        //     r = Abs(Madd(hZxyw, at2Yzxw, X86.Sse.mul_ps(hYzxw, at2Zxyw)));
        //
        //     if (AxisSeparates(r, tc, d1))
        //     {
        //         return false;
        //     }
        //
        //     var t0Zxyw = ShuffleZxyw(t0);
        //     var t0Yzxw = ShuffleYzxw(t0);
        //     var at0 = Abs(t0);
        //     var at0Zxyw = ShuffleZxyw(at0);
        //     var at0Yzxw = ShuffleYzxw(at0);
        //
        //     d1 = Msub(t0Yzxw, acZxyw, X86.Sse.mul_ps(t0Zxyw, acYzxw));
        //     d2 = Msub(t0Yzxw, ccZxyw, X86.Sse.mul_ps(t0Zxyw, ccYzxw));
        //     tc = X86.Sse.mul_ps(X86.Sse.add_ps(d1, d2), half);
        //     r = Abs(Madd(hZxyw, at0Yzxw, X86.Sse.mul_ps(hYzxw, at0Zxyw)));
        //
        //     return !AxisSeparates(r, tc, d1);
        // }
        //
        // private static v128 Load(float3 value)
        // {
        //     var result = default(v128);
        //     result.Float0 = value.x;
        //     result.Float1 = value.y;
        //     result.Float2 = value.z;
        //     result.Float3 = 0f;
        //     return result;
        // }
        //
        // private static v128 Abs(v128 value)
        // {
        //     var mask = default(v128);
        //     mask.UInt0 = 0x7FFFFFFFu;
        //     mask.UInt1 = 0x7FFFFFFFu;
        //     mask.UInt2 = 0x7FFFFFFFu;
        //     mask.UInt3 = 0x7FFFFFFFu;
        //     return X86.Sse.and_ps(value, mask);
        // }
        //
        // private static float Dot3(v128 a, v128 b)
        // {
        //     var mul = X86.Sse.mul_ps(a, b);
        //     return (mul.Float0 + mul.Float1) + mul.Float2;
        // }
        //
        // private static v128 Cross3(v128 a, v128 b)
        // {
        //     var aYzxw = ShuffleYzxw(a);
        //     var bZxyw = ShuffleZxyw(b);
        //     var aZxyw = ShuffleZxyw(a);
        //     var bYzxw = ShuffleYzxw(b);
        //     return X86.Sse.sub_ps(X86.Sse.mul_ps(aYzxw, bZxyw), X86.Sse.mul_ps(aZxyw, bYzxw));
        // }
        //
        // private static v128 Madd(v128 a, v128 b, v128 c)
        // {
        //     return X86.Sse.add_ps(X86.Sse.mul_ps(a, b), c);
        // }
        //
        // private static v128 Msub(v128 a, v128 b, v128 c)
        // {
        //     return X86.Sse.sub_ps(X86.Sse.mul_ps(a, b), c);
        // }
        //
        // private static v128 ShuffleZxyw(v128 value)
        // {
        //     return X86.Sse.shuffle_ps(value, value, X86.Sse.SHUFFLE(3, 1, 0, 2));
        // }
        //
        // private static v128 ShuffleYzxw(v128 value)
        // {
        //     return X86.Sse.shuffle_ps(value, value, X86.Sse.SHUFFLE(3, 0, 2, 1));
        // }
        //
        // private static bool AxisSeparates(v128 r, v128 tc, v128 d1)
        // {
        //     var lhs = X86.Sse.add_ps(r, Abs(X86.Sse.sub_ps(tc, d1)));
        //     var cmp = X86.Sse.cmplt_ps(lhs, Abs(tc));
        //     return (X86.Sse.movemask_ps(cmp) & 0x7) != 0;
        // }
    }
}
