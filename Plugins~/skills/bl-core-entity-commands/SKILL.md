---
name: bl-core-entity-commands
description: "Use when creating, refactoring, or debugging com.bovinelabs.core IEntityCommands workflows, including shared builder methods, shared component setup, BakerCommands, CommandBufferCommands, CommandBufferParallelCommands, and EntityManagerCommands."
---

# Core Entity Commands Usage

Use this skill for reusable entity builder code that must work across baking, runtime command buffers, jobs, tests/editor setup, and shared component setup.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/entity-commands.md`.
2. Decide whether the logic should be generic over `IEntityCommands` or intentionally tied to one concrete command type.
3. Choose the implementation from execution context first, then write the shared helper.
4. If the builder also owns blob creation, coordinate with the blobs skill after the command-selection rules are clear.

## Routing

- `references/entity-commands.md`: `IEntityCommands`, concrete command implementations, local-entity rules, baker limitations, blob-store handling, and common builder/test patterns.
