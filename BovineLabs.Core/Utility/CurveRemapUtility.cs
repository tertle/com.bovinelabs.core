// <copyright file="CurveRemapUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
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

            var sourceKeys = curve.keys;
            if (sourceKeys.Length == 0)
            {
                return false;
            }

            var firstTime = sourceKeys[0].time;
            var lastTime = sourceKeys[^1].time;
            var sourceDuration = lastTime - firstTime;

            Keyframe[] remappedKeys;
            if (Mathf.Approximately(sourceDuration, 0f))
            {
                remappedKeys = new Keyframe[sourceKeys.Length];
                for (var i = 0; i < sourceKeys.Length; i++)
                {
                    var key = sourceKeys[i];
                    key.time = clipIn;
                    remappedKeys[i] = key;
                }
            }
            else
            {
                var timeScale = clipDuration / sourceDuration;
                remappedKeys = new Keyframe[sourceKeys.Length];
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

                    remappedKeys[i] = key;
                }
            }

            remappedCurve = new AnimationCurve(remappedKeys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode,
            };

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
