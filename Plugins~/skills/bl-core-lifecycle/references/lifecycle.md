# Lifecycle Reference

Use this reference when implementing or debugging core entity initialization/destruction behavior.

## Read Order

1. `Documentation~/LifeCycle.md`
2. `BovineLabs.Core.Extensions.Authoring/LifeCycle/LifeCycleAuthoring.cs`
3. `BovineLabs.Core.Extensions/LifeCycle/InitializeEntity.cs`
4. `BovineLabs.Core.Extensions/LifeCycle/InitializeSubSceneEntity.cs`
5. `BovineLabs.Core.Extensions/LifeCycle/DestroyEntity.cs`
6. `BovineLabs.Core.Extensions/LifeCycle/InitializeSystemGroup.cs`
7. `BovineLabs.Core.Extensions/LifeCycle/SceneInitializeSystem.cs`
8. `BovineLabs.Core.Extensions/LifeCycle/InitializeEntitySystem.cs`
9. `BovineLabs.Core.Extensions/LifeCycle/DestroySystemGroup.cs`
10. `BovineLabs.Core.Extensions/LifeCycle/DestroyOnDestroySystem.cs`
11. `BovineLabs.Core.Extensions/LifeCycle/DestroyOnSubSceneUnloadSystem.cs`
12. `BovineLabs.Core.Extensions/LifeCycle/DestroyEntitySystem.cs`
13. `BovineLabs.Core.Extensions/LifeCycle/DestroyTimer.cs`
14. `BovineLabs.Core.Extensions/InstantiateCommandBufferSystem.cs`
15. `BovineLabs.Core.Extensions/LifeCycle/EndInitializeEntityCommandBufferSystem.cs`
16. `BovineLabs.Core.Extensions/LifeCycle/DestroyEntityCommandBufferSystem.cs`

## Setup Rules

1. Add `LifeCycleAuthoring` to authored entities that must participate in lifecycle phases.
2. For additional baked entities, call `LifeCycleAuthoring.AddComponents(IBaker, Entity, bool isPrefab)`.
3. Prefabs use `InitializeEntity` (enabled on bake); subscene entities use `InitializeSubSceneEntity` (enabled on bake).
4. Entities participating in lifecycle destruction must have `DestroyEntity` (default disabled on bake).

## Initialization Rules

1. Place one-shot initialization systems in `InitializeSystemGroup`.
2. Query with:
   - `[WithAll(typeof(InitializeEntity))]` for prefab-instantiated entities.
   - `[WithAll(typeof(InitializeSubSceneEntity))]` for opted-in subscene entities.
   - `[WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))]` for both.
3. Do not manually disable initialize components; `InitializeEntitySystem` (order last) disables them after initialize phase.

## Destruction Rules

1. In gameplay/runtime systems, request destruction by enabling `DestroyEntity` instead of directly destroying entities.
2. `DestroyOnDestroySystem` propagates destruction through `LinkedEntityGroup` hierarchies.
3. `DestroyOnSubSceneUnloadSystem` enables `DestroyEntity` for entities in unloading subscenes.
4. `DestroyEntitySystem` performs final destruction in `DestroySystemGroup` (order last).
5. On client worlds with NetCode, ghost entities are excluded from `DestroyEntitySystem` destruction queries.

## Command Buffer Rules

1. Use `InstantiateCommandBufferSystem.Singleton` for instantiation-phase structural work.
2. Use `EndInitializeEntityCommandBufferSystem.Singleton` for deferred commands at end of initialize phase.
3. Use `DestroyEntityCommandBufferSystem.Singleton` for deferred commands during destroy phase.
4. Keep lifecycle commands in these phase systems to preserve ordering and avoid ad-hoc sync points.

## Ordering Notes

1. `InitializeSystemGroup` and `DestroySystemGroup` both run in `BeforeSceneSystemGroup`, after `InstantiateCommandBufferSystem`.
2. `InitializeSystemGroup` runs before `DestroySystemGroup`.
3. `SceneInitializeSystem` (order first in `BeginSimulationSystemGroup`) explicitly updates `InitializeSystemGroup`.
4. During full pause (`PauseGame.PauseAll`), `InitializeSystemGroup` early-outs.

## DestroyTimer Rules

1. `DestroyTimer<T>` requires `sizeof(T) == sizeof(float)`.
2. Call `destroyTimer.OnCreate(ref state)` in system `OnCreate`.
3. Call `destroyTimer.OnUpdate(ref state)` each update to decrement timers and enable `DestroyEntity` at zero.

## Failure Checklist

1. Initialize logic not running:
   - Verify initialize component exists and is enabled.
   - Verify system is in `InitializeSystemGroup`.
2. Entities not being destroyed:
   - Verify `DestroyEntity` exists and is enabled.
   - Verify code is enabling, not just reading, the enableable component.
3. Child entities survive parent destroy:
   - Verify parent has `LinkedEntityGroup`.
   - Verify child entities have `DestroyEntity` component.
4. Timer-based destruction not firing:
   - Verify timer component is float-sized.
   - Verify `DestroyTimer<T>.OnCreate` is called.
5. Subscene unload cleanup not happening:
   - Verify entities carry `DestroyEntity` and belong to the unloading scene section.
