# Settings
## Summary
The BovineLabs Core Settings system provides a comprehensive framework for creating, managing, and using configuration data in your Unity projects with DOTS support. This system leverages Unity's ScriptableObject architecture and optionally bridges to DOTS Entity Component System, allowing you to define configuration in the familiar Unity Editor environment while supporting conversion to ECS data.

Key features include:
- ScriptableObject-based settings with organized editor interface
- Automatic creation and management of settings assets
- Customizable editor window for organizing and editing settings
- World targeting to control which ECS worlds your settings are available in
- When combined with [SubScenes](SubScenes.md), settings can be easily managed across different world types

## System Architecture

### Core Interfaces and Classes

| Interface/Class                | Purpose                                                                                       |
|--------------------------------|-----------------------------------------------------------------------------------------------|
| `ISettings`                    | Base interface that all settings implementations must inherit from                            |
| `SettingsBase`                 | Abstract base class for settings that inherits from ScriptableObject and implements ISettings |
| `EditorSettings`               | Core settings that control paths and default behaviors for the settings system                |

### Authoring Components

| Component                   | Purpose                                                                            |
|-----------------------------|------------------------------------------------------------------------------------|
| `SettingsAuthoring`         | MonoBehaviour for configuring which settings should be baked into a specific world |

### Utilities

| Utility                      | Purpose                                                                           |
|------------------------------|-----------------------------------------------------------------------------------|
| `AuthoringSettingsUtility`   | Utility for retrieving settings at authoring time                                 |
| `EditorSettingsUtility`      | Editor utility for creating and retrieving settings                               |

### Attributes

| Attribute                     | Purpose                                                                               |
|-------------------------------|---------------------------------------------------------------------------------------|
| `AlwaysShowSettingsAttribute` | Forces settings to appear in the editor window even if they have no modifiable fields |
| `ResourceSettingsAttribute`   | Places settings in a Resources folder for runtime loading                             |
| `SettingsGroupAttribute`      | Categorizes settings into groups in the settings window                               |
| `SettingsWorldAttribute`      | Specifies which worlds the settings should be baked into                              |

## Creating Settings

There are two primary approaches to creating settings:

### 1. Simple Configuration Settings

For settings that just need to be accessible in the editor or at runtime but don't need ECS integration, you can implement ISettings with any ScriptableObject:

```csharp
[SettingsGroup("Game")]
public class GameConfiguration : ScriptableObject, ISettings
{
    [SerializeField]
    private float musicVolume = 0.75f;
    
    [SerializeField]
    private bool enableTutorials = true;
    
    public float MusicVolume => this.musicVolume;
    public bool EnableTutorials => this.enableTutorials;
}
```

This configuration will appear in the settings window and be automatically created, but won't be baked into any ECS worlds.

### 2. ECS-Integrated Settings

For settings that need to be baked into ECS worlds, inherit from SettingsBase (which already implements ISettings) and implement the Bake method:

```csharp
[SettingsGroup("Game")]
[SettingsWorld("Client", "Server")]  // Optional: specify target worlds
public class GameplaySettings : SettingsBase
{
    [SerializeField]
    private float playerMoveSpeed = 5.0f;
    
    [SerializeField]
    private int maxHealth = 100;
    
    public float PlayerMoveSpeed => this.playerMoveSpeed;
    public int MaxHealth => this.maxHealth;
    
    // Implement custom baking logic to convert settings to ECS components
    public override void Bake(Baker<SettingsAuthoring> baker)
    {
        var entity = baker.GetEntity(TransformUsageFlags.None);
        
        // Add components with your settings data
        baker.AddComponent(entity, new GameplayData 
        { 
            MoveSpeed = this.playerMoveSpeed,
            Health = this.maxHealth,
        });
    }
}
```

## Settings Setup

Once you've defined your settings classes, they will be automatically created and managed:

1. Open the Settings Window via `BovineLabs → Settings`
2. Any settings that haven't been created will automatically be created
   1. By default settings will be placed in the `Assets/Settings/Settings` folder
   2. This can be configured via the Editor settings in the Settings Window
3. Your settings will appear organized by the groups you defined
4. Configure the values for your settings in the inspector panel

### Including Settings in Your ECS Worlds

If you want your settings to be available in ECS worlds:

1. Create a subscene that will be loaded in your target world(s)
2. Add a GameObject and attach the `SettingsAuthoring` component
3. Assign the settings you want associated with this subscene

For example, in a client-server setup:
- Client-only settings should be in a scene loaded only by the client
- Server-only settings should be in a scene loaded only by the server
- Shared settings should be in a scene loaded by both

### Automating the Assignment Process

Settings can be automatically placed in specific SettingsAuthoring prefabs based on the `SettingsWorldAttribute`. To set this up:

1. Create a GameObject with a `SettingsAuthoring` component and convert it to a prefab
2. Navigate to `BovineLabs → Settings → Core → Editor Settings`
3. Assign your prefab to the `Default Settings Authoring` field
   - This is the fallback where all settings without a specific world or with worlds that don't match any specific authoring will be placed
4. Add custom world-specific authorings using the `Settings Authoring` array
   - Fill in the World field (e.g., "client", "server") and assign a SettingsAuthoring prefab
   - All settings with matching `SettingsWorldAttribute` values will be assigned to these prefabs
5. Click the "Update Settings" button to automatically sort all your settings into the appropriate `SettingsAuthoring` prefabs
   - You can use this button any time you add new settings or change your world targeting
6. Once configured, any new settings will automatically be assigned to the correct SettingsAuthoring prefabs

## Using Settings

### Accessing Settings

There are two primary ways to access settings, depending on when and where you need them:

1. **In Editor Tools**:  
   Use `EditorSettingsUtility.GetSettings<T>()` for editor scripts and tools. This ensures settings assets exist, creating them if needed.

   ```csharp
   var gameConfig = EditorSettingsUtility.GetSettings<GameConfiguration>();
   ```

2. **During Authoring/Baking**:  
   Use `AuthoringSettingsUtility.GetSettings<T>()` during baking. This doesn't create settings if missing but will throw appropriate errors.

   ```csharp
   var gameSettings = AuthoringSettingsUtility.GetSettings<GameplaySettings>();
   ```

3. **In ECS Systems**:  
   Access baked components and buffers using standard ECS queries:

   ```csharp
   var settings = SystemAPI.GetSingleton<GameplayData>();
   ```

## Advanced Features

### Resource Settings

For settings that need to be accessible through Unity's Resources system:

```csharp
[ResourceSettings("MyGame")]
public class ResourceGameSettings : ScriptableObject, ISettings
{
    // Implementation
}
```

This will place the settings asset under a Resources folder for access via `Resources.Load<ResourceGameSettings>("MyGame/ResourceGameSettings")`.

### Custom Settings Panels

For advanced settings with custom UI:

1. Create a class inheriting from `SettingsBasePanel<T>` where T is your settings type
2. Implement custom UI drawing logic
3. The custom panel will automatically be used in the Settings window

### Empty Settings Handling

By default, settings with no visible inspector fields are hidden in the settings window.
For settings that need to appear in the window despite having hidden fields (for example, when you have custom editor functionality), use the `AlwaysShowSettings` attribute:

```csharp
[AlwaysShowSettings]
public class SpecialSettings : ScriptableObject, ISettings
{
    [HideInInspector]
    [SerializeField]
    private int value;
    
    // This will still show in the settings window despite having no visible fields
    // Useful for settings with custom editors or when hiding serialized fields
}
```