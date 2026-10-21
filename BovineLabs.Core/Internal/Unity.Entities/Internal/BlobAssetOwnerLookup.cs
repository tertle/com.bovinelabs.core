namespace BovineLabs.Core.Internal
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    public struct BlobAssetOwnerLookup
    {
        private SharedComponentLookup<Unity.Entities.BlobAssetOwner> _lookup;

        public BlobAssetOwnerLookup(ref SystemState state)
        {
            _lookup = state.GetSharedComponentLookup<Unity.Entities.BlobAssetOwner>(true);
        }

        public static ComponentType ComponentType => ComponentType.ReadOnly<Unity.Entities.BlobAssetOwner>();

        public void Update(ref SystemState state)
        {
            _lookup.Update(ref state);
        }

        public bool TryGetComponent(Entity entity, out BlobAssetOwner owner)
        {
            if (_lookup.TryGetComponent(entity, out var value))
            {
                owner = new BlobAssetOwner(value);
                return true;
            }

            owner = default;
            return false;
        }
    }
}
