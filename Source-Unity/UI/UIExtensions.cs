#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    [System.Serializable]
    public enum AnchorPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottonCenter,
        BottomRight,
        BottomStretch,

        VertStretchLeft,
        VertStretchRight,
        VertStretchCenter,

        HorStretchTop,
        HorStretchMiddle,
        HorStretchBottom,

        StretchAll
    }

    [System.Serializable]
    public enum PivotPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public static class UIExtensions
    {

        public static void SetAnchor(this RectTransform source, AnchorPresets allign, int anchoredPositionOffsetX = 0, int anchoredPositionOffsetY = 0)
        {
            //source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            switch (allign)
            {
                case (AnchorPresets.TopLeft):
                    {
                        source.anchorMin = new Vector2(0, 1);
                        source.anchorMax = new Vector2(0, 1);
                        break;
                    }
                case (AnchorPresets.TopCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 1);
                        source.anchorMax = new Vector2(0.5f, 1);
                        break;
                    }
                case (AnchorPresets.TopRight):
                    {
                        source.anchorMin = new Vector2(1, 1);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }

                case (AnchorPresets.MiddleLeft):
                    {
                        source.anchorMin = new Vector2(0, 0.5f);
                        source.anchorMax = new Vector2(0, 0.5f);
                        break;
                    }
                case (AnchorPresets.MiddleCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0.5f);
                        source.anchorMax = new Vector2(0.5f, 0.5f);
                        break;
                    }
                case (AnchorPresets.MiddleRight):
                    {
                        source.anchorMin = new Vector2(1, 0.5f);
                        source.anchorMax = new Vector2(1, 0.5f);
                        break;
                    }

                case (AnchorPresets.BottomLeft):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(0, 0);
                        break;
                    }
                case (AnchorPresets.BottonCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0);
                        source.anchorMax = new Vector2(0.5f, 0);
                        break;
                    }
                case (AnchorPresets.BottomRight):
                    {
                        source.anchorMin = new Vector2(1, 0);
                        source.anchorMax = new Vector2(1, 0);
                        break;
                    }

                case (AnchorPresets.HorStretchTop):
                    {
                        source.anchorMin = new Vector2(0, 1);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }
                case (AnchorPresets.HorStretchMiddle):
                    {
                        source.anchorMin = new Vector2(0, 0.5f);
                        source.anchorMax = new Vector2(1, 0.5f);
                        break;
                    }
                case (AnchorPresets.HorStretchBottom):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(1, 0);
                        break;
                    }

                case (AnchorPresets.VertStretchLeft):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(0, 1);
                        break;
                    }
                case (AnchorPresets.VertStretchCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0);
                        source.anchorMax = new Vector2(0.5f, 1);
                        break;
                    }
                case (AnchorPresets.VertStretchRight):
                    {
                        source.anchorMin = new Vector2(1, 0);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }

                case (AnchorPresets.StretchAll):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }

            }

            source.anchoredPosition = new Vector3(anchoredPositionOffsetX, anchoredPositionOffsetY, 0);

        }

        public static void SetPivot(this RectTransform source, PivotPresets preset)
        {

            switch (preset)
            {
                case (PivotPresets.TopLeft):
                    {
                        source.pivot = new Vector2(0, 1);
                        break;
                    }
                case (PivotPresets.TopCenter):
                    {
                        source.pivot = new Vector2(0.5f, 1);
                        break;
                    }
                case (PivotPresets.TopRight):
                    {
                        source.pivot = new Vector2(1, 1);
                        break;
                    }

                case (PivotPresets.MiddleLeft):
                    {
                        source.pivot = new Vector2(0, 0.5f);
                        break;
                    }
                case (PivotPresets.MiddleCenter):
                    {
                        source.pivot = new Vector2(0.5f, 0.5f);
                        break;
                    }
                case (PivotPresets.MiddleRight):
                    {
                        source.pivot = new Vector2(1, 0.5f);
                        break;
                    }

                case (PivotPresets.BottomLeft):
                    {
                        source.pivot = new Vector2(0, 0);
                        break;
                    }
                case (PivotPresets.BottomCenter):
                    {
                        source.pivot = new Vector2(0.5f, 0);
                        break;
                    }
                case (PivotPresets.BottomRight):
                    {
                        source.pivot = new Vector2(1, 0);
                        break;
                    }
            }
        }

        private static readonly Vector3[] cornerArray = new Vector3[4];
        private static readonly Vector3[] cornerArray2 = new Vector3[4];

        public static void ConstrainInsideCanvas(this RectTransform rectTransform, Canvas canvas)
        {

            ConstrainInsideRectTransform(rectTransform, canvas.gameObject.AddOrGetComponent<RectTransform>());

        }

        public static void ConstrainInsideRectTransform(this RectTransform rectTransform, RectTransform containingRectTransform)
        {

            rectTransform.GetWorldCorners(cornerArray);
            containingRectTransform.GetLocalCorners(cornerArray2);

            float minXLocal = float.MaxValue;
            float minYLocal = float.MaxValue;
            float maxXLocal = float.MinValue;
            float maxYLocal = float.MinValue;

            float minXContainer = float.MaxValue;
            float minYContainer = float.MaxValue;
            float maxXContainer = float.MinValue;
            float maxYContainer = float.MinValue;

            for (int a = 0; a < 4; a++)
            {

                Vector3 tooltipCorner = containingRectTransform.InverseTransformPoint(cornerArray[a]);
                Vector3 canvasCorner = cornerArray2[a];

                minXLocal = Mathf.Min(minXLocal, tooltipCorner.x);
                maxXLocal = Mathf.Max(maxXLocal, tooltipCorner.x);
                minYLocal = Mathf.Min(minYLocal, tooltipCorner.y);
                maxYLocal = Mathf.Max(maxYLocal, tooltipCorner.y);

                minXContainer = Mathf.Min(minXContainer, canvasCorner.x);
                maxXContainer = Mathf.Max(maxXContainer, canvasCorner.x);
                minYContainer = Mathf.Min(minYContainer, canvasCorner.y);
                maxYContainer = Mathf.Max(maxYContainer, canvasCorner.y);

            }

            rectTransform.position = rectTransform.position - containingRectTransform.TransformVector(new Vector3(Mathf.Max(0, maxXLocal - maxXContainer) + Mathf.Min(0, minXLocal - minXContainer), Mathf.Max(0, maxYLocal - maxYContainer) + Mathf.Min(0, minYLocal - minYContainer), 0));

        }

    }

}

#endif