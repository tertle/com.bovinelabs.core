# Settings Reference

Use this reference when creating, modifying, or debugging settings in core or dependent packages.

## Read Order

1. `Documentation~/Settings.md`
2. `BovineLabs.Core.Editor/Settings/EditorSettingsUtility.cs`
3. `BovineLabs.Core.Authoring/Settings/SettingsAuthoring.cs`
4. `BovineLabs.Core.Authoring/Settings/SettingsBase.cs`
5. `BovineLabs.Core/Settings/SettingsSingleton.cs`
6. `BovineLabs.Core.Editor/CoreBuildSetup.cs`

## Type Selection

1. Use `ScriptableObject, ISettings` for config that does not bake ECS data.
2. Use `SettingsBase` when data must be baked into ECS worlds.
3. Use `SettingsSingleton<T>` for global settings that must be available via `T.I` before gameplay logic.

## Authoring Rules

1. Add `[SettingsGroup("...")]` to every settings type.
2. Add `[SettingsWorld(...)]` only for `SettingsBase` that should route to specific world authorings.
3. Add `[SettingSubDirectory("...")]` when settings assets should live in a subfolder under the settings root.
4. Keep one asset instance per settings type.
5. Keep `SettingsBase.Bake(...)` deterministic and focused on baking data/components.

## Access Rules

1. Use `EditorSettingsUtility.GetSettings<T>()` for editor tooling/setup.
2. Use `AuthoringSettingsUtility.GetSettings<T>()` for authoring-time retrieval.
3. Use ECS singleton access (`SystemAPI.GetSingleton<TData>()`) for baked runtime data.
4. Use `MySettingsSingleton.I` for `SettingsSingleton<T>` access.

## Wiring Behavior

1. `EditorSettingsUtility` creates missing settings assets in the configured settings directory.
2. `EditorSettingsUtility` auto-adds `SettingsBase` assets to `SettingsAuthoring`.
3. `SettingsWorldAttribute` values map to `EditorSettings` world keys (case-insensitive).
4. If no world matches, settings fall back to default settings authoring.
5. `CoreBuildSetup` includes `SettingsSingleton` assets in preloaded assets during build and reverts them after build.

## Failure Checklist

1. Verify correct base type (`ISettings`, `SettingsBase`, or `SettingsSingleton<T>`).
2. Verify attributes and world keys are valid.
3. Verify only one asset exists for the settings type.
4. Verify `BovineLabs -> Settings -> Core -> Editor Settings` has valid authoring assignments.
5. Verify target subscenes include `SettingsAuthoring` with expected settings assets.
6. Verify retrieval path matches how data is authored and baked.
