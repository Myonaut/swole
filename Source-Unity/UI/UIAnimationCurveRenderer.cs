#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity; 

namespace Swole.UI
{
    public class UIAnimationCurveRenderer : MonoBehaviour
    {

        public AnimationCurve curve; 

        [SerializeField]
        protected float lineThickness = 5;
        public virtual float LineThickness 
        {
            get => lineThickness;
            set => SetLineThickness(value);
        }
        public virtual void SetLineThickness(float thickness)
        {
            lineThickness = thickness;
            foreach (var renderer in activeRenderers)
            {
                if (renderer == null || renderer.lineRenderer == null) continue;
                renderer.lineRenderer.SetThickness(lineThickness);
            }
        }

        public bool addStartCap;
        [Tooltip("Must be at least 3")]
        public int startCapResolution = 8;

        public bool addEndCap;
        [SerializeField, Tooltip("Must be at least 3")]
        public int endCapResolution = 8;

        [SerializeField]
        protected Texture2D texture;
        public virtual Texture2D Texture
        {
            get => texture;
            set => SetTexture(value);
        }
        public virtual void SetTexture(Texture2D texture)
        {
            this.texture = texture;
            foreach (var renderer in activeRenderers)
            {
                if (renderer == null || renderer.lineRenderer == null) continue; 
                renderer.lineRenderer.SetTexture(texture);
            }
        }

        [SerializeField]
        protected Color lineColor = Color.white;
        public virtual Color LineColor
        {
            get => lineColor;
            set => SetLineColor(value);
        }
        public virtual void SetLineColor(Color color)
        {
            lineColor = color;
            foreach (var renderer in activeRenderers)
            {
                if (renderer == null || renderer.lineRenderer == null) continue;
                renderer.lineRenderer.SetColor(lineColor);
            }
        }

        public int maxCurveSamples = 100;
        public int minCurveSamples = 10;
        public int slopeSamplingCount = 8;

        [Tooltip("If non-zero, will create a segment at the start and end of the rendered curve using this as the time offset. Useful for showing big spikes in value at the start and end of the curve.")]
        public float endPointPadding = 0.0001f;

        [SerializeField]
        protected UILineRenderer curveRendererPrototype;
        public virtual void SetCurveRendererPrototype(UILineRenderer prototype)
        {
            if (prototype == null) return;
            if (curveRendererPrototype != null) curveRendererPrototype.gameObject.SetActive(false);

            curveRendererPrototype = prototype;
            curveRendererPrototype.SetColor(lineColor);
            curveRendererPrototype.gameObject.SetActive(false);
            lineRendererPool.SetContainerTransform(rendererContainer, false, false, false);
            lineRendererPool.Reinitialize(curveRendererPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 1, 1024);
        } 

        [SerializeField]
        protected Transform rendererContainer;
        public virtual void SetRendererContainer(RectTransform container)
        {
            rendererContainer = container;
            if (lineRendererPool != null) lineRendererPool.SetContainerTransform(rendererContainer, true, true);
            if (activeRenderers != null)
            {
                foreach(var renderer in activeRenderers)
                {
                    if (renderer == null || renderer.RectTransform == null) continue;
                    renderer.RectTransform.SetParent(rendererContainer, false); 
                }
            }
        }

        public bool useEqualUVPortionsPerSegment;

        public Vector2 uvRangeX = new Vector2(0, 1);
        public float GetUVX(float t) => (uvRangeX.x) + (uvRangeX.y - uvRangeX.x) * t;
        public Vector2 uvRangeY = new Vector2(0, 1);
        public float GetUVY(float t) => (uvRangeY.x) + (uvRangeY.y - uvRangeY.x) * t;

        public bool useExternalRangeX;
        public Vector2 externalRangeX;

        public bool useExternalRangeY;
        public Vector2 externalRangeY;

        protected RectTransform rectTransform;
        public virtual RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null) rectTransform = gameObject.GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        [NonSerialized]
        protected PrefabPool lineRendererPool;
        public virtual bool Initialized => lineRendererPool != null;

        protected virtual void Awake()
        {
            if (rendererContainer == null) rendererContainer = gameObject.transform;
            if (curveRendererPrototype == null)
            {
                curveRendererPrototype = new GameObject("_curveRendererPrototype").AddComponent<UILineRenderer>();
                curveRendererPrototype.transform.SetParent(rendererContainer, false);
                curveRendererPrototype.gameObject.SetActive(false);
                curveRendererPrototype.SetColor(lineColor);
            }

            if (lineRendererPool == null)
            {
                lineRendererPool = new GameObject("_lineRendererPool").AddComponent<PrefabPool>();
                lineRendererPool.transform.SetParent(transform, false);
                lineRendererPool.worldPositionStays = false;
            }
            lineRendererPool.SetContainerTransform(rendererContainer, false, false, false);
            lineRendererPool.Reinitialize(curveRendererPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 1, 1024);

        }

        protected class CurveRenderer
        {
            public UILineRenderer lineRenderer;
            protected RectTransform rectTransform;
            public RectTransform RectTransform
            {
                get
                {
                    if (rectTransform == null && lineRenderer != null) rectTransform = lineRenderer.GetComponent<RectTransform>();
                    return rectTransform;
                }
            }
            public Vector2[] pointArray;

            public CurveRenderer(UILineRenderer lineRenderer)
            {
                this.lineRenderer = lineRenderer;
                this.rectTransform = lineRenderer.GetComponent<RectTransform>();
            }
        }

        protected readonly List<CurveRenderer> activeRenderers = new List<CurveRenderer>();
        public List<Vector3> GetRenderedPoints(List<Vector3> pointList = null) => GetRenderedPoints(rendererContainer == null ? transform.localToWorldMatrix : rendererContainer.localToWorldMatrix, pointList); 
        public List<Vector3> GetRenderedPoints(Matrix4x4 transformation, List<Vector3> pointList = null) 
        {
            if (pointList == null)  pointList = new List<Vector3>();

            var rect = RectTransform.rect; 
            var width = rect.width;
            var height = rect.height;
            var pivot = rectTransform.pivot;
            var centerX = width * pivot.x;
            var centerY = height * pivot.y;

            for(int a = 0; a < activeRenderers.Count; a++)
            {
                var renderer = activeRenderers[a];
                if (renderer == null || renderer.pointArray == null) continue;

                for (int b = 0; b < renderer.pointArray.Length; b++) 
                { 
                    var point = renderer.pointArray[b];
                    point.x = (point.x * width) - centerX;
                    point.y = (point.y * height) - centerY; 
                    pointList.Add(transformation.MultiplyPoint(point)); 
                }
            }

            return pointList;
        }

        public virtual void SetRaycastTarget(bool isRaycastTarget)
        {
            foreach (var renderer in activeRenderers)
            {
                if (renderer == null || renderer.lineRenderer == null) continue;
                renderer.lineRenderer.raycastTarget = isRaycastTarget;
            }
        }

#if UNITY_EDITOR
        [SerializeField]
        protected bool forceRebuild;

        public virtual void OnGUI()
        {
            if (forceRebuild)
            {
                Rebuild();
                forceRebuild = false;
            }
        }
#endif

        public virtual void Rebuild()
        {
            if (!Initialized) return;

            if (curve == null)
            {
                foreach (var renderer in activeRenderers) lineRendererPool.Release(renderer.lineRenderer);
                activeRenderers.Clear();
                return;
            }

            Keyframe[] keyframes = curve.keys;
            if (keyframes == null || keyframes.Length <= 1)
            {
                foreach (var renderer in activeRenderers) lineRendererPool.Release(renderer.lineRenderer);
                activeRenderers.Clear();
                return;
            }
            while (activeRenderers.Count > keyframes.Length - 1) // Minus 1 because the last key does not render anything after it
            {
                int i = activeRenderers.Count - 1;
                if (i < 0) break;
                lineRendererPool.Release(activeRenderers[i].lineRenderer);
                activeRenderers.RemoveAt(i);
            }
            while (activeRenderers.Count < keyframes.Length - 1)
            {
                if (lineRendererPool.TryGetNewInstance(out GameObject inst))
                {
                    var renderer = inst.AddOrGetComponent<UILineRenderer>();
                    renderer.SetThickness(lineThickness);
                    renderer.SetColor(lineColor);
                    renderer.SetTexture(texture);
                    activeRenderers.Add(new CurveRenderer(renderer));
                    inst.SetActive(true);
                }
                else break;
            }

            Vector2 rangeX = new Vector2(float.MaxValue, float.MinValue);
            if (useExternalRangeX) rangeX = externalRangeX; else foreach (var keyframe in keyframes) rangeX = new Vector2(Mathf.Min(rangeX.x, keyframe.time), Mathf.Max(rangeX.y, keyframe.time));
            if (rangeX.x == rangeX.y)
            {
                rangeX.x = 0;
                rangeX.y = 1;
            }

            Vector2 rangeY = new Vector2(float.MaxValue, float.MinValue);

            float GetTimeInRange(float timeInCurve)
            {
                return (timeInCurve - rangeX.x) / (rangeX.y - rangeX.x);
            }

            int slopeSamplingCountp1 = slopeSamplingCount + 1;
            float slopeSamplingStep = (1f / slopeSamplingCountp1);
            float slopeSamplingStepHalf = slopeSamplingStep * 0.5f;
            float slopeSamplingStepNeighbor = slopeSamplingStep * 0.01f;
            int rendererCount = activeRenderers.Count;
            for (int a = 0; a < rendererCount; a++)
            {
                var curveRenderer = activeRenderers[a];

                int b = a + 1;

                Keyframe kfA = keyframes[a];
                Keyframe kfB = keyframes[b];

                float startAnchorX = GetTimeInRange(kfA.time);
                float endAnchorX = GetTimeInRange(kfB.time);

                var rectTransform = curveRenderer.RectTransform;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(startAnchorX, 0);
                rectTransform.anchorMax = new Vector2(endAnchorX, 1);
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.anchoredPosition3D = Vector3.zero;

                int pointCount = maxCurveSamples;

                float startTime = kfA.time;
                float endTime = kfB.time;
                if (endPointPadding != 0)
                {
                    kfA.time = Mathf.Max(kfA.time, kfA.time + endPointPadding);
                    kfB.time = Mathf.Min(kfB.time, kfB.time - endPointPadding);
                }
                float timeLength = endTime - startTime;

                curveRenderer.lineRenderer.useEqualUVPortionsPerSegment = useEqualUVPortionsPerSegment;
                curveRenderer.lineRenderer.uvRangeX = new Vector2((GetTimeInRange(startTime) * (uvRangeX.y - uvRangeX.x)) + uvRangeX.x, (GetTimeInRange(endTime) * (uvRangeX.y - uvRangeX.x)) + uvRangeX.x);
                curveRenderer.lineRenderer.uvRangeY = uvRangeY;

                if (slopeSamplingCount > 0)
                {
                    float divergence = 0;
                    float prevSlope = 0;
                    for (int c = 0; c < slopeSamplingCountp1; c++)
                    {
                        float t = c / ((float)slopeSamplingCountp1);
                        //float va = curve.Evaluate(Mathf.Lerp(kfA.time, kfB.time, t));
                        //float vb = curve.Evaluate(Mathf.Lerp(kfA.time, kfB.time, t + slopeSamplingStep));
                        //float slope = Mathf.Atan((vb - va) / slopeSamplingStep);
                        // Let's sample close by instead to get more accurate samples of slopes
                        t = t + slopeSamplingStepHalf; 
                        float va = curve.Evaluate(Mathf.Lerp(startTime, endTime, t - slopeSamplingStepNeighbor));
                        float vb = curve.Evaluate(Mathf.Lerp(startTime, endTime, t + slopeSamplingStepNeighbor));
                        float slope = Mathf.Atan((vb - va) / (slopeSamplingStepNeighbor * 2));
                        if (!float.IsFinite(slope)) slope = 0; 
                        if (c == 0)
                        {
                            prevSlope = slope;
                        }
                        else
                        {
                            divergence += Mathf.Min((2 * Mathf.PI) - Mathf.Abs(slope - prevSlope), Mathf.Abs(slope - prevSlope)); // Big changes to the slope of the curve will add more resolution
                        }
                    }
                    if (float.IsFinite(divergence)) pointCount = (int)Mathf.Clamp(minCurveSamples + (maxCurveSamples - minCurveSamples) * divergence, minCurveSamples, maxCurveSamples); 
                }

                int arraySize = pointCount;
                int indexOffset = 0;
                if (endPointPadding != 0)
                {
                    arraySize = arraySize + 2;
                    indexOffset = 1;
                }
                if (curveRenderer.pointArray == null || curveRenderer.pointArray.Length != arraySize) curveRenderer.pointArray = new Vector2[arraySize];

                float RemapX(float x)
                {
                    return ((kfA.time + (kfB.time - kfA.time) * x) - startTime) / timeLength;
                }
                float div = pointCount > 1 ? (pointCount - 1f) : 1; 
                for (int c = 0; c < pointCount; c++) 
                {
                    float t = c / div;
                    float kt = Mathf.Lerp(kfA.time, kfB.time, t);
                    float v = curve.Evaluate(kt);
                    if (!useExternalRangeY) 
                    {
                        if (v < rangeY.x) rangeY.x = v;
                        if (v > rangeY.y) rangeY.y = v; 
                    }

                    curveRenderer.pointArray[c + indexOffset] = new Vector2(RemapX(t), v);
                }
                if (endPointPadding != 0)
                {
                    float v;

                    v = curve.Evaluate(startTime);
                    if (!useExternalRangeY)
                    {
                        if (v < rangeY.x) rangeY.x = v;
                        if (v > rangeY.y) rangeY.y = v;
                    }
                    curveRenderer.pointArray[0] = new Vector2(0, v);

                    v = curve.Evaluate(endTime);
                    if (!useExternalRangeY)
                    {
                        if (v < rangeY.x) rangeY.x = v;
                        if (v > rangeY.y) rangeY.y = v;
                    }
                    curveRenderer.pointArray[arraySize - 1] = new Vector2(1, v);
                }
            }

            if (useExternalRangeY) rangeY = externalRangeY;
            if (rangeY.x == rangeY.y) 
            { 
                rangeY.x = 0;
                rangeY.y = 1;
            }

            for (int a = 0; a < rendererCount; a++)
            {
                var curveRenderer = activeRenderers[a];

                curveRenderer.lineRenderer.addStartCap = addStartCap && a == 0;
                curveRenderer.lineRenderer.startCapResolution = startCapResolution;
                curveRenderer.lineRenderer.addEndCap = addEndCap && a == rendererCount - 1; 
                curveRenderer.lineRenderer.endCapResolution = endCapResolution;

                bool invalid = false;
                for (int b = 0; b < curveRenderer.pointArray.Length; b++)
                {
                    Vector2 point = curveRenderer.pointArray[b];
                    if (!float.IsFinite(point.x)) { point.x = 0; invalid = true; }
                    if (!float.IsFinite(point.y)) { point.y = 0; invalid = true; }
                    point.y = (point.y - rangeY.x) / (rangeY.y - rangeY.x);
                    curveRenderer.pointArray[b] = point; 
                }
                curveRenderer.lineRenderer.SetPoints(invalid ? null : curveRenderer.pointArray);  
            }
        }

    }
}

#endif