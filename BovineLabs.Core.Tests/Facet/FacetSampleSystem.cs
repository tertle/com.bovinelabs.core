// <copyright file="FacetSampleSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    public partial struct FacetSampleSystem : ISystem
    {
        private TestFacet.Lookup facetLookup;
        private TestFacet.TypeHandle facetHandle;
        private EntityQuery facetQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.facetLookup.Create(ref state);
            this.facetHandle.Create(ref state);
            this.facetQuery = TestFacet.CreateQueryBuilder().Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.facetLookup.Update(ref state);
            this.facetHandle.Update(ref state);

            state.Dependency = new JobChunk { FacetHandle = this.facetHandle }.ScheduleParallel(this.facetQuery, state.Dependency);
            state.Dependency = new JobEntity().ScheduleParallel(state.Dependency);
            state.Dependency = new JobLookup { FacetLookup = this.facetLookup, }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct JobChunk : IJobChunk
        {
            public TestFacet.TypeHandle FacetHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var resolved = this.FacetHandle.Resolve(chunk);

                for (var i = 0; i < chunk.Count; i++)
                {
                    TestFacet facet = resolved[i];
                }
            }
        }

        [BurstCompile]
        private partial struct JobEntity : IJobEntity
        {
            private static void Execute(RefRW<ComponentA> compA, RefRO<ComponentB> compB, EnabledRefRO<EnabledA> enableA, DynamicBuffer<BufferA> bufferA)
            {
                TestFacet facet = new TestFacet(compA, compB, enableA, bufferA);
            }
        }

        [BurstCompile]
        private partial struct JobLookup : IJobEntity
        {
            public TestFacet.Lookup FacetLookup;

            private void Execute(in ReferenceComponent comp)
            {
                if (this.FacetLookup.TryGet(comp.FacetEntity, out TestFacet facet))
                {
                }

                TestFacet facet2 = this.FacetLookup[comp.FacetEntity];
            }
        }

        private struct ReferenceComponent : IComponentData
        {
            public Entity FacetEntity;
        }
    }
}
