using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Swole.API.Unity.Animation;
using Unity.Collections;
using System.Runtime.InteropServices;
using Swole.API.Unity;

namespace Swole.Morphing
{

    public class CustomizableCharacterMesh : InstanceableSkinnedMeshBase
    {

#if UNITY_EDITOR

        public void OnValidate() 
        {
            if (Application.isPlaying && isActiveAndEnabled)
            { 
                if (prevBustSizeEditor != bustSizeEditor)
                {
                    prevBustSizeEditor = bustSizeEditor;
                    SetBustSize(bustSizeEditor);
                }
                if (prevShapeWeightsEditor == null || prevShapeWeightsEditor.Length == 0)  
                { 
                    prevShapeWeightsEditor = new NamedFloat[StandaloneShapesControl.Length];
                    for (int a = 0; a < prevShapeWeightsEditor.Length; a++) prevShapeWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetStandaloneShape(a).name };
                }
                if (shapeWeightsEditor == null || shapeWeightsEditor.Length == 0)
                {
                    shapeWeightsEditor = new NamedFloat[StandaloneShapesControl.Length];
                    for (int a = 0; a < shapeWeightsEditor.Length; a++) shapeWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetStandaloneShape(a).name };
                }

                if (prevMuscleWeightsEditor == null || prevMuscleWeightsEditor.Length == 0)
                {
                    prevMuscleWeightsEditor = new NamedMuscleData[MuscleGroupsControl.Length];
                    for (int a = 0; a < prevMuscleWeightsEditor.Length; a++) prevMuscleWeightsEditor[a] = new NamedMuscleData() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.muscleVertexGroupsBufferRange.x).name };
                }
                if (muscleWeightsEditor == null || muscleWeightsEditor.Length == 0)
                {
                    muscleWeightsEditor = new NamedMuscleData[MuscleGroupsControl.Length];
                    for (int a = 0; a < muscleWeightsEditor.Length; a++) muscleWeightsEditor[a] = new NamedMuscleData() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.muscleVertexGroupsBufferRange.x).name };
                }

                if (prevFatWeightsEditor == null || prevFatWeightsEditor.Length == 0)
                {
                    prevFatWeightsEditor = new NamedFloat[FatGroupsControl.Length];
                    for (int a = 0; a < prevFatWeightsEditor.Length; a++) prevFatWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.fatVertexGroupsBufferRange.x).name };
                }
                if (fatWeightsEditor == null || fatWeightsEditor.Length == 0)
                {
                    fatWeightsEditor = new NamedFloat[FatGroupsControl.Length];
                    for (int a = 0; a < fatWeightsEditor.Length; a++) fatWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.fatVertexGroupsBufferRange.x).name };
                }

                if (prevVariationWeightsEditor == null || prevVariationWeightsEditor.Length == 0)
                {
                    prevVariationWeightsEditor = new NamedFloat2[VariationShapesControl.Length]; 
                    for (int a = 0; a < prevVariationWeightsEditor.Length; a++) 
                    {
                        int groupIndex = a / CharacterMeshData.VariationShapesCount;
                        int shapeIndex = a % CharacterMeshData.VariationShapesCount;
                        prevVariationWeightsEditor[a] = new NamedFloat2() { name = CharacterMeshData.GetVertexGroup(CharacterMeshData.variationGroupIndices[groupIndex]).name + "_" + CharacterMeshData.GetVariationShape(shapeIndex).name }; 
                    }
                }
                if (variationWeightsEditor == null || variationWeightsEditor.Length == 0)
                {
                    variationWeightsEditor = new NamedFloat2[VariationShapesControl.Length];
                    for (int a = 0; a < variationWeightsEditor.Length; a++)
                    {
                        int groupIndex = a / CharacterMeshData.VariationShapesCount;
                        int shapeIndex = a % CharacterMeshData.VariationShapesCount;
                        variationWeightsEditor[a] = new NamedFloat2() { name = CharacterMeshData.GetVertexGroup(CharacterMeshData.variationGroupIndices[groupIndex]).name + "_" + CharacterMeshData.GetVariationShape(shapeIndex).name };
                    }
                }

                for (int a = 0; a < shapeWeightsEditor.Length; a++)
                {
                    if (prevShapeWeightsEditor[a].value != shapeWeightsEditor[a].value)
                    {
                        SetStandaloneShapeWeightUnsafe(a, shapeWeightsEditor[a].value);
                        prevShapeWeightsEditor[a].value = shapeWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < muscleWeightsEditor.Length; a++)
                {
                    if (prevMuscleWeightsEditor[a].value != muscleWeightsEditor[a].value)
                    {
                        SetMuscleDataUnsafe(a, muscleWeightsEditor[a].value);
                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < fatWeightsEditor.Length; a++)
                {
                    if (prevFatWeightsEditor[a].value != fatWeightsEditor[a].value)
                    {
                        SetFatLevelUnsafe(a, fatWeightsEditor[a].value);
                        prevFatWeightsEditor[a].value = fatWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < variationWeightsEditor.Length; a++)
                {
                    if (math.any(prevVariationWeightsEditor[a].value != variationWeightsEditor[a].value))
                    {
                        SetVariationWeightUnsafe(a, variationWeightsEditor[a].value);   
                        prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value;  
                    }
                }
            }
        }

        [Serializable]
        public struct NamedFloat
        {
            public string name;
            public float value;
        }
        [Serializable]
        public struct NamedFloat2
        {
            public string name;
            public float2 value;
        }
        [Serializable]
        public struct NamedMuscleData
        {
            public string name;
            public MuscleDataLR value;
        }

        private float prevBustSizeEditor;
        [Range(0, 2)]
        public float bustSizeEditor;

        public NamedFloat[] prevShapeWeightsEditor;
        public NamedFloat[] shapeWeightsEditor;

        public NamedMuscleData[] prevMuscleWeightsEditor;
        public NamedMuscleData[] muscleWeightsEditor;

        public NamedFloat[] prevFatWeightsEditor;
        public NamedFloat[] fatWeightsEditor;

        public NamedFloat2[] prevVariationWeightsEditor;
        public NamedFloat2[] variationWeightsEditor;

#endif

        public override void Dispose()
        {
            base.Dispose();

            if (standaloneShapesControl.IsCreated)
            {
                standaloneShapesControl.Dispose();
                standaloneShapesControl = default;
            }
            if (muscleGroupsControl.IsCreated) 
            {
                muscleGroupsControl.Dispose();
                muscleGroupsControl = default;
            }
            if (fatGroupsControl.IsCreated)
            {
                fatGroupsControl.Dispose();
                fatGroupsControl = default;
            }
            if (variationShapesControl.IsCreated)
            {
                variationShapesControl.Dispose();
                variationShapesControl = default;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (animatablePropertiesController != null)
            {
                string id = GetInstanceID().ToString();
                for (int a = 0; a < animatablePropertiesController.PropertyCount; a++)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(a);
                    prop.ClearListeners(id);
                }
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateBuffers(); 
        }

        [NonSerialized]
        protected float bustSize;
        public float BustSize
        {
            get => bustSize;
            set => SetBustSize(value);
        }
        [NonSerialized]
        protected bool hasBustSizeProperty;
        public void SetBustSize(float value)
        {
            bustSize = value;

            if (CharacterMeshData.bustSizeShapeIndex >= 0) SetStandaloneShapeWeightUnsafe(CharacterMeshData.bustSizeShapeIndex, value);

            if (instance != null && hasBustSizeProperty)
            {
                instance.SetFloatOverride(CharacterMeshData.BustMixPropertyName, Mathf.Clamp01(value), true); 
            }
        }
        [NonSerialized]
        protected bool hideNipples;
        public bool HideNipples
        {
            get => hideNipples;
            set => hideNipples = value;
        }
        [NonSerialized]
        protected bool hasHideNipplesProperty;
        public void SetHideNipples(bool value)
        {
            hideNipples = value;

            if (instance != null && hasHideNipplesProperty)
            {
                instance.SetFloatOverride(CharacterMeshData.HideNipplesPropertyName, hideNipples ? 1 : 0, true);  
            }
        }
        [NonSerialized]
        protected bool hideGenitals;
        public bool HideGenitals
        {
            get => hideGenitals;
            set => hideGenitals = value;
        }
        [NonSerialized]
        protected bool hasHideGenitalsProperty;
        public void SetHideGenitals(bool value)
        {
            hideGenitals = value;
             
            if (instance != null && hasHideGenitalsProperty)
            {
                instance.SetFloatOverride(CharacterMeshData.HideGenitalsPropertyName, hideGenitals ? 1 : 0, true);
            }
        }

        [SerializeField]
        protected CustomizableCharacterMeshData meshData;
        public void SetMeshData(CustomizableCharacterMeshData data) => meshData = data;
        public override InstanceableMeshData MeshData => meshData;
        public virtual CustomizableCharacterMeshData CharacterMeshData => meshData;
        public override InstancedMeshGroup MeshGroup => meshData.meshGroups[meshGroupIndex];

        [SerializeField]
        protected CustomAvatar avatar;
        public void SetAvatar(CustomAvatar av) => avatar = av;
        public CustomAvatar Avatar => avatar;
        [SerializeField]
        protected Transform rigRoot;
        public void SetRigRoot(Transform root) => rigRoot = root;
        public Transform RigRoot
        {
            get
            {
                if (rigRoot == null)
                {
                    if (avatar != null && !string.IsNullOrWhiteSpace(avatar.rigContainer))
                    {
                        rigRoot = (transform.parent == null ? transform : transform.parent).FindDeepChildLiberal(avatar.rigContainer);
                    }

                    if (rigRoot == null) rigRoot = transform;
                }

                return rigRoot;
            }
        }
        public override Transform BoundsRootTransform => rigRoot;

        [SerializeField]
        protected DynamicAnimationProperties animatablePropertiesController;
        [NonSerialized]
        protected List<DynamicAnimationProperties.Property> dynamicAnimationProperties;
        public void SetAnimatablePropertiesController(DynamicAnimationProperties controller)
        {
            if (dynamicAnimationProperties != null)
            {
                if (animatablePropertiesController != null)
                {
                    foreach (var prop in dynamicAnimationProperties) animatablePropertiesController.RemoveProperty(prop);
                }
                dynamicAnimationProperties.Clear();  
            }       

            animatablePropertiesController = controller;
            if (animatablePropertiesController == null) return; 

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            if (dynamicAnimationProperties == null) dynamicAnimationProperties = new List<DynamicAnimationProperties.Property>();;

            string id = GetInstanceID().ToString();
            for (int a = 0; a < CharacterMeshData.StandaloneShapesCount; a++)
            {
                var shape = CharacterMeshData.GetStandaloneShape(a);
                if (shape == null || !shape.animatable) continue;

                int shapeIndex = a;

                string name = $"SHAPE:{shape.name}";
                int index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                    continue;
                }

                var prop_ = animatablePropertiesController.CreateProperty(name, () => GetStandaloneShapeWeightUnsafe(shapeIndex), (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                dynamicAnimationProperties.Add(prop_);
            }
            for (int a = 0; a < CharacterMeshData.MuscleVertexGroupCount; a++)
            {
                var group = CharacterMeshData.GetMuscleVertexGroup(a);
                if (group == null || !group.flag) continue; // animatable permission is stored in vertex group flag field

                int shapeIndex = a;

                string name = $"FLEX_LEFT:{group.name}";
                int index = animatablePropertiesController.IndexOf(name); 
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }

                name = $"FLEX_RIGHT:{group.name}";
                index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex); 
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            var material = MeshGroup.GetMaterial(subMeshIndex);
            if (material != null)
            {
                hasBustSizeProperty = material.HasProperty(CharacterMeshData.BustMixPropertyName);
                hasHideNipplesProperty = material.HasProperty(CharacterMeshData.HideNipplesPropertyName);
                hasHideGenitalsProperty = material.HasProperty(CharacterMeshData.HideGenitalsPropertyName);  
            }

            SetAnimatablePropertiesController(animatablePropertiesController); 
        }

        protected override void OnStart()
        {
            base.OnStart();

            InitInstanceIDs();
            InitBuffers();
        }

        public override string RigID => rigRoot.GetInstanceID().ToString();

        [SerializeField]
        protected string rigBufferId;
        public void SetRigBufferID(string id) => rigBufferId = id;
        public string LocalRigBufferID => rigBufferId;
        public override string RigBufferID => rigInstanceReference != null ? rigInstanceReference.RigBufferID : rigBufferId;

        [SerializeField]
        protected string shapeBufferId;
        public void SetShapeBufferID(string id) => shapeBufferId = id;
        public string LocalShapeBufferID => shapeBufferId;
        public override string ShapeBufferID => shapesInstanceReference != null ? shapesInstanceReference.ShapeBufferID : shapeBufferId; 

        [SerializeField]
        protected string morphBufferId;
        public void SetMorphBufferID(string id) => morphBufferId = id;
        public string LocalMorphBufferID => morphBufferId; 
        public virtual string MorphBufferID => characterInstanceReference != null ? characterInstanceReference.MorphBufferID : morphBufferId;

        [NonSerialized]
        protected int shapesInstanceID;
        public int ShapesInstanceID => shapesInstanceID <= 0 ? InstanceSlot : (shapesInstanceID - 1);
        public InstanceableSkinnedMeshBase shapesInstanceReference;

        [NonSerialized]
        protected int rigInstanceID;
        public override int RigInstanceID => rigInstanceID <= 0 ? InstanceSlot : (rigInstanceID - 1);
        public InstanceableSkinnedMeshBase rigInstanceReference;

        public override Rigs.StandaloneSampler RigSampler
        {
            get
            {
                if (rigInstanceReference == null && rigInstanceID <= 0) return base.RigSampler;   
                return null;
            }
        }

        [NonSerialized]
        protected int characterInstanceID;
        public int CharacterInstanceID => characterInstanceID <= 0 ? InstanceSlot : (characterInstanceID - 1);
        public CustomizableCharacterMesh characterInstanceReference;

        public void SetShapesInstanceID(int id) 
        { 
            shapesInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, id);
                }
            }
        }
        public void SetRigInstanceID(int id) 
        { 
            rigInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, id);
                }
            }
        }
        public void SetCharacterInstanceID(int id) 
        { 
            characterInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, id);
                }
            }
        }

        protected virtual void InitInstanceIDs()
        {
            if (shapesInstanceReference != null) 
            { 
                SetShapesInstanceID(shapesInstanceReference.InstanceSlot);
                shapesInstanceReference.OnCreateInstanceID += SetShapesInstanceID; 
            }
            if (rigInstanceReference != null) 
            { 
                SetRigInstanceID(rigInstanceReference.InstanceSlot);
                rigInstanceReference.OnCreateInstanceID += SetRigInstanceID; 
            }
            if (characterInstanceReference != null) 
            {
                SetCharacterInstanceID(characterInstanceReference.InstanceSlot);
                characterInstanceReference.OnCreateInstanceID += SetCharacterInstanceID;
            }
        }

        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            base.CreateInstance(floatOverrides, colorOverrides, vectorOverrides);

            instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, ShapesInstanceID, false);
            instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, RigInstanceID, false);
            instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, CharacterInstanceID, true);
        }

        protected Transform[] bones;
        public override Transform[] Bones
        {
            get
            {
                if (bones == null)
                {
                    var rig_root = RigRoot;

                    if (avatar == null)
                    {
                        bones = new Transform[] { rig_root }; 
                    }
                    else
                    {
                        bones = new Transform[avatar.bones.Length];
                        for (int a = 0; a < avatar.bones.Length; a++) bones[a] = rig_root.FindDeepChildLiberal(avatar.bones[a]); 
                    }
                }

                return bones;
            }
        }

        public override int BoneCount => avatar == null ? 1 : avatar.bones.Length;

        public override Matrix4x4[] BindPose => meshData.ManagedBindPose;


        protected bool dirtyFlag_standaloneShapesControl;
        protected NativeArray<float> standaloneShapesControl;
        public NativeArray<float> StandaloneShapesControl
        {
            get
            {
                if (shapesInstanceReference is CustomizableCharacterMesh ccm) return ccm.StandaloneShapesControl;

                if (!standaloneShapesControl.IsCreated && !IsDestroyed)
                {
                    standaloneShapesControl = new NativeArray<float>(CharacterMeshData.StandaloneShapesCount, Allocator.Persistent);
                }

                return standaloneShapesControl;
            }
        }
        public float GetStandaloneShapeWeightUnsafe(int shapeIndex) => StandaloneShapesControl[shapeIndex];
        public float GetStandaloneShapeWeight(int shapeIndex)
        {
            if (shapeIndex < 0 || shapeIndex >= CharacterMeshData.StandaloneShapesCount) return 0;
            return GetStandaloneShapeWeightUnsafe(shapeIndex);
        }
        public void SetStandaloneShapeWeightUnsafe(int shapeIndex, float weight)
        {
            var array = StandaloneShapesControl;
            array[shapeIndex] = weight;

            dirtyFlag_standaloneShapesControl = true;
        }
        public void SetStandaloneShapeWeight(int shapeIndex, float weight)
        {
            if (shapeIndex < 0 || shapeIndex >= CharacterMeshData.StandaloneShapesCount) return; 
            SetStandaloneShapeWeightUnsafe(shapeIndex, weight);
        }
        internal readonly static Dictionary<string, InstanceBuffer<float>> _standaloneShapeControlBuffers = new Dictionary<string, InstanceBuffer<float>>();
        protected InstanceBuffer<float> standaloneShapeControlBuffer;
        public InstanceBuffer<float> StandaloneShapeControlBuffer
        {
            get
            {
                if (standaloneShapeControlBuffer == null)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{ShapeBufferID}.{nameof(StandaloneShapeControlBuffer)}";
                    string matProperty = CharacterMeshData.StandaloneShapesControlPropertyName;
                    if (!_standaloneShapeControlBuffers.TryGetValue(bufferID, out standaloneShapeControlBuffer) || standaloneShapeControlBuffer == null || !standaloneShapeControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float>(matProperty, CharacterMeshData.StandaloneShapesCount, out standaloneShapeControlBuffer);
                        _standaloneShapeControlBuffers[bufferID] = standaloneShapeControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            standaloneShapeControlBuffer.BindMaterialProperty(material, matProperty);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return standaloneShapeControlBuffer;
            }
        }

        protected bool dirtyFlag_muscleGroupsControl;
        protected NativeArray<MuscleDataLR> muscleGroupsControl;
        public NativeArray<MuscleDataLR> MuscleGroupsControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.MuscleGroupsControl;

                if (!muscleGroupsControl.IsCreated && !IsDestroyed)
                {
                    muscleGroupsControl = new NativeArray<MuscleDataLR>(CharacterMeshData.MuscleVertexGroupCount, Allocator.Persistent);
                }

                return muscleGroupsControl;
            }
        }
        public MuscleDataLR GetMuscleDataUnsafe(int groupIndex) => MuscleGroupsControl[groupIndex];
        public MuscleDataLR GetMuscleData(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.MuscleVertexGroupCount) return default;
            return GetMuscleDataUnsafe(groupIndex);
        }
        public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data)   
        {
            var array = MuscleGroupsControl;
            array[groupIndex] = data;

            dirtyFlag_muscleGroupsControl = true;
        }
        public void SetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.MuscleVertexGroupCount) return;
            SetMuscleDataUnsafe(groupIndex, data);
        }
        internal readonly static Dictionary<string, InstanceBuffer<MuscleDataLR>> _muscleGroupsControlBuffers = new Dictionary<string, InstanceBuffer<MuscleDataLR>>();
        protected InstanceBuffer<MuscleDataLR> muscleGroupsControlBuffer;
        public InstanceBuffer<MuscleDataLR> MuscleGroupsControlBuffer
        {
            get
            {
                if (muscleGroupsControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(MuscleGroupsControlBuffer)}";
                    string matProperty = CharacterMeshData.MuscleGroupsControlPropertyName;
                    if (!_muscleGroupsControlBuffers.TryGetValue(bufferID, out muscleGroupsControlBuffer) || muscleGroupsControlBuffer == null || !muscleGroupsControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<MuscleDataLR>(matProperty, CharacterMeshData.MuscleVertexGroupCount, out muscleGroupsControlBuffer);
                        _muscleGroupsControlBuffers[bufferID] = muscleGroupsControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            muscleGroupsControlBuffer.BindMaterialProperty(material, matProperty);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return muscleGroupsControlBuffer;
            }
        }

        protected bool dirtyFlag_fatGroupsControl;
        protected NativeArray<float3> fatGroupsControl;
        public NativeArray<float3> FatGroupsControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.FatGroupsControl;

                if (!fatGroupsControl.IsCreated && !IsDestroyed)
                {
                    fatGroupsControl = new NativeArray<float3>(CharacterMeshData.FatVertexGroupCount, Allocator.Persistent);
                    if (CharacterMeshData.fatGroupModifiers == null)
                    {
                        for (int a = 0; a < fatGroupsControl.Length; a++) fatGroupsControl[a] = new float3(0, CustomizableCharacterMeshData.DefaultFatGroupModifier.x, CustomizableCharacterMeshData.DefaultFatGroupModifier.y);
                    }
                    else
                    {
                        for (int a = 0; a < fatGroupsControl.Length; a++) 
                        {
                            var modifier = CharacterMeshData.GetFatGroupModifier(a);
                            fatGroupsControl[a] = new float3(0, modifier.x, modifier.y); 
                        }
                    }
                }

                return fatGroupsControl;
            }
        }
        public float GetFatLevelUnsafe(int groupIndex) => FatGroupsControl[groupIndex].x;
        public float GetFatLevel(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.FatVertexGroupCount) return 0;
            return GetFatLevelUnsafe(groupIndex);
        }
        public void SetFatLevelUnsafe(int groupIndex, float level)
        {
            var array = FatGroupsControl;
            var val = array[groupIndex];
            val.x = level;
            array[groupIndex] = val;

            dirtyFlag_fatGroupsControl = true;
        }
        public void SetFatLevel(int groupIndex, float level)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.FatVertexGroupCount) return;
            SetFatLevelUnsafe(groupIndex, level); 
        }
        internal readonly static Dictionary<string, InstanceBuffer<float3>> _fatGroupsControlBuffers = new Dictionary<string, InstanceBuffer<float3>>();
        protected InstanceBuffer<float3> fatGroupsControlBuffer;
        public InstanceBuffer<float3> FatGroupsControlBuffer
        {
            get
            {
                if (fatGroupsControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(FatGroupsControlBuffer)}";
                    string matProperty = CharacterMeshData.FatGroupsControlPropertyName;
                    if (!_fatGroupsControlBuffers.TryGetValue(bufferID, out fatGroupsControlBuffer) || fatGroupsControlBuffer == null || !fatGroupsControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float3>(matProperty, CharacterMeshData.FatVertexGroupCount, out fatGroupsControlBuffer);
                        _fatGroupsControlBuffers[bufferID] = fatGroupsControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            fatGroupsControlBuffer.BindMaterialProperty(material, matProperty); 
                        }

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return fatGroupsControlBuffer;
            }
        }

        protected bool dirtyFlag_variationShapesControl;
        protected NativeArray<float2> variationShapesControl;
        public NativeArray<float2> VariationShapesControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.VariationShapesControl;

                if (!variationShapesControl.IsCreated && !IsDestroyed)
                {
                    variationShapesControl = new NativeArray<float2>(CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount, Allocator.Persistent);
                }

                return variationShapesControl;
            }
        }
        public float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex) => VariationShapesControl[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex];
        public float2 GetVariationWeight(int variationShapeIndex, int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CharacterMeshData.VariationShapesCount) return 0;
            return GetVariationWeightUnsafe(variationShapeIndex, groupIndex); 
        }
        public void SetVariationWeightUnsafe(int variationShapeIndex, int groupIndex, float2 weight)
        {
            var array = VariationShapesControl;
            array[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex] = weight;

            dirtyFlag_variationShapesControl = true;
        }
        public void SetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            if (groupIndex < 0 || groupIndex >= CharacterMeshData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CharacterMeshData.VariationShapesCount) return;
            SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
        }

        public float2 GetVariationWeightUnsafe(int indexInArray) => VariationShapesControl[indexInArray];
        public float2 GetVariationWeight(int indexInArray)
        {
            if (indexInArray < 0 || indexInArray >= VariationShapesControl.Length) return 0;
            return GetVariationWeightUnsafe(indexInArray);
        }
        public void SetVariationWeightUnsafe(int indexInArray, float2 weight)
        {
            var array = VariationShapesControl;
            array[indexInArray] = weight;

            dirtyFlag_variationShapesControl = true; 
        }
        public void SetVariationWeight(int indexInArray, float2 weight)
        {
            if (indexInArray < 0 || indexInArray >= VariationShapesControl.Length) return;
            SetVariationWeightUnsafe(indexInArray, weight);
        }

        internal readonly static Dictionary<string, InstanceBuffer<float2>> _variationShapesControlBuffers = new Dictionary<string, InstanceBuffer<float2>>(); 
        protected InstanceBuffer<float2> variationShapesControlBuffer;
        public InstanceBuffer<float2> VariationShapesControlBuffer
        {
            get
            {
                if (variationShapesControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(VariationShapesControlBuffer)}";
                    string matProperty = CharacterMeshData.VariationShapesControlPropertyName;
                    if (!_variationShapesControlBuffers.TryGetValue(bufferID, out variationShapesControlBuffer) || variationShapesControlBuffer == null || !variationShapesControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float2>(matProperty, CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount, out variationShapesControlBuffer);  
                        _variationShapesControlBuffers[bufferID] = variationShapesControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            variationShapesControlBuffer.BindMaterialProperty(material, matProperty);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return variationShapesControlBuffer;
            }
        }

        protected void InitBuffers()
        {
            /*standaloneShapeControlBuffer = StandaloneShapeControlBuffer;
            muscleGroupsControlBuffer = MuscleGroupsControlBuffer;
            fatGroupsControlBuffer = FatGroupsControlBuffer;
            variationShapesControlBuffer = VariationShapesControlBuffer;*/
        }
        public void UpdateBuffers()
        {
            if (instance == null || IsDestroyed) return;

            if (dirtyFlag_standaloneShapesControl)
            {
                if (StandaloneShapeControlBuffer.RequestWriteToBuffer(StandaloneShapesControl, 0, InstanceSlot * CharacterMeshData.StandaloneShapesCount, CharacterMeshData.StandaloneShapesCount)) dirtyFlag_standaloneShapesControl = false;
            }

            if (dirtyFlag_muscleGroupsControl)
            {
                if (MuscleGroupsControlBuffer.RequestWriteToBuffer(MuscleGroupsControl, 0, InstanceSlot * CharacterMeshData.MuscleVertexGroupCount, CharacterMeshData.MuscleVertexGroupCount)) dirtyFlag_muscleGroupsControl = false;
            }

            if (dirtyFlag_fatGroupsControl)
            {
                if (FatGroupsControlBuffer.RequestWriteToBuffer(FatGroupsControl, 0, InstanceSlot * CharacterMeshData.FatVertexGroupCount, CharacterMeshData.FatVertexGroupCount)) dirtyFlag_fatGroupsControl = false; 
            }

            if (dirtyFlag_variationShapesControl)
            {
                int count = CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount;
                if (VariationShapesControlBuffer.RequestWriteToBuffer(VariationShapesControl, 0, InstanceSlot * count, count)) dirtyFlag_variationShapesControl = false;
            }
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleData : IEquatable<MuscleData>
    {
        [Range(0, 2)]
        public float mass;
        [Range(0, 1.5f)]
        public float flex;
        [Range(0, 1)]
        public float pump;

        public static implicit operator MuscleData(float3 data) => new MuscleData() { mass = data.x, flex = data.y, pump = data.z };
        public static implicit operator float3(MuscleData data) => new float3(data.mass, data.flex, data.pump);

        public override bool Equals(object obj)
        {
            if (obj is MuscleData dat) return dat.mass == mass && dat.flex == flex && dat.pump == pump;
            return false;
        }
        public bool Equals(MuscleData dat) => dat.mass == mass && dat.flex == flex && dat.pump == pump;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleData dat1, MuscleData dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleData dat1, MuscleData dat2) => !dat1.Equals(dat2);
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleDataLR : IEquatable<MuscleDataLR>
    {
        public MuscleData valuesLeft;
        public MuscleData valuesRight;

        public override bool Equals(object obj)
        {
            if (obj is MuscleDataLR dat) return dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;
            return false;
        }
        public bool Equals(MuscleDataLR dat) => dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleDataLR dat1, MuscleDataLR dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleDataLR dat1, MuscleDataLR dat2) => !dat1.Equals(dat2);
    }

}