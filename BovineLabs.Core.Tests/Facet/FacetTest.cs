// <copyright file="FacetTest.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Collections;
    using Unity.Entities;

    public partial struct FacetTest : IFacet
    {
        private Entity entity;
        private RefRO<ComponentA> compA;
        private RefRW<ComponentB> compB;
        [FacetOptional] private RefRO<ComponentC> compC;
        [FacetOptional] private RefRW<ComponentD> compD;
        private EnabledRefRO<EnabledA> enableA;
        private EnabledRefRW<EnabledB> enableB;
        [FacetOptional] private EnabledRefRO<EnabledC> enableC;
        [FacetOptional] private EnabledRefRW<EnabledD> enableD;
        [ReadOnly] private DynamicBuffer<BufferA> bufferA;
        private DynamicBuffer<BufferB> bufferB;
        [FacetOptional] [ReadOnly] private DynamicBuffer<BufferC> bufferC;
        [FacetOptional] private DynamicBuffer<BufferD> bufferD;
        [Singleton] private SingletonA singletonA;
        [Singleton] private DynamicBuffer<SingletonB> singletonB;
        [Facet] private Facet2Test facet2;
        [FacetOptional] [Facet] private Facet3Test facet3;

        public partial struct Lookup {} // Optional but this allows using the lookup within an IJobEntity
    }

    public partial struct Facet2Test : IFacet
    {
        [Singleton]
        public SingletonComponent SingletonComponent;
    }

    public partial struct Facet3Test : IFacet
    {
        [Singleton]
        public SingletonBuffer SingletonBuffer;
    }

    public struct SingletonComponent : IComponentData {}
    public struct SingletonBuffer : IBufferElementData{}
}
