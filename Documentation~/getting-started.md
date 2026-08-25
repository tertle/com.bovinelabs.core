# Getting started

This guide installs Core, exposes its assemblies to project code, and verifies the setup with one small ECS system.

## Requirements

Core 2.0.0-pre.1 requires Unity 6000.7 or newer and Unity Entities 6.7.0 or newer. Unity Input System 1.20.0 or newer is optional and enables the Core editor inspectors for Input Action assets.

Core's assemblies have `autoReferenced` disabled. Every consuming assembly definition must explicitly reference the Core assemblies it uses.

## Install Core

### BovineLabs Package Manager — recommended

Install the standalone [BovineLabs Package Manager](https://gitlab.com/tertle/com.bovinelabs) once per project:

1. Open **Window > Package Management > Package Manager**.
2. Select **Install package from git URL...** from the add menu and enter:

```text
https://gitlab.com/tertle/com.bovinelabs.git
```

3. Open **Window > Package Management > BovineLabs Package Manager**.
4. Select **BovineLabs Core** and click **Install**.

The manager installs Core and its required BovineLabs dependencies as embedded packages under `Packages/`. Commit the installed package directories
to version control. The manager connects to the BovineLabs registry itself; do not add the registry to Unity's scoped registry settings.

### Git or manifest alternative

To install Core directly from Git, open the Unity Package Manager, choose **Install package from git URL...**, and enter:

```text
https://gitlab.com/tertle/com.bovinelabs.core.git
```

The equivalent `Packages/manifest.json` entry is:

```json
{
  "dependencies": {
    "com.bovinelabs.core": "https://gitlab.com/tertle/com.bovinelabs.core.git"
  }
}
```

## Add assembly references

Add `BovineLabs.Core` to the runtime `.asmdef` that will use Core APIs:

```json
{
  "name": "MyGame",
  "references": [
    "BovineLabs.Core",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Entities"
  ]
}
```

Add other Core assemblies only where needed:

| Code | Reference |
|---|---|
| Bakers, `SettingsBase`, `SettingsAuthoring`, or `BakerCommands` | `BovineLabs.Core.Authoring` |
| Custom inspectors, settings tooling, ConfigVars tooling, or asset editor helpers | `BovineLabs.Core.Editor` |
| Tests using `ECSTestsFixture`, `TestLeakDetection`, or `AssertMath` | `BovineLabs.Testing` |

`BovineLabs.Core.Authoring`, `BovineLabs.Core.Editor`, and `BovineLabs.Testing` are editor-only. Do not reference them from a player-only assembly.

## Verify the runtime reference

Add a one-shot system to the runtime assembly:

```csharp
namespace MyGame
{
    using BovineLabs.Core;
    using Unity.Burst;
    using Unity.Entities;

    [BurstCompile]
    public partial struct CoreSmokeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            BLGlobalLogger.LogWarning("BovineLabs Core is running.");
            state.Enabled = false;
        }
    }
}
```

Enter Play mode. The warning appears once, proving that the runtime asmdef can resolve Core and that the system compiled. Delete the smoke system after verification.

Use `BLLogger` instead when an ECS system needs the current world and frame in each message. See [Debugging and logging](Debug.md).

## Pick the first production workflow

| Goal | Guide |
|---|---|
| Add entity-owned key/value data | [Dynamic buffer collections](DynamicCollections.md) |
| Share setup between a baker and runtime command buffer | [Entity commands](EntityCommands.md) |
| Author configuration that bakes into ECS | [Settings](Settings.md) |
| Tune a small value from the editor or command line | [ConfigVars](ConfigVars.md) |
| Group component access behind generated helpers | [Facets](Facets.md) |
| Choose or own a native container correctly | [Collections](Collections.md) |

## Optional packages

Core contains integrations for packages such as NetCode, Physics, Localization, and Splines, but does not make every integration a package dependency. Install the matching package before using its APIs. Unity then enables the corresponding asmdef version define and recompiles Core.

## Common setup problems

**Project code cannot resolve a Core namespace**

Add the matching Core assembly to the consuming `.asmdef`. A `using` directive does not create an assembly reference.

**A baker cannot resolve `BakerCommands` or `SettingsBase`**

Reference both `BovineLabs.Core` and `BovineLabs.Core.Authoring` from the authoring assembly.

**A test cannot resolve `ECSTestsFixture`**

Reference `BovineLabs.Testing`, enable `UNITY_INCLUDE_TESTS`, and configure the asmdef as an editor test assembly. See [Testing](Testing.md).

**An optional API is absent**

Install the package that supplies it and verify the expected symbol, such as `UNITY_NETCODE` or `UNITY_PHYSICS`, is active.

## Next steps

- [Core overview](index.md)
- [Collections](Collections.md)
- [Settings](Settings.md)
- [Troubleshooting](troubleshooting.md)
