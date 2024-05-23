#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer))]
    public class UILightweightGridRenderer : MaskableGraphic
    {
        [SerializeField]
        private Vector2Int gridSize = new Vector2Int(5, 5);
        public void SetGridSize(Vector2Int gridSize)
        {
            this.gridSize = gridSize;
            SetVerticesDirty();
        }

        public void SetColor(Color color)
        {
            this.color = color;
            SetVerticesDirty();
        }

        [SerializeField]
        private float thickness = 10;
        public void SetThickness(float thickness)
        {
            this.thickness = thickness;
            SetVerticesDirty();
        }

        [SerializeField]
        private Color[] tintSequenceX;
        public void SetTintSequenceX(Color[] tintSequenceX)
        {
            this.tintSequenceX = tintSequenceX;
            SetVerticesDirty();
        }

        [SerializeField]
        private Color[] tintSequenceY;
        public void SetTintSequenceY(Color[] tintSequenceY)
        {
            this.tintSequenceY = tintSequenceY;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
             
            vh.Clear();

            var rT = rectTransform;

            var rect = rT.rect;
            var width = rect.width;
            var height = rect.height;

            float px = rT.pivot.x;
            float py = rT.pivot.y;

            int indexOffset = 0;
            Vector2 LocalToContainer(Vector2 point)
            {
                point.x = (point.x * width) - (width * px);
                point.y = (point.y * height) - (height * py);
                return point;
            }

            float halfThickness = thickness * 0.5f;
            for (int x = 0; x < gridSize.x; x++) // Add Vertical Lines
            {

                float xt = x / (gridSize.x - 1f);

                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = color;

                if (tintSequenceX != null && tintSequenceX.Length > 0)
                {
                    int t = x % tintSequenceX.Length;
                    vertex.color = vertex.color * tintSequenceX[t];
                }

                Vector2 middlePos = LocalToContainer(new Vector2(xt, 0));
                vertex.position = middlePos + new Vector2(-halfThickness, 0);
                vh.AddVert(vertex);
                vertex.position = middlePos + new Vector2(halfThickness, 0);
                vh.AddVert(vertex);

                middlePos = LocalToContainer(new Vector2(xt, 1));
                vertex.position = middlePos + new Vector2(-halfThickness, 0);
                vh.AddVert(vertex);
                vertex.position = middlePos + new Vector2(halfThickness, 0);
                vh.AddVert(vertex);

                vh.AddTriangle(indexOffset + 2, indexOffset, indexOffset + 1);
                vh.AddTriangle(indexOffset + 1, indexOffset + 3, indexOffset + 2);

                indexOffset = indexOffset + 4;

            }

            for (int y = 0; y < gridSize.y; y++) // Add Horizontal Lines
            {

                float yt = y / (gridSize.y - 1f);

                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = color;

                if (tintSequenceY != null && tintSequenceY.Length > 0)
                {
                    int t = y % tintSequenceY.Length;
                    vertex.color = vertex.color * tintSequenceY[t];
                }

                Vector2 middlePos = LocalToContainer(new Vector2(0, yt));
                vertex.position = middlePos + new Vector2(0, -halfThickness);
                vh.AddVert(vertex);
                vertex.position = middlePos + new Vector2(0, halfThickness);
                vh.AddVert(vertex);

                middlePos = LocalToContainer(new Vector2(1, yt));
                vertex.position = middlePos + new Vector2(0, -halfThickness);
                vh.AddVert(vertex);
                vertex.position = middlePos + new Vector2(0, halfThickness);
                vh.AddVert(vertex);

                vh.AddTriangle(indexOffset + 2, indexOffset, indexOffset + 1);
                vh.AddTriangle(indexOffset + 1, indexOffset + 3, indexOffset + 2);

                indexOffset = indexOffset + 4;

            }

        }

    }

}

#endif
