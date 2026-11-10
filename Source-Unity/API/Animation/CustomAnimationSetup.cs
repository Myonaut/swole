#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

using Swole;
using Swole.Animation;
using Swole.API.Unity;
using Swole.API.Unity.Animation;
using System.Linq;

namespace Swole.API.Animation
{

    public static class CustomAnimationSetup
    {

        public static void BuildAnimations(
            AnimationParametersCreator animationParametersCreator,
            AnimationAssetLoader animationAssetLoader,
            IEnumerable<AnimationLayerCreator> animationLayerCreators,

            ref CustomAnimationParametersBuilder animationParametersBuilder,
            ref CustomMotionControllersBuilder motionControllersBuilder,

            CustomAnimationController controller = null
            )
        {
            BuildAnimations(
                animationParametersCreator == null ? null : animationParametersCreator.floatParameters,
                animationParametersCreator == null ? null : animationParametersCreator.boolParameters,
                animationParametersCreator == null ? null : animationParametersCreator.triggerParameters,
                animationAssetLoader, animationLayerCreators,
                ref animationParametersBuilder, ref motionControllersBuilder,
                controller);
        }

        public static void BuildAnimations(
            IEnumerable<CustomAnimationParameter.Float> floatParameters,
            IEnumerable<CustomAnimationParameter.Boolean> boolParameters,
            IEnumerable<CustomAnimationParameter.Trigger> triggerParameters,
            AnimationAssetLoader animationAssetLoader,
            IEnumerable<AnimationLayerCreator> animationLayerCreators,

            ref CustomAnimationParametersBuilder animationParametersBuilder,
            ref CustomMotionControllersBuilder motionControllersBuilder,

            CustomAnimationController controller = null
            )
        {
            if (animationParametersBuilder == null && floatParameters != null && boolParameters != null && triggerParameters != null) animationParametersBuilder = new CustomAnimationParametersBuilder();
            if (motionControllersBuilder == null && animationLayerCreators != null) motionControllersBuilder = new CustomMotionControllersBuilder();

            if (floatParameters != null)
            {
                foreach (var p in floatParameters)
                {
                    animationParametersBuilder.AddFloatParameter(p);
                }
            }
            if (boolParameters != null)
            {
                foreach (var p in boolParameters)
                {
                    animationParametersBuilder.AddBoolParameter(p);
                }
            }
            if (triggerParameters != null)
            {
                foreach (var p in triggerParameters)
                {
                    animationParametersBuilder.AddTriggerParameter(p);
                }
            }

            List<CustomAnimationLayer> layers = new List<CustomAnimationLayer>();
            if (animationLayerCreators != null)
            {
                int startIndex = 0;
                foreach (var layer in animationLayerCreators)
                {
                    int count = layer.MotionControllerCount;

                    layers.Add(layer.CreateInstance(animationAssetLoader, animationParametersBuilder, motionControllersBuilder, startIndex)); 

                    startIndex += count;
                }
            }

            if (controller != null)
            {
                int parametersIndexOffset = 0;
                if (animationParametersBuilder != null) animationParametersBuilder.Finalize(controller, true, out parametersIndexOffset);
                if (motionControllersBuilder != null)
                {
                    if (parametersIndexOffset != 0)
                    {
                        Dictionary<int, int> parametersIndexRemapper = new Dictionary<int, int>();
                        for (int a = 0; a < animationParametersBuilder.identifiers.Count; a++)
                        {
                            parametersIndexRemapper.Add(a, a + parametersIndexOffset);
                        }

                        for (int a = 0; a < motionControllersBuilder.motionControllerIds.Count; a++)
                        {
                            var motionController = motionControllersBuilder.GetMotionController(a);
                            motionController.RemapParameterIndices(null, parametersIndexRemapper, false, false);
                        }
                    }

                    motionControllersBuilder.Finalize(controller, true, out _, out _, out _, out _, true);
                    
                    /*if (animationLayerCreators != null) // shouldn't be necessary
                    {
                        int startIndex = 0;
                        foreach (var layer in animationLayerCreators)
                        {
                            int count = layer.MotionControllerCount;
                            motionControllersBuilder.Finalize(layer.tempInstance, false, ref startIndex, ref count, out _);

                            startIndex += count;
                        }
                    }*/
                }

                if (controller.layers != null && controller.layers.Length > 0)
                {
                    layers.InsertRange(0, controller.layers);
                }

                controller.layers = layers.ToArray();
            }
        }

    }

    [Serializable]
    public class AnimationParametersCreator
    {
        public List<CustomAnimationParameter.Float> floatParameters;
        public List<CustomAnimationParameter.Boolean> boolParameters;
        public List<CustomAnimationParameter.Trigger> triggerParameters;
    }

    [Serializable]
    public class AnimationAssetLoader
    {
        [Serializable]
        public class AnimationAsset
        {
            public string name;
            public float speed = 1f;
            public AnimationLoopMode loopMode;
            public AssetIdentifier fileIdentifier;
            public CustomAvatarMaskAsset mask;
            public bool invertMask;
            public bool calculateFootHeights;
            public float footHeightReferenceTime;
            public int footHeightSamplesPerSecond = 16;
            public bool useSeperateRightFootHeightReferenceTime;
            public float rightFootHeightReferenceTime;
            public bool useSeperateRightFootHeightSamplesPerSecond;
            public int rightFootHeightSamplesPerSecond;

            [NonSerialized]
            private CustomAnimation asset;
            public CustomAnimation Asset
            {
                get
                {
                    if (asset == null)
                    {
                        if (!ContentManager.TryFindContent<CustomAnimation>(fileIdentifier.name, fileIdentifier.package, out asset)) Debug.LogError($"Animation '{fileIdentifier}' not found!");
                    }

                    return asset;
                }
            }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
            public AnimationCurve footHeightDebugCurveL;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
            public AnimationCurve footHeightDebugCurveR;

            public void GenerateFootHeightCurves(Transform footL_transform, Transform footR_transform, out AnimationCurve footHeightCurveL, out AnimationCurve footHeightCurveR)
            {
                footHeightCurveL = null;
                footHeightCurveR = null;

                var asset_ = Asset;

                float animLength = asset_.LengthInSeconds;

                int samplesPerSecond = footHeightSamplesPerSecond <= 0 ? 16 : footHeightSamplesPerSecond;
                if (footL_transform != null) footHeightCurveL = asset_.GetBoneHeightCurve(footL_transform, samplesPerSecond, footHeightReferenceTime * animLength);
                if (footR_transform != null) footHeightCurveR = asset_.GetBoneHeightCurve(footR_transform, useSeperateRightFootHeightSamplesPerSecond ? rightFootHeightSamplesPerSecond : samplesPerSecond, useSeperateRightFootHeightReferenceTime ? (rightFootHeightReferenceTime * animLength) : (footHeightReferenceTime * animLength));

#if UNITY_EDITOR
                footHeightDebugCurveL = footHeightCurveL;
                footHeightDebugCurveR = footHeightCurveR;
#endif
            }

        }

        public List<AnimationAsset> animations;
        public AnimationAsset FindAnimationAsset(string name)
        {
            if (animations == null || string.IsNullOrWhiteSpace(name)) return null;

            foreach (var anim in animations)
            {
                if (anim == null) continue;

                if (anim.name == name) return anim;
            }

            return null;
        }

        public void ReplaceAnimationAsset(string name, AnimationAsset animationAsset)
        {
            if (animations == null || string.IsNullOrWhiteSpace(name)) return;

            for (int i = 0; i < animations.Count; i++)
            {
                var anim = animations[i];
                if (anim == null) continue;

                if (anim.name == name)
                {
                    animations[i] = animationAsset;
                    return;
                }
            }
        }

        public void AddOrReplaceAnimationAsset(AnimationAsset animationAsset) 
        {
            if (animationAsset == null) return;

            if (animations == null) animations = new List<AnimationAsset>();

            for (int i = 0; i < animations.Count; i++) 
            {
                var anim = animations[i];
                if (anim == null) continue;

                if (anim.name == animationAsset.name)
                {
                    animations[i] = animationAsset;
                    return;
                }
            }

            animations.Add(animationAsset);
        }

    }

    [Serializable]
    public class AnimationLayerCreator
    {
        public string name;
        public string initStateName;

        public CustomAnimationLayer rawData;

        public List<AnimationReferenceCreator> animationReferences;
        public List<BlendTree1DCreator> blendTree1Ds;
        public List<BlendTree2DCreator> blendTree2Ds;
        public List<MotionCompositeCreator> motionComposites;

        public int MotionControllerCount
        {
            get
            {
                int count = 0;
                if (animationReferences != null) count += animationReferences.Count;
                if (blendTree1Ds != null) count += blendTree1Ds.Count;
                if (blendTree2Ds != null) count += blendTree2Ds.Count;
                if (motionComposites != null) count += motionComposites.Count;
                return count;
            }
        }

        public int GetAnyMotionControllerIndex(string name, int startControllerIndex)
        {
            int index = 0;

            if (animationReferences != null)
            {
                for (int a = 0; a < animationReferences.Count; a++)
                {
                    var motC = animationReferences[a];
                    if (motC != null && motC.name == name) return index + startControllerIndex;
                    index++;
                }
            }
            if (blendTree1Ds != null)
            {
                for (int a = 0; a < blendTree1Ds.Count; a++)
                {
                    var motC = blendTree1Ds[a];
                    if (motC != null && motC.name == name) return index + startControllerIndex;
                    index++;
                }
            }
            if (blendTree2Ds != null)
            {
                for (int a = 0; a < blendTree2Ds.Count; a++)
                {
                    var motC = blendTree2Ds[a];
                    if (motC != null && motC.name == name) return index + startControllerIndex;
                    index++;
                }
            }
            if (motionComposites != null)
            {
                for (int a = 0; a < motionComposites.Count; a++)
                {
                    var motC = motionComposites[a];
                    if (motC != null && motC.name == name) return index + startControllerIndex;
                    index++;
                }
            }

            return -1;
        }

        public List<AnimationLayerStateCreator> layerStates;

        public List<AnimationLayerStateTransitionCreator> stateTransitions;
        public AnimationLayerStateTransitionCreator FindTransitionCreator(string name)
        {
            if (stateTransitions == null) return null;

            foreach (var creator in stateTransitions)
            {
                if (creator == null || creator.name != name) continue;
                return creator;
            }

            return null;
        }

        public int LayerStateCount => layerStates == null ? 0 : layerStates.Count;

        public int GetLayerStateIndex(string name)
        {
            if (layerStates == null) return -1;
            for (int a = 0; a < layerStates.Count; a++)
            {
                var state = layerStates[a];
                if (state != null && state.name == name) return a;
            }
            return -1;
        }

        [NonSerialized]
        public CustomAnimationLayer tempInstance;

        public CustomAnimationLayer CreateInstance(AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int startGlobalControllerIndex)
        {
            tempInstance = rawData == null ? new CustomAnimationLayer() : rawData.Duplicate();

            tempInstance.name = name;
#if UNITY_EDITOR
            Debug.Log($"Creating layer instance {name}"); 
#endif

            int motionControllerCount = MotionControllerCount;
            int endControllerIndex = motionControllerCount - 1;
            tempInstance.motionControllerIdentifiers = new MotionControllerIdentifier[motionControllerCount]; 

            int localMotionControllerIndex = 0;

            if (motionControllersBuilder != null)
            {
                if (animationReferences != null)
                {
                    for (int a = 0; a < animationReferences.Count; a++)
                    {
                        var motC = animationReferences[a];
                        int subIndex = -1;
                        if (motC != null)
                        {
                            motionControllersBuilder.AddAnimationReference(motC.CreateInstance(this, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, 0, endControllerIndex), out _, out subIndex);
                        }

                        tempInstance.motionControllerIdentifiers[localMotionControllerIndex] = new MotionControllerIdentifier() { type = MotionControllerType.AnimationReference, index = subIndex };
                        localMotionControllerIndex++;
                    }
                }

                if (blendTree1Ds != null)
                {
                    for (int a = 0; a < blendTree1Ds.Count; a++)
                    {
                        var motC = blendTree1Ds[a];
                        int subIndex = -1;
                        if (motC != null)
                        {
                            motionControllersBuilder.AddBlendTree1D(motC.CreateInstance(this, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, 0, endControllerIndex), out _, out subIndex);
                        }

                        tempInstance.motionControllerIdentifiers[localMotionControllerIndex] = new MotionControllerIdentifier() { type = MotionControllerType.BlendTree1D, index = subIndex };
                        localMotionControllerIndex++;
                    }
                }

                if (blendTree2Ds != null)
                {
                    for (int a = 0; a < blendTree2Ds.Count; a++)
                    {
                        var motC = blendTree2Ds[a];
                        int subIndex = -1;
                        if (motC != null)
                        {
                            motionControllersBuilder.AddBlendTree2D(motC.CreateInstance(this, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, 0, endControllerIndex), out _, out subIndex);
                        }

                        tempInstance.motionControllerIdentifiers[localMotionControllerIndex] = new MotionControllerIdentifier() { type = MotionControllerType.BlendTree2D, index = subIndex }; 
                        localMotionControllerIndex++;
                    }
                }
                 
                if (motionComposites != null)
                {
                    for (int a = 0; a < motionComposites.Count; a++)
                    {
                        var motC = motionComposites[a];
                        int subIndex = -1;
                        if (motC != null)
                        {
                            motionControllersBuilder.AddMotionComposite(motC.CreateInstance(this, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, 0, endControllerIndex), out _, out subIndex);
                        }

                        tempInstance.motionControllerIdentifiers[localMotionControllerIndex] = new MotionControllerIdentifier() { type = MotionControllerType.MotionComposite, index = subIndex };
                        localMotionControllerIndex++;
                    }
                }

                if (layerStates != null)
                {
                    tempInstance.states = new CustomAnimationLayerState[layerStates.Count];
                    for (int a = 0; a < layerStates.Count; a++)
                    {
                        var stateCreator = layerStates[a];
                        if (stateCreator != null)
                        {
                            tempInstance.states[a] = stateCreator.CreateInstance(a, this, 0);
                            if (stateCreator.name == initStateName) tempInstance.entryStateIndex = a;   
                        }
                    }
                }
            }

            return tempInstance;
        }
    }

    [Serializable]
    public class AnimationLayerStateCreator
    {
        public string name;
        public string motionControllerName;

        public SwolePuppetMuscleConfigurationAsset puppetConfig;

        public List<string> transitions;
        public void AddTransition(string transitionName)
        {
            if (transitions == null) transitions = new List<string>();
            transitions.Add(transitionName);
        }

        public CustomAnimationLayerState rawData;

        public CustomAnimationLayerState CreateInstance(int index, AnimationLayerCreator layerCreator, int minControllerIndex)
        {

            var instance = new CustomAnimationLayerState();
            instance.name = name;
            instance.index = index;
            instance.motionControllerIndex = layerCreator.GetAnyMotionControllerIndex(motionControllerName, minControllerIndex);
            instance.PuppetConfiguration = puppetConfig;

#if UNITY_EDITOR
            Debug.Log("Created layer state " + name + " on layer " + layerCreator.name + " with index " + index + " and motion controller " + motionControllerName + " with index " + instance.motionControllerIndex);

            if (instance.motionControllerIndex < 0)
            {
                Debug.LogError($"Motion controller '{motionControllerName}' not found for layer state '{name}'");
            }
#endif

            if (transitions != null)
            {
                instance.transitions = new Transition[transitions.Count];
                for (int b = 0; b < transitions.Count; b++)
                {
                    var transitionName = transitions[b];
                    var creator_ = layerCreator.FindTransitionCreator(transitionName);
                    if (creator_ != null)
                    {
                        var transitionInstance = creator_.rawData.Duplicate();
                        if (string.IsNullOrWhiteSpace(transitionInstance.id)) transitionInstance.id = creator_.name;
                        instance.transitions[b] = transitionInstance;
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.LogError($"State transition '{transitionName}' not found for layer state '{name}'");
                    }
#endif
                }
            }

            if (transitions != null && instance.transitions != null)
            {
                for (int b = 0; b < transitions.Count; b++)
                {
                    var transitionName = transitions[b];
                    var creator_ = layerCreator.FindTransitionCreator(transitionName);
                    if (creator_ != null)
                    {
                        var transitionInstance = instance.transitions[b];
                        transitionInstance.targetStateIndex = layerCreator.GetLayerStateIndex(creator_.targetStateName);
#if UNITY_EDITOR
                        Debug.Log($"Created transition {transitionName} with target state index {transitionInstance.targetStateIndex} for state {name}");
#endif
                    }
                }
            }

            return instance;
        }
    }
    [Serializable]
    public class AnimationLayerStateTransitionCreator
    {
        public string name;
        public string targetStateName;

        public Transition rawData;
    }

    [Serializable]
    public class AnimationReferenceCreator
    {
        public string name;
        public string speedParameter;
        public string timeOverrideParameter;

        public string animationName;

        public AnimationReference rawData;

        public AnimationReference CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            bool hasRawData = rawData != null;
            var instance = !hasRawData ? new AnimationReference() : rawData.Duplicate();

            instance.name = name;
            if (animationParametersBuilder != null)
            {
                instance.speedMultiplierParameter = animationParametersBuilder.FindParameterIndex(speedParameter);
                instance.timeOverrideParameter = animationParametersBuilder.FindParameterIndex(timeOverrideParameter);
            }

            if (animationAssetLoader != null)
            {
                var animAsset = animationAssetLoader.FindAnimationAsset(animationName);
                if (animAsset != null)
                {
                    instance.Animation = animAsset.Asset;
                    if (hasRawData)
                    {
                        instance.normalizedTimeStart = rawData.normalizedTimeStart;
                        instance.baseSpeed = rawData.baseSpeed * animAsset.speed;
                        instance.loopMode = rawData.loopMode;
                        if (rawData.AvatarMask == null && instance.AvatarMask != null)
                        {
                            instance.AvatarMask = animAsset.mask;
                            instance.InvertAvatarMask = animAsset.invertMask;
                        }
                        else
                        {
                            Debug.Log($"MASK FOR CRETOR {name} IS {(instance.AvatarMask == null ? "null" : "not null")} AND WAS NOT REPLACED BY ASSET MASK");
                        }
                    } 
                    else
                    {
                        instance.normalizedTimeStart = 0f;
                        instance.baseSpeed = animAsset.speed;
                        instance.loopMode = animAsset.loopMode;
                        instance.AvatarMask = animAsset.mask;
                        instance.InvertAvatarMask = animAsset.invertMask;
                    }

                    Debug.Log($"Creating animation reference {name} with speed {instance.baseSpeed}"); 
                }             
            }

            return instance;
        } 
    }

    [Serializable]
    public class BlendTree1DCreator
    {
        public string name;
        public string speedParameter;

        public string parameterX;
        public MotionField1DCreator[] motionFields;

        public BlendTree1D rawData;

        public BlendTree1D CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new BlendTree1D() : rawData.Duplicate();

            instance.name = name;
            if (animationParametersBuilder != null)
            {
                instance.speedMultiplierParameter = animationParametersBuilder.FindParameterIndex(speedParameter);
                instance.parameterIndex = animationParametersBuilder.FindParameterIndex(parameterX);
            }

            if (motionFields != null)
            {
                instance.motionFields = new MotionField1D[motionFields.Length];
                for (int a = 0; a < motionFields.Length; a++)
                {
                    var field = motionFields[a];
                    if (field == null) continue;

                    if (field.controllerName == name) Debug.LogError($"BlendTree1D {name} is self referencing!");
                    instance.motionFields[a] = field.CreateInstance(layerCreator, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, minControllerIndex, maxControllerIndex);
                }
            }

            return instance;
        }
    }
    [Serializable]
    public class MotionField1DCreator
    {
        public string controllerName;
        public string blendSpeedParameter;

        public MotionField1D rawData;

        public MotionField1D CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new MotionField1D() : rawData.Duplicate();

            if (animationParametersBuilder != null)
            {
                instance.blendSpeedParameterIndex = animationParametersBuilder.FindParameterIndex(blendSpeedParameter);
            }

            if (layerCreator != null)
            {
                instance.controllerIndex = layerCreator.GetAnyMotionControllerIndex(controllerName, minControllerIndex); // use the layer creator to find motion controller indices that may not have been added to the builder yet.
            }

            return instance;
        }
    }

    [Serializable]
    public class BlendTree2DCreator
    {
        public string name;
        public string speedParameter;

        public string parameterX;
        public string parameterY;
        public MotionField2DCreator[] motionFields;

        public BlendTree2D rawData;

        public BlendTree2D CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new BlendTree2D() : rawData.Duplicate();

            instance.name = name;
            if (animationParametersBuilder != null)
            {
                instance.speedMultiplierParameter = animationParametersBuilder.FindParameterIndex(speedParameter);
                instance.parameterIndexX = animationParametersBuilder.FindParameterIndex(parameterX);
                instance.parameterIndexY = animationParametersBuilder.FindParameterIndex(parameterY);
            }

            if (motionFields != null)
            {
                instance.motionFields = new MotionField2D[motionFields.Length];
                for (int a = 0; a < motionFields.Length; a++)
                {
                    var field = motionFields[a];
                    if (field == null) continue;

                    if (field.controllerName == name) Debug.LogError($"BlendTree2D {name} is self referencing!");
                    instance.motionFields[a] = field.CreateInstance(layerCreator, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, minControllerIndex, maxControllerIndex);
                }
            }

            return instance;
        }
    }
    [Serializable]
    public class MotionField2DCreator
    {
        public string controllerName;
        public string blendSpeedParameter;

        public MotionField2D rawData;

        public MotionField2D CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new MotionField2D() : rawData.Duplicate();

            if (animationParametersBuilder != null)
            {
                instance.blendSpeedParameterIndex = animationParametersBuilder.FindParameterIndex(blendSpeedParameter);
            }

            if (layerCreator != null)
            {
                instance.controllerIndex = layerCreator.GetAnyMotionControllerIndex(controllerName, minControllerIndex); // use the layer creator to find motion controller indices that may not have been added to the builder yet.
            }

            return instance;
        }
    }

    [Serializable]
    public class MotionCompositeCreator
    {
        public string name;
        public string speedParameter;

        public MotionPartCreator[] motionParts;

        public MotionComposite rawData;

        public MotionComposite CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new MotionComposite() : rawData.Duplicate();

            instance.name = name;
            if (animationParametersBuilder != null)
            {
                instance.speedMultiplierParameter = animationParametersBuilder.FindParameterIndex(speedParameter);
            }

            if (motionParts != null)
            {
                instance.motionParts = new MotionPart[motionParts.Length];
                for (int a = 0; a < motionParts.Length; a++)
                {
                    var part = motionParts[a];
                    if (part == null) continue;

                    if (part.controllerName == name) Debug.LogError($"MotionComposite {name} is self referencing!"); 
                    instance.motionParts[a] = part.CreateInstance(layerCreator, animationAssetLoader, animationParametersBuilder, motionControllersBuilder, minControllerIndex, maxControllerIndex);
                }
            }

            Debug.Log($"Created motion composite {name}");

            return instance;
        }
    }
    [Serializable]
    public class MotionPartCreator
    {
        public string controllerName;
        public string mixParameter;
        public string blendSpeedParameter;

        public MotionPart rawData;

        public MotionPart CreateInstance(AnimationLayerCreator layerCreator, AnimationAssetLoader animationAssetLoader, CustomAnimationParametersBuilder animationParametersBuilder, CustomMotionControllersBuilder motionControllersBuilder, int minControllerIndex, int maxControllerIndex)
        {
            var instance = rawData == null ? new MotionPart() : rawData.Duplicate();

            if (animationParametersBuilder != null)
            {
                instance.mixParameterIndex = animationParametersBuilder.FindParameterIndex(mixParameter);
                instance.blendSpeedParameterIndex = animationParametersBuilder.FindParameterIndex(blendSpeedParameter);              
            }

            if (layerCreator != null)
            { 
                instance.controllerIndex = layerCreator.GetAnyMotionControllerIndex(controllerName, minControllerIndex); // use the layer creator to find motion controller indices that may not have been added to the builder yet.
                Debug.Log("Creating motion part for controller " + controllerName + " with index " + instance.controllerIndex);
            }

            return instance;
        }
    }

}

#endif