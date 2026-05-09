# Core SubScenes Reference

## Read First

Use these sources when the task needs exact behavior:

- `Documentation~/SubScenes.md`
- `BovineLabs.Core.Extensions.Authoring/SubScenes/SubSceneSettings.cs`
- `BovineLabs.Core.Extensions.Authoring/SubScenes/SubSceneSet.cs`
- `BovineLabs.Core.Extensions.Authoring/SubScenes/AssetSet.cs`
- `BovineLabs.Core.Extensions.Authoring/SubScenes/SubSceneLoadAuthoring.cs`
- `BovineLabs.Core.Extensions.Authoring/SubScenes/SubSceneAuthUtil.cs`
- `BovineLabs.Core.Extensions/SubScenes/SubSceneLoadingSystem.cs`
- `BovineLabs.Core.Extensions/SubScenes/SubSceneLoadingManagedSystem.cs`
- `BovineLabs.Core.Extensions/SubScenes/AssetLoadingSystem.cs`
- `BovineLabs.Core.Extensions/SubScenes/SubScenePostLoadCommandBufferSystem.cs`

## Scope

This skill is for:

- authoring `SubSceneSettings`, `SubSceneSet`, `SubSceneEditorSet`, and `AssetSet`
- baking/load-entity setup with `SubSceneLoadAuthoring`
- runtime `LoadSubScene` and `SubSceneLoaded` behavior
- world targeting via `SubSceneLoadFlags`
- post-load managed setup and editor tooling

If the task is primarily settings retrieval or singleton creation, also use the settings skill.
If the task is primarily initialize/destroy sequencing after entities exist, also use the lifecycle skill.

## Authoring Model

`SubSceneSettings` is the root asset. It holds:

- `SceneSets` for runtime subscene sets
- `EditorSceneSets` for editor override/live-baking tooling
- `AssetSets` for GameObject asset instantiation tied to worlds

`SubSceneLoadAuthoring` bakes entities that drive both subscene loading and asset loading.

If you are adding a new world bootstrap flow, start at `SubSceneSettings` plus `SubSceneLoadAuthoring`, not at runtime systems.

## Target World Rules

Use `SubSceneLoadFlags` to describe where a set belongs.

Common targets:

- `Game`
- `Service`
- `Client`
- `Server`
- `ThinClient`
- `Menu`

Only target the worlds that should actually own the content. Do not mark everything as "all worlds" by default.

Use `SubSceneLoadFlagsUtility` and `SubSceneLoadUtil` behavior as the source of truth when changing world routing.

## Load Rule Semantics

`SubSceneSet` has three important knobs:

- `IsRequired`: the world should block until the scene is loaded.
- `WaitForLoad`: wait for the load to finish now, but without treating it as permanently required.
- `AutoLoad`: enable loading when the world starts.

Use them intentionally:

- Bootstrap scene that must exist before gameplay:
  `AutoLoad = true`, usually `IsRequired = true`.
- Content toggled later by runtime logic:
  `AutoLoad = false`, toggle `LoadSubScene` at runtime.
- Optional load that still needs to finish before continuing the current phase:
  use `WaitForLoad`.

Do not blur `IsRequired` and `AutoLoad`. One is about blocking policy, the other is about initial enable state.

## Runtime Control

Runtime control is through enableable components on the baked load entity:

- enable `LoadSubScene` to request load
- disable `LoadSubScene` to request unload
- inspect `SubSceneLoaded` to see whether the load completed

This is the intended runtime API after baking.

Do not add parallel "requested/loaded" components unless the built-in load entity model is genuinely insufficient.

## Pause Interaction

`SubSceneLoadingSystem` pauses the world with `PauseGame.Pause(ref state, true)` while required loading is in progress.

That means subscene bugs often look like pause bugs:

- world stays paused because a required scene never reports loaded
- wrong target-world flags mean the scene is never eligible in the current world
- load entity exists but `LoadSubScene` was never enabled

If the world seems stuck during bootstrap, inspect these before changing pause code.

## Asset Loading

`AssetSet` is for managed GameObjects that should exist alongside particular worlds.

- configure them in `SubSceneSettings`
- let `SubSceneLoadAuthoring` bake the asset load data
- let `AssetLoadingSystem` own instantiation/lifetime

Do not replace this with ad hoc MonoBehaviour bootstrap code when the asset belongs to a world lifecycle.

## Post-Load Managed Work

Use `ICreatePostLoadCommandBuffer` plus `SubScenePostLoadCommandBufferSystem` when a scene needs managed post-load command buffer setup.

This is the extension point for scene-specific post-load work.

Do not bolt custom reflection or one-off scene setup directly into `SubSceneLoadingSystem`.

## Editor Tooling

The built-in editor support covers:

- scene override via `SubSceneEditorSet`
- live baking toolbar behavior
- prebake scene support through editor settings

If the task is editor UX around subscene set selection, inspect the editor files under `BovineLabs.Core.Extensions.Editor/SubScenes`.

## Common Patterns

### Add A New Bootstrap Scene

1. Create or update `SubSceneSettings`.
2. Add a `SubSceneSet`.
3. Set target worlds precisely.
4. Mark `AutoLoad` and `IsRequired` only if the world truly must wait.
5. Reference the settings asset from `SubSceneLoadAuthoring`.

### Toggle A Scene At Runtime

1. Find the baked load entity for the set.
2. Enable or disable `LoadSubScene`.
3. Observe `SubSceneLoaded` rather than inventing a second completion signal.

### Add World-Tied Managed Assets

1. Add an `AssetSet`.
2. Set target worlds.
3. Let `AssetLoadingSystem` own instantiation and cleanup.

## Failure Checklist

- Scene never loads:
  Check target-world flags and whether `LoadSubScene` is enabled.
- World remains paused after startup:
  Check `IsRequired` and `WaitForLoad` scenes first.
- Content loads in the wrong world:
  Check `SubSceneLoadFlags` and conversions in `SubSceneLoadUtil`.
- Runtime code added new manual scene-loading state:
  Prefer the baked load entity plus `LoadSubScene`/`SubSceneLoaded`.
- Managed assets are instantiated manually beside an asset set:
  Consolidate ownership under `AssetLoadingSystem` if they share world lifecycle.
