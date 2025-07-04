#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole.Animation;
using Swole.Script;

namespace Swole.API.Unity.Animation
{
    [CreateAssetMenu(fileName = "CustomAnimationController", menuName = "Swole/Animation/CustomAnimationController", order = 1)]
    public class CustomAnimationController : ScriptableObject, IAnimationController
    {

        public static CustomAnimationController BuildDefaultController(string prefix, ICollection<CustomAnimationAsset> animations, float defaultMix = 1, bool additiveLayers = false)
        {
            CustomAnimation[] anims = new CustomAnimation[animations.Count];

            int i = 0; 
            foreach(var asset in animations)
            {
                if (asset == null)
                {
#if UNITY_EDITOR
                    Debug.LogError("An asset in the provided animation asset list was null!");
#endif
                    continue;
                }

                anims[i] = asset.Animation;
                i++;
            }

            return BuildDefaultController(prefix, anims, defaultMix, additiveLayers);
        }
        public static CustomAnimationController BuildDefaultController(string prefix, IEnumerable<CustomAnimation> animations, float defaultMix = 1, bool additiveLayers = false)
        {
            var controller = ScriptableObject.CreateInstance<CustomAnimationController>();
            controller.name = prefix;

            List<AnimationReference> animationReferences = new List<AnimationReference>();
            int i = 0;
            foreach(var anim in animations)
            {
                string animName = anim.Name;
                if (string.IsNullOrWhiteSpace(animName))
                {
                    animName = $"anim_{i}";
                }

                animationReferences.Add(new AnimationReference(animName, anim, AnimationLoopMode.Loop));
            }

            List<CustomAnimationLayer> animationLayers = new List<CustomAnimationLayer>();
            for(int index = 0; index < animationReferences.Count; index++)
            {
                var anim = animationReferences[index];

                string animName = anim.Name;
                if (string.IsNullOrWhiteSpace(animName))
                {
                    animName = $"anim_{index}";  
                }

                CustomAnimationLayer layer = new CustomAnimationLayer();

                layer.SetActive(true);
                layer.name = animName;
                layer.blendParameterIndex = -1;
                layer.entryStateIndex = 0;
                layer.mix = defaultMix;
                layer.IsAdditive = additiveLayers;
                layer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = index, type = MotionControllerType.AnimationReference } };

                CustomAnimationLayerState previewState = new CustomAnimationLayerState() { name = $"{animName}_state", motionControllerIndex = 0 };
                layer.states = new CustomAnimationLayerState[] { previewState };

                animationLayers.Add(layer);
            }

            controller.animationReferences = animationReferences.ToArray();
            controller.layers = animationLayers.ToArray();

            return controller;
        }

        #region IEngineObject

        public object Instance => this;
        public int InstanceID => GetInstanceID();

        public static void Destroy(EngineInternal.IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
        public static void AdminDestroy(EngineInternal.IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
        public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
        public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

        public bool IsDestroyed => swole.Engine.IsNull(Instance);

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        #endregion
         
        public string Prefix
        {
            get => (string.IsNullOrEmpty(name) ? "null" : name) + "/";
            set => name = value;
        }
        public string ConvertLayerName(string layerName) => Prefix + layerName;

#if UNITY_EDITOR
        protected void OnValidate()
        {

            if (layers != null)
            { 

                foreach (var layer in layers)
                {

                    for (int a = 0; a < layer.StateCount; a++)
                    {

                        var state = layer.GetStateUnsafe(a);

                        if (state == null) continue;

                        state.Index = a;
                        EditorUtility.SetDirty(this);

                    }

                }

            }

        }
#endif

        public CustomAnimationParameter.Float[] floatParameters;
        public IAnimationParameter[] FloatParameters
        {
            get => floatParameters;        
            set
            {
                if (value == null)
                {
                    floatParameters = null;
                    return;
                }

                floatParameters = new CustomAnimationParameter.Float[value.Length];
                for(int a = 0; a < value.Length; a++)
                {
                    var par = value[a];
                    if (par is not CustomAnimationParameter.Float parF)
                    {
                        parF = new CustomAnimationParameter.Float(par.Name, par.GetDefaultValue());
                        parF.indexInAnimator = par.IndexInAnimator;
                    }
                    floatParameters[a] = parF;
                }
            }
        }

        public CustomAnimationParameter.Boolean[] boolParameters;
        public IAnimationParameterBoolean[] BoolParameters
        {
            get => boolParameters;
            set
            {
                if (value == null)
                {
                    boolParameters = null;
                    return;
                }

                boolParameters = new CustomAnimationParameter.Boolean[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var par = value[a];
                    if (par is not CustomAnimationParameter.Boolean parF)
                    {
                        parF = new CustomAnimationParameter.Boolean(par.Name, par.GetDefaultValue() >= 0.5f);
                        parF.indexInAnimator = par.IndexInAnimator;
                    }
                    boolParameters[a] = parF;
                }
            }
        }

        public CustomAnimationParameter.Trigger[] triggerParameters;
        public IAnimationParameterTrigger[] TriggerParameters
        {
            get => triggerParameters;
            set
            {
                if (value == null)
                {
                    triggerParameters = null;
                    return;
                }

                triggerParameters = new CustomAnimationParameter.Trigger[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var par = value[a];
                    if (par is not CustomAnimationParameter.Trigger parF)
                    {
                        parF = new CustomAnimationParameter.Trigger(par.Name);
                        parF.indexInAnimator = par.IndexInAnimator;
                    }
                    triggerParameters[a] = parF;
                }
            }
        }

        public AnimationParameterIdentifier[] parameters;

        public IAnimationParameter[] Parameters
        {

            get
            {

                if (parameters == null) return new IAnimationParameter[0];

                IAnimationParameter[] array = new IAnimationParameter[parameters.Length];

                for (int a = 0; a < parameters.Length; a++) array[a] = GetAnimationParameter(parameters[a]);
                 
                return array;

            }

        }
        public IAnimationParameter[] GetParameters(bool instantiate = false)
        {

            var parameterRefs = Parameters;

            if (instantiate) for (int a = 0; a < parameterRefs.Length; a++) 
                { 
                    var p = (IAnimationParameter)parameterRefs[a].Clone();
                    p.Reset();
                    parameterRefs[a] = p;
                }

            return parameterRefs;

        }

        public CustomAnimationLayer[] layers;
        public IAnimationLayer[] Layers
        {
            get
            {
                if (layers == null) return null;
                var array = new IAnimationLayer[layers.Length];
                for (int a = 0; a < layers.Length; a++) array[a] = layers[a];
                return array;
            }
            set
            {
                if (value == null)
                {
                    layers = null;
                    return;
                }
                layers = new CustomAnimationLayer[value.Length];
                for(int a = 0; a < value.Length; a++)
                {
                    var l = value[a];
                    if (l is not CustomAnimationLayer cal)
                    {
                        cal = new CustomAnimationLayer();
                        if (l != null)
                        {
                            cal.name = l.Name;
                            cal.blendParameterIndex = l.BlendParameterIndex;
                            cal.indexInAnimator = l.IndexInAnimator;
                            cal.motionControllerIdentifiers = l.MotionControllerIdentifiers;
                            cal.States = l.States;
                            cal.mix = l.Mix;
                            cal.deactivate = l.Deactivate;
                            cal.entryStateIndex = l.EntryStateIndex;
                        } 
                        else
                        {
                            cal.name = "null";
                            cal.deactivate = true;
                            cal.mix = 0;
                            cal.blendParameterIndex = -1; 
                            cal.entryStateIndex = -1;
                        }
                    }
                    layers[a] = cal;
                }
            }
        }
        public int LayerCount => layers == null ? 0 : layers.Length;
        public IAnimationLayer GetLayer(int index)
        {
            if (layers == null || index < 0 || index >= layers.Length) return null;
            return GetLayerUnsafe(index);
        }
        public IAnimationLayer GetLayerUnsafe(int index) => layers[index];
        public void SetLayer(int index, IAnimationLayer layer)
        {
            if (layers == null || index < 0 || index >= layers.Length) return;
            SetLayerUnsafe(index, layer);
        }
        public void SetLayerUnsafe(int index, IAnimationLayer layer)
        {
            if (layer is not CustomAnimationLayer cal) return; 
            layers[index] = cal;
        }

        public AnimationReference[] animationReferences;
        public IAnimationReference[] AnimationReferences
        {
            get => animationReferences;
            set
            {
                if (value == null)
                {
                    animationReferences = null;
                    return;
                }

                animationReferences = new AnimationReference[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var ext = value[a];
                    if (ext is not AnimationReference inst)
                    {
                        inst = new AnimationReference(ext.Name, ext.Animation, ext.LoopMode);
                        inst.baseSpeed = ext.BaseSpeed;
                        inst.speedMultiplierParameter = ext.SpeedMultiplierParameter;
                    }
                    animationReferences[a] = inst;
                }
            }
        }

        public BlendTree1D[] blendTrees1D;
        public IBlendTree1D[] BlendTrees1D
        {
            get => blendTrees1D;
            set
            {
                if (value == null)
                {
                    blendTrees1D = null;
                    return;
                }

                blendTrees1D = new BlendTree1D[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var ext = value[a];
                    if (ext is not BlendTree1D inst)
                    {
                        inst = new BlendTree1D(ext.Name, ext.ParameterIndex, null);
                        inst.MotionFields = ext.MotionFields;
                        inst.baseSpeed = ext.BaseSpeed;
                        inst.speedMultiplierParameter = ext.SpeedMultiplierParameter;
                    }
                    blendTrees1D[a] = inst;
                }
            }
        }

        public BlendTree2D[] blendTrees2D;
        public IBlendTree2D[] BlendTrees2D
        {
            get => blendTrees2D;
            set
            {
                if (value == null)
                {
                    blendTrees2D = null;
                    return;
                }

                blendTrees2D = new BlendTree2D[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var ext = value[a];
                    if (ext is not BlendTree2D inst)
                    {
                        inst = new BlendTree2D(ext.Name, ext.ParameterIndexX, ext.ParameterIndexY, null);
                        inst.MotionFields = ext.MotionFields;
                        inst.baseSpeed = ext.BaseSpeed;
                        inst.speedMultiplierParameter = ext.SpeedMultiplierParameter;
                    }
                    blendTrees2D[a] = inst;
                }
            }
        }

        public MotionComposite[] motionComposites;
        public IMotionComposite[] MotionComposites
        {
            get => motionComposites;
            set
            {
                if (value == null)
                {
                    motionComposites = null;
                    return;
                }

                motionComposites = new MotionComposite[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    var ext = value[a];
                    if (ext is not MotionComposite inst)
                    {
                        inst = new MotionComposite(ext.Name, null);
                        inst.BaseMotionParts = ext.BaseMotionParts;
                        inst.baseSpeed = ext.BaseSpeed;
                        inst.speedMultiplierParameter = ext.SpeedMultiplierParameter;
                    }
                    motionComposites[a] = inst;
                }
            }
        }

        public IAnimationMotionController GetMotionController(MotionControllerIdentifier identifier)
        {

            switch (identifier.type)
            {

                case MotionControllerType.AnimationReference:

                    if (animationReferences != null && identifier.index >= 0 && identifier.index < animationReferences.Length) return animationReferences[identifier.index];

                    break;

                case MotionControllerType.BlendTree1D:

                    if (blendTrees1D != null && identifier.index >= 0 && identifier.index < blendTrees1D.Length) return blendTrees1D[identifier.index];

                    break;

                case MotionControllerType.BlendTree2D:

                    if (blendTrees2D != null && identifier.index >= 0 && identifier.index < blendTrees2D.Length) return blendTrees2D[identifier.index];

                    break;

                case MotionControllerType.MotionComposite:

                    if (motionComposites != null && identifier.index >= 0 && identifier.index < motionComposites.Length) return motionComposites[identifier.index];

                    break;

            }

            return null;

        }

        public IAnimationParameter GetAnimationParameter(AnimationParameterIdentifier identifier)
        {

            switch (identifier.type)
            {

                case AnimationParameterValueType.Float:

                    if (floatParameters != null && identifier.index >= 0 && identifier.index < floatParameters.Length) return floatParameters[identifier.index];

                    break;

                case AnimationParameterValueType.Boolean:

                    if (boolParameters != null && identifier.index >= 0 && identifier.index < boolParameters.Length) return boolParameters[identifier.index];

                    break;

                case AnimationParameterValueType.Trigger:

                    if (triggerParameters != null && identifier.index >= 0 && identifier.index < triggerParameters.Length) return triggerParameters[identifier.index];

                    break;

            }

            return null;

        }

    }
}

#endif