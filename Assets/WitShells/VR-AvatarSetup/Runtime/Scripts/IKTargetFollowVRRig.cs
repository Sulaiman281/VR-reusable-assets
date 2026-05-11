using System;
using UnityEngine;

namespace WitShells.VRAvatarSetup
{
    [Serializable]
    public class VRMap
    {
        public Transform vrTarget;
        public Transform ikTarget;
        public Vector3 trackingPositionOffset;
        public Vector3 trackingRotationOffset;

        public bool IsValid => vrTarget != null && ikTarget != null;

        public void Map()
        {
            if (!IsValid)
                return;

            ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
            ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
        }

        public void Map(Pose pose)
        {
            if (ikTarget == null)
                return;

            ikTarget.position = pose.position + pose.rotation * trackingPositionOffset;
            ikTarget.rotation = pose.rotation * Quaternion.Euler(trackingRotationOffset);
        }

        public void AutoComputeOffsetsFromCurrentPose()
        {
            if (!IsValid)
                return;

            trackingPositionOffset = vrTarget.InverseTransformPoint(ikTarget.position);
            trackingRotationOffset = (Quaternion.Inverse(vrTarget.rotation) * ikTarget.rotation).eulerAngles;
        }
    }

    public class IKTargetFollowVRRig : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] private float turnSmoothness = 0.1f;

        [SerializeField]
        private VRMap head = new VRMap
        {
            trackingPositionOffset = Vector3.zero,
            trackingRotationOffset = Vector3.zero
        };
        [SerializeField]
        private VRMap leftHand = new VRMap
        {
            trackingPositionOffset = new Vector3(-0.04f, -0.02f, -0.1f),
            trackingRotationOffset = new Vector3(11.5f, 87.3f, 105.8f)
        };
        [SerializeField]
        private VRMap rightHand = new VRMap
        {
            trackingPositionOffset = new Vector3(0.04f, -0.02f, -0.1f),
            trackingRotationOffset = new Vector3(11.5f, -87.3f, -105.8f)
        };

        [SerializeField] private Vector3 headBodyPositionOffset = new Vector3(0f, -0.61f, 0f);

        public VRMap Head => head;
        public VRMap LeftHand => leftHand;
        public VRMap RightHand => rightHand;

        private void LateUpdate()
        {
            if (!head.IsValid)
                return;

            transform.position = head.ikTarget.position + headBodyPositionOffset;
            var targetYaw = Quaternion.Euler(0f, head.vrTarget.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetYaw, turnSmoothness);

            head.Map();
            leftHand.Map();
            rightHand.Map();
        }

        public void AutoComputeOffsets()
        {
            head.AutoComputeOffsetsFromCurrentPose();
            leftHand.AutoComputeOffsetsFromCurrentPose();
            rightHand.AutoComputeOffsetsFromCurrentPose();
        }

        public void Configure(VRMap headMap, VRMap leftHandMap, VRMap rightHandMap, bool computeOffsets)
        {
            if (headMap != null)
                head = headMap;
            if (leftHandMap != null)
                leftHand = leftHandMap;
            if (rightHandMap != null)
                rightHand = rightHandMap;

            if (computeOffsets)
                AutoComputeOffsets();
        }

        public void ResetOffsetsToDefaults()
        {
            head.trackingPositionOffset = Vector3.zero;
            head.trackingRotationOffset = Vector3.zero;

            leftHand.trackingPositionOffset = new Vector3(-0.04f, -0.02f, -0.1f);
            leftHand.trackingRotationOffset = new Vector3(11.5f, 87.3f, 105.8f);

            rightHand.trackingPositionOffset = new Vector3(0.04f, -0.02f, -0.1f);
            rightHand.trackingRotationOffset = new Vector3(11.5f, -87.3f, -105.8f);

            headBodyPositionOffset = new Vector3(0f, -0.61f, 0f);
        }
    }
}
