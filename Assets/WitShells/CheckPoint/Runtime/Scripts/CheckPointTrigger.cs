namespace WitShells.CheckPoint
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable]
    public struct CheckPointTriggerData
    {
        public string Identifier;
        public Vector3 Position;
        public bool IsActive;
    }

    [RequireComponent(typeof(Collider))]
    public class CheckPointTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask triggerLayerMask;

        public CheckPointTriggerData Data;

        [Header("Events")]
        public UnityEvent<GameObject> OnObjectEntered;
        public UnityEvent<GameObject> OnObjectExited;



#if UNITY_EDITOR

        private void OnValidate()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

#endif

        private void OnTriggerEnter(Collider other)
        {
            if (IsInLayerMask(other.gameObject, triggerLayerMask))
            {
                OnObjectEntered.Invoke(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsInLayerMask(other.gameObject, triggerLayerMask))
            {
                OnObjectExited.Invoke(other.gameObject);
            }
        }


        private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
        {
            // Use != 0 instead of > 0: layer 31 sets the sign bit, making the result negative
            return (layerMask.value & (1 << obj.layer)) != 0;
        }
    }
}