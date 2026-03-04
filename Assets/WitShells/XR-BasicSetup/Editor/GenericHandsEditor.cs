using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WitShells.XR.Editor
{
    [CustomPropertyDrawer(typeof(Joint))]
    public class JointPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            var joinTransProp = property.FindPropertyRelative("joinTrans");
            var childJointProp = property.FindPropertyRelative("childJoint");

            // Main joint transform field
            var jointRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(jointRect, joinTransProp, label);

            // Draw child joint if it exists
            if (childJointProp != null && joinTransProp.objectReferenceValue != null)
            {
                var childRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, 
                    position.width, EditorGUIUtility.singleLineHeight);
                
                EditorGUI.indentLevel = indent + 1;
                var childName = joinTransProp.objectReferenceValue ? 
                    $"Child of {joinTransProp.objectReferenceValue.name}" : "Child Joint";
                
                EditorGUI.PropertyField(childRect, childJointProp, new GUIContent(childName));
                EditorGUI.indentLevel = indent;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var joinTransProp = property.FindPropertyRelative("joinTrans");
            var childJointProp = property.FindPropertyRelative("childJoint");

            float height = EditorGUIUtility.singleLineHeight;

            // Add height for child joint if parent exists
            if (childJointProp != null && joinTransProp.objectReferenceValue != null)
            {
                height += EditorGUIUtility.singleLineHeight + 2;
                // Recursively add height for nested children
                height += EditorGUI.GetPropertyHeight(childJointProp, true);
            }

            return height;
        }
    }

    [CustomPropertyDrawer(typeof(HandSkeleton))]
    public class HandSkeletonPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var currentY = position.y;
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = 2f;

            // Foldout for hand skeleton
            var foldoutRect = new Rect(position.x, currentY, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            currentY += lineHeight + spacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Wrist
                var wristRect = new Rect(position.x, currentY, position.width, lineHeight);
                var wristProp = property.FindPropertyRelative("wrist");
                EditorGUI.PropertyField(wristRect, wristProp);
                currentY += lineHeight + spacing;

                // Palm
                var palmRect = new Rect(position.x, currentY, position.width, lineHeight);
                var palmProp = property.FindPropertyRelative("palm");
                EditorGUI.PropertyField(palmRect, palmProp);
                currentY += lineHeight + spacing;

                // Fingers
                DrawFingerSection(ref currentY, position, property, "thumb", "Thumb", Color.red);
                DrawFingerSection(ref currentY, position, property, "index", "Index", Color.blue);
                DrawFingerSection(ref currentY, position, property, "middle", "Middle", Color.green);
                DrawFingerSection(ref currentY, position, property, "ring", "Ring", Color.yellow);
                DrawFingerSection(ref currentY, position, property, "pinky", "Pinky", Color.magenta);

                EditorGUI.indentLevel--;

                // Auto-fill button
                var buttonRect = new Rect(position.x, currentY, position.width, lineHeight + 4);
                if (GUI.Button(buttonRect, "Auto Fill Child Joints"))
                {
                    var target = property.serializedObject.targetObject;
                    
                    if (target is GenericHands genericHands)
                    {
                        if (property.name == "leftHand" && genericHands.leftHand != null)
                        {
                            genericHands.leftHand.AutoFillChildJoints();
                        }
                        else if (property.name == "rightHand" && genericHands.rightHand != null)
                        {
                            genericHands.rightHand.AutoFillChildJoints();
                        }
                        
                        EditorUtility.SetDirty(target);
                        property.serializedObject.Update();
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawFingerSection(ref float currentY, Rect position, SerializedProperty property, 
            string fingerName, string displayName, Color color)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = 2f;

            // Colored label for finger
            var labelRect = new Rect(position.x, currentY, position.width, lineHeight);
            var originalColor = GUI.color;
            GUI.color = color;
            EditorGUI.LabelField(labelRect, displayName, EditorStyles.boldLabel);
            GUI.color = originalColor;
            currentY += lineHeight;

            // Draw finger joint chain
            var fingerProp = property.FindPropertyRelative(fingerName);
            if (fingerProp != null)
            {
                var fingerRect = new Rect(position.x, currentY, position.width, 
                    EditorGUI.GetPropertyHeight(fingerProp, true));
                EditorGUI.PropertyField(fingerRect, fingerProp, GUIContent.none, true);
                currentY += EditorGUI.GetPropertyHeight(fingerProp, true) + spacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout

            if (property.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Wrist
                height += EditorGUIUtility.singleLineHeight + 2; // Palm

                // Fingers
                string[] fingers = { "thumb", "index", "middle", "ring", "pinky" };
                foreach (var finger in fingers)
                {
                    height += EditorGUIUtility.singleLineHeight; // Finger label
                    var fingerProp = property.FindPropertyRelative(finger);
                    if (fingerProp != null)
                    {
                        height += EditorGUI.GetPropertyHeight(fingerProp, true) + 2;
                    }
                }

                height += EditorGUIUtility.singleLineHeight + 6; // Auto-fill button
            }

            return height;
        }
    }

    [CustomEditor(typeof(GenericHands))]
    public class GenericHandsEditor : UnityEditor.Editor
    {
        private SerializedProperty leftHandProp;
        private SerializedProperty rightHandProp;
        private SerializedProperty enableTrackingProp;
        private SerializedProperty handPositionTrackingProp;
        private SerializedProperty enableSmoothingProp;
        private SerializedProperty positionSmoothingProp;
        private SerializedProperty rotationSmoothingProp;

        private bool showJointHierarchy = false;
        private bool showTrackingInfo = true;

        private void OnEnable()
        {
            leftHandProp = serializedObject.FindProperty("leftHand");
            rightHandProp = serializedObject.FindProperty("rightHand");
            enableTrackingProp = serializedObject.FindProperty("enableTracking");
            handPositionTrackingProp = serializedObject.FindProperty("handPositionTracking");
            enableSmoothingProp = serializedObject.FindProperty("enableSmoothing");
            positionSmoothingProp = serializedObject.FindProperty("positionSmoothing");
            rotationSmoothingProp = serializedObject.FindProperty("rotationSmoothing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var genericHands = target as GenericHands;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generic Hands Controller", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // Tracking Status
            if (Application.isPlaying)
            {
                showTrackingInfo = EditorGUILayout.Foldout(showTrackingInfo, "Tracking Status", true);
                if (showTrackingInfo)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Left Hand", EditorStyles.boldLabel);
                    EditorGUILayout.Toggle("Tracked", genericHands.leftHandTracked);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Right Hand", EditorStyles.boldLabel);
                    EditorGUILayout.Toggle("Tracked", genericHands.rightHandTracked);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.Space();
            }

            // Tracking Settings
            EditorGUILayout.LabelField("Tracking Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableTrackingProp);
            EditorGUILayout.PropertyField(handPositionTrackingProp);
            EditorGUILayout.PropertyField(enableSmoothingProp);
            
            // Show smoothing controls only if smoothing is enabled
            if (enableSmoothingProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(positionSmoothingProp);
                EditorGUILayout.PropertyField(rotationSmoothingProp);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            // Hand Skeletons
            EditorGUILayout.LabelField("Hand Skeletons", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(leftHandProp);
            EditorGUILayout.PropertyField(rightHandProp);

            // Joint Hierarchy Visualization
            EditorGUILayout.Space();
            showJointHierarchy = EditorGUILayout.Foldout(showJointHierarchy, "Joint Hierarchy View", true);
            if (showJointHierarchy)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawJointHierarchy("Left Hand", genericHands.leftHand);
                EditorGUILayout.Space();
                DrawJointHierarchy("Right Hand", genericHands.rightHand);
                EditorGUILayout.EndVertical();
            }

            // Utility Buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto Fill All Joints"))
            {
                if (genericHands.leftHand != null)
                    genericHands.leftHand.AutoFillChildJoints();
                if (genericHands.rightHand != null)
                    genericHands.rightHand.AutoFillChildJoints();
                
                EditorUtility.SetDirty(target);
                serializedObject.Update();
            }

            if (GUILayout.Button("Clear All Joints"))
            {
                if (EditorUtility.DisplayDialog("Clear Joints", "Are you sure you want to clear all joint assignments?", "Yes", "No"))
                {
                    // Clear palm references
                    if (genericHands.leftHand != null)
                        genericHands.leftHand.palm = null;
                    if (genericHands.rightHand != null)
                        genericHands.rightHand.palm = null;

                    // Clear finger joint chains
                    ClearJointChain(genericHands.leftHand?.thumb);
                    ClearJointChain(genericHands.leftHand?.index);
                    ClearJointChain(genericHands.leftHand?.middle);
                    ClearJointChain(genericHands.leftHand?.ring);
                    ClearJointChain(genericHands.leftHand?.pinky);

                    ClearJointChain(genericHands.rightHand?.thumb);
                    ClearJointChain(genericHands.rightHand?.index);
                    ClearJointChain(genericHands.rightHand?.middle);
                    ClearJointChain(genericHands.rightHand?.ring);
                    ClearJointChain(genericHands.rightHand?.pinky);

                    EditorUtility.SetDirty(target);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawJointHierarchy(string handName, HandSkeleton handSkeleton)
        {
            if (handSkeleton == null) return;

            EditorGUILayout.LabelField(handName, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Wrist: {(handSkeleton.wrist ? handSkeleton.wrist.name : "None")}");
            EditorGUILayout.LabelField($"Palm: {(handSkeleton.palm ? handSkeleton.palm.name : "None")}");
            DrawFingerChain("Thumb", handSkeleton.thumb);
            DrawFingerChain("Index", handSkeleton.index);
            DrawFingerChain("Middle", handSkeleton.middle);
            DrawFingerChain("Ring", handSkeleton.ring);
            DrawFingerChain("Pinky", handSkeleton.pinky);

            EditorGUI.indentLevel--;
        }

        private void DrawFingerChain(string fingerName, Joint finger)
        {
            EditorGUILayout.LabelField($"{fingerName}:");
            EditorGUI.indentLevel++;

            var current = finger;
            int jointIndex = 0;
            while (current != null)
            {
                string jointName = current.joinTrans ? current.joinTrans.name : "None";
                EditorGUILayout.LabelField($"Joint {jointIndex}: {jointName}");
                current = current.childJoint;
                jointIndex++;
            }

            EditorGUI.indentLevel--;
        }

        private void ClearJointChain(Joint joint)
        {
            if (joint == null) return;
            
            ClearJointChain(joint.childJoint);
            joint.joinTrans = null;
            joint.childJoint = null;
        }
    }
}