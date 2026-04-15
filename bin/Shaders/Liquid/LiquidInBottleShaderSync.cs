using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

namespace Swole
{

    /// <summary>
    /// Adapted from: https://www.patreon.com/posts/fake-liquid-urp-75665057
    /// </summary>
    [ExecuteInEditMode]
    public class LiquidInBottleShaderSync : MonoBehaviour
    {
        public enum UpdateMode { Normal, UnscaledTime }
        public UpdateMode updateMode;

        [SerializeField]
        float MaxWobble = 0.03f;
        [SerializeField]
        float WobbleSpeedMove = 1f;
        [SerializeField]
        public float fillAmount = 0.5f;
        [SerializeField]
        public bool invertFill;
        public float ShaderFill => Mathf.Clamp01(invertFill ? (1f - fillAmount) : fillAmount);
        [SerializeField]
        float Recovery = 1f;
        [SerializeField]
        float Thickness = 1f;
        [SerializeField]
        Mesh mesh;
        [SerializeField]
        Renderer rend;

        [SerializeField]
        private Vector3 boundsSizeOffset;
        [SerializeField]
        private Vector3 boundsCenterOffset;

        private void OnDrawGizmosSelected()
        {
            GetMeshAndRend();

            Gizmos.color = Color.yellow;
            if (mesh != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                var bounds = mesh.bounds;
                Gizmos.DrawWireCube(bounds.center + boundsCenterOffset, bounds.size + boundsSizeOffset);

                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        [SerializeField]
        private int[] materialSlots;
        [NonSerialized]
        private Material[] materials;
        public Material[] Materials => materials;

        Vector3 pos;
        Vector3 lastPos;
        Vector3 velocity;
        Quaternion lastRot;
        Vector3 angularVelocity;
        float wobbleAmountX;
        float wobbleAmountZ;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;
        float pulse;
        float sinewave;
        float time = 0.5f;
        Vector3 comp;

        // Use this for initialization
        void Start()
        {
            GetMeshAndRend();

            if (Application.isPlaying)
            {
                if (materialSlots != null)
                {
                    materials = new Material[materialSlots.Length];
                    var mats = rend.sharedMaterials;
                    for(int i = 0; i < materialSlots.Length; i++)
                    {
                        var slot = materialSlots[i];
                        if (slot < 0 || slot >= mats.Length) continue;

                        var mat = mats[slot];
                        mat = Instantiate(mat);
                        mats[slot] = mat;
                        materials[i] = mat;
                    }
                    rend.sharedMaterials = mats;
                }
            }
        }

        protected void OnDestroy()
        {
            if (materials != null)
            {
                foreach (var mat in materials) GameObject.Destroy(mat); 
                materials = null;
            }
        }

        private void OnValidate()
        {
            GetMeshAndRend();
        }

        void GetMeshAndRend()
        {
            if (mesh == null)
            {
                mesh = GetComponent<MeshFilter>().sharedMesh;
            }
            if (rend == null)
            {
                rend = GetComponent<Renderer>();
            }
        }
        void Update()
        {
            float deltaTime = 0;
            switch (updateMode)
            {
                case UpdateMode.Normal:
                    deltaTime = Time.deltaTime;
                    break;

                case UpdateMode.UnscaledTime:
                    deltaTime = Time.unscaledDeltaTime;
                    break;
            }

            time += deltaTime;

            if (deltaTime != 0)
            {


                // decrease wobble over time
                wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, (deltaTime * Recovery));
                wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, (deltaTime * Recovery));



                // make a sine wave of the decreasing wobble
                pulse = 2 * Mathf.PI * WobbleSpeedMove;
                sinewave = Mathf.Lerp(sinewave, Mathf.Sin(pulse * time), deltaTime * Mathf.Clamp(velocity.magnitude + angularVelocity.magnitude, Thickness, 10));

                wobbleAmountX = wobbleAmountToAddX * sinewave;
                wobbleAmountZ = wobbleAmountToAddZ * sinewave;



                // velocity
                velocity = (lastPos - transform.position) / deltaTime;

                angularVelocity = GetAngularVelocity(lastRot, transform.rotation);

                // add clamped velocity to wobble
                wobbleAmountToAddX += Mathf.Clamp((velocity.x + (velocity.y * 0.2f) + angularVelocity.z + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
                wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (velocity.y * 0.2f) + angularVelocity.x + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
            }

            if (materials != null)
            {
                float wobbleAmountX_ = fillAmount > 0.0001f ? wobbleAmountX : 0f;
                float wobbleAmountZ_ = fillAmount > 0.0001f ? wobbleAmountZ : 0f;
                foreach (var mat in materials)
                {
                    if (mat == null) continue;

                    mat.SetFloat("_WobbleX", wobbleAmountX_);
                    mat.SetFloat("_WobbleZ", wobbleAmountZ_); 
                }
            }
            // send it to the shader

            // set fill amount
            UpdatePos(deltaTime);

            // keep last position
            lastPos = transform.position;
            lastRot = transform.rotation;
        }

        void UpdatePos(float deltaTime)
        {

            var bounds = mesh.bounds;
            bounds.center = bounds.center + boundsCenterOffset;
            bounds.size = bounds.size + boundsSizeOffset;
            Vector3 center = transform.TransformPoint(bounds.center); 
            var pos = transform.position;
            var topPoint = bounds.ClosestPoint(transform.InverseTransformPoint(center + Vector3.up * 10000f));
            var botPoint = bounds.ClosestPoint(transform.InverseTransformPoint(center + Vector3.down * 10000f)); 
            pos = (new Vector3(center.x, 0f, center.z) - new Vector3(pos.x, 0f, pos.z)) + new Vector3(0f, Mathf.LerpUnclamped(transform.TransformVector(botPoint).y, transform.TransformVector(topPoint).y, ShaderFill), 0f);     

            if (materials != null)
            {
                foreach (var mat in materials)
                {
                    if (mat == null) continue;

                    mat.SetVector("_FillAmount", pos); 
                }
            }
        }

        //https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/#post-4302796
        Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
        {
            var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
            // no rotation?
            // You may want to increase this closer to 1 if you want to handle very small rotations.
            // Beware, if it is too close to one your answer will be Nan
            if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
                return Vector3.zero;
            float gain;
            // handle negatives, we could just flip it but this is faster
            if (q.w < 0.0f)
            {
                var angle = Mathf.Acos(-q.w);
                gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
            }
            else
            {
                var angle = Mathf.Acos(q.w);
                gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
            }
            Vector3 angularVelocity = new Vector3(q.x * gain, q.y * gain, q.z * gain);

            if (float.IsNaN(angularVelocity.z))
            {
                angularVelocity = Vector3.zero;
            }
            return angularVelocity;
        }
    }

}