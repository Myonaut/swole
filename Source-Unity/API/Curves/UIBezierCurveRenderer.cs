#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Swole.API.Unity;

namespace Swole.UI
{
    public class UIBezierCurveRenderer : UIAnimationCurveRenderer
    {
        new public IBezierCurve curve;
        public MorphingBezierCurve morphingCurve;
        [Range(0f, 1f)]
        public float morphWeight;
        private Vector2[] morphingVerts;

        public override void Rebuild()
        {
            if (!Initialized) return;
            
            bool isValid = curve != null || morphingCurve != null;
            if (!isValid)
            {
                foreach (var renderer in activeRenderers) lineRendererPool.Release(renderer.lineRenderer);
                activeRenderers.Clear();
                return;
            }

            if (morphingCurve != null && (morphingVerts == null || morphingVerts.Length != morphingCurve.VertexCount)) morphingVerts = new Vector2[morphingCurve.VertexCount];
            Vector2[] verts = morphingCurve == null ? curve.GetVertices2D() : morphingCurve.GetVertices2D(morphWeight, morphingVerts); 
            if (verts == null || verts.Length <= 1)
            {
                foreach (var renderer in activeRenderers) lineRendererPool.Release(renderer.lineRenderer);
                activeRenderers.Clear();
                return;
            }

            while (activeRenderers.Count > 1)
            {
                int i = activeRenderers.Count - 1;
                if (i <= 0) break;
                lineRendererPool.Release(activeRenderers[i].lineRenderer);
                activeRenderers.RemoveAt(i);
            }
            if (activeRenderers.Count < 1)
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
            }

            Vector2 rangeX = new Vector2(float.MaxValue, float.MinValue);
            if (useExternalRangeX) rangeX = externalRangeX; else foreach (var vert in verts) rangeX = new Vector2(Mathf.Min(rangeX.x, vert.x), Mathf.Max(rangeX.y, vert.x));
            if (rangeX.x == rangeX.y)
            {
                rangeX.x = 0;
                rangeX.y = 1; 
            }

            Vector2 rangeY = new Vector2(float.MaxValue, float.MinValue);
            if (useExternalRangeY) rangeY = externalRangeY; else foreach (var vert in verts) rangeY = new Vector2(Mathf.Min(rangeY.x, vert.y), Mathf.Max(rangeY.y, vert.y)); 
            if (rangeY.x == rangeY.y)
            {
                rangeY.x = 0;
                rangeY.y = 1;
            }

            var curveRenderer = activeRenderers[0]; 

            var rectTransform = curveRenderer.RectTransform;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchoredPosition3D = Vector3.zero;

            if (curveRenderer.pointArray == null || curveRenderer.pointArray.Length != verts.Length) curveRenderer.pointArray = new Vector2[verts.Length];
            verts.CopyTo(curveRenderer.pointArray, 0); 
            bool invalid = false;
            for (int b = 0; b < curveRenderer.pointArray.Length; b++)
            {
                Vector2 point = curveRenderer.pointArray[b];
                if (!float.IsFinite(point.x)) { point.x = 0; invalid = true; }
                if (!float.IsFinite(point.y)) { point.y = 0; invalid = true; }
                point.x = (point.x - rangeX.x) / (rangeX.y - rangeX.x);  
                point.y = (point.y - rangeY.x) / (rangeY.y - rangeY.x);
                curveRenderer.pointArray[b] = point;
            }
            curveRenderer.lineRenderer.useEqualUVPortionsPerSegment = useEqualUVPortionsPerSegment;  
            curveRenderer.lineRenderer.uvRangeX = uvRangeX;
            curveRenderer.lineRenderer.uvRangeY = uvRangeY; 
            curveRenderer.lineRenderer.SetPoints(invalid ? null : curveRenderer.pointArray);

        }
    }
}

#endif