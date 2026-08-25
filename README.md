# BovineLabs Core

BovineLabs Core is a Unity ECS/DOTS foundation package with Burst-friendly collections, source-generated entity helpers, reusable authoring and settings workflows, editor tooling, diagnostics, and shared test fixtures.

For support and discussions, join [Discord](https://discord.gg/RTsw6Cxvw3).

## Requirements

- Unity 6000.7 or newer.
- Unity Entities 6.7.0 or newer.
- NetCode and Unity Physics integrations require their matching Unity packages at 6.7.0 or newer. Input System 1.20.0 or newer enables Input Action asset inspectors. Localization, Splines, Terrain, and VFX integrations are compiled when their matching supported packages or Unity modules are available.

Optional integrations are guarded by asmdef `versionDefines` and compile symbols such as `UNITY_NETCODE`, `UNITY_PHYSICS`, `UNITY_INPUT_SYSTEM`, and `UNITY_LOCALIZATION`. Install the matching package before using an API that depends on it.

## Installation

### BovineLabs Package Manager — recommended

Install the standalone [BovineLabs Package Manager](https://gitlab.com/tertle/com.bovinelabs) once per project:

1. Open **Window > Package Management > Package Manager**.
2. Select **Install package from git URL...** from the add menu and enter:

```text
https://gitlab.com/tertle/com.bovinelabs.git
```

3. Open **Window > Package Management > BovineLabs Package Manager**.
4. Select **BovineLabs Core** and click **Install**.

The BovineLabs Package Manager installs this package and its required BovineLabs dependencies as embedded packages under `Packages/`. Commit the
installed package directories to version control. The manager connects to the BovineLabs registry itself; do not add the registry to Unity's scoped
registry settings.

### Git URL

To install Core directly from Git, open the Package Manager, select **Install package from git URL...**, and enter:

```text
https://gitlab.com/tertle/com.bovinelabs.core.git
```

The Git version may contain unpublished changes.

Then follow [Getting started](Documentation~/getting-started.md).

## Documentation

| Guide | Covers |
|---|---|
| [Overview](Documentation~/index.md) | Requirements, assemblies, feature selection, optional integrations, and the recommended reading path |
| [Getting started](Documentation~/getting-started.md) | Install Core, reference its assemblies, and verify a first ECS system |
| [Collections](Documentation~/Collections.md) | Choosing fixed, native, unsafe, blob, pooled, and entity-owned containers |
| [Dynamic buffer collections](Documentation~/DynamicCollections.md) | Entry-backed dictionaries, multi-dictionaries, and hash sets stored in ECS buffers |
| [Generated dynamic hash maps](Documentation~/DynamicHashMap.md) | Byte-backed maps, source-generated accessors, specialized variants, and optional NetCode serialization |
| [Entity commands](Documentation~/EntityCommands.md) | Reusing entity-shape builders across bakers, command buffers, jobs, and tests |
| [Settings](Documentation~/Settings.md) | Settings assets, ECS baking, world routing, and startup singletons |
| [ConfigVars](Documentation~/ConfigVars.md) | Burst-readable runtime variables, command-line overrides, EditorPrefs, and the ConfigVars window |
| [Debugging and logging](Documentation~/Debug.md) | `Check`, `BLLogger`, `BLGlobalLogger`, build gates, and entity-selection state |
| [Testing](Documentation~/Testing.md) | `BovineLabs.Testing`, `ECSTestsFixture`, leak checks, and math assertions |
| [Inspectors](Documentation~/Inspectors.md) | UI Toolkit editor bases, Graph Toolkit drawers, prefab-aware editing, and built-in drawers |
| [Troubleshooting](Documentation~/troubleshooting.md) | Assembly visibility, generators, settings, ConfigVars, collections, logging, and optional packages |

The [documentation overview](Documentation~/index.md#guides) links every specialized guide, including facets, jobs, iterators, spatial maps, keys, Burst trampolines, and utilities.

## Package layout

| Assembly | Purpose |
|---|---|
| `BovineLabs.Core` | Runtime APIs, collections, generators, diagnostics, settings singletons, and non-baker entity-command wrappers |
| `BovineLabs.Core.Authoring` | Bakers, `SettingsAuthoring`, `SettingsBase`, authoring utilities, and `BakerCommands` |
| `BovineLabs.Core.Editor` | Settings and ConfigVars windows, asset tools, custom inspectors, drawers, and editor utilities |
| `BovineLabs.Testing` | Editor-only ECS fixtures, leak detection, reflection helpers, and math assertions |

These assemblies have `autoReferenced` disabled. Add every assembly a consuming `.asmdef` uses; installing the package alone does not make its types visible.

If you want to support ongoing development or access private libraries, see [Buy Me a Coffee](https://buymeacoffee.com/bovinelabs).

## License

BovineLabs Core's original code is licensed under the [MIT License](LICENSE.md). Third-party portions retain their original licenses and copyright
notices; see [Third Party Notices](Third%20Party%20Notices.md).
