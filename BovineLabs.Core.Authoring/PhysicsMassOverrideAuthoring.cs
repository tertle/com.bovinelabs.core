// <copyright file="PhysicsMassOverrideAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring
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

        public bool IsKinematic => this.isKinematic;

        public bool SetVelocityToZero => this.setVelocityToZero;
    }

    public class PhysicsMassOverrideBaker : Baker<PhysicsMassOverrideAuthoring>
    {
        public override void Bake(PhysicsMassOverrideAuthoring authoring)
        {
            this.AddComponent(new PhysicsMassOverride
            {
                IsKinematic = (byte)(authoring.IsKinematic ? 1 : 0),
                SetVelocityToZero = (byte)(authoring.SetVelocityToZero ? 1 : 0),
            });
        }
    }
}
#endif
