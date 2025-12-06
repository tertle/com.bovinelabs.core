# Settings

## Summary

The Settings system provides a framework for managing configuration data in Unity projects with DOTS support. Create settings as ScriptableObjects with automatic ECS integration.

**Key Features:**
- ScriptableObject-based settings with organized editor interface
- Automatic creation and management of settings assets
- Global singleton settings that initialize automatically and are included in builds
- World targeting to control which ECS worlds receive settings
- Integrated with SubScenes for easy world management

## Core Components

- **ISettings**: Base interface for all settings
- **SettingsBase**: Abstract base class for ECS-integrated settings
- **SettingsSingleton**: Base class for global settings assets with automatic initialization
- **SettingsAuthoring**: MonoBehaviour for configuring world-specific settings
- **SettingsGroupAttribute**: Categorizes settings in the editor window
- **SettingsWorldAttribute**: Specifies target worlds for settings
- **SettingSubDirectoryAttribute**: Places generated assets into a subfolder under the settings root

## Creating Settings

### Basic Settings
For simple configuration without ECS integration:

```csharp
[SettingsGroup("Game")]
public class GameConfiguration : ScriptableObject, ISettings
{
    [SerializeField] private float musicVolume = 0.75f;
    [SerializeField] private bool enableTutorials = true;
    
    public float MusicVolume => musicVolume;
    public bool EnableTutorials => enableTutorials;
}
```

### ECS-Integrated Settings
For settings that need to be baked into ECS worlds, inherit from SettingsBase:

```csharp
[SettingsGroup("Game")]
[SettingsWorld("Client", "Server")]
public class GameplaySettings : SettingsBase
{
    [SerializeField] private float playerMoveSpeed = 5.0f;
    [SerializeField] private int maxHealth = 100;
    
    public override void Bake(Baker<SettingsAuthoring> baker)
    {
        var entity = baker.GetEntity(TransformUsageFlags.None);
        baker.AddComponent(entity, new GameplayData 
        { 
            MoveSpeed = playerMoveSpeed,
            Health = maxHealth,
        });
    }
}
```

## Setup

1. Open the Settings Window via `BovineLabs → Settings`
2. Settings are automatically created in `Assets/Settings/Settings` folder
3. Configure values in the inspector panel

### Asset Organization

Apply `[SettingSubDirectory("UI")]` (or any folder name) to a settings type to place its asset inside `Assets/Settings/Settings/UI`. You can also override the root directory through `BovineLabs → Settings → Core → Editor Settings` in the Paths section, which `EditorSettingsUtility` uses whenever it creates or locates assets.

### ECS World Integration

1. Create a subscene for your target world(s)
2. Add a GameObject with `SettingsAuthoring` component
3. Assign the settings you want in this world

### Automatic Assignment

`SettingsBase` assets are automatically injected into the default or world-specific `SettingsAuthoring` prefabs defined in `Core → Editor Settings` whenever the settings window touches them. If assignments fall out of sync (for example after renaming worlds or manually editing the prefabs) you can rebuild them manually:

1. Create prefabs with `SettingsAuthoring` components
2. Navigate to `BovineLabs → Settings → Core → Editor Settings`
3. Assign prefabs to `Default Settings Authoring` and `Settings Authoring` array
4. Click "Update Settings" to automatically sort settings by world targeting

## Using Settings

### Accessing Settings

**In Editor Tools:**
```csharp
var gameConfig = EditorSettingsUtility.GetSettings<GameConfiguration>();
```

**During Baking:**
```csharp
var gameSettings = AuthoringSettingsUtility.GetSettings<GameplaySettings>();
```

**In ECS Systems:**
```csharp
var settings = SystemAPI.GetSingleton<GameplayData>();
```

**Global Singletons:**
```csharp
var inputActions = ControlSettings.I.Asset;
```

## SettingsSingleton

Use `SettingsSingleton<T>` for global data that needs to exist before worlds are created (InputAction assets, UI configuration, lookup tables, etc.). These assets still implement `ISettings`, so they appear in the Settings window, follow `[SettingsGroup]`, and are created in the same directory as other settings.

### Creating a Singleton

```csharp
public class ControlSettings : SettingsSingleton<ControlSettings>
{
    [SerializeField] private InputActionAsset asset;
    [SerializeField] private ControlSchema[] schemas = Array.Empty<ControlSchema>();

    public InputActionAsset Asset => this.asset;
    public IReadOnlyList<ControlSchema> Schemas => this.schemas;
}
```

Create or open the asset from the Settings window and configure it like any other ScriptableObject. Access it anywhere with `ControlSettings.I`.

### Lifetime and Initialization

- `SettingsSingleton` uses `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]` and `[InitializeOnLoadMethod]` to call `Initialize` for every asset before gameplay code executes, so `ControlSettings.I` is valid immediately in both the editor and players.
- Only one asset per type should exist; the settings window enforces this when it creates the asset.

### Build Inclusion

During `BuildPlayer`, `CoreBuildSetup` temporarily adds every `SettingsSingleton` asset to `PlayerSettings.preloadedAssets`, guaranteeing the ScriptableObjects are included in the player build without having to reference them from scenes or Resources. After the build finishes, the processor removes those temporary entries so project settings stay clean.