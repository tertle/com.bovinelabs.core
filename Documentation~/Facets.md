# Facets

`IFacet` provides an aspect-like workflow built on the Core source generator. Declare the data you want to pull from an entity, and the generator emits the helpers to access it through lookups or chunk iteration.

## Declaring a facet

- Use a `partial struct` that implements `IFacet`.
- Supported fields include `RefRO<T>`, `RefRW<T>`, `EnabledRefRO<T>`, `EnabledRefRW<T>`, `DynamicBuffer<T>`, `Entity`, plain component fields marked with `[Singleton]`, and other facets marked with `[Facet]`.
- Add `[FacetOptional]` to allow a field to be missing on the entity; add `[ReadOnly]` on buffers when you only need read-only access.

```csharp
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
```

### Singleton fields

- Mark a field with `[Singleton]` to inject a single component value into the facet (e.g., configuration or global state).
- Singleton fields are treated as read-only and are not part of the entity query; you must pass the singleton value into `Lookup.Update` and `TypeHandle.Update`.
- Fetch the singleton in your system (e.g., `SystemAPI.GetSingleton<T>()`) and forward it to `Update`.

```csharp
public readonly partial struct TestWriter : IFacet
{
    private readonly RefRW<LocalTransform> localTransform;
    [Singleton] private readonly TestComponent singleton;
}

[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    var singleton = SystemAPI.GetSingleton<TestComponent>();

    this.lookup.Update(ref state, in singleton);
    this.typeHandle.Update(ref state, in singleton);
}
```

### Embedding facets

- Mark a field with `[Facet]` when its type is another `IFacet`. The generator wires the nested `Lookup`, `TypeHandle`, and `ResolvedChunk` so you can access the inner facet alongside your own data.
- `CreateQueryBuilder` does not automatically merge nested facet requirements; build your query by combining both facets’ requirements (e.g., call the nested facet’s builder and add any additional filters you need).

```csharp
public partial struct CompositeFacet : IFacet
{
    [Facet] private TestFacet testFacet; // brings along TestFacet.Lookup/TypeHandle/ResolvedChunk
}
```

### Usage notes

- Use `Lookup.Create(ref state)` once during system creation and `Lookup.Update(ref state)` each update to keep lookups current.
- Use `Lookup.Create(ref state)` once during system creation and `Lookup.Update(ref state)` each update to keep lookups current.
- If your facet has `[Singleton]` fields, pass those values into both `Lookup.Update` and `TypeHandle.Update`; the signatures expand with one `in T` parameter per singleton.
- `Entity` fields are always available on the lookup; they are not added to the query and simply pass through the entity id.
- `CreateQueryBuilder` ignores optional fields and singletons, so add any custom filters (e.g., tags) yourself when building the query.
- `Lookup.TryGet(Entity, out TFacet)` returns `false` only when required fields are missing; optional components and buffers remain safely defaulted when absent.
- `TypeHandle` and `ResolvedChunk` support chunk iteration scenarios where you want to materialize a facet for each entity in a chunk.

## Example

```csharp
public readonly partial struct TestFacet : IFacet
{
    private readonly RefRW<ComponentA> compA;
    private readonly RefRO<ComponentB> compB;
    private readonly EnabledRefRO<EnabledA> enableA;
    private readonly DynamicBuffer<BufferA> bufferA;

    public partial struct Lookup {} // Required for use in IJobEntity
}

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
```

## Generated output

The generator adds constructors plus the `Lookup`, `ResolvedChunk`, and `TypeHandle` helpers that mirror the Unity `IAspect` API surface.

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using BovineLabs.Core.Tests.Facet;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Core.Tests.Facet
{
    /// <summary>
    /// Facet helpers generated for FacetTest.
    /// </summary>
    public partial struct FacetTest
    {
        /// <summary>
        /// Initializes a new instance of FacetTest.
        /// </summary>
        public FacetTest(Entity entity, RefRO<ComponentA> compA, RefRW<ComponentB> compB, RefRO<ComponentC> compC, RefRW<ComponentD> compD, EnabledRefRO<EnabledA> enableA, EnabledRefRW<EnabledB> enableB, EnabledRefRO<EnabledC> enableC, EnabledRefRW<EnabledD> enableD, DynamicBuffer<BufferA> bufferA, DynamicBuffer<BufferB> bufferB, DynamicBuffer<BufferC> bufferC, DynamicBuffer<BufferD> bufferD, SingletonA singletonA, DynamicBuffer<SingletonB> singletonB, Facet2Test facet2, Facet3Test facet3)
        {
            this.entity = entity;
            this.compA = compA;
            this.compB = compB;
            this.compC = compC;
            this.compD = compD;
            this.enableA = enableA;
            this.enableB = enableB;
            this.enableC = enableC;
            this.enableD = enableD;
            this.bufferA = bufferA;
            this.bufferB = bufferB;
            this.bufferC = bufferC;
            this.bufferD = bufferD;
            this.singletonA = singletonA;
            this.singletonB = singletonB;
            this.facet2 = facet2;
            this.facet3 = facet3;
        }

        /// <summary>
        /// Creates an EntityQueryBuilder requesting the required components for FacetTest.
        /// </summary>
        /// <param name="allocator">Allocator used for the query builder.</param>
        public static EntityQueryBuilder CreateQueryBuilder(Allocator allocator = Allocator.Temp)
        {
            return new EntityQueryBuilder(allocator).WithAll<ComponentA>().WithAllRW<ComponentB>().WithAllRW<EnabledA>().WithAllRW<EnabledB>().WithAll<BufferA>().WithAllRW<BufferB>();
        }

        /// <summary>
        /// Provides entity-level access to FacetTest.
        /// </summary>
        public partial struct Lookup
        {
            [ReadOnly]
            public BufferLookup<BufferA> BufferAs;

            public BufferLookup<BufferB> BufferBs;

            [ReadOnly]
            public BufferLookup<BufferC> BufferCs;

            public BufferLookup<BufferD> BufferDs;

            [ReadOnly]
            public ComponentLookup<ComponentA> ComponentAs;

            public ComponentLookup<ComponentB> ComponentBs;

            [ReadOnly]
            public ComponentLookup<ComponentC> ComponentCs;

            public ComponentLookup<ComponentD> ComponentDs;

            [ReadOnly]
            public ComponentLookup<EnabledA> EnabledAs;

            public ComponentLookup<EnabledB> EnabledBs;

            [ReadOnly]
            public ComponentLookup<EnabledC> EnabledCs;

            public ComponentLookup<EnabledD> EnabledDs;

            public Facet2Test.Lookup Facet2;

            public Facet3Test.Lookup Facet3;

            [ReadOnly]
            public SingletonA SingletonA;

            [ReadOnly]
            public DynamicBuffer<SingletonB> SingletonB;

            /// <summary>
            /// Gets the FacetTest for the specified entity.
            /// </summary>
            public FacetTest this[Entity entity]
            {
                get
                {
                    var entityValue = entity;
                    var compA = this.ComponentAs.GetRefRO(entity);
                    var compB = this.ComponentBs.GetRefRW(entity);
                    this.ComponentCs.TryGetRefRO(entity, out var compC);
                    this.ComponentDs.TryGetRefRW(entity, out var compD);
                    var enableA = this.EnabledAs.GetEnabledRefRO<EnabledA>(entity);
                    var enableB = this.EnabledBs.GetEnabledRefRW<EnabledB>(entity);
                    var enableC = this.EnabledCs.GetEnabledRefROOptional<EnabledC>(entity);
                    var enableD = this.EnabledDs.GetEnabledRefRWOptional<EnabledD>(entity);
                    var bufferA = this.BufferAs[entity];
                    var bufferB = this.BufferBs[entity];
                    this.BufferCs.TryGetBuffer(entity, out var bufferC);
                    this.BufferDs.TryGetBuffer(entity, out var bufferD);
                    var singletonA = this.SingletonA;
                    var singletonB = this.SingletonB;
                    var facet2 = this.Facet2[entity];
                    this.Facet3.TryGet(entity, out var facet3);
                    return new FacetTest(entityValue, compA, compB, compC, compD, enableA, enableB, enableC, enableD, bufferA, bufferB, bufferC, bufferD, singletonA, singletonB, facet2, facet3);
                }
            }

            /// <summary>
            /// Initializes lookups used by FacetTest.
            /// </summary>
            /// <param name="state">System state providing lookup handles.</param>
            public void Create(ref SystemState state)
            {
                this.ComponentAs = state.GetComponentLookup<ComponentA>(true);
                this.ComponentBs = state.GetComponentLookup<ComponentB>();
                this.ComponentCs = state.GetComponentLookup<ComponentC>(true);
                this.ComponentDs = state.GetComponentLookup<ComponentD>();
                this.EnabledAs = state.GetComponentLookup<EnabledA>(true);
                this.EnabledBs = state.GetComponentLookup<EnabledB>();
                this.EnabledCs = state.GetComponentLookup<EnabledC>(true);
                this.EnabledDs = state.GetComponentLookup<EnabledD>();
                this.BufferAs = state.GetBufferLookup<BufferA>(true);
                this.BufferBs = state.GetBufferLookup<BufferB>();
                this.BufferCs = state.GetBufferLookup<BufferC>(true);
                this.BufferDs = state.GetBufferLookup<BufferD>();
                this.Facet2.Create(ref state);
                this.Facet3.Create(ref state);
            }

            /// <summary>
            /// Attempts to retrieve FacetTest for an entity.
            /// </summary>
            /// <param name="entity">The entity to read.</param>
            /// <param name="facet">The resolved facet when the entity has the required components.</param>
            public bool TryGet(Entity entity, out FacetTest facet)
            {
                facet = default;

                var entityValue = entity;
                if (!this.ComponentAs.TryGetRefRO(entity, out var compA))
                {
                    return false;
                }
                if (!this.ComponentBs.TryGetRefRW(entity, out var compB))
                {
                    return false;
                }
                this.ComponentCs.TryGetRefRO(entity, out var compC);
                this.ComponentDs.TryGetRefRW(entity, out var compD);
                var enableA = this.EnabledAs.GetEnabledRefROOptional<EnabledA>(entity);
                if (!enableA.IsValid)
                {
                    return false;
                }
                var enableB = this.EnabledBs.GetEnabledRefRWOptional<EnabledB>(entity);
                if (!enableB.IsValid)
                {
                    return false;
                }
                var enableC = this.EnabledCs.GetEnabledRefROOptional<EnabledC>(entity);
                var enableD = this.EnabledDs.GetEnabledRefRWOptional<EnabledD>(entity);
                if (!this.BufferAs.TryGetBuffer(entity, out var bufferA))
                {
                    return false;
                }
                if (!this.BufferBs.TryGetBuffer(entity, out var bufferB))
                {
                    return false;
                }
                this.BufferCs.TryGetBuffer(entity, out var bufferC);
                this.BufferDs.TryGetBuffer(entity, out var bufferD);
                var singletonA = this.SingletonA;
                var singletonB = this.SingletonB;
                if (!this.Facet2.TryGet(entity, out var facet2))
                {
                    return false;
                }
                this.Facet3.TryGet(entity, out var facet3);
                facet = new FacetTest(entityValue, compA, compB, compC, compD, enableA, enableB, enableC, enableD, bufferA, bufferB, bufferC, bufferD, singletonA, singletonB, facet2, facet3);
                return true;
            }

            /// <summary>
            /// Refreshes lookups for FacetTest and updates singleton caches.
            /// </summary>
            /// <param name="facet2SingletonComponent">Singleton value for SingletonComponent which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonComponent&gt;().</param>
            /// <param name="facet3SingletonBuffer">Singleton value for SingletonBuffer which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonBuffer&gt;().</param>
            /// <param name="singletonA">Singleton value for SingletonA which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonA&gt;().</param>
            /// <param name="singletonB">Singleton value for DynamicBuffer&lt;SingletonB&gt; which is typically retrieved via SystemAPI.GetSingletonBuffer&lt;SingletonB&gt;().</param>
            /// <param name="state">System state used to update handles.</param>
            public void Update(ref SystemState state, in SingletonA singletonA, in DynamicBuffer<SingletonB> singletonB, in SingletonComponent facet2SingletonComponent, in SingletonBuffer facet3SingletonBuffer)
            {
                this.ComponentAs.Update(ref state);
                this.ComponentBs.Update(ref state);
                this.ComponentCs.Update(ref state);
                this.ComponentDs.Update(ref state);
                this.EnabledAs.Update(ref state);
                this.EnabledBs.Update(ref state);
                this.EnabledCs.Update(ref state);
                this.EnabledDs.Update(ref state);
                this.BufferAs.Update(ref state);
                this.BufferBs.Update(ref state);
                this.BufferCs.Update(ref state);
                this.BufferDs.Update(ref state);
                this.Facet2.Update(ref state, facet2SingletonComponent);
                this.Facet3.Update(ref state, facet3SingletonBuffer);
                this.SingletonA = singletonA;
                this.SingletonB = singletonB;
            }
        }

        /// <summary>
        /// Chunk-level accessors for FacetTest.
        /// </summary>
        public struct ResolvedChunk
        {
            public BufferAccessor<BufferA> BufferAs;

            public BufferAccessor<BufferB> BufferBs;

            public BufferAccessor<BufferC> BufferCs;

            public BufferAccessor<BufferD> BufferDs;

            public NativeArray<ComponentA> ComponentAs;

            public NativeArray<ComponentB> ComponentBs;

            public NativeArray<ComponentC> ComponentCs;

            public NativeArray<ComponentD> ComponentDs;

            public EnabledMask EnabledAs;

            public EnabledMask EnabledBs;

            public EnabledMask EnabledCs;

            public EnabledMask EnabledDs;

            public NativeArray<Entity> Entities;

            public Facet2Test.ResolvedChunk Facet2;

            public Facet3Test.ResolvedChunk Facet3;

            public SingletonA SingletonA;

            public DynamicBuffer<SingletonB> SingletonB;

            /// <summary>
            /// Gets the FacetTest for an entity in the chunk by index.
            /// </summary>
            public FacetTest this[int index] => new FacetTest(Entities[index], new RefRO<ComponentA>(ComponentAs, index), new RefRW<ComponentB>(ComponentBs, index), ComponentCs.IsCreated ? new RefRO<ComponentC>(ComponentCs, index) : default, ComponentDs.IsCreated ? new RefRW<ComponentD>(ComponentDs, index) : default, EnabledAs.GetEnabledRefRO<EnabledA>(index), EnabledBs.GetEnabledRefRW<EnabledB>(index), EnabledCs.GetOptionalEnabledRefRO<EnabledC>(index), EnabledDs.GetOptionalEnabledRefRW<EnabledD>(index), BufferAs[index], BufferBs[index], BufferCs.Length != 0 ? BufferCs[index] : default, BufferDs.Length != 0 ? BufferDs[index] : default, this.SingletonA, this.SingletonB, Facet2[index], Facet3[index]);
        }

        /// <summary>
        /// Maintains type handles for chunk access to FacetTest.
        /// </summary>
        public partial struct TypeHandle
        {
            [ReadOnly]
            public BufferTypeHandle<BufferA> BufferAHandle;

            public BufferTypeHandle<BufferB> BufferBHandle;

            [ReadOnly]
            public BufferTypeHandle<BufferC> BufferCHandle;

            public BufferTypeHandle<BufferD> BufferDHandle;

            [ReadOnly]
            public ComponentTypeHandle<ComponentA> ComponentAHandle;

            public ComponentTypeHandle<ComponentB> ComponentBHandle;

            [ReadOnly]
            public ComponentTypeHandle<ComponentC> ComponentCHandle;

            public ComponentTypeHandle<ComponentD> ComponentDHandle;

            [ReadOnly]
            public ComponentTypeHandle<EnabledA> EnabledAHandle;

            public ComponentTypeHandle<EnabledB> EnabledBHandle;

            [ReadOnly]
            public ComponentTypeHandle<EnabledC> EnabledCHandle;

            public ComponentTypeHandle<EnabledD> EnabledDHandle;

            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            public Facet2Test.TypeHandle Facet2Handle;

            public Facet3Test.TypeHandle Facet3Handle;

            [ReadOnly]
            public SingletonA SingletonAHandle;

            [ReadOnly]
            public DynamicBuffer<SingletonB> SingletonBHandle;

            /// <summary>
            /// Initializes type handles used by FacetTest.
            /// </summary>
            /// <param name="state">System state used to create handles.</param>
            public void Create(ref SystemState state)
            {
                this.EntityHandle = state.GetEntityTypeHandle();
                this.ComponentAHandle = state.GetComponentTypeHandle<ComponentA>(true);
                this.ComponentBHandle = state.GetComponentTypeHandle<ComponentB>();
                this.ComponentCHandle = state.GetComponentTypeHandle<ComponentC>(true);
                this.ComponentDHandle = state.GetComponentTypeHandle<ComponentD>();
                this.EnabledAHandle = state.GetComponentTypeHandle<EnabledA>(true);
                this.EnabledBHandle = state.GetComponentTypeHandle<EnabledB>();
                this.EnabledCHandle = state.GetComponentTypeHandle<EnabledC>(true);
                this.EnabledDHandle = state.GetComponentTypeHandle<EnabledD>();
                this.BufferAHandle = state.GetBufferTypeHandle<BufferA>(true);
                this.BufferBHandle = state.GetBufferTypeHandle<BufferB>();
                this.BufferCHandle = state.GetBufferTypeHandle<BufferC>(true);
                this.BufferDHandle = state.GetBufferTypeHandle<BufferD>();
                this.Facet2Handle.Create(ref state);
                this.Facet3Handle.Create(ref state);
            }

            /// <summary>
            /// Resolves a chunk into FacetTest.ResolvedChunk for job access.
            /// </summary>
            /// <param name="chunk">Chunk being processed.</param>
            public ResolvedChunk Resolve(ArchetypeChunk chunk)
            {
                return new ResolvedChunk
                {
                    Entities = chunk.GetNativeArray(this.EntityHandle),
                    ComponentAs = chunk.GetNativeArray(ref this.ComponentAHandle),
                    ComponentBs = chunk.GetNativeArray(ref this.ComponentBHandle),
                    ComponentCs = chunk.GetNativeArray(ref this.ComponentCHandle),
                    ComponentDs = chunk.GetNativeArray(ref this.ComponentDHandle),
                    EnabledAs = chunk.GetEnabledMask(ref this.EnabledAHandle),
                    EnabledBs = chunk.GetEnabledMask(ref this.EnabledBHandle),
                    EnabledCs = chunk.GetEnabledMask(ref this.EnabledCHandle),
                    EnabledDs = chunk.GetEnabledMask(ref this.EnabledDHandle),
                    BufferAs = chunk.GetBufferAccessor(ref this.BufferAHandle),
                    BufferBs = chunk.GetBufferAccessor(ref this.BufferBHandle),
                    BufferCs = chunk.GetBufferAccessor(ref this.BufferCHandle),
                    BufferDs = chunk.GetBufferAccessor(ref this.BufferDHandle),
                    SingletonA = this.SingletonAHandle,
                    SingletonB = this.SingletonBHandle,
                    Facet2 = this.Facet2Handle.Resolve(chunk),
                    Facet3 = this.Facet3Handle.Resolve(chunk),
                };
            }

            /// <summary>
            /// Updates type handles for FacetTest and refreshes singleton caches.
            /// </summary>
            /// <param name="facet2SingletonComponent">Singleton value for SingletonComponent which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonComponent&gt;().</param>
            /// <param name="facet3SingletonBuffer">Singleton value for SingletonBuffer which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonBuffer&gt;().</param>
            /// <param name="singletonA">Singleton value for SingletonA which is typically retrieved via SystemAPI.GetSingleton&lt;SingletonA&gt;().</param>
            /// <param name="singletonB">Singleton value for DynamicBuffer&lt;SingletonB&gt; which is typically retrieved via SystemAPI.GetSingletonBuffer&lt;SingletonB&gt;().</param>
            /// <param name="state">System state used to update handles.</param>
            public void Update(ref SystemState state, in SingletonA singletonA, in DynamicBuffer<SingletonB> singletonB, in SingletonComponent facet2SingletonComponent, in SingletonBuffer facet3SingletonBuffer)
            {
                this.EntityHandle.Update(ref state);
                this.ComponentAHandle.Update(ref state);
                this.ComponentBHandle.Update(ref state);
                this.ComponentCHandle.Update(ref state);
                this.ComponentDHandle.Update(ref state);
                this.EnabledAHandle.Update(ref state);
                this.EnabledBHandle.Update(ref state);
                this.EnabledCHandle.Update(ref state);
                this.EnabledDHandle.Update(ref state);
                this.BufferAHandle.Update(ref state);
                this.BufferBHandle.Update(ref state);
                this.BufferCHandle.Update(ref state);
                this.BufferDHandle.Update(ref state);
                this.Facet2Handle.Update(ref state, facet2SingletonComponent);
                this.Facet3Handle.Update(ref state, facet3SingletonBuffer);
                this.SingletonAHandle = singletonA;
                this.SingletonBHandle = singletonB;
            }
        }
    }
}
```