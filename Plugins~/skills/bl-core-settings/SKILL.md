---
name: bl-core-settings
description: "Use when creating, wiring, extending, refactoring, or debugging com.bovinelabs.core settings assets and retrieval paths, including ISettings, SettingsBase, SettingsSingleton generic patterns, world routing, and build-time setup."
---

# Core Settings

Use this skill for settings authoring, editor wiring, and runtime access patterns in core.
Resolve core package paths against `Packages/com.bovinelabs.core` or the matching `Library/PackageCache/com.bovinelabs.core@*`.

## Workflow

1. Read `references/settings.md`.
2. Choose the correct settings base type (`ISettings`, `SettingsBase`, or `SettingsSingleton<T>`).
3. Follow the reference authoring/access rules and failure checklist before changing call sites.

## Routing

- `references/settings.md`: `ISettings`, `SettingsBase`, `SettingsSingleton<T>`, world routing, authoring assignment, and settings retrieval.
