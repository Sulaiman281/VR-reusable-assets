using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace WitShells.VRAvatarSetup
{
    [DefaultExecutionOrder(10000)]
    public class XRFingerMuscleOnlyTracker : MonoBehaviour
    {
        [Header("Avatar")]
        [SerializeField] private Animator animator;

        [Header("Tracking")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool trackLeftHand = true;
        [SerializeField] private bool trackRightHand = true;
        [SerializeField] private bool preserveCurrentBodyPose = true;
        [SerializeField] private bool preserveAnimatorTransform = true;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float muscleSmoothing = 18f;

        [Header("Finger Mapping")]
        [SerializeField] private float proximalMaxBend = 95f;
        [SerializeField] private float intermediateMaxBend = 100f;
        [SerializeField] private float distalMaxBend = 85f;

        private XRHandSubsystem handSubsystem;
        private HumanPoseHandler poseHandler;
        private HumanPose pose;

        private static readonly List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();

        public bool leftHandTracked { get; private set; }
        public bool rightHandTracked { get; private set; }

        private readonly int[] leftThumb = { 55, 57, 58 };
        private readonly int[] leftIndex = { 59, 61, 62 };
        private readonly int[] leftMiddle = { 63, 65, 66 };
        private readonly int[] leftRing = { 67, 69, 70 };
        private readonly int[] leftLittle = { 71, 73, 74 };

        private readonly int[] rightThumb = { 75, 77, 78 };
        private readonly int[] rightIndex = { 79, 81, 82 };
        private readonly int[] rightMiddle = { 83, 85, 86 };
        private readonly int[] rightRing = { 87, 89, 90 };
        private readonly int[] rightLittle = { 91, 93, 94 };

        private void OnEnable()
        {
            EnsurePoseHandler();
            InitializeHandSubsystem();
        }

        private void LateUpdate()
        {
            if (!enableTracking)
                return;

            EnsurePoseHandler();
            UpdateHandSubsystem();

            if (poseHandler == null || handSubsystem == null || !handSubsystem.running)
                return;

            var leftHand = handSubsystem.leftHand;
            var rightHand = handSubsystem.rightHand;

            leftHandTracked = leftHand.isTracked;
            rightHandTracked = rightHand.isTracked;

            poseHandler.GetHumanPose(ref pose);
            Vector3 currentBodyPosition = pose.bodyPosition;
            Quaternion currentBodyRotation = pose.bodyRotation;

            if (trackLeftHand && leftHandTracked)
                ApplyFingerCurls(leftHand, true, ref pose);

            if (trackRightHand && rightHandTracked)
                ApplyFingerCurls(rightHand, false, ref pose);

            if (preserveCurrentBodyPose)
            {
                pose.bodyPosition = currentBodyPosition;
                pose.bodyRotation = currentBodyRotation;
            }

            Vector3 savedLocalPosition = default;
            Quaternion savedLocalRotation = default;
            Vector3 savedLocalScale = default;
            if (preserveAnimatorTransform && animator != null)
            {
                savedLocalPosition = animator.transform.localPosition;
                savedLocalRotation = animator.transform.localRotation;
                savedLocalScale = animator.transform.localScale;
            }

            poseHandler.SetHumanPose(ref pose);

            if (preserveAnimatorTransform && animator != null)
            {
                animator.transform.localPosition = savedLocalPosition;
                animator.transform.localRotation = savedLocalRotation;
                animator.transform.localScale = savedLocalScale;
            }
        }

        private void OnDisable()
        {
            leftHandTracked = false;
            rightHandTracked = false;
        }

        private void OnDestroy()
        {
            if (poseHandler != null)
            {
                poseHandler.Dispose();
                poseHandler = null;
            }
        }

        private void EnsurePoseHandler()
        {
            if (poseHandler != null)
                return;

            if (animator == null)
                return;

            if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid)
            {
                Debug.LogWarning($"[{nameof(XRFingerMuscleOnlyTracker)}] Animator must use a valid Humanoid avatar.");
                return;
            }

            poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            pose = new HumanPose();
        }

        private void InitializeHandSubsystem()
        {
            SubsystemManager.GetSubsystems(s_SubsystemsReuse);
            for (var i = 0; i < s_SubsystemsReuse.Count; ++i)
            {
                if (!s_SubsystemsReuse[i].running)
                    continue;

                handSubsystem = s_SubsystemsReuse[i];
                return;
            }
        }

        private void UpdateHandSubsystem()
        {
            if (handSubsystem != null && handSubsystem.running)
                return;

            InitializeHandSubsystem();
        }

        private void ApplyFingerCurls(XRHand hand, bool isLeft, ref HumanPose targetPose)
        {
            var thumb = ComputeFingerCurlTriple(hand,
                XRHandJointID.ThumbMetacarpal,
                XRHandJointID.ThumbProximal,
                XRHandJointID.ThumbDistal,
                XRHandJointID.ThumbTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var index = ComputeFingerCurlTripleWithTip(hand,
                XRHandJointID.IndexMetacarpal,
                XRHandJointID.IndexProximal,
                XRHandJointID.IndexIntermediate,
                XRHandJointID.IndexDistal,
                XRHandJointID.IndexTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var middle = ComputeFingerCurlTripleWithTip(hand,
                XRHandJointID.MiddleMetacarpal,
                XRHandJointID.MiddleProximal,
                XRHandJointID.MiddleIntermediate,
                XRHandJointID.MiddleDistal,
                XRHandJointID.MiddleTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var ring = ComputeFingerCurlTripleWithTip(hand,
                XRHandJointID.RingMetacarpal,
                XRHandJointID.RingProximal,
                XRHandJointID.RingIntermediate,
                XRHandJointID.RingDistal,
                XRHandJointID.RingTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var little = ComputeFingerCurlTripleWithTip(hand,
                XRHandJointID.LittleMetacarpal,
                XRHandJointID.LittleProximal,
                XRHandJointID.LittleIntermediate,
                XRHandJointID.LittleDistal,
                XRHandJointID.LittleTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            if (isLeft)
            {
                SetMuscle(ref targetPose, leftThumb[0], thumb.x);
                SetMuscle(ref targetPose, leftThumb[1], thumb.y);
                SetMuscle(ref targetPose, leftThumb[2], thumb.z);

                SetMuscle(ref targetPose, leftIndex[0], index.x);
                SetMuscle(ref targetPose, leftIndex[1], index.y);
                SetMuscle(ref targetPose, leftIndex[2], index.z);

                SetMuscle(ref targetPose, leftMiddle[0], middle.x);
                SetMuscle(ref targetPose, leftMiddle[1], middle.y);
                SetMuscle(ref targetPose, leftMiddle[2], middle.z);

                SetMuscle(ref targetPose, leftRing[0], ring.x);
                SetMuscle(ref targetPose, leftRing[1], ring.y);
                SetMuscle(ref targetPose, leftRing[2], ring.z);

                SetMuscle(ref targetPose, leftLittle[0], little.x);
                SetMuscle(ref targetPose, leftLittle[1], little.y);
                SetMuscle(ref targetPose, leftLittle[2], little.z);
            }
            else
            {
                SetMuscle(ref targetPose, rightThumb[0], thumb.x);
                SetMuscle(ref targetPose, rightThumb[1], thumb.y);
                SetMuscle(ref targetPose, rightThumb[2], thumb.z);

                SetMuscle(ref targetPose, rightIndex[0], index.x);
                SetMuscle(ref targetPose, rightIndex[1], index.y);
                SetMuscle(ref targetPose, rightIndex[2], index.z);

                SetMuscle(ref targetPose, rightMiddle[0], middle.x);
                SetMuscle(ref targetPose, rightMiddle[1], middle.y);
                SetMuscle(ref targetPose, rightMiddle[2], middle.z);

                SetMuscle(ref targetPose, rightRing[0], ring.x);
                SetMuscle(ref targetPose, rightRing[1], ring.y);
                SetMuscle(ref targetPose, rightRing[2], ring.z);

                SetMuscle(ref targetPose, rightLittle[0], little.x);
                SetMuscle(ref targetPose, rightLittle[1], little.y);
                SetMuscle(ref targetPose, rightLittle[2], little.z);
            }
        }

        private Vector3 ComputeFingerCurlTriple(
            XRHand hand,
            XRHandJointID joint0,
            XRHandJointID joint1,
            XRHandJointID joint2,
            XRHandJointID joint3,
            float maxBend0,
            float maxBend1,
            float maxBend2)
        {
            if (!TryGetJointPosition(hand, joint0, out var p0) ||
                !TryGetJointPosition(hand, joint1, out var p1) ||
                !TryGetJointPosition(hand, joint2, out var p2) ||
                !TryGetJointPosition(hand, joint3, out var p3))
            {
                return Vector3.one;
            }

            float bend0 = ComputeJointBend(p0, p1, p2);
            float bend1 = ComputeJointBend(p1, p2, p3);
            float bend2 = bend1;

            return new Vector3(
                BendToMuscle(bend0, maxBend0),
                BendToMuscle(bend1, maxBend1),
                BendToMuscle(bend2, maxBend2));
        }

        private Vector3 ComputeFingerCurlTripleWithTip(
            XRHand hand,
            XRHandJointID joint0,
            XRHandJointID joint1,
            XRHandJointID joint2,
            XRHandJointID joint3,
            XRHandJointID joint4,
            float maxBend0,
            float maxBend1,
            float maxBend2)
        {
            if (!TryGetJointPosition(hand, joint0, out var p0) ||
                !TryGetJointPosition(hand, joint1, out var p1) ||
                !TryGetJointPosition(hand, joint2, out var p2) ||
                !TryGetJointPosition(hand, joint3, out var p3) ||
                !TryGetJointPosition(hand, joint4, out var p4))
            {
                return Vector3.one;
            }

            float bend0 = ComputeJointBend(p0, p1, p2);
            float bend1 = ComputeJointBend(p1, p2, p3);
            float bend2 = ComputeJointBend(p2, p3, p4);

            return new Vector3(
                BendToMuscle(bend0, maxBend0),
                BendToMuscle(bend1, maxBend1),
                BendToMuscle(bend2, maxBend2));
        }

        private void SetMuscle(ref HumanPose targetPose, int index, float value)
        {
            if (index < 0 || index >= HumanTrait.MuscleCount)
                return;

            float clamped = Mathf.Clamp(value, -1f, 1f);
            if (enableSmoothing)
            {
                float t = Mathf.Clamp01(muscleSmoothing * Time.deltaTime);
                targetPose.muscles[index] = Mathf.Lerp(targetPose.muscles[index], clamped, t);
            }
            else
            {
                targetPose.muscles[index] = clamped;
            }
        }

        private static bool TryGetJointPosition(XRHand hand, XRHandJointID jointID, out Vector3 pos)
        {
            var joint = hand.GetJoint(jointID);
            if (joint.TryGetPose(out var pose))
            {
                pos = pose.position;
                return true;
            }

            pos = default;
            return false;
        }

        private static float ComputeJointBend(Vector3 prev, Vector3 current, Vector3 next)
        {
            Vector3 a = (prev - current).normalized;
            Vector3 b = (next - current).normalized;
            if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f)
                return 0f;

            float angle = Vector3.Angle(a, b);
            return Mathf.Clamp(180f - angle, 0f, 180f);
        }

        private static float BendToMuscle(float bendDeg, float maxBendDeg)
        {
            if (maxBendDeg <= 0.001f)
                return 1f;

            float normalizedCurl = Mathf.Clamp01(bendDeg / maxBendDeg);
            return Mathf.Lerp(1f, -1f, normalizedCurl);
        }

        public void SetTrackingEnabled(bool enabled)
        {
            enableTracking = enabled;
        }

        public bool IsAnyHandTracked()
        {
            return leftHandTracked || rightHandTracked;
        }
    }
}
