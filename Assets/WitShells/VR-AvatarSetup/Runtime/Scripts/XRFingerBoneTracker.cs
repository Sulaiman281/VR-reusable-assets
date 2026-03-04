using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace WitShells.VRAvatarSetup
{
    [DefaultExecutionOrder(11000)]
    public class XRFingerBoneTracker : MonoBehaviour
    {
        private enum TrackingMode
        {
            JointRotation,
            CurlOnly
        }

        [Serializable]
        private class FingerBoneBinding
        {
            public HumanBodyBones bone;
            public XRHandJointID joint;
            public XRHandJointID parentJoint;
            [NonSerialized] public Transform boneTransform;
            [NonSerialized] public Quaternion localRotationOffset;
            [NonSerialized] public Quaternion initialLocalRotation;
            [NonSerialized] public bool calibrated;
        }

        [Header("Avatar")]
        [SerializeField] private Animator animator;

        [Header("Tracking")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool trackLeftHand = true;
        [SerializeField] private bool trackRightHand = true;
        [SerializeField] private TrackingMode trackingMode = TrackingMode.CurlOnly;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float rotationSmoothing = 20f;

        [Header("Curl Only")]
        [SerializeField] private Vector3 leftCurlAxisLocal = Vector3.right;
        [SerializeField] private Vector3 rightCurlAxisLocal = Vector3.right;
        [SerializeField] private float leftCurlSign = -1f;
        [SerializeField] private float rightCurlSign = 1f;
        [SerializeField] private float curlStrength = 1f;

        [Header("Debug")]
        [SerializeField] private bool autoRecalibrateOnTrackingAcquire = true;

        private XRHandSubsystem handSubsystem;
        private static readonly List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();

        public bool leftHandTracked { get; private set; }
        public bool rightHandTracked { get; private set; }

        private readonly FingerBoneBinding[] leftBindings =
        {
            new FingerBoneBinding { bone = HumanBodyBones.LeftThumbProximal, joint = XRHandJointID.ThumbProximal, parentJoint = XRHandJointID.ThumbMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftThumbIntermediate, joint = XRHandJointID.ThumbDistal, parentJoint = XRHandJointID.ThumbProximal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftThumbDistal, joint = XRHandJointID.ThumbTip, parentJoint = XRHandJointID.ThumbDistal },

            new FingerBoneBinding { bone = HumanBodyBones.LeftIndexProximal, joint = XRHandJointID.IndexProximal, parentJoint = XRHandJointID.IndexMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftIndexIntermediate, joint = XRHandJointID.IndexIntermediate, parentJoint = XRHandJointID.IndexProximal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftIndexDistal, joint = XRHandJointID.IndexDistal, parentJoint = XRHandJointID.IndexIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.LeftMiddleProximal, joint = XRHandJointID.MiddleProximal, parentJoint = XRHandJointID.MiddleMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftMiddleIntermediate, joint = XRHandJointID.MiddleIntermediate, parentJoint = XRHandJointID.MiddleProximal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftMiddleDistal, joint = XRHandJointID.MiddleDistal, parentJoint = XRHandJointID.MiddleIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.LeftRingProximal, joint = XRHandJointID.RingProximal, parentJoint = XRHandJointID.RingMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftRingIntermediate, joint = XRHandJointID.RingIntermediate, parentJoint = XRHandJointID.RingProximal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftRingDistal, joint = XRHandJointID.RingDistal, parentJoint = XRHandJointID.RingIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.LeftLittleProximal, joint = XRHandJointID.LittleProximal, parentJoint = XRHandJointID.LittleMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftLittleIntermediate, joint = XRHandJointID.LittleIntermediate, parentJoint = XRHandJointID.LittleProximal },
            new FingerBoneBinding { bone = HumanBodyBones.LeftLittleDistal, joint = XRHandJointID.LittleDistal, parentJoint = XRHandJointID.LittleIntermediate },
        };

        private readonly FingerBoneBinding[] rightBindings =
        {
            new FingerBoneBinding { bone = HumanBodyBones.RightThumbProximal, joint = XRHandJointID.ThumbProximal, parentJoint = XRHandJointID.ThumbMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.RightThumbIntermediate, joint = XRHandJointID.ThumbDistal, parentJoint = XRHandJointID.ThumbProximal },
            new FingerBoneBinding { bone = HumanBodyBones.RightThumbDistal, joint = XRHandJointID.ThumbTip, parentJoint = XRHandJointID.ThumbDistal },

            new FingerBoneBinding { bone = HumanBodyBones.RightIndexProximal, joint = XRHandJointID.IndexProximal, parentJoint = XRHandJointID.IndexMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.RightIndexIntermediate, joint = XRHandJointID.IndexIntermediate, parentJoint = XRHandJointID.IndexProximal },
            new FingerBoneBinding { bone = HumanBodyBones.RightIndexDistal, joint = XRHandJointID.IndexDistal, parentJoint = XRHandJointID.IndexIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.RightMiddleProximal, joint = XRHandJointID.MiddleProximal, parentJoint = XRHandJointID.MiddleMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.RightMiddleIntermediate, joint = XRHandJointID.MiddleIntermediate, parentJoint = XRHandJointID.MiddleProximal },
            new FingerBoneBinding { bone = HumanBodyBones.RightMiddleDistal, joint = XRHandJointID.MiddleDistal, parentJoint = XRHandJointID.MiddleIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.RightRingProximal, joint = XRHandJointID.RingProximal, parentJoint = XRHandJointID.RingMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.RightRingIntermediate, joint = XRHandJointID.RingIntermediate, parentJoint = XRHandJointID.RingProximal },
            new FingerBoneBinding { bone = HumanBodyBones.RightRingDistal, joint = XRHandJointID.RingDistal, parentJoint = XRHandJointID.RingIntermediate },

            new FingerBoneBinding { bone = HumanBodyBones.RightLittleProximal, joint = XRHandJointID.LittleProximal, parentJoint = XRHandJointID.LittleMetacarpal },
            new FingerBoneBinding { bone = HumanBodyBones.RightLittleIntermediate, joint = XRHandJointID.LittleIntermediate, parentJoint = XRHandJointID.LittleProximal },
            new FingerBoneBinding { bone = HumanBodyBones.RightLittleDistal, joint = XRHandJointID.LittleDistal, parentJoint = XRHandJointID.LittleIntermediate },
        };

        private void OnEnable()
        {
            CacheBoneTransforms();
            InitializeHandSubsystem();
        }

        private void LateUpdate()
        {
            if (!enableTracking)
                return;

            UpdateHandSubsystem();
            if (handSubsystem == null || !handSubsystem.running)
                return;

            var left = handSubsystem.leftHand;
            var right = handSubsystem.rightHand;

            bool wasLeftTracked = leftHandTracked;
            bool wasRightTracked = rightHandTracked;

            leftHandTracked = left.isTracked;
            rightHandTracked = right.isTracked;

            if (trackLeftHand && leftHandTracked)
            {
                if (trackingMode == TrackingMode.CurlOnly)
                {
                    ApplyCurlOnly(left, leftBindings, leftCurlAxisLocal, leftCurlSign);
                }
                else
                {
                    if (autoRecalibrateOnTrackingAcquire && !wasLeftTracked)
                        CalibrateBindings(leftBindings, left);

                    ApplyBindings(leftBindings, left);
                }
            }

            if (trackRightHand && rightHandTracked)
            {
                if (trackingMode == TrackingMode.CurlOnly)
                {
                    ApplyCurlOnly(right, rightBindings, rightCurlAxisLocal, rightCurlSign);
                }
                else
                {
                    if (autoRecalibrateOnTrackingAcquire && !wasRightTracked)
                        CalibrateBindings(rightBindings, right);

                    ApplyBindings(rightBindings, right);
                }
            }
        }

        private void CacheBoneTransforms()
        {
            if (animator == null || !animator.isHuman)
                return;

            CacheBindingSet(leftBindings);
            CacheBindingSet(rightBindings);
        }

        private void CacheBindingSet(FingerBoneBinding[] bindings)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i].boneTransform = animator.GetBoneTransform(bindings[i].bone);
                bindings[i].calibrated = false;
                bindings[i].localRotationOffset = Quaternion.identity;
                if (bindings[i].boneTransform != null)
                    bindings[i].initialLocalRotation = bindings[i].boneTransform.localRotation;
            }
        }

        private void CalibrateBindings(FingerBoneBinding[] bindings, XRHand xrHand)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.boneTransform == null)
                    continue;

                var xrJoint = xrHand.GetJoint(binding.joint);
                if (!xrJoint.TryGetPose(out var jointPose))
                    continue;

                Quaternion xrLocalRotation = GetXRLocalRotation(xrHand, binding.parentJoint, jointPose.rotation);
                binding.localRotationOffset = binding.boneTransform.localRotation * Quaternion.Inverse(xrLocalRotation);
                binding.calibrated = true;
            }
        }

        private void ApplyBindings(FingerBoneBinding[] bindings, XRHand xrHand)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding.boneTransform == null)
                    continue;

                var xrJoint = xrHand.GetJoint(binding.joint);
                if (!xrJoint.TryGetPose(out var jointPose))
                    continue;

                if (!binding.calibrated)
                {
                    Quaternion xrLocalRotation = GetXRLocalRotation(xrHand, binding.parentJoint, jointPose.rotation);
                    binding.localRotationOffset = binding.boneTransform.localRotation * Quaternion.Inverse(xrLocalRotation);
                    binding.calibrated = true;
                }

                Quaternion targetLocalRotation = GetXRLocalRotation(xrHand, binding.parentJoint, jointPose.rotation) * binding.localRotationOffset;
                if (enableSmoothing)
                {
                    float t = Mathf.Clamp01(rotationSmoothing * Time.deltaTime);
                    binding.boneTransform.localRotation = Quaternion.Slerp(binding.boneTransform.localRotation, targetLocalRotation, t);
                }
                else
                {
                    binding.boneTransform.localRotation = targetLocalRotation;
                }
            }
        }

        private static Quaternion GetXRLocalRotation(XRHand hand, XRHandJointID parentJointId, Quaternion jointWorldRotation)
        {
            if (parentJointId == XRHandJointID.Invalid)
                return jointWorldRotation;

            var parentJoint = hand.GetJoint(parentJointId);
            if (!parentJoint.TryGetPose(out var parentPose))
                return jointWorldRotation;

            return Quaternion.Inverse(parentPose.rotation) * jointWorldRotation;
        }

        private void ApplyCurlOnly(XRHand hand, FingerBoneBinding[] bindings, Vector3 axisLocal, float handSign)
        {
            Vector3 normalizedAxis = axisLocal.sqrMagnitude > 0.0001f ? axisLocal.normalized : Vector3.right;

            ApplyThumbCurl(hand, bindings, normalizedAxis, handSign);
            ApplyFingerCurl(hand, bindings, 3, XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip, normalizedAxis, handSign);
            ApplyFingerCurl(hand, bindings, 6, XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, normalizedAxis, handSign);
            ApplyFingerCurl(hand, bindings, 9, XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip, normalizedAxis, handSign);
            ApplyFingerCurl(hand, bindings, 12, XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip, normalizedAxis, handSign);
        }

        private void ApplyThumbCurl(XRHand hand, FingerBoneBinding[] bindings, Vector3 axisLocal, float handSign)
        {
            if (!TryGetJointPosition(hand, XRHandJointID.ThumbMetacarpal, out var p0) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbProximal, out var p1) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbDistal, out var p2) ||
                !TryGetJointPosition(hand, XRHandJointID.ThumbTip, out var p3))
            {
                return;
            }

            float bend0 = ComputeJointBend(p0, p1, p2) * curlStrength;
            float bend1 = ComputeJointBend(p1, p2, p3) * curlStrength;
            float bend2 = bend1;

            ApplyCurlToBinding(bindings[0], bend0, axisLocal, handSign);
            ApplyCurlToBinding(bindings[1], bend1, axisLocal, handSign);
            ApplyCurlToBinding(bindings[2], bend2, axisLocal, handSign);
        }

        private void ApplyFingerCurl(
            XRHand hand,
            FingerBoneBinding[] bindings,
            int startIndex,
            XRHandJointID j0,
            XRHandJointID j1,
            XRHandJointID j2,
            XRHandJointID j3,
            XRHandJointID j4,
            Vector3 axisLocal,
            float handSign)
        {
            if (!TryGetJointPosition(hand, j0, out var p0) ||
                !TryGetJointPosition(hand, j1, out var p1) ||
                !TryGetJointPosition(hand, j2, out var p2) ||
                !TryGetJointPosition(hand, j3, out var p3) ||
                !TryGetJointPosition(hand, j4, out var p4))
            {
                return;
            }

            float bend0 = ComputeJointBend(p0, p1, p2) * curlStrength;
            float bend1 = ComputeJointBend(p1, p2, p3) * curlStrength;
            float bend2 = ComputeJointBend(p2, p3, p4) * curlStrength;

            ApplyCurlToBinding(bindings[startIndex], bend0, axisLocal, handSign);
            ApplyCurlToBinding(bindings[startIndex + 1], bend1, axisLocal, handSign);
            ApplyCurlToBinding(bindings[startIndex + 2], bend2, axisLocal, handSign);
        }

        private void ApplyCurlToBinding(FingerBoneBinding binding, float bendDegrees, Vector3 axisLocal, float handSign)
        {
            if (binding.boneTransform == null)
                return;

            Quaternion targetLocal = binding.initialLocalRotation * Quaternion.AngleAxis(bendDegrees * handSign, axisLocal);
            if (enableSmoothing)
            {
                float t = Mathf.Clamp01(rotationSmoothing * Time.deltaTime);
                binding.boneTransform.localRotation = Quaternion.Slerp(binding.boneTransform.localRotation, targetLocal, t);
            }
            else
            {
                binding.boneTransform.localRotation = targetLocal;
            }
        }

        private static bool TryGetJointPosition(XRHand hand, XRHandJointID jointId, out Vector3 position)
        {
            var joint = hand.GetJoint(jointId);
            if (joint.TryGetPose(out var pose))
            {
                position = pose.position;
                return true;
            }

            position = default;
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

        private void InitializeHandSubsystem()
        {
            SubsystemManager.GetSubsystems(s_SubsystemsReuse);
            for (int i = 0; i < s_SubsystemsReuse.Count; i++)
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

        public void Recalibrate()
        {
            if (handSubsystem == null || !handSubsystem.running)
                return;

            if (trackLeftHand && handSubsystem.leftHand.isTracked)
                CalibrateBindings(leftBindings, handSubsystem.leftHand);

            if (trackRightHand && handSubsystem.rightHand.isTracked)
                CalibrateBindings(rightBindings, handSubsystem.rightHand);
        }
    }
}
