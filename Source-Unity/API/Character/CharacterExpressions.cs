#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class CharacterExpressions : MonoBehaviour
    {

        [SerializeField, Header("References")]
        protected CustomAnimator animator;
        public void SetAnimator(CustomAnimator animator)
        {
            if (this.animator != null)
            {
                animator.RemoveListener(CustomAnimator.BehaviourEvent.OnEnable, OnAnimatorEnabled);
                animator.RemoveListener(CustomAnimator.BehaviourEvent.OnDisable, OnAnimatorDisabled); 
            }

            this.animator = animator;

            if (this.animator != null)
            {
                this.animator.AddListener(CustomAnimator.BehaviourEvent.OnEnable, OnAnimatorEnabled);
                this.animator.AddListener(CustomAnimator.BehaviourEvent.OnDisable, OnAnimatorDisabled);

                if (this.animator.isActiveAndEnabled)
                {
                    OnAnimatorEnabled();
                } 
                else
                {
                    OnAnimatorDisabled();
                }
            } 
        }
        public CustomAnimator Animator
        {
            get => animator;
            set => SetAnimator(value);
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (headBone != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(headBone.TransformPoint(headSettings.eyesCenter), 0.05f);
                Gizmos.color = Color.green;
                Gizmos.DrawRay(headBone.position, headBone.TransformDirection(headSettings.upAxis) * 0.25f);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(headBone.position, headBone.TransformDirection(headSettings.rightAxis) * 0.25f);
            } 

            foreach (var eye in eyes)
            {
                if (eye.eyeball.eyeballBone != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(eye.eyeball.eyeballBone.TransformPoint(eye.eyeball.settings.corneaCenter), eye.eyeball.settings.corneaRadius);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(eye.eyeball.eyeballBone.position, eye.eyeball.eyeballBone.TransformDirection(eye.eyeball.settings.localForwardAxis) * 0.05f);

                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(eye.eyeball.eyeballBone.position, eye.eyeball.eyeballBone.TransformDirection(eye.eyeball.settings.localRollVerticalAxis) * 0.05f);

                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(eye.eyeball.eyeballBone.position, eye.eyeball.eyeballBone.TransformDirection(eye.eyeball.settings.localRollHorizontalAxis) * 0.05f);
                }

                void DrawEyelidBone(Eyelid eyelid)
                {
                    if (eyelid.eyelidBone != null)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(eyelid.eyelidBone.position, eyelid.settings.radius);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(eyelid.eyelidBone.position, eyelid.eyelidBone.TransformDirection(eyelid.settings.localPushFwdAxis) * eyelid.settings.radius * 1.5f);

                        Gizmos.color = Color.green;
                        Gizmos.DrawRay(eyelid.eyelidBone.position, eyelid.eyelidBone.TransformDirection(eyelid.settings.localPushVerticalAxis) * eyelid.settings.radius * 1.5f);

                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(eyelid.eyelidBone.position, eyelid.eyelidBone.TransformDirection(eyelid.settings.localPushHorizontalAxis) * eyelid.settings.radius * 1.5f); 
                    }
                }

                DrawEyelidBone(eye.eyelidUpperInnerBone);
                DrawEyelidBone(eye.eyelidUpperMiddleBone);
                DrawEyelidBone(eye.eyelidUpperOuterBone);

                DrawEyelidBone(eye.eyelidLowerInnerBone);
                DrawEyelidBone(eye.eyelidLowerMiddleBone);
                DrawEyelidBone(eye.eyelidLowerOuterBone); 
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(eyeTrackingBone.position, 0.1f);
        }
#endif

        [Serializable]
        public class Eye
        {
            [SerializeField, Range(0, 1)]
            protected float closeFactor;
            public void SetCloseFactor(float value, bool syncJobSettings = true)
            {
                eyeball.settings.closeFactor = closeFactor = value; 

                if (syncJobSettings)
                {
                    SyncJobEyeballSettings();
                }
            }

            public Eyeball eyeball;

            public Eyelid eyelidUpperInnerBone;
            public Eyelid eyelidUpperMiddleBone;
            public Eyelid eyelidUpperOuterBone;

            public Eyelid eyelidLowerInnerBone;
            public Eyelid eyelidLowerMiddleBone;
            public Eyelid eyelidLowerOuterBone;

            [NonSerialized]
            protected CharacterExpressionsJobs.IndexReferenceEye indexReference;
            public void SetIndexReference(CharacterExpressionsJobs.IndexReferenceEye indexReference)
            {
                if (this.indexReference != null && !ReferenceEquals(this.indexReference, indexReference)) DisposeIndexReference();

                this.indexReference = indexReference;
            }

            public void DisposeIndexReference()
            {
                if (indexReference == null) return;

                if (indexReference.IsValid) indexReference.Dispose();
                indexReference = null;
            }

            public void SetUndoPreviousOffset(bool value, bool syncJobSettings = true)
            {
                var eyeballSettings = eyeball.settings;
                eyeballSettings.undoPreviousOffset = value;  
                eyeball.settings = eyeballSettings;

                eyelidUpperInnerBone.settings.undoPreviousOffset = value;
                eyelidUpperMiddleBone.settings.undoPreviousOffset = value;
                eyelidUpperOuterBone.settings.undoPreviousOffset = value;

                eyelidLowerInnerBone.settings.undoPreviousOffset = value;
                eyelidLowerMiddleBone.settings.undoPreviousOffset = value;
                eyelidLowerOuterBone.settings.undoPreviousOffset = value;

                if (syncJobSettings) 
                {
                    SyncJobEyeballSettings(); 
                    SyncJobEyelidSettings();
                }
            }

            public void SyncJobEyeballSettings()
            {
                if (indexReference != null && indexReference.IsValid)
                {
                    indexReference.SetEyeballSettings(eyeball.settings); 
                }
            }
            public void SyncJobEyelidSettings()
            {
                if (indexReference != null && indexReference.IsValid)
                {
                    indexReference.SetEyelidSettings(0, eyelidUpperInnerBone.settings);
                    indexReference.SetEyelidSettings(1, eyelidUpperMiddleBone.settings);
                    indexReference.SetEyelidSettings(2, eyelidUpperOuterBone.settings);

                    indexReference.SetEyelidSettings(3, eyelidLowerInnerBone.settings);
                    indexReference.SetEyelidSettings(4, eyelidLowerMiddleBone.settings);
                    indexReference.SetEyelidSettings(5, eyelidLowerOuterBone.settings);
                }
            }
        }
        [Serializable]
        public struct HeadSettings
        {
            public HeadSettings(float3 eyesCenter, float3 upAxis, float3 rightAxis)
            {
                this.eyesCenter = eyesCenter;
                this.upAxis = upAxis;
                this.rightAxis = rightAxis;
            }

            public float3 eyesCenter;
            public float3 upAxis;
            public float3 rightAxis;
        }
        [Serializable]
        public struct Eyeball
        {
            public Transform eyeballBone;
            public EyeballSettings settings;
        }
        [Serializable]
        public struct EyeballSettings
        {
            public float3 corneaCenter;
            public float corneaRadius;

            public float maxRadiansYaw;
            public float maxRadiansPitch;

            public float3 localForwardAxis;
            public float3 localRollVerticalAxis;
            public float3 localRollHorizontalAxis;

            [NonSerialized]
            public int eyeTrackingBoneIndex;
            [NonSerialized]
            public bool undoPreviousOffset;
            [NonSerialized]
            public float closeFactor;
        }
        [Serializable]
        public struct Eyelid
        {
            public Transform eyelidBone;
            public EyelidSettings settings;
        }
        [Serializable]
        public struct EyelidSettings
        {
            public float radius;

            [NonSerialized]
            public float mix;
            [NonSerialized]
            public bool undoPreviousOffset;
            [NonSerialized]
            public float defaultDistanceFromCornea;

            public float3 localPushFwdAxis;
            public float pushFwdRollNear;
            public float pushFwdRollAway;
            public float closeFwd;

            public float3 localPushVerticalAxis;
            public float pushVerticalRollUp;
            public float pushVerticalRollDown;
            public float closeVertical;

            public float3 localPushHorizontalAxis;
            public float pushHorizontalRollIn;
            public float pushHorizontalRollOut;

            [NonSerialized]
            public float3 pushFwdAxisHS;
            [NonSerialized]
            public float3 pushVerticalAxisHS; 
            [NonSerialized]
            public float3 pushHorizontalAxisHS;
        }

        [Serializable]
        public struct EyeControlSettings
        {
            public EyeControlSettings(float eyeTrackingMix, float eyeBlinkMix, float eyeBlinkFrequencyMin, float eyeBlinkFrequencyMax, float eyeJitterMix, float eyeJitterOffsetMax, float eyeDartRangeHorizontal, float eyeDartRangeVertical, float eyeDartFrequencyMin, float eyeDartFrequencyMax, float eyeDartDistanceMin, float eyeDartDistanceMax, float eyeDartMix, float eyelidMovementMix, float eyeControlMix)
            {
                this.eyeTrackingMix = eyeTrackingMix;
                this.eyeBlinkMix = eyeBlinkMix;
                this.eyeBlinkFrequencyMin = eyeBlinkFrequencyMin;
                this.eyeBlinkFrequencyMax = eyeBlinkFrequencyMax;
                this.eyeJitterMix = eyeJitterMix;
                this.eyeJitterOffsetMax = eyeJitterOffsetMax;
                this.eyeDartRangeHorizontal = eyeDartRangeHorizontal;
                this.eyeDartRangeVertical = eyeDartRangeVertical;
                this.eyeDartFrequencyMin = eyeDartFrequencyMin;
                this.eyeDartFrequencyMax = eyeDartFrequencyMax;
                this.eyeDartDistanceMin = eyeDartDistanceMin;
                this.eyeDartDistanceMax = eyeDartDistanceMax;
                this.eyeDartMix = eyeDartMix;
                this.eyelidMovementMix = eyelidMovementMix;
                this.eyeControlMix = eyeControlMix;
            }

            [AnimatableProperty, Range(0, 1)]
            public float eyeTrackingMix;
            [AnimatableProperty, Range(0, 1)]
            public float eyeBlinkMix;
            [AnimatableProperty(true, 2f)]
            public float eyeBlinkFrequencyMin;
            [AnimatableProperty(true, 8f)]
            public float eyeBlinkFrequencyMax;
            [AnimatableProperty, Range(0, 1)]
            public float eyeJitterMix;
            [AnimatableProperty(true, 0.0125f)]
            public float eyeJitterOffsetMax;
            [AnimatableProperty(true, 0.5f), Range(0, 1)]
            public float eyeDartRangeHorizontal;
            [AnimatableProperty(true, 0.5f), Range(0, 1)]
            public float eyeDartRangeVertical;
            [AnimatableProperty(true, 0.2f)]
            public float eyeDartFrequencyMin;
            [AnimatableProperty(true, 6f)]
            public float eyeDartFrequencyMax;
            [AnimatableProperty(true, 0.01f)]
            public float eyeDartDistanceMin;
            [AnimatableProperty(true, 0.3f)]
            public float eyeDartDistanceMax;
            [AnimatableProperty, Range(0, 1)]
            public float eyeDartMix; // eye darting should not move eyeTrackingBone, but instead have a second vector layer value that is applied on top of the position of the eyeTrackingBone
            [AnimatableProperty, Range(0, 1)]
            public float eyelidMovementMix;
            [AnimatableProperty, Range(0, 1)]
            public float eyeControlMix;
        }

        [Header("Eyes")]
        public Transform headBone;
        public HeadSettings headSettings = new HeadSettings(float3.zero, new float3(0, 1, 0), new float3(1, 0, 0));
        public Transform eyeTrackingBone;
        public EyeControlSettings eyeControlSettings = new EyeControlSettings(1, 1, 2f, 8f, 1, 0.0125f, 0.5f, 0.5f, 0.2f, 6f, 0.01f, 0.3f, 1f, 1f, 1f);
        public EyeControlSettings GetEyeControlSettings() => eyeControlSettings;

        public List<Eye> eyes = new List<Eye>();

        protected virtual void Awake()
        {
            if (eyes != null)
            {
                for(int a = 0; a < eyes.Count; a++)
                {
                    var eye = eyes[a];

                    var eyeballSettings = eye.eyeball.settings;
                    eyeballSettings.undoPreviousOffset = true;
                    eye.eyeball.settings = eyeballSettings;

                    Eyelid InitializeEyelid(Eyelid eyelid)
                    {
                        var parentTransform = eyelid.eyelidBone.parent;

                        var settings = eyelid.settings;

                        settings.mix = 1;
                        settings.undoPreviousOffset = true;

                        settings.defaultDistanceFromCornea = math.max(0, math.distance(eye.eyeball.eyeballBone.TransformPoint(eye.eyeball.settings.corneaCenter), eyelid.eyelidBone.position) - eyelid.settings.radius);

                        var pushFwdAxisWorld = parentTransform.TransformDirection(settings.localPushFwdAxis);
                        var pushVerticalAxis = parentTransform.TransformDirection(settings.localPushVerticalAxis);
                        var pushHorizontalAxis = parentTransform.TransformDirection(settings.localPushHorizontalAxis);

                        settings.pushFwdAxisHS = headBone == null ? settings.localPushFwdAxis : headBone.InverseTransformDirection(pushFwdAxisWorld);
                        settings.pushVerticalAxisHS = headBone == null ? settings.localPushVerticalAxis : headBone.InverseTransformDirection(pushVerticalAxis);
                        settings.pushHorizontalAxisHS = headBone == null ? settings.localPushHorizontalAxis : headBone.InverseTransformDirection(pushHorizontalAxis);
                         
                        eyelid.settings = settings; 

                        return eyelid;
                    }

                    eye.eyelidUpperInnerBone = InitializeEyelid(eye.eyelidUpperInnerBone);
                    eye.eyelidUpperMiddleBone = InitializeEyelid(eye.eyelidUpperMiddleBone);
                    eye.eyelidUpperOuterBone = InitializeEyelid(eye.eyelidUpperOuterBone);

                    eye.eyelidLowerInnerBone = InitializeEyelid(eye.eyelidLowerInnerBone);
                    eye.eyelidLowerMiddleBone = InitializeEyelid(eye.eyelidLowerMiddleBone);
                    eye.eyelidLowerOuterBone = InitializeEyelid(eye.eyelidLowerOuterBone);

                    eyes[a] = eye;
                }
            }

            if (animator == null) animator = GetComponent<CustomAnimator>();
            SetAnimator(animator);
        }

        [NonSerialized]
        protected bool initialized = false;
        protected virtual void Start()
        {
            if (isActiveAndEnabled)
            {
                if (eyes != null)
                {
                    foreach (var eye in eyes) eye.SetIndexReference(CharacterExpressionsJobs.RegisterEye(eye, headBone, headSettings, eyeTrackingBone, GetEyeControlSettings));
                }
            }

            initialized = true;
        }

        protected virtual void OnEnable()
        {
            if (initialized && eyes != null)
            {
                foreach (var eye in eyes) eye.SetIndexReference(CharacterExpressionsJobs.RegisterEye(eye, headBone, headSettings, eyeTrackingBone, GetEyeControlSettings));
            }
        }
        protected virtual void OnDisable()
        {
            if (eyes != null)
            {
                foreach (var eye in eyes) eye.DisposeIndexReference();
            }
        }

        protected void OnDestroy()
        {
            if (eyes != null)
            {
                foreach (var eye in eyes) eye.DisposeIndexReference();
                eyes.Clear();
            }
        }

        protected void OnAnimatorEnabled()
        {
            if (eyes != null)
            {
                foreach (var eye in eyes) if (eye != null) eye.SetUndoPreviousOffset(false);
            }
        }
        protected void OnAnimatorDisabled()
        {
            if (eyes != null)
            {
                foreach (var eye in eyes) if (eye != null) eye.SetUndoPreviousOffset(true); 
            }
        }

    }
}

#endif