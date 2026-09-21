namespace BovineLabs.Core.Tests.Facet
{
    public readonly partial struct FacetEnabledRefRWFacet : IFacet
    {
        private readonly FacetEnabledRefRW<EnabledB> _enabled;

        public bool IsValid => _enabled.IsValid;

        public bool ValueRO => _enabled.GetComponentEnabled();

        public void SetEnabled(bool value)
        {
            _enabled.SetComponentEnabled(value);
        }
    }

    public readonly partial struct OptionalFacetEnabledRefRWFacet : IFacet
    {
        [FacetOptional]
        private readonly FacetEnabledRefRW<EnabledB> _enabled;

        public bool IsValid => _enabled.IsValid;
    }

    public readonly partial struct RequiredNestedFacetEnabledRefRWFacet : IFacet
    {
        [Facet]
        private readonly FacetEnabledRefRWFacet _enabled;
    }

    public readonly partial struct OptionalNestedFacetEnabledRefRWFacet : IFacet
    {
        [FacetOptional]
        [Facet]
        private readonly FacetEnabledRefRWFacet _enabled;
    }

    public readonly partial struct BufferFacetEnabledRefRWFacet : IFacet
    {
        private readonly FacetEnabledRefRW<EnabledBufferElement> _enabled;

        public bool IsValid => _enabled.IsValid;

        public bool ValueRO => _enabled.GetComponentEnabled();

        public void SetEnabled(bool value)
        {
            _enabled.SetComponentEnabled(value);
        }
    }

    public struct EnabledBufferElement : Unity.Entities.IBufferElementData, Unity.Entities.IEnableableComponent
    {
        public int Value;
    }
}
