# Camera

ECS camera integration providing frustum culling capabilities and automatic synchronization between Unity Camera and ECS components.

## Components

### CameraMain
Marks an entity as the main camera for client-side operations.

```csharp
public struct CameraMain : IComponentData
{
}
```

### CameraFrustumPlanes
Stores the six frustum planes for culling calculations. Provides indexed access with bounds checking.

```csharp
public struct CameraFrustumPlanes : IComponentData
{
    public float4 Left;
    public float4 Right;
    public float4 Bottom;
    public float4 Top;
    public float4 Near;
    public float4 Far;
    
    public float4 this[int index] { get; set; }
}
```

### CameraFrustumCorners
Contains world-space corner positions for the camera's view frustum.

```csharp
public struct CameraFrustumCorners : IComponentData
{
    public float3x4 NearPlane;
    public float3x4 FarPlane;
}
```

## Culling Extensions

### CameraFrustumPlanesPlanesExtensions
Provides frustum culling utilities for AABB intersection testing.

#### Methods

**Intersect(AABB)**: Returns detailed intersection result
- `IntersectResult.Out`: Object completely outside frustum
- `IntersectResult.In`: Object completely inside frustum  
- `IntersectResult.Partial`: Object partially intersecting frustum

**AnyIntersect(AABB)**: Fast boolean test for visibility culling

**GetNearCenter()**: Calculates center point of the near frustum plane

## Usage

### Basic Setup
Add `CameraAuthoring` to your camera GameObject to enable ECS integration:

```csharp
// CameraAuthoring automatically adds required components during baking
[SerializeField] private bool isMainCamera = true;
```

### Frustum Culling
Use frustum planes for efficient visibility culling:

```csharp
foreach (var (frustumPlanes, renderBounds) in 
    SystemAPI.Query<CameraFrustumPlanes, RenderBounds>())
{
    var bounds = new AABB 
    { 
        Center = renderBounds.Value.Center, 
        Extents = renderBounds.Value.Extents 
    };
    
    // Fast culling check
    if (!frustumPlanes.AnyIntersect(bounds))
        continue;
        
    // Detailed intersection for partial objects
    var result = frustumPlanes.Intersect(bounds);
    switch (result)
    {
        case IntersectResult.In:
            // Fully visible
            break;
        case IntersectResult.Partial:
            // Partially visible
            break;
        case IntersectResult.Out:
            // Not visible
            continue;
    }
}
```
