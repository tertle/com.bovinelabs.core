// <copyright file="CurveRemapUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using UnityEngine;

    /// <summary> Helper methods for remapping animation curves into clip-local space. </summary>
    public static class CurveRemapUtility
    {
        public static bool TryRemapToClipLength(AnimationCurve curve, float clipIn, float clipDuration, out AnimationCurve remappedCurve)
        {
            remappedCurve = null;

            if (curve == null || clipDuration <= Mathf.Epsilon)
            {
                return false;
            }

            if (!IsClampWrapMode(curve))
            {
                return false;
            }

            var keyCount = curve.length;
            if (keyCount == 0)
            {
                return false;
            }

            using var keyBuffer = new NativeArray<Keyframe>(keyCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var sourceKeys = keyBuffer.AsSpan();
            curve.GetKeys(sourceKeys);

            var firstTime = sourceKeys[0].time;
            var lastTime = sourceKeys[^1].time;
            var sourceDuration = lastTime - firstTime;

            if (Mathf.Approximately(sourceDuration, 0f))
            {
                for (var i = 0; i < sourceKeys.Length; i++)
                {
                    var key = sourceKeys[i];
                    key.time = clipIn;
                    sourceKeys[i] = key;
                }
            }
            else
            {
                var timeScale = clipDuration / sourceDuration;
                for (var i = 0; i < sourceKeys.Length; i++)
                {
                    var key = sourceKeys[i];
                    key.time = clipIn + ((key.time - firstTime) * timeScale);

                    if (!float.IsInfinity(key.inTangent))
                    {
                        key.inTangent /= timeScale;
                    }

                    if (!float.IsInfinity(key.outTangent))
                    {
                        key.outTangent /= timeScale;
                    }

                    sourceKeys[i] = key;
                }
            }

            remappedCurve = new AnimationCurve
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode,
            };
            remappedCurve.SetKeys(sourceKeys);

            return true;
        }

        public static bool IsClampWrapMode(AnimationCurve curve)
        {
            return curve != null && IsClamp(curve.preWrapMode) && IsClamp(curve.postWrapMode);
        }

        private static bool IsClamp(WrapMode mode)
        {
            return mode is WrapMode.Clamp or WrapMode.ClampForever or WrapMode.Default;
        }
    }
}
