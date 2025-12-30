# WitShells Sphere Robot

A hovering AI sphere companion for Unity with smooth floating animation and multiple behavior states.

## Features

- **Smooth Floating Animation** - Gentle sine-wave based hovering with configurable amplitude and speed
- **Ground Following** - Automatically maintains hover height above terrain
- **Multiple Behavior States:**
  - `Idle` - Hovers in place with floating animation
  - `Follow` - Follows a target while maintaining orbit distance
  - `Destination` - Moves to a specific position then returns to idle
  - `FaceToFace` - Positions itself in front of a target, facing them

## Installation

### Via Package Manager (Git URL)
1. Open Window → Package Manager
2. Click the `+` button → Add package from git URL
3. Enter: `https://github.com/YOUR_REPO/SphereRobot.git`

### Manual Installation
Copy the `SphereRobot` folder into your project's `Assets` folder.

## Quick Start

1. Add the `AI_Sphere` component to any GameObject
2. Assign a `Ground Layer` for hover detection
3. Configure settings in the Inspector

## API Reference

### Public Methods

```csharp
// Set a destination point - sphere moves there then returns to Idle
sphere.SetDestination(Vector3 position);

// Clear destination and go idle
sphere.ClearDestination();

// Set follow target and start following
sphere.SetFollowTarget(Transform target);

// Stop following and go idle
sphere.StopFollowing();

// Move to face-to-face position with target
sphere.FaceToFaceTheTarget(Transform target);
```

### States

| State | Description |
|-------|-------------|
| `Idle` | Hovers in place with floating animation |
| `Follow` | Follows target, maintains `orbitRadius` distance |
| `Destination` | Moves to point, then returns to Idle |
| `FaceToFace` | Positions in front of target's forward direction |

### Inspector Settings

| Setting | Description |
|---------|-------------|
| **Movement Settings** | |
| Move Speed | Movement speed (units/sec) |
| **Follow Settings** | |
| Orbit Radius | Minimum distance to keep from follow target |
| Face To Face Distance | Distance to stand in front of target |
| **Destination Settings** | |
| Stopping Distance | Distance threshold to consider destination reached |
| **Float Settings** | |
| Float Amplitude | Vertical range of floating motion |
| Float Speed | Oscillation speed (cycles/sec) |
| **Ground Check Settings** | |
| Ground Check Distance | Raycast distance for ground detection |
| Height Offset | Hover height above ground |
| Ground Layer | LayerMask for ground detection |

## Editor Testing

Right-click the component in the Inspector to access context menu commands:
- **Move To Test Target** - Move to test target position
- **Set Follow Test Target** - Start following test target
- **Face To Face Test Target** - Move to face-to-face with test target
- **Stop / Go Idle** - Return to idle state

## Requirements

- Unity 2021.3 or later

## License

MIT License - See LICENSE file for details
