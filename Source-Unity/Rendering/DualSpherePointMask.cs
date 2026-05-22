#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole.Rendering
{

    /// <summary>
    /// Given two end points and accompanying values, can determine if a point should be masked and/or pulled towards the given pull points. Useful for masking points that should be "under" an object, such as a hat.
    /// </summary>
    public class DualSpherePointMask : MonoBehaviour
    {

#if UNITY_EDITOR
        [SerializeField]
#endif
        protected Vector3 debugPoint;

        protected void OnDrawGizmosSelected()
        {
            Vector3 pA = PointA;
            Vector3 pB = PointB;
            Vector3 offset = pB - pA;
            Vector3 dir = offset.normalized;
            Vector3 tangent = new Vector3(dir.z, dir.x, dir.y);

            for (int i = 0; i < 6; i++)
            {
                float ang = (i / 6f) * 360f;
                Vector3 tangent_ = Quaternion.AngleAxis(ang, dir) * tangent;

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pA + tangent_ * pullRadiusMaxA, pA + offset + tangent_ * pullRadiusMaxB);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pA + tangent_ * clipRadiusMaxA, pA + offset + tangent_ * clipRadiusMaxB); 
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pA, pullRadiusMinA);
            Gizmos.DrawWireSphere(pA, pullRadiusMaxA);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pA, clipRadiusMinA);
            Gizmos.DrawWireSphere(pA, clipRadiusMaxA);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pB, pullRadiusMinB);
            Gizmos.DrawWireSphere(pB, pullRadiusMaxB);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pB, clipRadiusMinB);
            Gizmos.DrawWireSphere(pB, clipRadiusMaxB);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(pA, pB);

            Gizmos.color = Color.LerpUnclamped(Color.red, Color.yellow, 0.5f);
            Gizmos.DrawLine (pA, PullOriginA);
            Gizmos.DrawWireSphere(PullOriginA, 0.1f);
            Gizmos.DrawLine(pB, PullOriginB);
            Gizmos.DrawWireSphere(PullOriginB, 0.1f);

            var debugPoint = transform.TransformPoint(this.debugPoint);
            GetMaskingResult(debugPoint, out float masking, out _, out var newDebugPoint);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(debugPoint, 0.08f);
            Gizmos.color = Color.LerpUnclamped(Color.green, Color.magenta, masking);
            Gizmos.DrawLine(debugPoint, newDebugPoint);
            Gizmos.DrawWireSphere(newDebugPoint, 0.08f); 
        }

        [Header("Point A")]
        [SerializeField]
        protected Vector3 pointA;
        public Vector3 PointA => transform.TransformPoint(LocalPointA);
        public Vector3 LocalPointA => pointA;

        [SerializeField]
        protected float clipRadiusMinA = 0.5f;
        [SerializeField]
        protected float clipRadiusMaxA = 1f;
        [SerializeField]
        protected float clipExponentA = 1f;

        [SerializeField]
        protected Vector3 pullOriginA;
        public Vector3 PullOriginA => transform.TransformPoint(LocalPullOriginA);
        public Vector3 LocalPullOriginA => pointA + pullOriginA;

        [SerializeField]
        protected float pullRadiusMinA = 1.5f;
        [SerializeField]
        protected float pullRadiusMaxA = 2f;
        [SerializeField]
        protected float pullExponentA = 1f;

        [Header("Point B")]
        [SerializeField]
        protected Vector3 pointB = Vector3.up;
        public Vector3 PointB => transform.TransformPoint(LocalPointB);
        public Vector3 LocalPointB => pointB;

        [SerializeField]
        protected float clipRadiusMinB = 0.5f;
        [SerializeField]
        protected float clipRadiusMaxB = 1f;
        [SerializeField]
        protected float clipExponentB = 1f;

        [SerializeField]
        protected Vector3 pullOriginB;
        public Vector3 PullOriginB => transform.TransformPoint(LocalPullOriginB);
        public Vector3 LocalPullOriginB => pointB + pullOriginB;

        [SerializeField]
        protected float pullRadiusMinB = 1.5f;
        [SerializeField]
        protected float pullRadiusMaxB = 2f;
        [SerializeField]
        protected float pullExponentB = 1f;

        public void GetMaskingValues(Vector3 worldPoint, out float segT, out float clipT, out float pullT)
        {
            var pA = PointA;
            var pB = PointB;
            segT = Maths.GetValueOnSegment(worldPoint, pA, pB);
            var segPoint = math.lerp(pA, pB, segT);

            var clipRadiusMin = math.lerp(clipRadiusMinA, clipRadiusMinB, segT);
            var clipRadiusMax = math.lerp(clipRadiusMaxA, clipRadiusMaxB, segT);
            var clipExponent = math.lerp(clipExponentA, clipExponentB, segT);

            var pullRadiusMin = math.lerp(pullRadiusMinA, pullRadiusMinB, segT);
            var pullRadiusMax = math.lerp(pullRadiusMaxA, pullRadiusMaxB, segT);
            var pullExponent = math.lerp(pullExponentA, pullExponentB, segT);

            var offset = (float3)worldPoint - segPoint;
            var distance = math.length(offset);

            clipT = 1f - math.pow(math.saturate((distance - clipRadiusMin) / (clipRadiusMax - clipRadiusMin)), clipExponent <= 0f ? 1f : clipExponent);
            pullT = 1f - math.pow(math.saturate((distance - pullRadiusMin) / (pullRadiusMax - pullRadiusMin)), pullExponent <= 0f ? 1f : pullExponent);
        }
        public void GetMasking(Vector3 worldPoint, out float masking, out float pullBlend)
        {
            GetMaskingValues(worldPoint, out _, out var clipT, out var pullT);

            masking = clipT;
            pullBlend = pullT;
        }
        public void GetMaskingResult(Vector3 worldPoint, out float masking, out float pullBlend, out Vector3 newWorldPoint)
        {
            GetMaskingValues(worldPoint, out var segT, out masking, out pullBlend);

            newWorldPoint = math.lerp(worldPoint, math.lerp(PullOriginA, PullOriginB, segT), pullBlend);
        }

        public void UpdateValuesA(
            Vector3 localPointA,
            float clipRadiusMinA, float clipRadiusMaxA, float clipExponentA,
            Vector3 pullOffsetA, float pullRadiusMinA, float pullRadiusMaxA, float pullExponentA, bool updateRendering = true)
        {
            this.pointA = localPointA;
            this.clipRadiusMinA = clipRadiusMinA;
            this.clipRadiusMaxA = clipRadiusMaxA;
            this.clipExponentA = clipExponentA;
            this.pullOriginA = pullOffsetA;
            this.pullRadiusMinA = pullRadiusMinA;
            this.pullRadiusMaxA = pullRadiusMaxA;
            this.pullExponentA = pullExponentA;

            if (updateRendering) UpdateRendering();
        }

        public void UpdateValuesB(
            Vector3 localPointB,
            float clipRadiusMinB, float clipRadiusMaxB, float clipExponentB,
            Vector3 pullOffsetB, float pullRadiusMinB, float pullRadiusMaxB, float pullExponentB, bool updateRendering = true)
        {
            this.pointB = localPointB;
            this.clipRadiusMinB = clipRadiusMinB;
            this.clipRadiusMaxB = clipRadiusMaxB;
            this.clipExponentB = clipExponentB;
            this.pullOriginB = pullOffsetB;
            this.pullRadiusMinB = pullRadiusMinB;
            this.pullRadiusMaxB = pullRadiusMaxB;
            this.pullExponentB = pullExponentB;

            if (updateRendering) UpdateRendering();
        }

        public void UpdateValues(
            Vector3 localPointA,
            float clipRadiusMinA, float clipRadiusMaxA, float clipExponentA,
            Vector3 pullOffsetA, float pullRadiusMinA, float pullRadiusMaxA, float pullExponentA,

            Vector3 localPointB,
            float clipRadiusMinB, float clipRadiusMaxB, float clipExponentB,
            Vector3 pullOffsetB, float pullRadiusMinB, float pullRadiusMaxB, float pullExponentB, bool updateRendering = true)
        {

            UpdateValuesA(localPointA, clipRadiusMinA, clipRadiusMaxA, clipExponentA, pullOffsetA, pullRadiusMinA, pullRadiusMaxA, pullExponentA, false);
            UpdateValuesB(localPointB, clipRadiusMinB, clipRadiusMaxB, clipExponentB, pullOffsetB, pullRadiusMinB, pullRadiusMaxB, pullExponentB, false);

            if (updateRendering) UpdateRendering();
        }

        public const string matProp_pointA = "_DSPM_PointA";
        public const string matProp_clipRadiusMinA = "_DSPM_ClipRadiusMinA";
        public const string matProp_clipRadiusMaxA = "_DSPM_ClipRadiusMaxA";
        public const string matProp_clipExponentA = "_DSPM_ClipExponentA";

        public const string matProp_pullOriginA = "_DSPM_PullOriginA";
        public const string matProp_pullRadiusMinA = "_DSPM_PullRadiusMinA";
        public const string matProp_pullRadiusMaxA = "_DSPM_PullRadiusMaxA";
        public const string matProp_pullExponentA = "_DSPM_PullExponentA";

        public const string matProp_pointB = "_DSPM_PointB";
        public const string matProp_clipRadiusMinB = "_DSPM_ClipRadiusMinB";
        public const string matProp_clipRadiusMaxB = "_DSPM_ClipRadiusMaxB";
        public const string matProp_clipExponentB = "_DSPM_ClipExponentB";

        public const string matProp_pullOriginB = "_DSPM_PullOriginB";
        public const string matProp_pullRadiusMinB = "_DSPM_PullRadiusMinB";
        public const string matProp_pullRadiusMaxB = "_DSPM_PullRadiusMaxB";
        public const string matProp_pullExponentB = "_DSPM_PullExponentB";

        [Serializable]
        public class RenderBinding
        {
            [NonSerialized]
            protected bool isInitialized;

            public Material[] materials;
            public Renderer[] renderers;

            public string matProp_pointA;
            public string matProp_clipRadiusMinA;
            public string matProp_clipRadiusMaxA;
            public string matProp_clipExponentA;

            public string matProp_pullOriginA;
            public string matProp_pullRadiusMinA;
            public string matProp_pullRadiusMaxA;
            public string matProp_pullExponentA;

            public string matProp_pointB;
            public string matProp_clipRadiusMinB;
            public string matProp_clipRadiusMaxB;
            public string matProp_clipExponentB;

            public string matProp_pullOriginB;
            public string matProp_pullRadiusMinB;
            public string matProp_pullRadiusMaxB;
            public string matProp_pullExponentB;

            public void Init()
            {
                if (string.IsNullOrWhiteSpace(matProp_pointA)) matProp_pointA = DualSpherePointMask.matProp_pointA;
                if (string.IsNullOrWhiteSpace(matProp_clipRadiusMinA)) matProp_clipRadiusMinA = DualSpherePointMask.matProp_clipRadiusMinA;
                if (string.IsNullOrWhiteSpace(matProp_clipRadiusMaxA)) matProp_clipRadiusMaxA = DualSpherePointMask.matProp_clipRadiusMaxA;
                if (string.IsNullOrWhiteSpace(matProp_clipExponentA)) matProp_clipExponentA = DualSpherePointMask.matProp_clipExponentA;

                if (string.IsNullOrWhiteSpace(matProp_pullOriginA)) matProp_pullOriginA = DualSpherePointMask.matProp_pullOriginA;
                if (string.IsNullOrWhiteSpace(matProp_pullRadiusMinA)) matProp_pullRadiusMinA = DualSpherePointMask.matProp_pullRadiusMinA;
                if (string.IsNullOrWhiteSpace(matProp_pullRadiusMaxA)) matProp_pullRadiusMaxA = DualSpherePointMask.matProp_pullRadiusMaxA;
                if (string.IsNullOrWhiteSpace(matProp_pullExponentA)) matProp_pullExponentA = DualSpherePointMask.matProp_pullExponentA;

                if (string.IsNullOrWhiteSpace(matProp_pointB)) matProp_pointB = DualSpherePointMask.matProp_pointB;
                if (string.IsNullOrWhiteSpace(matProp_clipRadiusMinB)) matProp_clipRadiusMinB = DualSpherePointMask.matProp_clipRadiusMinB;
                if (string.IsNullOrWhiteSpace(matProp_clipRadiusMaxB)) matProp_clipRadiusMaxB = DualSpherePointMask.matProp_clipRadiusMaxB;
                if (string.IsNullOrWhiteSpace(matProp_clipExponentB)) matProp_clipExponentB = DualSpherePointMask.matProp_clipExponentB;

                if (string.IsNullOrWhiteSpace(matProp_pullOriginB)) matProp_pullOriginB = DualSpherePointMask.matProp_pullOriginB;
                if (string.IsNullOrWhiteSpace(matProp_pullRadiusMinB)) matProp_pullRadiusMinB = DualSpherePointMask.matProp_pullRadiusMinB;
                if (string.IsNullOrWhiteSpace(matProp_pullRadiusMaxB)) matProp_pullRadiusMaxB = DualSpherePointMask.matProp_pullRadiusMaxB;
                if (string.IsNullOrWhiteSpace(matProp_pullExponentB)) matProp_pullExponentB = DualSpherePointMask.matProp_pullExponentB;
            }

            public void UpdateMaterial(Material mat, DualSpherePointMask mask)
            {
                UpdateMaterial(mat, mask, mask.PointA, mask.PointB, mask.PullOriginA, mask.pullOriginB);
            }
            public void UpdateMaterial(Material mat, DualSpherePointMask mask, Vector3 pointA, Vector3 pointB, Vector3 pullOriginA, Vector3 pullOriginB)
            {
                if (mat == null) return;

                mat.SetVector(matProp_pointA, pointA);
                mat.SetFloat(matProp_clipRadiusMinA, mask.clipRadiusMinA);
                mat.SetFloat(matProp_clipRadiusMaxA, mask.clipRadiusMaxA);
                mat.SetFloat(matProp_clipExponentA, mask.clipExponentA);

                mat.SetVector(matProp_pullOriginA, pullOriginA);
                mat.SetFloat(matProp_pullRadiusMinA, mask.pullRadiusMinA);
                mat.SetFloat(matProp_pullRadiusMaxA, mask.pullRadiusMaxA);
                mat.SetFloat(matProp_pullExponentA, mask.pullExponentA);

                mat.SetVector(matProp_pointB, pointB);
                mat.SetFloat(matProp_clipRadiusMinB, mask.clipRadiusMinB);
                mat.SetFloat(matProp_clipRadiusMaxB, mask.clipRadiusMaxB);
                mat.SetFloat(matProp_clipExponentB, mask.clipExponentB);

                mat.SetVector(matProp_pullOriginB, pullOriginB);
                mat.SetFloat(matProp_pullRadiusMinB, mask.pullRadiusMinB);
                mat.SetFloat(matProp_pullRadiusMaxB, mask.pullRadiusMaxB);
                mat.SetFloat(matProp_pullExponentB, mask.pullExponentB);
            }
            public void UpdateRendering(DualSpherePointMask mask)
            {
                var pointA = mask.PointA;
                var pointB = mask.PointB;
                var pullOriginA = mask.PullOriginA;
                var pullOriginB = mask.PullOriginB;

                UpdateRendering(mask, pointA, pointB, pullOriginA, pullOriginB);
            }
            public void UpdateRendering(DualSpherePointMask mask, Vector3 pointA, Vector3 pointB, Vector3 pullOriginA, Vector3 pullOriginB)
            {
                if (!isInitialized) Init();

                if (materials != null && materials.Length > 0)
                {
                    foreach (var material in materials)
                    {
                        UpdateMaterial(material, mask, pointA, pointB, pullOriginA, pullOriginB);
                    }
                }

                if (renderers != null && renderers.Length > 0)
                {
                    foreach (var r in renderers)
                    {
                        var mats = r.sharedMaterials;
                        if (mats != null)
                        {
                            foreach (var material in mats) UpdateMaterial(material, mask, pointA, pointB, pullOriginA, pullOriginB);
                        }
                    }
                }
            }
        }

        [Header("Settings")]
        public bool autoUpdateRendering;

        [Header("Bindings")]
        [SerializeField]
        protected List<RenderBinding> renderBindings = new List<RenderBinding>();

        public void UpdateRendering()
        {
            if (renderBindings != null && renderBindings.Count > 0)
            {
                var pointA = PointA;
                var pointB = PointB;
                var pullOriginA = PullOriginA;
                var pullOriginB = PullOriginB;

                foreach (var binding in renderBindings) binding.UpdateRendering(this, pointA, pointB, pullOriginA, pullOriginB);
            }
        }

        protected virtual void LateUpdate()
        {
            if (autoUpdateRendering)
            {
                UpdateRendering(); 
            }
        }

    }

}

#endif