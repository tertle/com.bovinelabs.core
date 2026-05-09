# Core Pause Reference

## Read First

Use these sources when the task needs exact behavior:

- `Documentation~/Pause.md`
- `BovineLabs.Core.Extensions/Pause/PauseGame.cs`
- `BovineLabs.Core.Extensions/Pause/PauseUtility.cs`
- `BovineLabs.Core.Extensions/Pause/PauseRateManager.cs`
- `BovineLabs.Core.Extensions/Pause/PauseLimitSystem.cs`
- `BovineLabs.Core/Pause/IUpdateWhilePaused.cs`
- `BovineLabs.Core/Pause/IDisableWhilePaused.cs`

## Model

`PauseGame` is the world-level pause switch.

- `PauseGame.Pause(ref state, pauseAll: false)` is normal gameplay pause.
- `PauseGame.Pause(ref state, pauseAll: true)` is full bootstrap/loading pause.
- `PauseGame.Unpause(ref state)` resumes updates.
- `PauseGame.IsPaused(ref state)` checks current state.

Do not add ad hoc "paused" singletons or custom root-group disabling when the built-in pause flow already covers the case.

## Choose The Right Pause Mode

- Use normal pause when gameplay should stop but most of the world can still update.
  Typical example: menus, client UX, limited service logic.
- Use full pause when only explicitly whitelisted systems should keep updating.
  Typical example: initial world creation, required subscene loading, critical bootstrapping.

`PauseGame.Pause` always updates the component even if the world is already paused, so switching between pause modes is valid.

## Marker Interfaces First

Prefer these on your own systems and groups:

- `IUpdateWhilePaused`: the system must continue updating during both pause modes.
- `IDisableWhilePaused`: the root system group should stop during normal pause.

This repo already uses these patterns:

- `BLSimulationSystemGroup` implements `IDisableWhilePaused`.
- `SingletonInitializeSystemGroup`, `SceneInitializeSystem`, and several UI/input groups implement `IUpdateWhilePaused`.

If you own the code, add the interface instead of editing `PauseUtility`.

## When To Use `PauseUtility`

Use `PauseUtility.UpdateWhilePaused` or `PauseUtility.DisableWhilePaused` only for systems you do not control cleanly, such as third-party or awkward root-group cases.

Do not use these static sets as the default policy for first-party code.

## Group Interaction Rules

- Anything under `BLSimulationSystemGroup` is disabled during normal pause unless a child system is explicitly allowed to update while paused.
- If an entire custom root simulation group should stop during normal pause, deriving from `BLSimulationSystemGroup` is the simplest choice.
- If only a few child systems must continue during pause, keep the group placement and mark those systems with `IUpdateWhilePaused`.
- If a whole feature must remain active during pause, do not casually place it under a pause-disabled root group and hope ordering fixes it.

Choose group placement first, then pause markers.

## Time Behavior

`PauseRateManager` freezes world elapsed time while paused to avoid catch-up spikes on resume.

Do not add manual time rewinds, elapsed-time resets, or special "skip first frame after unpause" code unless you have verified a real bug.

## Common Patterns

### Pause During Loading

- Use `PauseGame.Pause(ref state, true)` while required work finishes.
- Resume with `PauseGame.Unpause(ref state)` once the blocking condition is gone.
- This is the intended pattern for subscene/bootstrap flows.

### Keep UI Or Bootstrap Systems Running

- Mark the system or group with `IUpdateWhilePaused`.
- Only fall back to `PauseUtility.UpdateWhilePaused` for external systems.

### Disable A Whole Simulation Feature During Normal Pause

- Put the root group on `BLSimulationSystemGroup` or implement `IDisableWhilePaused` on the root group.

## Failure Checklist

- World appears frozen when only gameplay should pause:
  Check whether `pauseAll` was set to `true`.
- A system stopped but should have continued:
  Check whether it or its required root group needs `IUpdateWhilePaused`.
- A simulation feature still runs during normal pause:
  Check whether its root group derives from `BLSimulationSystemGroup` or otherwise implements `IDisableWhilePaused`.
- Pause policy is being driven from static registration code:
  Move first-party behavior to marker interfaces if possible.
- Resume causes timing spikes:
  Inspect custom time manipulation before blaming the built-in pause path.
