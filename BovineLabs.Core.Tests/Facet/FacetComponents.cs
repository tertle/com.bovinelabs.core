// <copyright file="FacetComponents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable SA1649

namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Entities;

    public readonly partial struct TestFacet : IFacet
    {
        private readonly RefRW<ComponentA> compA;
        private readonly RefRO<ComponentB> compB;
        private readonly EnabledRefRO<EnabledA> enableA;
        private readonly DynamicBuffer<BufferA> bufferA;

        public partial struct Lookup {}
    }

    public struct ComponentA : IComponentData
    {
        public int Value;
    }

    public struct ComponentB : IComponentData
    {
        public int Value;
    }

    public struct ComponentC : IComponentData
    {
        public int Value;
    }

    public struct ComponentD : IComponentData
    {
        public int Value;
    }

    public struct EnabledA : IComponentData, IEnableableComponent
    {
    }

    public struct EnabledB : IComponentData, IEnableableComponent
    {
    }

    public struct EnabledC : IComponentData, IEnableableComponent
    {
    }

    public struct EnabledD : IComponentData, IEnableableComponent
    {
    }

    public struct BufferA : IBufferElementData
    {
        public int Value;
    }

    public struct BufferB : IBufferElementData
    {
        public int Value;
    }

    public struct BufferC : IBufferElementData
    {
        public int Value;
    }

    public struct BufferD : IBufferElementData
    {
        public int Value;
    }

    public struct SingletonA : IComponentData
    {
        public int Value;
    }

    public struct SingletonB : IBufferElementData
    {
        public int Value;
    }
}
