// <copyright file="FacetEnabledRefRWFacet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    public readonly partial struct FacetEnabledRefRWFacet : IFacet
    {
        private readonly FacetEnabledRefRW<EnabledB> enabled;

        public bool IsValid => this.enabled.IsValid;

        public bool ValueRO => this.enabled.GetComponentEnabled();

        public void SetEnabled(bool value)
        {
            this.enabled.SetComponentEnabled(value);
        }
    }

    public readonly partial struct OptionalFacetEnabledRefRWFacet : IFacet
    {
        [FacetOptional]
        private readonly FacetEnabledRefRW<EnabledB> enabled;

        public bool IsValid => this.enabled.IsValid;
    }

    public readonly partial struct RequiredNestedFacetEnabledRefRWFacet : IFacet
    {
        [Facet]
        private readonly FacetEnabledRefRWFacet enabled;
    }

    public readonly partial struct OptionalNestedFacetEnabledRefRWFacet : IFacet
    {
        [FacetOptional]
        [Facet]
        private readonly FacetEnabledRefRWFacet enabled;
    }

    public readonly partial struct BufferFacetEnabledRefRWFacet : IFacet
    {
        private readonly FacetEnabledRefRW<EnabledBufferElement> enabled;

        public bool IsValid => this.enabled.IsValid;

        public bool ValueRO => this.enabled.GetComponentEnabled();

        public void SetEnabled(bool value)
        {
            this.enabled.SetComponentEnabled(value);
        }
    }

    public struct EnabledBufferElement : Unity.Entities.IBufferElementData, Unity.Entities.IEnableableComponent
    {
        public int Value;
    }
}
