# SubScenes

## Summary

The SubScenes system provides enhanced control over Unity DOTS SubScenes and Assets with world targeting, loading management, and editor tools.

**Key Features:**
- Load management across different world types (Game, Service, Client, Server)
- Fine-grained control over when SubScenes are loaded
- Optional blocking of world updates until critical SubScenes are loaded
- Integrated support for loading managed assets alongside SubScenes
- Editor tools for live baking and testing

## Core Components

**Runtime Components:**
- `LoadSubScene`: Enableable component indicating a SubScene should be loaded
- `SubSceneLoaded`: Enableable component indicating a SubScene has been loaded
- `SubSceneLoadData`: Configuration data for how a SubScene should load
- `AssetLoad`: Buffer for loading and instantiating GameObjects tied to a world at runtime

**Authoring Components:**
- `SubSceneLoadAuthoring`: MonoBehaviour for configuring SubScene and asset loading
- `SubSceneSettings`: ScriptableObject container for SubSceneSets, AssetSets, and EditorSceneSets
- `SubSceneSet`: ScriptableObject defining a set of SubScenes with load configuration
- `AssetSet`: ScriptableObject defining a set of GameObjects to instantiate in specific worlds

## Configuration Options

### TargetWorld
Load scenes into specific world types:
- `Game` - Main game world
- `Service` - Service worlds
- `Client` - Client worlds (requires NetCode)
- `Server` - Server worlds (requires NetCode)
- `ThinClient` - Thin client worlds (requires NetCode)

### Loading Behavior
- **Required Loading**: Pauses game updates until the scene is fully loaded. Essential for content that must be available before gameplay can begin.
- **Wait For Load**: Similar to Required Loading but doesn't mark the scene as permanently required. The system will still wait for the scene to load before continuing updates.
- **Auto Load**: Automatically loads when the world starts. Ideal for persistent content that should always be available or for loading your first scene. Without Auto Load, SubScenes must be manually loaded via the `LoadSubScene` component.

## Setup

### SubScene Loading
1. Open `BovineLabs → Settings → Core → SubScene`
2. Under Scene Sets, create a new `SubSceneSet`
   - Add the scenes you want to be loaded in this set
   - Configure the load rules (Required Loading, Wait For Load, Auto Load)
   - Set the TargetWorld flags to specify which worlds should load these scenes
3. Create a SubScene with AutoLoad enabled (this will be your bootstrap scene)
4. In this SubScene, add a `SubSceneLoadAuthoring` component to a GameObject
5. Assign your SubSceneSettings object to the `SubSceneLoadAuthoring` component

### Asset Loading
The asset loading system allows you to instantiate GameObjects in specific world types alongside your SubScenes:

1. In the same SubScene Settings, under Asset Sets, create a new `AssetSet`
2. Add GameObjects to the Assets list
3. Configure which worlds to load assets into using TargetWorld flags (usually Service or Client)
4. The `SubSceneLoadAuthoring` component handles both SubScene and asset loading automatically

Assets are loaded and instantiated by the `AssetLoadingSystem` and remain active as long as their hosting worlds exist.

### Runtime Loading
SubScenes can be loaded and unloaded at runtime by toggling the `LoadSubScene` enableable component:

```csharp
// Load a SubScene
SystemAPI.SetComponentEnabled<LoadSubScene>(entity, true);

// Unload a SubScene
SystemAPI.SetComponentEnabled<LoadSubScene>(entity, false);

// Check if a SubScene is loaded
var isLoaded = SystemAPI.IsComponentEnabled<SubSceneLoaded>(entity);
```

This is the primary way to control SubScene loading after the initial Auto Load phase.

## Development Tools

### Scene Override
Quickly switch between different sets of SubScenes during development:

1. In SubScene Settings, under Editor Scene Sets, create a new `SubSceneEditorSet`
2. Add scenes and configure target worlds
3. Use the "Scene Override" toolbar button to select different scene sets for testing
4. Toggle via Config Vars: `debug.subscene-override`

### Live Baking
Test changes to scenes without stopping play mode:

- "Live Baking" button appears in toolbar during play mode
- Shows list of available SubScenes that can be loaded/unloaded
- Required SubScenes are hidden from this list
- Toggle via Config Vars: `debug.livebaking-toolbar-button`

### Prebake SubScenes
Start baking required SubScenes immediately after domain reload:

1. Go to `BovineLabs → Settings → Core → Editor Settings`
2. Add scene assets to the Prebake Scenes list
3. Scenes will start baking immediately after domain reload, saving time when entering play mode