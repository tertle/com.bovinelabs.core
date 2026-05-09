# Inspectors Reference

Use this reference when implementing or modifying custom inspectors and property drawers.

## Read Order

1. `Documentation~/Inspectors.md`
2. `BovineLabs.Core.Editor/Inspectors/ElementEditor.cs`
3. `BovineLabs.Core.Editor/Inspectors/ElementProperty.cs`
4. `BovineLabs.Core.Editor/Inspectors/PrefabElementEditor.cs`
5. `BovineLabs.Core.Editor/Inspectors/PrefabElementProperty.cs`
6. `BovineLabs.Core.Editor/Inspectors/ElementUtility.cs`

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

## Parent and Display Rules

1. Use `ParentType` to control container style (`Foldout`, `Label`, `None`).
2. Override `GetDisplayName(...)` for list entries when `Element N` is not useful.
3. Use `[PrefabElement]` or prefab-aware base classes when instance edits should target prefab assets.
