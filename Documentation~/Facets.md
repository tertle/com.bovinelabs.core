# Facets

`IFacet` provides an aspect-like workflow built on the Core source generator. Declare the data you want to pull from an entity, and the generator emits helpers for entity lookup and chunk iteration.

## Declaring a facet

- Use a `partial struct` that implements `IFacet`.
- Supported entity fields include `RefRO<T>`, `RefRW<T>`, `EnabledRefRO<T>`, `EnabledRefRW<T>`, `DynamicBuffer<T>`, `ComponentLookup<T>`, `BufferLookup<T>`, `Entity`, `EntityStorageInfo`, and `EntityStorageInfoLookup`.
- Supported shared fields include plain component fields marked with `[Singleton]` and read-only singleton buffers marked with `[ReadOnly] [Singleton]`.
- Mark nested facets with `[Facet]`.
- Add `[FacetOptional]` to entity or nested-facet fields that may be missing from the target entity.
- Add `[ReadOnly]` when a field only needs read access.

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
    [ReadOnly] [Singleton] private SingletonA singletonA;
    [ReadOnly] [Singleton] private DynamicBuffer<SingletonB> singletonB;
    [Facet] private Facet2Test facet2;
    [FacetOptional] [Facet] private Facet3Test facet3;

    public partial struct Lookup {} // Optional, but useful when the lookup is passed to IJobEntity.
}
```
## Singleton fields

`[Singleton]` fields are cached on the generated `Lookup` and `TypeHandle` instead of being read from each target entity. They are useful for config, queues, blob holders, and other global data that a facet needs while it resolves per-entity fields.

When a facet or one of its nested facets has singleton dependencies, the generator emits a nested `SingletonData` struct. Create it once in `OnCreate`, then pass it to the generated `Update` overload.

```csharp
public readonly partial struct IntrinsicWriter : IFacet
{
    private readonly Entity entity;

    [ReadOnly]
    [Singleton]
    private readonly EssenceConfig essenceConfig;

    [Singleton]
    private readonly IntrinsicConditionWriteQueue intrinsicConditionWrites;

    private readonly DynamicBuffer<Intrinsic> intrinsics;
}

public partial struct ActionIntrinsicSystem : ISystem
{
    private IntrinsicWriter.SingletonData intrinsicWriterSingletonData;
    private IntrinsicWriter.Lookup intrinsicWriters;

    public void OnCreate(ref SystemState state)
    {
        this.intrinsicWriterSingletonData.Create(ref state);
        this.intrinsicWriters.Create(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        this.intrinsicWriters.Update(ref state, this.intrinsicWriterSingletonData);
    }
}
```

You usually do not need to call `SystemAPI.GetSingleton<T>()` yourself. The `Update(ref state, SingletonData data)` overload resolves each singleton from the generated queries and forwards the values to the lower-level overload.

The explicit overload still exists for advanced cases where the system already has the values:

```csharp
var essenceConfig = SystemAPI.GetSingleton<EssenceConfig>();
var intrinsicConditionWrites = SystemAPI.GetSingleton<IntrinsicConditionWriteQueue>();

this.intrinsicWriters.Update(ref state, in essenceConfig, in intrinsicConditionWrites);
```

Singleton query access follows the field attributes. `[ReadOnly] [Singleton]` fields use read-only queries, while non-read-only singleton component fields use read/write queries. Singleton buffer fields must be `[ReadOnly]`.

## Embedding facets

- Mark a field with `[Facet]` when its type is another `IFacet`.
- The generator wires the nested `Lookup`, `TypeHandle`, and `ResolvedChunk` so the parent facet can access the inner facet alongside its own data.
- Required nested facets contribute their required entity fields to the parent `CreateQueryBuilder`.
- Optional nested facets do not contribute query requirements; they resolve to `default` when absent.
- Singleton dependencies from nested facets are included in the parent `SingletonData` and forwarded to the nested update methods.

```csharp
public partial struct CompositeFacet : IFacet
{
    [Facet] private TestFacet testFacet;
    [FacetOptional] [Facet] private OptionalFacet optionalFacet;
}
```

## Usage notes

- Call `Lookup.Create(ref state)` and `TypeHandle.Create(ref state)` once during system creation.
- For facets without singleton dependencies, call `Lookup.Update(ref state)` or `TypeHandle.Update(ref state)` each update.
- For facets with singleton dependencies, call `SingletonData.Create(ref state)` once, then call `Update(ref state, singletonData)` each update.
- `CreateQueryBuilder` includes required `Ref*`, `EnabledRef*`, and `DynamicBuffer<T>` fields from the facet and required nested facets.
- `CreateQueryBuilder` ignores optional fields, singletons, entity metadata fields, `ComponentLookup<T>`, `BufferLookup<T>`, and optional nested facets.
- `Lookup.TryGet(Entity, out TFacet)` returns `false` when required fields are missing; optional fields remain safely defaulted when absent.
- `ResolvedChunk.TryGet(int, out TFacet)` mirrors lookup behavior for chunk iteration.
- `ResolvedChunk` indexers throw in collection-checks builds when required fields are missing; use `TryGet` when availability is uncertain.

## Example

```csharp
public readonly partial struct TestFacet : IFacet
{
    private readonly RefRW<ComponentA> compA;
    private readonly RefRO<ComponentB> compB;
    private readonly EnabledRefRO<EnabledA> enableA;
    private readonly DynamicBuffer<BufferA> bufferA;

    public partial struct Lookup {} // Optional, but useful when the lookup is passed to IJobEntity.
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
        state.Dependency = new JobLookup { FacetLookup = this.facetLookup }.Schedule(state.Dependency);
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
                if (resolved.TryGet(i, out TestFacet facet))
                {
                }
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
        }
    }

    private struct ReferenceComponent : IComponentData
    {
        public Entity FacetEntity;
    }
}
```

## Generated helpers

The generator adds constructors plus `Lookup`, `ResolvedChunk`, `TypeHandle`, and, when needed, `SingletonData` helpers that mirror the Unity `IAspect` style.

For a facet with singleton dependencies, `Lookup` and `TypeHandle` receive two update overloads: the recommended `SingletonData` overload and the explicit singleton-value overload.

```csharp
public readonly partial struct IntrinsicWriter
{
    public partial struct Lookup
    {
        [ReadOnly]
        public EssenceConfig EssenceConfig;

        public IntrinsicConditionWriteQueue IntrinsicConditionWrites;

        public void Update(ref SystemState state, SingletonData data)
        {
            var essenceConfig = data.EssenceConfigQuery.GetSingleton<EssenceConfig>();
            var intrinsicConditionWrites = data.IntrinsicConditionWritesQuery.GetSingleton<IntrinsicConditionWriteQueue>();

            this.Update(ref state, in essenceConfig, in intrinsicConditionWrites);
        }

        public void Update(ref SystemState state, in EssenceConfig essenceConfig, in IntrinsicConditionWriteQueue intrinsicConditionWrites)
        {
            // Refreshes component and buffer lookups, nested facets, and cached singleton values.
            this.EssenceConfig = essenceConfig;
            this.IntrinsicConditionWrites = intrinsicConditionWrites;
        }
    }

    public partial struct TypeHandle
    {
        [ReadOnly]
        public EssenceConfig EssenceConfig;

        public IntrinsicConditionWriteQueue IntrinsicConditionWrites;

        public void Update(ref SystemState state, SingletonData data)
        {
            var essenceConfig = data.EssenceConfigQuery.GetSingleton<EssenceConfig>();
            var intrinsicConditionWrites = data.IntrinsicConditionWritesQuery.GetSingleton<IntrinsicConditionWriteQueue>();

            this.Update(ref state, in essenceConfig, in intrinsicConditionWrites);
        }

        public void Update(ref SystemState state, in EssenceConfig essenceConfig, in IntrinsicConditionWriteQueue intrinsicConditionWrites)
        {
            // Refreshes component and buffer type handles, nested facets, and cached singleton values.
            this.EssenceConfig = essenceConfig;
            this.IntrinsicConditionWrites = intrinsicConditionWrites;
        }
    }

    public partial struct SingletonData
    {
        public EntityQuery EssenceConfigQuery;
        public EntityQuery IntrinsicConditionWritesQuery;

        public void Create(ref SystemState state)
        {
            this.EssenceConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EssenceConfig>()
                .WithOptions(EntityQueryOptions.IncludeSystems)
                .Build(ref state);

            this.IntrinsicConditionWritesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<IntrinsicConditionWriteQueue>()
                .WithOptions(EntityQueryOptions.IncludeSystems)
                .Build(ref state);
        }
    }
}
```
