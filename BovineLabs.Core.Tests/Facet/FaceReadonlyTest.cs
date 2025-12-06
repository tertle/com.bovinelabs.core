// <copyright file="FaceReadonlyTest.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Collections;
    using Unity.Entities;

    public readonly partial struct FaceReadonlyTest : IFacet
    {
        private readonly RefRO<ComponentA> compA;
        private readonly RefRW<ComponentB> compB;
        [FacetOptional] private readonly RefRO<ComponentC> compC;
        [FacetOptional] private readonly RefRW<ComponentD> compD;
        private readonly EnabledRefRO<EnabledA> enableA;
        private readonly EnabledRefRW<EnabledB> enableB;
        [FacetOptional] private readonly EnabledRefRO<EnabledC> enableC;
        [FacetOptional] private readonly EnabledRefRW<EnabledD> enableD;
        [ReadOnly] private readonly DynamicBuffer<BufferA> bufferA;
        private readonly DynamicBuffer<BufferB> bufferB;
        [FacetOptional] [ReadOnly] private readonly DynamicBuffer<BufferC> bufferC;
        [FacetOptional] private readonly DynamicBuffer<BufferD> bufferD;
    }
}
