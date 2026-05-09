---
name: bl-core-config-vars
description: "Use when adding, changing, or debugging com.bovinelabs.core ConfigVars, including ConfigVarAttribute/ConfigurableAttribute declaration, ConfigVarManager initialization behavior, and ConfigVars editor window bindings."
---

# Core Config Vars Usage

Use this skill for ConfigVar declaration, manager wiring, and editor panel behavior.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/config-vars.md`.
2. Define config vars on `[Configurable]` owners using supported `SharedStatic<T>` types.
3. Follow naming/value-precedence rules and failure checklist before adding new var types or bindings.

## Routing

- `references/config-vars.md`: `ConfigVarAttribute`, `ConfigurableAttribute`, `ConfigVarManager`, ConfigVars window/panels, and value source precedence.
