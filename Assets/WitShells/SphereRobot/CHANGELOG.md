# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-12-30

### Added
- Initial release
- `AI_Sphere` component with hovering behavior
- Four behavior states: Idle, Follow, Destination, FaceToFace
- Smooth floating animation with configurable amplitude and speed
- Ground detection and automatic height adjustment
- Public API methods:
  - `SetDestination(Vector3)`
  - `ClearDestination()`
  - `SetFollowTarget(Transform)`
  - `StopFollowing()`
  - `FaceToFaceTheTarget(Transform)`
- Editor context menu for testing
- Gizmos for visualizing ground check, orbit radius, and destinations
