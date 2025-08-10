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

        // Unity calls this method automatically when it enables this component
        private void OnEnable()
        {
            camera = Camera; 

            // Add WriteLogMessage as a delegate of the RenderPipelineManager.beginCameraRendering event
            RenderPipelineManager.beginCameraRendering += BeginRender;
            RenderPipelineManager.endCameraRendering += EndRender;
        }

        // Unity calls this method automatically when it disables this component
        private void OnDisable()
        {
            // Remove WriteLogMessage as a delegate of the  RenderPipelineManager.beginCameraRendering event
            RenderPipelineManager.beginCameraRendering -= BeginRender;
            RenderPipelineManager.endCameraRendering -= EndRender;
        }

        private RenderSettingsState defaultSettings;
        // When this method is a delegate of RenderPipeline.beginCameraRendering event, Unity calls this method every time it raises the beginCameraRendering event
        void BeginRender(ScriptableRenderContext context, Camera camera)
        {
            if (ReferenceEquals(camera, this.camera))
            {
                defaultSettings = RenderSettingsState.GetCurrentState();
                renderSettings.Apply();
            }
        }

        void EndRender(ScriptableRenderContext context, Camera camera)
        {
            if (ReferenceEquals(camera, this.camera))
            {
                defaultSettings.Apply();
            }
        }
    }
}

#endif