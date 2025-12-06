# Object Management

## Object Definition

### Summary
Object Definition provides automatic mapping of unique IDs to specific objects, offering a stable, deterministic way to reference prefabs and other objects at runtime.

**Key Features:**
- Deterministic ID assignment across different machines/builds
- Automatic hole filling when definitions are deleted
- Implicit conversion to ObjectId for easy runtime usage
- Integration with Object Groups for flexible categorization

**Fields:**
- **Friendly Name**: Optional designer friendly name for tooling
- **Description**: Optional long description about the object
- **Categories**: Define high level categories for automatic grouping
- **Prefab**: The prefab that will be baked into an Entity prefab

### Creation
Create from `Create -> BovineLabs -> Object Definition` or duplicate existing definitions.
Always create a Null Definition as the first ID with 0.

For multiple assets: `BovineLabs -> Utility -> Create Definitions from Assets`.

### Authoring
Store an ObjectId on your entity. ObjectDefinition can implicit cast to ObjectId:

```cs
public struct SpawnCommand : IComponentData
{
    public ObjectId Prefab;
    public float3 Position;
}

public class SpawnCommandAuthoring : MonoBehaviour
{
    public ObjectDefinition Prefab;
    public Vector3 Position;

    public class Baker : Baker<SpawnCommandAuthoring>
    {
        public override void Bake(SpawnCommandAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.None), new SpawnCommand
            {
                Prefab = authoring.Prefab,
                Position = authoring.Position,
            });
        }
    }
}
```

### Runtime
Access definitions via the **ObjectDefinitionRegistry** singleton buffer, set up from **ObjectManagementSettings**.

Setup:
1. Open `BovineLabs -> Settings` window to create settings
2. Add **SettingsAuthoring** script to a GameObject in any SubScene
3. For Netcode, ensure SubScene is loaded on both client and server

The ObjectDefinitionRegistry is a direct lookup buffer where ObjectId serves as the index (O(1) access):

```cs
public void OnUpdate(ref SystemState state)
{
    var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
    var objectDefinitionRegistry = SystemAPI.GetSingleton<ObjectDefinitionRegistry>();
    
    foreach (var (command, commandEntity) in SystemAPI.Query<RefRO<SpawnCommand>>().WithEntityAccess())
    {
        var entityPrefab = objectDefinitionRegistry[command.ValueRO.Prefab];
        var entity = ecb.Instantiate(entityPrefab);
        ecb.SetComponent(entity, LocalTransform.FromPosition(command.ValueRO.Position));
        ecb.DestroyEntity(commandEntity);
    }

    ecb.Playback(state.EntityManager);
}
```

### Search Integration
ObjectDefinition implements a `ObjectDefinitionSearchProvider` that integrates with Unity's search system, enabling powerful filtering during authoring workflows.

Use the `SearchContext` attribute to filter ObjectDefinition fields in the inspector:

```cs
[SearchContext("ca=creature", ObjectDefinition.SearchProviderType)]
public ObjectDefinition Definition;
```

**Available Search Filters:**
- `ca=` - Filter by category type (e.g., `ca=creature`)
- `cid=` - Filter by category ID value (e.g., `cid=5`)
- `n=` - Filter by ObjectDefinition name (e.g., `n=Goblin`)
- `d=` - Filter by ObjectDefinition description (e.g., `d=melee`)

Multiple filters can be combined for more precise filtering.

## Object Group

### Summary
Object groups provide an easy way to group and manage collections of objects. They are a collection of Object Definitions and can be built from other groups as well as excluding objects.

**Common Use Cases:**
- Spell/ability targeting (e.g., "affects all buildings")
- Unlocking systems (e.g., "unlock all tier 2 units")
- AI behavior categories (e.g., "can be harvested")
- UI filtering and display organization

### Authoring
Authored the same as Object Definitions except you store a GroupId instead of an ObjectId.

### Runtime
Access via the **ObjectGroupMatcher** singleton buffer to check if a GroupId matches an ObjectId:

```cs
public void OnUpdate(ref SystemState state)
{
    var objectGroupMatcher = SystemAPI.GetSingletonBuffer<ObjectGroupMatcher>();
    foreach (var (dealDamage, target) in SystemAPI.Query<DealDamage, Target>())
    {
        var targetId = SystemAPI.GetComponent<TargetId>(target.Entity);
        if (objectGroupMatcher.Matches(dealDamage.Group, targetId.Value))
        {
            // Deal damage
        }
    }
}
```

## Object Categories

### Summary
Object Categories provide automatic assignment and organization of objects into high-level groups. Categories use a flag system with values 0-31 inclusive (32 categories total).

Managed via K in the Object Categories window under `BovineLabs -> Settings`.

**Benefits:**
- Automatic organization of objects into logical groups
- Flag-based system allows objects to belong to multiple categories
- Integration with Object Groups for automatic assignment

### Runtime
Access via the standard K system:

```cs
var flag = ObjectCategories.NameToKey("actor");
```

The `[ObjectCategories(bool flag = true)]` attribute provides a custom drawer for selecting categories in editors.

When an Object Group is assigned to a category, all Object Definitions in that category are automatically assigned to the group as well.