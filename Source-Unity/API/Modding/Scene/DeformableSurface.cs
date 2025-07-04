#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Mathematics;

using Swole.DataStructures;

namespace Swole.API.Unity
{
    /// <summary>
    /// A component that compliments SurfaceProxy by adding nuance to mesh surface snapping.
    /// </summary>
    [ExecuteAlways]
    public class DeformableSurface : MonoBehaviour
    {
        public MeshFilter[] filters;

        [Tooltip("Provide a displacement map to help preserve height details when using local height adjutment.")]
        public Texture2D displacementMap;
        public UVChannelURP displacementUV;
        public float displacementHeight = 1f;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [Tooltip("These points allow some mesh vertices to be grouped around a center 'surface' position. Useful for things like rocks that are resting on a surface but are part of the same mesh. The points are used to preserve detailed height geometry, where it might otherwise get flattened out by surface snapping.")]
        public List<SurfaceOriginPoint> originPoints;

        [Serializable]
        public class SurfaceOriginPoint
        {
            [Tooltip("The local surface position that the mapped vertices will use as a reference point for deformation. This point will be converted to world space and any surface deformation that is applied there will also be applied to the mapped vertices.")]
            public Vector3 surfacePointLocalPosition;

            public SurfaceOriginPointBounds[] bounds;

            public List<MappedCollider> colliders;

            public List<AffectedIndex> GetAffectedIndices(Transform meshTransform, ICollection<Vector3> vertexPositions, List<AffectedIndex> affectedIndices = null)
            {
                if (affectedIndices == null) affectedIndices = new List<AffectedIndex>();

                if (bounds != null)
                {
                    if (debugData == null) debugData = new List<DebugVertex>();
                    debugData.Clear();

                    for(int a = 0; a < bounds.Length; a++)
                    {
                        var boundingSphere = bounds[a];
                        var boundingPos = boundingSphere.localPosition + surfacePointLocalPosition;

                        int i = 0;
                        foreach (var vertexPos in vertexPositions)
                        {
                            float distance = math.distance(vertexPos, boundingPos);
                            if (distance <= boundingSphere.radius)
                            {
                                float t = distance / boundingSphere.radius;
                                float weight = boundingSphere.falloff == null || boundingSphere.falloff.length <= 0 ? t : boundingSphere.falloff.Evaluate(t);

                                affectedIndices.Add(new AffectedIndex()
                                {
                                    index = i,
                                    weight = weight,
                                    offset = (float3)(boundingPos - vertexPos)
                                });

                                if (meshTransform != null)
                                {
                                    debugData.Add(new DebugVertex
                                    {
                                        boundingSphereIndex = a,
                                        vertexPos = vertexPos,
                                        weight = weight
                                    });
                                }
                            }

                            i++;
                        }
                    }
                }

                return affectedIndices;
            }

            [HideInInspector]
            public List<DebugVertex> debugData;
        }

        [Serializable]
        public struct SurfaceOriginPointBounds
        {
            public Vector3 localPosition;
            public float radius;
            public AnimationCurve falloff;
        }
        [Serializable]
        public struct AffectedIndex
        {
            public int index;
            public float weight;
            public float3 offset;
        }
        [Serializable]
        public struct DebugVertex
        {
            public int boundingSphereIndex;
            public float3 vertexPos;
            public float weight;
        }
        [Serializable]
        public struct MappedCollider
        {
            public Collider collider;
            public float weight;
        }
        [Serializable]
        private struct WeightedOffset
        {
            public float totalWeight;
            public float3 combinedOffset;
        }

        public bool drawDebugData = true;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (originPoints != null && filters != null && filters.Length > 0)
            {
                var filter = filters[0];
                int f = 1;
                while(filter == null && f < filters.Length)
                {
                    filter = filters[f];
                    f++;
                }
                if (filter == null) return;

                var meshTransform = filter.transform;

                var view = SceneView.currentDrawingSceneView;
                var viewPos = view != null && view.camera != null ? view.camera.transform.position : Vector3.zero;
                foreach(var point in originPoints)
                {
                    if (point != null)
                    {
                        Vector3 originPos = meshTransform.TransformPoint(point.surfacePointLocalPosition);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(originPos, view == null || view.camera == null ? 0.05f : (0.01f * (viewPos - originPos).magnitude)); 

                        if (point.bounds != null)
                        {
                            foreach(var boundingSphere in point.bounds)
                            {
                                var pos = meshTransform.TransformPoint(point.surfacePointLocalPosition + boundingSphere.localPosition);

                                Gizmos.color = Color.green;
                                Gizmos.DrawWireSphere(pos, boundingSphere.radius); 
                                Gizmos.color = Color.blue;
                                Gizmos.DrawLine(pos, originPos);
                            }
                        }

                        if (drawDebugData && point.debugData != null && point.bounds != null)
                        {
                            foreach (var debugPoint in point.debugData)
                            {
                                if (debugPoint.boundingSphereIndex < 0 || debugPoint.boundingSphereIndex >= point.bounds.Length) continue;

                                var boundingSphere = point.bounds[debugPoint.boundingSphereIndex];
                                Gizmos.color = Color.Lerp(Color.red, Color.green, debugPoint.weight);
                                Gizmos.DrawLine(meshTransform.TransformPoint(debugPoint.vertexPos), meshTransform.TransformPoint(point.surfacePointLocalPosition + boundingSphere.localPosition)); 
                            }
                        }

                        if (point.colliders != null)
                        {
                            foreach (var mapping in point.colliders)
                            {
                                if (mapping.collider != null)
                                {
                                    Gizmos.color = Color.Lerp(Color.red, Color.green, mapping.weight);
                                    Gizmos.DrawLine(mapping.collider.transform.position, originPos);
                                }
                            }
                        }
                    }
                }
            }
        }
#endif

        public void WriteOriginPointOffsets(Transform meshTransform, List<Vector4> uv, ICollection<Vector3> vertexPositions)
        {
            if (originPoints != null && originPoints.Count > 0)
            {
                List<AffectedIndex> affectedIndices = new List<AffectedIndex>();
                Dictionary<int, WeightedOffset> offsets = new Dictionary<int, WeightedOffset>();
                for (int i = 0; i < originPoints.Count; i++)
                {
                    var point = originPoints[i];
                    if (point != null)
                    {
                        affectedIndices.Clear();
                        point.GetAffectedIndices(meshTransform, vertexPositions, affectedIndices);

                        foreach(var affectedIndex in affectedIndices)
                        {
                            offsets.TryGetValue(affectedIndex.index, out WeightedOffset offsetEntry);
                            offsetEntry.totalWeight += affectedIndex.weight;
                            offsetEntry.combinedOffset = offsetEntry.combinedOffset + affectedIndex.offset * affectedIndex.weight;
                            offsets[affectedIndex.index] = offsetEntry;
                        }
                    }
                }

                foreach(var entry in offsets)
                {
                    if (entry.Value.totalWeight == 0f) continue;

                    Vector3 finalOffset = entry.Value.combinedOffset / (entry.Value.totalWeight > 1f ? entry.Value.totalWeight : 1f); 
                    uv[entry.Key] = uv[entry.Key] + new Vector4(finalOffset.x, finalOffset.y, finalOffset.z, 0f);
                }
            }
        }

        public Mesh WriteToMesh(Transform meshTransform, Mesh mesh, UVChannelURP uvChannel, bool instantiate)
        {
            if (mesh == null) return null;

            if (instantiate) mesh = MeshUtils.DuplicateMesh(mesh);

            var vertices = mesh.vertices;

            List<Vector2> displacementUVs = MeshUtils.GetUVsByChannelAsList(mesh, displacementUV);
            List<Vector4> uv = MeshUtils.GetUVsByChannelAsListV4(mesh, uvChannel);
            if (uv == null) uv = new List<Vector4>();
            if (uv.Count < vertices.Length) 
            {
                uv.Clear();
                for (int i = 0; i < vertices.Length; i++)
                {
                    uv.Add(Vector4.zero); 
                }
            } 
            else
            {
                for (int i = 0; i < uv.Count; i++)
                {
                    uv[i] = new Vector4(0f, 0f, 0f, uv[i].w); 
                }
            }

            if (displacementMap != null && displacementHeight != 0f && displacementUVs != null && displacementUVs.Count > 0)
            {
                for(int i = 0; i < displacementUVs.Count; i++)
                {
                    var disP_uv = displacementUVs[i];
                    uv[i] = uv[i] + new Vector4(0f, displacementMap.GetPixelBilinear(disP_uv.x, disP_uv.y).r * -displacementHeight, 0f, 0f);  
                }
            }

            WriteOriginPointOffsets(meshTransform, uv, vertices);
            mesh.SetUVs((int)uvChannel, uv);

            return mesh;
        }

        public UVChannelURP surfaceOffsetsUVChannel = UVChannelURP.UV2;
        public bool writeSurfaceOffsetsToMesh;

        public bool instantiateMesh = true;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public string meshSaveDir;
        public string meshAssetName;

        [HideInInspector]
        public Mesh[] originalMeshes;
        [HideInInspector]
        public Mesh[] instantiatedMeshes;
        public bool reset;
        public bool reapply;

#if UNITY_EDITOR
        private void OnValidate()
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

                        var ogMesh = originalMeshes[a];
                        var instMesh = instantiatedMeshes[a];

                        if (ReferenceEquals(instMesh, filter.sharedMesh)) filter.sharedMesh = ogMesh;
                        originalMeshes[a] = null;
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

                        var ogMesh = originalMeshes[a];
                        var instMesh = instantiatedMeshes[a];

                        if (!ReferenceEquals(instMesh, filter.sharedMesh)) 
                        {
                            originalMeshes[a] = filter.sharedMesh;
                            filter.sharedMesh = instMesh; 
                        }
                    }
                }
            }

            if (writeSurfaceOffsetsToMesh)
            {
                writeSurfaceOffsetsToMesh = false;

                if (filters != null)
                {
                    if (originalMeshes == null || originalMeshes.Length != filters.Length) Array.Resize(ref originalMeshes, filters.Length);
                    if (instantiatedMeshes == null || instantiatedMeshes.Length != filters.Length) Array.Resize(ref instantiatedMeshes, filters.Length);
                }

                for(int a = 0; a < filters.Length; a++)
                {
                    var filter = filters[a];

                    if (filter == null) filter = gameObject.GetComponent<MeshFilter>();
                    if (filter != null)
                    {
                        var ogMesh = originalMeshes[a];
                        if (ogMesh == null || (!ReferenceEquals(ogMesh, filter.sharedMesh) && !ReferenceEquals(ogMesh, filter.sharedMesh))) ogMesh = filter.sharedMesh;
                        originalMeshes[a] = ogMesh;

                        var instMesh = WriteToMesh(filter.transform, ogMesh, surfaceOffsetsUVChannel, instantiateMesh);
                        if (instMesh != null)
                        {
                            if (string.IsNullOrWhiteSpace(meshAssetName))
                            {
                                meshAssetName = $"{ogMesh.name}_{GetInstanceID()}{Time.frameCount}"; 
                            }

                            if (instantiateMesh)
                            {
                                instMesh.name = $"{meshAssetName}_{a}";

                                string saveDir = meshSaveDir;
                                if (string.IsNullOrWhiteSpace(saveDir))
                                {
                                    var activeScene = SceneManager.GetActiveScene();
                                    saveDir = Path.Combine(Path.GetDirectoryName(activeScene.path), activeScene.name, "Surfaces", "Meshes");
                                }

                                if (!string.IsNullOrWhiteSpace(saveDir))
                                {
                                    instMesh = instMesh.CreateOrReplaceAsset(instMesh.CreateUnityAssetPathString(saveDir, "asset"));
                                }
                            }

                            filter.sharedMesh = instMesh;
                            instantiatedMeshes[a] = instMesh;
                        }
                    }
                }
            }
        }
#endif
    }
}

#endif