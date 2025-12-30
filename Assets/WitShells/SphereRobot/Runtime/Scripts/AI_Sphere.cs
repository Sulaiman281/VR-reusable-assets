namespace WitShells.SphereRobot
{
    using UnityEngine;

    public enum SphereState
    {
        Idle,
        Follow,
        Destination,
        FaceToFace
    }

    public class AI_Sphere : MonoBehaviour
    {
        public SphereState currentState = SphereState.Idle;

        [SerializeField, Tooltip("Target to follow when in Follow state.")]
        private Transform followTarget;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;

        [Header("Follow Settings")]
        [SerializeField, Tooltip("Minimum distance to keep from target")]
        private float orbitRadius = 4f;

        [SerializeField, Tooltip("Threshold to consider sphere as stopped")]
        private float stoppedThreshold = 0.05f;

        [SerializeField, Tooltip("Distance to stand in front of target for face-to-face")]
        private float faceToFaceDistance = 2f;

        [Header("Destination Settings")]
        [SerializeField, Tooltip("Distance to stop before reaching destination")]
        private float stoppingDistance = 0.5f;

        private Vector3 _destination;
        private bool _hasDestination;

        private Transform _faceToFaceTarget;

        private bool _isMoving;

        [Header("Float Settings")]
        [SerializeField, Tooltip("Vertical amplitude of the floating motion (units)")]
        private float floatAmplitude = 0.25f;

        [SerializeField, Tooltip("Speed of the floating oscillation (cycles per second)")]
        private float floatSpeed = 1.0f;

        private float _floatPhase;
        private float _floatOffsetY;
        private float _baseHoverY;
        private float _verticalVelocity;


        [Header("Ground Check Settings")]
        public float groundCheckDistance = 10f;
        public float heightOffset = 1.5f;
        public LayerMask groundLayer;


        private void OnEnable()
        {
            _floatPhase = Random.Range(0f, Mathf.PI * 2f);

            if (IsGrounded(out float distance))
            {
                _baseHoverY = transform.position.y - distance + heightOffset;
            }
            else
            {
                _baseHoverY = transform.position.y;
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case SphereState.Idle:
                    HandleIdleState();
                    break;
                case SphereState.Follow:
                    HandleFollowState();
                    break;
                case SphereState.Destination:
                    HandleDestinationState();
                    break;
                case SphereState.FaceToFace:
                    HandleFaceToFaceState();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (currentState == SphereState.Idle || currentState == SphereState.Follow || currentState == SphereState.FaceToFace)
            {
                if (IsGrounded(out float distance))
                {
                    float groundY = transform.position.y - distance;
                    float desiredBaseY = groundY + heightOffset;
                    _baseHoverY = Mathf.SmoothDamp(_baseHoverY, desiredBaseY, ref _verticalVelocity, 0.3f);
                }
            }
        }


        private void HandleIdleState()
        {
            _isMoving = false;
            ApplyFloatAndHeight();
        }

        private void HandleFollowState()
        {
            if (followTarget == null)
            {
                _isMoving = false;
                ApplyFloatAndHeight();
                return;
            }

            Vector3 toTarget = followTarget.position - transform.position;
            toTarget.y = 0; // Only consider horizontal distance
            float distanceToTarget = toTarget.magnitude;

            if (distanceToTarget > orbitRadius)
            {
                // Move towards target, stop at orbit radius
                _isMoving = true;
                Vector3 direction = toTarget.normalized;
                float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, distanceToTarget - orbitRadius);

                Vector3 newPos = transform.position + direction * moveDistance;
                newPos.y = _baseHoverY; // No floating while moving
                transform.position = newPos;

                // Face movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 5f
                    );
                }
            }
            else
            {
                // Within orbit radius - stay and float
                _isMoving = false;
                ApplyFloatAndHeight();

                // Slowly face the target
                Vector3 lookDir = toTarget.normalized;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookDir),
                        Time.deltaTime * 2f
                    );
                }
            }
        }

        private void HandleDestinationState()
        {
            if (!_hasDestination)
            {
                currentState = SphereState.Idle;
                return;
            }

            Vector3 toDestination = _destination - transform.position;
            toDestination.y = 0; // Only consider horizontal distance
            float distanceToDestination = toDestination.magnitude;

            if (distanceToDestination > stoppingDistance)
            {
                // Move towards destination
                _isMoving = true;
                Vector3 direction = toDestination.normalized;
                float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, distanceToDestination - stoppingDistance);

                Vector3 newPos = transform.position + direction * moveDistance;
                newPos.y = _baseHoverY; // No floating while moving
                transform.position = newPos;

                // Face movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 5f
                    );
                }
            }
            else
            {
                // Reached destination - go back to idle
                _isMoving = false;
                _hasDestination = false;
                currentState = SphereState.Idle;
            }
        }

        /// <summary>
        /// Set a destination for the sphere to move to.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            _hasDestination = true;
            currentState = SphereState.Destination;
        }

        /// <summary>
        /// Clear the current destination.
        /// </summary>
        public void ClearDestination()
        {
            _hasDestination = false;
            currentState = SphereState.Idle;
        }

        /// <summary>
        /// Set the follow target and switch to Follow state.
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[AI_Sphere] SetFollowTarget: target is null!");
                currentState = SphereState.Idle;
                return;
            }

            followTarget = target;
            currentState = SphereState.Follow;
        }

        /// <summary>
        /// Stop following and go idle.
        /// </summary>
        public void StopFollowing()
        {
            followTarget = null;
            currentState = SphereState.Idle;
        }

        /// <summary>
        /// Move the sphere to stand in front of the target, facing it.
        /// The sphere will position itself at the target's forward direction.
        /// </summary>
        public void FaceToFaceTheTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[AI_Sphere] FaceToFaceTheTarget: target is null!");
                return;
            }

            _faceToFaceTarget = target;
            currentState = SphereState.FaceToFace;
        }

        private void HandleFaceToFaceState()
        {
            if (_faceToFaceTarget == null)
            {
                currentState = SphereState.Idle;
                return;
            }

            // Calculate the position in front of the target
            Vector3 targetForward = _faceToFaceTarget.forward;
            targetForward.y = 0;
            targetForward.Normalize();

            Vector3 faceToFacePosition = _faceToFaceTarget.position + targetForward * faceToFaceDistance;

            Vector3 toPosition = faceToFacePosition - transform.position;
            toPosition.y = 0;
            float distanceToPosition = toPosition.magnitude;

            if (distanceToPosition > stoppingDistance)
            {
                // Move towards face-to-face position
                _isMoving = true;
                Vector3 direction = toPosition.normalized;
                float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, distanceToPosition - stoppingDistance);

                Vector3 newPos = transform.position + direction * moveDistance;
                newPos.y = _baseHoverY;
                transform.position = newPos;

                // Face movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 5f
                    );
                }
            }
            else
            {
                // Reached position - stay, float, and face the target
                _isMoving = false;
                ApplyFloatAndHeight();

                // Face the target
                Vector3 lookDir = _faceToFaceTarget.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(lookDir.normalized),
                        Time.deltaTime * 3f
                    );
                }
            }
        }

        private void ApplyFloatAndHeight()
        {
            // Only float when not moving
            float floatY = _isMoving ? 0f : Mathf.Sin(Time.time * floatSpeed + _floatPhase) * floatAmplitude;

            transform.position = new Vector3(
                transform.position.x,
                _baseHoverY + floatY,
                transform.position.z
            );
        }

        private bool IsGrounded(out float distanceToGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
            {
                distanceToGround = hit.distance;
                return true;
            }
            distanceToGround = .1f;
            return false;
        }

#if UNITY_EDITOR

        [Header("Editor Testing")]
        [SerializeField, Tooltip("Test target for destination testing")]
        private Transform testTarget;

        [ContextMenu("Move To Test Target")]
        private void MoveToTestTarget()
        {
            if (testTarget != null)
            {
                SetDestination(testTarget.position);
                Debug.Log($"[AI_Sphere] Moving to destination: {testTarget.position}");
            }
            else
            {
                Debug.LogWarning("[AI_Sphere] Test target is not assigned!");
            }
        }

        [ContextMenu("Set Follow Test Target")]
        private void SetFollowTestTarget()
        {
            if (testTarget != null)
            {
                SetFollowTarget(testTarget);
                Debug.Log($"[AI_Sphere] Now following: {testTarget.name}");
            }
            else
            {
                Debug.LogWarning("[AI_Sphere] Test target is not assigned!");
            }
        }

        [ContextMenu("Stop / Go Idle")]
        private void StopAndIdle()
        {
            ClearDestination();
            currentState = SphereState.Idle;
            Debug.Log("[AI_Sphere] Stopped, now idle.");
        }

        [ContextMenu("Face To Face Test Target")]
        private void FaceToFaceTestTarget()
        {
            if (testTarget != null)
            {
                FaceToFaceTheTarget(testTarget);
                Debug.Log($"[AI_Sphere] Moving to face-to-face with: {testTarget.name}");
            }
            else
            {
                Debug.LogWarning("[AI_Sphere] Test target is not assigned!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Ground check ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

            // Orbit radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, orbitRadius);

            // Destination
            if (_hasDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _destination);
                Gizmos.DrawWireSphere(_destination, stoppingDistance);
            }

            // Test target
            if (testTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, testTarget.position);
                Gizmos.DrawWireSphere(testTarget.position, 0.3f);
            }
        }

#endif
    }
}