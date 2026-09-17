namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using UnityEngine;

    public class TransformAuthoring : MonoBehaviour
    {
        public TransformUsageFlags TransformUsageFlags = TransformUsageFlags.Dynamic;
    }

    public class TransformBaker : Baker<TransformAuthoring>
    {
        public override void Bake(TransformAuthoring authoring)
        {
            this.GetEntity(authoring.TransformUsageFlags);
        }
    }
}
