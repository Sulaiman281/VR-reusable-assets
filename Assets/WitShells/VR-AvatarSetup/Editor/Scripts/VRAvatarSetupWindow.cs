using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using WitShells.VRAvatarSetup;

namespace WitShells.VRAvatarSetup.Editor
{
    public class VRAvatarSetupWindow : EditorWindow
    {
        private enum MapKind
        {
            Head,
            LeftHand,
            RightHand
        }

        private GameObject avatarRoot;

        private Transform vrHead;
        private Transform vrLeftHand;
        private Transform vrRightHand;

        private Transform ikHead;
        private Transform ikLeftHand;
        private Transform ikRightHand;
        private Transform ikLeftFoot;
        private Transform ikRightFoot;

        private bool autoCreateMissingTargets = true;
        private bool autoComputeOffsets = true;

        [MenuItem("WitShells/VR Avatar Setup")]
        public static void Open()
        {
            var window = GetWindow<VRAvatarSetupWindow>("VR Avatar Setup");
            window.minSize = new Vector2(460f, 620f);
            window.TryAssignSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);
            avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar Root", avatarRoot, typeof(GameObject), true);

            if (GUILayout.Button("Use Current Selection"))
                TryAssignSelection();

            DrawValidation();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("VR References", EditorStyles.boldLabel);
            vrHead = (Transform)EditorGUILayout.ObjectField("VR Head", vrHead, typeof(Transform), true);
            vrLeftHand = (Transform)EditorGUILayout.ObjectField("VR Left Hand", vrLeftHand, typeof(Transform), true);
            vrRightHand = (Transform)EditorGUILayout.ObjectField("VR Right Hand", vrRightHand, typeof(Transform), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("IK Targets", EditorStyles.boldLabel);
            ikHead = (Transform)EditorGUILayout.ObjectField("IK Head Target", ikHead, typeof(Transform), true);
            ikLeftHand = (Transform)EditorGUILayout.ObjectField("IK Left Hand Target", ikLeftHand, typeof(Transform), true);
            ikRightHand = (Transform)EditorGUILayout.ObjectField("IK Right Hand Target", ikRightHand, typeof(Transform), true);
            ikLeftFoot = (Transform)EditorGUILayout.ObjectField("IK Left Foot Target", ikLeftFoot, typeof(Transform), true);
            ikRightFoot = (Transform)EditorGUILayout.ObjectField("IK Right Foot Target", ikRightFoot, typeof(Transform), true);

            EditorGUILayout.Space(4f);
            autoCreateMissingTargets = EditorGUILayout.ToggleLeft("Auto-create missing IK targets", autoCreateMissingTargets);
            autoComputeOffsets = EditorGUILayout.ToggleLeft("Auto-compute VR -> IK offsets when possible", autoComputeOffsets);

            using (new EditorGUI.DisabledScope(avatarRoot == null))
            {
                if (GUILayout.Button("Reset Offsets To Defaults"))
                    ResetOffsetsOnAvatar();
            }

            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(!CanSetupAvatar()))
            {
                if (GUILayout.Button("Setup Selected Avatar", GUILayout.Height(36f)))
                    SetupAvatar();
            }

            if (!CanSetupAvatar())
            {
                EditorGUILayout.HelpBox("Setup requires: a selected Avatar Root with Humanoid Animator and at least one Rig component in hierarchy.", MessageType.Info);
            }
        }

        private void DrawValidation()
        {
            if (avatarRoot == null)
            {
                EditorGUILayout.HelpBox("Select an avatar root object to begin setup.", MessageType.Warning);
                return;
            }

            var animator = avatarRoot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                EditorGUILayout.HelpBox("No Animator found. Add an Animator with a Humanoid avatar.", MessageType.Error);
                return;
            }

            if (!animator.isHuman)
            {
                EditorGUILayout.HelpBox("Animator is not Humanoid. Retarget avatar as Humanoid before setup.", MessageType.Error);
                return;
            }

            if (!HasRigComponent(avatarRoot))
            {
                EditorGUILayout.HelpBox("No Rig component found in avatar hierarchy. Add a Rig (Animation Rigging) before setup.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Humanoid + Rig requirements satisfied.", MessageType.Info);
            }
        }

        private bool CanSetupAvatar()
        {
            if (avatarRoot == null)
                return false;

            var animator = avatarRoot.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
                return false;

            return HasRigComponent(avatarRoot);
        }

        private void TryAssignSelection()
        {
            if (Selection.activeGameObject == null)
                return;

            avatarRoot = Selection.activeGameObject;
        }

        private bool HasRigComponent(GameObject root)
        {
            return root.GetComponentInChildren<Rig>(true) != null;
        }

        private void SetupAvatar()
        {
            var animator = avatarRoot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[VRAvatarSetup] Animator not found.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            var targetRoot = animator.transform;
            Undo.RegisterFullObjectHierarchyUndo(targetRoot.gameObject, "Setup VR Avatar");

            EnsureTargets(targetRoot);
            SetupFollowComponent(targetRoot);
            SetupFootSolvers(targetRoot);

            EditorUtility.SetDirty(targetRoot);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[VRAvatarSetup] Avatar setup complete. If some references were missing, assign them manually in the generated components.");
            Selection.activeObject = targetRoot.gameObject;
        }

        private void EnsureTargets(Transform targetRoot)
        {
            if (!autoCreateMissingTargets)
                return;

            var created = EnsureTargetContainer(targetRoot);

            if (ikHead == null)
                ikHead = CreateTarget(created, "IK_Head", targetRoot.position + Vector3.up * 1.6f);
            if (ikLeftHand == null)
                ikLeftHand = CreateTarget(created, "IK_LeftHand", targetRoot.position + targetRoot.right * -0.25f + Vector3.up * 1.2f);
            if (ikRightHand == null)
                ikRightHand = CreateTarget(created, "IK_RightHand", targetRoot.position + targetRoot.right * 0.25f + Vector3.up * 1.2f);
            if (ikLeftFoot == null)
                ikLeftFoot = CreateTarget(created, "IK_LeftFoot", targetRoot.position + targetRoot.right * -0.1f);
            if (ikRightFoot == null)
                ikRightFoot = CreateTarget(created, "IK_RightFoot", targetRoot.position + targetRoot.right * 0.1f);
        }

        private Transform EnsureTargetContainer(Transform targetRoot)
        {
            var existing = targetRoot.Find("IK_Targets");
            if (existing != null)
                return existing;

            var go = new GameObject("IK_Targets");
            Undo.RegisterCreatedObjectUndo(go, "Create IK_Targets");
            go.transform.SetParent(targetRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private Transform CreateTarget(Transform parent, string name, Vector3 worldPosition)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create IK Target");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            return go.transform;
        }

        private void SetupFollowComponent(Transform targetRoot)
        {
            var follow = targetRoot.GetComponent<IKTargetFollowVRRig>();
            if (follow == null)
                follow = Undo.AddComponent<IKTargetFollowVRRig>(targetRoot.gameObject);

            follow.Configure(
                BuildMap(vrHead, ikHead, MapKind.Head),
                BuildMap(vrLeftHand, ikLeftHand, MapKind.LeftHand),
                BuildMap(vrRightHand, ikRightHand, MapKind.RightHand),
                autoComputeOffsets);

            EditorUtility.SetDirty(follow);
        }

        private VRMap BuildMap(Transform vrTarget, Transform ikTarget, MapKind kind)
        {
            Vector3 defaultPositionOffset;
            Vector3 defaultRotationOffset;

            switch (kind)
            {
                case MapKind.LeftHand:
                    defaultPositionOffset = new Vector3(-0.04f, -0.02f, -0.1f);
                    defaultRotationOffset = new Vector3(11.5f, 87.3f, 105.8f);
                    break;
                case MapKind.RightHand:
                    defaultPositionOffset = new Vector3(0.04f, -0.02f, -0.1f);
                    defaultRotationOffset = new Vector3(11.5f, -87.3f, -105.8f);
                    break;
                default:
                    defaultPositionOffset = Vector3.zero;
                    defaultRotationOffset = Vector3.zero;
                    break;
            }

            var map = new VRMap
            {
                vrTarget = vrTarget,
                ikTarget = ikTarget,
                trackingPositionOffset = defaultPositionOffset,
                trackingRotationOffset = defaultRotationOffset
            };

            return map;
        }

        private void ResetOffsetsOnAvatar()
        {
            if (avatarRoot == null)
                return;

            var animator = avatarRoot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[VRAvatarSetup] No Animator found on selected avatar.");
                return;
            }

            var follow = animator.transform.GetComponent<IKTargetFollowVRRig>();
            if (follow == null)
            {
                follow = Undo.AddComponent<IKTargetFollowVRRig>(animator.transform.gameObject);
            }

            Undo.RecordObject(follow, "Reset VR Offsets To Defaults");
            follow.ResetOffsetsToDefaults();
            EditorUtility.SetDirty(follow);
            Debug.Log("[VRAvatarSetup] IKTargetFollowVRRig offsets reset to defaults.");
        }

        private void SetupFootSolvers(Transform targetRoot)
        {
            if (ikLeftFoot == null || ikRightFoot == null)
            {
                Debug.LogWarning("[VRAvatarSetup] Foot targets missing. Foot solvers were not fully configured.");
                return;
            }

            var leftSolver = ikLeftFoot.GetComponent<IKFootSolver>();
            if (leftSolver == null)
                leftSolver = Undo.AddComponent<IKFootSolver>(ikLeftFoot.gameObject);

            var rightSolver = ikRightFoot.GetComponent<IKFootSolver>();
            if (rightSolver == null)
                rightSolver = Undo.AddComponent<IKFootSolver>(ikRightFoot.gameObject);

            SetSerializedField(leftSolver, "body", targetRoot);
            SetSerializedField(rightSolver, "body", targetRoot);
            SetSerializedField(leftSolver, "otherFoot", rightSolver);
            SetSerializedField(rightSolver, "otherFoot", leftSolver);

            EditorUtility.SetDirty(leftSolver);
            EditorUtility.SetDirty(rightSolver);
        }

        private void SetSerializedField(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var prop = serialized.FindProperty(fieldName);
            if (prop == null)
            {
                serialized.Dispose();
                return;
            }

            prop.objectReferenceValue = value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Dispose();
        }
    }
}
