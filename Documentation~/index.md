# BovineLabs Core

Core is the shared ECS/DOTS foundation used by BovineLabs packages. It combines runtime containers and helpers with source generation, authoring, editor tooling, diagnostics, settings, and test infrastructure.

Core provides:

- Burst-friendly native, unsafe, blob, pooled, spatial, and entity-owned collections.
- Source-generated facets and dynamic-buffer map accessors.
- `IEntityCommands` wrappers for sharing entity construction between bakers, command buffers, jobs, and tests.
- Settings assets that can remain editor-only, bake ECS data, or initialize before worlds are created.
- Burst-readable ConfigVars with command-line and editor overrides.
- Logging, assertions, editor inspectors, asset ID tooling, and test fixtures.
- Optional integrations for NetCode, Physics, Input System, Localization, Splines, Terrain, and VFX Graph.

## Start here

1. Follow [Getting started](getting-started.md) to install the package and add the required assembly references.
2. Read [Collections](Collections.md) before choosing a Core container. For entity-owned lookup data, start with [dynamic buffer collections](DynamicCollections.md).
3. Use [Entity commands](EntityCommands.md) for reusable entity-shape builders and [Facets](Facets.md) for reusable entity access.
4. Choose [Settings](Settings.md) for authored configuration or [ConfigVars](ConfigVars.md) for small runtime-tunable values.
5. Use [Debugging and logging](Debug.md) and [Testing](Testing.md) for diagnostics and validation.
6. Check [Troubleshooting](troubleshooting.md) when a type, generator, setting, or optional integration is missing.

## Requirements and assemblies

Core 2.0.0-pre.1 requires Unity 6000.7 or newer and Unity Entities 6.7.0 or newer. Unity Input System 1.20.0 or newer is optional and enables the Core editor inspectors for Input Action assets.

All public Core assemblies have `autoReferenced` disabled. Reference only the surfaces the consuming assembly uses:

| Assembly | Add it when |
|---|---|
| `BovineLabs.Core` | Runtime code uses Core collections, helpers, settings singletons, ConfigVars, logging, facets, jobs, or non-baker entity commands. |
| `BovineLabs.Core.Authoring` | A baker uses `SettingsBase`, `SettingsAuthoring`, authoring helpers, or `BakerCommands`. This assembly is editor-only. |
| `BovineLabs.Core.Editor` | Editor code uses the Settings or ConfigVars tooling, Core inspectors, asset tools, or editor utilities. |
| `BovineLabs.Testing` | Editor tests derive from `ECSTestsFixture` or use the shared leak, reflection, or math helpers. |

Source generators are distributed with the runtime assembly. A consuming runtime `.asmdef` still needs an explicit `BovineLabs.Core` reference before generated facet or dynamic-map helpers can compile.

## Choose a workflow

| Need | Start with |
|---|---|
| Store a normal dictionary, multi-dictionary, or set directly on an entity | [Dynamic buffer collections](DynamicCollections.md) |
| Store specialized byte-backed maps or replicate them with NetCode | [Generated dynamic hash maps](DynamicHashMap.md) |
| Share component/buffer setup across baking, runtime, jobs, and tests | [Entity commands](EntityCommands.md) |
| Resolve a reusable group of entity fields | [Facets](Facets.md) |
| Build transient many-writer data for one consuming system | [Singleton collections](SingletonCollection.md) |
| Run deferred or worker-lifecycle jobs over Core/Unity data | [Jobs](Jobs.md) |
| Iterate queries, chunks, blobs, or low-level lookups | [Iterators](Iterators.md) |
| Rebuild a spatial broad phase every frame | [Spatial](Spatial.md) |
| Author configuration and bake it into ECS | [Settings](Settings.md) |
| Tune a small Burst-readable value from the command line or editor | [ConfigVars](ConfigVars.md) |
| Create extensible enum/layer-style keys | [K](K.md) |
| Bridge from Burst code to a managed callback | [Burst trampoline](BurstTrampoline.md) |
| Reuse a temporary `NativeList<T>` allocation | [PooledNativeList](PooledNativeList.md) |
| Create ScriptableObject references and stable IDs | [Asset](Asset.md) |
| Build a UI Toolkit or Graph Toolkit inspector | [Inspectors](Inspectors.md) |

## Optional integrations

Core's main asmdefs list optional Unity assemblies and enable matching code through `versionDefines`. Common symbols include:

| Symbol | Integration |
|---|---|
| `UNITY_NETCODE` | NetCode-specific wrappers and generated ghost serializers |
| `UNITY_PHYSICS` | Physics helpers and authoring integrations |
| `UNITY_LOCALIZATION` | Unmanaged localization references |
| `UNITY_SPLINES` | Spline helpers |
| `UNITY_VFX_GRAPH` | Editor-only VFX Graph tooling |

An API behind one of these symbols is unavailable until the matching package is installed and Unity recompiles the consuming assembly.

## Ownership rules

- Dispose native and unsafe containers created with an allocator, or chain their `Dispose(JobHandle)` into the last user.
- Do not dispose `DynamicBuffer<T>` wrappers or Core maps backed by them; the entity owns that storage.
- Treat methods containing `Unsafe`, raw pointer access, `NoSync`, or `GetOrAddRefUnsafe` as low-level APIs. Respect dependencies and consume returned references before any later write that can resize or rehash the container.
- Cache type handles and lookups in systems, create them once, and update them before scheduling work.
- Prefer a documented high-level wrapper when it covers the use case.

## Guides

### Start and diagnose

- [Getting started](getting-started.md)
- [Troubleshooting](troubleshooting.md)
- [Debugging and logging](Debug.md)
- [Testing](Testing.md)

### ECS data and execution

- [Collections](Collections.md)
- [Dynamic buffer collections](DynamicCollections.md)
- [Generated dynamic hash maps](DynamicHashMap.md)
- [Entity commands](EntityCommands.md)
- [Facets](Facets.md)
- [Iterators](Iterators.md)
- [Jobs](Jobs.md)
- [Singleton collections](SingletonCollection.md)
- [Spatial](Spatial.md)

### Configuration and authoring

- [Settings](Settings.md)
- [ConfigVars](ConfigVars.md)
- [K](K.md)
- [Asset](Asset.md)
- [Inspectors](Inspectors.md)

### Focused utilities

- [Burst trampoline](BurstTrampoline.md)
- [Extensions](Extensions.md)
- [Global random](GlobalRandom.md)
- [PooledNativeList](PooledNativeList.md)
- [Utility](Utility.md)
