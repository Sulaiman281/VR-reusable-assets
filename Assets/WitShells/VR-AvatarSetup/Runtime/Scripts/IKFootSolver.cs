using UnityEngine;

namespace WitShells.VRAvatarSetup
{
    public class IKFootSolver : MonoBehaviour
    {
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private Transform body;
        [SerializeField] private IKFootSolver otherFoot;

        [Header("Step")]
        [SerializeField] private float speed = 4f;
        [SerializeField] private float stepDistance = 0.2f;
        [SerializeField] private float forwardStepLength = 0.2f;
        [SerializeField] private float sideStepLength = 0.1f;
        [SerializeField] private float stepHeight = 0.3f;

        [Header("Foot Offsets")]
        [SerializeField] private Vector3 footOffset;
        [SerializeField] private Vector3 footRotationOffset;
        [SerializeField] private float footYPositionOffset = 0.1f;

        [Header("Ground Ray")]
        [SerializeField] private float rayStartYOffset;
        [SerializeField] private float rayLength = 1.5f;

        private float footSpacing;
        private Vector3 oldPosition;
        private Vector3 currentPosition;
        private Vector3 targetPosition;
        private float lerp;

        public bool IsMoving => lerp < 1f;

        private void Start()
        {
            footSpacing = transform.localPosition.x;
            oldPosition = transform.position;
            currentPosition = oldPosition;
            targetPosition = oldPosition;
            lerp = 1f;
        }

        private void Update()
        {
            if (body == null)
                return;

            transform.position = currentPosition + Vector3.up * footYPositionOffset;
            transform.localRotation = Quaternion.Euler(footRotationOffset);

            var rayOrigin = body.position + body.right * footSpacing + Vector3.up * rayStartYOffset;
            var ray = new Ray(rayOrigin, Vector3.down);
            Debug.DrawRay(rayOrigin, Vector3.down * rayLength, Color.yellow);

            if (Physics.Raycast(ray, out var hit, rayLength, terrainLayer))
            {
                TryQueueStep(hit.point);
            }

            UpdateStepMotion();
        }

        private void TryQueueStep(Vector3 hitPoint)
        {
            if (Vector3.Distance(targetPosition, hitPoint) <= stepDistance)
                return;

            if (otherFoot != null && otherFoot.IsMoving)
                return;

            if (lerp < 1f)
                return;

            lerp = 0f;
            var planarDirection = Vector3.ProjectOnPlane(hitPoint - currentPosition, Vector3.up).normalized;
            var forwardDot = Vector3.Dot(body.forward, planarDirection);
            var isForward = Mathf.Abs(forwardDot) > 0.6f;
            var length = isForward ? forwardStepLength : sideStepLength;

            targetPosition = hitPoint + planarDirection * length + footOffset;
        }

        private void UpdateStepMotion()
        {
            if (lerp >= 1f)
            {
                oldPosition = targetPosition;
                return;
            }

            var interpolated = Vector3.Lerp(oldPosition, targetPosition, lerp);
            interpolated.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = interpolated;
            lerp += Time.deltaTime * speed;
        }
    }
}
