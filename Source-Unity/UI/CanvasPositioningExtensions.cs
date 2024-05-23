#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using UnityEngine;

namespace Swole.Depracated
{

    /// Source: https://gist.github.com/FlaShG/ac3afac0ef65d98411401f2b4d8a43a5
    /// <summary>
    /// Small helper class to convert viewport, screen or world positions to canvas space.
    /// Only works with screen space canvases.
    /// </summary>
    /// <example>
    /// <code>
    /// objectOnCanvasRectTransform.anchoredPosition = specificCanvas.WorldToCanvasPoint(worldspaceTransform.position);
    /// </code>
    /// </example>
    public static class CanvasPositioningExtensions
    {

        [Obsolete]
        public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }
            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        /// <summary>
        /// Use Extensions.ScreenToCanvasSpace instead.
        /// </summary>
        [Obsolete("Use Extensions.ScreenToCanvasSpace instead.")]
        public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
        {
            var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                                               screenPosition.y / Screen.height,
                                               0);
            return canvas.ViewportToCanvasPosition(viewportPosition); 
        }

        [Obsolete]
        public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }
    }

}

#endif
