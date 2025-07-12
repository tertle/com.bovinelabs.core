# Settings

## Summary

The Settings system provides a framework for managing configuration data in Unity projects with DOTS support. Create settings as ScriptableObjects with automatic ECS integration.

**Key Features:**
- ScriptableObject-based settings with organized editor interface
- Automatic creation and management of settings assets
- World targeting to control which ECS worlds receive settings
- Integrated with SubScenes for easy world management

## Core Components

- **ISettings**: Base interface for all settings
- **SettingsBase**: Abstract base class for ECS-integrated settings
- **SettingsAuthoring**: MonoBehaviour for configuring world-specific settings
- **SettingsGroupAttribute**: Categorizes settings in the editor window
- **SettingsWorldAttribute**: Specifies target worlds for settings

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

### ECS World Integration

1. Create a subscene for your target world(s)
2. Add a GameObject with `SettingsAuthoring` component
3. Assign the settings you want in this world

### Automatic Assignment

Settings can be automatically assigned to world-specific SettingsAuthoring prefabs:

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

### Resource Settings
For settings accessible through Unity's Resources system:

```csharp
[ResourceSettings("MyGame")]
public class ResourceGameSettings : ScriptableObject, ISettings
{
    // Implementation
}
```

Access via `Resources.Load<ResourceGameSettings>("MyGame/ResourceGameSettings")`.