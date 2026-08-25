# Asset references and IDs

Core's Asset helpers keep ScriptableObject catalogs synchronized and provide stable IDs without requiring a dependency on Nerve.

Runtime namespace:

```csharp
using BovineLabs.Core.Asset;
```

Editor namespace:

```csharp
using BovineLabs.Core.Editor.Asset;
```

## Choose a tool

| Need | Use |
|---|---|
| Keep every asset of one type in a settings/catalog field | `[AutoRef]` |
| Include imported non-`.asset` files in AutoRef processing | `[AutoRefImport]` |
| Run cleanup after an AutoRef field changes | `IAutoRefPostProcessor` |
| Assign an integer unique among assets of the same type | `IUID` |
| Assign an integer unique across all participating asset types | `IUIDGlobal` |
| Draw a generated ID without allowing manual edits | `[UID]` |
| Pack a local ID and mod ID for runtime data | `BLId` |

## AutoRef catalogs

Apply `AutoRefAttribute` to a ScriptableObject type. The processor finds matching assets after imports and writes them to the configured manager field.

```csharp
namespace MyGame.Content
{
    using BovineLabs.Core.Asset;
    using UnityEngine;

    [AutoRef(nameof(ContentSettings), "items", nameof(ItemDefinition), "Content/Items")]
    public sealed class ItemDefinition : ScriptableObject
    {
    }
}
```

The four-argument form supplies:

1. Manager type name.
2. Serialized array/list field name.
3. Directory key used by Core's configurable path settings.
4. Default subdirectory under `Assets/Settings`.

Use the five-argument constructor when the directory key, full default directory, and default manager filename must be controlled separately.

An asset type can have multiple `[AutoRef]` attributes when it belongs to more than one catalog. Processing also discovers derived asset types.

### Preserve data beside the reference

If the manager stores entry structs rather than a plain object array, set `ReferenceFieldName` to the object-reference field. AutoRef then matches and updates by that field while preserving the entry's other serialized data.

```csharp
[AutoRef(nameof(ContentSettings), "items", ReferenceFieldName = "definition")]
public sealed class ItemDefinition : ScriptableObject
{
}
```

The field name must match the serialized entry schema exactly.

### Post-processing

When the manager implements `IAutoRefPostProcessor`, Core calls `OnAutoRefUpdated` after changing the field. Use it to remove invalid auxiliary entries or rebuild derived editor data; keep the callback deterministic and avoid creating another import loop.

## Imported file extensions

`AutoRefProcessor` normally reacts to `.asset` files. Apply `AutoRefImportAttribute` to an editor-visible marker type to include another extension:

```csharp
[AutoRefImport("dialogue")]
public sealed class DialogueImporterMarker
{
}
```

Pass the extension without a leading dot.

## Generated asset IDs

Implement `IUID` on a ScriptableObject when each asset needs a stable positive ID unique within its concrete asset type:

```csharp
public sealed class ItemDefinition : ScriptableObject, IUID
{
    [SerializeField]
    [UID(typeof(ItemDefinition))]
    private int id;

    public int ID
    {
        get => this.id;
        set => this.id = value;
    }
}
```

Generated IDs start at 1; zero remains the unassigned/null value. Use `IUIDGlobal` only when IDs must not collide across different participating asset types.

Do not hand-edit generated IDs. Let the importer resolve duplicates so branch merges and copied assets remain consistent.

## `BLId`

`BovineLabs.Core.BLId` packs a 24-bit local ID and an 8-bit mod ID into one `int` payload:

```csharp
var baseId = new BLId(item.ID);
var moddedId = baseId.WithMod(modId);

if (!moddedId.IsNull)
{
    var local = moddedId.ID;
    var mod = moddedId.Mod;
}
```

Rules:

- Local ID `0` is null, regardless of mod input.
- `BLId.MaxLocalId` is 16,777,215.
- The payload supports mod IDs `0` through `255`; namespace `0` is base/global content, leaving 255 non-zero runtime namespaces.
- `RawValue` exposes the packed value for serialization, including ghost serialization.
- Equality and ordering compare the packed identity; ordering sorts by mod then local ID.

Type-safe project or package IDs can wrap `BLId` while sharing its serialized payload.

## Editor creation helpers

`AssetCreator<T>` builds an inspector element for creating assets that belong to an AutoRef-managed settings collection. `AssetUtility` contains the lower-level creation helpers used by `AssetCreator` and package-specific menu items.

Keep editor creation code in an assembly referencing `BovineLabs.Core.Editor`. Runtime asset schemas need only `BovineLabs.Core`.

## Troubleshooting

**An asset is missing from the catalog**

Verify the manager type and serialized field names, confirm the asset matches the attributed type, and reimport it. For imported custom files, add the matching `[AutoRefImport]` extension.

**Per-entry metadata was reset**

Use `ReferenceFieldName` when the target collection contains entry structs. Without it, AutoRef treats the whole element as the reference value.

**Two assets have the same ID**

Reimport the affected assets and let the UID processor resolve the collision. Do not repair only one branch by manually changing serialized IDs.

**A `BLId` constructor throws in the editor**

The local or mod value exceeds the packed bit range. Allocate a valid source ID rather than truncating it.

## Related guides

- [Settings](Settings.md)
- [Inspectors](Inspectors.md)
- [Generated dynamic hash maps](DynamicHashMap.md)
