# Changelog

## [1.2.0] - 2023-11-09

### Added
* Added GetChunkComponentDataRW to ArchetypeChunk

### Changed
* Bumped requirement to Entities 1.2.0-exp.3
* Renamed InputSettings to InputCommonSettings to avoid bug in Input package
* Moved K back into core

### Fixed
* Fixed compile errors when object definitions were disabled

## [1.1.5] - 2023-11-09

### Fixed
* ObjectCategories looking in wrong place when baking

### Changed
* Bumped requirement to Entities 1.1.0-pre.3

## [1.1.4] - 2023-11-09

### Added
* BeginSimulationSystemGroup
* SetChangeFilter and GetRefRWNoChangeFilter extensions to ComponentLookup
* Custom SearchProvider for ObjectDefinitions
* SelectedEntities if you have a way to select multiple

### Fixed
* UIDAttributeDrawer when using nested scriptable objects
* KAttributeDrawer on flags if there were holes

### Changed
* Moved K into extensions
* K assets now need to exist in a K folder inside resources to speed up editor iteration time
* You now need to place [Configurable] on any class/struct that uses ConfigVar. This significantly speeds up the initialization

### Removed
* Removed dependency window as you can use https://github.com/Unity-Technologies/com.unity.search.extensions/wiki/dependency-viewer instead

## [1.1.3] - 2023-10-27

### Fixed
* Destroy system with Netcode

## [1.1.2] - 2023-10-27

### Added
* Added GetOrDefault to DynamicHashMap
* Added StructuralCommandBufferSystem to LifeCycle
* UnsafeMultiHashMap
* DestroyOnDestroySystem to support LinkedEntityGroup with DestroyEntity

### Changed
* Destroy renamed LifeCycle as it now encompasses creating and changes as well
* EntityDestroyX have been renamed DestroyEntityX

### Fixed
* Fixed UnsafeComponentLookup in builds

### Documentation
* Added documentation for Entropy
* Added documentation for Spatial

## [1.1.1] - 2023-10-18

### Added
* More batch operations for hash maps
* AssemblyBuilder can now add Entities.Graphics
* IJobHashMapDefer and IJobParallelHashMapDefer to replace removed jobs
* FixedArray<T, TS>
* FunctionBuilder to pass FunctionPointers with data into jobs to allow extended behaviour
* UnsafeComponentLookup and UnsafeBufferLookup
* SearchElement

### Changed
* AssemblyBuilder now sets auto reference false
* Updated ObjectDefinitionSystem to handle closing SubScenes
* Added a range check to ObjectDefinitionRegistry
* ReflectionUtility now also returns structs
* Renamed UnsafeDynamicBufferAccessor to UnsafeUntypedDynamicBufferAccessor to match what it returns
* GetOrAddRef extension now has optional default value
* Added override to ChangeFilterLookup to specify version

### Removed
* IJobHashMapVisitKeyValue and IJobParallelHashMapVisitKeyValue as these were unsafe when scheduling when a job was resizing capacity

### Documentation
* Added documentation for Functions

## [1.1.0] - 2023-09-24

### Changed
* Updated for Entities 1.1.0 support. Stick to 1.0.0 if you are still on Entities 1.0.X
* Removed unity version requirement

### Deleted
* ColliderInspector
* SubSceneInspectorUtility
* DisableRenderingHelper

## [1.0.0] - 2023-09-21

### Added
* A modified copy of Lieene blob curve
* Contains(Key,Value) to DynamicMultiHashMap
* CountValuesForKey to DynamicMultiHashMap
* DynamicHashMapKeyEnumerator for DynamicMultiHashMap
* DynamicPerfectHashMap
* GetSingletonBufferNoSync extension for EntityQuery
* Added a copy of pinLock from the Entities library

### Changed
* Reworked ObjectDefinition a little for modding support

### Fixed
* PauseSystem breaking with headless server
* AssemblyBuilderWindow now correctly updates path when using one column layout

### Documentation
* Added documentation for SubScenes
* Added IDynamicPerfectHashMap to DynamicHashMap

## [1.0.0-pre.3] - 2023-08-18

### Added 
* SettingsGroupAttribute for grouping in the SettingsWindow
* float2 Rotate method to mathex
* CollectionCreator for a easy UnsafeCollection*
* Added Remove(key, out value) extension for NativeHashMap and UnsafeHashMaps

### Changed
* Updated to support Entities 1.0.14
* PauseSystem now ticks simulation and presentation command buffers in case they were used previous frame

### Fixed
* Fixed building player issues
* KAttributeDrawer was shifting non-flagged values

## [1.0.0-pre.2] - 2023-08-04

### Added 
* Instantiate and create methods to IEntityCommands
* DestroyTimer
* GetOrAddRef to NativeHashMap

### Changed
* NativeThreadRandom renamed ThreadRandom as NativeContainer and safety has been removed (always thread safe within job context)
* IConvert renamed to IEntityCommands
* NativeEventStream renamed to NativeThreadStream
* Various namespaces updates
* Updated Stateful events not to include EntityA. EntityB will now always be the other entity.

### Documentation
* Added documentation for SingletonCollections
* Added documentation for EntityCommands
* Added documentation for DynamicHashMap

## [1.0.0-pre.1] - 2023-08-01

### Added 
* Added GetEnableablMask to archetype extensions for IEnableabale extensions
* Support for ObjectDefinition to support child ScriptableObjects
* Added support to edit other package components to make them IEnableabale
* Added WorldSafeShutdown to extensions that ensures a deterministic shutdown and systems are stopped before subscenes are unloaded
* Added StatefulTriggerEvent and StatefulCollisionEvent to extensions. They provide the same functionality as Unity's implementation except rewritten to be parallel providing 5x+ performance gains
* Added NativeMultiHashMap
* Added UIDCreateAttribute to adding an easy right click create menu for ObjectManagement

### Changed
* Renamed CopyEnablable to CopyEnableable
* Renamed BufferCapacitySettings to TypeManagerOverrideSettings. You'll need to update the file if you use this.
* Rewrote DynamicHashMap back end for performance improvements
* Reworked asset creator to now not require inheritances, instead looks for the AssetCreatorAttribute

### Fixed
* Fixed NativeThreadRandom seeds all being the same
* Added a workaround for unity disposing log handle before world shutdown
* Fixed missing define in Core.Tests causing unit tests to incorrectly appear in runner
* Fixed ObjectGroupMatcher not being initialized
* NetCode breaking VirtualChunks if systems landed out of order
* ObjectCategories incorrectly bringing in baking GameObjects into the build
* TypeMangerEx loading wrong file in builds

### Documentation
* Added documentation for K
* Added documentation for States
* Added documentation for Jobs
* Updated outdated setup documentation for Object Definitions

## [0.15.5] - 2023-07-12

### Added
* NativeThreadRandom
* Entropy feature singleton which maintains a NativeThreadRandom
* SetComponentEnabled to IConvert
* Extensions for IEnableabale - CopyEnableMaskFrom, GetEnabledBitsRO, GetRequiredEnabledBitsRO, GetEnabledBitsRW, GetRequiredEnabledBitsRW
* AddUntypedBuffer and a new variation of UnsafeAddComponent to ECB
* UnityBakingSettings component that can be read in BakingSystems to expose baking info

### Changed
* DynamicHashMaps now need to be initialized explicitly (using during baking) with Initialize instead of checking in AsHashMap
* Reworked ObjectManagementSettings to ensure bakers detect changes and fixed some lingering UI issues
* EntityDestroy is now IEnableableComponent instead
* IJobHashMapVisitKeyValue now also passes in jobIndex

### Fixed
* KMap at max capacity infinite looping

## [0.15.4] - 2023-06-13

### Fixed
* Compile error when Extensions enabled but not ObjectDefinitions

## [0.15.3] - 2023-06-12

### Added
* Added ObjectGroup and tied it into the rest of the ObjectManagement
* Documentation for ObjectManagement. This is found in the BovineLabs.Core.Extensions/ObjectManagement folder
* VirtualChunks now copy baked data
* VirtualChunks are now defined by a string which can be configured to a specific group from settings
* Support for custom versions of K with extra data
* HasComponent to UnsafeEnableableLookup 
* More extensions for hash maps and native slice
* EditorWorldSafeShutdown to work around bug in entities when an entity is selected and changing play modes

### Changed
* Updated to support entities 1.0.10
* Switched to MallocTracked
* Switched to ThreadIndexCount
* Removed EndDestroyEntityCommandBufferSystem, now uses BeginInitializationCommandBufferSystem

### Fixed
* Critical bug in all the DynamicHash collections when instantiating from an existing entity with a collection would point to wrong memory
