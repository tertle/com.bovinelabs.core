---
name: bl-core-inspectors
description: "Use for BovineLabs Core custom inspectors or property drawers, prefab override handling, serialized-property editing, or inspector debugging."
---

# Core Inspectors

Resolve Core editor source from `Packages/com.bovinelabs.core` or its exact `Library/PackageCache` entry. Preserve per-element state and prefab-aware behavior.

## Read Order

1. `Documentation~/Inspectors.md`
2. `BovineLabs.Core.Editor/Inspectors/ElementEditor.cs`
3. `BovineLabs.Core.Editor/Inspectors/ElementProperty.cs`
4. `BovineLabs.Core.Editor/Inspectors/GraphToolkitElementProperty.cs`
5. `BovineLabs.Core.Editor/Inspectors/PrefabElementEditor.cs`
6. `BovineLabs.Core.Editor/Inspectors/PrefabElementProperty.cs`
7. `BovineLabs.Core.Editor/Inspectors/ElementUtility.cs`

## Build Pattern

1. Override `CreateElement(SerializedProperty property)` and intercept only required properties.
2. Return `base.CreateElement(property)` for default field rendering.
3. Use `PreElementCreation(...)` to prepare state.
4. Use `PostElementCreation(...)` to register callbacks and apply initial state.
5. Use `ElementUtility.SetVisible(...)` for conditional UI sections.
6. Return `null` from `CreateElement(...)` when intentionally skipping a property.

## ElementProperty State Rule

Use `Cache<T>()` for callback state that must remain isolated per drawn element.

Reason:
- The same drawer type can render multiple elements at once (especially list/array entries).
- Shared instance-field state can cause callbacks to read or write the wrong `SerializedProperty`.

## Graph Toolkit Property Drawers

Use `GraphToolkitElementProperty` for Unity 6.5+ custom property drawers rendered inside Graph Toolkit inspectors or inline value editors.

Rules:
- An empty subclass is enough when default child-field rendering is desired.
- It uses foldout rendering by default; override `UseFoldout => false` only for drawers that must stay inline, such as compact value-reference rows.
- Override `SkipSingleRoot => true` for wrapper drawers that should unwrap a single composite `Value` child.
- `SkipSingleRoot` only unwraps when there is exactly one direct child and that child is a generic property with visible children.
- Use its protected `CreatePropertyField(...)` helpers when overriding `CreateElement(...)`; they return Graph Toolkit-aligned fields.
- Prefer a dedicated serializable value type and `[CustomPropertyDrawer(typeof(MyValueType))]` over targeting primitive or broad Unity types.

## Parent and Display Rules

1. Use `ParentType` to control container style (`Foldout`, `Label`, `None`).
2. Override `GetDisplayName(...)` for list entries when `Element N` is not useful.
3. Use `[PrefabElement]` or prefab-aware base classes when instance edits should target prefab assets.
