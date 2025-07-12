# Debug

BovineLabs Core provides comprehensive debugging and assertion utilities designed for Unity DOTS applications, with special consideration for Burst compilation compatibility and ECS-specific debugging needs.

## Runtime Assertions

### `Check`
Primary assertion utility for runtime validation in production code.
- `Check.Assume(bool assumption)` - Validates assumptions with compiler optimization hints
- `Check.Assume(bool assumption, string message)` - Validates assumptions with custom error message
- Burst-compatible with aggressive inlining
- Only active when `ENABLE_UNITY_COLLECTIONS_CHECKS` or `UNITY_DOTS_DEBUG` is defined
- Uses Unity's native `Debug.Assert` internally

## Logging Utilities

### `BLLogger`
World-specific logging component with configurable log levels.

**Log Levels**: `Disabled`, `Fatal`, `Error`, `Warning`, `Info`, `Debug`, `Verbose`

**Logging Methods**:
- **Debug**: `LogDebug()`, `LogDebug512()`, `LogDebug4096()`, `LogDebugString()` - Development logging
- **Info**: `LogInfo()`, `LogInfo512()`, `LogInfo4096()`, `LogInfoString()` - General information
- **Warning**: `LogWarning()`, `LogWarning512()`, `LogWarning4096()`, `LogWarningString()` - Warnings
- **Error**: `LogError()`, `LogError512()`, `LogError4096()`, `LogErrorString()` - Errors
- **Verbose**: `LogVerbose()`, `LogVerboseString()` - Editor-only verbose logging

**Features**:
- World-specific logging with world name prefixes
- Support for different string lengths (128, 512, 4096 bytes)
- Configurable via `debug.loglevel` config variable
- Conditional compilation based on build configuration

### `BLGlobalLogger`
Static logging utility for global use without world context.
- Same logging methods as `BLLogger` but static
- Additional `LogFatal(Exception ex)` for exception logging
- `LogString(string msg, LogLevel level)` for dynamic level selection
- Shares log level configuration with `BLLogger`

## Debug Utilities

### `DebugUtil`
Utility methods for debugging from Burst-compiled code.
- `SplitInt(float value, int digits, out int integer, out int decimals)` - Splits float for debugging
- `SplitInt(double value, int digits, out int integer, out int decimals)` - Splits double for debugging
- Enables debugging numeric values where string formatting is not available

### Entity Selection Debug Components
Components for debugging entity selection in development builds.
- `SelectedEntity` - Component to track a single selected entity
- `SelectedEntities` - Buffer for tracking multiple selected entities
- `SelectedEntitySystem` - System that creates selection tracking entities

## Testing Utilities

### `AssertMath`
Mathematics-specific assertion utilities for unit tests.
- `AreApproximatelyEqual(quaternion expected, quaternion result, float delta)` - Quaternion comparison
- `AreApproximatelyEqual(float3 expected, float3 result, float delta)` - Float3 comparison

### `TestLeakDetectionAttribute`
NUnit test attribute for memory leak detection.
- Automatically enables leak detection before tests
- Validates no leaks occurred after test completion
- Integrates with Unity's native leak detection system

### `ECSTestsFixture`
Base class for ECS unit tests with world setup and cleanup.
- Sets up isolated test world
- Enables Jobs Debugger for tests
- Creates `BLLogger` entity for logging
- Provides `World`, `EntityManager`, and debug utilities
- Automatically cleans up after tests

## Configuration

### Config Variables
- `debug.loglevel` - Controls global log level (default: Warning)
- `debug.loglevel.min-world-length` - Controls world name formatting for alignment

### Build Configuration
- Assertions and debug utilities respect build configuration flags
- Production builds automatically disable debug features unless explicitly enabled
- `UNITY_DOTS_DEBUG` and `ENABLE_UNITY_COLLECTIONS_CHECKS` control assertion behavior

## Usage Patterns

### Runtime Validation
```csharp
// Use Check.Assume for runtime assertions
Check.Assume(entity != Entity.Null, "Entity must not be null");
Check.Assume(index >= 0 && index < length);
```

### World-Specific Logging
```csharp
// Use BLLogger component for context-aware logging
var logger = SystemAPI.GetSingleton<BLLogger>();
logger.LogInfo("Processing entities");
logger.LogWarning("Potential performance issue detected");
```

### Global Logging
```csharp
// Use BLGlobalLogger for static logging
BLGlobalLogger.LogError("Critical system failure");
BLGlobalLogger.LogInfo("Application started");
```

### Burst Debugging
```csharp
// Use DebugUtil for debugging in Burst code
DebugUtil.SplitInt(someFloat, 2, out int whole, out int fraction);
```

### Unit Testing
```csharp
[TestLeakDetection]
public class MyECSTest : ECSTestsFixture
{
    [Test] 
    public void TestEntityCreation()
    {
        var entity = EntityManager.CreateEntity();
        AssertMath.AreApproximatelyEqual(expected, actual, 0.01f);
    }
}
```