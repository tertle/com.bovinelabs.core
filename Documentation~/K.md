# K

## Summary

K is a type-safe, Burst-compatible alternative to Enums and LayerMasks that allows you to define key-value pairs in settings files. It provides a streamlined way to convert human-readable strings into values (and vice versa) that works efficiently, even within Burst-compiled jobs.

**Key Features:**
- Define extensible key-value mappings across multiple libraries and projects
- Burst compatibility
- Support any unmanaged value types
- Inspector integration with custom property drawers

Particularly useful when you need enum-like functionality but want to make the values extendable across multiple libraries and projects without requiring hard dependencies.

## Core Components

**Classes:**
- `KSettings<T, TV>`: Core implementation for automated Values implementation
- `KSettingsBase<T, TV>`: Core implementation for custom authoring data
- `KAttribute`: Property attribute for inspector display of K values

## Setup

### 1. Define Your Settings Class

```csharp
// Automatically provides pairs of string, int in the inspector
public class ClientStates : KSettings<ClientStates, int>
{
}

// Alternatively implement the base to add custom authoring data
public class ClientStates : KSettingsBase<ClientStates, int>
{
    [SerializeField]
    private KeyValues[] keys = Array.Empty<KeyValues>();
    
    public override IEnumerable<NameValue<int>> Keys => keys.Select(k => new NameValue<int>(k.Name, k.Value));

    [Serializable]
    public class KeyValues
    {
        public string Name = string.Empty;
        [Min(0)] public int Value = -1;
    }
}
```

### 2. Create the Settings Asset

Open the Settings window via `BovineLabs → Settings`. Your K settings class will appear in the list. Select it to automatically create the associated asset.

### 3. Configure Key-Value Pairs

In the inspector for your settings asset:
1. Add entries to the Keys array
2. Assign a human-readable name for each entry
3. Set the corresponding value (integer, byte, etc.)

Maximum key length is a UTF8 string of length 29 (due to FixedString32Bytes).

### 4. Optional: Default Values

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

## Usage

### Converting Names to Keys

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

```csharp
// Get the name for a value
FixedString32Bytes name = ClientStates.KeyToName(2); // Returns "gameplay"
```

### Inspector Integration

```csharp
// Display as a dropdown with available values
[K("ClientStates")]
public int currentState;

// Display as a flags field (for bitwise operations)
[K("GameLayers", flags: true)]
public int layerMask;
```

Supports int, uint, short, ushort, byte and sbyte types.

### Burst Compatibility

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

```csharp
// Get an enumerator for all key-value pairs
var enumerator = ClientStates.Enumerator();
while (enumerator.MoveNext())
{
    var pair = enumerator.Current;
    Debug.Log($"Name: {pair.Name}, Value: {pair.Value}");
}
```
