# K

## Summary

K is a type-safe, Burst-compatible alternative to Enums and LayerMasks that allows you to define key-value pairs in settings files.
It provides a streamlined way to convert human-readable strings into values (and vice versa) that works efficiently, even within Burst-compiled jobs.

Key features include:

- Define extensible key-value mappings across multiple libraries and projects
- Burst compatibility
- Support any unmanaged value types
- Inspector integration with custom property drawers

In particular, K is useful when you need enum-like functionality but want to make the values extendable across multiple libraries and projects without requiring
hard dependencies.

## System Architecture

### Core Classes and Interfaces

| Class/Interface        | Purpose                                                                                                                                        |
|------------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| `KSettingsBase`        | Abstract base class that integrates with the Settings system. You should not implement this.                                                   |
| `KSettingsBase<TV>`    | Abstract generic base with value type parameter, used for tooling and you should not implement this.                                           |
| `KSettingsBase<T, TV>` | Core implementation with generic parameters for the settings type and value type. Implement this if you want custom authoring data for Values. |
| `KSettings<T, TV>`     | Core implementation with generic parameters for the settings type and value type. Implement this to automate implementation of your Values.    |

### Attributes

| Attribute    | Purpose                                                                                       |
|--------------|-----------------------------------------------------------------------------------------------|
| `KAttribute` | Property attribute for inspector display of K values, supporting both single values and flags |

## Setup

### 1. Define Your Settings Class

Create a class that inherits from `KSettings<T, TV>` where T is your class type and TV is the value type you want to use (int, uint, byte, etc.):

```csharp
// Will automatically provide pairs of string, int in the inspector
public class ClientStates : KSettings<ClientStates, int>
{
}

// Alternatively implement the base to add custom authoring data
public class ClientStates : KSettingsBase<ClientStates, int>
{
    [SerializeField]
    private KeyValues[] keys = Array.Empty<KeyValues>();
    
    // Implemented method to convert our custom authoring
    public override IEnumerable<NameValue<int>> Keys => this.keys.Select(k => new NameValue<int>(k.Name, k.Value));

    [Serializable]
    public class KeyValues
    {
        public string Name = string.Empty;

        [Min(0)]
        public int Value = -1;
    }
}
```

### 2. Create the Settings Asset

In Unity, open the Settings window via `BovineLabs → Settings`. Your K settings class will appear in the list. Select it to automatically create the associated
asset.

Go to the [Settings](Settings.md) documentation for more information about setting settings up.

![K Settings](Images/K.png)

### 3. Configure Key-Value Pairs

In the inspector for your settings asset:

1. Add entries to the Keys array
2. Assign a human-readable name for each entry
3. Set the corresponding value (integer, byte, etc.)

The maximum key length is a UTF8 string of length 29 (due to FixedString32Bytes).

### 4. Optional: Default Values

You can provide default values by overriding the `SetReset` method:

```csharp
public class ClientStates : KSettings<ClientStates, int>
{
    protected override IEnumerable<NameValue<int>> SetReset()
    {
        yield return new NameValue<int>("menu", 0);
        yield return new NameValue<int>("loading", 1);
        yield return new NameValue<int>("gameplay", 2);
        yield return new NameValue<int>("paused", 3);
    }
}
```

## Runtime Usage

### Converting Names to Keys

To get a value from a string name at runtime, from anywhere including Burst jobs:

```csharp
// Get a value by name
var state = ClientStates.NameToKey("menu");

// Try to get a value with error handling
if (ClientStates.TryNameToKey("menu", out var stateValue))
{
    // Use stateValue
}
```

### Converting Keys to Names

For debugging or display purposes, you can convert values back to their string representations:

```csharp
// Get the name for a value
FixedString32Bytes name = ClientStates.KeyToName(2); // Returns "gameplay" in our example
```

## Inspector Integration

You can use the `KAttribute` to display your K values in the inspector:

```csharp
// Display as a dropdown with available values
[K("ClientStates")]
public int currentState;

// Display as a flags field (for bitwise operations)
[K("GameLayers", flags: true)]
public int layerMask;
```

This will show a dropdown or flags field in the inspector with the names from your K settings instead of raw numbers.

Note, this only supports int, uint, short, ushort, byte and sbyte types.

## Burst Compatibility

K is fully compatible with Burst-compiled code. The conversion between names and values happens through Burst-friendly SharedStatic containers:

```csharp
[BurstCompile]
private struct MyJob : IJob
{
    public void Execute()
    {
        // Works in Burst jobs
        var value = ClientStates.NameToKey("gameplay");
    }
}
```

### Iterating All Key-Value Pairs

K also implements an ordered list you can enumerate if you need these values at runtime in burst:

```csharp
// Get an enumerator for all key-value pairs
var enumerator = ClientStates.Enumerator();
while (enumerator.MoveNext())
{
    var pair = enumerator.Current;
    Debug.Log($"Name: {pair.Name}, Value: {pair.Value}");
}
```
