namespace BovineLabs.Core.Tests.Facet
{
    using BovineLabs.Core;

    public partial struct FacetComposition : IFacet
    {
        [Facet]
        private TestFacet _testFacet;
    }
}
