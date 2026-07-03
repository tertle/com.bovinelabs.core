---
name: bl-core-subscenes
description: "Use for BovineLabs Core SubScene settings, world-targeted loading, scene or asset load requests, editor tooling, or load failures."
---

# Core SubScenes

Resolve Core source from `Packages/com.bovinelabs.core` or its exact package-cache entry. Keep world routing, scene loading, managed asset loading, and post-load work separate.

## Authoring Model

`SubSceneSettings` owns runtime `SceneSets`, editor override sets, and world-tied `AssetSets`. `SubSceneLoadAuthoring` bakes the entities consumed by loading systems. Start new bootstrap flows in these assets rather than patching runtime systems.

## World And Load Policy

Use precise `SubSceneLoadFlags` such as `Game`, `Service`, `Client`, `Server`, `ThinClient`, or `Menu`; do not default to every world.

| Setting | Meaning |
|---|---|
| `AutoLoad` | Enable the load request at world start |
| `IsRequired` | Block world progress until loaded |
| `WaitForLoad` | Wait during the current phase without making the scene permanently required |

Typical bootstrap content uses `AutoLoad = true` and `IsRequired = true`; later-toggle content uses `AutoLoad = false`.

## Runtime Contract

- Enable `LoadSubScene` on the baked load entity to request load; disable it to request unload.
- Read `SubSceneLoaded` for completion instead of adding parallel requested/loaded components.
- Required loading calls `PauseGame.Pause(ref state, true)`, so a stuck bootstrap often means an ineligible world flag, disabled request, or required scene that never completes.

## Managed Assets And Post-Load Work

- Configure world-tied GameObjects in `AssetSet` and let `AssetLoadingSystem` own their lifetime.
- Use `ICreatePostLoadCommandBuffer` with `SubScenePostLoadCommandBufferSystem` for managed post-load setup.
- Do not add one-off MonoBehaviour bootstrap ownership or reflection inside `SubSceneLoadingSystem` when these extension points fit.

## Editor Tooling

Use `SubSceneEditorSet`, live-baking toolbar support, and prebake settings for editor workflows. Inspect `BovineLabs.Core.Extensions.Editor/SubScenes` before adding custom scene-selection UX.

## Failure Triage

- Scene never loads: check target-world flags and whether `LoadSubScene` is enabled.
- World stays paused: inspect required/waiting scenes and `SubSceneLoaded`.
- Content appears in the wrong world: inspect flag conversion in `SubSceneLoadUtil`.
- Managed assets are instantiated twice: consolidate ownership under `AssetLoadingSystem`.
- Runtime code mirrors load state: use the baked load entity contract instead.

Read `Documentation~/SubScenes.md`, `SubSceneSettings.cs`, `SubSceneLoadAuthoring.cs`, and the relevant loading-system source for exact behavior.
