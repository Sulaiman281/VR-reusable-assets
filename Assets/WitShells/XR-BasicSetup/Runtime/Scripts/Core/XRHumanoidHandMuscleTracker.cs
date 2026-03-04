using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace WitShells.XR
{
    public class XRHumanoidHandMuscleTracker : MonoBehaviour
    {
        [Header("Avatar")]
        [SerializeField] private Animator animator;

        [Header("Tracking")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool trackLeftHand = true;
        [SerializeField] private bool trackRightHand = true;
        [SerializeField] private bool trackFingerSpread = false;
        [SerializeField] private bool trackWristMuscles = false;
        [SerializeField] private bool preserveAnimatorTransform = true;
        [SerializeField] private bool lockHumanoidBodyPose = true;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float muscleSmoothing = 18f;

        [Header("Finger Mapping")]
        [SerializeField] private float proximalMaxBend = 95f;
        [SerializeField] private float intermediateMaxBend = 100f;
        [SerializeField] private float distalMaxBend = 85f;
        [SerializeField] private float spreadMaxAngle = 22f;

        [Header("Spread Tuning")]
        [SerializeField] private float leftSpreadSign = 1f;
        [SerializeField] private float rightSpreadSign = 1f;

        private XRHandSubsystem handSubsystem;
        private HumanPoseHandler poseHandler;
        private HumanPose pose;
        private Vector3 lockedBodyPosition;
        private Quaternion lockedBodyRotation;
        private bool hasLockedBodyPose;

        private static readonly List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();

        public bool leftHandTracked { get; private set; }
        public bool rightHandTracked { get; private set; }

        private const int LeftWristDownUp = 44;
        private const int LeftWristInOut = 45;
        private const int RightWristDownUp = 53;
        private const int RightWristInOut = 54;

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

        private const int LeftThumbSpread = 56;
        private const int LeftIndexSpread = 60;
        private const int LeftMiddleSpread = 64;
        private const int LeftRingSpread = 68;
        private const int LeftLittleSpread = 72;

        private const int RightThumbSpread = 76;
        private const int RightIndexSpread = 80;
        private const int RightMiddleSpread = 84;
        private const int RightRingSpread = 88;
        private const int RightLittleSpread = 92;

        private HashSet<int> allowedMuscles;

        private void OnEnable()
        {
            BuildAllowedMuscleSet();
            EnsurePoseHandler();
            InitializeHandSubsystem();
            CaptureLockedBodyPose();
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

            if (trackLeftHand && leftHandTracked)
                ApplyHandToPose(leftHand, true, ref pose);

            if (trackRightHand && rightHandTracked)
                ApplyHandToPose(rightHand, false, ref pose);

            if (lockHumanoidBodyPose && hasLockedBodyPose)
            {
                pose.bodyPosition = lockedBodyPosition;
                pose.bodyRotation = lockedBodyRotation;
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
                Debug.LogWarning($"[{nameof(XRHumanoidHandMuscleTracker)}] Animator must use a valid Humanoid avatar.");
                return;
            }

            poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            pose = new HumanPose();
            CaptureLockedBodyPose();
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

        private void ApplyHandToPose(XRHand hand, bool isLeft, ref HumanPose targetPose)
        {
            var thumb = ComputeFingerCurlTriple(hand,
                XRHandJointID.ThumbMetacarpal,
                XRHandJointID.ThumbProximal,
                XRHandJointID.ThumbDistal,
                XRHandJointID.ThumbTip,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var index = ComputeFingerCurlTriple(hand,
                XRHandJointID.IndexMetacarpal,
                XRHandJointID.IndexProximal,
                XRHandJointID.IndexIntermediate,
                XRHandJointID.IndexDistal,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var middle = ComputeFingerCurlTriple(hand,
                XRHandJointID.MiddleMetacarpal,
                XRHandJointID.MiddleProximal,
                XRHandJointID.MiddleIntermediate,
                XRHandJointID.MiddleDistal,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var ring = ComputeFingerCurlTriple(hand,
                XRHandJointID.RingMetacarpal,
                XRHandJointID.RingProximal,
                XRHandJointID.RingIntermediate,
                XRHandJointID.RingDistal,
                proximalMaxBend,
                intermediateMaxBend,
                distalMaxBend);

            var little = ComputeFingerCurlTriple(hand,
                XRHandJointID.LittleMetacarpal,
                XRHandJointID.LittleProximal,
                XRHandJointID.LittleIntermediate,
                XRHandJointID.LittleDistal,
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

            if (trackFingerSpread)
            {
                ApplyFingerSpread(hand, isLeft, ref targetPose);
            }

            if (trackWristMuscles)
            {
                ApplyWristMuscles(hand, isLeft, ref targetPose);
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

        private void ApplyFingerSpread(XRHand hand, bool isLeft, ref HumanPose targetPose)
        {
            if (!TryGetJointPosition(hand, XRHandJointID.Wrist, out var wrist) ||
                !TryGetJointPosition(hand, XRHandJointID.IndexMetacarpal, out var idxMeta) ||
                !TryGetJointPosition(hand, XRHandJointID.MiddleMetacarpal, out var midMeta) ||
                !TryGetJointPosition(hand, XRHandJointID.RingMetacarpal, out var ringMeta) ||
                !TryGetJointPosition(hand, XRHandJointID.LittleMetacarpal, out var littleMeta) ||
                !TryGetJointPosition(hand, XRHandJointID.IndexProximal, out var idxProx) ||
                !TryGetJointPosition(hand, XRHandJointID.MiddleProximal, out var midProx) ||
                !TryGetJointPosition(hand, XRHandJointID.RingProximal, out var ringProx) ||
                !TryGetJointPosition(hand, XRHandJointID.LittleProximal, out var littleProx) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbProximal, out var thumbProx) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbMetacarpal, out var thumbMeta))
            {
                return;
            }

            var palmNormal = Vector3.Cross(idxMeta - wrist, littleMeta - wrist).normalized;
            if (palmNormal.sqrMagnitude < 0.0001f)
                return;

            float sign = isLeft ? leftSpreadSign : rightSpreadSign;

            float thumbSpread = SignedSpread(thumbMeta, thumbProx, midMeta, midProx, palmNormal, sign);
            float indexSpread = SignedSpread(idxMeta, idxProx, midMeta, midProx, palmNormal, sign);
            float middleSpread = 0f;
            float ringSpread = SignedSpread(ringMeta, ringProx, midMeta, midProx, palmNormal, sign);
            float littleSpread = SignedSpread(littleMeta, littleProx, midMeta, midProx, palmNormal, sign);

            if (isLeft)
            {
                SetMuscle(ref targetPose, LeftThumbSpread, thumbSpread);
                SetMuscle(ref targetPose, LeftIndexSpread, indexSpread);
                SetMuscle(ref targetPose, LeftMiddleSpread, middleSpread);
                SetMuscle(ref targetPose, LeftRingSpread, ringSpread);
                SetMuscle(ref targetPose, LeftLittleSpread, littleSpread);
            }
            else
            {
                SetMuscle(ref targetPose, RightThumbSpread, thumbSpread);
                SetMuscle(ref targetPose, RightIndexSpread, indexSpread);
                SetMuscle(ref targetPose, RightMiddleSpread, middleSpread);
                SetMuscle(ref targetPose, RightRingSpread, ringSpread);
                SetMuscle(ref targetPose, RightLittleSpread, littleSpread);
            }
        }

        private void ApplyWristMuscles(XRHand hand, bool isLeft, ref HumanPose targetPose)
        {
            var wristJoint = hand.GetJoint(XRHandJointID.Wrist);
            var palmJoint = hand.GetJoint(XRHandJointID.Palm);
            if (!wristJoint.TryGetPose(out var wristPose) || !palmJoint.TryGetPose(out var palmPose))
                return;

            Quaternion localPalm = Quaternion.Inverse(wristPose.rotation) * palmPose.rotation;
            Vector3 euler = NormalizeEuler(localPalm.eulerAngles);

            float downUp = Mathf.Clamp(euler.x / 45f, -1f, 1f);
            float inOut = Mathf.Clamp(euler.z / 35f, -1f, 1f);

            if (isLeft)
            {
                SetMuscle(ref targetPose, LeftWristDownUp, downUp);
                SetMuscle(ref targetPose, LeftWristInOut, inOut);
            }
            else
            {
                SetMuscle(ref targetPose, RightWristDownUp, downUp);
                SetMuscle(ref targetPose, RightWristInOut, inOut);
            }
        }

        private void SetMuscle(ref HumanPose targetPose, int index, float value)
        {
            if (index < 0 || index >= HumanTrait.MuscleCount)
                return;

            if (allowedMuscles == null || !allowedMuscles.Contains(index))
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

        private float SignedSpread(
            Vector3 fingerBase,
            Vector3 fingerNext,
            Vector3 middleBase,
            Vector3 middleNext,
            Vector3 palmNormal,
            float sign)
        {
            Vector3 fingerDir = (fingerNext - fingerBase).normalized;
            Vector3 middleDir = (middleNext - middleBase).normalized;

            if (fingerDir.sqrMagnitude < 0.0001f || middleDir.sqrMagnitude < 0.0001f)
                return 0f;

            float angle = Vector3.SignedAngle(middleDir, fingerDir, palmNormal) * sign;
            float normalized = Mathf.Clamp(angle / Mathf.Max(0.1f, spreadMaxAngle), -1f, 1f);
            return Mathf.Clamp(normalized, -1f, 1f);
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);
            euler.z = NormalizeAngle(euler.z);
            return euler;
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        public void SetTrackingEnabled(bool enabled)
        {
            enableTracking = enabled;
        }

        public bool IsAnyHandTracked()
        {
            return leftHandTracked || rightHandTracked;
        }

        private void BuildAllowedMuscleSet()
        {
            allowedMuscles = new HashSet<int>
            {
                leftThumb[0], leftThumb[1], leftThumb[2],
                leftIndex[0], leftIndex[1], leftIndex[2],
                leftMiddle[0], leftMiddle[1], leftMiddle[2],
                leftRing[0], leftRing[1], leftRing[2],
                leftLittle[0], leftLittle[1], leftLittle[2],
                rightThumb[0], rightThumb[1], rightThumb[2],
                rightIndex[0], rightIndex[1], rightIndex[2],
                rightMiddle[0], rightMiddle[1], rightMiddle[2],
                rightRing[0], rightRing[1], rightRing[2],
                rightLittle[0], rightLittle[1], rightLittle[2],
                LeftThumbSpread, LeftIndexSpread, LeftMiddleSpread, LeftRingSpread, LeftLittleSpread,
                RightThumbSpread, RightIndexSpread, RightMiddleSpread, RightRingSpread, RightLittleSpread
            };

            if (trackWristMuscles)
            {
                allowedMuscles.Add(LeftWristDownUp);
                allowedMuscles.Add(LeftWristInOut);
                allowedMuscles.Add(RightWristDownUp);
                allowedMuscles.Add(RightWristInOut);
            }
        }

        private void OnValidate()
        {
            BuildAllowedMuscleSet();
        }

        private void CaptureLockedBodyPose()
        {
            if (poseHandler == null)
                return;

            poseHandler.GetHumanPose(ref pose);
            lockedBodyPosition = pose.bodyPosition;
            lockedBodyRotation = pose.bodyRotation;
            hasLockedBodyPose = true;
        }

        public void RecalibrateBodyLock()
        {
            CaptureLockedBodyPose();
        }
    }
}
