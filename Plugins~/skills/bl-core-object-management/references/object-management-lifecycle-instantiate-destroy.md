# Object Management Lifecycle Instantiate/Destroy

Use this reference for object-management runtime flows that resolve ObjectId to prefab, then instantiate/initialize/destroy entities.

## Read Order

1. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectManagementSettingsBase.cs`
2. `BovineLabs.Core.Extensions/ObjectManagement/ObjectDefinitionRegistrySystem.cs`
3. `BovineLabs.Core.Extensions/ObjectManagement/ObjectGroupMatcher.cs`
4. `Documentation~/ObjectManagement.md`

For lifecycle internals, also read the `core-lifecycle-usage` skill references rather than duplicating lifecycle rules here.

## Runtime Data Flow

1. `ObjectManagementSettingsBase.Bake(...)` emits setup buffers for definitions/groups.
2. `ObjectDefinitionRegistrySystem` builds runtime `ObjectDefinitionRegistry` singleton data (`ObjectId -> prefab entity`).
3. Runtime systems resolve prefab by object id, instantiate, and then rely on initialize/destroy lifecycle systems.
4. Group checks use `ObjectGroupMatcher` singleton buffer and `Matches(...)` extensions.

## Access Pattern

Use:

1. `SystemAPI.GetSingleton<ObjectDefinitionRegistry>().TryGetValue(objectId, out prefab)` when absence is a valid runtime possibility.
2. `SystemAPI.GetSingleton<ObjectDefinitionRegistry>()[objectId]` when the object id is guaranteed by authoring/settings invariants.
3. `SystemAPI.GetSingletonBuffer<ObjectGroupMatcher>().Matches(groupId, objectId)` for group membership checks.

## Constraints

1. Keep runtime systems non-structural except through approved instantiate command-buffer paths.
2. Avoid `Run()`/`Complete()` sync points in update flow unless explicitly required.
3. Keep initialization work job-first and map/object-id driven.

## Failure Checklist

1. Verify registry singleton exists in the target world.
2. Verify object id is valid and corresponds to a prefab entity in registry.
3. Verify group matcher contains expected `(GroupId, ObjectId)` pairs.
4. Verify initialize/destroy systems are in expected groups and worlds.
5. Verify no duplicate object IDs are emitted at runtime (registry system logs collisions).
6. If an optional object id path can miss, use `TryGetValue` instead of the throwing registry indexer.
