# Debugging and logging

Core provides Burst-compatible invariant checks, world-aware logging, global logging, numeric debug helpers, and selected-entity state for development tooling.

## Choose a logger

| Context | Use | Why |
|---|---|---|
| ECS system or job with a valid Core debug world | `BLLogger` | Prefixes messages with frame and world information |
| Bootstrap, editor tool, static helper, or code without a world | `BLGlobalLogger` | No ECS singleton required |
| A condition the optimizer may rely on | `Check.Assume` | Emits a checked failure in debug/check builds and a Burst assumption |

Do not use `BLLogger` merely to avoid passing a logger into a job; follow normal ECS dependency and data-flow rules.

## `BLLogger`

`BLDebugSystem` creates one `BLLogger` singleton in matching default, thin-client, and editor worlds. Read it directly in systems where that setup is guaranteed:

```csharp
namespace MyGame
{
    using BovineLabs.Core;
    using Unity.Burst;
    using Unity.Entities;

    [BurstCompile]
    public partial struct ProcessOrdersSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var logger = SystemAPI.GetSingleton<BLLogger>();
            logger.LogInfo("Processing orders.");
        }
    }
}
```

The fixed-string methods avoid managed formatting and are suitable for Burst code. Each level supplies common fixed-string sizes and a `*String` method for managed strings:

| Level | `BLLogger` methods |
|---|---|
| Verbose | `LogVerbose`, `LogVerboseString` |
| Debug | `LogDebug`, `LogDebug512`, `LogDebug4096`, `LogDebugString` |
| Info | `LogInfo`, `LogInfo512`, `LogInfo4096`, `LogInfoString` |
| Warning | `LogWarning`, `LogWarning512`, `LogWarning4096`, `LogWarningString` |
| Error | `LogError`, `LogError512`, `LogError4096`, `LogErrorString` |

Verbose methods are Editor-only. Debug methods use `UNITY_INCLUDE_INSTRUMENTATION` as their sole availability gate; Unity defines it in the Editor and in Instrumented, Checked, or Debug players. Info, warning, and error methods remain available in players and are filtered by the configured level.

## `BLGlobalLogger`

Use the static logger when world context is unavailable:

```csharp
BLGlobalLogger.LogInfo("Bootstrap complete.");
BLGlobalLogger.LogWarningString($"Missing optional profile: {profileName}");
BLGlobalLogger.LogFatal(exception);
```

The fixed-string and managed-string level methods mirror `BLLogger` closely. Two naming differences matter for long debug strings: global logging uses `LogDebugLong512` and `LogDebugLong4096`. For a 4096-byte info message, `BLGlobalLogger` overloads `LogInfo`.

`Log128(message, level)` and `LogString(message, level)` select a level dynamically.

## Log levels

`debug.loglevel` is an integer ConfigVar with a default of `Warning`:

| Value | Level |
|---:|---|
| 0 | Disabled |
| 1 | Fatal |
| 2 | Error |
| 3 | Warning |
| 4 | Info |
| 5 | Debug |
| 6 | Verbose |

Set it from **BovineLabs > ConfigVars** or pass a command-line override such as:

```text
-debug.loglevel 4
```

`debug.loglevel.min-world-length` pads short world names in logger prefixes. ConfigVar values stored in the editor are local `EditorPrefs`; see [ConfigVars](ConfigVars.md).

## Runtime invariant checks

`BovineLabs.Core.Assertions.Check.Assume` combines a checked assertion with `Unity.Burst.CompilerServices.Hint.Assume`:

```csharp
using BovineLabs.Core.Assertions;

Check.Assume(index >= 0 && index < length, "Index must be in range.");
```

When `ENABLE_UNITY_COLLECTIONS_CHECKS` or `UNITY_DOTS_DEBUG` is active, a failed condition logs an assertion and throws. Without those symbols, the checked branch is removed but the optimizer assumption remains.

Only use `Check.Assume` for invariants that are always true in release builds. It is not input validation and must not replace an ordinary conditional for recoverable data.

## Numeric debugging from Burst

`BovineLabs.Core.Utility.DebugUtil.SplitInt` separates a floating-point value into integer and decimal parts. It is useful when a Burst diagnostic cannot use the desired managed formatting:

```csharp
DebugUtil.SplitInt(value, 2, out var whole, out var decimals);

var message = new FixedString128Bytes("Value: ");
message.Append(whole);
message.Append('.');
message.Append(decimals);
logger.LogInfo(message);
```

Overloads accept `float` and `double`.

## Selected entity state

In the editor, `SelectedEntityEditorSystem` writes the current Entities Hierarchy selection to:

- `SelectedEntity`, containing the first selected entity.
- `SelectedEntities`, containing every selected entity.

The `debug.selection` ConfigVar enables or disables synchronization. In an instrumentation-enabled player, `SelectedEntitySystem` creates the same components but does not populate them from an editor selection.

This state is intended for debug panels and inspection systems, not gameplay authority.

## Custom worlds

A custom world that omits `BLDebugSystem` does not receive a `BLLogger` singleton. Either include the normal Core debug system when constructing the world or use `BLGlobalLogger` in code that must work without world setup.

Do not hide a missing guaranteed singleton with `TryGetSingleton`; fix the world setup when the system requires world-aware logging.

## Build behavior

Managed Code Variant is the source of truth for player debug code. Select it in Player Settings or the active build profile; do not add Unity-owned variant symbols to scripting define settings.

- `UNITY_INCLUDE_INSTRUMENTATION` is the sole availability gate for debug tooling and debug log methods. Unity defines it in the Editor and in Instrumented, Checked, and Debug players.
- Instrumented includes instrumentation without check-only validation.
- Checked includes instrumentation and defines `UNITY_ENABLE_CHECKS` for code-specific validation.
- Debug includes the Checked behavior and managed debugger support.
- Release includes neither instrumentation nor checks.
- `UNITY_EDITOR` independently includes genuinely Editor-only verbose paths and selection synchronization.
- `ENABLE_UNITY_COLLECTIONS_CHECKS`, `UNITY_DOTS_DEBUG`, and other package-specific symbols retain their existing safety semantics.

The native **Development Build** flag is independent from Managed Code Variant.

Log-level filtering is separate from compile-time inclusion. Raising `debug.loglevel` cannot restore a method removed by a compile symbol.

## Testing utilities

`ECSTestsFixture`, `TestLeakDetectionAttribute`, `AssertMath`, and `ReflectionTestHelper` live in the separate `BovineLabs.Testing` assembly. See [Testing](Testing.md) for current setup and examples.

## Troubleshooting

**`SystemAPI.GetSingleton<BLLogger>()` throws**

Confirm the world contains `BLDebugSystem`. Use `BLGlobalLogger` when the code legitimately has no Core-managed world.

**Debug messages do not appear in a player**

Select the Instrumented, Checked, or Debug Managed Code Variant and set `debug.loglevel` to at least 5. Verbose logging is editor-only.

**An info or warning message is filtered**

Check the numeric `debug.loglevel`. A lower value admits fewer levels.

**A release build behaves incorrectly after `Check.Assume`**

The condition was not a true invariant. Replace it with ordinary validation or make the invariant hold before the assumption.

## Related guides

- [ConfigVars](ConfigVars.md)
- [Testing](Testing.md)
- [Troubleshooting](troubleshooting.md)
