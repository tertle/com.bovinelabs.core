# Object Management World Object Registry

Use this reference for the runtime `WorldObjectRegistry` that maps initialized live-world entities by their authored `ObjectId`.

## Read Order

1. `BovineLabs.Core.Extensions/ObjectManagement/ObjectId.cs`
2. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectDefinitionAuthoring.cs`
3. `BovineLabs.Core.Extensions/ObjectManagement/WorldObjectRegistry.cs`
4. `BovineLabs.Core.Extensions/ObjectManagement/WorldObjectRegistryCreateSystem.cs`
5. `BovineLabs.Core.Extensions/ObjectManagement/WorldObjectRegistryDestroySystem.cs`

## Concept Boundaries

1. Use `ObjectDefinition`/`ObjectId` for prefab type identity, prefab resolution, and live-world object lookup keys.
2. `WorldObjectRegistry` is a runtime multi-map from `ObjectId` to live initialized entities.
3. Multiple live entities may share the same `ObjectId`. Callers that require a specific entity must pass that entity directly or iterate all registry matches.
4. `ObjectId.Null` is not registered.

## Authoring Rules

1. Add `ObjectDefinitionAuthoring` to entities that need an `ObjectId` component.
2. Add lifecycle authoring when runtime lookup must track entity initialize and destroy state.
3. The registry entry exists only for entities with a non-null `ObjectId` that enter `InitializeEntity` or `InitializeSubSceneEntity`.
4. Do not add or remove `ObjectId` structurally at runtime; author or bake the component up front.

## Runtime Data Flow

1. `WorldObjectRegistryCreateSystem` creates the `WorldObjectRegistry` singleton dynamic multi-hash map in the simulation world.
2. Entities with `ObjectId` are registered as `(ObjectId, Entity)` pairs when they initialize through `InitializeEntity` or `InitializeSubSceneEntity`.
3. Entities with `ObjectId` are unregistered by exact pair when they enter the `DestroyEntity` lifecycle path.
4. Use `SystemAPI.GetSingletonBuffer<WorldObjectRegistry>()` and `TryGetFirstValue(id, out entity)` when any matching live entity is acceptable.
5. Use `.AsMultiHashMap<WorldObjectRegistry, ObjectId, Entity>().GetValuesForKey(id)` when duplicate live objects must all be considered.

## Failure Checklist

1. Verify the `ObjectDefinition` asset has a valid non-null id after import.
2. Verify the authoring component bakes an `ObjectId` component onto the entity.
3. Verify the entity enters `InitializeEntity` or `InitializeSubSceneEntity` before lookup code expects it in the registry.
4. Verify the entity enters `DestroyEntity` when it should be removed from the registry.
5. Verify duplicate live matches are handled intentionally by the caller.
