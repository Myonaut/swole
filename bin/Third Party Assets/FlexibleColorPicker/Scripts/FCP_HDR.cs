#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Swole;
using NUnit.Framework;

namespace FCP
{
    [RequireComponent(typeof(FlexibleColorPicker))]
    public class FCP_HDR : MonoBehaviour
    {
        protected float intensity;
        public void SetIntensity(float intensity)
        {
            this.intensity = intensity;
            OnColorChangeFCP(fcp.color);
        }
        [SerializeField]
        protected Slider slider;
        public void SetSlider(Slider slider)
        {
            if (this.slider != null)
            {
                if (this.slider.onValueChanged != null) this.slider.onValueChanged.RemoveListener(SetIntensity);
            }

            this.slider = slider;
            if (slider != null)
            {
                if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                slider.onValueChanged.AddListener(SetIntensity);

                slider.SetValueWithoutNotify(intensity); 
            }
        }

        private Color GetColorHDR(Color col)
        {
            AdvancedColor.SetGlobalBaseColor(col);
            AdvancedColor.SetGlobalColorExposure(intensity);
            return AdvancedColor.GetGlobalColor();
        }
        public Color ColorHDR
        {
            get => GetColorHDR(fcp.color);
            set
            {
                AdvancedColor.SetGlobalColorHDR(value);
                intensity = AdvancedColor.GetGlobalColorExposure();
                if (slider != null) slider.SetValueWithoutNotify(intensity);
                fcp.color = AdvancedColor.GetGlobalBaseColor();
            }
        }
        public void SetColorWithoutNotify(Color value)
        {
            AdvancedColor.SetGlobalColorHDR(value);
            intensity = AdvancedColor.GetGlobalColorExposure();
            if (slider != null) slider.SetValueWithoutNotify(intensity);
            fcp.SetColorWithoutNotify(AdvancedColor.GetGlobalBaseColor());
            SyncGraphics(value); 
        }

        public UnityEvent<Color> onColorChange;
        private void OnColorChangeFCP(Color col)
        {
            var colorHDR = GetColorHDR(col);
            SyncGraphics(colorHDR);

            onColorChange?.Invoke(colorHDR);
        }

        private FlexibleColorPicker fcp;
        public FlexibleColorPicker ColorPicker => fcp;
        protected void Awake()
        {
            fcp = gameObject.GetComponent<FlexibleColorPicker>();
            if (fcp.onColorChange == null) fcp.onColorChange = new FlexibleColorPicker.ColorUpdateEvent();
            fcp.onColorChange.AddListener(OnColorChangeFCP);

            if (graphicSyncs != null)
            {
                foreach(var sync in graphicSyncs)
                {
                    if (sync.graphic == null) continue;

                    if (sync.instantiateMaterial)
                    {
                        var mat = sync.graphic.material == null ? sync.graphic.defaultMaterial : sync.graphic.material;
                        if (mat != null) 
                        { 
                            sync.instantiatedMaterial = Instantiate(mat);
                            sync.graphic.material = sync.instantiatedMaterial;
                        }
                    }
                }
            }

            SetSlider(slider);
        }

        [Serializable]
        public class GraphicSync
        {
            public Graphic graphic;
            public bool instantiateMaterial;
            public string hdrColorPropertyName = "_ColorHDR";
            public bool useAlpha;

            [NonSerialized]
            public Material instantiatedMaterial;
        }

        public List<GraphicSync> graphicSyncs = new List<GraphicSync>();
        public void SyncGraphics() => SyncGraphics(ColorHDR);
        protected void SyncGraphics(Color colorHDR)
        {
            if (graphicSyncs != null)
            {
                foreach (var sync in graphicSyncs)
                {
                    if (sync.graphic == null) continue;

                    if (sync.instantiatedMaterial == null)
                    {
                        if (sync.graphic.material != null) sync.graphic.material.SetColor(sync.hdrColorPropertyName, sync.useAlpha ? colorHDR : new Color(colorHDR.r, colorHDR.g, colorHDR.b, 1f));
                    }
                    else
                    {
                        sync.instantiatedMaterial.SetColor(sync.hdrColorPropertyName, sync.useAlpha ? colorHDR : new Color(colorHDR.r, colorHDR.g, colorHDR.b, 1f));
                    }
                }
            }
        }

        protected void OnDestroy()
        {
            if (graphicSyncs != null)
            {
                foreach(var sync in graphicSyncs)
                {
                    if (sync.instantiatedMaterial != null) GameObject.Destroy(sync.instantiatedMaterial);
                }

                graphicSyncs.Clear();
                graphicSyncs = null;
            }
        }

    }
}

#endif