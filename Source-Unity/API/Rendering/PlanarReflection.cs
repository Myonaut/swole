#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class PlanarReflection : MonoBehaviour
    {

        public const string _ReflectionTag = "Reflection";

#if UNITY_EDITOR
        protected void OnDrawGizmosSelected()
        {
            if (mainCameraTransform != null && reflectionCameraTransform != null)
            {
                float dist = (mainCameraTransform.position - reflectionCameraTransform.position).magnitude;

                Gizmos.DrawRay(mainCameraTransform.position, mainCameraTransform.forward * dist * 3);
                Gizmos.DrawRay(reflectionCameraTransform.position, reflectionCameraTransform.forward * dist * 3);  
            }
        }
#endif

        [Serializable]
        public enum UpdateMode
        {
            EveryFrame, CameraMoved, Manual
        }

        [SerializeField] public UpdateMode updateMode;  
        protected void SetUpdateMode(UpdateMode updateMode)
        {
            this.updateMode = updateMode;
            if (updateMode == UpdateMode.Manual)
            {
                PlanarReflections.Unregister(this);
            } 
            else
            {
                PlanarReflections.Register(this);
            }
            if (updateMode == UpdateMode.CameraMoved) reflectionCamera.enabled = false;
        }

        [SerializeField] protected float heightOffset;
        public void SetHeightOffset(float offset)
        {
            heightOffset = offset;

            mainCameraPrevTransformState = default; // force update
        }

        [SerializeField] protected Camera mainCamera;
        public Camera MainCamera
        {
            get => mainCamera;
            set => SetMainCamera(value);
        }
        protected Transform mainCameraTransform;
        public virtual void SetMainCamera(Camera camera)
        {
            if (camera == null) camera = Camera.main;

            mainCamera = camera;
            mainCameraTransform = camera == null ? null : camera.transform;
        }
        protected TransformState mainCameraPrevTransformState;
        public bool CheckIfMainCameraHasMoved(bool reset = true)
        {
            var state = new TransformState(mainCameraTransform, true);
            if (state != mainCameraPrevTransformState)
            {
                if (reset) mainCameraPrevTransformState = state;
                return true;
            }

            return false;
        }


        [SerializeField] protected Camera reflectionCamera;
        public bool allowPostProcessing;

        [SerializeField] protected bool useCustomCullingMask;
        [SerializeField] protected LayerMask cullingMask;
        public void SetUseCustomCullingMask(bool value) 
        {
            useCustomCullingMask = value;
            SetReflectionCamera(reflectionCamera);
        }
        public void SetCullingMask(LayerMask cullingMask)
        {
            this.cullingMask = cullingMask;
            SetReflectionCamera(reflectionCamera);
        }

        [SerializeField] protected bool useCustomVolumeMask;
        [SerializeField] protected LayerMask volumeMask;
        public void SetUseCustomVolumeMask(bool value)
        {
            useCustomVolumeMask = value;
            SetReflectionCamera(reflectionCamera);
        }
        public void SetVolumeMask(LayerMask volumeMask)
        {
            this.volumeMask = volumeMask;
            SetReflectionCamera(reflectionCamera); 
        }

        public Camera ReflectionCamera
        {
            get => reflectionCamera;
            set => SetReflectionCamera(value);
        }
        protected Transform reflectionCameraTransform;
        public virtual void SetReflectionCamera(Camera camera)
        {
            reflectionCamera = camera;
            reflectionCamera.enabled = true;
            reflectionCamera.gameObject.SetActive(true);

            reflectionCamera.tag = _ReflectionTag;
            reflectionCameraTransform = camera == null ? null : camera.transform; 

            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData mainUAC = mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData uac = camera.gameObject.AddOrGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (allowPostProcessing)
            {
                uac.renderPostProcessing = mainUAC.renderPostProcessing; 
            } 
            else
            {
                uac.renderPostProcessing = false;
            }
            uac.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
#if UNITY_2022_3_OR_NEWER
            uac.allowHDROutput = mainUAC.allowHDROutput;
#endif
            uac.dithering = mainUAC.dithering;
            uac.renderShadows = mainUAC.renderShadows; 
            uac.stopNaN = mainUAC.stopNaN;

            camera.cullingMask = useCustomCullingMask ? cullingMask : mainCamera.cullingMask;
            uac.volumeLayerMask = useCustomVolumeMask ? volumeMask : mainUAC.volumeLayerMask;

            camera.depth = -10;

            if (reflectionTexture == null) reflectionTexture = RenderTexture.GetTemporary(new RenderTextureDescriptor()
            {
                 autoGenerateMips = false,
                  colorFormat = RenderTextureFormat.ARGB32,
                   width = TargetResolutionWidth,
                   height = TargetResolutionHeight
                    
            });
            reflectionCamera.targetTexture = reflectionTexture;
        }


        [SerializeField] protected RenderTexture reflectionTexture;
        public RenderTexture ReflectionTexture
        {
            get => reflectionTexture;
            set => SetReflectionTexture(value);
        }
        public virtual void SetReflectionTexture(RenderTexture reflectionTexture)
        {
            this.reflectionTexture = reflectionTexture;
            if (reflectionCamera != null) reflectionCamera.targetTexture = reflectionTexture;

            SetResolutionScale(resolutionScale);
        }

        [SerializeField, Range(0.05f, 1f)] protected float resolutionScale = 1f;
        public float ResolutionScale
        {
            get => resolutionScale;
            set => SetResolutionScale(value);
        }
        protected int lastResolutionWidth, lastResolutionHeight;
        public int TargetResolutionWidth => Mathf.CeilToInt(mainCamera.pixelWidth * resolutionScale);
        public int TargetResolutionHeight => Mathf.CeilToInt(mainCamera.pixelHeight * resolutionScale);
        public virtual void SetResolutionScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.05f, 1f);

            resolutionScale = scale;

            if (reflectionTexture.IsCreated()) reflectionTexture.Release(); 
            lastResolutionWidth = reflectionTexture.width = TargetResolutionWidth;
            lastResolutionHeight = reflectionTexture.height = TargetResolutionHeight; 
            reflectionTexture.Create(); 
        }

        protected virtual void Start()
        {
            SetMainCamera(mainCamera);

            if (reflectionCamera == null) 
            {
                reflectionCamera = new GameObject(name + "_reflectionCamera").AddComponent<Camera>();

                reflectionCamera.nearClipPlane = mainCamera.nearClipPlane;
                reflectionCamera.farClipPlane = mainCamera.farClipPlane;
                reflectionCamera.allowHDR = mainCamera.allowHDR;
                reflectionCamera.allowMSAA = mainCamera.allowMSAA;
                reflectionCamera.backgroundColor = mainCamera.backgroundColor;
                reflectionCamera.clearFlags = mainCamera.clearFlags;

                //reflectionCamera.gameObject.hideFlags = HideFlags.DontSave; // HideFlags.HideAndDontSave // hide in scene hierarchy
            }
            SetReflectionCamera(reflectionCamera);
            SetReflectionTexture(reflectionTexture);

            SetUpdateMode(updateMode);
        }

        protected void OnDestroy()
        {
            PlanarReflections.Unregister(this);
        }

        protected void OnEnable()
        {
            SetUpdateMode(updateMode);
        }
        protected void OnDisable()
        {
            PlanarReflections.Unregister(this);
            if (reflectionCamera != null) reflectionCamera.enabled = false; 
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float clipPlaneOffset, float sideSign)
        {
            var offsetPos = pos + normal * clipPlaneOffset;
            var m = cam.worldToCameraMatrix;
            var cameraPosition = m.MultiplyPoint(offsetPos);
            var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }

        public virtual void Redraw() => Redraw(new TransformState(mainCameraTransform, true));
        public virtual void Redraw(TransformState mainCameraTransformState)
        {

            var targetWidth = TargetResolutionWidth;
            var targetHeight = TargetResolutionHeight;
            if (targetWidth != lastResolutionWidth || targetHeight != lastResolutionHeight) SetResolutionScale(resolutionScale);

            Vector3 planeNormal = transform.up;
            Quaternion toLocal = Quaternion.FromToRotation(planeNormal, Vector3.up); 
            Quaternion toWorld = Quaternion.Inverse(toLocal);

            Quaternion reflectedCameraRotation = toLocal * mainCameraTransformState.rotation;
            var euler = reflectedCameraRotation.eulerAngles;
            reflectedCameraRotation = Quaternion.Euler(-euler.x, euler.y, -euler.z); 
            reflectedCameraRotation = toWorld * reflectedCameraRotation;

            reflectionCamera.fieldOfView = mainCamera.fieldOfView;

            var planePos = transform.position;
            float localHeight = Vector3.Dot(planePos, planeNormal);
            float camHeight = Vector3.Dot(mainCameraTransformState.position, planeNormal) - localHeight;
            var mainCamPos = mainCameraTransformState.position;
            mainCamPos = mainCamPos - (planeNormal * Vector3.Dot(mainCamPos, planeNormal));

            reflectionCameraTransform.SetPositionAndRotation(mainCamPos + (planeNormal * (localHeight + heightOffset + (camHeight * -1))), reflectedCameraRotation);

            var clipPlane = CameraSpacePlane(reflectionCamera, planePos, planeNormal, heightOffset, 1.0f);
            var projection = mainCamera.CalculateObliqueMatrix(clipPlane); 
            reflectionCamera.projectionMatrix = projection;

            if (!reflectionCamera.enabled) reflectionCamera.Render(); 

        }
        public virtual void Refresh()
        {
            switch(updateMode)
            {
                case UpdateMode.CameraMoved:
                    if (CheckIfMainCameraHasMoved(true)) Redraw(mainCameraPrevTransformState);
                    break;

                case UpdateMode.EveryFrame:
                    Redraw();
                    break;
            }
        }

    }

    public class PlanarReflections : SingletonBehaviour<PlanarReflections>
    {

        public override int Priority => 999999999;

        public override void OnFixedUpdate() {}
        public override void OnUpdate() {}

        protected readonly List<PlanarReflection> reflections = new List<PlanarReflection>();
        public override void OnLateUpdate()
        {
            foreach (var reflection in reflections) reflection.Refresh();
        }

        public static void Register(PlanarReflection obj)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(obj);
        }
        public static void Unregister(PlanarReflection obj)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(obj);
        }
        public void RegisterLocal(PlanarReflection obj)
        {
            if (!reflections.Contains(obj)) reflections.Add(obj);
        }
        public void UnregisterLocal(PlanarReflection obj)
        {
            reflections.RemoveAll(i => ReferenceEquals(i, obj));
        }

    }
}

#endif