# Changelog

## [1.0.0-pre.4] - 2023-09-18

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
