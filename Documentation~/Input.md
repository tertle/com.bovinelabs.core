# Input

## Summary

Input provides source generation for Unity's InputSystem integration with ECS. Define input components with attributes and the system automatically generates the required systems and bindings.

**Key Features:**
- `[InputAction]` attribute for automatic input binding
- `InputCommon` struct with common input utilities (cursor position, screen bounds, etc.)
- `InputAPI` for enabling/disabling action maps
- Source generation creates systems that update component fields every frame

## Defining Input Components

Create input components using the `[InputAction]` attribute:

```cs
public partial struct InputTest : IComponentData
{
    [InputAction]
    public float SingleAxis;

    [InputAction]
    public float2 Axis; // Mouse

    [InputActionDelta] // Modified by delta time
    public float2 AxisDelta; 

    [InputAction]
    public bool Button; // Pressed

    [InputAction]
    public ButtonState ButtonEvents; // Down, Pressed, Up events
}
```

## Setup

1. Open Input Common settings via `BovineLabs -> Settings -> Core -> Input Common`
2. Click `Find all Input` to discover new input components
3. Assign InputActionReferences to each field
4. Configure your InputActionAsset in the Asset field
5. Set default enabled ActionMaps in the Default Enabled dropdown

## Assembly Dependencies

Your assembly must reference:
- `BovineLabs.Core.Input` (runtime)
- `Unity.InputSystem`

For authoring components:
- `BovineLabs.Core.Input.Authoring`

## InputCommon

`InputCommon` provides common input utilities updated every frame:

```cs
var inputCommon = SystemAPI.GetSingleton<InputCommon>();

// Common properties
inputCommon.CursorScreenPoint;    // Screen space cursor position
inputCommon.CursorViewPoint;      // Viewport space cursor position
inputCommon.CursorInViewPort;     // Is cursor in viewport
inputCommon.InputOverUI;          // Is cursor over UI
inputCommon.CameraRay;            // Ray from camera through cursor
inputCommon.AnyButtonPress;       // Any button pressed this frame
```

## InputAPI

Enable/disable ActionMaps at runtime:

```cs
// Enable an action map
InputAPI.InputEnable("PlayerInput");

// Disable an action map
InputAPI.InputDisable("PlayerInput");
```

## Common Issues

**Source generator not running:**
- Missing `partial` keyword on component struct
- Component doesn't implement `IComponentData`
- Missing assembly references

**Input not responding:**
- InputActionAsset not assigned in settings
- ActionMaps not enabled in Default Enabled dropdown
- InputActionReferences not properly assigned

**Setup issues:**
- Haven't clicked "Find all Input" after creating components
- InputCommonSettings not configured