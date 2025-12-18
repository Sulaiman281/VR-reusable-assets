using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace WitShells.XR
{
    public class HandControllerVisualizer : MonoBehaviour
    {
        [Header("Controller Input (Input System)")]
        [Tooltip("Input action for trigger value (float). Bind to the controller's trigger.")]
        public InputActionProperty triggerAction;

        [Tooltip("Input action for grip value (float). Bind to the controller's grip.")]
        public InputActionProperty gripAction;


        [Header("Animation")]
        [Tooltip("Animator that has 'Trigger' and 'Grip' float parameters.")]
        public Animator handAnimator;

        [Header("Visual Roots")]
        [SerializeField] private HandVisualizer handVisualizer;
        [Tooltip("Optional: visual root to toggle renderers. If null, uses this GameObject.")]
        public GameObject visualRoot;

        [Header("Events")]
        public UnityEvent<float> onTriggerValueChanged;
        public UnityEvent<float> onGripValueChanged;

        void Awake()
        {
            if (visualRoot == null)
                visualRoot = gameObject;
        }

        void OnEnable()
        {
            EnableInputActions();
            ApplyVisualToggle();
        }

        void Update()
        {
            // Update animation from input actions if enabled
            UpdateHandAnimationFromInputActions();

            // Toggle visuals based on controller OR hand tracking state
            ApplyVisualToggle();
        }

        void EnableInputActions()
        {
            if (triggerAction.action != null && !triggerAction.action.enabled)
                triggerAction.action.Enable();
            if (gripAction.action != null && !gripAction.action.enabled)
                gripAction.action.Enable();
        }

        void UpdateHandAnimationFromInputActions()
        {
            if (handAnimator == null)
                return;

            float triggerValue = 0f;
            float gripValue = 0f;

            if (triggerAction.action != null)
            {
                var v = triggerAction.action.ReadValue<float>();
                triggerValue = v;
            }

            if (gripAction.action != null)
            {
                var v = gripAction.action.ReadValue<float>();
                gripValue = v;
            }

            handAnimator.SetFloat("Trigger", triggerValue);
            handAnimator.SetFloat("Grip", gripValue);

            onTriggerValueChanged?.Invoke(triggerValue);
            onGripValueChanged?.Invoke(gripValue);
        }


        void ApplyVisualToggle()
        {
            bool isControllerTracked = Utils.Utils.IsControllerActuallyTracked();
            bool isHandTracked = handVisualizer != null && handVisualizer.anyHandTracked;

            bool enable = isControllerTracked && !isHandTracked;
            ToggleRenderers(enable);
        }

        void ToggleRenderers(bool enable)
        {
            if (visualRoot == null)
                return;

            visualRoot.SetActive(enable);
        }
    }
}
