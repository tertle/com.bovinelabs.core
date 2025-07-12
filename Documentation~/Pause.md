# Pause Extension

The Pause extension provides a comprehensive world-level pause system for Unity DOTS applications, allowing fine-grained control over which systems continue to update during pause states.

## Core Components

### PauseGame Component

`PauseGame` is the main component that controls pause behavior for the entire world.

```csharp
public struct PauseGame : IComponentData
{
    // Controls pause mode behavior
    public bool PauseAll;
}
```

**Key Methods:**
- `PauseGame.IsPaused(ref SystemState)` - Check if world is currently paused
- `PauseGame.Pause(ref SystemState, bool pauseAll = false)` - Pause the world
- `PauseGame.Unpause(ref SystemState)` - Resume the world

**Usage Example:**
```csharp
// Pause the world during loading
PauseGame.Pause(ref state, pauseAll: true);

// Resume normal operation
PauseGame.Unpause(ref state);

// Check pause status
if (PauseGame.IsPaused(ref state))
{
    // Handle paused state
}
```

## Pause Modes

### Normal Pause (`PauseAll = false`)
- Only systems marked with `IDisableWhilePaused` or in `PauseUtility.DisableWhilePaused` stop updating
- Other systems continue normal operation
- Ideal for in-game pause menus where UI and input should remain active

### Full Pause (`PauseAll = true`)
- All systems stop updating except those marked with `IUpdateWhilePaused` or in `PauseUtility.UpdateWhilePaused`
- Used during critical operations like initial world setup or scene loading
- Most restrictive pause mode

## System Control Interfaces

### IUpdateWhilePaused
Marker interface allowing systems to continue updating during any pause mode.

```csharp
public partial struct MySystem : ISystem, IUpdateWhilePaused
{
    public void OnUpdate(ref SystemState state)
    {
        // This system always updates, even when paused
    }
}
```

**Built-in systems that implement this interface:**
- `InputSystemGroup` - Input processing continues
- `DebugSystemGroup` - Debug functionality remains active
- `SingletonInitializeSystemGroup` - Initialization systems
- `InitializationSystemGroup` - Core initialization

### IDisableWhilePaused
Marker interface for root systems that should stop during normal pause mode.

```csharp
public partial struct MySimulationGroup : ISystem, IDisableWhilePaused
{
    // This system group stops during normal pause
}
```

## Third-Party System Support

For systems that cannot implement the marker interfaces, use the static HashSets in `PauseUtility`:

```csharp
// Register a system to update during pause
PauseUtility.UpdateWhilePaused.Add(typeof(ThirdPartySystem));

// Register a system to be disabled during pause
PauseUtility.DisableWhilePaused.Add(typeof(ThirdPartyRootSystem));
```

**Pre-registered disabled systems:**
- `FixedStepSimulationSystemGroup`
- `LateSimulationSystemGroup`
- `VariableRateSimulationSystemGroup`

## Time Management

The pause system prevents fixed timestep catchup issues by freezing `World.Time.ElapsedTime` during pause, ensuring smooth resumption without frame rate hitches.

## Integration Examples

### Scene Loading Integration
```csharp
// Automatic pause during subscene loading
public partial struct SubSceneLoadingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (loadingInProgress)
        {
            PauseGame.Pause(ref state, pauseAll: true);
        }
        else if (loadingComplete)
        {
            PauseGame.Unpause(ref state);
        }
    }
}
```

### Menu System Integration
```csharp
public partial struct MenuSystem : ISystem, IUpdateWhilePaused
{
    public void OnUpdate(ref SystemState state)
    {
        if (menuOpened)
        {
            PauseGame.Pause(ref state); // Normal pause - UI continues
        }
        else if (menuClosed)
        {
            PauseGame.Unpause(ref state);
        }
    }
}
```

## Architecture Details

The pause system uses a custom `IRateManager` implementation (`PauseRateManager`) that:
- Intercepts system group update decisions
- Freezes world time during pause to prevent catchup
- Selectively updates systems based on pause mode and interfaces
- Maintains compatibility with existing rate managers