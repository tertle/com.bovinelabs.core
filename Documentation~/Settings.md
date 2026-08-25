# Settings

The Settings framework discovers project configuration `ScriptableObject` types, creates and organizes their assets, and presents them in **BovineLabs > Settings**. Choose the settings shape according to how the configured data must reach runtime code.

For process-wide developer tuning, command-line overrides, or Burst-compatible values that do not belong in a project asset, see [ConfigVars](ConfigVars.md).

## Choose a Settings Type

| Shape | Use it for | Runtime path |
|---|---|---|
| `ScriptableObject, ISettings` | Editor tooling or configuration referenced explicitly by another asset | Reference or load the asset yourself |
| `SettingsBase` | Authoring data that must bake ECS components into selected worlds | Read the baked ECS component |
| `SettingsSingleton<T>` | A configured global asset needed before ECS worlds or by managed runtime code | Access `T.I` |

Implementing `ISettings` provides Settings-window discovery and asset creation. It does not by itself include the asset in a player or create ECS data.

## Assemblies

The Core assemblies have `autoReferenced` disabled:

| Assembly | Settings surface |
|---|---|
| `BovineLabs.Core` | `ISettings`, attributes, `SettingsSingleton<T>`, and `SettingsTag` |
| `BovineLabs.Core.Authoring` | `SettingsBase`, `SettingsAuthoring`, and `AuthoringSettingsUtility` |
| `BovineLabs.Core.Editor` | Settings window, `EditorSettingsUtility`, editor routing, and custom settings panels |

`BovineLabs.Core.Authoring` is constrained to `UNITY_EDITOR`. Do not call `AuthoringSettingsUtility` from player code.

## Plain Settings Assets

Use `ScriptableObject, ISettings` when the Settings window should own and display an asset but no automatic ECS or global runtime integration is required.

```csharp
namespace Example.Settings
{
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [SettingsGroup("Game")]
    public sealed class GameConfiguration : ScriptableObject, ISettings
    {
        [SerializeField]
        private float musicVolume = 0.75f;

        [SerializeField]
        private bool enableTutorials = true;

        public float MusicVolume => this.musicVolume;

        public bool EnableTutorials => this.enableTutorials;
    }
}
```

Reference this asset from another included asset, load it through project-specific code, or use it only in editor tooling. The Settings framework does not automatically preload plain `ISettings` assets in a player.

## ECS-Integrated Settings

Inherit `SettingsBase` when the settings asset should bake ECS data through `SettingsAuthoring`.

```csharp
namespace Example.Settings
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    public struct GameplayData : IComponentData
    {
        public float MoveSpeed;
        public int MaxHealth;
    }

    [SettingsGroup("Game")]
    [SettingsWorld("Client", "Server")]
    public sealed class GameplaySettings : SettingsBase
    {
        [SerializeField]
        private float playerMoveSpeed = 5;

        [SerializeField]
        private int maxHealth = 100;

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            baker.AddComponent(entity, new GameplayData
            {
                MoveSpeed = this.playerMoveSpeed,
                MaxHealth = this.maxHealth,
            });
        }
    }
}
```

`SettingsAuthoring` adds `SettingsTag` to its entity, registers a baking dependency on each distinct settings asset, and invokes `Bake` on that asset. All settings assigned to one authoring component bake onto the same entity.

At runtime, read the component through normal ECS APIs:

```csharp
var settings = SystemAPI.GetSingleton<GameplayData>();
```

This requires exactly one matching component in the world. Avoid loading multiple `SettingsAuthoring` objects that bake the same singleton component into one world.

## Create and Organize Assets

In an interactive editor, when the Package Manager adds or updates a package, Core makes one pass over every settings type owned by an installed package and
creates any missing assets after registration finishes. The pass includes settings activated in an existing package by a newly installed optional dependency.
It does not run on ordinary domain reloads, create settings declared by project assemblies, or remove settings left behind by removed packages. Open
**BovineLabs > Settings** to create project settings or after editing an embedded package without changing its registration.

1. Open **BovineLabs > Settings** and select a settings panel.
2. Configure the generated asset.
3. Configure paths and ECS authoring assignments under **Core > Editor Settings**.

The default settings directory is `Assets/Settings/Settings`.

Use `[SettingSubDirectory("UI")]` to create a new settings asset under `Assets/Settings/Settings/UI`. The configured root path in **Core > Editor Settings** is applied when an asset is created. Existing assets are discovered anywhere in the project and are not moved when the root or attribute changes.

Only one asset of each settings type is valid. If duplicates exist, editor retrieval logs an error and uses one result; singleton initialization can initialize multiple assets and leave an order-dependent value in `T.I`.

## Route ECS Settings to Worlds

`SettingsWorldAttribute` is an editor-time routing key. It does not inspect or filter ECS worlds at runtime.

Configure routing under **BovineLabs > Settings > Core > Editor Settings**:

1. Create a prefab containing `SettingsAuthoring` for the default route.
2. Create any additional `SettingsAuthoring` prefabs used by client, server, menu, service, or other world-specific SubScenes.
3. Assign the default prefab to **Default Settings Authoring**.
4. Add world-key and prefab pairs to **Settings Authoring**.
5. Click **Update Settings** to clear and rebuild every authoring assignment.

Routing behavior:

- A `SettingsBase` without `[SettingsWorld]` goes to the default authoring.
- Keys are matched case-insensitively against the configured world-key entries.
- A blank key routes to the default authoring.
- If none of the declared keys resolves to an assigned authoring, the setting falls back to default.
- Repeating a resolved authoring in the attribute does not duplicate the asset in that authoring.

Opening the Settings window or calling `EditorSettingsUtility.GetSettings<T>()` asks the editor utility to add a `SettingsBase` to its configured authoring. Use **Update Settings** after changing keys, prefabs, attributes, or multi-world mappings so every assignment is rebuilt from a clean state.

The selected authoring prefab still has to be present in content baked into the intended ECS world. The attribute alone does not load a SubScene or prefab.

## Provide ECS Settings to the Editor World

Editor systems can require settings even when the SubScene that normally contains them is not open. Core always loads **Default Settings Authoring** as an
Editor fallback. **Additional Editor World Settings** contains route keys resolved through the existing **Settings Authoring** mappings and includes
`client` by default. Core loads each resolved prefab through `SceneSystem`, so normal baker dependencies continue to invalidate and rebake it when a
referenced settings asset changes. Empty or unresolved additional keys are invalid and throw when the Editor world is created.

The fallback is active only while no normal instance of that same prefab exists in the Editor world. When a SubScene supplies the prefab, Core restores
`Prefab` to the fallback's original linked entities; when that instance disappears, Core removes `Prefab` again. The fallback therefore remains loaded for
dependency tracking without creating duplicate settings singletons. Existing `Disabled` components on linked entities are never changed.

`SettingsAuthoring` is only valid on a prefab root; baking throws when it is placed on a scene GameObject or below a prefab root. Systems in Simulation or
Presentation observe the resolved settings state. An Initialization system that consumes these settings must update after
`EditorSettingsFallbackSystem`.

## Retrieve Settings in Editor and Authoring Code

### Editor tooling

```csharp
using BovineLabs.Core.Editor.Settings;

var settings = EditorSettingsUtility.GetSettings<GameConfiguration>();
```

`GetSettings<T>()` creates a missing asset and, when Core Editor Settings has a default authoring configured, attempts to wire `SettingsBase` into editor authoring configuration. Use `TryGetSettings<T>(out var settings)` when inspection must not create files or directories.

### Baking and authoring

```csharp
using BovineLabs.Core.Authoring.Settings;

var settings = this.DependsOn(AuthoringSettingsUtility.GetSettings<GameplaySettings>());
```

`AuthoringSettingsUtility.GetSettings<T>()` finds an existing asset and throws when none exists. Open the Settings window first to create it. When another baker reads the asset, register it with `DependsOn` so edits invalidate the bake.

`AuthoringSettingsUtility.TryGetSettings<T>()` reports absence without creating an asset.

## Global SettingsSingleton Assets

Use `SettingsSingleton<T>` for managed configuration that must be available before ECS worlds are created, such as UI maps, boot assets, and lookup tables.

```csharp
namespace Example.Settings
{
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [SettingsGroup("Game")]
    public sealed class GameBootstrapSettings : SettingsSingleton<GameBootstrapSettings>
    {
        [SerializeField]
        private TextAsset bootConfig;

        [SerializeField]
        private string defaultProfile = "Default";

        public TextAsset BootConfig => this.bootConfig;

        public string DefaultProfile => this.defaultProfile;

        protected override void OnInitialize()
        {
            // Rebuild non-serialized lookup state derived from the configured fields here.
        }
    }
}
```

Access the initialized asset with:

```csharp
var bootConfig = GameBootstrapSettings.I.BootConfig;
```

### Initialization

- Players initialize loaded singleton assets before the splash screen.
- The shared BovineLabs `[InitializeOnLoad]` editor initializer initializes singleton assets after editor load.
- `OnInitialize()` runs after the concrete asset becomes `T.I`; use it to rebuild non-serialized state.
- `T.I` lazily creates a transient default instance when no asset has initialized. This keeps the property non-null but does not provide configured serialized values and does not call `OnInitialize()` on that transient instance.

Treat a transient singleton as a setup failure when configured data is required.

### Build inclusion

Before a player build, `CoreBuildSetup`:

1. Removes existing `SettingsSingleton` entries from `PlayerSettings.preloadedAssets`.
2. Adds every discovered singleton whose `IncludeInBuild` is `true`.
3. Removes all `SettingsSingleton` entries again after the build.

The build processor therefore owns preloaded-asset handling for this settings type. Do not rely on manually maintained `SettingsSingleton` entries in `PlayerSettings.preloadedAssets`.

Override `IncludeInBuild` and return `false` only when the configured asset must be excluded from the player. Accessing `T.I` in that player can still create an unconfigured transient instance.

## Customize the Settings Window

`[SettingsGroup("Name")]` groups related settings panels. Without it, the panel uses the settings type's display name.

The Settings window hides panels with no visible serialized properties by default. Add `[AlwaysShowSettings]` when a custom editor draws the content or all serialized fields use `[HideInInspector]`.

For a fully custom panel, derive directly from `SettingsBasePanel<T>`. Override `OnActivate`, `OnDeactivate`, or `GetKeyWords` as needed. The Settings window discovers direct subclasses and uses their group, display name, empty-state, and filtering behavior instead of `GenericSettingsPanel<T>`.

## Troubleshooting

### A settings asset is missing

- After adding or updating a package, wait for Package Manager registration and compilation to finish.
- Open **BovineLabs > Settings** to create discoverable project settings or to recover from an interrupted automatic pass.
- Confirm the settings type is concrete, derives from `ScriptableObject`, and implements `ISettings`.
- Confirm the editor assembly references `BovineLabs.Core.Editor`.
- Use `EditorSettingsUtility.GetSettings<T>()` when creation is intended; `TryGetSettings` and `AuthoringSettingsUtility` do not create assets.

### A settings asset was created in the wrong directory

`SettingSubDirectory` and the configured settings root apply only during creation. Move an existing asset manually if its location should change, and keep only one asset of the type.

### ECS settings are missing from a world

- Confirm **Default Settings Authoring** is assigned.
- Confirm every `[SettingsWorld]` key has the expected case-insensitive mapping.
- Click **Update Settings** after routing changes.
- Confirm the selected authoring prefab or SubScene is actually baked into the world.
- Confirm the settings `Bake` method adds the expected component.

### ECS singleton access reports zero or multiple matches

Inspect the baked `SettingsTag` entities. The intended world should contain one settings authoring path for each singleton component type.

### Baking does not react to a referenced settings edit

When a baker fetches another settings asset through `AuthoringSettingsUtility`, pass that asset to `baker.DependsOn(...)`.

### A configured SettingsSingleton is empty in a player

- Confirm its asset exists and is unique.
- Confirm `IncludeInBuild` is `true`.
- Do not access `T.I` so early that it creates a transient instance before configured assets initialize.
