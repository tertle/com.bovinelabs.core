---
name: bl-core-pause
description: "Use when creating, wiring, extending, or debugging com.bovinelabs.core pause behavior, including PauseGame, IUpdateWhilePaused/IDisableWhilePaused markers, PauseUtility overrides, and pause-aware system groups."
---

# Core Pause Usage

Use this skill for world pause behavior, pause-aware system placement, and pause-policy debugging.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/pause.md`.
2. Decide whether the task is normal pause, full pause, or pause-aware system placement.
3. Prefer marker interfaces on owned systems and groups before using `PauseUtility` overrides.
4. Verify the chosen system group and pause mode match the intended runtime behavior.

## Routing

- `references/pause.md`: `PauseGame`, `PauseLimitSystem`, `PauseRateManager`, `PauseUtility`, and `IUpdateWhilePaused`/`IDisableWhilePaused` usage rules.
