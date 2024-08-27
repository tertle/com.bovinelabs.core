// <copyright file="RayExtension.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Core.Extensions
{
    using Unity.Mathematics;
    using Unity.Physics;

    public static class RayExtension
    {
        public static float3 ReciprocalDisplacement(this Ray ray)
        {
            return ray.ReciprocalDisplacement;
        }
    }
}
#endif
