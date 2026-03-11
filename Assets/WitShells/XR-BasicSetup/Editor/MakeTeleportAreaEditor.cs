using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace WitShells.XR.Editor
{
    public class MakeTeleportAreaEditor : EditorWindow
    {
        // ── Teleport mode ────────────────────────────────────────────────────────
        private enum TeleportMode
        {
            AnywhereUserClicks,
            SpecificPosition
        }

        // ── State ─────────────────────────────────────────────────────────────────
        private static GameObject s_Target;

        private TeleportMode m_TeleportMode = TeleportMode.AnywhereUserClicks;
        private Transform m_TeleportAnchor;           // used for SpecificPosition
        private XRRayInteractor m_LeftRayInteractor;
        private XRRayInteractor m_RightRayInteractor;
        private bool m_SingleHandMode;
        private XRRayInteractor m_SingleRayInteractor;

        // ── Optional reticle overrides (applied to XRInteractorLineVisual) ────────
        private GameObject m_SingleReticle;
        private GameObject m_SingleBlockedReticle;
        private GameObject m_LeftReticle;
        private GameObject m_LeftBlockedReticle;
        private GameObject m_RightReticle;
        private GameObject m_RightBlockedReticle;

        // ── Line Visual Style ─────────────────────────────────────────────────────
        private enum LineStylePreset
        {
            None,           // leave line visual unchanged
            CyberNeon,      // electric cyan → white / violet blocked
            DragonFire,     // orange → red   / grey blocked
            IceBeam,        // sky-blue → white / steel-blue blocked
            NatureGreen,    // lime → emerald / olive blocked
            RoyalPurple,    // magenta → violet / indigo blocked
            Custom          // user-defined gradients
        }

        private enum LineCurve { Straight, BezierCurve, ProjectileCurve }

        private struct LineStyleData
        {
            public bool apply;
            public XRRayInteractor.LineType lineType;
            public float lineWidth;
            public Gradient validGradient;    // ray hits a valid teleport surface
            public Gradient invalidGradient;  // ray hits an invalid / non-teleport surface
            public Gradient blockedGradient;  // ray is physically occluded / obstructed
        }

        private LineStylePreset m_LineStylePreset = LineStylePreset.CyberNeon;
        private LineCurve m_LineCurve = LineCurve.Straight;
        private float m_LineWidth = 0.005f;
        private Gradient m_CustomValidGradient;
        private Gradient m_CustomInvalidGradient;
        private Gradient m_CustomBlockedGradient;
        private bool m_LineStyleFoldout = true;
        private Vector2 m_ScrollPos;

        // ── Scene prerequisites ─────────────────────────────────────────────────────
        private TeleportationProvider m_TeleportationProvider;
        private LocomotionMediator m_LocomotionMediator;
        private bool m_PrereqFoldout = true;

        // ── Layer name the teleport system uses ──────────────────────────────────
        private const string TeleportLayerName = "Teleport";

        // ── Menu entry ────────────────────────────────────────────────────────────
        private const string MenuPath = "GameObject/WitShells XR/Make Teleport Area";

        [MenuItem(MenuPath, false, 11)]
        public static void OpenWindow()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[MakeTeleportArea] No GameObject selected.");
                return;
            }

            s_Target = selected;
            var window = GetWindow<MakeTeleportAreaEditor>(true, "Make Teleport Area", true);
            window.minSize = new Vector2(420, 700);
            window.Show();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateOpenWindow()
        {
            return Selection.activeGameObject != null;
        }

        private void OnEnable() => TryAutoDetectPrerequisites();

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Make Teleport Area", EditorStyles.largeLabel);
            EditorGUILayout.Space(4);

            // Target object (read-only display)
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Target Object", s_Target, typeof(GameObject), true);

            EditorGUILayout.Space(8);
            DrawDivider();

            // ── Scene prerequisites ───────────────────────────────────────────────
            DrawPrerequisites();

            EditorGUILayout.Space(8);
            DrawDivider();

            // ── Teleport mode ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Teleport Mode", EditorStyles.boldLabel);
            m_TeleportMode = (TeleportMode)EditorGUILayout.EnumPopup(
                new GUIContent("Mode",
                    "AnywhereUserClicks – player lands wherever the ray hits the area.\n" +
                    "SpecificPosition   – player always teleports to a fixed anchor Transform."),
                m_TeleportMode);

            if (m_TeleportMode == TeleportMode.SpecificPosition)
            {
                EditorGUI.indentLevel++;
                m_TeleportAnchor = (Transform)EditorGUILayout.ObjectField(
                    new GUIContent("Anchor Transform",
                        "The Transform the player will be moved to. Leave empty to auto-create one on the target."),
                    m_TeleportAnchor, typeof(Transform), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);
            DrawDivider();

            // ── Raycast interactor references ─────────────────────────────────────
            EditorGUILayout.LabelField("Ray Interactor References", EditorStyles.boldLabel);

            m_SingleHandMode = EditorGUILayout.Toggle(
                new GUIContent("Single Hand Mode", "Enable if you have only one ray interactor."),
                m_SingleHandMode);

            EditorGUI.indentLevel++;
            if (m_SingleHandMode)
            {
                m_SingleRayInteractor = (XRRayInteractor)EditorGUILayout.ObjectField(
                    new GUIContent("Ray Interactor", "The single XRRayInteractor that should be able to teleport."),
                    m_SingleRayInteractor, typeof(XRRayInteractor), true);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Line Visual Reticles (optional)", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                m_SingleReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Reticle",
                        "Assigned to XRInteractorLineVisual.reticle on the same GameObject as the ray interactor."),
                    m_SingleReticle, typeof(GameObject), true);
                m_SingleBlockedReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Blocked Reticle",
                        "Assigned to XRInteractorLineVisual.blockedReticle – shown when the ray is blocked."),
                    m_SingleBlockedReticle, typeof(GameObject), true);
                EditorGUI.indentLevel--;
            }
            else
            {
                m_LeftRayInteractor = (XRRayInteractor)EditorGUILayout.ObjectField(
                    new GUIContent("Left Ray Interactor", "Left-hand XRRayInteractor."),
                    m_LeftRayInteractor, typeof(XRRayInteractor), true);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Left Line Visual Reticles (optional)", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                m_LeftReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Reticle",
                        "Assigned to XRInteractorLineVisual.reticle on the left ray interactor's GameObject."),
                    m_LeftReticle, typeof(GameObject), true);
                m_LeftBlockedReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Blocked Reticle",
                        "Assigned to XRInteractorLineVisual.blockedReticle on the left ray interactor's GameObject."),
                    m_LeftBlockedReticle, typeof(GameObject), true);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);
                m_RightRayInteractor = (XRRayInteractor)EditorGUILayout.ObjectField(
                    new GUIContent("Right Ray Interactor", "Right-hand XRRayInteractor."),
                    m_RightRayInteractor, typeof(XRRayInteractor), true);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Right Line Visual Reticles (optional)", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                m_RightReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Reticle",
                        "Assigned to XRInteractorLineVisual.reticle on the right ray interactor's GameObject."),
                    m_RightReticle, typeof(GameObject), true);
                m_RightBlockedReticle = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Blocked Reticle",
                        "Assigned to XRInteractorLineVisual.blockedReticle on the right ray interactor's GameObject."),
                    m_RightBlockedReticle, typeof(GameObject), true);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            // Warning if none are set
            bool hasInteractor = m_SingleHandMode
                ? m_SingleRayInteractor != null
                : (m_LeftRayInteractor != null || m_RightRayInteractor != null);

            if (!hasInteractor)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "No XRRayInteractor assigned. Assign at least one ray interactor above so the tool " +
                    "can configure its interaction layers to include the Teleport layer.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            DrawDivider();

            // ── Line Visual Style ─────────────────────────────────────────────────
            DrawLineVisualStyle();

            EditorGUILayout.Space(12);
            DrawDivider();

            // ── Apply button ──────────────────────────────────────────────────────
            GUI.enabled = s_Target != null;
            if (GUILayout.Button("Apply Teleport Area", GUILayout.Height(32)))
                ApplyTeleportArea();
            GUI.enabled = true;

            EditorGUILayout.Space(4);
            EditorGUILayout.EndScrollView();
        }

        // ── Apply logic ───────────────────────────────────────────────────────────
        private void ApplyTeleportArea()
        {
            if (s_Target == null)
            {
                EditorUtility.DisplayDialog("Make Teleport Area",
                    "Target object is null. Please close this window and reselect a GameObject.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Make Teleport Area");
            int undoGroup = Undo.GetCurrentGroup();

            // ── 1. Ensure Teleport layer exists ───────────────────────────────────
            int teleportLayer = EnsureLayer(TeleportLayerName);

            // ── 2. Assign layer to target ─────────────────────────────────────────
            Undo.RecordObject(s_Target, "Set Teleport Layer");
            s_Target.layer = teleportLayer;

            // ── 3. Ensure a collider is present ───────────────────────────────────
            var collider = s_Target.GetComponent<Collider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider>(s_Target);
                Debug.Log($"[MakeTeleportArea] Added BoxCollider to '{s_Target.name}'.");
            }

            // ── 4. Add / configure TeleportationArea ──────────────────────────────
            var area = s_Target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
            if (area == null)
                area = Undo.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>(s_Target);

            Undo.RecordObject(area, "Configure TeleportationArea");

            if (m_TeleportMode == TeleportMode.SpecificPosition)
            {
                area.teleportTrigger = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;

                // Auto-create anchor if not supplied
                if (m_TeleportAnchor == null)
                {
                    var anchorGO = new GameObject("TeleportAnchor");
                    Undo.RegisterCreatedObjectUndo(anchorGO, "Create Teleport Anchor");
                    Undo.SetTransformParent(anchorGO.transform, s_Target.transform, "Parent Anchor");
                    anchorGO.transform.localPosition = Vector3.zero;
                    anchorGO.transform.localRotation = Quaternion.identity;
                    m_TeleportAnchor = anchorGO.transform;
                    Debug.Log($"[MakeTeleportArea] Auto-created TeleportAnchor child on '{s_Target.name}'.");
                }

                area.customReticle = m_TeleportAnchor.gameObject;
            }
            else
            {
                // AnywhereUserClicks – trigger on hover exit so it fires where the ray lands
                area.teleportTrigger = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
            }

            // ── 5. Build the interaction layer mask for the Teleport layer ─────────
            var teleportMask = InteractionLayerMask.GetMask(TeleportLayerName);
            Undo.RecordObject(area, "Set TeleportationArea Interaction Layer");
            area.interactionLayers = teleportMask;

            // ── 6. Configure ray interactors ──────────────────────────────────────
            var interactors = CollectInteractors();
            var lineStyle = BuildLineStyleData();
            if (interactors.Count == 0)
            {
                Debug.LogWarning("[MakeTeleportArea] No XRRayInteractor assigned – skipping interactor layer configuration. " +
                                 "Assign interactors in the window and re-apply to finish setup.");
            }
            else
            {
                if (m_SingleHandMode)
                {
                    if (m_SingleRayInteractor != null)
                        ConfigureRayInteractor(m_SingleRayInteractor, teleportMask, teleportLayer, m_SingleReticle, m_SingleBlockedReticle, lineStyle);
                }
                else
                {
                    if (m_LeftRayInteractor != null)
                        ConfigureRayInteractor(m_LeftRayInteractor, teleportMask, teleportLayer, m_LeftReticle, m_LeftBlockedReticle, lineStyle);
                    if (m_RightRayInteractor != null)
                        ConfigureRayInteractor(m_RightRayInteractor, teleportMask, teleportLayer, m_RightReticle, m_RightBlockedReticle, lineStyle);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.SetDirty(s_Target);
            Debug.Log($"[MakeTeleportArea] '{s_Target.name}' successfully configured as a TeleportationArea " +
                      $"(mode: {m_TeleportMode}, layer: {TeleportLayerName}).");

            Close();
        }

        // ── Helper: configure a single ray interactor ─────────────────────────────
        private static void ConfigureRayInteractor(XRRayInteractor interactor, int xriTeleportMask,
            int teleportPhysicsLayer, GameObject reticle, GameObject blockedReticle, LineStyleData style)
        {
            Undo.RecordObject(interactor, "Configure Ray Interactor Teleport Layer");

            // 1. XRI interaction layer – lets TeleportationArea recognise this interactor.
            interactor.interactionLayers |= xriTeleportMask;

            // 2. Physics raycast mask – without this the ray never HITS the collider,
            //    so the line snaps to the object pivot (0,0,0 local) and teleport fires
            //    at the centre of the surface instead of where the ray actually lands.
            interactor.raycastMask |= 1 << teleportPhysicsLayer;

            // 3. Line curve type + per-curve defaults.
            if (style.apply)
            {
                interactor.lineType = style.lineType;
                switch (style.lineType)
                {
                    case XRRayInteractor.LineType.StraightLine:
                        // Straight: extend reach to 10 m so the ray comfortably hits the floor.
                        interactor.maxRaycastDistance = 10f;
                        break;
                    case XRRayInteractor.LineType.BezierCurve:
                        interactor.controlPointDistance = 0.5f;
                        interactor.controlPointHeight = 0.5f;
                        break;
                    case XRRayInteractor.LineType.ProjectileCurve:
                        interactor.velocity = 16f;
                        interactor.acceleration = 9.8f;
                        break;
                }
            }

            EditorUtility.SetDirty(interactor);
            Debug.Log($"[MakeTeleportArea] Configured '{interactor.gameObject.name}' – " +
                      $"XRI layer + physics raycast layer {teleportPhysicsLayer} added.");

            // ── XRInteractorLineVisual ────────────────────────────────────────────
            var lineVisual = interactor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
            if (lineVisual == null)
            {
                if (reticle != null || blockedReticle != null || style.apply)
                    Debug.LogWarning($"[MakeTeleportArea] '{interactor.gameObject.name}' has no XRInteractorLineVisual – " +
                                     "reticle/style changes were not applied. Add the component and re-apply.");
                return;
            }

            Undo.RecordObject(lineVisual, "Configure Line Visual");

            // Ensure the line originates from the controller's attach point, not world (0,0,0).
            lineVisual.overrideInteractorLineOrigin = false;

            if (style.apply)
            {
                lineVisual.lineWidth = style.lineWidth;
                lineVisual.validColorGradient = style.validGradient;
                lineVisual.invalidColorGradient = style.invalidGradient;
                lineVisual.blockedColorGradient = style.blockedGradient;
                Debug.Log($"[MakeTeleportArea] Applied line style to '{interactor.gameObject.name}' " +
                          $"(width: {style.lineWidth:F4}, curve: {style.lineType}).");
            }

            if (reticle != null)
            {
                lineVisual.reticle = reticle;
                Debug.Log($"[MakeTeleportArea] Assigned reticle '{reticle.name}' to '{interactor.gameObject.name}'.");
            }

            if (blockedReticle != null)
            {
                lineVisual.blockedReticle = blockedReticle;
                Debug.Log($"[MakeTeleportArea] Assigned blocked reticle '{blockedReticle.name}' to '{interactor.gameObject.name}'.");
            }

            EditorUtility.SetDirty(lineVisual);
        }

        // ── Helper: collect assigned interactors into a list ─────────────────────
        private List<XRRayInteractor> CollectInteractors()
        {
            var list = new List<XRRayInteractor>();
            if (m_SingleHandMode)
            {
                if (m_SingleRayInteractor != null) list.Add(m_SingleRayInteractor);
            }
            else
            {
                if (m_LeftRayInteractor != null) list.Add(m_LeftRayInteractor);
                if (m_RightRayInteractor != null) list.Add(m_RightRayInteractor);
            }
            return list;
        }

        // ── Helper: auto-detect prerequisites from scene ────────────────────────────
        private void TryAutoDetectPrerequisites()
        {
            // Walk up from the target first (provider is usually on the XR Origin, target is a child)
            if (m_TeleportationProvider == null)
            {
                if (s_Target != null)
                    m_TeleportationProvider = s_Target.GetComponentInParent<TeleportationProvider>(true);
                if (m_TeleportationProvider == null)
                    m_TeleportationProvider = FindAnyObjectByType<TeleportationProvider>();
            }

            if (m_LocomotionMediator == null)
            {
                // Co-locate check: look on/near the provider first
                if (m_TeleportationProvider != null)
                {
                    m_LocomotionMediator = m_TeleportationProvider.GetComponent<LocomotionMediator>();
                    if (m_LocomotionMediator == null)
                        m_LocomotionMediator = m_TeleportationProvider.GetComponentInParent<LocomotionMediator>(true);
                }
                if (m_LocomotionMediator == null)
                    m_LocomotionMediator = FindAnyObjectByType<LocomotionMediator>();
            }
        }

        // ── Helper: draw prerequisites section ──────────────────────────────────
        private void DrawPrerequisites()
        {
            m_PrereqFoldout = EditorGUILayout.Foldout(
                m_PrereqFoldout, "Scene Prerequisites", true, EditorStyles.foldoutHeader);
            if (!m_PrereqFoldout) return;

            EditorGUILayout.Space(2);
            EditorGUI.indentLevel++;

            // TeleportationProvider row
            using (new EditorGUILayout.HorizontalScope())
            {
                m_TeleportationProvider = (TeleportationProvider)EditorGUILayout.ObjectField(
                    new GUIContent("Teleport Provider",
                        "Processes teleport requests and moves the XR Origin.\n" +
                        "Must exist in the scene. Usually on the XR Origin."),
                    m_TeleportationProvider, typeof(TeleportationProvider), true);
                if (GUILayout.Button("Find", GUILayout.Width(46)))
                {
                    m_TeleportationProvider = null;
                    m_LocomotionMediator = null;
                    TryAutoDetectPrerequisites();
                }
            }

            // LocomotionMediator row
            using (new EditorGUILayout.HorizontalScope())
            {
                m_LocomotionMediator = (LocomotionMediator)EditorGUILayout.ObjectField(
                    new GUIContent("Locomotion Mediator",
                        "XRI 3.x coordinator that grants locomotion authority to providers.\n" +
                        "Required for TeleportationProvider to actually be allowed to move the rig."),
                    m_LocomotionMediator, typeof(LocomotionMediator), true);
                if (GUILayout.Button("Find", GUILayout.Width(46)))
                {
                    m_LocomotionMediator = null;
                    TryAutoDetectPrerequisites();
                }
            }

            bool missingProvider = m_TeleportationProvider == null;
            bool missingMediator = m_LocomotionMediator == null;

            EditorGUILayout.Space(3);
            if (missingProvider || missingMediator)
            {
                string msg = "Missing required components:" +
                    (missingProvider ? "\n  • TeleportationProvider – teleport requests are never processed." : "") +
                    (missingMediator ? "\n  • LocomotionMediator    – provider never gets authority to move." : "") +
                    "\n\nClick 'Add Missing' to add them to a new 'XR Locomotion' GameObject.";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
                EditorGUILayout.Space(2);
                if (GUILayout.Button("Add Missing Components", GUILayout.Height(24)))
                    AddMissingLocomotionComponents();
            }
            else
            {
                DrawPrereqStatusRow("TeleportationProvider", m_TeleportationProvider.gameObject.name);
                DrawPrereqStatusRow("LocomotionMediator", m_LocomotionMediator.gameObject.name);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawPrereqStatusRow(string component, string goName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(component);
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = new Color(0.25f, 0.78f, 0.25f);
                EditorGUILayout.LabelField($"✓  {goName}", style);
            }
        }

        private void AddMissingLocomotionComponents()
        {
            Undo.SetCurrentGroupName("Add XR Locomotion Components");
            int grp = Undo.GetCurrentGroup();

            if (m_TeleportationProvider == null)
            {
                var host = new GameObject("XR Locomotion");
                Undo.RegisterCreatedObjectUndo(host, "Create XR Locomotion");
                m_TeleportationProvider = Undo.AddComponent<TeleportationProvider>(host);
                Debug.Log("[MakeTeleportArea] Created 'XR Locomotion' with TeleportationProvider.");
            }

            if (m_LocomotionMediator == null)
            {
                var host = m_TeleportationProvider.gameObject;
                m_LocomotionMediator = Undo.AddComponent<LocomotionMediator>(host);
                EditorUtility.SetDirty(host);
                Debug.Log($"[MakeTeleportArea] Added LocomotionMediator to '{host.name}'.");
            }

            Undo.CollapseUndoOperations(grp);
        }

        // ── Line Visual Style GUI ──────────────────────────────────────────────────
        private void DrawLineVisualStyle()
        {
            m_LineStyleFoldout = EditorGUILayout.Foldout(
                m_LineStyleFoldout, "Line Visual Style  (XRInteractorLineVisual)",
                true, EditorStyles.foldoutHeader);
            if (!m_LineStyleFoldout) return;

            EditorGUILayout.Space(2);
            EditorGUI.indentLevel++;

            m_LineStylePreset = (LineStylePreset)EditorGUILayout.EnumPopup(
                new GUIContent("Color Preset",
                    "Apply a built-in color scheme to validColorGradient / invalidColorGradient.\n" +
                    "Select None to leave the gradients unchanged."),
                m_LineStylePreset);

            if (m_LineStylePreset == LineStylePreset.None)
            {
                EditorGUI.indentLevel--;
                return;
            }

            m_LineCurve = (LineCurve)EditorGUILayout.EnumPopup(
                new GUIContent("Curve Type",
                    "Sets XRRayInteractor.lineType.\n" +
                    "Straight        – direct laser beam (best for teleport).\n" +
                    "BezierCurve     – smooth parabolic arc.\n" +
                    "ProjectileCurve – physics-based arc."),
                m_LineCurve);

            m_LineWidth = EditorGUILayout.Slider(
                new GUIContent("Line Width", "Sets XRInteractorLineVisual.lineWidth."),
                m_LineWidth, 0.001f, 0.025f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Gradient Preview", EditorStyles.miniLabel);

            if (m_LineStylePreset == LineStylePreset.Custom)
            {
                if (m_CustomValidGradient == null) m_CustomValidGradient = BuildGradient(new Color(0f, 0.9f, 1f, 0f), new Color(0f, 0.9f, 1f, 1f));
                if (m_CustomInvalidGradient == null) m_CustomInvalidGradient = BuildGradient(new Color(0.5f, 0.5f, 0.5f, 0f), new Color(0.5f, 0.5f, 0.5f, 0.7f));
                if (m_CustomBlockedGradient == null) m_CustomBlockedGradient = BuildGradient(new Color(1f, 0.3f, 0.1f, 0f), new Color(1f, 0.3f, 0.1f, 1f));
                m_CustomValidGradient = EditorGUILayout.GradientField(
                    new GUIContent("Valid", "XRInteractorLineVisual.validColorGradient\nShown when the ray hits a valid teleport surface."),
                    m_CustomValidGradient);
                m_CustomInvalidGradient = EditorGUILayout.GradientField(
                    new GUIContent("Invalid", "XRInteractorLineVisual.invalidColorGradient\nShown when the ray hits a non-teleport / invalid surface."),
                    m_CustomInvalidGradient);
                m_CustomBlockedGradient = EditorGUILayout.GradientField(
                    new GUIContent("Blocked", "XRInteractorLineVisual.blockedColorGradient\nShown when the ray is physically occluded or obstructed."),
                    m_CustomBlockedGradient);
            }
            else
            {
                GetPresetGradients(m_LineStylePreset, out var validPrev, out var invalidPrev, out var blockedPrev);
                DrawGradientSwatch("Valid", validPrev);
                DrawGradientSwatch("Invalid", invalidPrev);
                DrawGradientSwatch("Blocked", blockedPrev);
            }

            EditorGUI.indentLevel--;
        }

        private LineStyleData BuildLineStyleData()
        {
            if (m_LineStylePreset == LineStylePreset.None)
                return default;

            GetPresetGradients(m_LineStylePreset, out var valid, out var invalid, out var blocked);
            if (m_LineStylePreset == LineStylePreset.Custom)
            {
                valid = m_CustomValidGradient ?? BuildGradient(new Color(0f, 0.9f, 1f, 0f), new Color(0f, 0.9f, 1f, 1f));
                invalid = m_CustomInvalidGradient ?? BuildGradient(new Color(0.5f, 0.5f, 0.5f, 0f), new Color(0.5f, 0.5f, 0.5f, 0.7f));
                blocked = m_CustomBlockedGradient ?? BuildGradient(new Color(1f, 0.3f, 0.1f, 0f), new Color(1f, 0.3f, 0.1f, 1f));
            }

            return new LineStyleData
            {
                apply = true,
                lineType = ToXRLineType(m_LineCurve),
                lineWidth = m_LineWidth,
                validGradient = valid,
                invalidGradient = invalid,
                blockedGradient = blocked
            };
        }

        private static void GetPresetGradients(LineStylePreset preset,
            out Gradient valid, out Gradient invalid, out Gradient blocked)
        {
            switch (preset)
            {
                case LineStylePreset.CyberNeon:
                    // valid   : transparent cyan → bright white (neon glow)
                    // invalid : transparent dark-grey → mid-grey (neutral miss)
                    // blocked : transparent violet → vivid violet (obstructed)
                    valid = BuildGradient(new Color(0.00f, 0.90f, 1.00f, 0f),
                                            new Color(0.00f, 0.90f, 1.00f, 1f),
                                            new Color(0.80f, 0.97f, 1.00f, 1f));
                    invalid = BuildGradient(new Color(0.35f, 0.35f, 0.35f, 0f),
                                            new Color(0.55f, 0.55f, 0.55f, 0.65f));
                    blocked = BuildGradient(new Color(0.55f, 0.00f, 0.85f, 0f),
                                            new Color(0.55f, 0.00f, 0.85f, 0.85f));
                    break;

                case LineStylePreset.DragonFire:
                    // valid   : transparent deep-orange → vivid red
                    // invalid : transparent dark-grey → mid-grey
                    // blocked : transparent dark-red → deep crimson
                    valid = BuildGradient(new Color(1.00f, 0.55f, 0.00f, 0f),
                                            new Color(1.00f, 0.18f, 0.00f, 1f));
                    invalid = BuildGradient(new Color(0.35f, 0.35f, 0.35f, 0f),
                                            new Color(0.55f, 0.55f, 0.55f, 0.65f));
                    blocked = BuildGradient(new Color(0.45f, 0.05f, 0.00f, 0f),
                                            new Color(0.70f, 0.05f, 0.05f, 0.80f));
                    break;

                case LineStylePreset.IceBeam:
                    // valid   : transparent sky-blue → pure white
                    // invalid : transparent dark-grey → mid-grey
                    // blocked : transparent dark steel-blue → steel-blue
                    valid = BuildGradient(new Color(0.55f, 0.88f, 1.00f, 0f),
                                            new Color(1.00f, 1.00f, 1.00f, 1f));
                    invalid = BuildGradient(new Color(0.35f, 0.35f, 0.35f, 0f),
                                            new Color(0.55f, 0.55f, 0.55f, 0.65f));
                    blocked = BuildGradient(new Color(0.15f, 0.28f, 0.45f, 0f),
                                            new Color(0.30f, 0.50f, 0.70f, 0.80f));
                    break;

                case LineStylePreset.NatureGreen:
                    // valid   : transparent lime → deep emerald
                    // invalid : transparent dark-grey → mid-grey
                    // blocked : transparent dark-olive → olive-yellow
                    valid = BuildGradient(new Color(0.25f, 0.92f, 0.28f, 0f),
                                            new Color(0.04f, 0.58f, 0.10f, 1f));
                    invalid = BuildGradient(new Color(0.35f, 0.35f, 0.35f, 0f),
                                            new Color(0.55f, 0.55f, 0.55f, 0.65f));
                    blocked = BuildGradient(new Color(0.30f, 0.28f, 0.02f, 0f),
                                            new Color(0.55f, 0.52f, 0.08f, 0.80f));
                    break;

                case LineStylePreset.RoyalPurple:
                    // valid   : transparent hot-magenta → deep violet
                    // invalid : transparent dark-grey → mid-grey
                    // blocked : transparent dark-indigo → indigo
                    valid = BuildGradient(new Color(0.90f, 0.12f, 1.00f, 0f),
                                            new Color(0.42f, 0.00f, 0.92f, 1f));
                    invalid = BuildGradient(new Color(0.35f, 0.35f, 0.35f, 0f),
                                            new Color(0.55f, 0.55f, 0.55f, 0.65f));
                    blocked = BuildGradient(new Color(0.10f, 0.00f, 0.22f, 0f),
                                            new Color(0.22f, 0.00f, 0.45f, 0.80f));
                    break;

                default:
                    valid = BuildGradient(Color.white, Color.white);
                    invalid = BuildGradient(new Color(0.5f, 0.5f, 0.5f), new Color(0.5f, 0.5f, 0.5f));
                    blocked = BuildGradient(Color.red, Color.red);
                    break;
            }
        }

        // Build a 2-key Gradient
        private static Gradient BuildGradient(Color start, Color end)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) });
            return g;
        }

        // Build a 3-key Gradient (start → mid → end)
        private static Gradient BuildGradient(Color start, Color mid, Color end)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(mid, 0.5f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(mid.a, 0.5f), new GradientAlphaKey(end.a, 1f) });
            return g;
        }

        private static XRRayInteractor.LineType ToXRLineType(LineCurve curve) =>
            curve switch
            {
                LineCurve.BezierCurve => XRRayInteractor.LineType.BezierCurve,
                LineCurve.ProjectileCurve => XRRayInteractor.LineType.ProjectileCurve,
                _ => XRRayInteractor.LineType.StraightLine,
            };

        private static void DrawGradientSwatch(string label, Gradient gradient)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);
            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            var swatchRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y + 1f,
                                     rect.width - EditorGUIUtility.labelWidth, rect.height - 2f);
            GUI.Label(labelRect, label);
            if (Event.current.type == EventType.Repaint)
            {
                int steps = Mathf.Max(2, (int)swatchRect.width);
                for (int i = 0; i < steps; i++)
                {
                    float t = (float)i / (steps - 1);
                    EditorGUI.DrawRect(
                        new Rect(swatchRect.x + i, swatchRect.y, 1f, swatchRect.height),
                        gradient.Evaluate(t));
                }
            }
        }

        // ── Helper: ensure a named layer exists, return its index ─────────────────
        private static int EnsureLayer(string layerName)
        {
            int idx = LayerMask.NameToLayer(layerName);
            if (idx >= 0)
                return idx;

            // Try to add the layer via SerializedObject on TagManager
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0)
            {
                Debug.LogError($"[MakeTeleportArea] Could not load TagManager. " +
                               $"Please add a layer named '{layerName}' manually (Edit > Project Settings > Tags and Layers).");
                return 0;
            }

            var so = new SerializedObject(tagManager[0]);
            var layers = so.FindProperty("layers");

            // Find a free slot (Unity reserves 0-7; user layers start at 8)
            for (int i = 8; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = layerName;
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    idx = LayerMask.NameToLayer(layerName);
                    Debug.Log($"[MakeTeleportArea] Created layer '{layerName}' at index {idx}.");
                    return idx >= 0 ? idx : 0;
                }
            }

            Debug.LogError($"[MakeTeleportArea] No free layer slots available. " +
                           $"Please add a layer named '{layerName}' manually.");
            return 0;
        }

        // ── Helper: draw a thin horizontal divider ────────────────────────────────
        private static void DrawDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f, 1f));
            EditorGUILayout.Space(4);
        }
    }
}
