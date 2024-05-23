#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    /// <summary>
    /// Renders a UI line that stays attached to two anchor transforms.
    /// </summary>
    public class UIAnchoredLine : UISimpleLine
    {

        [Header("Anchor Objects")]
        public RectTransform anchorA;

        public Vector3 offsetA;

        public RectTransform anchorB;

        public Vector3 offsetB;

        protected Vector3 prevPositionA, prevPositionB;

        protected Quaternion prevRotationA, prevRotationB;

        public override void SetStartPoint(Vector3 pointStart) { }
        public override void SetEndPoint(Vector3 pointEnd) { }
        public override void SetStartPointAndThickness(Vector3 pointStart, float thickness) => SetThickness(thickness);
        public override void SetEndPointAndThickness(Vector3 pointEnd, float thickness) => SetThickness(thickness);
        public override void SetPoints(Vector3 pointStart, Vector3 pointEnd) { }
        public override void SetPointsAndThickness(Vector3 pointStart, Vector3 pointEnd, float thickness) => SetThickness(thickness);

        public override void Refresh()
        {

            Transform parent = RectTransform.parent;

            if (anchorA != null)
            {

                prevPositionA = anchorA.position;
                prevRotationA = anchorA.rotation;

                Vector3 pA = anchorA.TransformPoint(offsetA);

                if (parent != null) pA = parent.InverseTransformPoint(pA);

                pointStart = pA;

            }


            if (anchorB != null)
            {

                prevPositionB = anchorB.position;
                prevRotationB = anchorB.rotation;

                Vector3 pB = anchorB.TransformPoint(offsetB);

                if (parent != null) pB = parent.InverseTransformPoint(pB);

                pointEnd = pB;

            }

            base.Refresh();

        }

        public virtual bool RefreshIfUpdated()
        {

            if (anchorA != null)
            {

                if (prevPositionA != anchorA.position || prevRotationA != anchorA.rotation)
                {

                    Refresh();
                    return true;

                }

            }

            if (anchorB != null)
            {

                if (prevPositionB != anchorB.position || prevRotationB != anchorB.rotation)
                {

                    Refresh();
                    return true;

                }

            }

            return false;

        }

    }

}

#endif
