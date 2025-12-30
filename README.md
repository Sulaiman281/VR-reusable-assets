# WitShells XR Packages

Reusable VR/XR utilities for Unity — movement, gestures, hand visuals, and AI companions.

**Repository:** https://github.com/Sulaiman281/VR-reusable-assets

---

## 📑 Table of Contents

- [WitShells XR Packages](#witshells-xr-packages)
  - [📑 Table of Contents](#-table-of-contents)
  - [Installation](#installation)
  - [Packages \& Git URLs](#packages--git-urls)
    - [XR Basic Setup](#xr-basic-setup)
    - [Hand Swing Input](#hand-swing-input)
    - [Face Direction Controller](#face-direction-controller)
    - [Sphere Robot](#sphere-robot)
  - [Package Details](#package-details)
    - [XR Basic Setup](#xr-basic-setup-1)
    - [Hand Swing Input](#hand-swing-input-1)
    - [Face Direction Controller](#face-direction-controller-1)
    - [Sphere Robot](#sphere-robot-1)
  - [Requirements](#requirements)
  - [License](#license)

---

## Installation

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL...**
3. Paste any package URL from below

---

## Packages & Git URLs

### XR Basic Setup
```
https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/XR-BasicSetup
```

### Hand Swing Input
```
https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Hand-Swing
```

### Face Direction Controller
```
https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Face-Direction
```

### Sphere Robot
```
https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/SphereRobot
```

---

## Package Details

### XR Basic Setup

Reusable XR utilities for coordinating controller and hand visuals.

**Features:**
- `HandControllerVisualizer` — Animates hand models using Input System actions (`Trigger`, `Grip` float parameters)
- `HandVisualizer` — Manages hand tracking visuals with XR Hands subsystem
- `Utils` — Common XR helper functions

**Quick Start:**
1. Add `HandControllerVisualizer` to your controller visual root
2. Assign `triggerAction` and `gripAction` (InputActionProperty)
3. Assign `handAnimator` with float params: `Trigger`, `Grip`
4. Optionally set `visualRoot` (defaults to this GameObject)
5. Ensure an active `XRHandSubsystem` in your XR configuration

**Samples:** Package Manager → WitShells XR Basic Setup → Samples → Import "Basic Setup"

---

### Hand Swing Input

Detects VR hand swing gestures from left/right hand transforms.

**Features:**
- Emits normalized intensity via `UnityEvent<float>` (0 = idle → 1 = full swing)
- Per-hand velocity events for precise control
- Configurable swing thresholds and detection sensitivity

**Use Cases:**
- Arm-swing locomotion
- Gesture-based interactions
- Physical activity tracking

---

### Face Direction Controller

CharacterController-based movement using head-facing direction with smooth body rotation.

**Features:**
- Normalized input (0–1) via `SetInput(float)` to control movement speed
- Smooth body rotation toward head facing direction
- Automatic rotation during movement or large head yaw angles
- Works seamlessly with Hand Swing Input for arm-swing locomotion

**Use Cases:**
- VR locomotion that follows where you look
- Natural walking direction in VR
- Combine with hand swing for immersive movement

---

### Sphere Robot

A hovering AI sphere companion with smooth floating animation and multiple behavior states.

**Features:**
- **Smooth Floating Animation** — Gentle sine-wave hovering with configurable amplitude and speed
- **Ground Following** — Automatically maintains hover height above terrain
- **Multiple Behavior States:**
  - `Idle` — Hovers in place with floating animation
  - `Follow` — Follows a target while maintaining orbit distance
  - `Destination` — Moves to a specific position then returns to idle
  - `FaceToFace` — Positions itself in front of a target, facing them

**Public API:**
```csharp
sphere.SetDestination(Vector3 position);    // Move to point
sphere.ClearDestination();                   // Cancel & go idle
sphere.SetFollowTarget(Transform target);    // Start following
sphere.StopFollowing();                      // Stop following
sphere.FaceToFaceTheTarget(Transform target); // Position face-to-face
```

**Use Cases:**
- AI companion robot
- Tutorial guide that follows the player
- Interactive NPC that approaches the player

---

## Requirements

| Package | Unity Version | Dependencies |
|---------|---------------|--------------|
| XR Basic Setup | 2022.3+ | Input System, XR Hands |
| Hand Swing Input | 2021.3+ | None |
| Face Direction Controller | 2021.3+ | None |
| Sphere Robot | 2021.3+ | None |

---

## License

MIT License — See individual package LICENSE files for details.