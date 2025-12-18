WitShells XR Basic Setup
========================

Reusable XR utilities for coordinating controller and hand visuals.

What’s inside
- `HandControllerVisualizer`: Drives Animator `Trigger`/`Grip` via Input System `InputActionProperty` and toggles controller visuals based on controller vs hand tracking.
- `Utils`: Small helpers for detection (replace with your own if preferred).

Project URL
- https://github.com/Sulaiman281/VR-reusable-assets

Install (copy-paste Git URL)
- Unity Package Manager → Add package from Git URL…
- Paste this:
  - https://github.com/Sulaiman281/VR-reusable-assets.git?path=/Assets/WitShells/XR-BasicSetup

Requirements
- Unity 2022.3+
- Input System
- XR Hands

Quick setup
- Add `HandControllerVisualizer` to your controller visual root.
- Assign `triggerAction` and `gripAction` (InputActionProperty).
- Assign `handAnimator` (float params: `Trigger`, `Grip`).
- Optionally set `visualRoot` (defaults to this GameObject).
- Ensure an active `XRHandSubsystem` in your XR configuration.

Samples
- Package Manager → WitShells XR Basic Setup → Samples → Import "Basic Setup".