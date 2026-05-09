# Object Management LookupAuthoring

Use this reference when adding, changing, or debugging `LookupAuthoring`/`LookupMultiAuthoring` object initialization maps.

## Read Order

1. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/LookupAuthoring.cs`
2. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectManagementSettingsBase.cs`
3. `BovineLabs.Core.Extensions.Editor/ObjectManagement/LookupAuthoringEditor.cs`

## Bake Wiring

`ObjectManagementSettingsBase.SetupLookups(...)` scans every non-null ObjectDefinition prefab:

1. `foreach (var c in d.Prefab.GetComponents<ILookupAuthoring>())`
2. `c.Bake(baker, entity, d, maps)`

`maps` is a per-bake `Dictionary<Type, object>` that caches one wrapper per map type so repeated writers append to the same buffer-backed map.

## Authoring Pattern

Prefer one:

1. `LookupAuthoring<TMap, TValue>` for one value per `ObjectId` (`IDynamicHashMap<ObjectId, TValue>`).
2. `LookupMultiAuthoring<TMap, TValue>` for multiple values per `ObjectId` (`IDynamicMultiHashMap<ObjectId, TValue>`).
3. Direct `ILookupAuthoring<TMap, TValue>` implementation when inheritance constraints require it.

Rules:

1. Keep `TValue` unmanaged.
2. Keep key type as `ObjectId` through `TMap`.
3. Return `false` from `TryGetInitialization(out TValue)` when optional data should not be emitted for a prefab.
4. Use multi-map flavor when duplicate object-id entries are valid.
5. Keep the authoring component on the prefab referenced by `ObjectDefinition`.
6. `LookupAuthoring` base classes require `LifeCycleAuthoring`.

## Runtime Consumption Pattern

Systems commonly:

1. `RequireForUpdate<TMap>()` for the baked map buffer type.
2. Read singleton map buffer once per update:
   `GetSingletonBufferNoSync<TMap>(true).AsHashMap<TMap, ObjectId, TValue>()`
   or multi-map equivalent.
3. Resolve by object id inside jobs using `TryGetValue` (or multi-map iteration).

## Failure Checklist

1. Verify `LookupAuthoring` component exists on each expected ObjectDefinition prefab.
2. Verify `TryGetInitialization(...)` returns `true` when data is expected.
3. Verify runtime system requires and reads the same `TMap` type baked by authoring.
4. Verify map flavor matches semantics (`IDynamicHashMap` vs `IDynamicMultiHashMap`).
5. Verify values are unmanaged and deterministic across bake/runtime.

