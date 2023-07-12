# Changelog

## [0.15.5] - 2023-07-12

### Added
* NativeThreadRandom
* Entropy feature singleton which maintains a NativeThreadRandom
* SetComponentEnabled to IConvert
* Extensions for IEnablebale - CopyEnableMaskFrom, GetEnabledBitsRO, GetRequiredEnabledBitsRO, GetEnabledBitsRW, GetRequiredEnabledBitsRW
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