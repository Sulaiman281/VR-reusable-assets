using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace WitShells.XR
{
    [Serializable]
    public class Joint
    {
        public Transform joinTrans;
        public Joint childJoint;
    }

    [Serializable]
    public class HandSkeleton
    {
        public Transform wrist;
        public Transform palm;
        public Joint thumb;
        public Joint index;
        public Joint middle;
        public Joint ring;
        public Joint pinky;

        public void AutoFillChildJoints()
        {
            // First, try to find missing finger roots and palm from wrist
            if (wrist != null)
            {
                if (palm == null)
                    palm = FindChildRecursive(wrist, "palm");
                if (thumb == null)
                    thumb = FindFingerFromWrist("thumb");
                if (index == null)
                    index = FindFingerFromWrist("index");
                if (middle == null)
                    middle = FindFingerFromWrist("middle");
                if (ring == null)
                    ring = FindFingerFromWrist("ring");
                if (pinky == null)
                    pinky = FindFingerFromWrist("pinky", "little");
            }

            // Then auto-fill child joints for each finger
            AutoFillJointRecursive(thumb);
            AutoFillJointRecursive(index);
            AutoFillJointRecursive(middle);
            AutoFillJointRecursive(ring);
            AutoFillJointRecursive(pinky);
        }

        private Joint FindFingerFromWrist(params string[] fingerNames)
        {
            if (wrist == null) return null;

            // Search through all children of wrist to find finger root
            foreach (string fingerName in fingerNames)
            {
                Transform fingerTransform = FindChildRecursive(wrist, fingerName.ToLower());
                if (fingerTransform != null)
                {
                    return new Joint { joinTrans = fingerTransform };
                }
            }

            return null;
        }

        private Transform FindChildRecursive(Transform parent, string searchName)
        {
            // First check direct children
            foreach (Transform child in parent)
            {
                string childName = child.name.ToLower();
                if (childName.Contains(searchName) ||
                    childName.Contains(searchName + "_") ||
                    childName.Contains(searchName + "1") ||
                    childName.Contains(searchName + "01") ||
                    childName.StartsWith(searchName))
                {
                    return child;
                }
            }

            // Then search recursively in children
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, searchName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void AutoFillJointRecursive(Joint joint)
        {
            if (joint == null || joint.joinTrans == null) return;

            if (joint.joinTrans.childCount > 0)
            {
                Transform childTransform = joint.joinTrans.GetChild(0);
                joint.childJoint = new Joint
                {
                    joinTrans = childTransform
                };
                AutoFillJointRecursive(joint.childJoint);
            }
        }
    }

    public class GenericHands : MonoBehaviour
    {
        [Header("Hand Skeletons")]
        public HandSkeleton leftHand;
        public HandSkeleton rightHand;

        [Header("Tracking Settings")]
        [SerializeField] private bool enableTracking = true;

        private XRHandSubsystem handSubsystem;
        static readonly List<XRHandSubsystem> s_SubsystemsReuse = new List<XRHandSubsystem>();

        private XRHandSkeletonDriver leftDriver;
        private XRHandSkeletonDriver rightDriver;
        private XRHandTrackingEvents leftEvents;
        private XRHandTrackingEvents rightEvents;

        // Public tracking state
        public bool leftHandTracked { get; private set; }
        public bool rightHandTracked { get; private set; }

        private void Start()
        {
            SetupHandDriver(leftHand, Handedness.Left, out leftDriver, out leftEvents);
            SetupHandDriver(rightHand, Handedness.Right, out rightDriver, out rightEvents);
            InitializeHandSubsystem();
        }

        private void SetupHandDriver(HandSkeleton skeleton, Handedness handedness, out XRHandSkeletonDriver driver, out XRHandTrackingEvents events)
        {
            driver = null;
            events = null;
            if (skeleton == null || skeleton.wrist == null) return;

            // 1. Add Tracking Events
            events = skeleton.wrist.gameObject.GetComponent<XRHandTrackingEvents>();
            if (events == null) events = skeleton.wrist.gameObject.AddComponent<XRHandTrackingEvents>();
            events.handedness = handedness;
            events.updateType = XRHandTrackingEvents.UpdateTypes.Dynamic;

            // 2. Add Skeleton Driver
            driver = skeleton.wrist.gameObject.GetComponent<XRHandSkeletonDriver>();
            if (driver == null) driver = skeleton.wrist.gameObject.AddComponent<XRHandSkeletonDriver>();
            
            driver.handTrackingEvents = events;
            driver.rootTransform = skeleton.wrist;

            // 3. Map our custom joints to Unity's official driver
            var refs = new List<JointToTransformReference>();
            
            if (skeleton.wrist != null) refs.Add(new JointToTransformReference { xrHandJointID = XRHandJointID.Wrist, jointTransform = skeleton.wrist });
            if (skeleton.palm != null) refs.Add(new JointToTransformReference { xrHandJointID = XRHandJointID.Palm, jointTransform = skeleton.palm });

            AddFingerToDriver(refs, skeleton.thumb, new[] { XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip });
            AddFingerToDriver(refs, skeleton.index, new[] { XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip });
            AddFingerToDriver(refs, skeleton.middle, new[] { XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip });
            AddFingerToDriver(refs, skeleton.ring, new[] { XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip });
            AddFingerToDriver(refs, skeleton.pinky, new[] { XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip });

            driver.jointTransformReferences = refs;
            
            // Let Unity calculate the complex rest-pose offsets automatically
            driver.InitializeFromSerializedReferences();
        }

        private void AddFingerToDriver(List<JointToTransformReference> refs, Joint fingerRoot, XRHandJointID[] jointIDs)
        {
            Joint currentJoint = fingerRoot;
            int index = 0;

            while (currentJoint != null && currentJoint.joinTrans != null && index < jointIDs.Length)
            {
                refs.Add(new JointToTransformReference 
                { 
                    xrHandJointID = jointIDs[index], 
                    jointTransform = currentJoint.joinTrans 
                });
                currentJoint = currentJoint.childJoint;
                index++;
            }
        }

        private void Update()
        {
            if (leftEvents != null) leftEvents.enabled = enableTracking;
            if (rightEvents != null) rightEvents.enabled = enableTracking;
            if (leftDriver != null) leftDriver.enabled = enableTracking;
            if (rightDriver != null) rightDriver.enabled = enableTracking;

            UpdateHandSubsystem();

            if (handSubsystem != null && handSubsystem.running)
            {
                leftHandTracked = handSubsystem.leftHand.isTracked;
                rightHandTracked = handSubsystem.rightHand.isTracked;
            }
        }

        private void InitializeHandSubsystem()
        {
            SubsystemManager.GetSubsystems(s_SubsystemsReuse);
            for (int i = 0; i < s_SubsystemsReuse.Count; i++)
            {
                if (s_SubsystemsReuse[i].running)
                {
                    handSubsystem = s_SubsystemsReuse[i];
                    break;
                }
            }
        }

        private void UpdateHandSubsystem()
        {
            if (handSubsystem != null && handSubsystem.running) return;

            SubsystemManager.GetSubsystems(s_SubsystemsReuse);
            for (int i = 0; i < s_SubsystemsReuse.Count; i++)
            {
                if (s_SubsystemsReuse[i].running)
                {
                    handSubsystem = s_SubsystemsReuse[i];
                    break;
                }
            }
        }

        private void OnValidate()
        {
            if (leftHand != null) leftHand.AutoFillChildJoints();
            if (rightHand != null) rightHand.AutoFillChildJoints();
        }

        // Public API
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