#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// The camera used during play mode. Avoids being destroyed or disabled at all costs.
    /// </summary>
    public class PlayModeCamera : MonoBehaviour, EngineInternal.ITransform
    {

        public System.Type EngineComponentType => GetType();

        public static PlayModeCamera activeInstance;
        public static bool HasActiveInstance => activeInstance != null && activeInstance.camera != null;

        new public Camera camera;

        protected void Awake()
        {
            if (activeInstance == null || activeInstance.camera == null) activeInstance = this;
            if (camera == null) camera = gameObject.GetComponentInChildren<Camera>(true);
            if (camera == null)
            {
                var mainCam = Camera.main;
                camera = gameObject.AddComponent<Camera>();
                if (mainCam != null)
                {
                    if (swole.IsInPlayMode) mainCam.gameObject.SetActive(false); 
                    camera.fieldOfView = mainCam.fieldOfView;
                    camera.orthographic = false;
                    camera.nearClipPlane = mainCam.nearClipPlane;
                    camera.farClipPlane = mainCam.farClipPlane;
                } 
                else
                {
                    camera.fieldOfView = 60;
                    camera.orthographic = false;
                }
                camera.gameObject.tag = "Untagged";
            }
        }

        protected void LateUpdate()
        {
            Sync();
        }

        private bool quitting;
        private bool destroyed;
        protected void OnApplicationQuit()
        {
            quitting = true;
        }
        protected void OnDestroy()
        {
            destroyed = true;
            if (swole.IsInPlayMode && activeInstance == this && gameObject.scene.isLoaded && !quitting) // make sure scene isn't being changed and app isnt quitting
            {
                Camera nextCam = null;

                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    nextCam = Instantiate(mainCam);
                    nextCam.orthographic = mainCam.orthographic;
                    nextCam.orthographicSize = mainCam.orthographicSize;
                    nextCam.fieldOfView = nextCam.fieldOfView;
                    nextCam.nearClipPlane = nextCam.nearClipPlane;
                    nextCam.farClipPlane = nextCam.farClipPlane;

                    nextCam.transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);
                }
                else
                {
                    nextCam = new GameObject().AddComponent<Camera>();
                    nextCam.orthographic = camera.orthographic;
                    nextCam.orthographicSize = camera.orthographicSize;
                    nextCam.fieldOfView = camera.fieldOfView;
                    nextCam.nearClipPlane = camera.nearClipPlane;
                    nextCam.farClipPlane = camera.farClipPlane;

                    nextCam.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
                }

                nextCam.name = name;
                nextCam.gameObject.SetActive(true);
                nextCam.gameObject.tag = "Untagged";

                activeInstance = nextCam.gameObject.AddComponent<PlayModeCamera>();
                activeInstance.camera = nextCam;
            }

        }

        // Use a proxy so that the camera can't be destroyed or disabled by its 'parent'
        protected EngineInternal.Transform proxyTransform; 
        public EngineInternal.Transform ProxyTransform
        {
            get
            {
                if (swole.IsNull(proxyTransform) && !destroyed && !quitting)
                {
                    proxyTransform = UnityEngineHook.AsSwoleTransform(new GameObject("_playModeCamera").transform);
                    if (transform.parent != null) proxyTransform.SetParent(UnityEngineHook.AsSwoleTransform(transform.parent));
                    proxyTransform.SetPositionAndRotation(UnityEngineHook.AsSwoleVector(transform.position), UnityEngineHook.AsSwoleQuaternion(transform.rotation));
                    proxyTransform.localScale = UnityEngineHook.AsSwoleVector(transform.localScale);

                    var handler = proxyTransform.TransformEventHandler; 
                    handler.onPositionChange += SyncPosition;
                    handler.onRotationChange += SyncRotation;
                    handler.onParentChange += Sync;
                }
                return proxyTransform;
            }
        }
        protected void Sync(EngineInternal.ITransform parent, bool isFinal) => Sync();
        protected void Sync()
        {
            if (camera != null && !camera.enabled) camera.enabled = true;
            if (swole.IsNotNull(ProxyTransform))
            {
                proxyTransform.GetPositionAndRotation(out var pos, out var rot);
                SyncPositionAndRotation(pos, rot);
            }
        }
        protected void SyncPosition(EngineInternal.Vector3 v, bool isFinal)
        {
            transform.position = UnityEngineHook.AsUnityVector(v);
        }
        protected void SyncRotation(EngineInternal.Quaternion q, bool isFinal)
        {
            transform.rotation = UnityEngineHook.AsUnityQuaternion(q);
        }
        protected void SyncPositionAndRotation(EngineInternal.Vector3 v, EngineInternal.Quaternion q)
        {
            transform.SetPositionAndRotation(UnityEngineHook.AsUnityVector(v), UnityEngineHook.AsUnityQuaternion(q));
        }

        #region ITransform

        public string ID => UnityEngineHook.GetUnityObjectIDString(UnityEngineHook.AsUnityTransform(ProxyTransform));

        public EngineInternal.TransformEventHandler TransformEventHandler => swole.IsNull(ProxyTransform) ? null : proxyTransform.TransformEventHandler;

        public int LastParent 
        {
            get 
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.LastParent;
            }
            set 
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.LastParent = value;
            }
        }
        public EngineInternal.Vector3 LastPosition
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.LastPosition;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.LastPosition = value;
            }
        }
        public EngineInternal.Quaternion LastRotation
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.LastRotation;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.LastRotation = value;
            }
        }
        public EngineInternal.Vector3 LastScale
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.LastScale;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.LastScale = value;
            }
        }
        public EngineInternal.ITransform parent
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.parent;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.parent = value;
            }
        }
        public EngineInternal.Vector3 position
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.position;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.position = value;
            }
        }
        public EngineInternal.Quaternion rotation
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.rotation;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.rotation = value;
            }
        }

        public EngineInternal.Vector3 lossyScale => swole.IsNull(ProxyTransform) ? default : proxyTransform.lossyScale;

        public EngineInternal.Vector3 localPosition
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.localPosition;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.localPosition = value;
            }
        }
        public EngineInternal.Quaternion localRotation
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.localRotation;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.localRotation = value;
            }
        }
        public EngineInternal.Vector3 localScale
        {
            get
            {
                if (swole.IsNull(ProxyTransform)) return default;
                return proxyTransform.localScale;
            }
            set
            {
                if (swole.IsNull(ProxyTransform)) return;
                proxyTransform.localScale = value;
            }
        }

        public EngineInternal.Matrix4x4 worldToLocalMatrix => swole.IsNull(ProxyTransform) ? default : proxyTransform.worldToLocalMatrix;

        public EngineInternal.Matrix4x4 localToWorldMatrix => swole.IsNull(ProxyTransform) ? default : proxyTransform.localToWorldMatrix;

        public int childCount => swole.IsNull(ProxyTransform) ? default : proxyTransform.childCount;

        public EngineInternal.GameObject baseGameObject => swole.IsNull(ProxyTransform) ? default : proxyTransform.baseGameObject;

        public object Instance => swole.IsNull(ProxyTransform) ? default : proxyTransform.Instance;

        public int InstanceID => swole.IsNull(ProxyTransform) ? default : proxyTransform.InstanceID;

        public bool IsDestroyed => swole.IsNull(ProxyTransform) ? default : proxyTransform.IsDestroyed;

        public bool HasEventHandler => swole.IsNull(ProxyTransform) ? default : proxyTransform.HasEventHandler;

        public IRuntimeEventHandler EventHandler => swole.IsNull(ProxyTransform) ? default : proxyTransform.EventHandler;

        public EngineInternal.ITransform GetParent()
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.GetParent();
        }

        public void SetParent(EngineInternal.ITransform p)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.SetParent(p);
        }

        public void SetParent(EngineInternal.ITransform parent, bool worldPositionStays)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.SetParent(parent, worldPositionStays);
        }

        public void SetPositionAndRotation(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.SetPositionAndRotation(position, rotation);
        }

        public void SetLocalPositionAndRotation(EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.SetLocalPositionAndRotation(localPosition, localRotation);
        }

        public void GetPositionAndRotation(out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation)
        {
            position = default;
            rotation = default;
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.GetPositionAndRotation(out position, out rotation);
        }

        public void GetLocalPositionAndRotation(out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation)
        {
            localPosition = default;
            localRotation = default;
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.GetLocalPositionAndRotation(out localPosition, out localRotation);
        }

        public EngineInternal.Vector3 TransformDirection(EngineInternal.Vector3 direction)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformDirection(direction);
        }

        public EngineInternal.Vector3 TransformDirection(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformDirection(x, y, z);
        }

        public EngineInternal.Vector3 InverseTransformDirection(EngineInternal.Vector3 direction)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformDirection(direction);
        }

        public EngineInternal.Vector3 InverseTransformDirection(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformDirection(x, y, z);
        }

        public EngineInternal.Vector3 TransformVector(EngineInternal.Vector3 vector)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformVector(vector);
        }

        public EngineInternal.Vector3 TransformVector(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformVector(x, y, z);
        }

        public EngineInternal.Vector3 InverseTransformVector(EngineInternal.Vector3 vector)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformVector(vector);
        }

        public EngineInternal.Vector3 InverseTransformVector(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformVector(x, y, z);
        }

        public EngineInternal.Vector3 TransformPoint(EngineInternal.Vector3 position)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformPoint(position);
        }

        public EngineInternal.Vector3 TransformPoint(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.TransformPoint(x, y, z);
        }

        public EngineInternal.Vector3 InverseTransformPoint(EngineInternal.Vector3 position)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformPoint(position);
        }

        public EngineInternal.Vector3 InverseTransformPoint(float x, float y, float z)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.InverseTransformPoint(x, y, z);
        }

        public EngineInternal.ITransform Find(string n)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.Find(n);
        }

        public bool IsChildOf(EngineInternal.ITransform parent)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.IsChildOf(parent);
        }

        public EngineInternal.ITransform GetChild(int index)
        {
            if (swole.IsNull(ProxyTransform)) return default;
            return proxyTransform.GetChild(index);
        }

        public void Destroy(float timeDelay = 0)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.Destroy(timeDelay);
        }

        public void AdminDestroy(float timeDelay = 0)
        {
            if (swole.IsNull(ProxyTransform)) return;
            proxyTransform.AdminDestroy(timeDelay);
        }

        #endregion

    }
}

#endif