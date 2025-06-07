# Change Filter Tracking

## Summary

When using change filters it is very easy to break their usage by accidentally introducing a ref field on the component causing them to trigger every frame.

The `ChangeFilterTrackingAttribute` allows you to track how frequently a component triggers a change filter and warns you if it is happening too frequently.

## Using
Simply add the attribute to any component you want to track.

```csharp
[ChangeFilterTracking]
public struct MyComponent : IComponentData
{
    public int Value;    
}
```

## Configuration

Configuration is handled in the config var window. `BovineLabs -> ConfigVars` toggling the `debug.changefiltertracking` option.

| Key       | Description                                                                                                                                                                                                    |
|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Enabled   | Tracking a lot of components can add a little overhead to the editor (but not the runtime), this lets you toggle it on and off.                                                                                |
| Threshold | Allows you to change the warn level. Use a value of 0 to 1, default 85% (0.85). This means if a component triggers a change filter more than 85% of the time over 600 frames it will log a warning in console. |

![ChangeFilterWindow](Images/ChangeFilterTrackingOption.png)

## Window
There is also a window under `BovineLabs -> Tools -> Change Filter` that will show you all the components that are triggering change filters and how frequently they are doing so.

![ChangeFilterWindow](Images/ChangeFilterWindow.png)
