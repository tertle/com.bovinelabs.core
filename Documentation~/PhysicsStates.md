# PhysicsStates

## Summary

PhysicsStates provides stateful collision and trigger event tracking for Unity Physics, transforming single-frame physics events into Enter, Stay, and Exit states. This eliminates the need for manual event state tracking and provides a clean, ECS-friendly approach to physics event handling across multiple frames.

This implementation provides a similar API to the popular stateful events from Unity samples, but significantly improved for efficiency with parallel processing.

**Key Features:**
- Automatic Enter/Stay/Exit state tracking for collision and trigger events
- Optional detailed collision information (contact points, impulse, positions)

## Core Components

**Event Buffer Components:**
- `StatefulCollisionEvent` - Buffer containing collision events with state information
- `StatefulTriggerEvent` - Buffer containing trigger events with state information

**Configuration Components:**
- `StatefulCollisionEventDetails` - Enables detailed collision information calculation

**Event States:**
- `StatefulEventState.Enter` - Event just started this frame
- `StatefulEventState.Stay` - Event continuing from previous frame
- `StatefulEventState.Exit` - Event ended this frame

## Usage

### Basic Collision Event Tracking

Add the authoring component to entities that need collision state tracking:

```csharp
// In authoring - add StatefulCollisionEventAuthoring to GameObject
public class StatefulCollisionEventAuthoring : MonoBehaviour
{
    public bool EventDetails; // Enable detailed collision information
}
```

Process collision events in your systems:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(StatefulCollisionEventSystem))]
public partial struct CollisionHandlerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new ProcessCollisionEventsJob().ScheduleParallel();
    }

    [BurstCompile]
    private partial struct ProcessCollisionEventsJob : IJobEntity
    {
        private void Execute(Entity entity, in DynamicBuffer<StatefulCollisionEvent> collisionEvents)
        {
            foreach (var collisionEvent in collisionEvents)
            {
                switch (collisionEvent.State)
                {
                    case StatefulEventState.Enter:
                        // Handle collision start
                        break;
                    case StatefulEventState.Stay:
                        // Handle ongoing collision
                        break;
                    case StatefulEventState.Exit:
                        // Handle collision end
                        break;
                }
            }
        }
    }
}
```

### Detailed Collision Information

Enable detailed collision information by setting `EventDetails = true` in the authoring component:

```csharp
private void Execute(Entity entity, in DynamicBuffer<StatefulCollisionEvent> collisionEvents)
{
    foreach (var collisionEvent in collisionEvents)
    {
        if (collisionEvent.TryGetDetails(out var details))
        {
            // Access detailed collision information
            var impulse = details.EstimatedImpulse;
            var contactPosition = details.AverageContactPointPosition;
            var contactType = details.NumberOfContactPoints; // 1=vertex, 2=edge, 3+=face
        }
    }
}
```

### Trigger Event Tracking

Similar pattern for trigger events:

```csharp
// Add StatefulTriggerEventAuthoring to GameObject
public class StatefulTriggerEventAuthoring : MonoBehaviour
{
    // No additional configuration needed
}
```

```csharp
[BurstCompile]
private partial struct ProcessTriggerEventsJob : IJobEntity
{
    private void Execute(Entity entity, in DynamicBuffer<StatefulTriggerEvent> triggerEvents)
    {
        foreach (var triggerEvent in triggerEvents)
        {
            switch (triggerEvent.State)
            {
                case StatefulEventState.Enter:
                    // Object entered trigger
                    break;
                case StatefulEventState.Exit:
                    // Object left trigger
                    break;
            }
        }
    }
}
```

## System Integration

PhysicsStates automatically integrates with Unity Physics systems:

- `StatefulCollisionEventSystem` and `StatefulTriggerEventSystem` run after `PhysicsSimulationGroup`
- Event buffers are cleared each frame by `StatefulCollisionEventClearSystem` and `StatefulTriggerEventClearSystem`
- Events are stored from both entities' perspectives for efficient queries

## Performance Considerations

- Enable `EventDetails` only when detailed collision information is needed
- Event buffers are automatically cleared each frame to prevent memory buildup