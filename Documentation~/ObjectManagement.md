# Object Management
## Object Definition
### Summary
Object Definition is a way of automatically mapping a unique ID to a specific object.  

The ID for an Object Definition is designed to automatically increment (not hash), fill holes and fix itself when multiple users create new definitions from different branches at the same time. If a definition is deleted a hole will exist but will be filled in when the next definition is created.

![Object Definition](Images/ObjectDefinition.png)

|Field|Description|
|----|----|
|Friendly Name|An optional designer friendly name for this object. Only used in custom tooling.|
|Description|An optional long description about this object that can be used to describe where it's used etc.|
|Categories|Define high level [Categories](#object-categories) that your objects will be automatically [Grouped](#object-group) into. |
|Prefab|The prefab of this object definition. This will be baked into an Entity prefab.|

### Creation
They can be created from `Create -> BovineLabs -> Object Definition`.
However, it's usually easier to just press your duplicate key on an existing definition and update fields.
It's advised that you always create a Null Definition as the first ID with 0.

You can also quickly create definitions for multiple assets via the menu `BovineLabs -> Utility -> Create Definitions from Assets`.  
This will automatically create a new Object Definition for every selected GameObject.

### Authoring
To authoring the ObjectDefinition to your entity store an ObjectId on your entity. ObjectDefinition can implicit cast to ObjectId which is just a convenient struct wrapper for an int.
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
                this.AddComponent(
                    this.GetEntity(TransformUsageFlags.None),
                    new SpawnCommand
                    {
                        Prefab = authoring.Prefab,
                        Position = authoring.Position,
                    });
            }
        }
    }
```

### Runtime
At runtime, definitions are accessed via the Singleton Buffer **ObjectDefinitionRegistry**, which is set up from the **ObjectManagementSettings**. To automatically create settings in Core, just open the `BovineLabs -> Settings` window, and all settings will be created.

To have these settings bake, add a **SettingsAuthoring** script to a GameObject in any SubScene. The baking scripts will handle the rest and ensure all GameObjects are converted to Entity prefabs. If you are using Netcode, make sure this SubScene is loaded on both the client and server.

To use the buffer, the ID of the Object Definition is the index in the buffer. For example:

```cs
[BurstCompile]
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

## Object Group
### Summary
Object groups are an easy way to group and manage a collection of objects. There are countless reasons you might want to group objects, from determining what your spells affect to unlocking new buildings, and object groups are designed to be a one-stop solution for all your groupings, regardless of your use case.

They are a collection of Object Definitions and can be built from other groups as well as excluding objects.

![Object Definition](Images/ObjectGroups.png)

### Authoring
They author the same as Object Definitions except instead of an ObjectId you store a GroupId.

### Runtime
At runtime a group matcher can be accessed via the Singleton Buffer **ObjectGroupMatcher** which can be used to check if a GroupId matches an ObjectId.

```cs
[BurstCompile]
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

In the future a map of GroupID to ObjectID will be provided.

## Object Categories
### Summary
Object Categories are an easy way to automatically assign and organize objects into high-level groups. Categories are a flag, and so allowed values are 0-31 inclusive.
They are managed via K in the Object Categories window under `BovineLabs -> Settings`.

![Object Definition](Images/ObjectCategories.png)

### Runtime
While not usually needed for Object Categories, they can still be accessed at runtime via the standard K way (and as always with K, this works inside burst).
```cs
var flag = K<ObjectCategories>.NameToKey("actor");
```

The `[ObjectCategories(bool flag = true)]` attribute, when applied to an int field, will provide a custom drawer for selecting categories in your editors.

One of the more powerful features of Object Categories, however, is the optional Object Group field. When assigned, `all Object Definitions that are put in this category will automatically be assigned to the group as well.` Note: if you intend to use the group, it should be assigned before assigning objects to the category, as this is currently not retroactive; however, this is planned.