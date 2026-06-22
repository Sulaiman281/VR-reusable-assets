# WitShells XR Packages

Reusable VR/XR utilities for Unity — movement, gestures, hand visuals, and AI companions.

**Repository:** https://github.com/syed-suleman-shah-engineer/VR-reusable-assets

---

## 📑 Table of Contents

- [WitShells XR Packages](#witshells-xr-packages)
  - [📑 Table of Contents](#-table-of-contents)
  - [Installation](#installation)
  - [Packages \& Git URLs](#packages--git-urls)
    - [XR Basic Setup](#xr-basic-setup)
    - [VR Avatar Setup](#vr-avatar-setup)
    - [Hand Swing Input](#hand-swing-input)
    - [Face Direction Controller](#face-direction-controller)
    - [Sphere Robot](#sphere-robot)
    - [CheckPoint](#checkpoint)
  - [Package Details](#package-details)
    - [XR Basic Setup](#xr-basic-setup-1)
    - [VR Avatar Setup](#vr-avatar-setup-1)
    - [Hand Swing Input](#hand-swing-input-1)
    - [Face Direction Controller](#face-direction-controller-1)
    - [Sphere Robot](#sphere-robot-1)
    - [CheckPoint](#checkpoint-1)
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
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/XR-BasicSetup
```

### VR Avatar Setup
```
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/VR-AvatarSetup
```

### Hand Swing Input
```
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/Hand-Swing
```

### Face Direction Controller
```
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/Face-Direction
```

### Sphere Robot
```
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/SphereRobot
```

### CheckPoint
```
https://github.com/syed-suleman-shah-engineer/VR-reusable-assets.git?path=/Assets/WitShells/CheckPoint
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

### VR Avatar Setup

Tools to quickly rig and drive full-body humanoid avatars in VR.

**Features:**
- **Setup Wizard** (Window → WitShells → VR Avatar Setup) — Auto-configures IK targets and constraints.
- `XRFingerBoneTracker` — Maps XR finger tracking data directly to avatar bone rotations.
- `IKFootSolver` — Basic procedural foot placement for grounding.
- `AnimateOnInput` — Maps controller buttons (grip/trigger) to animator parameters.

**Quick Start:**
1. Open **WitShells → VR Avatar Setup**.
2. Assign your avatar root.
3. Assign VR targets (Camera/Hands) and IK targets (Head/Hands/Feet).
4. Click **Setup** to generate the Rig Builder and constraints.

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
spVR Avatar Setup | 2022.3+ | Input System, XR Hands, Animation Rigging |
| here.ClearDestination();                   // Cancel & go idle
sphere.SetFollowTarget(Transform target);    // Start following
sphere.StopFollowing();                      // Stop following
sphere.FaceToFaceTheTarget(Transform target); // Position face-to-face
```

**Use Cases:**
- AI companion robot
- Tutorial guide that follows the player
- Interactive NPC that approaches the player

---

### CheckPoint

A cylinder trigger zone with a custom Shader Graph material that fires events when objects enter or exit.

**Features:**
- `CheckPointTrigger` — Fires `OnObjectEntered` / `OnObjectExited` `UnityEvent<GameObject>` on trigger overlap
- `LayerMask` filtering — only reacts to objects on the configured layers
- `CheckPointTriggerData` — Attach an identifier, world position, and active flag to each zone
- **Editor tool** (GameObject → WitShells → Create CheckPoint) — spawns a Cylinder with the `CheckPoint-Cylinder` shader material and the trigger script pre-configured in one click

**Quick Start:**
1. Use **GameObject → WitShells → Create CheckPoint** to place a zone in the scene
2. Set `Trigger Layer Mask` to the layers you want to detect (e.g. `Player`)
3. Fill in `Data.Identifier` to tell zones apart in callbacks
4. Subscribe to `OnObjectEntered` / `OnObjectExited` in the Inspector or via code

---

## Requirements

| Package | Unity Version | Dependencies |
|---------|---------------|--------------|
| XR Basic Setup | 2022.3+ | Input System, XR Hands |
| Hand Swing Input | 2021.3+ | None |
| Face Direction Controller | 2021.3+ | None |
| Sphere Robot | 2021.3+ | None |
| CheckPoint | 2022.3+ | Shader Graph |

---

## License

MIT License — See individual package LICENSE files for details.