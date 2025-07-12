# PhysicsUpdate

## Summary

PhysicsUpdate ensures Unity Physics spatial data structures remain current when running at frame rates higher than the fixed timestep rate. When FPS exceeds the physics simulation frequency (typically 60 FPS), this extension rebuilds the physics world without running additional physics simulations, preventing stale spatial queries.

**Key Features:**
- Maintains current physics spatial data at high frame rates
- Selective updates only when fixed timestep hasn't run

## Usage

Once enabled, the system operates automatically:

- No additional setup required - systems run automatically
- Physics world will be rebuilt when:
  - Frame rate > fixed timestep rate
  - Fixed step physics didn't run this frame