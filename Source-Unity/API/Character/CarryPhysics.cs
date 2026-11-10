#if UNITY_2017_1_OR_NEWER

using System;

using UnityEngine;

namespace Swole.API.Unity
{
    /// <summary>
    /// Tunables for the rotational springiness of a carried object.
    /// </summary>
    [Serializable]
    public class CarryRotationSpringSettings
    {
        [Tooltip("Master scale of the rotational reaction. 0 disables it.")]
        [Range(0f, 2f)] public float strength = 1f;
        [Tooltip("Spring stiffness pulling the object back to its held orientation.")]
        public float stiffness = 60f;
        [Tooltip("Damping ratio of the rotation spring (1 = critically damped, lower is bouncier).")]
        [Range(0.05f, 2f)] public float dampingRatio = 0.35f;
        [Tooltip("How strongly linear acceleration of the grip tilts the object (pitch/roll).")]
        public float accelerationTilt = 2.5f;
        [Tooltip("How strongly the carrier's turning (yaw rate) twists the object, like a hanging arm drifting around its forearm.")]
        public float yawTwist = 0.6f;
        [Tooltip("Maximum tilt (pitch/roll) in degrees.")]
        public float maxTilt = 12f;
        [Tooltip("Maximum twist in degrees.")]
        public float maxTwist = 15f;
        [Tooltip("Bounce kept when the rotation hits a limit.")]
        [Range(0f, 0.8f)] public float limitElasticity = 0.4f;
        [Tooltip("Random variation applied to driving forces to avoid mechanical motion.")]
        [Range(0f, 1f)] public float randomization = 0.1f;
    }

    /// <summary>
    /// Shared rotation spring used by all carry styles. Driven by grip acceleration (tilt) and carrier yaw rate (twist).
    /// Inspired by the wrist twist / inertia tilt of <see cref="SingleArmCarryPhysics"/>.
    /// </summary>
    [Serializable]
    public struct CarryRotationSpring
    {
        private Vector3 angles;   // x = pitch, y = twist, z = roll (degrees, in the reference frame)
        private Vector3 velocity;
        private Vector3 lastPosition;
        private Vector3 lastVelocity;
        private float lastYaw;
        private bool initialized;

        public void Reset()
        {
            angles = velocity = lastVelocity = Vector3.zero;
            initialized = false;
        }

        /// <summary>Advances the spring and returns the offset rotation, expressed in the given reference frame (forward = z, up = y).</summary>
        public Quaternion Update(CarryRotationSpringSettings settings, Vector3 gripPosition, Quaternion referenceFrame, float carrierYaw, float massScale, float deltaTime)
        {
            if (settings == null || settings.strength <= 0f || deltaTime <= 0f)
            {
                lastPosition = gripPosition;
                lastYaw = carrierYaw;
                return Quaternion.identity;
            }

            if (!initialized)
            {
                lastPosition = gripPosition;
                lastVelocity = Vector3.zero;
                lastYaw = carrierYaw;
                initialized = true;
            }

            Vector3 worldVelocity = (gripPosition - lastPosition) / deltaTime;
            Vector3 worldAccel = (worldVelocity - lastVelocity) / deltaTime;
            lastPosition = gripPosition;
            lastVelocity = worldVelocity;
            worldAccel = Vector3.ClampMagnitude(worldAccel, 60f);

            float yawRate = Mathf.DeltaAngle(lastYaw, carrierYaw) / deltaTime;
            lastYaw = carrierYaw;

            Vector3 localAccel = Quaternion.Inverse(referenceFrame) * worldAccel;
            float rand = 1f - settings.randomization * UnityEngine.Random.value;
            float inertia = Mathf.Max(0.1f, massScale);

            // inertia: the object lags its grip, so it tilts against the acceleration
            Vector3 drive = new Vector3(
                -localAccel.z * settings.accelerationTilt,
                -yawRate * settings.yawTwist * 0.1f,
                localAccel.x * settings.accelerationTilt) * rand;

            float stiffness = Mathf.Max(0f, settings.stiffness) / inertia;
            float damping = 2f * Mathf.Sqrt(stiffness) * settings.dampingRatio;
            velocity += (drive - stiffness * angles - damping * velocity) * deltaTime;
            angles += velocity * deltaTime;

            angles.x = Limit(angles.x, ref velocity.x, settings.maxTilt, settings.limitElasticity);
            angles.y = Limit(angles.y, ref velocity.y, settings.maxTwist, settings.limitElasticity);
            angles.z = Limit(angles.z, ref velocity.z, settings.maxTilt, settings.limitElasticity);

            Vector3 a = angles * settings.strength;
            return Quaternion.AngleAxis(a.y, Vector3.up) * Quaternion.AngleAxis(a.x, Vector3.right) * Quaternion.AngleAxis(a.z, Vector3.forward);
        }

        private static float Limit(float value, ref float vel, float max, float elasticity)
        {
            if (max <= 0f) { vel = 0f; return 0f; }
            if (value > max) { if (vel > 0f) vel = -vel * elasticity; return max; }
            if (value < -max) { if (vel < 0f) vel = -vel * elasticity; return -max; }
            return value;
        }
    }

    /// <summary>
    /// A set of capsules attached to body bones used to keep carried objects and hands out of the body.
    /// </summary>
    [Serializable]
    public class CarryBodyCollision
    {
        [Serializable]
        public struct Capsule
        {
            public Transform boneA;
            public Transform boneB;
            public float radius;
        }

        public Capsule[] capsules = new Capsule[0];

        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float len = ab.sqrMagnitude;
            if (len < 1e-8f) return a;
            return a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
        }

        /// <summary>Pushes a sphere out of all body capsules. Returns the total push applied.</summary>
        public Vector3 ResolveSphere(ref Vector3 center, float radius, Vector3 fallbackNormal, int iterations = 2)
        {
            Vector3 total = Vector3.zero;
            if (capsules == null) return total;
            for (int it = 0; it < iterations; it++)
            {
                bool any = false;
                for (int i = 0; i < capsules.Length; i++)
                {
                    var c = capsules[i];
                    if (c.boneA == null || c.radius <= 0f) continue;
                    Vector3 a = c.boneA.position;
                    Vector3 b = c.boneB != null ? c.boneB.position : a;
                    Vector3 closest = ClosestPointOnSegment(a, b, center);
                    Vector3 offset = center - closest;
                    float minDist = c.radius + radius;
                    float dist = offset.magnitude;
                    if (dist >= minDist) continue;
                    Vector3 normal = dist > 1e-4f ? offset / dist : fallbackNormal;
                    Vector3 push = normal * (minDist - dist);
                    center += push;
                    total += push;
                    any = true;
                }
                if (!any) break;
            }
            return total;
        }

        /// <summary>Pushes a carried object (approximated by a sphere at its bounds center) out of the body, preserving its rotation.</summary>
        public Vector3 ResolveObject(Vector3 objectPosition, Vector3 boundsCenter, float radius, Vector3 fallbackNormal)
        {
            Vector3 c = boundsCenter;
            Vector3 push = ResolveSphere(ref c, radius, fallbackNormal);
            return objectPosition + push;
        }
    }
}

#endif
