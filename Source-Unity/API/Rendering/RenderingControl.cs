#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace Swole.API.Unity
{
    public static class RenderingControl
    {

        private static List<ScriptableRendererFeature> _rendererFeatures;

        public static void RefreshFeatureList()
        {
            var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);
            var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            
            _rendererFeatures = property.GetValue(renderer) as List<ScriptableRendererFeature>;
        }

        public static void SetActiveStateOfFeature(string name, bool active)
        {
            if (_rendererFeatures == null) RefreshFeatureList();

            name = name.AsID();

            foreach(var feature in _rendererFeatures)
            {
                if (feature.name.AsID() == name)
                {
                    feature.SetActive(active);
                }
            }
        }

        public const string RFID_renderAnimationBones = "RenderAnimationBones";
        public static void SetRenderAnimationBones(bool shouldRender) => SetActiveStateOfFeature(RFID_renderAnimationBones, shouldRender);

    }
}

#endif