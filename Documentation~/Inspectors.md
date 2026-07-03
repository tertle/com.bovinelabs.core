# Inspectors

## Summary

BovineLabs Core provides UI Toolkit base classes for editor tooling:

- **`ElementEditor`** for `[CustomEditor]` inspectors.
- **`ElementProperty`** for `[CustomPropertyDrawer]` drawers.
- **`GraphToolkitElementProperty`** for Unity 6.5+ Graph Toolkit property drawers.

The base classes default to Unity's `PropertyField` rendering and let you selectively override only the fields that need custom behavior.

## Core Types

### `ElementEditor`

Base for custom inspectors (`UnityEditor.Editor`).

Key behavior:
- Automatically builds a root `VisualElement`.
- Optionally includes the script field (`m_Script`) via `IncludeScript`.
- Iterates serialized fields and calls `CreateElement(SerializedProperty)` for each one.
- Calls `PreElementCreation(root)` before iteration and `PostElementCreation(root, createdElements)` after.

Useful members:
- `Parent`: root inspector element.
- `MultiEditing`: true when inspecting multiple targets.
- `CreatePropertyField(...)`: helper that creates/binds `PropertyField`.
- `CreateFoldout(text, value)`: foldout aligned to inspector list styles.

### `ElementProperty`

Base for custom property drawers (`UnityEditor.PropertyDrawer`).

Key behavior:
- Builds a parent container based on `ParentType`:
  - `Foldout` (default)
  - `Label`
  - `None`
- Calls `PreElementCreation(root)` before drawing child fields.
- Draws children for generic properties, or the property itself for non-generic types.
- Calls `PostElementCreation(root, createdElements)` after element creation.

Useful members:
- `RootProperty`: property passed to the drawer.
- `SerializedObject`: serialized object for the current draw context.
- `CreatePropertyField(...)`: helper that creates/binds `PropertyField`.
- `Cache<T>()`: per-root-property cached state for fields/properties used across callbacks.
  Use this when the same drawer type can be created multiple times at once (for example list/array elements). It prevents shared state between elements, which otherwise can cause callbacks to read/write the wrong `SerializedProperty`.
- `GetDisplayName(...)`: override to customize foldout/label title (especially useful for list elements).
- `GetTooltip(...)`: override to customize foldout tooltips.

### `GraphToolkitElementProperty`

Base for custom property drawers rendered inside Unity Graph Toolkit inspectors and inline value editors.

Key behavior:
- Available on Unity 6.5+.
- Derives from `ElementProperty`.
- Inline by default (`ParentType.None`) so it matches Graph Toolkit field rows.
- Adds Graph Toolkit label/input USS classes to nested `PropertyField`s and mirrors Graph Toolkit inspector label-width sizing.
- Uses Graph Toolkit wrapper owner titles and tooltips for foldouts when `UseFoldout` is enabled.
- Exposes `UseFoldout`; override it to `true` only when the drawer should use a normal nested struct foldout.

### `PrefabElementEditor` and `PrefabElementProperty`

Prefab-aware variants that route edits to the prefab source object when editing a prefab instance.

- `PrefabElementEditor` derives from `ElementEditor`.
- `PrefabElementProperty` derives from `ElementProperty`.
- `[PrefabElement]` attribute uses `PrefabElementProperty` for field-level prefab editing behavior.

### `ElementUtility`

Shared UI helpers:
- `SetVisible(element, visible)` toggles `DisplayStyle`.
- `AddLabelStyles(label)` applies inspector-aligned label styling.

## Lifecycle Pattern

Both base classes follow the same extension flow:

1. Override `PreElementCreation` to initialize cached state.
2. Override `CreateElement` to intercept specific properties and return custom UI elements.
3. Return `base.CreateElement(property)` for standard field rendering.
4. Override `PostElementCreation` to register callbacks and apply initial visibility/state.

For dynamic inspectors, keep property references in `Cache<T>()` and drive visibility through callbacks plus a one-time initial update. This is important when multiple elements are visible simultaneously, because each drawn element needs isolated state.

## In-Project Examples

These APIs are used broadly across packages. Useful references:

- `Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Component/ComponentFieldAssetEditor.cs` (`ElementEditor`)
- `Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Settings/EditorSettingsEditor.cs` (`ElementEditor`)
- `Packages/com.bovinelabs.anchor/BovineLabs.Anchor.Editor/AnchorNavOptionsEditor.cs` (`ElementProperty`)
- `Packages/com.bovinelabs.essence/BovineLabs.Essence.Editor/ActionStatAuthoringDataEditor.cs` (`ElementProperty`)
- `Packages/com.bovinelabs.reaction/BovineLabs.Reaction.Editor/ConditionAuthoringConditionDataEditor.cs` (`ElementProperty`)
- `Packages/com.bovinelabs.traverse/BovineLabs.NavMesh.Editor/NavMeshSurfaceAuthoringSurfaceEditor.cs` (`ElementProperty`)

## Example: `ElementEditor`

```csharp
namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ExampleSettings))]
    public class ExampleSettingsEditor : ElementEditor
    {
        private SerializedProperty modeProperty;
        private PropertyField advancedField;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "mode":
                    this.modeProperty = property;
                    return CreatePropertyField(property);

                case "advanced":
                    return this.advancedField = CreatePropertyField(property);

                default:
                    return base.CreateElement(property);
            }
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.modeProperty.RegisterValueChangeCallback(_ => this.UpdateVisibility());
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            ElementUtility.SetVisible(this.advancedField, this.modeProperty.enumValueIndex > 0);
        }
    }
}
```

## Example: `ElementProperty`

```csharp
namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ExampleData))]
    public class ExampleDataProperty : ElementProperty
    {
        protected override ParentTypes ParentType => ParentTypes.None;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var cache = this.Cache<Cache>();

            switch (property.name)
            {
                case "enabled":
                    cache.EnabledProperty = property;
                    return cache.EnabledField = CreatePropertyField(property);

                case "value":
                    return cache.ValueField = CreatePropertyField(property);

                default:
                    return base.CreateElement(property);
            }
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var cache = this.Cache<Cache>();
            cache.EnabledField.RegisterValueChangeCallback(_ => Update(cache));
            Update(cache);
        }

        private static void Update(Cache cache)
        {
            ElementUtility.SetVisible(cache.ValueField, cache.EnabledProperty.boolValue);
        }

        private class Cache
        {
            public SerializedProperty EnabledProperty;
            public PropertyField EnabledField;
            public PropertyField ValueField;
        }
    }
}
```

## Example: `GraphToolkitElementProperty`

When default child-field rendering is enough, the drawer can be empty:

```csharp
namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ExampleData))]
    public class ExampleDataGraphToolkitProperty : GraphToolkitElementProperty
    {
    }
}
```

For foldout-style rendering:

```csharp
protected override bool UseFoldout => true;
```

## Implementation Notes

- Prefer `CreatePropertyField(...)` from these base classes so bindings match the correct serialized object.
- Use `GraphToolkitElementProperty` instead of `ElementProperty` for custom drawers rendered inside Graph Toolkit inspectors or inline value editors.
- Return `null` from `CreateElement` to intentionally skip rendering a property.
- For list/array element drawers, override `GetDisplayName` to replace generic names like `Element 0`.
- In `ElementProperty`, do not store callback state in instance fields when the drawer may render multiple elements at once; store it in `Cache<T>()` to avoid cross-element data access bugs.
- If you need prefab-source editing behavior for fields, use `[PrefabElement]`.
