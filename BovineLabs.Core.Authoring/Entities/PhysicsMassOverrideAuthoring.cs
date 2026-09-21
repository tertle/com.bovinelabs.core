#if UNITY_PHYSICS
namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Entities;
    using Unity.Physics;
    using UnityEngine;

    public class PhysicsMassOverrideAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool _isKinematic = true;

        [SerializeField]
        private bool _setVelocityToZero;

        private class Baker : Baker<PhysicsMassOverrideAuthoring>
        {
            public override void Bake(PhysicsMassOverrideAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.None), new PhysicsMassOverride
                {
                    IsKinematic = (byte)(authoring._isKinematic ? 1 : 0),
                    SetVelocityToZero = (byte)(authoring._setVelocityToZero ? 1 : 0),
                });
            }
        }
    }
}
#endif
