using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WitShells.HandSwing
{
    public class HandSwingInput : MonoBehaviour
    {
        [System.Serializable]
        public class HandSwingEvent : UnityEvent<Vector3> { }
        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }

        [Header("Tracked Hands")]
        [Tooltip("World-space transforms to track for swing detection.")]
        public Transform leftHand;
        public Transform rightHand;

        [Header("Detection")]
        [Tooltip("Minimum linear speed (m/s) to count as a swing.")]
        [Min(0f)]
        public float swingSpeedThreshold = 1.2f;

        [Tooltip("Minimum seconds between successive triggers per hand.")]
        [Min(0f)]
        public float triggerCooldown = 0.25f;

        [Tooltip("Optional velocity smoothing, as number of frames to average.")]
        [Range(0, 10)]
        public int velocitySmoothingFrames = 2;

        [Tooltip("Speed considered full swing (for 0-1 normalization).")]
        [Min(0f)]
        public float swingSpeedMax = 3.0f;

        [Header("Events")]
        public HandSwingEvent onLeftSwing = new HandSwingEvent();
        public HandSwingEvent onRightSwing = new HandSwingEvent();
        [Tooltip("Continuous normalized swing intensity (0 idle, 1 full swing).")]
        public FloatEvent onSwing = new FloatEvent();

        [Header("Debug")]
        public bool debugDrawVelocity = false;
        [Min(0f)] public float debugVectorScale = 0.2f;
        public Color debugLeftColor = Color.cyan;
        public Color debugRightColor = Color.magenta;

        private Vector3 _prevLeftPos;
        private Vector3 _prevRightPos;
        private Vector3 _leftVelocity;
        private Vector3 _rightVelocity;
        private readonly Queue<Vector3> _leftVelHistory = new Queue<Vector3>();
        private readonly Queue<Vector3> _rightVelHistory = new Queue<Vector3>();
        private float _lastLeftTriggerTime = -999f;
        private float _lastRightTriggerTime = -999f;
        private float _swingIntensity = 0f;

        public Vector3 LeftVelocity => _leftVelocity;
        public Vector3 RightVelocity => _rightVelocity;
        public float SwingIntensity => _swingIntensity;

        /// <summary>
        /// Assigns the hand transforms at runtime.
        /// </summary>
        public void SetHands(Transform left, Transform right)
        {
            leftHand = left;
            rightHand = right;
            ResetPositions();
        }

        private void OnEnable()
        {
            ResetPositions();
        }

        private void ResetPositions()
        {
            if (leftHand != null) _prevLeftPos = leftHand.position;
            if (rightHand != null) _prevRightPos = rightHand.position;
            _leftVelHistory.Clear();
            _rightVelHistory.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (leftHand != null)
            {
                Vector3 vel = (leftHand.position - _prevLeftPos) / dt;
                _prevLeftPos = leftHand.position;
                _leftVelocity = SmoothVelocity(vel, _leftVelHistory, velocitySmoothingFrames);
                TryTriggerSwing(_leftVelocity, true);
            }

            if (rightHand != null)
            {
                Vector3 vel = (rightHand.position - _prevRightPos) / dt;
                _prevRightPos = rightHand.position;
                _rightVelocity = SmoothVelocity(vel, _rightVelHistory, velocitySmoothingFrames);
                TryTriggerSwing(_rightVelocity, false);
            }

            if (debugDrawVelocity)
            {
                if (leftHand != null)
                    Debug.DrawRay(leftHand.position, _leftVelocity * debugVectorScale, debugLeftColor);
                if (rightHand != null)
                    Debug.DrawRay(rightHand.position, _rightVelocity * debugVectorScale, debugRightColor);
            }

            // Compute normalized swing intensity and raise single float event
            float leftNorm = (leftHand != null) ? NormalizeSpeed(_leftVelocity.magnitude) : 0f;
            float rightNorm = (rightHand != null) ? NormalizeSpeed(_rightVelocity.magnitude) : 0f;
            _swingIntensity = Mathf.Max(leftNorm, rightNorm);
            onSwing.Invoke(_swingIntensity);
        }

        private static Vector3 SmoothVelocity(Vector3 vel, Queue<Vector3> history, int frames)
        {
            if (frames <= 0) return vel;
            history.Enqueue(vel);
            while (history.Count > frames) history.Dequeue();
            Vector3 sum = Vector3.zero;
            foreach (var v in history) sum += v;
            return sum / history.Count;
        }

        private void TryTriggerSwing(Vector3 velocity, bool isLeft)
        {
            float speed = velocity.magnitude;
            if (speed < swingSpeedThreshold) return;

            float now = Time.time;
            if (isLeft)
            {
                if (now - _lastLeftTriggerTime < triggerCooldown) return;
                _lastLeftTriggerTime = now;
                onLeftSwing.Invoke(velocity);
            }
            else
            {
                if (now - _lastRightTriggerTime < triggerCooldown) return;
                _lastRightTriggerTime = now;
                onRightSwing.Invoke(velocity);
            }
        }

        private float NormalizeSpeed(float speed)
        {
            // If max <= threshold, treat threshold as binary
            if (swingSpeedMax <= swingSpeedThreshold)
                return speed >= swingSpeedThreshold ? 1f : 0f;

            if (speed <= swingSpeedThreshold) return 0f;
            return Mathf.Clamp01((speed - swingSpeedThreshold) / (swingSpeedMax - swingSpeedThreshold));
        }
    }
}
