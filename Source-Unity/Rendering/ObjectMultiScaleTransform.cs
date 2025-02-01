#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    public class ObjectMultiScaleTransform : MonoBehaviour
    {

        public bool destroyAfterFirstSync;

        [Serializable]
        public struct Doppelganger
        {
            public Transform transform;
            public float scale;
            public bool syncTransformScale;
        }

        public Doppelganger[] doppelgangers;

        public void SyncPosition()
        {
            if (doppelgangers == null) return;

            Vector3 worldPos = transform.position;
            foreach(var d in doppelgangers)
            {
                if (d.transform == null) continue;

                d.transform.position = worldPos * d.scale;
            }
        }
        public void SyncRotation()
        {
            if (doppelgangers == null) return;

            Quaternion worldRot = transform.rotation;
            foreach (var d in doppelgangers)
            {
                if (d.transform == null) continue;

                d.transform.rotation = worldRot;
            }
        }
        public void SyncScale()
        {
            if (doppelgangers == null) return;

            Vector3 scale = transform.localScale;
            foreach (var d in doppelgangers)
            {
                if (d.transform == null || !d.syncTransformScale) continue;

                d.transform.localScale = scale * d.scale;
            }
        }
        public void SyncPositionAndRotation()
        {
            if (doppelgangers == null) return;

            transform.GetPositionAndRotation(out Vector3 worldPos, out Quaternion worldRot);
            foreach (var d in doppelgangers)
            {
                if (d.transform == null || d.scale <= 0) continue;

                d.transform.SetPositionAndRotation(worldPos * d.scale, worldRot);
            }
        }
        public void Sync()
        {
            if (doppelgangers == null) return;

            transform.GetPositionAndRotation(out Vector3 worldPos, out Quaternion worldRot);
            Vector3 scale = transform.localScale;
            foreach (var d in doppelgangers)
            {
                if (d.transform == null || d.scale <= 0) continue;

                d.transform.SetPositionAndRotation(worldPos * d.scale, worldRot);
                if (d.syncTransformScale) d.transform.localScale = scale * d.scale; 
            }
        }

        protected void Awake()
        {
            Sync();
        }

        protected void Start()
        {
            Sync();
            if (destroyAfterFirstSync) Destroy(this);
        }

        protected void LateUpdate()
        {
            SyncPositionAndRotation();
        }

    }
}

#endif