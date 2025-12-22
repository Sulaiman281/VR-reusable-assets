WitShells XR Packages
=====================

Reusable VR utilities for movement, gestures, and controller/hand visuals.

Project URL
- https://github.com/Sulaiman281/VR-reusable-assets

Install (copy-paste Git URLs)
- Unity Package Manager → Add package from Git URL…
- Paste any of these:
  - XR Basic Setup:
    https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/XR-BasicSetup
  - Hand Swing Input:
    https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Hand-Swing
  - VR Face Direction Controller:
    https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Face-Direction

Packages
- XR Basic Setup: Reusable XR utilities for coordinating controller and hand visuals.
  - Includes `HandControllerVisualizer` (Animator `Trigger`/`Grip` via Input System) and `Utils`.
  - URL: https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/XR-BasicSetup
- Hand Swing Input: Detects VR hand swing gestures from left/right hand transforms.
  - Emits normalized intensity `UnityEvent<float>` (0 idle → 1 full swing) and per-hand velocity events.
  - URL: https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Hand-Swing
- VR Face Direction Controller: CharacterController-based movement using head-facing direction with smooth body rotation.
  - Normalized input (0–1) via `SetInput(float)` to control speed; rotates body toward facing during movement or large head yaw.
  - URL: https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/Face-Direction

Requirements
- Unity 2022.3+
- Input System (for XR Basic Setup)
- XR Hands (for XR Basic Setup)

XR Basic Setup — Quick setup
- Add `HandControllerVisualizer` to your controller visual root.
- Assign `triggerAction` and `gripAction` (InputActionProperty).
- Assign `handAnimator` (float params: `Trigger`, `Grip`).
- Optionally set `visualRoot` (defaults to this GameObject).
- Ensure an active `XRHandSubsystem` in your XR configuration.

Samples
- Package Manager → WitShells XR Basic Setup → Samples → Import "Basic Setup".