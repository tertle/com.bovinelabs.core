# Change Filter Tracking

## Overview

Change filters can be easily broken by ref fields or frequent modifications, causing them to trigger every frame and defeating their performance benefits. The `ChangeFilterTrackingAttribute` monitors component change frequency and warns when components exceed optimal thresholds.

**Editor-only system with zero runtime overhead.**

## Usage

Add the attribute to components you want to monitor:

```csharp
[ChangeFilterTracking]
public struct MyComponent : IComponentData
{
    public int Value;    
}
```

Works with `IComponentData`, `IBufferElementData`, and `IEnableableComponent` types.

## Monitoring

### Real-time Window
Access via `BovineLabs -> Tools -> Change Filter` to view:
- **60 Frames %**: Short-term change frequency
- **600 Frames %**: Long-term average (used for warnings)
- Multi-world support with world selector

![ChangeFilterWindow](Images/ChangeFilterWindow.png)

### Configuration
Configure via `BovineLabs -> ConfigVars`:

| ConfigVar                      | Default | Description                                  |
|--------------------------------|---------|----------------------------------------------|
| `debug.changefilter.enabled`   | true    | Enable/disable tracking                      |
| `debug.changefilter.threshold` | 0.85    | Warning threshold (85%) for console warnings |

![ChangeFilterWindow](Images/ChangeFilterTrackingOption.png)

## Common Issues

**High change frequency (>85%) usually indicates:**
- Ref fields breaking change detection
- Unnecessary component writes every frame
- Structural changes affecting the component