#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;
using static Swole.InstancedRendering;

namespace Swole.API.Unity
{

    public class TileInstanceBehaviour : MonoBehaviour
    {
        [NonSerialized]
        protected TileInstance tileInstance;
        public TileInstance Tile => tileInstance; 

        [SerializeField]
        public int parentSwoleId = -1;
        [SerializeField]
        public TileSet tileSet; 
        [SerializeField]
        public int tileIndex;
        public void SetTileInstance(TileInstance tileInstance)
        {
            this.tileInstance = tileInstance;
            if (tileInstance == null) 
            {
                GameObject.Destroy(gameObject); 
                return;
            }

            tileSet = tileInstance.tileSet;
            tileIndex = tileInstance.tileIndex;
            parentSwoleId = tileInstance.SwoleId;
        }

        protected void OnDestroy()
        {
            if (tileInstance == null) return;

            tileInstance.Dispose();
            tileInstance = null;
        }

        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastLossyScale;
        public Transform lastParent;

        protected void LateUpdate()
        {
            if (tileInstance == null)
            {
                if (ReferenceEquals(tileInstance, null) && tileSet != null) // Never had an instance but there's a tile set, so this is an instantiated behaviour that needs a new tile instance object
                {
                    ICollection<MaterialPropertyInstanceOverride<float>> floatOverrides = null;
                    ICollection<MaterialPropertyInstanceOverride<Color>> colorOverrides = null;
                    ICollection<MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null;
                    if (parentSwoleId >= 0)
                    {
                        var root = swole.Engine.GetRootCreationInstance(gameObject);
                        if (root != null)
                        {
                            var parentInst = root.FindSwoleGameObject(parentSwoleId);
                            if (parentInst != null && parentInst.instance.instance is GameObject go)
                            { 
                                var parentComp = go.GetComponent<TileInstanceBehaviour>();
                                if (parentComp != null && parentComp.tileSet == tileSet && parentComp.tileIndex == tileIndex)
                                {
                                    floatOverrides = parentComp.Tile.FloatPropertyOverrides; // Already gets copied in constructor
                                    colorOverrides = parentComp.Tile.ColorPropertyOverrides == null ? null : new List<MaterialPropertyInstanceOverride<Color>>(parentComp.Tile.ColorPropertyOverrides);
                                    vectorOverrides = parentComp.Tile.VectorPropertyOverrides == null ? null : new List<MaterialPropertyInstanceOverride<Vector4>>(parentComp.Tile.VectorPropertyOverrides);
                                }
                            }
                        }

                        var swlObj = GetComponent<SwoleGameObject>();  
                        if (swlObj != null) Destroy(swlObj); // Remove cloned component
                    }
                    tileInstance = new TileInstance(tileSet, tileIndex, transform, -1, floatOverrides, colorOverrides, vectorOverrides);
                    var tile = tileSet[tileIndex];
                    if (tile != null) tileInstance.baseRenderingMatrix = Matrix4x4.TRS(UnityEngineHook.AsUnityVector(tile.PositionOffset), Quaternion.Euler(UnityEngineHook.AsUnityVector(tile.InitialRotationEuler)), UnityEngineHook.AsUnityVector(tile.InitialScale)); 
                    tileInstance.ReevaluateRendering();
                    return;
                }

                Destroy(this);
                return;
            }

            var t = transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
            Vector3 scale = t.lossyScale;
            Transform parent = t.parent;

            try
            {

                bool updatePos = pos != lastPosition;
                bool updateRot = rot != lastRotation;
                bool updateScale = scale != lastLossyScale;
                bool updateParent = parent != lastParent;

                if (updatePos) tileInstance.OnPositionChange(UnityEngineHook.AsSwoleVector(pos), !updateRot && !updateScale && !updateParent);
                if (updateRot) tileInstance.OnRotationChange(UnityEngineHook.AsSwoleQuaternion(rot), !updateScale && !updateParent);
                if (updateScale) tileInstance.OnScaleChange(UnityEngineHook.AsSwoleVector(scale), !updateParent); 
                if (updateParent) tileInstance.OnParentChange(UnityEngineHook.AsSwoleTransform(parent), true);

            } 
            catch(Exception ex)
            {
                swole.LogError(ex);
            }

            lastPosition = pos;
            lastRotation = rot;
            lastLossyScale = scale;
            lastParent = parent; 
        }

        public void ForceUpdateLastKnownTransformData()
        {
            var t = transform;
            ForceUpdateLastKnownTransformData(t.position, t.rotation, t.lossyScale, t.parent);
        }
        public void ForceUpdateLastKnownTransformData(Vector3 pos, Quaternion rot, Vector3 scale, Transform parent)
        {
            var t = transform;
            lastPosition = pos;
            lastRotation = rot;
            lastLossyScale = scale;
            lastParent = parent;
        }

        protected void OnEnable()
        {
            if (tileInstance == null) return;
            tileInstance.visible = true;
        }
        protected void OnDisable()
        {
            if (tileInstance == null) return;
            tileInstance.visible = false;
        }

    }

    /// <summary>
    /// A rendered instance of a tile
    /// </summary>
    public class TileInstance : ITileInstance
    {

        public System.Type EngineComponentType => GetType();

        public static bool operator ==(TileInstance lhs, object rhs)
        {
            if (ReferenceEquals(lhs, null)) return ReferenceEquals(rhs, null);
            if (rhs is TileInstance ts) return ReferenceEquals(lhs, ts);
            if (lhs.IsDestroyed && rhs == null) return true; 
            return false;
        }
        public static bool operator !=(TileInstance lhs, object rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        protected bool isDisposed;
        public bool IsDestroyed => isDisposed; 
        public static void Destroy(EngineInternal.IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
        public static void AdminDestroy(EngineInternal.IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
        public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
        public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);
        public void Dispose()
        {
            isDisposed = true;

            if (eventHandler != null) eventHandler.Dispose(); 
            if (renderingInstance != null)
            {
                renderingInstance.Dispose(); 
                renderingInstance = null;
            }
            if (outlineRenderingInstance != null)
            {
                outlineRenderingInstance.Dispose();
                outlineRenderingInstance = null;
            }
            if (rootTransform != null) 
            { 
                GameObject.Destroy(rootTransform.gameObject);
                rootTransform = null; 
            }
        }

        public EngineInternal.GameObject baseGameObject => rootTransform == null ? default : new EngineInternal.GameObject(rootTransform.gameObject, this); 

        public int InstanceID => rootTransform == null ? 0 : rootTransform.GetInstanceID(); 

        public int swoleId = -1;
        public int SwoleId 
        {
            get => swoleId;
            set => swoleId = value;
        }

        protected string id;
        public string ID 
        { 
            get
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    if (IsUsingProxyTransform)
                    {
                        id = $"swole_{RuntimeHandler.NextUniqueId}";
                    } 
                    else
                    {
                        id = UnityEngineHook.GetUnityObjectIDString(rootTransform);
                    }
                }

                return id;
            }
        }

        public TileSet tileSet;
        public string TileSetId => tileSet == null ? string.Empty : tileSet.ID;

        public int tileIndex;
        public int TileIndex => tileIndex;

        protected RenderingInstance<InstanceDataMatrixAndMotionVectors> renderingInstance;
        protected RenderingInstance<InstanceDataMatrixAndMotionVectors> outlineRenderingInstance;

        protected int subMeshIndex;
        protected bool canRender;
        protected bool isHidden;
        public bool visible 
        {
            get => !isHidden;
            set
            {
                if (isHidden == value) SetVisible(value);  
            }      
        }
        protected void SetVisible(bool visible, bool forceCreateRenderInstance = false)
        {
            isHidden = !visible;
            if (visible && !IsDestroyed)
            {
                if (forceCreateRenderInstance) 
                { 
                    CreateRenderingInstance(subMeshIndex);  
                }
                else if (canRender)
                {
                    if (tileSet == null || tileSet.material == null || tileSet.mesh == null) return;
                    var tile = tileSet[tileIndex];
                    if (tile == null || tile.SubModelId == SubModelID.None) return; // Don't render anything

                    var renderGroup = GetRenderGroup<InstanceDataMatrixAndMotionVectors>(tileSet.mesh, subMeshIndex);

                    var matrix = UnityEngineHook.AsUnityMatrix(localToWorldMatrix) * baseRenderingMatrix;
                    var data = new InstanceDataMatrixAndMotionVectors() { objectToWorld = matrix, prevObjectToWorld = matrix };

                    renderingInstance = renderGroup.AddNewInstance(tileSet.material, data, true, false, default, floatPropertyOverrides, colorPropertyOverrides, vectorPropertyOverrides);
                    if (tileSet.outlineMaterial != null) outlineRenderingInstance = renderGroup.AddNewInstance(tileSet.outlineMaterial, data, true, false, default, floatPropertyOverrides, colorPropertyOverrides, vectorPropertyOverrides);
                }
            } 
            else
            {
                if (renderingInstance != null) renderingInstance.Dispose();
                if (outlineRenderingInstance != null) outlineRenderingInstance.Dispose();
                renderingInstance = null;
                outlineRenderingInstance = null;
            }
        }
        protected void CreateRenderingInstance(int subMeshIndex = 0)
        {
            if (renderingInstance != null) renderingInstance.Dispose();
            if (outlineRenderingInstance != null) outlineRenderingInstance.Dispose();
            renderingInstance = null;
            outlineRenderingInstance = null;

            if (tileSet == null || tileSet.material == null || tileSet.mesh == null) return; 
            var tile = tileSet[tileIndex];
            canRender = !(tile == null || tile.SubModelId == SubModelID.None);
            this.subMeshIndex = subMeshIndex;
            SetVisible(!isHidden, false);
        }

        protected TileInstanceBehaviour behaviour;
        public TileInstanceBehaviour Behaviour => behaviour;
        public void ForceUseRealTransform() => ForceUseRealTransform(null); 
        public void ForceUseRealTransform(Transform transform)
        {
            bool createNewTransform = transform == null;
            if (createNewTransform && !IsUsingProxyTransform) return;

            if (createNewTransform)
            {
                EngineInternal.Vector3 worldPos = position;
                EngineInternal.Quaternion worldRot = rotation;
                EngineInternal.Vector3 ls = localScale;
                EngineInternal.ITransform par = parent;
                  
                rootTransform = transform;
                rootTransform.SetPositionAndRotation(UnityEngineHook.AsUnityVector(worldPos), UnityEngineHook.AsUnityQuaternion(worldRot));
                SetParent(par, true);
                localScale = ls;
            } 
            else
            {
                rootTransform = transform;
            }

            if (SwoleId >= 0)
            {
                var swoleObj = rootTransform.gameObject.AddOrGetComponent<SwoleGameObject>(); 
                swoleObj.id = SwoleId; 
            }

            behaviour = rootTransform.gameObject.AddOrGetComponent<TileInstanceBehaviour>();
            behaviour.SetTileInstance(this);

        }

        public void ReevaluateRendering()
        {
            if (renderingInstance == null) CreateRenderingInstance(subMeshIndex);
        }

        public bool IsRenderOnly => rootTransform == null;
        public Transform rootTransform;
        public Matrix4x4 baseRenderingMatrix;
        public EngineInternal.GameObject Root => rootTransform == null ? default : UnityEngineHook.AsSwoleGameObject(rootTransform.gameObject);

        protected List<MaterialPropertyInstanceOverride<float>> floatPropertyOverrides;
        public List<MaterialPropertyInstanceOverride<float>> FloatPropertyOverrides => floatPropertyOverrides;
        protected ICollection<MaterialPropertyInstanceOverride<Color>> colorPropertyOverrides;
        public ICollection<MaterialPropertyInstanceOverride<Color>> ColorPropertyOverrides => colorPropertyOverrides;
        protected ICollection<MaterialPropertyInstanceOverride<Vector4>> vectorPropertyOverrides;
        public ICollection<MaterialPropertyInstanceOverride<Vector4>> VectorPropertyOverrides => vectorPropertyOverrides;

        public const string _tileMaterialIdMaskPropertyName = "_IdMask";

        public TileInstance(TileSet tileSet, int tileIndex, Transform rootTransform = null, int swoleId = -1, 
            ICollection<MaterialPropertyInstanceOverride<float>> floatPropertyOverrides = default,
            ICollection<MaterialPropertyInstanceOverride<Color>> colorPropertyOverrides = default,
            ICollection<MaterialPropertyInstanceOverride<Vector4>> vectorPropertyOverrides = default)
        {
            baseRenderingMatrix = Matrix4x4.identity;

            this.tileSet = tileSet;
            this.tileIndex = tileIndex;
            if (rootTransform != null) ForceUseRealTransform(rootTransform); 
            this.SwoleId = swoleId;

            this.floatPropertyOverrides = floatPropertyOverrides == null ? new List<MaterialPropertyInstanceOverride<float>>() : new List<MaterialPropertyInstanceOverride<float>>(floatPropertyOverrides);
            var tile = tileSet == null ? null : tileSet[tileIndex];
            if (tile != null) 
            {
                this.floatPropertyOverrides.RemoveAll(i => i.propertyName == _tileMaterialIdMaskPropertyName);  
                this.floatPropertyOverrides.Add(new MaterialPropertyInstanceOverride<float>() { propertyName = _tileMaterialIdMaskPropertyName, value = (int)(tileSet.ignoreMeshMasking ? (tile.SubModelId == SubModelID.None ? SubModelID.A : SubModelID.None) : tile.SubModelId) }); // if ignoreMeshMasking is on, setting the submodel id to A should hide the mesh for a tile that has its submodelid set to none
            }
            this.colorPropertyOverrides = colorPropertyOverrides;
            this.vectorPropertyOverrides = vectorPropertyOverrides;
        }

        public bool IsUsingProxyTransform => rootTransform == null;
        protected Vector3 proxy_localPosition;
        protected Quaternion proxy_localRotation;
        protected Vector3 proxy_localScale;
        protected EngineInternal.ITransform proxy_parent;
        protected List<EngineInternal.ITransform> proxy_children;
        protected Matrix4x4 ProxyTRS
        {
            get
            {
                return Matrix4x4.TRS(proxy_localPosition, proxy_localRotation, proxy_localScale);
            }
        }

        public object Instance => IsUsingProxyTransform ? this : rootTransform;
        public bool HasEventHandler => false;
        protected EngineInternal.TransformEventHandler eventHandler; 
        protected void RefreshRenderingMatrix()
        {
            if (renderingInstance != null)
            {
                var data = renderingInstance.GetData();
                data.prevObjectToWorld = data.objectToWorld;
                data.objectToWorld = UnityEngineHook.AsUnityMatrix(localToWorldMatrix) * baseRenderingMatrix; 
                renderingInstance.SetData(data);
                if (outlineRenderingInstance != null) outlineRenderingInstance.SetData(data);
            }
        }
        public void OnPositionChange(EngineInternal.Vector3 pos, bool isFinal)
        {
            if (!isFinal || renderingInstance == null) return;

            RefreshRenderingMatrix();

            if (behaviour != null) behaviour.lastPosition = UnityEngineHook.AsUnityVector(pos);
        }
        public void OnRotationChange(EngineInternal.Quaternion rot, bool isFinal)
        {
            if (!isFinal || renderingInstance == null) return;

            RefreshRenderingMatrix();

            if (behaviour != null) behaviour.lastRotation = UnityEngineHook.AsUnityQuaternion(rot);
        }
        public void OnScaleChange(EngineInternal.Vector3 scale, bool isFinal)
        {
            if (!isFinal || renderingInstance == null) return;

            RefreshRenderingMatrix();

            if (behaviour != null) behaviour.lastLossyScale = UnityEngineHook.AsUnityVector(scale); 
        }
        public void OnParentChange(EngineInternal.ITransform parent, bool isFinal)
        {
            if (!isFinal || renderingInstance == null) return;

            RefreshRenderingMatrix();

            if (behaviour != null) behaviour.lastParent = UnityEngineHook.AsUnityTransform(parent);
        }

        public EngineInternal.TransformEventHandler TransformEventHandler 
        { 
            get
            {
                if (eventHandler == null)
                {
                    eventHandler = new EngineInternal.TransformEventHandler(this);
                    // necessary to detect when parent moves as well
                    eventHandler.onPositionChange += OnPositionChange;
                    eventHandler.onRotationChange += OnRotationChange;
                    eventHandler.onScaleChange += OnScaleChange;
                    eventHandler.onParentChange += OnParentChange;
                }
                return eventHandler;
            }
        }

        public IRuntimeEventHandler EventHandler => TransformEventHandler;

        protected EngineInternal.Vector3 lastPosition;
        protected EngineInternal.Quaternion lastRotation;
        protected EngineInternal.Vector3 lastScale;
        protected int lastParent;

        public EngineInternal.Vector3 LastPosition
        {
            get => lastPosition;
            set 
            { 
                lastPosition = value;
                if (behaviour != null) behaviour.lastPosition = UnityEngineHook.AsUnityVector(value);
            }
        }
        public EngineInternal.Quaternion LastRotation
        {
            get => lastRotation;
            set 
            { 
                lastRotation = value;
                if (behaviour != null) behaviour.lastRotation = UnityEngineHook.AsUnityQuaternion(value);
            }
        }
        public EngineInternal.Vector3 LastScale
        {
            get => lastScale;
            set 
            { 
                lastScale = value;
                if (behaviour != null) behaviour.lastLossyScale = UnityEngineHook.AsUnityVector(value);
            }
        }
        public int LastParent
        {
            get => lastParent;
            set 
            { 
                lastParent = value;
                if (behaviour != null) behaviour.lastParent = UnityEngineHook.AsUnityTransform(value);
            }
        }

        public string name => rootTransform == null ? $"tile {swoleId}" : rootTransform.name;
        public override string ToString() => name;

        public EngineInternal.ITransform parent { get => GetParent(); set => SetParent(value); }
        public EngineInternal.Vector3 position 
        {
            get
            {
                if (IsUsingProxyTransform)
                {
                    if (parent != null)
                        return parent.localToWorldMatrix.MultiplyPoint(localPosition); 
                    else
                        return localPosition;
                }
                return UnityEngineHook.AsSwoleVector(rootTransform.position);
            }
            set
            {
                if (IsUsingProxyTransform)
                {
                    var newPosition = value; 
                    EngineInternal.ITransform p = parent;
                    if (p != null)
                    {
                        newPosition = p.InverseTransformPoint(newPosition);
                    }
                    localPosition = newPosition;
                    return;
                }
                var unityVector = UnityEngineHook.AsUnityVector(value);
                rootTransform.position = unityVector;

                if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(unityVector, UnityEngineHook.AsUnityQuaternion(rotation), UnityEngineHook.AsUnityVector(lossyScale), UnityEngineHook.AsUnityTransform(parent));
                OnPositionChange(value, true);
            }
        }
        public EngineInternal.Quaternion rotation 
        {
            get 
            { 
                if (IsUsingProxyTransform)
                {
                    Quaternion worldRot = UnityEngineHook.AsUnityQuaternion(localRotation);
                    EngineInternal.ITransform p = parent;
                    while (p != null)
                    {
                        worldRot = UnityEngineHook.AsUnityQuaternion(p.localRotation) * worldRot; 
                        p = p.parent;
                    }

                    return UnityEngineHook.AsSwoleQuaternion(worldRot);
                }
                return UnityEngineHook.AsSwoleQuaternion(rootTransform.rotation);
            }
            set 
            {
                if (IsUsingProxyTransform)
                {
                    EngineInternal.ITransform p = parent;
                    if (p != null)
                        localRotation = UnityEngineHook.AsSwoleQuaternion(Quaternion.Normalize(Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(p.rotation)) * UnityEngineHook.AsUnityQuaternion(value)));
                    else
                        localRotation = UnityEngineHook.AsSwoleQuaternion(Quaternion.Normalize(UnityEngineHook.AsUnityQuaternion(value)));
                    return;
                }
                var unityQuaternion = UnityEngineHook.AsUnityQuaternion(value);
                rootTransform.rotation = unityQuaternion;

                if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(UnityEngineHook.AsUnityVector(position), unityQuaternion, UnityEngineHook.AsUnityVector(lossyScale), UnityEngineHook.AsUnityTransform(parent));
                OnRotationChange(value, true); 
            }
        }
        public EngineInternal.Vector3 lossyScale 
        {
            get => UnityEngineHook.AsSwoleVector(IsUsingProxyTransform ? GetWorldScaleLossy() : rootTransform.lossyScale);
        }
        public EngineInternal.Vector3 localPosition 
        {
            get => UnityEngineHook.AsSwoleVector(IsUsingProxyTransform ? proxy_localPosition : rootTransform.localPosition);
            set 
            {
                if (IsUsingProxyTransform)
                {
                    proxy_localPosition = UnityEngineHook.AsUnityVector(value);
                }
                else
                {
                    rootTransform.localPosition = UnityEngineHook.AsUnityVector(value);
                    if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(UnityEngineHook.AsUnityVector(position), UnityEngineHook.AsUnityQuaternion(rotation), UnityEngineHook.AsUnityVector(lossyScale), UnityEngineHook.AsUnityTransform(parent));
                }
                OnPositionChange(position, true);
            }
        }
        public EngineInternal.Quaternion localRotation 
        { 
            get => UnityEngineHook.AsSwoleQuaternion(IsUsingProxyTransform ? proxy_localRotation : rootTransform.localRotation);
            set
            {
                if (IsUsingProxyTransform)
                {
                    proxy_localRotation = UnityEngineHook.AsUnityQuaternion(value);
                }
                else
                {
                    rootTransform.localRotation = UnityEngineHook.AsUnityQuaternion(value);
                    if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(UnityEngineHook.AsUnityVector(position), UnityEngineHook.AsUnityQuaternion(rotation), UnityEngineHook.AsUnityVector(lossyScale), UnityEngineHook.AsUnityTransform(parent));
                }
                OnRotationChange(rotation, true); 
            }
        }
        public EngineInternal.Vector3 localScale 
        { 
            get => UnityEngineHook.AsSwoleVector(IsUsingProxyTransform ? proxy_localScale : rootTransform.localScale);
            set
            {
                if (IsUsingProxyTransform)
                {
                    proxy_localScale = UnityEngineHook.AsUnityVector(value);
                }
                else
                {

                    rootTransform.localScale = UnityEngineHook.AsUnityVector(value);
                    if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(UnityEngineHook.AsUnityVector(position), UnityEngineHook.AsUnityQuaternion(rotation), UnityEngineHook.AsUnityVector(lossyScale), UnityEngineHook.AsUnityTransform(parent));
                }
                OnScaleChange(lossyScale, true);
            }
        }

        protected Matrix4x4 GetWorldScale()
        {
            Matrix4x4 invRotation = new Matrix4x4();
            Maths.QuaternionToMatrix(Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(rotation)), ref invRotation);
            Matrix4x4 scaleAndRotation = Maths.GetWorldRotationAndScale(this);
            return invRotation * scaleAndRotation;
        }

        protected Vector3 GetWorldScaleLossy()
        {
            Matrix4x4 rot = GetWorldScale();
            return new Vector3(rot[0, 0], rot[1, 1], rot[2, 2]);
        }

        public EngineInternal.Matrix4x4 worldToLocalMatrix 
        {
            get
            {
                return UnityEngineHook.AsSwoleMatrix(UnityEngineHook.AsUnityMatrix(localToWorldMatrix).inverse); 
            }
        }

        public EngineInternal.Matrix4x4 localToWorldMatrix
        {
            get
            {
                if (IsUsingProxyTransform)
                {
                    Matrix4x4 t = new Matrix4x4();
                    t.SetTRS(UnityEngineHook.AsUnityVector(localPosition), UnityEngineHook.AsUnityQuaternion(localRotation), UnityEngineHook.AsUnityVector(localScale));
                    if (parent != null)
                    {
                        t = UnityEngineHook.AsUnityMatrix(parent.localToWorldMatrix) * t;
                    }

                    return UnityEngineHook.AsSwoleMatrix(t);
                }

                return UnityEngineHook.AsSwoleMatrix(rootTransform.localToWorldMatrix);
            }
        }

        public int childCount => IsUsingProxyTransform ? (proxy_children == null ? 0 : proxy_children.Count) : rootTransform.childCount;

        public EngineInternal.ITransform GetParent()
        {
            if (IsUsingProxyTransform) return proxy_parent;
            return UnityEngineHook.AsSwoleTransform(rootTransform.parent); 
        }

        public void SetParent(EngineInternal.ITransform p) => SetParent(p, false);
        public void SetParent(EngineInternal.ITransform newParent, bool worldPositionStays)
        {
            SetParent(newParent, worldPositionStays, (newParent is EngineInternal.Transform && !swole.Engine.IsExperienceRoot(newParent))); // If the new parent is a unity transform and isn't the root transform of the gameplay experience, then this instance should switch to using a unity transform
        }
        public void SetParent(EngineInternal.ITransform newParent, bool worldPositionStays, bool forceRealTransformConversion)
        {
            if (forceRealTransformConversion) ForceUseRealTransform();

            if (IsUsingProxyTransform)
            {
                if (newParent == parent) return;
                if (newParent == this || this.IsParentOf(newParent)) return; 

                // Save the old position in worldspace
                Vector3 worldPosition = new Vector3();
                Quaternion worldRotation = Quaternion.identity;
                Matrix4x4 worldScale = new Matrix4x4();

                if (worldPositionStays)
                {
                    worldPosition = UnityEngineHook.AsUnityVector(position);
                    worldRotation = UnityEngineHook.AsUnityQuaternion(rotation);
                    worldScale = Maths.GetWorldRotationAndScale(this); 
                }

                EngineInternal.ITransform previousParent = parent;
                if (previousParent != null)
                {
                    swole.Engine.UntrackTransform(previousParent); 
                    var prevParentEvents = previousParent.TransformEventHandler;
                    prevParentEvents.onPositionChange -= OnPositionChange;
                    prevParentEvents.onRotationChange -= OnRotationChange;
                    prevParentEvents.onScaleChange -= OnScaleChange;
                    prevParentEvents.onParentChange -= OnParentChange;

                    if (previousParent is TileInstance ti)
                    {
                        if (ti.proxy_children != null) ti.proxy_children.Remove(this);
                    }
                    else if (previousParent is EngineInternal.TileInstance iti)
                    {
                        if (iti.instance is TileInstance ti_)
                        {
                            if (ti_.proxy_children != null) ti_.proxy_children.Remove(this);
                        }
                    }
                }

                if (newParent != null)
                {
                    swole.Engine.TrackTransform(newParent);
                    var newParentEvents = newParent.TransformEventHandler;
                    newParentEvents.onPositionChange += OnPositionChange;
                    newParentEvents.onRotationChange += OnRotationChange;
                    newParentEvents.onScaleChange += OnScaleChange;
                    newParentEvents.onParentChange += OnParentChange;  

                    if (newParent is TileInstance ti)
                    {
                        if (ti.proxy_children == null) ti.proxy_children = new List<EngineInternal.ITransform>();
                        ti.proxy_children.Add(this);
                    }
                    else if (newParent is EngineInternal.TileInstance iti)
                    {
                        if (iti.instance is TileInstance ti_)
                        {
                            if (ti_.proxy_children == null) ti_.proxy_children = new List<EngineInternal.ITransform>();
                            ti_.proxy_children.Add(this);
                        }
                    }
                }

                proxy_parent = newParent; 

                if (worldPositionStays)
                {
                    SetPositionAndRotation(UnityEngineHook.AsSwoleVector(worldPosition), UnityEngineHook.AsSwoleQuaternion(worldRotation));
                    SetWorldRotationAndScale(worldScale);
                }

            }
            else
            {
                if (newParent is ITileInstance tile)
                {
                    tile.ForceUseRealTransform();
                }
                var unityParent = UnityEngineHook.AsUnityTransform(newParent);
                rootTransform.SetParent(unityParent, worldPositionStays);
                 
                if (behaviour != null) behaviour.ForceUpdateLastKnownTransformData(UnityEngineHook.AsUnityVector(position), UnityEngineHook.AsUnityQuaternion(rotation), UnityEngineHook.AsUnityVector(lossyScale), unityParent);
            }

            OnParentChange(newParent, true);
        }

        protected void SetWorldRotationAndScale(Matrix4x4 scale)
        {
            localScale = EngineInternal.Vector3.one;
            
            Matrix4x4 inverseRS = Maths.GetWorldRotationAndScale(this);
            inverseRS = inverseRS.inverse;

            inverseRS = inverseRS * scale;

            localScale = new EngineInternal.Vector3(inverseRS[0, 0], inverseRS[1, 1], inverseRS[2, 2]);  
        }
        public void SetPositionAndRotation(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation)
        {
            if (IsUsingProxyTransform)
            {
                Vector3 localPos;
                Quaternion localRot;
                if (parent != null)
                {
                    localPos = UnityEngineHook.AsUnityVector(parent.InverseTransformPoint(position));
                    localRot = Quaternion.Normalize(Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(parent.rotation)) * UnityEngineHook.AsUnityQuaternion(rotation));
                }
                else
                {
                    localPos = UnityEngineHook.AsUnityVector(position);
                    localRot = Quaternion.Normalize(UnityEngineHook.AsUnityQuaternion(rotation));
                }

                this.localPosition = UnityEngineHook.AsSwoleVector(localPos);
                this.localRotation = UnityEngineHook.AsSwoleQuaternion(localRot);
            } 
            else
            {
                rootTransform.SetPositionAndRotation(UnityEngineHook.AsUnityVector(position), UnityEngineHook.AsUnityQuaternion(rotation));
            }
        }

        public void SetLocalPositionAndRotation(EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation)
        {
            if (IsUsingProxyTransform)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
            }
            else
            {
                rootTransform.SetLocalPositionAndRotation(UnityEngineHook.AsUnityVector(localPosition), UnityEngineHook.AsUnityQuaternion(localRotation));
            }
        }

        public void GetPositionAndRotation(out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation)
        {
            if (IsUsingProxyTransform)
            {
                Vector3 worldPos = proxy_localPosition;
                Quaternion worldRot = proxy_localRotation;
                EngineInternal.ITransform par = parent;
                while (par != null)
                {
                    var ls = par.localScale;
                    worldPos.x = worldPos.x * ls.x;
                    worldPos.y = worldPos.y * ls.y;
                    worldPos.z = worldPos.z * ls.z; 

                    worldPos = UnityEngineHook.AsUnityQuaternion(par.localRotation) * worldPos;
                    worldPos += UnityEngineHook.AsUnityVector(par.localPosition);

                    worldRot = UnityEngineHook.AsUnityQuaternion(par.localRotation) * worldRot;

                    par = par.parent;
                }

                position = UnityEngineHook.AsSwoleVector(worldPos);
                rotation = UnityEngineHook.AsSwoleQuaternion(worldRot); 
            }
            else
            {
                rootTransform.GetPositionAndRotation(out var tempP, out var tempR);
                position = UnityEngineHook.AsSwoleVector(tempP);
                rotation = UnityEngineHook.AsSwoleQuaternion(tempR);
            }
        }

        public void GetLocalPositionAndRotation(out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation)
        {
            if (IsUsingProxyTransform)
            {
                localPosition = UnityEngineHook.AsSwoleVector(proxy_localPosition);
                localRotation = UnityEngineHook.AsSwoleQuaternion(proxy_localRotation);
            }
            else
            {
                rootTransform.GetLocalPositionAndRotation(out var tempP, out var tempR);
                localPosition = UnityEngineHook.AsSwoleVector(tempP);
                localRotation = UnityEngineHook.AsSwoleQuaternion(tempR);
            }
        }

        public EngineInternal.Vector3 TransformDirection(EngineInternal.Vector3 direction)
        {
            var dir = UnityEngineHook.AsUnityVector(direction);

            if (IsUsingProxyTransform)
            {
                dir = UnityEngineHook.AsUnityQuaternion(rotation) * dir;
            } 
            else
            {
                dir = rootTransform.TransformDirection(dir);
            }

            return UnityEngineHook.AsSwoleVector(dir);
        }
        public EngineInternal.Vector3 TransformDirection(float x, float y, float z) => TransformDirection(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.Vector3 InverseTransformDirection(EngineInternal.Vector3 direction)
        {
            var dir = UnityEngineHook.AsUnityVector(direction);

            if (IsUsingProxyTransform)
            {
                dir = Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(rotation)) * dir;
            }
            else
            {
                dir = rootTransform.InverseTransformDirection(dir);
            }

            return UnityEngineHook.AsSwoleVector(dir);
        }
        public EngineInternal.Vector3 InverseTransformDirection(float x, float y, float z) => InverseTransformDirection(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.Vector3 TransformVector(EngineInternal.Vector3 vector)
        {
            if (IsUsingProxyTransform)
            {
                Vector3 worldVector = UnityEngineHook.AsUnityVector(vector);

                EngineInternal.ITransform par = this;
                while (par != null)
                {
                    var ls = par.localScale;
                    worldVector.x = worldVector.x * ls.x;
                    worldVector.y = worldVector.y * ls.y;
                    worldVector.z = worldVector.z * ls.z;

                    worldVector = UnityEngineHook.AsUnityQuaternion(par.localRotation) * worldVector;

                    par = par.parent;
                }

                return UnityEngineHook.AsSwoleVector(worldVector);
            }

            return UnityEngineHook.AsSwoleVector(rootTransform.TransformVector(UnityEngineHook.AsUnityVector(vector)));
        }
        public EngineInternal.Vector3 TransformVector(float x, float y, float z) => TransformVector(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.Vector3 InverseTransformVector(EngineInternal.Vector3 vector)
        {
            if (IsUsingProxyTransform)
            {
                Vector3 newVector, localVector;
                EngineInternal.ITransform par = parent;
                if (par != null)
                    localVector = UnityEngineHook.AsUnityVector(par.InverseTransformVector(vector));
                else
                    localVector = UnityEngineHook.AsUnityVector(vector);

                newVector = Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(localRotation)) * localVector;
                
                var ls = Maths.InverseSafe(UnityEngineHook.AsUnityVector(localScale));
                newVector.x = newVector.x * ls.x;
                newVector.y = newVector.y * ls.y;
                newVector.z = newVector.z * ls.z;

                return UnityEngineHook.AsSwoleVector(newVector);
            }

            return UnityEngineHook.AsSwoleVector(rootTransform.InverseTransformVector(UnityEngineHook.AsUnityVector(vector)));
        }
        public EngineInternal.Vector3 InverseTransformVector(float x, float y, float z) => InverseTransformVector(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.Vector3 TransformPoint(EngineInternal.Vector3 position) 
        { 
            if (IsUsingProxyTransform) return localToWorldMatrix.MultiplyPoint(position);

            return UnityEngineHook.AsSwoleVector(rootTransform.TransformPoint(UnityEngineHook.AsUnityVector(position)));
        }
        public EngineInternal.Vector3 TransformPoint(float x, float y, float z) => TransformPoint(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.Vector3 InverseTransformPoint(EngineInternal.Vector3 position)
        {
            if (IsUsingProxyTransform)
            {
                Vector3 newPosition, localPosition;
                EngineInternal.ITransform par = parent;
                if (par != null)
                    localPosition = UnityEngineHook.AsUnityVector(par.InverseTransformPoint(position));
                else
                    localPosition = UnityEngineHook.AsUnityVector(position);

                localPosition -= UnityEngineHook.AsUnityVector(this.localPosition);
                newPosition = Quaternion.Inverse(UnityEngineHook.AsUnityQuaternion(localRotation)) * localPosition;
                
                var ls = Maths.InverseSafe(UnityEngineHook.AsUnityVector(localScale));
                newPosition.x = newPosition.x * ls.x;
                newPosition.y = newPosition.y * ls.y;
                newPosition.z = newPosition.z * ls.z;

                return UnityEngineHook.AsSwoleVector(newPosition);
            }

            return UnityEngineHook.AsSwoleVector(rootTransform.InverseTransformPoint(UnityEngineHook.AsUnityVector(position)));
        }
        public EngineInternal.Vector3 InverseTransformPoint(float x, float y, float z) => InverseTransformPoint(new EngineInternal.Vector3(x, y, z));

        public EngineInternal.ITransform Find(string n)
        {
            if (IsUsingProxyTransform)
            {
                if (proxy_children == null) return default;
                foreach (var child in proxy_children) if (child != null && child.name == n) return child;
            }

            return UnityEngineHook.AsSwoleTransform(rootTransform.Find(n));
        }

        private List<EngineInternal.ITransform> loopSafety = new List<EngineInternal.ITransform>();
        public bool IsChildOf(EngineInternal.ITransform parent)
        {
            loopSafety.Clear();
            var par = parent;
            while(par != null && !loopSafety.Contains(par))
            {
                for(int a = 0; a < par.childCount; a++)
                {
                    var child = par.GetChild(a);
                    if (child != null && ReferenceEquals(child.Instance, Instance)) return true;
                }

                loopSafety.Add(par);
                par = par.parent;
            }

            return false;
        }
        public bool IsParentOf(EngineInternal.ITransform child)
        {
            if (IsUsingProxyTransform)
            {
                if (proxy_children != null && proxy_children.Contains(child)) return true;
            }

            loopSafety.Clear();
            var par = child.parent;
            while (par != null && !loopSafety.Contains(par))
            {
                if (par == this) return true;

                loopSafety.Add(par);
                par = par.parent;
            }

            return false;
        }

        public EngineInternal.ITransform GetChild(int index)
        {
            if (IsUsingProxyTransform)
            {
                if (proxy_children == null || index < 0 || index >= proxy_children.Count) return default;
                return proxy_children[index];
            }

            return UnityEngineHook.AsSwoleTransform(rootTransform.GetChild(index));
        }
    }

}

#endif
