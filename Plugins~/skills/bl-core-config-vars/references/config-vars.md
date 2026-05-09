# Config Vars Reference

Use this reference when adding, changing, or debugging ConfigVars and their editor window behavior.

## Read Order

1. `BovineLabs.Core/ConfigVars/ConfigVarAttribute.cs`
2. `BovineLabs.Core/ConfigVars/ConfigurableAttribute.cs`
3. `BovineLabs.Core/ConfigVars/ConfigVarManager.cs`
4. `BovineLabs.Core.Editor/ConfigVars/ConfigVarsWindow.cs`
5. `BovineLabs.Core.Editor/ConfigVars/ConfigVarPanel.cs`
6. Example usages:
   - `BovineLabs.Core/Debug/BLLogger.cs`
   - `BovineLabs.Core.Editor/ChangeFilterTracking/ChangeFilterTrackingSystem.cs`
   - `BovineLabs.Core.Extensions/Utility/BovineLabsBootstrap.cs`

## Build Pattern

1. Add `[Configurable]` to the owning type (class/struct).
2. Add static `SharedStatic<T>` field(s) with `[ConfigVar(...)]`.
3. Keep names lowercase and dot-separated (validated by `ConfigVarManager`).
4. Use logical prefix groups like `debug.*`, `app.*`, `network.*`; the first segment becomes the panel group in ConfigVars window.
5. Set `isReadOnly: true` for values that should be immutable in play mode.
6. Set `isHidden: true` when values should stay internal and not appear in the window.

## Supported Value Types

`ConfigVarManager` and `ConfigVarPanel` currently support:

- `SharedStatic<int>`
- `SharedStatic<float>`
- `SharedStatic<bool>`
- `SharedStatic<Color>`
- `SharedStatic<Vector4>`
- `SharedStatic<Rect>`
- `SharedStatic<FixedString32Bytes>`
- `SharedStatic<FixedString64Bytes>`
- `SharedStatic<FixedString128Bytes>`
- `SharedStatic<FixedString512Bytes>`
- `SharedStatic<FixedString4096Bytes>`

If you add a new type, update both:

1. container mapping in `ConfigVarManager.GetContainer(...)`
2. UI binding creation in `ConfigVarPanel.CreateVisualElement(...)`

## Value Source Precedence

1. Command line `-<configVarName>` argument (highest priority).
2. EditorPrefs persisted value (editor).
3. Attribute default value (fallback).

## Failure Checklist

1. Confirm owning type has `[Configurable]`.
2. Confirm field is static `SharedStatic<T>` with supported `T`.
3. Confirm config var name passes validation (no uppercase, no invalid chars).
4. Confirm duplicate names are not being registered.
5. Confirm hidden vars are not expected in the UI (`isHidden: true`).
6. Confirm read-only vars are not expected to be editable during play mode.
