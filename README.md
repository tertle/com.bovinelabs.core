# BovineLabs Core

BovineLabs Core is a library that provides numerous extensions, new containers, and tools for building games with DOTS (Data-Oriented Technology Stack).

For support and discussions, join [Discord](https://discord.gg/RTsw6Cxvw3).

If you want to support my work or get access to a few private libraries, [Buy Me a Coffee](https://buymeacoffee.com/bovinelabs).

## Installation

Stick to the version that matches your entities version. The incremental versions do not have to match. For example, users on Entities 1.3.X stick with Core
1.3.Y.

The latest version of the library is available on [GitLab](https://gitlab.com/tertle/com.bovinelabs.core). The project is actively worked on daily in various branches.

Every month or so, a new stable version is pushed to [GitHub](https://github.com/tertle/com.bovinelabs.core) and [OpenUPM](https://openupm.com/packages/com.bovinelabs.core/).

Once the library is installed, it grants you access to various utilities, custom containers, and high-performance extensions that should not cause any changes
to your workflow.

## Features

| Feature                                                          | Description                                                                                                                        |
|------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------|
| [Change Filter Tracking](Documentation~/ChangeFilterTracking.md) | Allows you to track how frequently a component triggers a change filter and warns you if it is happening too frequently            |
| [DynamicHashMap](Documentation~/DynamicHashMap.md)               | Adds HashMap support to entities                                                                                                   |
| [EntityCommands](Documentation~/EntityCommands.md)               | Provides a shared interface between EntityManager, EntityCommandBuffer, EntityCommandBuffer.ParallelWriter and IBaker              |
| [Global Random](Documentation~/GlobalRandom.md)                  | A static random usable from everywhere, even parallel burst jobs                                                                   |
| [Input](Documentation~/Input.md)                                 | Support for input integrated with entities using source generation and common properties                                           |
| [Life Cycle](Documentation~/LifeCycle.md)                        | Framework for managing entity initialization and destruction                                                                       |
| [Jobs](Documentation~/Jobs.md)                                   | Custom jobs (IJobParallelForDeferBatch, IJobHashMapVisitKeyValue)                                                                  |
| [Functions](Documentation~/Functions.md)                         | Functions provide an easy way to add support for extending jobs to other developers or modders                                     |
| [K](Documentation~/K.md)                                         | K is a type-safe, Burst-compatible alternative to Enums and LayerMasks that allows you to define key-value pairs in settings files |
| [Object Management](Documentation~/ObjectManagement.md)          | Automatic ID, category and group management                                                                                        |
| [Settings](Documentation~/Settings.md)                           | Settings framework for managing and creating settings                                                                              | 
| [Singleton Collection](Documentation~/SingletonCollection.md)    | Easily set up a Many-To-One container singleton with minimal boilerplate and syncless job support                                  | 
| [Spatial](Documentation~/Spatial.md)                             | Spatial provides a very fast to generate spatial hashmap                                                                           |
| [States](Documentation~/States.md)                               | Provides states on entities by mapping a bit field to components automatically                                                     |
| [SubScenes](Documentation~/SubScenes.md)                         | Provides convenient features for SubScene loading                                                                                  |

## Utility

| Utility                                                        | Description                                                                                           |
|----------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| [PooledNativeList](Documentation~/Utility/PooledNativeList.md) | A high-performance, thread-safe pooling system for Unity's NativeList collections with Burst support  |

## Toggle Features

The Core library strives to maintain the status quo in your project by default. However, there is a collection of powerful features that can be manually
enabled, which may require changes to your workflow. To enable these features, navigate to the `BovineLabs -> Toggle Features` menu.

![Toggle Features](Documentation~/Images/ToggleFeatures.png)