# SubScenes
## Summary
The BovineLabs Core SubScenes system provides a comprehensive solution for managing Unity DOTS SubScenes and Assets with enhanced control over loading, unloading, and world targeting. The system offers several key features:

- Load management of SubScenes and Assets across different world types (Game, Service, Client, Server)
- Fine-grained control over when SubScenes are loaded
- Optional blocking of world system updates until critical SubScenes are fully loaded
- Integrated support for loading managed assets alongside SubScenes
- Editor tools for live baking and testing

## System Architecture

### Components

| Component          | Purpose                                                                     |
|--------------------|-----------------------------------------------------------------------------|
| `SubSceneLoadData` | Configuration data for how a SubScene should load                           |
| `SubSceneBuffer`   | Buffer containing references to scene assets to load in a set               |
| `SubSceneEntity`   | Buffer of the runtime SubScene entities, these may not be loaded yet        |
| `LoadSubScene`     | Enableable component indicating a SubScene should be loaded                 |
| `SubSceneLoaded`   | Enableable component indicating a SubScene has been loaded                  |
| `AssetLoad`        | Buffer for loading and instantiating gameobjects tied to a world at runtime |

Generally at runtime the only component you need to interact with is LoadSubScene, the rest are used internally but can provide useful information.

### Systems

| System                         | Purpose                                                                          |
|--------------------------------|----------------------------------------------------------------------------------|
| `SubSceneLoadingSystem`        | Primary system handling the loading, unloading, and status tracking of SubScenes |
| `SubSceneLoadingManagedSystem` | Managed system that handles integration with Unity SubScene objects              |
| `AssetLoadingSystem`           | System for loading and instantiating assets in specific world types              |

### Authoring Components

| Component/ScriptableObject   | Purpose                                                                                           |
|------------------------------|---------------------------------------------------------------------------------------------------|
| `SubSceneLoadAuthoring`      | MonoBehaviour for configuring both SubScene and asset loading in a scene                          |
| `SubSceneSettings`           | ScriptableObject container for all SubSceneSets, AssetSets, and EditorSceneSets                   |
| `SubSceneSet`                | ScriptableObject defining a set of SubScenes with load configuration                              |
| `AssetSet`                   | ScriptableObject defining a set of GameObjects to instantiate in specific world types             |
| `SubSceneEditorSet`          | ScriptableObject defining editor-only scene sets for testing                                      |

## SubSceneSet Configuration

### TargetWorld

The SubScenes system supports loading scenes into specific world types.

Available flags:
- `Game` - Load in the main game world
- `Service` - Load in service worlds
- `Client` - Load in client worlds (requires NetCode)
- `Server` - Load in server worlds (requires NetCode)
- `ThinClient` - Load in thin client worlds (requires NetCode)

### Required Loading
When a SubScene is marked as required, the system will pause game updates until the scene is fully loaded. This is crucial for essential content that must be available before gameplay can begin.

### Wait For Load
Similar to Required Loading but doesn't mark the scene as permanently required. The system will still wait for the scene to load before continuing updates.

### Auto Load
SubScenes can be configured to automatically load when the world starts. This is ideal for persistent content that should always be available or loading your first scene.

## SubScene Loading Setup

1. Open the SubScene Settings via the Settings window `BovineLabs → Settings → Core → SubScene`
2. Under Scene Sets, click the + button to create a new `SubSceneSet`
   - Add the scenes you want to be loaded in this set and configure the load rules as required
3. Create a SubScene with AutoLoad enabled
4. In this SubScene add a `SubSceneLoadAuthoring` component to a GameObject and assign your SubSceneSettings object to it

Your setup should look something like this:

![SubSceneSetup](Images/SubSceneSetup.png)

## Asset Loading Setup

The asset loading system allows you to instantiate GameObjects in specific world types alongside your SubScenes. Asset loading is configured through AssetSets, which are managed within the same SubSceneSettings asset.

### Creating an AssetSet

1. Open the SubScene Settings via the Settings window `BovineLabs → Settings → Core → SubScene`
2. Under Asset Sets, click the + button to create a new `AssetSet`
3. Configure which assets to load by adding GameObjects to the Assets list
4. Configure which worlds to load the assets into using the TargetWorld flags (usually Service or Client)

### Adding Assets to Your Scene

The same `SubSceneLoadAuthoring` component that handles SubScene loading also handles asset loading:

1. Add `SubSceneLoadAuthoring` to a GameObject in a SubScene (if you haven't already)
2. Assign your SubSceneSettings asset containing your AssetSets
3. When the SubScene loads, the specified assets will be instantiated in the appropriate worlds

Assets are loaded and instantiated by the `AssetLoadingSystem` and remain active as long as their hosting worlds exist. The system handles GameObject instantiation and cleanup automatically.

## On-Demand Loading

SubScenes can be loaded and unloaded at runtime by toggling the `LoadSubScene` enableable component.

## Editor Tools

### Scene Override

The Scene Override feature allows you to quickly switch between different sets of SubScenes during development without modifying your main SubScene configuration. This is particularly useful for testing different scene configurations or for focusing on specific areas of your game.

#### Setting up Editor Scene Sets

1. Open the SubScene Settings via the Settings window `BovineLabs → Settings → Core → SubScene`
2. Under Editor Scene Sets, click the + button to create a new `SubSceneEditorSet`
   - Add the scenes you want to load for this editor set
   - Configure the target worlds for the scenes (which worlds they should be loaded into)

#### Using Scene Override

When in the Unity Editor (not in play mode), you'll see a "Scene Override" button in the toolbar. When clicked, it displays a dropdown menu of all available editor scene sets.

- Select an editor scene set to override the default loading behavior when entering play mode
- The selected set's name will replace "Scene Override" in the toolbar, indicating it's active
- When you enter play mode, non-required subscenes will be disabled, and the scenes from your selected editor set will be loaded instead
- To disable the override, click the button again and select the currently active set (it will be checked)

This feature works by temporarily replacing the normal SubScene loading behavior with the scenes defined in your editor set. The system automatically:
1. Disables loading of non-required subscenes
2. Creates an entity with the Scene Override configuration
3. Forces the selected scenes to load at the start of play mode

You can toggle this toolbar button on/off via the Config Vars window by modifying the `debug.subscene-override` value.

### Live Baking

Usually to use Live Baking, you need to have an open SubScene in your hierarchy. The SubScenes system includes a toolbar extension that adds support for this for SubScenes being baked using EntitySceneReference.

When in play mode, a "Live Baking" button appears in the toolbar. Clicking it shows a list of available SubScenes that can be loaded or unloaded. This lets you test changes to scenes without stopping play mode.

SubScenes that have been marked as Required will be hidden from this list as it's assumed resetting these would break the game.

You can hide this button from the Config Vars window by toggling `debug.livebaking-toolbar-button`.

### Prebake SubScenes

When using EntitySceneReferences, after a domain reload these will not be automatically baked. This can be both a positive and a negative.

It means SubScenes that aren't going to be used in your next test session don't need to be rebaked, reducing iteration time.
However, for SubScenes you will use, they won't start baking until you enter play mode, instead of while you're working in the editor, which can increase waiting time.

To improve on this, you can choose to automatically start baking certain required SubScenes in the editor:

1. Go to `BovineLabs → Settings → Core → Editor Settings`
2. Add your scene assets to the Prebake Scenes list
3. These scenes will start baking immediately after a domain reload, saving time when you enter play mode