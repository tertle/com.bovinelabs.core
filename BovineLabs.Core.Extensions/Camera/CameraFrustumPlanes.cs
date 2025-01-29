// <copyright file="CameraFrustumPlanes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;
    using Unity.Mathematics;

    [SuppressMessage("ReSharper", "UnassignedField.Global", Justification = "Reinterpret")]
    public unsafe struct CameraFrustumPlanes : IComponentData, IEquatable<CameraFrustumPlanes>
    {
        public float4 Left;
        public float4 Right;
        public float4 Bottom;
        public float4 Top;
        public float4 Near;
        public float4 Far;

        public bool IsDefault => this.Equals(default);

        public float4 this[int index]
        {
            get
            {
                CheckRange(index);
                fixed (CameraFrustumPlanes* v = &this)
                {
                    return ((float4*)v)[index];
                }
            }

            set
            {
                CheckRange(index);
                fixed (CameraFrustumPlanes* v = &this)
                {
                    ((float4*)v)[index] = value;
                }
            }
        }

        public bool Equals(CameraFrustumPlanes other)
        {
            return this.Left.Equals(other.Left) &&
                this.Right.Equals(other.Right) &&
                this.Bottom.Equals(other.Bottom) &&
                this.Top.Equals(other.Top) &&
                this.Near.Equals(other.Near) &&
                this.Far.Equals(other.Far);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Left.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Right.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Top.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Near.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Far.GetHashCode();
                return hashCode;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckRange(int index)
        {
            if (index is < 0 or >= 6)
            {
                throw new IndexOutOfRangeException("Frustum Planes must be in range of 6");
            }
        }
    }
}
#endif
