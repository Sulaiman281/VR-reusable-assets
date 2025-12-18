using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace WitShells.XR.Editor
{
    public static class MakeItGrabbableEditor
    {
        private const string MenuPath = "GameObject/WitShells XR/Make It Grabbable";

        [MenuItem(MenuPath, false, 10)]
        public static void MakeItGrabbable()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("No GameObjects selected. Select one or more visuals to make grabbable.");
                return;
            }

            foreach (var visual in selected)
            {
                ProcessVisual(visual);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateMakeItGrabbable()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        private static void ProcessVisual(GameObject visual)
        {
            // Create parent with same name, placed next to the visual in hierarchy
            Transform oldParent = visual.transform.parent;
            int siblingIndex = visual.transform.GetSiblingIndex();

            var parentGO = new GameObject(visual.name);
            Undo.RegisterCreatedObjectUndo(parentGO, "Create Grabbable Parent");

            // Match parent's transform to visual so visual becomes identity under it
            parentGO.transform.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
            parentGO.transform.localScale = visual.transform.lossyScale; // best effort to retain child identity after reparent

            // Parent under old parent and set sibling order before moving visual
            Undo.SetTransformParent(parentGO.transform, oldParent, "Reparent to Grabbable Parent");
            parentGO.transform.SetSiblingIndex(siblingIndex);

            // Move visual under parent as first child, keeping world transform
            Undo.SetTransformParent(visual.transform, parentGO.transform, "Move Visual Under Grabbable Parent");
            visual.transform.SetSiblingIndex(0);

            // Ensure a Rigidbody exists on parent (required by XRGrabInteractable)
            var rb = Undo.AddComponent<Rigidbody>(parentGO);
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Add XRGrabInteractable to parent
            var grab = Undo.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(parentGO);

            // Build collider child as separate object (no collider on visuals)
            var colliderChild = new GameObject("Collider");
            Undo.RegisterCreatedObjectUndo(colliderChild, "Create Collider Child");
            Undo.SetTransformParent(colliderChild.transform, parentGO.transform, "Parent Collider Child");
            colliderChild.transform.localPosition = Vector3.zero;
            colliderChild.transform.localRotation = Quaternion.identity;
            colliderChild.transform.localScale = Vector3.one;

            var box = Undo.AddComponent<BoxCollider>(colliderChild);
            FitBoxColliderToVisuals(box, visual, parentGO.transform);

            // Remove any colliders from the visuals hierarchy
            RemoveCollidersInChildren(visual);

            // Set attachTransform to visuals so predicted pose attaches at visuals origin
            grab.attachTransform = visual.transform;

            // Select new parent for user feedback
            Selection.activeGameObject = parentGO;
        }

        private static void RemoveCollidersInChildren(GameObject root)
        {
            var cols = root.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                Undo.DestroyObjectImmediate(c);
            }
        }

        private static void FitBoxColliderToVisuals(BoxCollider box, GameObject visualRoot, Transform parentTransform)
        {
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                // default small box centered at parent
                box.center = Vector3.zero;
                box.size = Vector3.one * 0.1f;
                return;
            }

            Bounds worldBounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Convert world bounds center to parent local space
            Vector3 localCenter = parentTransform.InverseTransformPoint(worldBounds.center);

            // Convert world size to local size by dividing out lossy scale
            Vector3 lossy = parentTransform.lossyScale;
            lossy.x = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
            lossy.y = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
            lossy.z = Mathf.Approximately(lossy.z, 0f) ? 1f : lossy.z;

            Vector3 localSize = new Vector3(
                worldBounds.size.x / lossy.x,
                worldBounds.size.y / lossy.y,
                worldBounds.size.z / lossy.z
            );

            box.center = localCenter;
            box.size = localSize;
        }
    }
}
