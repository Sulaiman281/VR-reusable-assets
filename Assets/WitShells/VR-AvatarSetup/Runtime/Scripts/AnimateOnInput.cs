using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WitShells.VRAvatarSetup
{
    [Serializable]
    public class AnimationInput
    {
        public string animationPropertyName;
        public InputActionProperty action;
    }

    public class AnimateOnInput : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private List<AnimationInput> animationInputs = new List<AnimationInput>();

        private void Update()
        {
            if (animator == null || animationInputs == null || animationInputs.Count == 0)
                return;

            for (int i = 0; i < animationInputs.Count; i++)
            {
                var input = animationInputs[i];
                if (input == null || string.IsNullOrEmpty(input.animationPropertyName) || input.action.action == null)
                    continue;

                animator.SetFloat(input.animationPropertyName, input.action.action.ReadValue<float>());
            }
        }
    }
}
