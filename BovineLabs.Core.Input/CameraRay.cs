// <copyright file="CameraRay.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using JetBrains.Annotations;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// A clone of Unity.Physics ray that can be used without reference to Unity physics with implicit conversion to both Physics and UnityEngine rays..
    /// </summary>
    public struct CameraRay
    {
        /// <summary>
        /// The Origin point of the Ray in query space.
        /// </summary>
        /// <value> Point vector coordinate. </value>
        public float3 Origin;

        private float3 displacement;

        [UsedImplicitly]
        internal float3 ReciprocalDisplacement { get; private set; }

        /// <summary>
        /// This represents the line from the Ray's Origin to a second point on the Ray. The second point will be the Ray End if nothing is hit.
        /// </summary>
        public float3 Displacement
        {
            get => this.displacement;
            set
            {
                this.displacement = value;
                this.ReciprocalDisplacement = math.select(math.rcp(this.displacement), math.sqrt(float.MaxValue), this.displacement == float3.zero);
            }
        }

        public static implicit operator CameraRay(UnityEngine.Ray ray)
        {
            return new CameraRay
            {
                Origin = ray.origin,
                Displacement = ray.direction,
            };
        }

        public static implicit operator UnityEngine.Ray(CameraRay ray)
        {
            return new UnityEngine.Ray(ray.Origin, math.normalize(ray.Displacement));
        }

#if UNITY_PHYSICS
        public static implicit operator CameraRay(Unity.Physics.Ray ray)
        {
            return UnsafeUtility.As<Unity.Physics.Ray, CameraRay>(ref ray);
        }

        public static implicit operator Unity.Physics.Ray(CameraRay ray)
        {
            return UnsafeUtility.As<CameraRay, Unity.Physics.Ray>(ref ray);
        }
#endif
    }
}
