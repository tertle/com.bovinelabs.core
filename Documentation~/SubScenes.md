# SubScenes
## Summary
Provides convenient features for SubScene loading that can:

- Ensure SubScenes have been loaded before any system updates.
- Load and unload SubScenes automatically in range of the player.
- Only load SubScenes into certain worlds (Client or Server).

To use, simply place a `SubSceneLoadConfig` on any SubScene.

## Load Mode
### Auto Load
This option will automatically load the SubScene at startup.

If "IsRequired" is enabled, the World will not commence updating until the SubScene has been loaded. This is one of the most valuable features of the entire Core library.

It can be used to ensure that settings are loaded so "RequireForUpdate" isn't necessary, or to ensure that the ground of your world always exists preventing objects from falling through, and so on.

### Bounding Volume
This feature automatically loads and unloads SubScenes when they are within range of an entity marked with `LoadsSubScene`.

In `GameSettings`, you can define default load and unload distances. However, you can override these settings on a per-SubScene basis.

Note: This feature does not work when a SubScene is open; instead, the scene will always be loaded.

### On Demand
The SubScene will be set up, but loading of the SubScene is left up to the user.

## Target World
If NetCode is in the project, the Target World option will be visible. This allows you to only load a SubScene into a specific world.
