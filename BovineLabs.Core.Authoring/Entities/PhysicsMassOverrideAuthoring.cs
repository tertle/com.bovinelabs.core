// <copyright file="PhysicsMassOverrideAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Entities;
    using Unity.Physics;
    using UnityEngine;

    public class PhysicsMassOverrideAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool isKinematic = true;

        [SerializeField]
        private bool setVelocityToZero;

        private class Baker : Baker<PhysicsMassOverrideAuthoring>
        {
            public override void Bake(PhysicsMassOverrideAuthoring authoring)
            {
                this.AddComponent(this.GetEntity(TransformUsageFlags.None), new PhysicsMassOverride
                {
                    IsKinematic = (byte)(authoring.isKinematic ? 1 : 0),
                    SetVelocityToZero = (byte)(authoring.setVelocityToZero ? 1 : 0),
                });
            }
        }
    }
}
#endif
