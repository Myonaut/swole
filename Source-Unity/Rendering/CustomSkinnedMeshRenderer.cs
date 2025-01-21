#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.API.Unity;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class CustomSkinnedMeshRenderer : MonoBehaviour
    {

#if UNITY_EDITOR

        public virtual void OnDrawGizmosSelected()
        {
            if (meshRenderer == null) return;

            Vector3 extents = meshRenderer.transform.TransformVector(bounds.extents);
            Vector3 center = meshRenderer.transform.TransformPoint(bounds.center);

            Vector3 p1 = center + new Vector3(-extents.x, -extents.y, extents.z);
            Vector3 p2 = center + new Vector3(extents.x, -extents.y, extents.z);
            Vector3 p3 = center + new Vector3(extents.x, extents.y, extents.z);
            Vector3 p4 = center + new Vector3(-extents.x, extents.y, extents.z);

            Vector3 p5 = center + new Vector3(-extents.x, -extents.y, -extents.z);
            Vector3 p6 = center + new Vector3(extents.x, -extents.y, -extents.z);
            Vector3 p7 = center + new Vector3(extents.x, extents.y, -extents.z);
            Vector3 p8 = center + new Vector3(-extents.x, extents.y, -extents.z);

            Gizmos.color = Color.white;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            Gizmos.DrawLine(p1, p5);
            Gizmos.DrawLine(p2, p6);
            Gizmos.DrawLine(p3, p7);
            Gizmos.DrawLine(p4, p8);

            Gizmos.DrawLine(p5, p6);
            Gizmos.DrawLine(p6, p7);
            Gizmos.DrawLine(p7, p8);
            Gizmos.DrawLine(p8, p5); 
        }

        [Serializable]
        public class BlendShapeController
        {

            public string name;
            [Range(0, 100)]
            public float weight;
            [NonSerialized]
            protected float prevWeight;

            public bool HasUpdated(bool reset = true)
            {

                bool updated = prevWeight != weight;

                if (reset) prevWeight = weight;

                return updated;

            }

        }

        public BlendShapeController[] blendShapes;

        protected void OnValidate()
        {

            if (m_mesh != null && blendShapes != null)
            {

                for (int a = 0; a < blendShapes.Length; a++)
                {

                    var shape = blendShapes[a];

                    if (shape == null || !shape.HasUpdated()) continue;

                    m_mesh.SetBlendShapeWeight(a, shape.weight);

                }

            }

        }

#endif

        public bool instantiateMaterials = true;

        public CustomSkinnedMeshData data;

        public MeshRenderer meshRenderer;

        public Mesh GetMesh()
        {

            MeshFilter filter = meshRenderer == null ? null : meshRenderer.gameObject.GetComponent<MeshFilter>();

            if (filter == null) return null;

            return filter.sharedMesh;

        }

        public Bounds bounds;

        public Material[] materials;

        [SerializeField]
        protected Transform[] bones;
        public Transform[] Bones => bones;
        public void SetBones(Transform[] bones)
        {
            if (bones == null)
            {
                this.bones = null;
                return;
            }

            this.bones = (Transform[])bones.Clone(); 
        }

        [NonSerialized]
        protected bool initialized;
        public bool IsInitialized => initialized;

        protected CustomSkinnedMesh.RenderedMesh m_mesh;

        public int BlendShapeCount => data.BlendShapeCount;

        public string GetBlendShapeName(int shapeIndex) => data == null ? "" : data.GetBlendShapeName(shapeIndex);

        public int GetBlendShapeFrameCount(int shapeIndex) => data == null ? 0 : data.GetBlendShapeFrameCount(shapeIndex);

        public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex) => data == null ? 0 : data.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

        public int GetBlendShapeIndex(string blendShapeName) => m_mesh == null ? -1 : m_mesh.GetBlendShapeIndex(blendShapeName);

        public void SetBlendShapeWeight(string blendShapeName, float weight)
        {

            m_mesh?.SetBlendShapeWeight(blendShapeName, weight);

#if UNITY_EDITOR

            if (blendShapes != null)
            {

                int index = m_mesh.GetBlendShapeIndex(blendShapeName);
                if (index >= 0 && index < blendShapes.Length) blendShapes[index].weight = weight;

            }

#endif

        }

        public void SetBlendShapeWeight(int blendShapeIndex, float weight)
        {

            m_mesh?.SetBlendShapeWeight(blendShapeIndex, weight);

#if UNITY_EDITOR

            if (blendShapes != null)
            {

                if (blendShapeIndex >= 0 && blendShapeIndex < blendShapes.Length) blendShapes[blendShapeIndex].weight = weight;

            }

#endif

        }

        public void Initialize()
        {

            if (initialized) return;

            if (meshRenderer != null) meshRenderer.localBounds = bounds;

            var tempBones = bones;
            if (tempBones != null)
            {
                var remapper = GetComponentInParent<BoneTransformRemapper>();
                if (remapper != null && remapper.ShouldRemap(this))
                {
                    swole.Log($"Remapping rig for {name} using remapper {remapper.name}");   

                    tempBones = new Transform[bones.Length];
                    bones.CopyTo(tempBones, 0);
                    bool ContainsBone(Transform bone)
                    {
                        if (bone == null) return false;
                        foreach (var t in tempBones) if (t == bone) return true;
                        return false;
                    }
                    var root = transform.parent == null ? transform : transform.parent;
                    if (bones.Length > 0)
                    {
                        root = bones[0];
                        while (ContainsBone(root.parent)) root = root.parent; // Find the rig's root
                    }
                    remapper.Remap(tempBones, root);  
                }
            } 

            //initialized = CustomSkinnedMesh.TryAddMesh(out m_mesh, data, bones, meshRenderer, materials, instantiateMaterials, data.rigId + "_" + GetInstanceID().ToString());
            initialized = CustomSkinnedMesh.TryAddMesh(out m_mesh, data, tempBones, meshRenderer, materials, instantiateMaterials, (tempBones == null || tempBones.Length <= 0 || tempBones[0] == null) ? (data.rigId + "_" + GetInstanceID().ToString()) : tempBones[0].GetInstanceID().ToString()); // Try to use root bone instance id as rig id

            //if (!Application.isEditor) GameObject.Destroy(this);

            if (data.blendShapeNames != null)
            {

#if UNITY_EDITOR

                blendShapes = new BlendShapeController[data.blendShapeNames.Length];

                for (int a = 0; a < data.blendShapeNames.Length; a++)
                {

                    BlendShapeController controller = new BlendShapeController()
                    {

                        name = data.blendShapeNames[a],

                    };

                    blendShapes[a] = controller;

                }

#endif

            }

        }

        public void Start()
        {

            Initialize();

        }

    }

}

#endif