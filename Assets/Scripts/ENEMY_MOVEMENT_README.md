# Enemy Movement Components

## Moveable Component

The `Moveable` component provides basic movement functionality for game entities (like enemies).

### Features:
- Configurable movement and run speeds
- Movement state properties: `IsMoving`, `IsRunning`, `IsIdling`
- Directional facing properties: `IsFacingUp`, `IsFacingDown`, `IsFacingLeft`, `IsFacingRight`
- Exposes a `FacingDirection` vector for the current facing direction

### Public Methods:
- `Move(Vector2 direction, bool run = false)` - Moves the entity in the specified direction
- `FaceDirection(Vector2 direction)` - Changes facing direction without moving
- `Stop()` - Stops all movement

### Inspector Fields:
- **Move Speed** - Base movement speed (default: 3)
- **Run Speed Multiplier** - Multiplier applied when running (default: 2)

### Requirements:
- Automatically adds a `Rigidbody2D` component

---

## RandomMovementBehaviour Component

The `RandomMovementBehaviour` component creates autonomous random movement patterns for enemies.

### Features:
- Randomly moves in cardinal directions (up, down, left, right)
- Alternates between moving and idling
- Sometimes only changes facing direction without moving
- Automatically stops when detecting damageable objects ahead
- Stops when obstacles are detected

### Inspector Fields:
- **Min Move Duration** - Minimum time to move in one direction (default: 0.5s)
- **Max Move Duration** - Maximum time to move in one direction (default: 2s)
- **Min Idle Duration** - Minimum time to stay idle (default: 0.5s)
- **Max Idle Duration** - Maximum time to stay idle (default: 3s)
- **Only Face Chance** - Probability (0-1) of only facing a direction without moving (default: 0.2)
- **Detection Radius** - Distance to check for obstacles ahead (default: 1)
- **Obstacle Layer Mask** - Layers considered as obstacles

### Requirements:
- Requires a `Moveable` component
- Optionally uses a `NearestDetector` component for detecting Damageable objects
  - Configure the NearestDetector with `TargetDetectableName = "Damageable"` to detect damageable entities

### Usage Example (Orc):
1. Add `Moveable` component to the enemy GameObject
2. Add `RandomMovementBehaviour` component
3. (Optional) Add a child GameObject with a `NearestDetector` component:
   - Set `TargetDetectableName` to "Damageable"
   - Configure `DetectionRadius` as needed
4. Configure the obstacle layer mask to include walls/obstacles
5. Adjust movement and idle durations as desired

The enemy will now move randomly, avoiding obstacles and stopping when approaching damageable objects.
