using System;

namespace Swole
{

    [Serializable]
    public struct TransformSnapshot
    {
        public bool isLocalSpace;

        public EngineInternal.Vector3 position;
        public EngineInternal.Vector3 scale;
        public EngineInternal.Quaternion rotation;

        public static TransformSnapshot CreateFromLocal(EngineInternal.ITransform transform) => new TransformSnapshot() { isLocalSpace = true, position = transform.localPosition, rotation = transform.localRotation, scale = transform.localScale };
        public static TransformSnapshot CreateFromWorld(EngineInternal.ITransform transform) => new TransformSnapshot() { isLocalSpace = false, position = transform.position, rotation = transform.rotation, scale = transform.localScale };

        public void ApplyTo(EngineInternal.ITransform transform)
        {
            if (isLocalSpace)
            {
                transform.SetLocalPositionAndRotation(position, rotation);    
            } 
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }

            transform.localScale = scale;
        }
    }
    [Serializable]
    public struct TransformSnapshotEuler
    {
        public bool isLocalSpace;

        public EngineInternal.Vector3 position;
        public EngineInternal.Vector3 eulerRotation;
        public EngineInternal.Vector3 scale;

        public static TransformSnapshotEuler CreateFromLocal(EngineInternal.ITransform transform) => new TransformSnapshotEuler() { isLocalSpace = true, position = transform.localPosition, eulerRotation = transform.localRotation.EulerAngles, scale = transform.localScale };
        public static TransformSnapshotEuler CreateFromWorld(EngineInternal.ITransform transform) => new TransformSnapshotEuler() { isLocalSpace = false, position = transform.position, eulerRotation = transform.rotation.EulerAngles, scale = transform.localScale };

        public void ApplyTo(EngineInternal.ITransform transform)
        {
            var rotation = EngineInternal.Quaternion.Euler(eulerRotation);
            if (isLocalSpace)
            {
                transform.SetLocalPositionAndRotation(position, rotation);
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }

            transform.localScale = scale;
        }
    }

    [Serializable]
    public struct RectTransformSnapshot
    {
        public bool IsLocalSpace => transformSnapshot.isLocalSpace;
        public TransformSnapshot transformSnapshot;

        public EngineInternal.Vector2 anchorMin;
        public EngineInternal.Vector2 anchorMax;

        public EngineInternal.Vector2 pivot;

        public EngineInternal.Vector2 sizeDelta;

        public bool setAnchoredPosition;
        public EngineInternal.Vector3 anchoredPosition;

        public bool setOffsetMin;
        public EngineInternal.Vector2 offsetMin;

        public bool setOffsetMax;
        public EngineInternal.Vector2 offsetMax;

        public static RectTransformSnapshot CreateFromLocal(EngineInternal.IRectTransform transform) => new RectTransformSnapshot() { transformSnapshot = TransformSnapshot.CreateFromLocal(transform), anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot, sizeDelta = transform.sizeDelta, anchoredPosition = transform.anchoredPosition3D, offsetMin = transform.offsetMin, offsetMax = transform.offsetMax };
        public static RectTransformSnapshot CreateFromWorld(EngineInternal.IRectTransform transform) => new RectTransformSnapshot() { transformSnapshot = TransformSnapshot.CreateFromWorld(transform), anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot, sizeDelta = transform.sizeDelta, anchoredPosition = transform.anchoredPosition3D, offsetMin = transform.offsetMin, offsetMax = transform.offsetMax };

        public void ApplyTo(EngineInternal.IRectTransform rectTransform)
        {
            rectTransform.pivot = pivot;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = sizeDelta;
             
            transformSnapshot.ApplyTo(rectTransform);

            if (setAnchoredPosition) rectTransform.anchoredPosition3D = anchoredPosition;

            if (setOffsetMin) rectTransform.offsetMin = offsetMin;
            if (setOffsetMax) rectTransform.offsetMax = offsetMax;
        }
    }
    [Serializable]
    public struct RectTransformSnapshotEuler
    {
        public bool IsLocalSpace => transformSnapshot.isLocalSpace;
        public TransformSnapshotEuler transformSnapshot;

        public EngineInternal.Vector2 anchorMin;
        public EngineInternal.Vector2 anchorMax;

        public EngineInternal.Vector2 pivot;

        public EngineInternal.Vector2 sizeDelta;

        public bool setAnchoredPosition;
        public EngineInternal.Vector3 anchoredPosition;

        public bool setOffsetMin;
        public EngineInternal.Vector2 offsetMin;
         
        public bool setOffsetMax;
        public EngineInternal.Vector2 offsetMax;

        public static RectTransformSnapshotEuler CreateFromLocal(EngineInternal.IRectTransform transform) => new RectTransformSnapshotEuler() { transformSnapshot = TransformSnapshotEuler.CreateFromLocal(transform), anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot, sizeDelta = transform.sizeDelta, anchoredPosition = transform.anchoredPosition3D, offsetMin = transform.offsetMin, offsetMax = transform.offsetMax };
        public static RectTransformSnapshotEuler CreateFromWorld(EngineInternal.IRectTransform transform) => new RectTransformSnapshotEuler() { transformSnapshot = TransformSnapshotEuler.CreateFromWorld(transform), anchorMin = transform.anchorMin, anchorMax = transform.anchorMax, pivot = transform.pivot, sizeDelta = transform.sizeDelta, anchoredPosition = transform.anchoredPosition3D, offsetMin = transform.offsetMin, offsetMax = transform.offsetMax };

        public void ApplyTo(EngineInternal.IRectTransform rectTransform)
        {
            rectTransform.pivot = pivot;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = sizeDelta;
             
            transformSnapshot.ApplyTo(rectTransform);

            if (setAnchoredPosition) rectTransform.anchoredPosition3D = anchoredPosition; 

            if (setOffsetMin) rectTransform.offsetMin = offsetMin;
            if (setOffsetMax) rectTransform.offsetMax = offsetMax;
        }
    }

}
