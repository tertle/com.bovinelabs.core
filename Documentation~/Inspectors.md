# Inspectors

BovineLabs Core provides UI Toolkit base classes for custom inspectors and property drawers, prefab-source editing helpers, ECS-oriented property attributes, and reusable entity-container inspector elements.

The editor APIs are in `BovineLabs.Core.Editor`. Runtime property attributes are in `BovineLabs.Core`. Both assemblies have `autoReferenced` disabled, so add the appropriate assembly references to consuming assembly definitions.

## Base Classes

| Type | Use it for | Default rendering |
|---|---|---|
| `ElementEditor` | A `[CustomEditor]` inspector | Top-level serialized fields |
| `ElementProperty` | A `[CustomPropertyDrawer]` drawer | Generic child fields inside a foldout |
| `GraphToolkitElementProperty` | A drawer hosted by Graph Toolkit | Generic child fields inside a foldout with Graph Toolkit alignment |
| `PrefabElementEditor` | A component inspector whose instance edits should modify the prefab source | Same lifecycle as `ElementEditor` |
| `PrefabElementProperty` | A component field drawer whose instance edits should modify the prefab source | Same lifecycle as `ElementProperty` |

The bases use `PropertyField` for properties a subclass does not intercept. Return `base.CreateElement(property)` for normal rendering or `null` to omit a property intentionally.

## Lifecycle

`ElementEditor` and `ElementProperty` use the same extension flow:

1. `PreElementCreation(root)` prepares state and returns whether automatic property iteration should run.
2. `CreateElement(property)` creates, replaces, or omits each field.
3. `PostElementCreation(root, createdElements)` registers callbacks and performs the initial state update.

The `createdElements` argument is the boolean returned by `PreElementCreation`; it is not a field count. Returning `false` skips automatic iteration but still calls `PostElementCreation`. This is useful when a subclass builds the whole UI itself or must show an unavailable-state message.

Prefer the base `CreatePropertyField(...)` helpers so each field binds to the intended `SerializedObject`.

## ElementEditor

`ElementEditor`:

- Builds the root `Parent` element.
- Includes a disabled `m_Script` field by default; override `IncludeScript => false` to remove it.
- Exposes `MultiEditing` for multi-object inspector behavior.
- Iterates visible top-level properties and falls back to bound `PropertyField` controls.
- Provides `CreateFoldout(text, value)` with inspector list alignment.

### Conditional field example

```csharp
namespace Example
{
    using UnityEngine;

    public enum ExampleMode
    {
        Basic,
        Advanced,
    }

    public sealed class ExampleSettings : ScriptableObject
    {
        [SerializeField]
        private ExampleMode mode;

        [SerializeField]
        private int advancedValue;
    }
}

namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ExampleSettings))]
    public sealed class ExampleSettingsEditor : ElementEditor
    {
        private SerializedProperty modeProperty;
        private PropertyField modeField;
        private PropertyField advancedField;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "mode":
                    this.modeProperty = property;
                    return this.modeField = CreatePropertyField(property);

                case "advancedValue":
                    return this.advancedField = CreatePropertyField(property);

                default:
                    return base.CreateElement(property);
            }
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.modeField.RegisterValueChangeCallback(_ => this.UpdateVisibility());
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            ElementUtility.SetVisible(this.advancedField, this.modeProperty.enumValueIndex == (int)ExampleMode.Advanced);
        }
    }
}
```

Register the callback and call the same update method once so the first frame has the correct state.

## ElementProperty

`ElementProperty` exposes:

- `RootProperty`: the property passed to the drawer.
- `SerializedObject`: the current draw context.
- `Parent`: the generated parent container.
- `GetDisplayName(...)` and `GetTooltip(...)`.
- `Cache<T>()` for per-root-property callback state.

### Parent type

Override `ParentType` to select:

- `ParentTypes.Foldout`: default.
- `ParentTypes.Label`: a header label followed by fields.
- `ParentTypes.None`: no generated heading or foldout.

### Child iteration

`IterateChildren` defaults to `true`. Generic properties iterate their direct visible children. Non-generic properties, or drawers with `IterateChildren => false`, pass the selected root property to `CreateElement` instead.

`SkipSingleRoot` defaults to `false`. When enabled, it unwraps a property only when:

1. The property has exactly one direct child.
2. That child is a non-array generic property.
3. The child itself has visible children.

Primitive children, array children, empty wrappers, and roots with multiple children remain wrapped. This keeps wrapper-value drawers predictable.

### Per-element callback state

Unity can reuse one drawer instance for multiple visible list or array elements. Do not keep element-specific `SerializedProperty` or field references in instance fields. Store them in `Cache<T>()` so callbacks remain isolated by root property.

```csharp
namespace Example
{
    using System;

    [Serializable]
    public sealed class ExampleData
    {
        public bool Enabled;
        public int Value;
    }
}

namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ExampleData))]
    public sealed class ExampleDataProperty : ElementProperty
    {
        protected override ParentTypes ParentType => ParentTypes.None;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var cache = this.Cache<Cache>();

            switch (property.name)
            {
                case nameof(ExampleData.Enabled):
                    cache.EnabledProperty = property;
                    return cache.EnabledField = CreatePropertyField(property);

                case nameof(ExampleData.Value):
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

        private sealed class Cache
        {
            public SerializedProperty EnabledProperty;
            public PropertyField EnabledField;
            public PropertyField ValueField;
        }
    }
}
```

## GraphToolkitElementProperty

Use `GraphToolkitElementProperty` for drawers rendered in Graph Toolkit inspectors or inline value editors. It:

- Applies Graph Toolkit and Unity `PropertyField` label/input classes.
- Aligns label widths across the owning inspector or node constant-editor scope.
- Uses Graph Toolkit wrapper-owner title/display-name and tooltip metadata for foldouts.
- Provides Graph Toolkit-aligned `CreatePropertyField(...)` helpers.

### Foldout and inline behavior

`UseFoldout` defaults to `true`. An empty subclass therefore renders a normal nested-struct foldout:

```csharp
namespace Example
{
    using System;

    [Serializable]
    public struct GraphValue
    {
        public int Value;
    }
}

namespace Example.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(GraphValue))]
    public sealed class GraphValueProperty : GraphToolkitElementProperty
    {
    }
}
```

Override `UseFoldout => false` only when the row must stay inline:

```csharp
protected override bool UseFoldout => false;
```

Inline value-reference and fixed-string drawers often also use `IterateChildren => false` to create a single compact field. Wrapper drawers can combine `SkipSingleRoot => true` with `IterateChildren => false` when `CreateElement` should receive the unwrapped value property.

Real examples:

- `Packages/com.bovinelabs.grove/BovineLabs.Grove.Editor/ValueRefEditor.cs`: inline layout, conditional fields, and `Cache<T>()`.
- `Packages/com.bovinelabs.grove/BovineLabs.Grove.Editor/CustomEditor.cs`: inline single-root unwrapping.
- `Packages/com.bovinelabs.nerve/BovineLabs.Nerve.Editor/FixedString64BytesProperty.cs`: custom inline primitive control.

## Prefab-Source Editing

`PrefabElementEditor` and `PrefabElementProperty` redirect fields on a prefab instance to a `SerializedObject` for the nearest prefab source. The inspector displays **Changes are applied to the prefab** when redirection is active.

These helpers require a `Component` target. Both implementations cast the inspected target to `Component` and are not suitable for arbitrary `ScriptableObject` inspectors.

Use `PrefabElementEditor` as the base for a whole component inspector. `AllowChangesIfNoPrefab` defaults to `true`; override it to `false` when the inspector should suppress fields and show an unavailable message for non-prefab objects.

Use `[PrefabElement]` on an individual serialized component field:

```csharp
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

public sealed class ProjectileAuthoring : MonoBehaviour
{
    [SerializeField]
    [PrefabElement]
    private float damage = 10;
}
```

The attribute edits the prefab source rather than creating a normal instance override. Use it only when that ownership is intentional.

## Built-In Property Drawers

The attributes are in `BovineLabs.Core.PropertyDrawers`.

| Attribute or type | Behavior | Constraint |
|---|---|---|
| `[InspectorReadOnly]` | Displays a disabled field in UI Toolkit and IMGUI inspectors | Inspector-only protection; code can still mutate data |
| `[MinMax(min, max)]` | Foldout with a range slider and numeric endpoints | `Vector2` or `Vector2Int` only |
| `[InlineObject]` | Draws the referenced object's visible serialized fields below its object picker | Object-reference fields only |
| `[PrefabElement]` | Redirects a component field to its prefab source | Component-backed targets only |
| `half`, `half2`, `half3`, `half4` | Automatic float-based editors for mathematics half types | No attribute required |

```csharp
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

public sealed class ComponentSelection : ScriptableObject
{
    [SerializeField]
    [InspectorReadOnly]
    private string generatedName;

    [SerializeField]
    [MinMax(0, 100)]
    private Vector2 spawnRange = new(10, 25);
}
```

## Type Assets

`TypeAsset` stores an assembly-qualified type name and resolves it with `ResolveType()`. `ComponentAsset` derives from it and resolves the current
Entities stable type hash only when `GetStableTypeHash()` is called. Its inspector restricts selection to registered component and buffer types outside
editor assemblies.

`ComponentEnableableAsset` and `ComponentTagAsset` derive from `ComponentAsset`. Their inspectors further restrict selection to enableable types and
zero-sized component-data types, respectively. Use `ComponentAsset` for fields that may reference any of these component asset variants.

Derive a custom drawer from `BitFieldAttributeEditor<TAttribute>` when an integer-backed attribute implements `IBitFieldAttribute` and should display named values or a flag mask. Implement `GetKeyValues(...)` to supply the displayed names and bit/value indices.

## Entity Container Inspector Elements

The editor assembly also exposes reusable UI for dynamic-buffer-backed containers:

- `DynamicHashMapElement`
- `DynamicHashMapListElement`
- `DynamicHashMapSearchElement`
- `DynamicHashSetListElement`
- `DynamicListElement`
- `EntityInspector<T>`

Use these when building custom Entities inspectors for containers described in [DynamicHashMap](DynamicHashMap.md). Existing examples include Essence stat/intrinsic inspectors and Perception shared-memory inspection.

## Utility Helpers

`ElementUtility.SetVisible(element, visible)` toggles `DisplayStyle`.

`ElementUtility.AddLabelStyles(label)` applies default inspector field alignment to a custom label.

`PropertyUtil.CreateProperty(property, serializedObject)` creates and binds a named `PropertyField` when code is not inside one of the base classes.
