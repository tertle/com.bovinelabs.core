# EntityBlob

## Summary

The EntityBlob system provides a memory-efficient way to store multiple `BlobAssetReference<T>` objects in a single blob using perfect hash maps. This allows entities to have multiple blob assets accessible by integer keys while minimizing memory overhead and providing fast lookup performance.

**Key Features:**
- Combines multiple blob assets into a single blob using perfect hash maps
- Type-safe access to individual blob assets via integer keys
- Memory efficient storage with optimized blob layout

## Core Components

- **EntityBlob**: Runtime component that stores the perfect hash map of blob assets
- **EntityBlobBakedData**: Baking-time component representing individual blob entries

## Usage

### Adding Blobs During Baking

To add blob assets to an entity, create `EntityBlobBakedData` components during baking:

```csharp
public class MyAuthoringComponent : MonoBehaviour
{
    public MyBlobData[] blobData;
}

public class MyBaker : Baker<MyAuthoringComponent>
{
    public override void Bake(MyAuthoringComponent authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        
        for (int i = 0; i < authoring.blobData.Length; i++)
        {
            // Create your blob asset
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<MyBlobStruct>();
            // ... populate blob data ...
            var blobAssetRef = blobBuilder.CreateBlobAssetReference<MyBlobStruct>(Allocator.Persistent);
            
            // Convert to byte blob for EntityBlobBakedData
            var byteBlob = UnsafeUtility.As<BlobAssetReference<MyBlobStruct>, BlobAssetReference<byte>>(ref blobAssetRef);
            
            // Add to entity with unique key
            AddComponent(entity, new EntityBlobBakedData
            {
                Target = entity,
                Key = i,  // Use unique integer key
                Blob = byteBlob
            });
            
            blobBuilder.Dispose();
        }
    }
}
```

### Accessing Blobs at Runtime

Access blob assets from the EntityBlob component using their keys:

```csharp
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var entityBlob in SystemAPI.Query<RefRO<EntityBlob>>())
        {
            // Safe access with null checking
            if (entityBlob.ValueRO.TryGet<MyBlobStruct>(0, out var blobAsset))
            {
                // Use the blob asset
                var data = blobAsset.Value.SomeField;
            }
            
            // Direct access (throws if key doesn't exist)
            var directBlob = entityBlob.ValueRO.Get<MyBlobStruct>(1);
        }
    }
}
```

## Baking Pipeline

1. **Authoring Phase**: Multiple `EntityBlobBakedData` components are added to entities during baking
2. **Grouping Phase**: `EntityBlobBakingSystem` groups all blob data by target entity
3. **Hash Map Creation**: A perfect hash map is constructed for each entity's blob collection
4. **Blob Assembly**: Individual blobs are copied into the final combined blob structure
5. **Storage Phase**: `EntityBlobBakingBlobStoreSystem` registers blobs with the asset store

### Key Collision Handling

If multiple `EntityBlobBakedData` components use the same key for the same target entity, the system will:
- Log an error message identifying the duplicate key and entity
- Ignore subsequent blob data with the same key
- Continue processing other valid blob entries

## API Reference

### EntityBlob

**Main Methods:**
- `bool TryGet<T>(int key, out BlobAssetReference<T> blobAssetReference)`: Safe blob retrieval with existence checking
- `BlobAssetReference<T> Get<T>(int key)`: Direct blob access (throws exception if key not found)

**Properties:**
- `BlobCount`: Number of blob assets stored (debugging/inspection)
- `BlobSize`: Total size of the blob in bytes (debugging/inspection)

### EntityBlobBakedData

**Fields:**
- `Entity Target`: The entity that will receive the final EntityBlob component
- `int Key`: Unique identifier for this blob within the target entity
- `BlobAssetReference<byte> Blob`: The blob asset data as a byte blob

## Performance Considerations

- Perfect hash maps provide O(1) lookup time for blob access
- All blob data is stored contiguously for cache efficiency

## Live Baking Support

The system fully supports Unity's live baking workflow:
- Existing EntityBlob components are automatically removed during rebaking
- New blob collections are reconstructed from updated authoring data