namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Collections;
    using Unity.Entities;

    public partial struct FacetTest : IFacet
    {
        private Entity _entity;
        private RefRO<ComponentA> _compA;
        private RefRW<ComponentB> _compB;
        [FacetOptional] private RefRO<ComponentC> _compC;
        [FacetOptional] private RefRW<ComponentD> _compD;
        private EnabledRefRO<EnabledA> _enableA;
        private FacetEnabledRefRW<EnabledB> _enableB;
        [FacetOptional] private EnabledRefRO<EnabledC> _enableC;
        [FacetOptional] private FacetEnabledRefRW<EnabledD> _enableD;
        [ReadOnly] private DynamicBuffer<BufferA> _bufferA;
        private DynamicBuffer<BufferB> _bufferB;
        [FacetOptional] [ReadOnly] private DynamicBuffer<BufferC> _bufferC;
        [FacetOptional] private DynamicBuffer<BufferD> _bufferD;
        [ReadOnly][Singleton] private SingletonA _singletonA;
        [ReadOnly][Singleton] private DynamicBuffer<SingletonB> _singletonB;
        [Facet] private Facet2Test _facet2;
        [FacetOptional] [Facet] private Facet3Test _facet3;
    }

    public partial struct Facet2Test : IFacet
    {
        [Singleton]
        public SingletonComponent SingletonComponent;
    }

    public partial struct Facet3Test : IFacet
    {
        [Singleton]
        [ReadOnly]
        public DynamicBuffer<SingletonBuffer> SingletonBuffer;
    }

    public struct SingletonComponent : IComponentData {}
    public struct SingletonBuffer : IBufferElementData{}
}
