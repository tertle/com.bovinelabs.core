# Changelog

## [1.5.2] - 2026-01-16

### Fixed
* Compiling < 6.3 - this will be the last support version
* No longer requires unity input system

## [1.5.1] - 2025-12-23

### Added
* Menu toggle added to WelcomeWindow
* Welcome window config
* Added 4 mod BurstTrampoline
* More safety on PooledNativeList
* BovineLabsBootstrap experimental hostworld support for netcode (if enabled)

### Changed
* Reduced InitializeOnLoad count to 1 
* Improved configvar window styling

### Fixed
* BurstTrampoline on certain IL2CPP configs
* CreateEditorWorld on leaving play mode
* TryGetValue for DynamicUntypedHashMap if offsetting was > 256
* DynamicUntypedHashMap resizeData incorrectly potentially adding too much capacity.

### Removed
* WorldAllocator
* EditorWorldSafeShutdown

## [1.5.0] - 2025-12-06

### Added
* New Manager Window that'll show once, now handles Extensions and can install other BovineLabs packages
* 6.4a4+ support
* 2.6.3 collections support
* Vector4 and Rect configvar support.
* IFacet, IAspect replacement
* Diagnostic warnings for Input and DynamicMaps
* Readonly support for CodeGenHelpers
* BurstTrampoline for easy breaking out of burst into managed code
* SyncEnableStateUtil
* PhysicsLayerUtil
* CameraMain to editor world for tooling
* AudioClipUnityObjectRefInspector
* Schedule to IJobHashMapDefer
* MainToolbarPresetAttribute and an IL PostProcessor to allow toolbar elements to be visible by default

### Changed
* Exposed mingrowth on UnsafeMultiHashMap
* Changed how Input works to work around Unity bug loses references to the Assets, run Update Settings from the setting BovineLabs->Settings->Core->Editor
* InputCommon no longer has Physics dependency
* Added overload to ClearRewind for enforced safety.
* PhysicsUpdate is now enablable as an extension

### Fixed
* GlobalRandom now initializes earlier to ensure it's ready before OnCreate when using AutomaticBootstrap

### Removed
* EntityCache
* FeatureWindow, replaced by Manager

## [1.4.7] - 2025-10-30

### Added
* MemoryLabelAllocator that wraps unity 6.3 MemoryLabel (with backwards compatability support for <6.3)
* CurveRemapUtility
* CameraSystemGroup
* ToSpline() on BlobSpline
* Evaluate to BlobSpline

### Changed
* Trying to use a list from PooledNativeList after it has returned will now throw exceptions

### Fixed
* ConfigVar reset
* BitFieldAttributeEditor now supports multiple fields with same name
* Pause fix for netcode 1.9
* NativeWorkQueue now supports custom allocators

### Removed
* ResourceSettingsAttribute - use SettingsSingleton which will auto include in builds

## [1.4.6] - 2025-10-03

### Breaking
* FeatureWindow now uses new EditorSettings scripting defines which will make existing projects incorrectly appear like features are disabled until reenabled again

### Added
* Can now load scenes as subscenes
* EnableableComponentAsset and a new shared ComponentAssetBase
* FrameCount to BlLogger
* Color support for config vars
* Right click context menu for config vars to reset or copy values
* New platform shared scripting defines in EditorSettings
* IJobForThread to schedule work across a fixed number of threads
* Custom inspector for EntitySceneReference
* SettingsSingleton<T> to reduce boilerplate
* ObjectInspectorProxy and PropertyInspector to inspector anything using Unitys prperty package
* ViewModelToolbar to inspect all UI Toolkit view models

### Changed
* Reload Toolbar Button no longer requires extensions (as you can hide it with new Unity Toolbar)
* Reworked the toolbar buttons a little
* TimeProfiler is now burstable
* Reworked the alive detection in the favourite/selection windows to fix odd Unity behaviour
* SubSceneLoadingSystem now pauses by default to fix fast world creation
* CameraMain now auto generates if no authoring setup.

### Fixed
* Startup Scene was incorrectly using Async causing it to break if it was a heavier scene
* FromToRotation
* Feature and Selection window selection when scrolled down
* Setting values with DynamicHashMap custom inspector
* Errors when BlobHashMap when capacity is 0
* Unloading runtime live scenes
* LoadPrefabAsEntity when no LEG

### Removed
* Support for Editor Toolbar below Unity 6.3
* ToEuler in mathex as math package has this method now
* KSettings, now uses SettingsSingleton<T>

## [1.4.5.1] - 2025-08-17

### Fixed
* SettingSubDirectoryAttribute not correctly creating directory

## [1.4.5] - 2025-08-17

### Added
* Selection history window
* Favourites window
* SubSettingsAttribute
* SettingsSingleton which auto includes settings in build
* Unity 6.3 support
* cid support for ObjectDefinitionSearchProvider
* BlobAssetStore to ECSTestsFixture
* ReflectionTestHelper
* FromToRotation
* BitWise internals

### Changed
* Updated ECSTestFixture for Entities latest changes
* K no longer goes in Resources, automatically added to builds
* Exposed SearchProviderType on ObjectDefinition for easy attribute usage
* Moved InitializeSystemGroup before DestroySystemGroup to allow for reacting to dead entities on same frame
* Added SceneInitializeSystem where InitializeSystemGroup used to exist that initializes new subscenes and ghosts

### Fixed
* Id bug in ObjectManagementProcessor which was causing ids to duplicate

### Removed
* ISpline from BlobSpline so you can no longer accidentaly use block breaking extension methods

### Documentation
* EntityBlob documentation added covering memory-efficient storage of multiple BlobAssetReferences
* ObjectDefinition search integration documentation added covering SearchContext attribute usage with filters
* LifeCycle documentation explaining destroy features

## [1.4.4] - 2025-07-12

### Added
* Scene toolbar button for opening and closing scenes in editor, there is a bunch of config in configvars
* Better exception handling to source generators
* 1.4.0-pre.3 support
* BlobSpline if the Unity Spline package is found
* Added an optional baker for SplineContainer, must be manually enabled with BL_BAKE_SPLINE
* int indexer to BitArray
* Source generator support for VariableMap - auto-generates Initialize() and AsMap() extension methods
* Source generator support for PerfectHashMap - auto-generates AsMap() extension method (Initialize() requires manual implementation)
* Added SetName to IEntityCommands

### Changed
* SubScenePrebakeSystem no longer forces settings to be generated
* AddUnique renamed to Add for BlobBuilderMultiHashMap
* InputSystemGroup now updates while paused
* Increased label size of config var window
* GlobalRandom now in Utility namespace

### Removed
* ListPool<T> - just use the built in one UnityEngine.Pool.ListPool<T>

### Fixed
* SingletonInitializeSystemGroup lifecycle requirement

### Documentation
* Major documentation restructuring
* Input documentation updated for new assembly structure and significantly streamlined
* ObjectManagement documentation updated for ObjectId changes and consolidated explanations
* Jobs documentation rewritten with examples and best practices
* EntityCommands documentation rewritten with usage patterns
* Settings documentation streamlined with simplified component overview
* Functions documentation improved with cleaner examples and reduced verbosity
* SubScenes documentation enhanced with better setup instructions and runtime examples)
* States documentation cleaned up with consolidated examples and removed incomplete sections
* LifeCycle documentation streamlined with simplified architecture tables
* K documentation improved with cleaner setup instructions and usage examples
* README.md updated with clearer descriptions and current job types
* GlobalRandom documentation improved
* ChangeFilterTracking documentation updated
* DynamicHashMap documentation enhanced with examples
* SingletonCollection documentation improved
* Collections documentation added covering all specialized collection types
* Extensions documentation added covering all extension methods
* Utility documentation added covering all utility classes and helpers
* Debug documentation added covering debugging and assertion utilities
* Iterators documentation added covering high-performance iterator utilities
* PhysicsStates documentation added covering stateful collision and trigger event tracking
* PhysicsUpdate documentation added covering high frame rate physics spatial data maintenance
* Camera documentation added covering ECS camera integration with frustum culling
* Pause documentation added covering world-level pause system with fine-grained control
* Analyzers documentation added covering automatic Roslyn analyzer integration infrastructure

## [1.4.3] - 2025-06-07

### Added
* ComponentAsset and ComponentFieldAsset for more stable type and fields instead of directly storing StableTypeHash and Offsets
* A Unity compatible and updated version of CodeGenHelpers https://github.com/dansiegel/CodeGenHelpers to use with source generators
* Source Generator for generating IDynamic[HashMap|HashSet|MultiHashMap|UntypedHashMap] Initialize and AsMap methods
* ProfilerTimer for quick easy scoped Timing
* BLGlobalLogger
* SubScenePostLoadCommandBufferSystem for allowing setup of PostLoadCommandBuffer from multiple places
* Added BL_TOOLS_MENU if you want to move the BovineLabs menu to Tools/BovineLabs
* SingletonAttribute which can be added to DynamicBuffers to auto merge multiple into a single singleton at runtime
* Support for AutoRef to optionally generate a 'null' asset
* DynamicVariableMap<TKey, TValue, T, TC> and DynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2> that allow you to have dynamic multiple column indexing
* MultiHashColumn and OrderedListColumn to be used in IDynamicVariableMap, you can implement your own type by implementing the IColumn interface

### Changed
* Scope limited source generators
* Disable auto reference of assemblies
* Reduced memory cost of MeshSimplifier to help support larger terrain
* Rewrote InputGenerator to use CodeGenHelpers
* Breaking: Input has been moved to its own assembly to scope limit the source generator
* Analyzers no longer inject in projects that match Unity.*
* ConfigVars now save on domain reload
* Named BLDebug to BLLogger and prefixed all BLDebug methods with Log; this now means you can double click Console to directly go to the correct stack
* Replaced all of Core Debug with BLLogger
* Performance optimizations to ObjectManagermentProcessor
* ObjectId is again just an int
* ObjectDefinitions no longer use Mod keys
* SubSceneSet ID is now a struct SubSceneSetId not an int
* Moved AssetCreator config to AutoRef
* Added better checks in AssetCreator

### Removed
* AllTypeIndex from TypeManagerEx to speed up domain reloads
* DynamicIndexedMap as it's replaced by DynamicVariableMap

### Documentation
* LifeCycle
* DynamicHashMap updated to reflect source generation and new types

### Fixed
* SearchView can now handle generic paths
* SearchView issue with having to double click
* SearchView error on hitting escape in nested menu
* Timing issue with SubSceneLoadingManagedSystem and subscribing to SceneManager.sceneLoaded
* ObjectManagementProcessor no longer triggers save unless it triggered dirtying the asset
* SubSceneEditorToolbar can again unload SubScenes
* CodeGenHelpers FullyQualifiedName not working with nested classes
* CodeGenHelpers not working in global namespace

## [1.4.2] - 2025-04-26

### Added
* AssetSet as a replacement for AssetLoadAuthoring
* StartupScene to SubScene for auto swapping to a default set
* Priority to Toolbar attribute to order elements
* DynamicIndexedMap
* ElementAt on FixedArray
* Update Settings button to EditorSettings to automatically resort your settings if you've made destination world changes
* PooledNativeList
* LibraryLoader for loading native libraries dynamically in cross platform manner at runtime

### Changed
* Stopped baking when overriding subscenes
* Groups no longer add to thin clients by default
* Cleaned up K validation a little and exposed it a bit easier
* K rewritten to support any value type
* Tweaked element property nesting a little
* Optimized DestroySystemGroup and InitializeSystemGroup for high entity counts
* BovineLabsBootStrap.RequireConnectionApproval is now a ConfigVar
* InspectorSearch no longer iterates forever on hidden inspectors
* Toolbar dropdowns now match Unity 6 style and look pretty

### Removed
* AssetLoadAuthoring, now merged into SubSceneLoadAuthoring
* Settings<T> and SettingsBuffer as they weren't used and too simplistic

### Fixed
* Compile errors when disabling SubScene feature
* NativeParallelMultiHashMapFallback could lose elements
* InspectorSearch not able to handle instance component removal

### Documentation
* SubScene documentation updated to reflect changes to AssetSet
* K documentation updated to match refactor and fleshed out
* PooledNativeList added and a new section for Utility

## [1.4.1] - 2025-03-29

### Added
* Reload button for domain and subscene in Extensions, can be toggled off in config vars
* QueryEntityEnumerator for manual chunk iteration with Enablable components

### Changed
* BL_DISABLE_OBJECT_AUTO_INSTANTIATE removed, replaced by a config var
* Internal logging no longer writes to a separate json file
* Breaking: SubScene loading rewritten from scratch with new live baking path, backup your setup before updating and check documentation
* Modernized thread stream safety
* Merged thread stream large writes into the container

### Fixed
* InputProcessSystemGroup missing conditional defines
* Searcher missing the search field

### Documentation
* Updated SubScene to match changes and be more comprehensive

## [1.4.0] - 2025-03-15

### Changed
* Updated to entities 1.4.0-exp.2

## [1.3.6] - 2025-03-15

### Added
* WorldUnmanagedExtensions.GetAllSystemDependencies
* IsEditorWorld extension
* Editor world is now always created instead of lazy loaded, you can disable this with BL_DISABLE_CREATE_EDITOR_WORLD
* WriteLarge to UnsafeThreadStream
* UnsafeThreadStream to SingletonCollect
* TypeUtility
* Some convenient NativeList and List extensions

### Changed
* Modernized AssemblyBuilder Tests generation using optionalUnityReferences with TestAssemblies
* Removed some event allocations

### Fixed
* Removed [NativeContainer] from UnsafeMultiHashMap

## [1.3.5] - 2025-02-19

### Added
* Exposed InspectorUtility
* Thread check to ThreadRandom
* WeakObjectReferenceInspector
* DynamicUntypedHashMap.Contains
* GameObject Inspector search, can be disabled with BL_DISABLE_INSPECTOR_SEARCH if you have your own solution already
* Added ObjectInstantiate workflow for object management
* Pin for managed objects

### Changed
* Reworked pause
* UnityObjectRefInspector can now update
* Continued improvements to ReflectionUtility performance and caching

### Fixed
* UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS errors
* DynamicUntypedHashMap.TryGetValue when sizeof(TValue) < sizeof(4)

### Remove
* AlwaysUpdateSystemGroup as it's no longer required

### Documentation
* GlobalRandom
* Input

## [1.3.4] - 2025-01-28

### Added
* Added HashSet support to IJobHashMapDefer
* GetChunkComponent to ComponentLookup
* Type safety checks to DynameUntypedhashMap
* WeakObjectReferenceExtensions and UnityObjectRefExtensions to get internal id
* BlobPerfectHashMap
* EntityBlob allowing a single Blob that automatically contains multiple BlobAssetReferences
* HashSet support to IJobHashMapDefer
* TypeManagerEx can now provide an index for all unmanaged types
* GetUniqueKeyArray(NativeList<TKey>) to NativeMultiHashMap and NativeParallelMultiHashMap
* More Caching to ReflectionUtility
* Global Random
* IUpdateWhilePaused support to unmanaged systems
* PauseUtility.UpdateWhilePaused for 3rd party libraries but use IUpdateWhilePaused for your own systems
* Support for Entities 1.3.9

### Changed
* Standardized code
* Significantly improved attribute reflection speed
* ObjectDefinitions no longer require LifeCycle (but still have extra support for it)

### Fixed
* Logging directory
* DynameUntypedhashMap.GetOrAddRef on first frame would return wrong value 
* StripSystem out of range exception
* UNITY_CLIENT build error
* Conditional wrapping for SubSceneLoadAuthoringDataEditor
* A potential error in ObjectDefinitionAuthoring as it was using EditorSettingsUtility not AuthoringSettingsUtility

### Removed
* Entropy - use GlobalRandom instead

## [1.3.3] - 2024-11-09
### Added
* WorldAllocator
* A button to prefabs to bake and load them into a world; this can be enabled from BovineLabs/Tools/Load Prefabs as Entities
* Ability to prebake subscenes
* More hashmap extensions
* Entity overloads to IEntityCommands
* String variations to BLDebug
* GetEntity(Object) extension to IBaker 
* Connection approval to bootstrap
* Input now supports generating for NetCode IInputComponentData
* IUpdateWhilePaused which allows a ComponentSystemGroup to continue to Update when the game is paused
* StripLocalAttribute to strip network only components in Local Simulation world
* Support for Unity 6.1

### Changed
* Requires Unity 6+
* Updated to Entities 1.3.5
* SubSceneLoadFlags numbers have been changed, this may require you to set them again
* SubScene Loading now works with normally loaded SubSceneLoadConfig in normally loaded Scenes instead of only at start. This ensure compatibility with things like Unity's Multiplayer Play Mode package scenarios
* BovineLabsBootstrap.Netcode being more fleshed out
* CameraMainAuthoring renamed to CameraAuthoring
* BLDebug now implements a CustomLog to avoid having to use 4096 fixed string every time
* InitializeSystemGroup moved to BeginSimulationGroup to support NetCode ghosts
* Renamed InitializationRequireSubScenesSystemGroup to RequireSubScenesSystemGroup
* Pause now uses the same RateManager on multiplayer worlds as well
* CameraFrustumCorners and CameraFrustumPlanes are now IComponentData

### Fixed
* SubSceneLoadAuthoring missing BL_DISABLE_SUBSCENE check
* A few defines

### Removed
* GameSettings and a default bounding volume distance. Now must be set per subscene
* AssetCreatorAttribute, data is now specified in AssetCreator 
* Ability to set lifecycle components, now always added to reduce chance of bugs
* CameraFrustrumAuthroing as it's now included in CameraAuthoring

## [1.3.2] - 2024-09-18

### Added
* SubSceneLoadAuthoring for weak referenced subscene loading and stripping
* Better SubSceneLoading netcode support
* Journaling to SetFilter methods

### Changed
* Updated to Entities 1.3.2
* Pause now uses a rate manager on non-multiplayer worlds to stop OnStartRunning and OnStopRunning triggering
* Cleaned up BovineLabs menu

### Fixed
* Sample missing runtime style
* InputCommonSettings refresh null exception without any debug

### Removed
* UI and Toolbar removed from core but I have a new standalone project called Anchor https://gitlab.com/tertle/com.bovinelabs.anchor

## [1.3.1] - 2024-08-28

### Added
* SearchWindow now works on 2022.3
* AddEnabledComponent and AddEnabledBuffer Baker extension
* ElementProperty now provides ability to override DisplayName to automatically support names on List elements instead of Element 0 etc
* Added MinMaxAttribute for Vector2 and Vector2Int
* LifeCycle can now conditionally exclude Initialize or Destroy
* InputAPI
* UIAPI
* AppAPI
* Added Construct methods to BlobCurveX so they can be added to your own BlobAssetReference

### Changed
* LookupAuthoring now allows for optional values
* SettingsBase.Bake(IBaker) changed to SettingsBase.Bake(Baker<SettingsAuthoring>)
* Cleaned up BitArray, made more consistent, improved documentation, readonlys

### Fixed
* DynamicUntypedHashMap was writing to wrong index on smaller data types
* liblz4 on linux
* NativeMultiHashMap becoming unique when exceeded 256 elements
* DynamicUntypedHashMap growing in memory when using large data

### Removed
* InitializeAttribute as newer version of Unity seem to have broken it

### Documentation
* Added an Extension sample with everything setup

## [1.3.0] - 2024-06-26

### Added
* IJobChunkWorkerBeginEnd
* ContainsKey to DynamicPerfectHashMap
* Added OnWorkerBegin, OnWorkerEnd, OnBucketEnd to IJobParallelHashMapDefer
* NativeParallelMultiHashMapFallback now supports hash overrides
* An error message if you try to use SystemAPI.Register multiple times on same system
* GetAllDependencies to ObjectGroup

### Changed
* Updated to 1.3.0-exp.1

### Fixed
* A safety check in Timers
* Settings that are deleted should regenerate without a domain reload.
* IUID issues with duplication

## [1.2.15] - 2024-06-08

### Added
* CacheImpl which adds an easy way to add a cache to a meta entity
* WriteGroupMatcher which matches existence of any component from a write group including enable state
* IJobParallelHashMapDefer now supports ReadOnly variants
* Added GetRef to DynamicHashMap
* Find Authoring Problems to Assembly Graph
* TestLeakDetectionAttribute from Ribitta
* OnlySize option to StableTypeHashAttribute
* IndexOf and TryGetValue predicate extensions to UnsafeList
* NativeHashMapFactory and NativeHashSetFactory to control min growth
* Ref check safety into DynamicHashMaps - they should be passed by ref if you are adding
* ConfigVars for setting FixedUpdateTick and TargetFrameRates
* VFXGraphTemplateWindow
* Added an Enumerator for Blob[Multi]HashMap
* NativeParallelMultiHashMapFallback that falls back to a NativeQueue if the capacity is exceeded instead of crashing
* Ability to HotKey DataModes on Inspector and Entities Hierarchy

### Changed
* UIDManagerAttribute renamed to AutoRefAttribute
* AutoRefAttribute and IUID have been moved to Core from ObjectManagement Extension
* IUID now starts at 1 so 0 can be used as a null or default check
* Renamed UnsafeComponentTypeHandle to UnsafeEntityDataAccess
* Serializer now uses UnsafeList<T>* instead of NativeList<T>
* Assembly Builder now adds Authoring reference to Editor assembly
* ElementProperty and ElementEditor now have some shared logic in ElementUtility
* Blob[Multi]HashMap now always returns values by ref or Ptr<T>

### Fixed
* SubScene unloading was no longer triggering Destroy
* Auto IDs were broken by a recent Unity patch
* Warnings due to nullable context and UI
* Wrapped TerrainToMesh to require the module
* AlwaysUpdatePhysicsWorld should work without my fork
* ObjectDefinition create authoring couldn't handle null definition fields
 
## [1.2.14] - 2024-05-13

### Added
* AddDependency<T> extension for SystemState
* SpatialMap3 for a spatial map in 3D
* ConfigVar option to disable SelectedEntityEditorSystem

### Changed
* Removed Batch from IJobParallelForDeferBatch methods
* Input with the word Debug in the name are now separate so they can be stripped
* Exposed iterator data for DynamicHashMap
* SpatialMap has been replaced by a HashMap variation. The old SpatialMap has been renamed to SpatialKeyedMap
* Rewrote SelectedEntityEditorSystem to hopefully handle large entity count slightly better

### Fixed
* Created Definitions from Assets wasn't adding the authoring component

## [1.2.13] - 2024-05-04

### Added
* NativeThreadStream.Writer<T> generic variation
* NativeMultiHashMap.Remove(key, value)
* BitFieldAttributeEditor which can be used to create a bit and flag inspector element from any data
* MaskFieldL for a long mask field
* Removing need for CustomBindings by adding custom BindableX elements instead
* [Native|Unsafe]PerfectHashMap
* NativeMultiHashMap.ReadOnly
* UnmanagedPool
* NativeHashMap extensions to access via index
* Serializer and Deserializer originally from my save package

### Changed
* Updated to Entities 1.2.1
* No longer supports 2023.3, only Unity 6+ (and 2022.3)
* UI moved to BovineLabs.Core.UI and restricted to Unity 6+
* SystemState.AddSystemDependency renamed to SystemState.AddDependency
* LifeCycle system groups no longer update unless there is something to work on

### Fixed
* Toolbar issues in IL2CPP build
* Netcode compile issues
* SelectedEntity wasn't being set

## [1.2.12] - 2024-04-05

### Added
* Ability to specify ThreadIndex for UnsafeThreadStream and UnsafeParallelHashMap

### Fixed
* Unity 2022.3 support

### Changed
* Renamed StatefulNewCollisionEventAuthoring to StatefulCollisionEventAuthoring

## [1.2.11] - 2024-04-02

### Added
* Hotkeys for timescale if TimeToolbar exists
* ElementEditor has had a few helpers added alignment, visibility etc
* Basic main camera pipeline with frustum and corner buffers
* Added InlineObjectAttribute for drawing the fields of an ObjectField
* Added SubSceneToolbarSystem
* The Destroy Pipeline can now handle unloading SubScenes (if done via SubSceneUtil.UnloadScene)
* ConvexHullBuilder
* MeshSimplifier
* TerrainToMesh
* IntersectionTests.AABBTriangle

### Changed
* SubSceneLoadConfig now supports when SubScenes are marked AutoLoad - setting will be ignored at runtime but will allow auto loading in editor
* Modernized SubSceneLoadConfigEditor
* Reordered Initialize and Destroy system groups a bit
* Initialize now separated into InitializeEntity and InitializeSubSceneEntity
* Updated to Entities 1.2.0
* SingletonCollection now doesn't require type to be IDisposable
* Safety on NativeWorkQueue.Update
* Toolbar now jumps to latest created tab

### Fixed
* Destroy pipeline changes in 1.2.10 that broke destroying LinkedEntityGroups
* Toolbar breaking 2022.3
* Fixed  BitArray256.None actually being one
* Stopped SubScenes being loaded into default world when SubSceneLoading is enabled

### Removed
* PhysicsDraw as it depended on internal libraries

## [1.2.10] - 2024-03-11

### Added
* BlobHashMap, BlobMultiHashMap
* PrefabElementEditor and PrefabElementAttribute which makes an inspector where changes are written to the prefab not instance
* Inspectors for various UnityObjectRef<T> including Material and Mesh
* Separate ViewPorts in InputCommon for camera vs screen
* Added ApplicationFocus to InputCommon
* Added gamma and normal distributions to mathex
* Static method for toggle pause on PauseGame to unify logs
* Burst replacement for UpdateWorldTimeSystem if using BovineLabsBoostrap
* PauseSystem can now pause presentation separately

### Changed
* Renamed UIHelper GetToolbar to GetPanel
* LifeCycleAuthoring DisableInstantiateOnInstance has been inverted to InstantiateOnInstance
* DynamicHashMaps can now be accessed on main thread without triggering safety, allowing you to pass them to jobs
* Improved performance of destroy pipeline and removed DestroyEntityCommandBufferSystem
* SubSceneLoadingSystem is now a bursted ISystem
* UnityTimeSystem is now bursted

### Fixed
* UIHelper.GetPanel again returns the actual panel
* FixedArray Length value was wrong
* ConfigVarManager loading via environmental variables
* Loading volume SubScenes
* ToolbarManager error when disabled

### Removed
* Removed UnityTime, instead just directly use UnityEngine.Time in burst

## [1.2.9] - 2024-03-03

### Added
* UNITY_DOTS_DEBUG to SpatialMap, K, DynamicHashMaps, EntityCommands and Check

### Changed
* Changed exception to warning in InputActionMapSystem

### Fixed
* Dependency issue on InputActionMapSystem

## [1.2.8] - 2024-03-02

### Added
* SetBinding to allow you to bind a binding
* You can now automatically generate hashmaps from data on Object Definitions
* Automatically add your settings files to the appropriate settings authoring in the SubScene. Assign a default in EditorSettings and optionally use SettingsWorldAttribute if you have multiple worlds
* Settings search now highlights

### Changed
* InputSystemGroup now updates in Initialization so input still works while paused
* Settings now use a TreeView. All Core settings are now grouped
* Settings now hides empty settings with an option to toggle them visible

### Fixed
* FrameTime on FPS toolbar wasn't updating
* Stateful events error when physics simulation disabled
* UI binding and IL2CPP builds

## [1.2.7] - 2024-02-29

### Added
* ButtonState can now be generated from [InputAction]

### Changed
* Updated to Entities 1.2.0-pre.12
* ObjectDefinitions now add ObjectDefinitionAuthoring to their prefab to make baking scene objects and adding components easier
* Functions can now return any type of data

### Fixed
* Missing missing ObjectId Equals(object) override was breaking the inspector
* Making changes to string config vars in in the window wasn't apply changes
* DynamicHashMap.GetOrDefault safety fail when readonly
* PauseSystem now stops elapsed time updates to avoid fixed step issues

### Removed
* Hybrid (copy transform to/from gameobject) has been removed
* Old virtual chunks implementation

## [1.2.6] - 2024-02-11

### Fixed
* Weird compile issue some users were experiencing related to internal
* Toolbar causing missing warnings in builds

## [1.2.5] - 2024-02-11

### Added
* 2022.3 Core support back, with limitations
* ListViewCountTrackerBinding for binding ListViews
* AlwaysUpdatePhysicsWorld to rebuild physics world when fps is above fixed update. Enabled with BL_PHYSICS_ALWAYS_UPDATE for now
* Support for multiple data bindings on UI via manually invoking Load/Unload
* Ticks to UnityTime
* GetEnabledMaskRO extension to ArchetypeChunk
* BlobCurve2, BlobCurve3 and BlobCurve4 as well as BlobCurveNT
* GetEnableRefRWNoChangeFilter from ComponentLookup

### Changed
* Toolbar systems will now disable itself instead of an error if state not set
* Changed IUIAssetManagement GetPanel to object
* Default folder for settings moved from Assets/Configs to Assets/Settings to match where Unity is now using
    * You will need to update (or clear) your Editor Folder settings file if you want this to be used

### Fixed
* Toolbar will no longer error if used in a scene without the manager
* UI helper not calling Load/Unload
* DynamicHashMap.RemoveRangeShiftDown error if trying to remove all elements
* Compile error when lifecycle was disabled
* AssemblyBuilder adding unticked references

### Removed
* EnabledBinding has been removed as it's not longer required

## [1.2.4] - 2024-01-12

### Fixed
* Compiling when toolbar extension was enabled

## [1.2.3] - 2024-01-10

### Added
* minmax to mathex
* DebugLong to BLDebug
* GetInternalDependency extension for SystemState

### Fixed
* Compile error on BovineLabsBoostrap when NetCode was installed

## [1.2.2] - 2024-01-05

### Added
* Toolbar now has the option of showing and hiding groups
* InputActionMapSystem
* AddComponentObject to IEntityCommand
* Added HybridComponent to Hybrid feature
* Internal access to CompanionComponentSupportedTypes
* Added AfterSceneSystemGroup

### Changed
* Updated dependency to Unity 2023.3.0b1
* Toolbar rewritten to support new UITK binding and use bursted ISystem
* PauseSystem will now only pause presentation for the first initial pause
* Renamed the feature Copy Transform to Hybrid 
* Updated NoAllocHelpers to no longer use cached reflection
* ObjectDefinition now sets itself up on scene objects
* ChangeFilterTracking threshold can now be configured

### Removed
* Removed Popup and PopupMask in favour of updated MaskField and Dropdown
* Draw toolbar
* RemoveLinkedEntityGroupSystem - use my entities fork if you want this behaviour

### Documentation
* Added documentation for ChangeFilterTracking

## [1.2.1] - 2023-12-07

### Fixed
* Compile error in ReflectionUtility

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