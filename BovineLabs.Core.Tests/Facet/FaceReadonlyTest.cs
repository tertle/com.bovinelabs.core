namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Collections;
    using Unity.Entities;

    public readonly partial struct FaceReadonlyTest : IFacet
    {
        private readonly RefRO<ComponentA> _compA;
        private readonly RefRW<ComponentB> _compB;
        [FacetOptional] private readonly RefRO<ComponentC> _compC;
        [FacetOptional] private readonly RefRW<ComponentD> _compD;
        private readonly EnabledRefRO<EnabledA> _enableA;
        private readonly FacetEnabledRefRW<EnabledB> _enableB;
        [FacetOptional] private readonly EnabledRefRO<EnabledC> _enableC;
        [FacetOptional] private readonly FacetEnabledRefRW<EnabledD> _enableD;
        [ReadOnly] private readonly DynamicBuffer<BufferA> _bufferA;
        private readonly DynamicBuffer<BufferB> _bufferB;
        [FacetOptional] [ReadOnly] private readonly DynamicBuffer<BufferC> _bufferC;
        [FacetOptional] private readonly DynamicBuffer<BufferD> _bufferD;
    }
}
