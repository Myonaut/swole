#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Unity.Mathematics;

namespace Swole
{
    public class CustomizableActionCamera : MonoBehaviour
    {

        [NonSerialized]
        protected OrbitCameraViewGroup viewGroup;

        [NonSerialized]
        protected OrbitCameraView activeView;

        [Serializable]
        public struct OrbitState
        {
            public float zoom;
            public float2 distanceRange;
            public float fieldOfView;
            public float3 originInRoot;
        }

        public OrbitState State
        {
            get => new OrbitState()
            {
                zoom = OrbitZoom,
                distanceRange = OrbitDistanceRange,
                fieldOfView = FOV,
                originInRoot = OrbitOriginInRoot
            };

            set
            {
                OrbitDistanceRange = value.distanceRange;
                OrbitZoom = value.zoom;
                FOV = value.fieldOfView;
                OrbitOriginInRoot = value.originInRoot;
            }
        }

        private Coroutine activeTransitionRoutine;
        public void TransitionToState(OrbitState state, float transitionTime)
        {
            if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = StartCoroutine(TransitionToStateRoutine(state, transitionTime));
        }
        protected IEnumerator TransitionToStateRoutine(OrbitState state, float transitionTime)
        {
            float startZoom = OrbitZoom;
            float2 startDistanceRange = OrbitDistanceRange;
            float startFOV = FOV;
            float3 startOrigin = OrbitWorldOrigin;

            float t = 0f;
            while (t < transitionTime)
            {
                t += Time.deltaTime;

                var rootL2w = RootL2W;

                float nT = Mathf.Clamp01(t / transitionTime);
                OrbitZoom = Mathf.LerpUnclamped(startZoom, state.zoom, nT);
                OrbitDistanceRange = math.lerp(startDistanceRange, state.distanceRange, nT);
                FOV = Mathf.LerpUnclamped(startFOV, state.fieldOfView, nT);
                OrbitWorldOrigin = Vector3.LerpUnclamped(startOrigin, rootL2w.MultiplyPoint(state.originInRoot), nT);

                yield return null;
            }

            OrbitZoom = state.zoom;
            OrbitDistanceRange = state.distanceRange;
            FOV = state.fieldOfView;
            OrbitOriginInRoot = state.originInRoot;
        }

        public virtual void SetViewGroup(OrbitCameraViewGroup viewGroup, float transitionTime = 0f)
        {
            this.viewGroup = viewGroup;
            SetView(viewGroup.DefaultView, transitionTime);
        }

        protected IEnumerator TransitionToView(OrbitCameraView view, float transitionTime) 
        {
            yield return TransitionToStateRoutine(new OrbitState()
            {
                zoom = view.startZoom,
                distanceRange = new float2(view.minDistance, view.maxDistance),
                fieldOfView = view.fieldOfView,
                originInRoot = view.originInRoot
            }, transitionTime);
        }
        public virtual void SetView(OrbitCameraView view, float transitionTime = 0f)
        {
            activeView = view;
            SetMode(Mode.Orbit);

            if (activeTransitionRoutine != null) StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = StartCoroutine(TransitionToView(view, transitionTime));
        }
        public virtual void SetView(string id, float transitionTime = 0f)
        {
            if (viewGroup.TryGetView(id, out var view)) SetView(view, transitionTime);
        }

        public bool TryLoadViewGroup(string directory, string fileName)
        {
            if (OrbitCameraViewGroup.TryLoad(directory, fileName, out var viewGroup)) 
            {
                SetViewGroup(viewGroup);
                return true;
            }

            return false;
        }

        [Serializable]
        public enum Mode
        {
            Orbit, Free
        }
        protected Mode mode;
        public Mode CurrentMode => mode;

        public bool recalculateOrbitOriginOnModeSwitch;
        public float maxDistanceFromTarget = 4f;

        public virtual void SetMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.Orbit:
                    if (this.mode != Mode.Orbit)
                    {
                        if (recalculateOrbitOriginOnModeSwitch) RecalculateOrbitOrigin();

                        var targetPos = OrbitWorldOrigin; 
                        var pos = transform.position;

                        var offset = pos - (Vector3)targetPos;
                        float dist = offset.magnitude;
                        var dir = offset / (dist == 0f ? 1f : dist); 

                        OrbitYaw = Vector3.SignedAngle(Vector3.forward, new Vector3(dir.x, 0f, dir.z).normalized, Vector3.up);

                        float orbitPitch = Vector3.SignedAngle(Vector3.forward, Quaternion.Euler(0f, -OrbitYaw, 0f) * dir, Vector3.right);
                        while (orbitPitch > 180f) orbitPitch -= 360f;
                        while (orbitPitch < -180f) orbitPitch += 360f;
                        OrbitPitch = orbitPitch; 

                        if (recalculateOrbitOriginOnModeSwitch)
                        {
                            var distRange = OrbitDistanceRange;

                            //if (dist - 0.25f < distRange.x) distRange.x = Mathf.Max(0.01f, dist - 0.25f);
                            //if (dist + 0.25f > distRange.y) distRange.y = dist + 0.25f;

                            distRange.x = 0.05f;

                            var targetRootPos = OrbitTargetPosition;
                            var originOffset = (Vector3)(targetPos - targetRootPos);
                            float originDist = originOffset.magnitude;

                            distRange.y = Mathf.Max(0f, maxDistanceFromTarget - originDist) + 0.05f; 

                            OrbitDistanceRange = distRange;
                            OrbitZoom = (dist - distRange.x) / (distRange.y - distRange.x);
                        }
                    }
                    setOrbitActiveState?.Invoke(true);
                    break;

                case Mode.Free:
                    setOrbitActiveState?.Invoke(false);
                    if (this.mode != Mode.Free)
                    {
                        var euler = transform.rotation.eulerAngles;
                        freeCamYaw = euler.y;
                        freeCamPitch = euler.x;
                        while (freeCamPitch > 180f) freeCamPitch -= 360f;
                        while (freeCamPitch < -180f) freeCamPitch += 360f; 
                    }
                    break;
            }

            this.mode = mode;
        }

        public float yawSpeed = 1f;
        public float pitchSpeed = 1f;
        public float freeCamSpeed = 1f;

        public float2 pitchRange = new Vector2(-80f, 80f);

        protected float freeCamPitch;
        protected float freeCamYaw;
        protected virtual void LateUpdate()
        {
            switch(mode)
            {
                case Mode.Free:
                    freeCamYaw = Maths.NormalizeDegrees(freeCamYaw + InputX * Time.deltaTime * 100f * yawSpeed);
                    freeCamPitch = Mathf.Clamp(freeCamPitch - InputY * Time.deltaTime * 100f * pitchSpeed, pitchRange.x, pitchRange.y); 

                    var pos = transform.position;
                    var rot = Quaternion.Euler(freeCamPitch, freeCamYaw, 0f);
                    pos = pos + ((rot * new Vector3(MoveInputX * freeCamSpeed, 0f, MoveInputZ * freeCamSpeed)) + new Vector3(0f, MoveInputY * freeCamSpeed, 0f)) * Time.deltaTime;

                    var targetPos = OrbitTargetPosition;
                    var targetOffset = pos - (Vector3)targetPos;
                    float distanceFromTarget = targetOffset.magnitude;
                    if (distanceFromTarget > maxDistanceFromTarget)
                    {
                        var targetDir = targetOffset / distanceFromTarget;
                        pos = (Vector3)targetPos + (targetDir * maxDistanceFromTarget); 
                    }

                    transform.SetPositionAndRotation(pos, rot);
                    break;
            }
        }

        public delegate void SetOrbitActiveStateDelegate(bool activeState);
        public delegate bool GetOrbitActiveStateDelegate();

        public delegate void SetFovDelegate(float fov);
        public delegate float GetFovDelegate();

        public delegate void SetOrbitZoomDelegate(float zoom);
        public delegate float GetOrbitZoomDelegate();

        public delegate void SetOrbitDistanceRangeDelegate(float minDistance, float maxDistance);
        public delegate float2 GetOrbitDistanceRangeDelegate();

        public delegate float GetInputDelegate();

        public delegate Matrix4x4 GetRootL2WMatrixDelegate();

        public delegate void SetOrbitOriginInRootDelegate(float3 originInRoot);
        public delegate float3 GetOrbitOriginInRootDelegate();

        public delegate void SetOrbitWorldOriginDelegate(float3 origin);
        public delegate float3 GetOrbitWorldOriginDelegate();

        public SetOrbitActiveStateDelegate setOrbitActiveState;
        public GetOrbitActiveStateDelegate getOrbitActiveState;
        public bool OrbitActiveState
        {
            get => getOrbitActiveState == null ? false : getOrbitActiveState();
            set => setOrbitActiveState?.Invoke(value);
            
        }

        public SetFovDelegate setFov;
        public GetFovDelegate getFov;
        public float FOV
        {
            get => getFov == null ? 0f : getFov();
            set => setFov?.Invoke(value);
        }

        public SetOrbitZoomDelegate setZoom;
        public GetOrbitZoomDelegate getZoom;
        public float OrbitZoom
        {
            get => getZoom == null ? 0f : getZoom();
            set => setZoom?.Invoke(value); 
        }

        public SetOrbitZoomDelegate setOrbitYaw;
        public GetOrbitZoomDelegate getOrbitYaw;
        public float OrbitYaw
        {
            get => getOrbitYaw == null ? 0f : getOrbitYaw();
            set => setOrbitYaw?.Invoke(value);
        }

        public SetOrbitZoomDelegate setOrbitPitch;
        public GetOrbitZoomDelegate getOrbitPitch;
        public float OrbitPitch
        {
            get => getOrbitPitch == null ? 0f : getOrbitPitch();
            set => setOrbitPitch?.Invoke(value); 
        }

        public SetOrbitDistanceRangeDelegate setOrbitDistanceRange;
        public GetOrbitDistanceRangeDelegate getOrbitDistanceRange;
        public float2 OrbitDistanceRange
        {
            get => getOrbitDistanceRange == null ? default : getOrbitDistanceRange();
            set => setOrbitDistanceRange?.Invoke(value.x, value.y);
        }

        public GetInputDelegate getInputX;
        public float InputX => getInputX == null ? default : getInputX();
        
        public GetInputDelegate getInputY;
        public float InputY => getInputY == null ? default : getInputY();

        public GetInputDelegate getMoveInputX;
        public float MoveInputX => getMoveInputX == null ? default : getMoveInputX();

        public GetInputDelegate getMoveInputY;
        public float MoveInputY => getMoveInputY == null ? default : getMoveInputY();

        public GetInputDelegate getMoveInputZ;
        public float MoveInputZ => getMoveInputZ == null ? default : getMoveInputZ();

        public GetRootL2WMatrixDelegate getRootL2W;
        public Matrix4x4 RootL2W => getRootL2W == null ? Matrix4x4.identity : getRootL2W();

        public SetOrbitOriginInRootDelegate setOrbitOriginInRoot;
        public GetOrbitOriginInRootDelegate getOrbitOriginInRoot;
        public float3 OrbitOriginInRoot
        {
            get => getOrbitOriginInRoot == null ? default : getOrbitOriginInRoot();
            set => setOrbitOriginInRoot?.Invoke(value);
        }

        public SetOrbitWorldOriginDelegate setOrbitWorldOrigin;
        public GetOrbitWorldOriginDelegate getOrbitWorldOrigin;
        public float3 OrbitWorldOrigin
        {
            get => getOrbitWorldOrigin == null ? default : getOrbitWorldOrigin();
            set => setOrbitWorldOrigin?.Invoke(value);
        }


        public delegate float3 OrbitTargetPositionDelegate();
        public OrbitTargetPositionDelegate getOrbitTargetPosition;
        public float3 OrbitTargetPosition => getOrbitTargetPosition == null ? float3.zero : getOrbitTargetPosition();

        public float maxRaycastDistance = 50f;

        public delegate bool RaycastAgainstOrbitTargetDelegate(float3 origin, float3 offset, out Maths.RaycastHitResult hit);
        public RaycastAgainstOrbitTargetDelegate raycastAgainstOrbitTarget;

        public void RecalculateOrbitOrigin()
        {
            if (raycastAgainstOrbitTarget != null)
            {
                var rayOrigin = transform.position;
                var rayOffset = transform.forward * maxRaycastDistance;

                if (raycastAgainstOrbitTarget(rayOrigin, rayOffset, out var hit))
                { 
                    OrbitWorldOrigin = hit.point;
                } 
                else
                {
                    OrbitWorldOrigin = rayOrigin + transform.forward;
                }
            }
        }

    }

    [Serializable]
    public struct OrbitCameraView
    {
        public string id;
        public float startZoom;
        public float minDistance;
        public float maxDistance;
        public float fieldOfView;
        public float3 originInRoot; 
    }

    [Serializable]
    public struct OrbitCameraViewGroup
    {
        public string id;
        public string defaultView;

        public OrbitCameraView[] views;
        public bool IsValid => views != null && views.Length > 0;

        public OrbitCameraView DefaultView => !IsValid ? default : (TryGetView(defaultView, out var view_) ? view_ : views[0]);

        public bool TryGetView(string id, out OrbitCameraView view)
        {
            view = default;
            if (!IsValid) return false;

            for(int a = 0; a < views.Length; a++)
            {
                var view_ = views[a];
                if (view.id == id)
                {
                    view = view_;
                    return true;
                }
            }
            return false;
        }

        public void Save(string directory, string fileName = null)
        {
            if (!IsValid) return;

            if (string.IsNullOrWhiteSpace(fileName)) fileName = id;

            if (!fileName.EndsWith(".json")) fileName = $"{fileName}.json";

            var dir = Directory.CreateDirectory(directory);
            string path = Path.Combine(dir.FullName, fileName);
            var json = swole.ToJson(this, true);
            File.WriteAllText(path, json);
        }

        public static string DefaultViewDirectory => Path.Combine(swole.AppDataDirectory.FullName, "action_views");
        public void Save() => Save(DefaultViewDirectory, null);

        public static bool TryLoad(string directory, string fileName, out OrbitCameraViewGroup viewGroup)
        {
            viewGroup = default;
            if (string.IsNullOrWhiteSpace(fileName)) return false; 

            if (!fileName.EndsWith(".json")) fileName = $"{fileName}.json";

            if (!Directory.Exists(directory)) return false;
            string path = Path.Combine(directory, fileName);
            if (!File.Exists(path)) return false;

            var json = File.ReadAllText(path);
            viewGroup = swole.FromJson<OrbitCameraViewGroup>(path);

            return true;
        }
        public static bool TryLoad(string fileName, out OrbitCameraViewGroup viewGroup) => TryLoad(DefaultViewDirectory, fileName, out viewGroup);
        
    }

}

#endif