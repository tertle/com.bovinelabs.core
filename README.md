# BovineLabs Core

BovineLabs Core provides extensions, containers, and tools for building games with Unity DOTS (Data-Oriented Technology Stack).

For support and discussions, join [Discord](https://discord.gg/RTsw6Cxvw3).

If you want to support my work or get access to a few private libraries, [Buy Me a Coffee](https://buymeacoffee.com/bovinelabs).

## Installation

The latest version of the library is available on [GitLab](https://gitlab.com/tertle/com.bovinelabs.core). The project is actively worked on daily in various branches.

Once installed, the library provides utilities, custom containers, and high-performance extensions without disrupting your existing workflow.

## Core

| Feature                                                          | Description                                                                                                                           |
|------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| [Change Filter Tracking](Documentation~/ChangeFilterTracking.md) | Allows you to track how frequently a component triggers a change filter and warns you if it is happening too frequently               |
| [Collections](Documentation~/Collections.md)                     | Specialized collection types with performance optimizations, thread safety, and ECS-focused functionality                             |
| [Debug](Documentation~/Debug.md)                                 | Comprehensive debugging and assertion utilities with Burst compatibility and ECS-specific debugging support                           |
| [DynamicHashMap](Documentation~/DynamicHashMap.md)               | Adds HashMap support to entities                                                                                                      |
| [EntityCommands](Documentation~/EntityCommands.md)               | Provides a shared interface between EntityManager, EntityCommandBuffer, EntityCommandBuffer.ParallelWriter and IBaker                 |
| [Facets](Documentation~/Facets.md)                               | Source-generated `IFacet` helpers that provide aspect-like access via lookups and chunk iteration                                     |
| [Extensions](Documentation~/Extensions.md)                       | Extension methods that enhance Unity's DOTS APIs with performance optimizations and convenience methods                               |
| [Functions](Documentation~/Functions.md)                         | Extensible way to add support for extending jobs to other developers or modders                                                       |
| [Iterators](Documentation~/Iterators.md)                         | High-performance iterator utilities for ECS applications with Burst-compatible enumeration capabilities                               |
| [Jobs](Documentation~/Jobs.md)                                   | Custom jobs (IJobForThread, IJobParallelForDeferBatch, IJobHashMapDefer, IJobParallelHashMapDefer)                                    |
| [K](Documentation~/K.md)                                         | K is a type-safe, Burst-compatible alternative to Enums and LayerMasks that allows you to define key-value pairs in settings files    |
| [Settings](Documentation~/Settings.md)                           | Settings framework for managing and creating settings                                                                                 | 
| [Singleton Collection](Documentation~/SingletonCollection.md)    | Easily set up a Many-To-One container singleton with minimal boilerplate and syncless job support                                     | 
| [Spatial](Documentation~/Spatial.md)                             | Fast spatial hashmap generation                                                                                                       |
| [States](Documentation~/States.md)                               | Provides states on entities by mapping a bit field to components automatically                                                        |

| Utility                                                 | Description                                                                                         |
|---------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| [BurstTrampoline](Documentation~/BurstTrampoline.md)    | Invoke managed delegates from Burst code with reusable trampolines and optional out parameters      |
| [Global Random](Documentation~/GlobalRandom.md)         | A static random usable from everywhere, even parallel burst jobs                                    |
| [PooledNativeList](Documentation~/PooledNativeList.md)  | High-performance, thread-safe pooling system for Unity's NativeList collections                     |
| [Utility](Documentation~/Utility.md)                    | Comprehensive collection of utility classes and helpers for high-performance Unity DOTS development |

## Extensions

The Core library maintains the status quo in your project by default. However, there are powerful features that can be manually enabled, which may require changes to your workflow. To enable these features, navigate to the `BovineLabs -> Toggle Features` menu.

![Toggle Features](Documentation~/Images/ToggleFeatures.png)

| Feature                                                 | Description                                                                                           |
|---------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| [Analyzers](Documentation~/Analyzers.md)                | Automatic Roslyn analyzer integration infrastructure for seamless code analysis and style enforcement |
| [Camera](Documentation~/Camera.md)                      | ECS camera integration with frustum culling and Unity Camera synchronization                          |
| [EntityBlob](Documentation~/EntityBlob.md)              | Memory-efficient storage of multiple BlobAssetReferences in a single blob using perfect hash maps     |
| [Input](Documentation~/Input.md)                        | Support for input integrated with entities using source generation and common properties              |
| [Life Cycle](Documentation~/LifeCycle.md)               | Framework for managing entity initialization and destruction                                          |
| [Object Management](Documentation~/ObjectManagement.md) | Automatic ID, category and group management                                                           |
| [Pause](Documentation~/Pause.md)                        | World-level pause system with fine-grained control over system updates during pause states            |
| [PhysicsStates](Documentation~/PhysicsStates.md)        | Stateful collision and trigger event tracking with Enter/Stay/Exit states for Unity Physics           |
| [PhysicsUpdate](Documentation~/PhysicsUpdate.md)        | Ensures Unity Physics spatial data remains current at high frame rates above fixed timestep           |
| [SubScenes](Documentation~/SubScenes.md)                | Enhanced SubScene loading with world targeting and editor tools                                       |
