# K keys

K is a settings-backed, Burst-readable alternative to a closed enum or layer mask. It maps human-readable `FixedString32Bytes` names to unmanaged values without forcing every contributing package to edit one shared enum.

Use K when names and values are authored as project data but runtime jobs need fast static lookup. Use a normal C# enum when the value set is closed and compile-time ownership is desirable.

## Core types

| Type | Purpose |
|---|---|
| `KSettings<TSelf, TValue>` | Standard serialized `NameValue<TValue>[]` settings asset |
| `KSettingsBase<TSelf, TValue>` | Custom settings schema that supplies its own `Keys` sequence |
| `KAttribute` | Inspector dropdown or flags field for supported integer values |

K settings are `SettingsSingleton` assets stored under the `K` settings subdirectory. Create exactly one asset for each K type and keep it included in builds.

## Define a key set

```csharp
namespace MyGame
{
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    [SettingsGroup("Game")]
    public sealed class ClientStates : KSettings<ClientStates, int>
    {
    }
}
```

Open **BovineLabs > Settings**, select `Client States`, and create entries in the `Keys` array.

Names are stored as `FixedString32Bytes`, whose UTF-8 payload limit is 29 bytes. Names must be unique. Duplicate values are allowed; reverse lookup returns the first name registered for that value.

## Provide reset defaults

Override `SetReset()` when a newly created or reset asset should begin with known entries:

```csharp
namespace MyGame
{
    using System.Collections.Generic;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    [SettingsGroup("Game")]
    public sealed class ClientStates : KSettings<ClientStates, int>
    {
        protected override IEnumerable<NameValue<int>> SetReset()
        {
            yield return new NameValue<int>("menu", 0);
            yield return new NameValue<int>("loading", 1);
            yield return new NameValue<int>("gameplay", 2);
            yield return new NameValue<int>("paused", 3);
        }
    }
}
```

`SetReset()` supplies editor reset data. The serialized asset remains the source used at runtime.

## Use a custom authoring schema

Derive from `KSettingsBase<TSelf, TValue>` when each entry needs additional editor-facing structure before it becomes a name/value pair:

```csharp
namespace MyGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [SettingsGroup("Game")]
    public sealed class GameLayers : KSettingsBase<GameLayers, uint>
    {
        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        public override IEnumerable<NameValue<uint>> Keys =>
            this.entries.Select(e => new NameValue<uint>(e.Name, e.Value));

        [Serializable]
        private sealed class Entry
        {
            public string Name = string.Empty;
            public uint Value;
        }
    }
}
```

Keep the resulting names unique and values valid for the fields that consume them.

## Runtime lookup

```csharp
var gameplay = ClientStates.NameToKey("gameplay");

if (ClientStates.TryNameToKey("paused", out var paused))
{
    // Use paused.
}

var name = ClientStates.KeyToName(gameplay);
```

The static maps are backed by `SharedStatic` native collections and are available to Burst jobs after the settings singleton initializes:

```csharp
[BurstCompile]
private struct ResolveStateJob : IJob
{
    public NativeReference<int> Result;

    public void Execute()
    {
        this.Result.Value = ClientStates.NameToKey("gameplay");
    }
}
```

`NameToKey` returns the default value when a name is absent and logs in checks/debug configurations. Use `TryNameToKey` when absence is valid.

`KeyToName` also returns the default fixed string when no reverse mapping exists.

## Inspector fields

`KAttribute` supports `int`, `uint`, `short`, `ushort`, `byte`, and `sbyte` fields:

```csharp
[K(nameof(ClientStates))]
public int CurrentState;

[K(nameof(GameLayers), flags: true)]
public uint CollisionMask;
```

For flags, author values as individual bit values and store a compatible integer field. The drawer combines selected values; K does not validate that entries are powers of two.

## Enumerate registered pairs

`Enumerator()` exposes the initialized ordered native list:

```csharp
var enumerator = ClientStates.Enumerator();
while (enumerator.MoveNext())
{
    var pair = enumerator.Current;
    BLGlobalLogger.LogInfoString($"{pair.Name}: {pair.Value}");
}
```

Treat the enumerator as a view of singleton-owned storage. Do not dispose or retain it across reinitialization.

## Initialization and builds

K maps are populated when their settings singleton asset initializes. Merely calling `ClientStates.I` can create a transient ScriptableObject fallback, but it does not supply authored keys when the real asset was excluded.

Ensure the asset exists, remains unique, and has `IncludeInBuild == true` through its `SettingsSingleton` base behavior. See [Settings](Settings.md#global-settingssingleton-assets) for build inclusion and duplicate-asset rules.

## Troubleshooting

**`K not setup` is thrown**

The settings asset did not initialize. Create it through **BovineLabs > Settings**, keep it included in the build, and remove duplicate assets.

**A name resolves to the default value**

Use `TryNameToKey` to distinguish a missing name from a deliberately authored default value. Check UTF-8 length and exact case.

**The inspector dropdown is empty**

Confirm the `KAttribute` type-name string matches the settings type and that the editor assembly references `BovineLabs.Core.Editor`.

**Flags combine incorrectly**

Use unique single-bit values for flag entries and a supported integer field type.

## Related guides

- [Settings](Settings.md)
- [ConfigVars](ConfigVars.md)
- [Inspectors](Inspectors.md)
