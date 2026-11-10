#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Swole
{

    public class PerCameraRenderSettings : MonoBehaviour
    {

        public bool applyEveryFrame = true;

        [SerializeField]
        new private Camera camera;
        public Camera Camera
        {
            get
            {
                if (camera == null) camera = gameObject.GetComponent<Camera>();
                return camera;
            }
        }

        public RenderSettingsState renderSettings;

        [Serializable]
        public struct RenderSettingsState
        {
            public bool overrideFog;
            public bool enableFog;
            public Color fogColor;
            public FogMode fogMode;
            public float fogDensity;
            public float fogStartDistance;
            public float fogEndDistance;

            public bool overrideAmbientLight;
            public AmbientMode ambientMode;
            public float ambientIntensity;
            [ColorUsage(false, true)]
            public Color ambientLight;
            [ColorUsage(false, true)]
            public Color ambientSkyColor;
            [ColorUsage(false, true)]
            public Color ambientEquatorColor;
            [ColorUsage(false, true)]
            public Color ambientGroundColor;

            public static RenderSettingsState GetCurrentState()
            {
                var state = new RenderSettingsState();

                state.overrideFog = true;
                state.enableFog = RenderSettings.fog;
                state.fogColor = RenderSettings.fogColor;
                state.fogMode = RenderSettings.fogMode;
                state.fogDensity = RenderSettings.fogDensity;
                state.fogStartDistance = RenderSettings.fogStartDistance;
                state.fogEndDistance = RenderSettings.fogEndDistance;

                state.overrideAmbientLight = true;
                state.ambientMode = RenderSettings.ambientMode;
                state.ambientIntensity = RenderSettings.ambientIntensity;
                state.ambientLight = RenderSettings.ambientLight;
                state.ambientSkyColor = RenderSettings.ambientSkyColor;
                state.ambientEquatorColor = RenderSettings.ambientEquatorColor;
                state.ambientGroundColor = RenderSettings.ambientGroundColor;

                return state;
            }

            public void Apply()
            {
                if (overrideFog)
                {
                    RenderSettings.fog = enableFog;
                    RenderSettings.fogColor = fogColor;
                    RenderSettings.fogMode = fogMode;
                    RenderSettings.fogDensity = fogDensity;
                    RenderSettings.fogStartDistance = fogStartDistance;
                    RenderSettings.fogEndDistance = fogEndDistance;
                }

                if (overrideAmbientLight)
                {
                    RenderSettings.ambientMode = ambientMode;
                    RenderSettings.ambientIntensity = ambientIntensity;
                    RenderSettings.ambientLight = ambientLight;
                    RenderSettings.ambientSkyColor = ambientSkyColor;
                    RenderSettings.ambientEquatorColor = ambientEquatorColor;
                    RenderSettings.ambientGroundColor = ambientGroundColor;
                }
            }
        }

        // Static baseline to preserve true global settings across all scene instances
        private RenderSettingsState globalDefaultSettings;
        private bool isGlobalDefaultCached = false;

        private bool hasApplied;

        private void Awake()
        {
            // Cache the pristine, original scene settings BEFORE any rendering loops start
            if (!isGlobalDefaultCached)
            {
                globalDefaultSettings = RenderSettingsState.GetCurrentState();
                isGlobalDefaultCached = true;
            }

            hasApplied = false; 
        }

        private void OnEnable()
        {
            camera = Camera;
            RenderPipelineManager.beginCameraRendering += BeginRender;
            RenderPipelineManager.endCameraRendering += EndRender;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginRender;
            RenderPipelineManager.endCameraRendering -= EndRender;
        }

        void BeginRender(ScriptableRenderContext context, Camera camera)
        {
            if (applyEveryFrame || !hasApplied)
            {
                // Only override if URP is processing your exact targeted Game Camera
                if (ReferenceEquals(camera, this.camera))
                {
                    renderSettings.Apply();
                    hasApplied = true;
                }
            }
        }

        void EndRender(ScriptableRenderContext context, Camera camera)
        {
            if (applyEveryFrame)
            {
                // Restore back to the clean global baseline, avoiding mid-frame pipeline corruption
                if (ReferenceEquals(camera, this.camera))
                {
                    globalDefaultSettings.Apply();
                }
            }
        }
    }

}

#endif