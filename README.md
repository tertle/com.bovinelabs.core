# BovineLabs Core
Core library that provides a lot of extensions, new containers and tools for building games with DOTS.

Support: https://discord.gg/RTsw6Cxvw3

## Installation

The latest version of the library is available from [gitlab](https://gitlab.com/tertle/com.bovinelabs.core) and is worked on daily in various branches.

Every month or so a new stable version is pushed to [github](https://github.com/tertle/com.bovinelabs.core) and [openupm](https://openupm.com/packages/com.bovinelabs.core/).

When installed the library includes a large core set of features and extensions that should have no but should make no obvious changes to your workflow by default. This will allow you access to various utility, custom containers and high performance extensions etc without changing your workflow.

## Features
Features I have documented are listed below. This is but a tiny fraction of the features however I'm going to try to start documenting more so this list should hopefully grow.

| Feature | Description                                                                                            |
|-------|--------------------------------------------------------------------------------------------------------|
|[Jobs](BovineLabs.Core/Jobs/README.md)| Custom jobs (IJobParallelForDefer, IJobHashMapVisitKeyValue).                                          |
|[K](BovineLabs.Core/Keys/README.md)| K is an Enum or LayerMask alternative that allows you to define your key-value pairs in setting files. |
|[Object Management](BovineLabs.Core.Extensions/ObjectManagement/README.md)| Automatically ID, category and group management.                                                       |
|[States](BovineLabs.Core/States/README.md)| Provides states on entities by mapping a bit field to components automatically.                        |

## Toggle Features

Core strives to make no changes to your project by default however there are a collection of powerful features that can be manually enabled that may require changes to workflow that can be enabled via the `BovineLabs -> Toggle Features` menu.

![Toggle Features](Images~/ToggleFeatures.png)
