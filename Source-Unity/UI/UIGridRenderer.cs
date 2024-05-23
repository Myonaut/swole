#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    [ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer))]
    public class UIGridRenderer : MaskableGraphic
    {
        [SerializeField]
        private Vector2Int gridSize = new Vector2Int(5, 5);
        public void SetGridSize(Vector2Int gridSize)
        {
            this.gridSize = gridSize;
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
        private Color[] tintSequence;
        public void SetTintSequence(Color[] tintSequence)
        {
            this.tintSequence = tintSequence;
            SetVerticesDirty();
        } 

        protected float width;
        protected float height;

        protected float cellWidth;
        protected float cellHeight;

        protected float distance;
        protected Vector2 offset;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            
            vh.Clear();

            Vector2 pivot = rectTransform.pivot;

            width = rectTransform.rect.width;
            height = rectTransform.rect.height;

            offset = new Vector2(width * pivot.x, height * pivot.y);

            cellWidth = width / gridSize.x;
            cellHeight = height / gridSize.y;

            float widthSqr = thickness * thickness;
            float distanceSqr = widthSqr / 2f;
            distance = Mathf.Sqrt(distanceSqr);

            int index = 0;
            for (int y = 0; y < gridSize.y; y++)
            {

                for (int x = 0; x < gridSize.x; x++)
                {

                    DrawCell(x, y, index, vh);

                    index++;

                }

            }

        }

        private void DrawCell(int x, int y, int index, VertexHelper vh)
        {

            Vector3 offsetPos = new Vector3((x * cellWidth) - offset.x, (y * cellHeight) - offset.y, 0);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            if (tintSequence != null && tintSequence.Length > 0)
            {
                int t = (((gridSize.y - 1 - y) * gridSize.x) + x) % tintSequence.Length;
                vertex.color = vertex.color * tintSequence[t];
            }

            vertex.position = offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(0, cellHeight) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(cellWidth, cellHeight) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(cellWidth, 0) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(distance, distance) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(distance, cellHeight - distance) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(cellWidth - distance, cellHeight - distance) + offsetPos;
            vh.AddVert(vertex);

            vertex.position = new Vector3(cellWidth - distance, distance) + offsetPos;
            vh.AddVert(vertex);

            int offsetIndex = index * 8; 

            //Left Edge
            vh.AddTriangle(offsetIndex + 0, offsetIndex + 1, offsetIndex + 5);
            vh.AddTriangle(offsetIndex + 5, offsetIndex + 4, offsetIndex + 0);

            //Top Edge
            vh.AddTriangle(offsetIndex + 1, offsetIndex + 2, offsetIndex + 6);
            vh.AddTriangle(offsetIndex + 6, offsetIndex + 5, offsetIndex + 1);

            //Right Edge
            vh.AddTriangle(offsetIndex + 2, offsetIndex + 3, offsetIndex + 7);
            vh.AddTriangle(offsetIndex + 7, offsetIndex + 6, offsetIndex + 2);

            //Bottom Edge
            vh.AddTriangle(offsetIndex + 3, offsetIndex + 0, offsetIndex + 4);
            vh.AddTriangle(offsetIndex + 4, offsetIndex + 7, offsetIndex + 3);

        }

    }

}

#endif