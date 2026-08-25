# ConfigVars

ConfigVars are process-wide, Burst-compatible tuning values stored in `SharedStatic<T>`. They are useful for developer options, diagnostics, and launch-time overrides that should be available without an ECS world.

Use [Settings](Settings.md) instead when configuration should be authored as a project asset, reviewed in source control, baked into a world, or included in a player as a configured `ScriptableObject`.

## Assemblies

ConfigVar attributes, storage, and initialization are in `BovineLabs.Core`. The editor window is in `BovineLabs.Core.Editor`. Both assemblies have `autoReferenced` disabled, so consuming assembly definitions must reference the surfaces they use.

## Declare a ConfigVar

Add `[Configurable]` to the owning non-generic type, place `[ConfigVar]` on a static `SharedStatic<T>` field, and give every field its own SharedStatic context key.

```csharp
namespace Example
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using Unity.Collections;

    [Configurable]
    public static class GameConfig
    {
        [ConfigVar("game.enemy-health", 100, "Base health assigned to newly spawned enemies.")]
        public static readonly SharedStatic<int> EnemyHealth = SharedStatic<int>.GetOrCreate<EnemyHealthKey>();

        [ConfigVar("game.profile", "default", "Profile selected during startup.", isReadOnly: true)]
        public static readonly SharedStatic<FixedString64Bytes> Profile = SharedStatic<FixedString64Bytes>.GetOrCreate<ProfileKey>();

        private struct EnemyHealthKey
        {
        }

        private struct ProfileKey
        {
        }
    }
}
```

Read the current value through `Data`, including from Burst-compiled code:

```csharp
var health = GameConfig.EnemyHealth.Data;
var profile = GameConfig.Profile.Data;
```

Do not reuse the same SharedStatic context for multiple fields of the same value type. Those fields would address the same storage even if their ConfigVar names differ.

## Names and Groups

Names are process-global identifiers. Keep every name unique across all loaded assemblies; editor persistence adds the current project scope
automatically.

`ConfigVarManager` validates names with this expression:

```text
^[a-z_+-][a-z0-9_+.-]*$
```

Names must therefore be lowercase and cannot contain spaces. A dot is not required, but a prefix such as `debug.*`, `app.*`, `game.*`, or `network.*` is recommended. The ConfigVars window uses the segment before the first dot as the group name; names without a dot appear under `ungrouped`.

## Supported Value Types

The runtime manager and editor window support these `SharedStatic<T>` types:

| Value type | Attribute default |
|---|---|
| `int` | Integer overload or string |
| `float` | Float overload or string |
| `bool` | Boolean overload or string |
| `Color` | Four-float overload: red, green, blue, alpha |
| `Vector4` | Four-float overload: x, y, z, w |
| `FixedString32Bytes` | String overload |
| `FixedString64Bytes` | String overload |
| `FixedString128Bytes` | String overload |
| `FixedString512Bytes` | String overload |
| `FixedString4096Bytes` | String overload |

`SharedStatic<Rect>` is also recognized by discovery and the window, but its current string decoder reuses `y` as the height instead of restoring the fourth value. Do not use `Rect` when defaults, command-line values, reset, or persistence must round-trip reliably.

Unsupported visible field types are invalid. The runtime manager logs an error for them, and the editor window cannot create a matching control.

## Initialization and Value Precedence

`ConfigVarManager` initializes automatically after assemblies load in a player and through the shared BovineLabs editor initializer in the editor. Normal gameplay code can read the `SharedStatic` value after startup. Editor tooling that directly reads `ConfigVarManager.All` or calls `FindAllConfigVars()` should call `ConfigVarManager.Initialize()` first; initialization is idempotent.

Values are selected in this order:

1. Command-line argument `-<name> <value>`.
2. The value stored for the current project in `EditorPrefs` when running in the editor.
3. The default declared by `[ConfigVar]`.

For example:

```text
Shattered.exe -game.enemy-health 250 -game.profile veteran
```

Players do not persist changes automatically; without a command-line override they use the attribute default on each launch.

Editor values are stored in `EditorPrefs` under `BovineLabs.Core.ConfigVars.<product-guid>.<name>`, using the stable
`PlayerSettings.productGUID` for the current project. They remain local user preferences rather than project assets, but changing a ConfigVar in one
Unity project no longer affects another project. Unscoped ConfigVar keys are ignored.

## ConfigVars Window

Open **BovineLabs > ConfigVars** to inspect and edit discovered values. The window:

- Groups rows by the first name segment.
- Searches names, group names, and descriptions.
- Offers **Reset To Default** for all registered values.
- Offers **Copy Name**, **Copy Value**, and **Reset To Default** from each row's context menu.
- Tracks changes made directly through the underlying `SharedStatic` while the field is not focused.

`isReadOnly: true` disables the editor field only while the editor is in Play mode. It does not prevent command-line initialization, direct writes to `SharedStatic<T>.Data`, or edit-mode changes.

`isHidden: true` removes a value from the ConfigVars window. The value is still discovered, initialized, and available to code and command-line overrides.

## Complex Editor Values

`Color`, `Vector4`, and `Rect` containers use a colon-delimited string internally. The current UI binding immediately writes Unity's `ToString()` representation to `EditorPrefs`, then the manager normally normalizes registered values back to the container format during editor shutdown or domain unload.

This means an editor crash or interrupted reload can lose the most recent complex value. Together with the current `Rect` height decoder issue, complex ConfigVars should be treated as live developer tooling rather than durable project configuration. Scalar and fixed-string values have straightforward editor round-tripping.

## Troubleshooting

### A field is not discovered

- Confirm the owning type has `[Configurable]`.
- Confirm the field is static and its exact value is a supported `SharedStatic<T>`.
- Confirm the consuming assembly references `BovineLabs.Core`.
- Avoid open generic owning types; discovery scans concrete types.

### A value is zero instead of its declared default

- Confirm `ConfigVarManager` has initialized before the read.
- Confirm the name passes validation and is globally unique.
- Check the editor log for unsupported-type, invalid-name, duplicate-name, or conversion errors.
- Check whether multiple fields accidentally reuse one SharedStatic context key.

### An editor value will not load

The stored `EditorPrefs` string may not match the value type, or a fixed string may exceed its capacity. Use the row or window reset when the window
opens. If initialization fails before the window can render, delete the project-scoped `EditorPrefs` entry whose key ends with the ConfigVar name,
then reopen the project.

### A read-only or hidden value behaves unexpectedly

Read-only affects window editing only during Play mode. Hidden affects window visibility only. Neither option changes runtime access to `SharedStatic<T>.Data`.

### A Rect height changes after reset or reload

This is a current container limitation: Rect decoding restores `(x, y, width, y)` rather than `(x, y, width, height)`. Use separate scalar ConfigVars or another settings mechanism until the container is corrected.
