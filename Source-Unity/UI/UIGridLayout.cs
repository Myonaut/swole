#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    public class UIGridLayout : LayoutGroup
    {

        public enum FitType
        {

            Uniform, Width, Height, FixedRows, FixedColumns

        }

        public FitType fitType;

        public int rows;

        public int columns;

        public Vector2 cellSize;

        public Vector2 spacing;

        public bool fitX, fitY;

        public override void CalculateLayoutInputHorizontal()
        {

            base.CalculateLayoutInputHorizontal();

            float sqrRt = Mathf.Sqrt(transform.childCount);

            if (fitType == FitType.Uniform || fitType == FitType.Width || fitType == FitType.Height)
            {

                fitX = fitY = true;

                rows = Mathf.CeilToInt(sqrRt);
                columns = rows;

            }
            else
            {

                rows = Mathf.Max(1, rows);
                columns = Mathf.Max(1, columns);

            }

            switch (fitType)
            {

                case FitType.FixedColumns:
                case FitType.Width:

                    rows = Mathf.CeilToInt(transform.childCount / (float)columns);

                    break;

                case FitType.FixedRows:
                case FitType.Height:

                    columns = Mathf.CeilToInt(transform.childCount / (float)rows);

                    break;

            }

            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            float cellWidth = parentWidth / columns - spacing.x / columns * (columns - 1) - padding.left / (float)columns - padding.right / (float)columns;
            float cellHeight = parentHeight / rows - spacing.y / rows * (rows - 1) - padding.top / (float)rows - padding.bottom / (float)rows;

            cellSize = new Vector2(fitX ? cellWidth : cellSize.x, fitY ? cellHeight : cellSize.y);

            for (int a = 0; a < rectChildren.Count; a++)
            {

                int rowIndex = a / columns;
                int columnIndex = a % columns;

                var child = rectChildren[a];

                float xPos = cellSize.x * columnIndex + spacing.x * columnIndex + padding.left;
                float yPos = cellSize.y * rowIndex + spacing.y * rowIndex + padding.top;

                SetChildAlongAxis(child, 0, xPos, cellSize.x);
                SetChildAlongAxis(child, 1, yPos, cellSize.y);

            }

        }

        public override void CalculateLayoutInputVertical()
        {

        }

        public override void SetLayoutHorizontal()
        {

        }

        public override void SetLayoutVertical()
        {

        }

    }

}

#endif