# Object Management IDs and AutoRef

Use this reference for ObjectDefinition/ObjectGroup identity, import-time normalization, and editor propagation.

## Read Order

1. `Documentation~/ObjectManagement.md`
2. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectDefinition.cs`
3. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectGroup.cs`
4. `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectManagementSettingsBase.cs`
5. `BovineLabs.Core.Editor/ObjectManagement/ObjectManagementProcessor.cs`
6. `BovineLabs.Core.Extensions.Editor/ObjectManagement/ObjectDefinitionInspector.cs`
7. `BovineLabs.Core.Extensions.Editor/ObjectManagement/ObjectGroupInspector.cs`
8. `BovineLabs.Core.Extensions.Editor/ObjectManagement/ObjectDefinitionSearchProvider.cs`

## Authoring Rules

1. Keep exactly one Null Definition with ID `0`.
2. Add a prefab for all non-null definitions.
3. Keep prefab mappings unique across definitions.
4. Use `ObjectCategories` for coarse grouping and `ObjectGroup` for explicit include/exclude sets.
5. Use `AutoRef`-managed arrays in settings instead of manual list maintenance.
6. Keep one `ObjectDefinitionAuthoring` component per authored prefab; the authoring component is marked with `DisallowMultipleComponent`.

## ObjectId Storage Rules

`ObjectId` is an `IComponentData` struct with public `RawValue` storage so it can be serialized by systems such as NetCode ghosts.

Use the constructor or explicit cast from `ObjectDefinition` when creating normal IDs. Use `ID` and `Mod` for semantic reads, and reserve `RawValue` for serialization, persistence, or interop code that needs the packed representation.

`ObjectId.Null` is the default value and represents the null definition.

## Auto-ID / AutoRef Behavior

`ObjectManagementProcessor` runs on imported assets and:

1. Enforces unique `IUID` IDs (fills holes and resolves duplicates).
2. Updates manager arrays declared via `[AutoRef(...)]`.
3. Rebuilds references to all matching assets of the defining type.

Expect duplicated or merged assets to be normalized after import.

## Inspector / Search Hooks

1. `ObjectDefinitionInspector` keeps prefab `ObjectDefinitionAuthoring` assignment consistent.
2. `ObjectDefinitionInspector` and `ObjectGroupInspector` propagate category/group changes to related assets.
3. `ObjectDefinitionSearchProvider` enables filters (`n=`, `d=`, `ca=`, `cid=`).

## Failure Checklist

1. Verify `ObjectManagementSettings` exists and contains expected definition/group arrays.
2. Verify `SettingsAuthoring` includes `ObjectManagementSettings` in target subscenes/worlds.
3. Verify null definition exists at ID `0`.
4. Verify no duplicate non-zero IDs after import.
5. Verify no duplicate prefab references across definitions.
6. Verify serialization or ghost code preserves `RawValue` rather than rebuilding IDs from only `ID`.
