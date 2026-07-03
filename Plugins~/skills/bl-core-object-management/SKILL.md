---
name: bl-core-object-management
description: "Use for BovineLabs Core ObjectId definitions, registries, groups, auto-id or autoref, lookup baking, runtime lookup, or world object registry behavior."
---

# Core Object Management Usage

Use this skill for object definition/group authoring, ObjectId storage, WorldObjectRegistry live-entity lookup, LookupAuthoring bake wiring, and runtime lookup behavior.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read these baseline package sources first:
   - `Documentation~/ObjectManagement.md`
   - `BovineLabs.Core.Extensions.Authoring/ObjectManagement/ObjectManagementSettingsBase.cs`
2. Select and read only the focused reference files required for the task.
3. Apply rules from the relevant focused reference.
4. Validate bake/runtime and editor behavior using the focused failure checklist.

## Routing Decision

1. Use `references/object-management-ids-autoref.md` for ObjectDefinition/ObjectGroup IDs, `ObjectId` storage/ghost serialization, null definition, prefab uniqueness, AutoRef updates, import-time normalization, and inspector/search tooling.
2. Use `references/object-management-lifecycle-instantiate-destroy.md` for runtime prefab resolution, instantiate/initialize flow, and destroy/lifecycle sequencing for object-driven entities.
3. Use `references/object-management-lookup-authoring.md` for `LookupAuthoring` and map-based object initialization data.
4. Use `references/object-management-world-object-registry.md` for `WorldObjectRegistry` live entity registration and lookup by `ObjectId`.

## Fast Triage

1. Wrong ObjectDefinition ID, duplicate IDs, or missing definitions in settings:
   Use `references/object-management-ids-autoref.md`.
2. Spawned entity has wrong prefab or object id cannot resolve:
   Use `references/object-management-lifecycle-instantiate-destroy.md`.
3. Object-specific initialization map is empty or missing entries:
   Use `references/object-management-lookup-authoring.md`.
4. A world object cannot be found, resolves to the wrong live entity, or has duplicate live matches:
   Use `references/object-management-world-object-registry.md`.

## Routing

- `references/object-management-ids-autoref.md`: Null definition rules, ID uniqueness, prefab uniqueness, AutoRef/auto-id processing, and inspector/search integration.
- `references/object-management-lifecycle-instantiate-destroy.md`: ObjectDefinition registry and object-management-specific instantiate/initialize/destroy flows.
- `references/object-management-lookup-authoring.md`: `LookupAuthoring`/`LookupMultiAuthoring` bake patterns and runtime map consumption.
- `references/object-management-world-object-registry.md`: Live-world entity registration with `ObjectId` and `WorldObjectRegistry`.
