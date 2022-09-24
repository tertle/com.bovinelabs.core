// <copyright file="PhysicsMassOverrideAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.Physics;
    using UnityEngine;

    public class PhysicsMassOverrideAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private bool isKinematic = true;

        [SerializeField]
        private bool setVelocityToZero;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PhysicsMassOverride
            {
                IsKinematic = (byte)(this.isKinematic ? 1 : 0),
                SetVelocityToZero = (byte)(this.setVelocityToZero ? 1 : 0),
            });
        }
    }
}
#endif
