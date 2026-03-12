using UnityEditor;
using UnityEngine;
using WitShells.CheckPoint;

namespace WitShells.CheckPoint.Editor
{
    public static class CreateCheckPointEditor
    {
        private const string ShaderPath = "Shader Graphs/CheckPoint-Cylinder";
        private const string MenuPath   = "GameObject/WitShells/Create CheckPoint";

        // ── Menu entry ────────────────────────────────────────────────────────────
        [MenuItem(MenuPath, false, 10)]
        public static void CreateCheckPoint()
        {
            // 1. Create a Cylinder primitive (comes with a CapsuleCollider)
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "CheckPoint";

            // Register the whole object for Undo; everything below is part of the same op
            Undo.RegisterCreatedObjectUndo(cylinder, "Create CheckPoint");

            // 2. Place at scene-view pivot so it lands in view
            if (SceneView.lastActiveSceneView != null)
                cylinder.transform.position = SceneView.lastActiveSceneView.pivot;

            // 3. Create (or reuse) the material and apply it
            var material = GetOrCreateMaterial();
            if (material != null)
                cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;

            // 4. CapsuleCollider from CreatePrimitive satisfies [RequireComponent(typeof(Collider))].
            //    Mark it as a trigger so it responds to overlap events.
            cylinder.GetComponent<Collider>().isTrigger = true;

            // 5. Attach the runtime script
            Undo.AddComponent<CheckPointTrigger>(cylinder);

            Selection.activeGameObject = cylinder;
        }

        // ── Helpers ────────────────────────────────────────────────────────────────
        private static Material GetOrCreateMaterial()
        {
            var materialPath = ResolveMaterialPath();

            // Reuse an existing material asset when available
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find(ShaderPath);
            if (shader == null)
            {
                Debug.LogWarning($"[CheckPoint] Shader \"{ShaderPath}\" not found. Assign a material manually.");
                return null;
            }

            EnsureFolder(materialPath);

            var material = new Material(shader) { name = "CheckPoint-Cylinder" };
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();

            return material;
        }

        // Derives the material path from this script's own location so the path
        // stays correct whether the package lives in Assets/ or Packages/.
        private static string ResolveMaterialPath()
        {
            var guids = AssetDatabase.FindAssets($"t:Script {nameof(CreateCheckPointEditor)}");
            if (guids.Length > 0)
            {
                // ".../CheckPoint/Editor/CreateCheckPointEditor.cs" → up two levels = package root
                var scriptPath  = AssetDatabase.GUIDToAssetPath(guids[0]);
                var packageRoot = System.IO.Path.GetDirectoryName(
                    System.IO.Path.GetDirectoryName(scriptPath)).Replace('\\', '/');
                return $"{packageRoot}/Runtime/Materials/CheckPoint-Cylinder.mat";
            }

            // Fallback (should never be reached in a properly imported package)
            return "Assets/WitShells/CheckPoint/Runtime/Materials/CheckPoint-Cylinder.mat";
        }

        // Creates every missing folder segment via AssetDatabase (required for asset paths).
        private static void EnsureFolder(string assetPath)
        {
            var dir   = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var parts = dir.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
