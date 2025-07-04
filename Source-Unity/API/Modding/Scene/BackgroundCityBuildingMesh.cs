#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole.DataStructures;

namespace Swole.API.Unity
{
    [ExecuteAlways]
    public class BackgroundCityBuildingMesh : MonoBehaviour
    {
        public bool generate;

        public string saveDir;
        public string assetName;

        [Serializable]
        public struct MaterialReplacement
        {
            public bool instantiateMaterial;
            public Material targetMaterial;
            public Material replacementMaterial;
        }

        public MaterialReplacement[] targetMaterials;
        public bool TryGetMaterialReplacement(Material mat, out MaterialReplacement replacement)
        {
            replacement = default;
            if (targetMaterials == null) return false;

            foreach (var replacement_ in targetMaterials) if (mat == replacement_.targetMaterial)
                {
                    replacement = replacement_; 
                    return true;
                }

            return false;
        }

        public UVChannelURP dataChannelA = UVChannelURP.UV3;
        //public UVChannelURP dataChannelB = UVChannelURP.UV3;

        public Vector3 heightAxis = Vector3.up;
        public Vector3 forwardAxis = Vector3.forward;

        public string localHeightAxisProperty = "_LocalHeightAxis";
        public string localForwardAxisProperty = "_LocalForwardAxis";
        public string localRightAxisProperty = "_LocalRightAxis";

        [Serializable]
        public struct MaterialArray
        {
            public Material[] array;
            public bool IsValid => array != null && array.Length > 0;
            public int Length => array == null ? 0 : array.Length;

            public Material this[int index] => array == null ? null : array[index];

            public static implicit operator MaterialArray(Material[] array) => new MaterialArray() { array = array };
            public static implicit operator Material[](MaterialArray array) => array.array;
        }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public MeshFilter[] filters;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public Mesh[] originalMeshes;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public MaterialArray[] originalMaterials;


#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public Mesh[] instantiatedMeshes;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public MaterialArray[] instantiatedMaterials;

        public bool reset;
        public bool reapply;

        public Mesh GenerateBuildingMesh(Mesh baseMesh, MeshFilter filter, MeshRenderer renderer, out List<Material> materialsToSave)
        {
            materialsToSave = null;
            if (filter == null || baseMesh == null) return null;

            if (renderer == null) renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null) return null;

            var mats = renderer.sharedMaterials;
            if (mats == null || mats.Length <= 0) return null;

            var newMesh = MeshUtils.DuplicateMesh(baseMesh);

            Vector3[] vertices = newMesh.vertices;
            var islands = MeshDataTools.CalculateMeshIslands(newMesh, null, true, out var weldedVertices);

            var colors = newMesh.colors;
            if (colors == null || colors.Length != vertices.Length)
            {
                colors = new Color[vertices.Length];
            }

            List<Vector4> uv0 = newMesh.GetUVsByChannelAsListV4(UVChannelURP.UV0);
            List<Vector4> uv1 = newMesh.GetUVsByChannelAsListV4(UVChannelURP.UV1);
            List<Vector4> uv2 = newMesh.GetUVsByChannelAsListV4(UVChannelURP.UV2);
            List<Vector4> uv3 = newMesh.GetUVsByChannelAsListV4(UVChannelURP.UV3);

            void PrepDataChannel(UVChannelURP dataChannel)
            {
                switch (dataChannel)
                {
                    case UVChannelURP.UV0:
                        if (uv0 == null) uv0 = new List<Vector4>();
                        if (uv0.Count != vertices.Length)
                        {
                            uv0.Clear();
                            for (int a = 0; a < vertices.Length; a++) uv0.Add(Vector4.zero);
                        }
                        break;
                    case UVChannelURP.UV1:
                        if (uv1 == null) uv1 = new List<Vector4>();
                        if (uv1.Count != vertices.Length)
                        {
                            uv1.Clear();
                            for (int a = 0; a < vertices.Length; a++) uv1.Add(Vector4.zero);
                        }
                        break;
                    case UVChannelURP.UV2:
                        if (uv2 == null) uv2 = new List<Vector4>();
                        if (uv2.Count != vertices.Length)
                        {
                            uv2.Clear();
                            for (int a = 0; a < vertices.Length; a++) uv2.Add(Vector4.zero);
                        }
                        break;
                    case UVChannelURP.UV3:
                        if (uv3 == null) uv3 = new List<Vector4>();
                        if (uv3.Count != vertices.Length)
                        {
                            uv3.Clear();
                            for (int a = 0; a < vertices.Length; a++) uv3.Add(Vector4.zero);
                        }
                        break;
                }
            }
            PrepDataChannel(dataChannelA);
            //PrepDataChannel(dataChannelB);

            List<Vector3> tempPositions = new List<Vector3>();
            Vector3 localHeightAxis = filter.transform.InverseTransformDirection(heightAxis).normalized;
            Vector3 localForwardAxis = filter.transform.InverseTransformDirection(forwardAxis).normalized;
            Vector3 localRightAxis = Vector3.Cross(localForwardAxis, localHeightAxis).normalized;

#if UNITY_EDITOR
            int islandIndex = 0;
#endif
            foreach (var island in islands)
            {
                if (island.vertices == null || island.vertices.Length <= 0) continue;

                float centerX = 0f;
                float centerZ = 0f;
                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;
                float radius = 0f;

                tempPositions.Clear();

                int i = 0;
                foreach (var vert in island.vertices)
                {
                    if (weldedVertices[vert].firstIndex != vert) continue;

                    i++;

                    var pos = vertices[vert];

                    centerX += Vector3.Dot(pos, localRightAxis);
                    centerZ += Vector3.Dot(pos, localForwardAxis);
                }
                centerX = centerX / i;
                centerZ = centerZ / i;
                Vector2 center = new Vector2(centerX, centerZ);

                foreach (var vert in island.vertices)
                {
                    var pos = vertices[vert];

                    var posX = Vector3.Dot(pos, localRightAxis);
                    var posZ = Vector3.Dot(pos, localForwardAxis);

                    float height = Vector3.Dot(pos, localHeightAxis);
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height); 

                    radius = Mathf.Max(radius, Vector2.Distance(center, new Vector2(posX, posZ)));
                }

#if UNITY_EDITOR
                if (Mathf.Abs(maxHeight - minHeight) < 0.0001f)
                {
                    Debug.Log($"Error: zero height island {islandIndex} :: {island.vertices.Length}");
                    Debug.DrawRay(filter.transform.TransformPoint(vertices[island.vertices[0]]), Vector3.up * 1000, Color.red, 60); 
                }
                islandIndex++;
#endif

                var uvData = new Vector4(centerX, centerZ, minHeight, maxHeight); 
                foreach (var vert in island.vertices)
                {
                    switch(dataChannelA)
                    {
                        case UVChannelURP.UV0:
                            uv0[vert] = uvData;
                            break;
                        case UVChannelURP.UV1:
                            uv1[vert] = uvData;
                            break;
                        case UVChannelURP.UV2:
                            uv2[vert] = uvData;
                            break;
                        case UVChannelURP.UV3:
                            uv3[vert] = uvData;
                            break;
                    }

                    var color = colors[vert];
                    color.a = radius;

                    colors[vert] = color;

                    var weld = weldedVertices[vert];
                    for (int j = 0; j < weld.indices.Count; j++)
                    {
                        int ind = weld.indices[j];
                        switch (dataChannelA)
                        {
                            case UVChannelURP.UV0:
                                uv0[ind] = uvData;
                                break;
                            case UVChannelURP.UV1:
                                uv1[ind] = uvData;
                                break;
                            case UVChannelURP.UV2:
                                uv2[ind] = uvData;
                                break;
                            case UVChannelURP.UV3:
                                uv3[ind] = uvData;
                                break;
                        }

                        color = colors[ind];
                        color.a = radius;

                        colors[ind] = color;  
                    }
                }
            }

            if (uv0 != null && uv0.Count > 0) newMesh.SetUVs((int)UVChannelURP.UV0, uv0);
            if (uv1 != null && uv1.Count > 0) newMesh.SetUVs((int)UVChannelURP.UV1, uv1);
            if (uv2 != null && uv2.Count > 0) newMesh.SetUVs((int)UVChannelURP.UV2, uv2);
            if (uv3 != null && uv3.Count > 0) newMesh.SetUVs((int)UVChannelURP.UV3, uv3);

            for (int a = 0; a < newMesh.subMeshCount; a++)
            {
                var mat = mats[a];
                if (mat == null || !TryGetMaterialReplacement(mat, out var replacement)) continue;

                var replacementMat = replacement.replacementMaterial;
                if (replacement.instantiateMaterial) 
                {
                    replacementMat = Instantiate(replacementMat);
                    replacementMat.name = $"{replacement.replacementMaterial.name}_{GetInstanceID()}{Time.frameCount}";

                    if (materialsToSave == null) materialsToSave = new List<Material>();
                    materialsToSave.Add(replacementMat);
                }

                if (replacementMat.HasProperty(localHeightAxisProperty)) replacementMat.SetVector(localHeightAxisProperty, localHeightAxis);
                if (replacementMat.HasProperty(localForwardAxisProperty)) replacementMat.SetVector(localForwardAxisProperty, localForwardAxis);
                if (replacementMat.HasProperty(localRightAxisProperty)) replacementMat.SetVector(localRightAxisProperty, localRightAxis);

                mats[a] = replacementMat;
            }

            newMesh.colors = colors;

            filter.sharedMesh = newMesh;
            renderer.sharedMaterials = mats;

            return newMesh;
        }

#if UNITY_EDITOR
        public void Update()
        {
            if (reset)
            {
                reset = false;
                if (filters != null && originalMeshes != null && instantiatedMeshes != null)
                {
                    int count = Mathf.Min(filters.Length, originalMeshes.Length, instantiatedMeshes.Length); 
                    for (int a = 0; a < count; a++)
                    {
                        var filter = filters[a];
                        var renderer = filter.GetComponent<MeshRenderer>();

                        var ogMesh = originalMeshes[a];
                        var instMesh = instantiatedMeshes[a];

                        if (ReferenceEquals(instMesh, filter.sharedMesh)) filter.sharedMesh = ogMesh;
                        originalMeshes[a] = null;

                        if (renderer != null && originalMaterials != null && originalMaterials.Length >= count)
                        {
                            if (originalMaterials[a].IsValid)
                            {
                                renderer.sharedMaterials = originalMaterials[a].array;
                            }

                            originalMaterials[a] = default;
                        }
                    }
                }
            }
            if (reapply)
            {
                reapply = false;
                if (filters != null && originalMeshes != null && instantiatedMeshes != null)
                {
                    int count = Mathf.Min(filters.Length, originalMeshes.Length, instantiatedMeshes.Length);
                    for (int a = 0; a < count; a++)
                    {
                        var filter = filters[a];
                        var renderer = filter.GetComponent<MeshRenderer>();

                        var ogMesh = originalMeshes[a];
                        var instMesh = instantiatedMeshes[a];

                        if (!ReferenceEquals(instMesh, filter.sharedMesh))
                        {
                            originalMeshes[a] = filter.sharedMesh;
                            filter.sharedMesh = instMesh;
                        }

                        if (renderer != null && originalMaterials != null && instantiatedMaterials != null && originalMaterials.Length >= count && instantiatedMaterials.Length >= count)
                        {
                            if (!originalMaterials[a].IsValid) originalMaterials[a] = renderer.sharedMaterials;
                            renderer.sharedMaterials = instantiatedMaterials[a];
                        }
                    }
                }
            }

            if (generate)
            {
                generate = false;

                if (filters != null)
                {
                    if (originalMeshes == null || originalMeshes.Length != filters.Length) Array.Resize(ref originalMeshes, filters.Length);
                    if (instantiatedMeshes == null || instantiatedMeshes.Length != filters.Length) Array.Resize(ref instantiatedMeshes, filters.Length);

                    if (originalMaterials == null || originalMaterials.Length != filters.Length) Array.Resize(ref originalMaterials, filters.Length);
                    if (instantiatedMaterials == null || instantiatedMaterials.Length != filters.Length) Array.Resize(ref instantiatedMaterials, filters.Length);
                }

                for (int a = 0; a < filters.Length; a++)
                {
                    var filter = filters[a];

                    if (filter == null) filter = gameObject.GetComponent<MeshFilter>();

                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;

                    if (filter != null)
                    {
                        var ogMesh = originalMeshes[a];
                        if (ogMesh == null || (!ReferenceEquals(ogMesh, filter.sharedMesh) && !ReferenceEquals(ogMesh, filter.sharedMesh))) ogMesh = filter.sharedMesh;
                        originalMeshes[a] = ogMesh;

                        var ogMats = originalMaterials[a];
                        if (!ogMats.IsValid) ogMats = renderer.sharedMaterials;
                        originalMaterials[a] = ogMats;

                        var instMesh = GenerateBuildingMesh(ogMesh, filter, renderer, out var matsToSave);
                        if (instMesh != null)
                        {
                            if (string.IsNullOrWhiteSpace(assetName))
                            {
                                assetName = $"{name}_{GetInstanceID()}{Time.frameCount}";
                            }

                            instMesh.name = $"{assetName}_{a}";

                            string saveDir = this.saveDir;
                            if (string.IsNullOrWhiteSpace(saveDir))
                            {
                                var activeScene = SceneManager.GetActiveScene();
                                saveDir = Path.Combine(Path.GetDirectoryName(activeScene.path), activeScene.name, "Meshes", "City");
                            }

                            if (!string.IsNullOrWhiteSpace(saveDir))
                            {
                                instMesh = instMesh.CreateOrReplaceAsset(instMesh.CreateUnityAssetPathString(saveDir));
                            }


                            filter.sharedMesh = instMesh;
                            instantiatedMeshes[a] = instMesh;

                            var newMats = renderer.sharedMaterials;
                            if (matsToSave != null && matsToSave.Count > 0)
                            {
                                var oldMats = instantiatedMaterials[a];
                                if (oldMats.IsValid)
                                {
                                    for (int b = 0; b < Mathf.Min(newMats.Length, oldMats.Length); b++)
                                    {
                                        var newMat = newMats[b];
                                        var oldMat = oldMats[b];

                                        if (newMat != null)
                                        {
                                            bool save = matsToSave.Contains(newMat);
                                            if (oldMat != null && save)
                                            {
                                                newMat.name = oldMat.name;
                                            }

                                            if (save && !string.IsNullOrWhiteSpace(saveDir)) newMat.CreateOrReplaceAsset(instMesh.CreateUnityAssetPathString(saveDir));
                                        }
                                    }
                                }
                            }

                            instantiatedMaterials[a] = newMats;
                        }
                    }
                }
            }
        }
#endif

    }
}

#endif