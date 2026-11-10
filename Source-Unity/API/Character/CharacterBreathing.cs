#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    /// <summary>
    /// Procedural breathing that offsets torso bones (e.g. gut.top, gut.upper, gut.lower, chest/shoulder bones) on top of animation.
    /// Blends between Normal, Exerting and Out Of Breath modes using a breathlessness value (0 = normal, 0.5 = exerting, 1 = out of breath)
    /// that is read from input providers (channel "breathlessness") and optionally raised by a normalized heart rate (channel "heartRate").
    /// Also acts as an input provider ("breathInhale", "breathExhale", "breathPhase", "breathlessness") so auto flexers can react to breathing (e.g. abs bracing on forced exhales).
    /// </summary>
    public class CharacterBreathing : ExecutableBehaviourObject, IAutoFlexInputProvider
    {
        /// <summary>Update before ik so any changes made to spine/chest bones don't affect ik enabled arms.</summary>
        public static int ExecutionPriority => CustomIKManagerUpdater.ExecutionPriority - 10;
        public override int Priority => ExecutionPriority;

        public enum BreathRegion
        {
            Belly, Chest, Shoulders
        }

        [Serializable]
        public class BreathBone
        {
            public Transform transform;
            public BreathRegion region;
            [Tooltip("Offsets applied at full inhale (scaled by mode depth and region weight).")]
            public Vector3 localPositionOffset;
            public Vector3 localRotationOffsetEuler;
            public Vector3 localScaleOffset;

            [NonSerialized] public Vector3 appliedPos;
            [NonSerialized] public Quaternion appliedRot = Quaternion.identity;
            [NonSerialized] public Vector3 appliedScale;
            [NonSerialized] public Vector3 lastPos;
            [NonSerialized] public Quaternion lastRot;
            [NonSerialized] public Vector3 lastScale;
            [NonSerialized] public bool hasApplied;
        }

        [Serializable]
        public class BreathMode
        {
            [Tooltip("Breaths per minute.")]
            public float rate = 14f;
            [Tooltip("Overall depth multiplier.")]
            public float depth = 1f;
            [Tooltip("Fraction of the cycle spent inhaling.")]
            [Range(0.1f, 0.9f)] public float inhaleFraction = 0.4f;
            [Tooltip("Fraction of the cycle held at the bottom of the exhale (pause).")]
            [Range(0f, 0.6f)] public float restFraction = 0.25f;
            public float bellyWeight = 1f;
            public float chestWeight = 0.3f;
            public float shoulderWeight = 0f;
            [Tooltip("Random variation of rate and depth per breath (0-1).")]
            [Range(0f, 0.5f)] public float irregularity = 0.05f;
            [Tooltip("How forcefully the exhale is pushed out (feeds the 'breathExhale' channel).")]
            [Range(0f, 1f)] public float forcedExhale = 0f;

            public static BreathMode Lerp(BreathMode a, BreathMode b, float t, BreathMode result)
            {
                result.rate = Mathf.Lerp(a.rate, b.rate, t);
                result.depth = Mathf.Lerp(a.depth, b.depth, t);
                result.inhaleFraction = Mathf.Lerp(a.inhaleFraction, b.inhaleFraction, t);
                result.restFraction = Mathf.Lerp(a.restFraction, b.restFraction, t);
                result.bellyWeight = Mathf.Lerp(a.bellyWeight, b.bellyWeight, t);
                result.chestWeight = Mathf.Lerp(a.chestWeight, b.chestWeight, t);
                result.shoulderWeight = Mathf.Lerp(a.shoulderWeight, b.shoulderWeight, t);
                result.irregularity = Mathf.Lerp(a.irregularity, b.irregularity, t);
                result.forcedExhale = Mathf.Lerp(a.forcedExhale, b.forcedExhale, t);
                return result;
            }
        }

        public List<BreathBone> bones = new List<BreathBone>();

        [Header("Modes")]
        public BreathMode normal = new BreathMode() { rate = 14f, depth = 0.6f, inhaleFraction = 0.4f, restFraction = 0.25f, bellyWeight = 1f, chestWeight = 0.25f, shoulderWeight = 0f, irregularity = 0.08f, forcedExhale = 0f };
        public BreathMode exerting = new BreathMode() { rate = 26f, depth = 1f, inhaleFraction = 0.45f, restFraction = 0.08f, bellyWeight = 1f, chestWeight = 0.7f, shoulderWeight = 0.25f, irregularity = 0.06f, forcedExhale = 0.35f };
        public BreathMode outOfBreath = new BreathMode() { rate = 44f, depth = 1.35f, inhaleFraction = 0.42f, restFraction = 0f, bellyWeight = 0.9f, chestWeight = 1f, shoulderWeight = 1f, irregularity = 0.15f, forcedExhale = 0.8f };

        [Header("Breathlessness")]
        [Tooltip("Components implementing IAutoFlexInputProvider. Found automatically on this hierarchy if empty.")]
        public List<MonoBehaviour> inputProviders = new List<MonoBehaviour>();
        public string breathlessnessChannel = "breathlessness";
        public string heartRateChannel = "heartRate";
        [Tooltip("Used when no provider supplies the breathlessness channel.")]
        [Range(0f, 1f)] public float manualBreathlessness;
        [Tooltip("Normalized heart rate (0-1) to minimum breathlessness. Lets a racing heart keep breathing heavy.")]
        public AnimationCurve heartRateToBreathlessness = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 0.1f), new Keyframe(0.7f, 0.55f), new Keyframe(1f, 0.85f));
        [Tooltip("How much heart rate also scales the breath rate (0 = none).")]
        [Range(0f, 1f)] public float heartRateToRateInfluence = 0.25f;
        [Tooltip("Breathlessness per second when rising.")]
        public float breathlessnessRiseSpeed = 1.5f;
        [Tooltip("Breathlessness per second when recovering.")]
        public float breathlessnessFallSpeed = 0.2f;

        [Header("Output")]
        [AnimatableProperty(true, 1f), Range(0f, 2f)] public float weight = 1f;

        [Header("Region Blend"), Tooltip("Per-region weights (0 = blended out). Animatable so animations can blend breathing groups out where necessary.")]
        [AnimatableProperty(true, 1f), Range(0f, 1f)] public float bellyBlend = 1f;
        [AnimatableProperty(true, 1f), Range(0f, 1f)] public float chestBlend = 1f;
        [AnimatableProperty(true, 1f), Range(0f, 1f)] public float shoulderBlend = 1f;

        public void SetRegionWeight(BreathRegion region, float value)
        {
            value = Mathf.Clamp01(value);
            switch (region)
            {
                case BreathRegion.Belly: bellyBlend = value; break;
                case BreathRegion.Chest: chestBlend = value; break;
                default: shoulderBlend = value; break;
            }
        }
        public float GetRegionWeight(BreathRegion region) => region == BreathRegion.Belly ? bellyBlend : (region == BreathRegion.Chest ? chestBlend : shoulderBlend);

        [Header("Debug (read only)")]
        public float breathlessness;
        public float heartRate01;
        public float phase;
        public float inhale;
        public float exhaleForce;

        [NonSerialized]
        private readonly BreathMode current = new BreathMode();
        private float breathRateJitter = 1f;
        private float breathDepthJitter = 1f;

        protected void Awake()
        {
            if (inputProviders == null) inputProviders = new List<MonoBehaviour>();
            inputProviders.RemoveAll(p => p == null);
            if (inputProviders.Count == 0) AutoFlexInputUtility.FindProviders(transform.root, inputProviders, this);
        }

        protected void OnEnable()
        {
            SingletonCallStack.Insert(this);
        }

        protected void OnDisable()
        {
            SingletonCallStack.Remove(this);
            if (bones == null) return;
            foreach (var bone in bones) Remove(bone);
        }

        public bool TryGetAutoFlexInput(string channel, out float value)
        {
            switch (channel)
            {
                case "breathInhale": value = inhale; return true;
                case "breathExhale": value = exhaleForce; return true;
                case "breathPhase": value = phase; return true;
                case "breathRawBreathlessness": value = breathlessness; return true;
            }
            value = 0f;
            return false;
        }

        private void UpdateBreathlessness(float deltaTime)
        {
            float target = AutoFlexInputUtility.TryGetInput(inputProviders, breathlessnessChannel, out float b) ? b : manualBreathlessness;
            heartRate01 = AutoFlexInputUtility.TryGetInput(inputProviders, heartRateChannel, out float hr) ? Mathf.Clamp01(hr) : 0f;
            if (heartRateToBreathlessness != null && heartRateToBreathlessness.length > 0) target = Mathf.Max(target, heartRateToBreathlessness.Evaluate(heartRate01));
            target = Mathf.Clamp01(target);
            breathlessness = Mathf.MoveTowards(breathlessness, target, deltaTime * (target > breathlessness ? breathlessnessRiseSpeed : breathlessnessFallSpeed));
        }

        private void UpdateMode()
        {
            if (breathlessness <= 0.5f) BreathMode.Lerp(normal, exerting, Mathf.SmoothStep(0f, 1f, breathlessness * 2f), current);
            else BreathMode.Lerp(exerting, outOfBreath, Mathf.SmoothStep(0f, 1f, (breathlessness - 0.5f) * 2f), current);
        }

        private float EvaluateWave(float p, out float exhaleAmount)
        {
            float inhaleEnd = current.inhaleFraction;
            float exhaleEnd = Mathf.Max(inhaleEnd + 0.01f, 1f - current.restFraction);
            exhaleAmount = 0f;
            if (p < inhaleEnd) return Mathf.SmoothStep(0f, 1f, p / inhaleEnd);
            if (p < exhaleEnd)
            {
                float t = (p - inhaleEnd) / (exhaleEnd - inhaleEnd);
                exhaleAmount = Mathf.Sin(t * Mathf.PI);
                return 1f - Mathf.SmoothStep(0f, 1f, t);
            }
            return 0f;
        }

        public override void OnLateUpdate()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            UpdateBreathlessness(deltaTime);
            UpdateMode();

            float rate = current.rate * Mathf.Lerp(1f, Mathf.Lerp(0.8f, 1.25f, heartRate01), heartRateToRateInfluence) * breathRateJitter;
            phase += deltaTime * Mathf.Max(0.01f, rate) / 60f;
            if (phase >= 1f)
            {
                phase -= Mathf.Floor(phase);
                breathRateJitter = 1f + UnityEngine.Random.Range(-current.irregularity, current.irregularity);
                breathDepthJitter = 1f + UnityEngine.Random.Range(-current.irregularity, current.irregularity);
            }

            inhale = EvaluateWave(phase, out float exhaleAmount);
            exhaleForce = exhaleAmount * current.forcedExhale;

            if (bones == null) return;
            float amount = inhale * current.depth * breathDepthJitter * weight;
            float bellyW = current.bellyWeight * Mathf.Clamp01(bellyBlend);
            float chestW = current.chestWeight * Mathf.Clamp01(chestBlend);
            float shoulderW = current.shoulderWeight * Mathf.Clamp01(shoulderBlend);
            foreach (var bone in bones)
            {
                if (bone == null || bone.transform == null) continue;
                float regionWeight = bone.region == BreathRegion.Belly ? bellyW : (bone.region == BreathRegion.Chest ? chestW : shoulderW);
                Apply(bone, amount * regionWeight);
            }
        }

        private static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-12f;

        private static void Remove(BreathBone bone)
        {
            if (bone == null || bone.transform == null || !bone.hasApplied) return;
            var t = bone.transform;
            t.GetLocalPositionAndRotation(out var pos, out var rot);
            // Only undo the last offset if nothing else (e.g. the animator) has rewritten the transform since.
            if (Same(pos, bone.lastPos) && Quaternion.Dot(rot, bone.lastRot) > 0.999999f && Same(t.localScale, bone.lastScale))
            {
                t.SetLocalPositionAndRotation(pos - bone.appliedPos, Quaternion.Inverse(bone.appliedRot) * rot);
                t.localScale = t.localScale - bone.appliedScale;
            }
            bone.hasApplied = false;
        }

        private static void Apply(BreathBone bone, float amount)
        {
            Remove(bone);
            var t = bone.transform;
            bone.appliedPos = bone.localPositionOffset * amount;
            bone.appliedRot = Quaternion.Euler(bone.localRotationOffsetEuler * amount);
            bone.appliedScale = bone.localScaleOffset * amount;

            t.GetLocalPositionAndRotation(out var pos, out var rot);
            pos += bone.appliedPos;
            rot = bone.appliedRot * rot;
            var scale = t.localScale + bone.appliedScale;
            t.SetLocalPositionAndRotation(pos, rot);
            t.localScale = scale;

            bone.lastPos = pos;
            bone.lastRot = rot;
            bone.lastScale = scale;
            bone.hasApplied = true;
        }
    }
}

#endif
