using UnityEngine;

namespace WitShells.FaceDirection
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("WitShells/Input/Face Direction Controller")]
    public class FaceDirectionController : MonoBehaviour
    {
        [Header("References")]
        public CharacterController characterController;
        public Transform headTransform;
        public Transform bodyTransform;

        [Header("Movement")]
        [Min(0f)] public float maxMoveSpeed = 2.0f;
        [Min(0f)] public float speedChangeRate = 10.0f;
        public bool useGravity = true;
        [Min(0f)] public float gravity = 9.81f;
        [Min(0f)] public float groundStickForce = 0.2f;

        [Header("Body Rotation")]
        [Min(0f)] public float rotateSpeed = 360f;
        [Range(0f, 90f)] public float idleHeadAngleThreshold = 30f;
        public bool rotateOnlyWhenMoving = false;

        [Header("Input")]
        [Range(0f, 1f)] public float inputStrength = 0f;

        private float _currentSpeed;
        private float _verticalVelocity;

        public void SetInput(float normalized)
        {
            inputStrength = Mathf.Clamp01(normalized);
        }

        public void SetHead(Transform head)
        {
            headTransform = head;
        }

        public void SetBody(Transform body)
        {
            bodyTransform = body;
        }

        private void Awake()
        {
            if (!characterController) characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 forward = Vector3.zero;
            if (headTransform)
            {
                forward = headTransform.forward;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.zero;
            }

            float targetSpeed = inputStrength * maxMoveSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

            Vector3 horizontalMove = forward * _currentSpeed;

            if (useGravity)
            {
                if (characterController.isGrounded)
                {
                    _verticalVelocity = -groundStickForce;
                }
                else
                {
                    _verticalVelocity -= gravity * Time.deltaTime;
                }
            }
            else
            {
                _verticalVelocity = 0f;
            }

            Vector3 move = horizontalMove + new Vector3(0f, _verticalVelocity, 0f);
            characterController.Move(move * Time.deltaTime);

            TryRotateBody(forward);
        }

        private void TryRotateBody(Vector3 forward)
        {
            if (bodyTransform == null) return;
            Transform target = bodyTransform ? bodyTransform : transform;
            if (forward == Vector3.zero) return;

            Vector3 bodyForward = target.forward;
            bodyForward.y = 0f;
            bodyForward = bodyForward.sqrMagnitude > 0f ? bodyForward.normalized : Vector3.forward;

            float angle = Vector3.SignedAngle(bodyForward, forward, Vector3.up);
            bool hasMovement = inputStrength > 0.001f && _currentSpeed > 0.001f;
            bool shouldRotate = hasMovement || Mathf.Abs(angle) >= idleHeadAngleThreshold;
            if (rotateOnlyWhenMoving) shouldRotate = hasMovement;
            if (!shouldRotate) return;

            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
            target.rotation = Quaternion.RotateTowards(target.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }
}
