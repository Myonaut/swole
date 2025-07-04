#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

using Unity.Collections.LowLevel.Unsafe;

using Swole.UI;
using Swole.API.Unity;

namespace Swole.Modding
{
    public class MeshEditor : MonoBehaviour, IDisposable
    {

        unsafe public void Dispose()
        {
            if (collisionTriangles.IsCreated)
            {
                collisionTriangles.Dispose();
                collisionTriangles = default;
            }
            if (collisionVertexData.IsCreated)
            {
                collisionVertexData.Dispose();
                collisionVertexData = default;
            }
            if (collisionVertexSkinning.IsCreated)
            {
                collisionVertexSkinning.Dispose();
                collisionVertexSkinning = default;
            }     

            if (collisionBlendShapeData.IsCreated)
            {
                collisionBlendShapeData.Dispose();
                collisionBlendShapeData = default;
            }
            if (collisionBlendShapeFrameCounts.IsCreated)
            {
                collisionBlendShapeFrameCounts.Dispose();
                collisionBlendShapeFrameCounts = default;
            }
            if (collisionBlendShapeStartIndices.IsCreated)
            {
                collisionBlendShapeStartIndices.Dispose();
                collisionBlendShapeStartIndices = default;
            }

            if (nearbyCollisionTriangles.IsCreated)
            {
                nearbyCollisionTriangles.Dispose();
                nearbyCollisionTriangles = default;
            }
            if (nearbyCollisionTriangleCounts.IsCreated)
            {
                nearbyCollisionTriangleCounts.Dispose();
                nearbyCollisionTriangleCounts = default;
            }
            if (nearbyCollisionTriangleStartIndices.IsCreated)
            {
                nearbyCollisionTriangleStartIndices.Dispose();
                nearbyCollisionTriangleStartIndices = default;
            }

            if (targetVertexIndices.IsCreated)
            {
                targetVertexIndices.Dispose();
                targetVertexIndices = default;
            }

            verticesArray = null;
            if (vertices.IsCreated)
            {
                vertices.Dispose();
                vertices = default;
            }
            normalsArray = null;
            if (normals.IsCreated)
            {
                normals.Dispose();
                normals = default;
            }
            tangentsArray = null;
            if (tangents.IsCreated)
            {
                tangents.Dispose();
                tangents = default;
            }

            if (baseVertexData.IsCreated)
            {
                baseVertexData.Dispose();
                baseVertexData = default;
            }

            trianglesArray = null;
            if (triangles.IsCreated)
            {
                triangles.Dispose();
                triangles = default;
            }

            if (boneWeights.IsCreated)
            {
                boneWeights.Dispose();
                boneWeights = default;
            }
            if (bonesPerVertex.IsCreated)
            {
                bonesPerVertex.Dispose();
                bonesPerVertex = default;
            }

            bonesArray = null;
            bindposesArray = null;
            if (bindposes.IsCreated)
            {
                bindposes.Dispose();
                bindposes = default;
            }
            boneMatricesArray = null;
            if (boneMatrices.IsCreated)
            {
                boneMatrices.Dispose();
                boneMatrices = default;
            }

            perVertexSkinningMatricesArray = null;
            perVertexSkinningMatricesInverseArray = null;
            if (perVertexSkinningMatrices.IsCreated)
            {
                perVertexSkinningMatrices.Dispose();
                perVertexSkinningMatrices = default;
            }
            if (perVertexSkinningMatricesInverse.IsCreated)
            {
                perVertexSkinningMatricesInverse.Dispose();
                perVertexSkinningMatricesInverse = default;
            }
            skinnedVerticesArray = null;
            if (skinnedVertices.IsCreated)
            {
                skinnedVertices.Dispose();
                skinnedVertices = default;
            }

            mergedVerticesArray = null;
            if (mergedVertexIndices.IsCreated)
            {
                mergedVertexIndices.Dispose();
                mergedVertexIndices = default;
            }
            if (mergedVertexFirstIndices.IsCreated)
            {
                mergedVertexFirstIndices.Dispose();
                mergedVertexFirstIndices = default;
            }
            if (mergedVertexIndexCount.IsCreated)
            {
                mergedVertexIndexCount.Dispose();
                mergedVertexIndexCount = default;
            }
            if (mergedVertexStartIndices.IsCreated)
            {
                mergedVertexStartIndices.Dispose();
                mergedVertexStartIndices = default;
            }

            vertexConnectionArrays = null;
            if (vertexConnections.IsCreated)
            {
                vertexConnections.Dispose();
                vertexConnections = default;
            }
            if (vertexConnectionCounts.IsCreated)
            {
                vertexConnectionCounts.Dispose();
                vertexConnectionCounts = default;
            }
            if (vertexConnectionStartIndices.IsCreated)
            {
                vertexConnectionStartIndices.Dispose();
                vertexConnectionStartIndices = default;
            }

            vertexTriangleArrays = null;
            if (vertexTriangles.IsCreated)
            {
                vertexTriangles.Dispose();
                vertexTriangles = default;
            }
            if (vertexTriangleCounts.IsCreated)
            {
                vertexTriangleCounts.Dispose();
                vertexTriangleCounts = default;
            }
            if (vertexTriangleStartIndices.IsCreated)
            {
                vertexTriangleStartIndices.Dispose();
                vertexTriangleStartIndices = default;
            }

            editedBlendShapes.Clear();
            if (editedBlendShapeDeltas.IsCreated)
            {
                editedBlendShapeDeltas.Dispose();
                editedBlendShapeDeltas = default;
            }
            if (previousEditedBlendShapeDeltas.IsCreated)
            {
                previousEditedBlendShapeDeltas.Dispose();
                previousEditedBlendShapeDeltas = default;
            }
            if (editedBlendShapeFrameCounts.IsCreated)
            {
                editedBlendShapeFrameCounts.Dispose();
                editedBlendShapeFrameCounts = default;
            }
            if (editedBlendShapeStartIndices.IsCreated)
            {
                editedBlendShapeStartIndices.Dispose();
                editedBlendShapeStartIndices = default;
            }
            foreach(var list in savedBlendShapeStates)
            {
                if (list == null) continue;

                foreach(var state in list)
                {
                    foreach (var frame in state)
                    {
                        if (frame.IsCreated)
                        {
                            frame.Dispose();
                        }
                    }
                }
                list.Clear();
            }
            savedBlendShapeStates.Clear();

            if (depenetrationMask.IsCreated)
            { 
                depenetrationMask.Dispose();
                depenetrationMask = default;
            }
        }
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        protected NativeList<MeshDataTools.Triangle> collisionTriangles;
        protected NativeList<BaseVertexData> collisionVertexData;
        protected NativeList<float4x4> collisionVertexSkinning;

        protected List<string> collisionShapeNames;
        protected NativeList<ShapeVertexDelta> collisionBlendShapeData;
        protected NativeList<int> collisionBlendShapeFrameCounts;
        protected NativeList<int> collisionBlendShapeStartIndices;
        protected int FindCollisionShapeIndex(string shapeName)
        {
            if (collisionShapeNames == null) return -1;

            for (int a = 0; a < collisionShapeNames.Count; a++)
            {
                var name = collisionShapeNames[a];
                if (name == shapeName) return a;
            }

            shapeName = shapeName.AsID();

            for (int a = 0; a < collisionShapeNames.Count; a++)
            {
                var name = collisionShapeNames[a];
                if (!string.IsNullOrWhiteSpace(name) && name.AsID() == shapeName) return a;
            }

            return -1;
        }

        protected NativeList<int> nearbyCollisionTriangles;
        protected NativeList<int> nearbyCollisionTriangleCounts;
        protected NativeList<int> nearbyCollisionTriangleStartIndices;
        public bool NearbyCollisionVerticesAreInitialized => nearbyCollisionTriangles.IsCreated;
        public void InitializeNearbyCollisionTriangles(float maxDistance = 0.15f, int batchSize = 1000)
        {
            if (editedMesh == null || !HasCollisionData) return;

            if (!nearbyCollisionTriangles.IsCreated) nearbyCollisionTriangles = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!nearbyCollisionTriangleCounts.IsCreated) nearbyCollisionTriangleCounts = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!nearbyCollisionTriangleStartIndices.IsCreated) nearbyCollisionTriangleStartIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);

            nearbyCollisionTriangles.Clear();
            nearbyCollisionTriangleCounts.Clear();
            nearbyCollisionTriangleStartIndices.Clear(); 

            NativeArray<bool> nearbyCollisionTrianglesOutput = new NativeArray<bool>(batchSize * collisionTriangles.Length, Allocator.Persistent);
            int batchCount = Mathf.CeilToInt(editedMesh.vertexCount / (float)batchSize); 
            JobHandle jobs = default;

            for (int a = 0; a < batchCount; a++)
            {
                int startIndex = a * batchSize;
                int size = Mathf.Min(batchSize, editedMesh.vertexCount - startIndex);

                jobs = new FindNearbyCollisionTrianglesJob()
                {
                    batchSize = batchSize,
                    size = size,
                    localIndexOffset = startIndex,
                    maxDistance = maxDistance,
                    collisionTriangles = collisionTriangles,
                    collisionVertexData = collisionVertexData,
                    collisionSkinningData = collisionVertexSkinning,
                    localVertexData = BaseVertexData,
                    localSkinningData = PerVertexSkinningMatrices,
                    proximityOutput = nearbyCollisionTrianglesOutput
                }.Schedule(collisionTriangles.Length, 1, jobs);
                jobs = new EvaluateNearbyCollisionTrianglesJob()
                {
                    batchSize = batchSize,
                    collisionTriangleCount = collisionTriangles.Length,
                    proximityOutput = nearbyCollisionTrianglesOutput,
                    nearbyCollisionTriangles = nearbyCollisionTriangles,
                    nearbyCollisionTriangleCounts = nearbyCollisionTriangleCounts,
                    nearbyCollisionTriangleStartIndices = nearbyCollisionTriangleStartIndices
                }.Schedule(size, jobs);
            }

            jobs.Complete();
            nearbyCollisionTrianglesOutput.Dispose();
        }

        [BurstCompile]
        protected struct FindNearbyCollisionTrianglesJob : IJobParallelFor
        {

            public int batchSize;
            public int size;
            public int localIndexOffset;

            public float maxDistance;

            [ReadOnly]
            public NativeList<MeshDataTools.Triangle> collisionTriangles;
            [ReadOnly]
            public NativeList<BaseVertexData> collisionVertexData;
            [ReadOnly]
            public NativeList<float4x4> collisionSkinningData;

            [ReadOnly]
            public NativeList<BaseVertexData> localVertexData;
            [ReadOnly]
            public NativeList<float4x4> localSkinningData;

            [NativeDisableParallelForRestriction]
            public NativeArray<bool> proximityOutput;

            public void Execute(int collisionTriangleIndex)
            {
                var triangle = collisionTriangles[collisionTriangleIndex];

                var collisionVertex1 = math.transform(collisionSkinningData[triangle.i0], collisionVertexData[triangle.i0].position);
                var collisionVertex2 = math.transform(collisionSkinningData[triangle.i1], collisionVertexData[triangle.i1].position);
                var collisionVertex3 = math.transform(collisionSkinningData[triangle.i2], collisionVertexData[triangle.i2].position); 

                for (int a = 0; a < size; a++)
                {
                    int localIndex = localIndexOffset + a;

                    var localVertex = math.transform(localSkinningData[localIndex], localVertexData[localIndex].position);
                    float3 distances = new float3(math.distance(localVertex, collisionVertex1), math.distance(localVertex, collisionVertex2), math.distance(localVertex, collisionVertex3));
                    bool3 withinRange = distances <= maxDistance;

                    int outputIndex = (collisionTriangleIndex * batchSize) + a;
                    proximityOutput[outputIndex] = math.any(withinRange);
                }
            }
        }

        [BurstCompile]
        protected struct EvaluateNearbyCollisionTrianglesJob : IJobFor
        {
            public int batchSize;

            public int collisionTriangleCount; 

            [ReadOnly]
            public NativeArray<bool> proximityOutput;

            public NativeList<int> nearbyCollisionTriangles;
            public NativeList<int> nearbyCollisionTriangleCounts;
            public NativeList<int> nearbyCollisionTriangleStartIndices; 

            public void Execute(int index)
            {
                nearbyCollisionTriangleStartIndices.Add(nearbyCollisionTriangles.Length);

                int nearbyCount = 0;
                for(int collisionTriangleIndex = 0; collisionTriangleIndex < collisionTriangleCount; collisionTriangleIndex++)
                {
                    if (proximityOutput[(collisionTriangleIndex * batchSize) + index]) 
                    {
                        nearbyCollisionTriangles.Add(collisionTriangleIndex);
                        nearbyCount++;
                    }
                }
                nearbyCollisionTriangleCounts.Add(nearbyCount);
                //Debug.Log(index + " : " + nearbyCount);
            }
        }

        public bool HasCollisionData
        {
            get
            {
                if (!collisionVertexData.IsCreated) return false;
                if (!collisionTriangles.IsCreated) return false;

                return true;
            }
        }

        public void SetCollisionData(ICollection<MeshDataTools.Triangle> triangleData, ICollection<BaseVertexData> vertexData, ICollection<float4x4> vertexSkinning, ICollection<string> blendShapeNames, ICollection<ShapeVertexDelta> blendShapeData, ICollection<int> blendShapeFrameCounts, ICollection<int> blendShapeStartIndices, float maxNearbyVertexCollisionDistance = 0.15f)
        {
            if (!collisionTriangles.IsCreated) collisionTriangles = new NativeList<MeshDataTools.Triangle>(triangleData.Count, Allocator.Persistent);
            if (!collisionVertexData.IsCreated) collisionVertexData = new NativeList<BaseVertexData>(vertexData.Count, Allocator.Persistent);
            if (!collisionVertexSkinning.IsCreated) collisionVertexSkinning = new NativeList<float4x4>(vertexSkinning.Count, Allocator.Persistent);

            if (collisionShapeNames == null) collisionShapeNames = new List<string>();
            if (!collisionBlendShapeData.IsCreated) collisionBlendShapeData = new NativeList<ShapeVertexDelta>(blendShapeData.Count, Allocator.Persistent);
            if (!collisionBlendShapeFrameCounts.IsCreated) collisionBlendShapeFrameCounts = new NativeList<int>(blendShapeFrameCounts.Count, Allocator.Persistent);
            if (!collisionBlendShapeStartIndices.IsCreated) collisionBlendShapeStartIndices = new NativeList<int>(blendShapeStartIndices.Count, Allocator.Persistent);

            collisionTriangles.Clear();
            collisionVertexData.Clear();
            collisionVertexSkinning.Clear();

            collisionShapeNames.Clear();
            collisionBlendShapeData.Clear();
            collisionBlendShapeFrameCounts.Clear();
            collisionBlendShapeStartIndices.Clear();

            foreach (var tri in triangleData) collisionTriangles.Add(tri);
            foreach (var vertex in vertexData) collisionVertexData.Add(vertex);
            foreach (var mat in vertexSkinning) collisionVertexSkinning.Add(mat);

            foreach (var shapeName in blendShapeNames) collisionShapeNames.Add(shapeName);
            foreach (var delta in blendShapeData) collisionBlendShapeData.Add(delta);
            foreach (var count in blendShapeFrameCounts) collisionBlendShapeFrameCounts.Add(count);
            foreach (var ind in blendShapeStartIndices) collisionBlendShapeStartIndices.Add(ind);

            InitializeNearbyCollisionTriangles(maxNearbyVertexCollisionDistance); 
        }
        public void SetCollisionData(NativeArray<MeshDataTools.Triangle> triangleData, NativeArray<BaseVertexData> vertexData, NativeArray<float4x4> vertexSkinning, ICollection<string> blendShapeNames, NativeArray<ShapeVertexDelta> blendShapeData, NativeArray<int> blendShapeFrameCounts, NativeArray<int> blendShapeStartIndices, float maxNearbyVertexCollisionDistance = 0.15f)
        {
            if (!collisionTriangles.IsCreated) collisionTriangles = new NativeList<MeshDataTools.Triangle>(triangleData.Length, Allocator.Persistent);
            if (!collisionVertexData.IsCreated) collisionVertexData = new NativeList<BaseVertexData>(vertexData.Length, Allocator.Persistent);
            if (!collisionVertexSkinning.IsCreated) collisionVertexSkinning = new NativeList<float4x4>(vertexSkinning.Length, Allocator.Persistent);

            if (collisionShapeNames == null) collisionShapeNames = new List<string>();
            if (!collisionBlendShapeData.IsCreated) collisionBlendShapeData = new NativeList<ShapeVertexDelta>(blendShapeData.Length, Allocator.Persistent);
            if (!collisionBlendShapeFrameCounts.IsCreated) collisionBlendShapeFrameCounts = new NativeList<int>(blendShapeFrameCounts.Length, Allocator.Persistent);
            if (!collisionBlendShapeStartIndices.IsCreated) collisionBlendShapeStartIndices = new NativeList<int>(blendShapeStartIndices.Length, Allocator.Persistent);

            collisionTriangles.Clear();
            collisionVertexData.Clear();
            collisionVertexSkinning.Clear();

            collisionShapeNames.Clear();
            collisionBlendShapeData.Clear();
            collisionBlendShapeFrameCounts.Clear();
            collisionBlendShapeStartIndices.Clear();

            collisionTriangles.CopyFrom(triangleData);
            collisionVertexData.CopyFrom(vertexData);
            collisionVertexSkinning.CopyFrom(vertexSkinning);

            foreach (var shapeName in blendShapeNames) collisionShapeNames.Add(shapeName);
            collisionBlendShapeData.CopyFrom(blendShapeData);
            collisionBlendShapeFrameCounts.CopyFrom(blendShapeFrameCounts);
            collisionBlendShapeStartIndices.CopyFrom(blendShapeStartIndices);

            InitializeNearbyCollisionTriangles(maxNearbyVertexCollisionDistance);
        }

        public void UpdateCollisionVertexSkinning(ICollection<Matrix4x4> vertexSkinning)
        {
            if (!collisionVertexSkinning.IsCreated) collisionVertexSkinning = new NativeList<float4x4>(vertexSkinning.Count, Allocator.Persistent);
            collisionVertexSkinning.Clear(); 
            foreach (var mat in vertexSkinning) collisionVertexSkinning.Add(mat);
        }
        public void UpdateCollisionVertexSkinning(ICollection<float4x4> vertexSkinning)
        {
            if (!collisionVertexSkinning.IsCreated) collisionVertexSkinning = new NativeList<float4x4>(vertexSkinning.Count, Allocator.Persistent);
            collisionVertexSkinning.Clear();
            foreach (var mat in vertexSkinning) collisionVertexSkinning.Add(mat);
        }
        public void UpdateCollisionVertexSkinning(NativeArray<float4x4> vertexSkinning)
        {
            if (!collisionVertexSkinning.IsCreated) collisionVertexSkinning = new NativeList<float4x4>(vertexSkinning.Length, Allocator.Persistent);
            collisionVertexSkinning.Clear();
            collisionVertexSkinning.CopyFrom(vertexSkinning);
        }

        protected NativeList<bool> depenetrationMask;
        public NativeList<bool> DepenetrationMask
        {
            get
            {
                if (!depenetrationMask.IsCreated)
                {
                    if (VerticesArray == null) return default;
                    depenetrationMask = new NativeList<bool>(verticesArray.Length, Allocator.Persistent);
                    for (int a = 0; a < verticesArray.Length; a++) depenetrationMask.Add(false);
                }

                return depenetrationMask; 
            }
        }
        public void ClearDepenetrationMask()
        {
            if (!depenetrationMask.IsCreated) return;
            for (int a = 0; a < depenetrationMask.Length; a++) depenetrationMask[a] = false;  
        }
        public void SelectDepenetrationMask(bool clearSelection = true)
        {
            if (!DepenetrationMask.IsCreated || !TargetVertexIndices.IsCreated) return;

            if (clearSelection) 
            {
                ClearSelectedVertices();
                for (int a = 0; a < depenetrationMask.Length; a++) if (depenetrationMask[a]) targetVertexIndices.Add(a);
            } 
            else
            {
                for (int a = 0; a < depenetrationMask.Length; a++) if (depenetrationMask[a] && !IsVertexSelected(a)) targetVertexIndices.Add(a);
            }
        }

        protected Mesh originalMesh;
        public Mesh OriginalMesh => originalMesh;
        protected Mesh editedMesh;
        public Mesh EditedMesh => editedMesh;

        public void StartEditingMesh(Mesh mesh)
        {
            if (ReferenceEquals(originalMesh, mesh)) return;

            Dispose();

            originalMesh = mesh;
            editedMesh = MeshUtils.DuplicateMesh(originalMesh);

            if (!targetVertexIndices.IsCreated) targetVertexIndices = new NativeList<int>(0, Allocator.Persistent);
        }

        public MeshFilter meshViewerFilter;
        public SkinnedMeshRenderer meshViewerSkinnedRenderer;

        protected NativeList<int> targetVertexIndices;
        public NativeList<int> TargetVertexIndices
        {
            get
            {
                if (!targetVertexIndices.IsCreated) targetVertexIndices = new NativeList<int>(0, Allocator.Persistent);
                return targetVertexIndices;
            }
        }

        public void SelectVertices(ICollection<int> indices, bool clearSelection = true)
        {
            if (!targetVertexIndices.IsCreated) targetVertexIndices = new NativeList<int>(indices.Count, Allocator.Persistent);

            if (clearSelection)
            {
                ClearSelectedVertices();
                foreach (var index in indices) targetVertexIndices.Add(index);
            }
            else
            {
                foreach (var index in indices) 
                { 
                    if (!IsVertexSelected(index)) targetVertexIndices.Add(index);
                }
            }
        }
        public void SelectAllVertices()
        {
            if (editedMesh == null) return;

            TargetVertexIndices.Clear();
            for (int a = 0; a < editedMesh.vertexCount; a++) targetVertexIndices.Add(a);
        }
        public void ClearSelectedVertices()
        {
            TargetVertexIndices.Clear();
        }
        public bool IsVertexSelected(int index)
        {
            for (int a = 0; a < targetVertexIndices.Length; a++) if (targetVertexIndices[a] == index) return true;
            return false;
        }

        protected Vector3[] verticesArray;
        public Vector3[] VerticesArray
        {
            get
            {
                if (verticesArray == null)
                {
                    if (editedMesh == null) return null;
                    verticesArray = editedMesh.vertices;
                }

                return verticesArray;
            }
        }
        protected NativeList<float3> vertices;
        public NativeList<float3> Vertices
        {
            get
            {
                if (!vertices.IsCreated)
                {
                    if (editedMesh == null) return default;

                    vertices = new NativeList<float3>(VerticesArray.Length, Allocator.Persistent);
                    for(int a = 0; a < verticesArray.Length; a++) vertices.Add(verticesArray[a]);  
                }

                return vertices;
            }
        }
        protected Vector3[] normalsArray;
        public Vector3[] NormalsArray
        {
            get
            {
                if (normalsArray == null)
                {
                    if (editedMesh == null) return null;
                    normalsArray = editedMesh.normals;
                }

                return normalsArray;
            }
        }
        protected NativeList<float3> normals;
        public NativeList<float3> Normals
        {
            get
            {
                if (!normals.IsCreated)
                {
                    if (editedMesh == null) return default;

                    normals = new NativeList<float3>(NormalsArray.Length, Allocator.Persistent);
                    for (int a = 0; a < normalsArray.Length; a++) normals.Add(normalsArray[a]);
                }

                return normals;
            }
        }
        protected Vector4[] tangentsArray;
        public Vector4[] TangentsArray
        {
            get
            {
                if (tangentsArray == null)
                {
                    if (editedMesh == null) return null;
                    tangentsArray = editedMesh.tangents; 
                }

                return tangentsArray;
            }
        }
        protected NativeList<float4> tangents;
        public NativeList<float4> Tangents
        {
            get
            {
                if (!tangents.IsCreated)
                {
                    if (editedMesh == null) return default;

                    tangents = new NativeList<float4>(TangentsArray.Length, Allocator.Persistent);
                    for (int a = 0; a < tangentsArray.Length; a++) tangents.Add(tangentsArray[a]);
                }

                return tangents; 
            }
        }

        protected NativeList<BaseVertexData> baseVertexData;
        public NativeList<BaseVertexData> BaseVertexData
        {
            get
            {
                if (!baseVertexData.IsCreated)
                {
                    RefreshBaseVertexData();
                }

                return baseVertexData;
            }
        }
        public void RefreshBaseVertexData()
        {
            if (editedMesh == null || VerticesArray == null || NormalsArray == null || TangentsArray == null) return;

            if (!baseVertexData.IsCreated) baseVertexData = new NativeList<BaseVertexData>(editedMesh.vertexCount, Allocator.Persistent); 

            baseVertexData.Clear();
            for(int a = 0; a < editedMesh.vertexCount; a++)
            {
                baseVertexData.Add(new BaseVertexData() { position = verticesArray[a], normal = normalsArray[a], tangent = tangentsArray[a] });
            }
        }

        protected int[] trianglesArray;
        public int[] TrianglesArray
        {
            get
            {
                if (trianglesArray == null)
                {
                    if (editedMesh == null) return null;
                    trianglesArray = editedMesh.triangles;
                }

                return trianglesArray;
            }
        }
        protected NativeList<int> triangles;
        public NativeList<int> Triangles
        {
            get
            {
                if (!triangles.IsCreated)
                {
                    if (editedMesh == null) return default;

                    triangles = new NativeList<int>(TrianglesArray.Length, Allocator.Persistent);
                    for (int a = 0; a < trianglesArray.Length; a++) triangles.Add(trianglesArray[a]);
                }

                return triangles;
            }
        }

        protected NativeList<BoneWeight1> boneWeights;
        public NativeList<BoneWeight1> BoneWeights
        {
            get
            {
                if (!boneWeights.IsCreated)
                {
                    if (editedMesh == null) return default;

                    boneWeights = new NativeList<BoneWeight1>(editedMesh.vertexCount, Allocator.Persistent);
                    using (var temp = editedMesh.GetAllBoneWeights())
                    {
                        for (int a = 0; a < temp.Length; a++) boneWeights.Add(temp[a]);
                    }
                }

                return boneWeights;
            }
        }
        protected NativeList<byte> bonesPerVertex;
        public NativeList<byte> BonesPerVertex
        {
            get
            {
                if (!bonesPerVertex.IsCreated)
                {
                    if (editedMesh == null) return default;

                    bonesPerVertex = new NativeList<byte>(editedMesh.vertexCount, Allocator.Persistent);
                    using (var temp = editedMesh.GetBonesPerVertex())
                    {
                        for (int a = 0; a < temp.Length; a++) bonesPerVertex.Add(temp[a]);
                    }
                }

                return bonesPerVertex;
            }
        }

        protected Transform[] bonesArray;
        public Transform[] BonesArray
        {
            get
            {
                if (bonesArray == null)
                {
                    if (meshViewerSkinnedRenderer == null) return default;
                    bonesArray = meshViewerSkinnedRenderer.bones;
                }

                return bonesArray;
            }
        }
        protected Matrix4x4[] bindposesArray;
        public Matrix4x4[] BindposesArray
        {
            get
            {
                if (bindposesArray == null)
                {
                    if (editedMesh == null) return default;
                    bindposesArray = editedMesh.bindposes;
                }

                return bindposesArray;
            }
        }
        protected NativeArray<float4x4> bindposes;
        public NativeArray<float4x4> Bindposes
        {
            get
            {
                if (!bindposes.IsCreated)
                {
                    if (BindposesArray == null) return default;

                    bindposes = new NativeArray<float4x4>(bindposesArray.Length, Allocator.Persistent);
                    for (int a = 0; a < bindposesArray.Length; a++) bindposes[a] = bindposesArray[a];
                }

                return bindposes;
            }
        }
        protected Matrix4x4[] boneMatricesArray;
        public Matrix4x4[] BoneMatricesArray
        {
            get
            {
                if (boneMatricesArray == null)
                {
                    RefreshBoneMatrices();
                }

                return boneMatricesArray;
            }
        }
        protected NativeArray<float4x4> boneMatrices;
        public NativeArray<float4x4> BoneMatrices
        {
            get
            {
                if (!boneMatrices.IsCreated)
                {
                    if (BoneMatricesArray == null) return default;

                    boneMatrices = new NativeArray<float4x4>(boneMatricesArray.Length, Allocator.Persistent);
                    for (int a = 0; a < boneMatricesArray.Length; a++) boneMatrices[a] = boneMatricesArray[a];
                }

                return boneMatrices;
            }
        }
        public void RefreshBoneMatrices()
        {
            if (BonesArray == null || BindposesArray == null) return;

            if (boneMatricesArray == null) boneMatricesArray = new Matrix4x4[bonesArray.Length];
            if (boneMatrices.IsCreated)
            {
                for (int a = 0; a < boneMatricesArray.Length; a++)
                {
                    Transform bone = bonesArray[a];
                    boneMatrices[a] = boneMatricesArray[a] = bone.localToWorldMatrix * bindposesArray[a];
                }
            } 
            else
            {
                for (int a = 0; a < boneMatricesArray.Length; a++)
                {
                    Transform bone = bonesArray[a];
                    boneMatricesArray[a] = bone.localToWorldMatrix * bindposesArray[a];
                }
            }
        }

        protected Matrix4x4[] perVertexSkinningMatricesArray;
        protected Matrix4x4[] perVertexSkinningMatricesInverseArray;
        public Matrix4x4[] PerVertexSkinningMatricesArray
        {
            get
            {
                if (perVertexSkinningMatricesArray == null)
                {
                    RefreshPerVertexSkinningMatrices();
                }
                
                return perVertexSkinningMatricesArray;
            }
        }
        public Matrix4x4[] PerVertexSkinningMatricesInverseArray
        {
            get
            {
                if (perVertexSkinningMatricesInverseArray == null)
                {
                    RefreshPerVertexSkinningMatrices();
                }

                return perVertexSkinningMatricesInverseArray;
            }
        }
        protected NativeList<float4x4> perVertexSkinningMatrices;
        protected NativeList<float4x4> perVertexSkinningMatricesInverse;
        public NativeList<float4x4> PerVertexSkinningMatrices
        {
            get
            {
                if (!perVertexSkinningMatrices.IsCreated)
                {
                    if (PerVertexSkinningMatricesArray == null) return default;

                    perVertexSkinningMatrices = new NativeList<float4x4>(perVertexSkinningMatricesArray.Length, Allocator.Persistent);
                    for (int a = 0; a < perVertexSkinningMatricesArray.Length; a++) perVertexSkinningMatrices.Add(perVertexSkinningMatricesArray[a]);
                }

                return perVertexSkinningMatrices;
            }
        }
        public NativeList<float4x4> PerVertexSkinningMatricesInverse
        {
            get
            {
                if (!perVertexSkinningMatricesInverse.IsCreated)
                {
                    if (PerVertexSkinningMatricesInverseArray == null) return default;

                    perVertexSkinningMatricesInverse = new NativeList<float4x4>(perVertexSkinningMatricesInverseArray.Length, Allocator.Persistent);
                    for (int a = 0; a < perVertexSkinningMatricesInverseArray.Length; a++) perVertexSkinningMatricesInverse.Add(perVertexSkinningMatricesInverseArray[a]);
                }

                return perVertexSkinningMatricesInverse;
            }
        }
        public void RefreshPerVertexSkinningMatrices(bool refreshBoneMatrices = true)
        {
            if (refreshBoneMatrices) RefreshBoneMatrices();
            if (BoneMatricesArray == null) return;

            if (perVertexSkinningMatricesArray == null) perVertexSkinningMatricesArray = new Matrix4x4[BonesPerVertex.Length];
            perVertexSkinningMatricesArray = RealtimeMesh.GetPerVertexSkinningMatrices(boneMatricesArray, BoneWeights.AsArray(), BonesPerVertex.AsArray(), perVertexSkinningMatricesArray);
            if (perVertexSkinningMatricesInverseArray == null) perVertexSkinningMatricesInverseArray = new Matrix4x4[perVertexSkinningMatricesArray.Length];
            for (int a = 0; a < perVertexSkinningMatricesArray.Length; a++) perVertexSkinningMatricesInverseArray[a] = perVertexSkinningMatricesArray[a].inverse;
             
            if (perVertexSkinningMatrices.IsCreated)
            {
                perVertexSkinningMatrices.Clear(); 
                for (int a = 0; a < perVertexSkinningMatricesArray.Length; a++) perVertexSkinningMatrices.Add(perVertexSkinningMatricesArray[a]);
            }
            if (perVertexSkinningMatricesInverse.IsCreated)
            {
                perVertexSkinningMatricesInverse.Clear();
                for (int a = 0; a < perVertexSkinningMatricesInverseArray.Length; a++) perVertexSkinningMatricesInverse.Add(perVertexSkinningMatricesInverseArray[a]); 
            }
        }

        protected SkinnedVertex[] skinnedVerticesArray;
        public SkinnedVertex[] SkinnedVerticesArray
        {
            get
            {
                if (skinnedVerticesArray == null)
                {
                    RefreshSkinnedVerticesArray();
                }

                return skinnedVerticesArray;
            }
        }
        public void RefreshSkinnedVerticesArray(bool refreshBoneMatrices = true)
        {
            if (refreshBoneMatrices) RefreshBoneMatrices();
            if (VerticesArray == null || PerVertexSkinningMatricesArray == null) return;

            if (skinnedVerticesArray == null) skinnedVerticesArray = new SkinnedVertex[verticesArray.Length];
            skinnedVerticesArray = MeshEditing.GetSkinnedVertexData(verticesArray, BoneWeights.AsArray(), BonesPerVertex.AsArray(), perVertexSkinningMatricesArray, skinnedVerticesArray);
        }
        protected NativeList<SkinnedVertex8Reference> skinnedVertices;
        public NativeList<SkinnedVertex8Reference> SkinnedVertices
        {
            get
            {
                if (!skinnedVertices.IsCreated)
                {
                    RefreshSkinnedVertices();
                }
                
                return skinnedVertices;
            }
        }
        public void RefreshSkinnedVertices(bool refreshBoneMatrices = true)
        {
            if (refreshBoneMatrices) RefreshBoneMatrices();
            if (VerticesArray == null || PerVertexSkinningMatricesArray == null) return; 

            if (!skinnedVertices.IsCreated) skinnedVertices = new NativeList<SkinnedVertex8Reference>(verticesArray.Length, Allocator.Persistent); 
            skinnedVertices.Clear();
            skinnedVertices = MeshEditing.GetSkinnedVertex8DataAsList(meshViewerSkinnedRenderer, verticesArray, BoneWeights.AsArray(), BonesPerVertex.AsArray(), perVertexSkinningMatricesArray, skinnedVertices);
        }
        
        protected MeshDataTools.MergedVertex[] mergedVerticesArray;
        public MeshDataTools.MergedVertex[] MergedVerticesArray
        {
            get
            {
                if (mergedVerticesArray == null)
                {
                    if (editedMesh == null) return null;
                    mergedVerticesArray = MeshDataTools.MergeVertices(VerticesArray);
                }
                
                return mergedVerticesArray;
            }
        }

        protected NativeList<int> mergedVertexIndices;
        protected NativeList<int> mergedVertexFirstIndices;
        protected NativeList<int> mergedVertexIndexCount;
        protected NativeList<int> mergedVertexStartIndices;
        public bool MergedVerticesAreInitialized => mergedVertexIndices.IsCreated;
        public void InitializeMergedVertices()
        {
            if (editedMesh == null) return;

            if (!mergedVertexIndices.IsCreated) mergedVertexIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!mergedVertexFirstIndices.IsCreated) mergedVertexFirstIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!mergedVertexIndexCount.IsCreated) mergedVertexIndexCount = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!mergedVertexStartIndices.IsCreated) mergedVertexStartIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);

            mergedVertexIndices.Clear();
            mergedVertexFirstIndices.Clear();
            mergedVertexIndexCount.Clear();
            mergedVertexStartIndices.Clear();

            var mergedVertices = MergedVerticesArray;

            int mergedVertexIndex = 0;
            for(int a = 0; a < mergedVertices.Length; a++)
            {
                var mergedVertex = mergedVertices[a];

                mergedVertexStartIndices.Add(mergedVertexIndex);
                mergedVertexFirstIndices.Add(mergedVertex.firstIndex); 
                int indexCount = mergedVertex.indices.Count;
                mergedVertexIndexCount.Add(indexCount);
                for(int b = 0; b < indexCount; b++)
                {
                    mergedVertexIndices.Add(mergedVertex.indices[b]); 
                }

                mergedVertexIndex = mergedVertexIndex + indexCount;
            }
        }

        protected MeshDataTools.WeightedVertexConnection[][] vertexConnectionArrays;
        public MeshDataTools.WeightedVertexConnection[][] VertexConnectionArrays
        {
            get
            {
                if (vertexConnectionArrays == null)
                {
                    if (editedMesh == null) return null;
                    vertexConnectionArrays = MeshDataTools.GetDistanceWeightedVertexConnections(TrianglesArray, VerticesArray, MergedVerticesArray);
                }

                return vertexConnectionArrays;
            }
        }
        protected NativeList<MeshDataTools.WeightedVertexConnection> vertexConnections;
        protected NativeList<int> vertexConnectionCounts;
        protected NativeList<int> vertexConnectionStartIndices;
        public bool VertexConnectionsAreInitialized => vertexConnections.IsCreated;
        public void InitializeVertexConnections()
        {
            if (editedMesh == null) return;

            if (!vertexConnections.IsCreated) vertexConnections = new NativeList<MeshDataTools.WeightedVertexConnection>(editedMesh.vertexCount, Allocator.Persistent);
            if (!vertexConnectionCounts.IsCreated) vertexConnectionCounts = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!vertexConnectionStartIndices.IsCreated) vertexConnectionStartIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);

            vertexConnections.Clear();
            vertexConnectionCounts.Clear();
            vertexConnectionStartIndices.Clear();

            var arrays = VertexConnectionArrays;

            int vertexConnectionIndex = 0;
            for (int a = 0; a < arrays.Length; a++)
            {
                var connections = arrays[a];

                vertexConnectionStartIndices.Add(vertexConnectionIndex);  
                int connectionCount = connections.Length;
                vertexConnectionCounts.Add(connectionCount);
                for (int b = 0; b < connections.Length; b++)
                {
                    var connection = connections[b];
                    vertexConnections.Add(connection);
                }

                vertexConnectionIndex = vertexConnectionIndex + connectionCount;
            }
        }

        protected MeshDataTools.Triangle[][] vertexTriangleArrays;
        public MeshDataTools.Triangle[][] VertexTriangleArrays
        {
            get
            {
                if (vertexTriangleArrays == null)
                {
                    if (editedMesh == null) return null;
                    vertexTriangleArrays = MeshDataTools.GetTriangleReferencesPerVertex(editedMesh.vertexCount, TrianglesArray, MergedVerticesArray);
                }

                return vertexTriangleArrays;
            }
        }
        protected NativeList<MeshDataTools.Triangle> vertexTriangles;
        protected NativeList<int> vertexTriangleCounts;
        protected NativeList<int> vertexTriangleStartIndices;
        public bool VertexTrianglesAreInitialized => vertexTriangles.IsCreated;
        public void InitializeVertexTriangles()
        {
            if (editedMesh == null) return;

            if (!vertexTriangles.IsCreated) vertexTriangles = new NativeList<MeshDataTools.Triangle>(editedMesh.vertexCount, Allocator.Persistent);
            if (!vertexTriangleCounts.IsCreated) vertexTriangleCounts = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);
            if (!vertexTriangleStartIndices.IsCreated) vertexTriangleStartIndices = new NativeList<int>(editedMesh.vertexCount, Allocator.Persistent);

            vertexTriangles.Clear();
            vertexTriangleCounts.Clear();
            vertexTriangleStartIndices.Clear();

            var arrays = VertexTriangleArrays;

            int vertexTriangleIndex = 0;
            for (int a = 0; a < arrays.Length; a++)
            {
                var triangles = arrays[a];

                vertexTriangleStartIndices.Add(vertexTriangleIndex);
                int triangleCount = triangles.Length;
                vertexTriangleCounts.Add(triangleCount);
                for (int b = 0; b < triangles.Length; b++)
                {
                    var triangle = triangles[b];
                    vertexTriangles.Add(triangle);
                }

                vertexTriangleIndex = vertexTriangleIndex + triangleCount;
            }
        }

        protected readonly List<BlendShape> editedBlendShapes = new List<BlendShape>();
        protected readonly List<List<NativeList<ShapeVertexDelta>[]>> savedBlendShapeStates = new List<List<NativeList<ShapeVertexDelta>[]>>(); 
        public int SaveBlendShapeState(int shapeIndex, int slot = -1)
        {
            if (editedMesh == null) return -1;

            int vertexCount = editedMesh.vertexCount;

            while (savedBlendShapeStates.Count < editedBlendShapes.Count) savedBlendShapeStates.Add(new List<NativeList<ShapeVertexDelta>[]>()); 

            var list = savedBlendShapeStates[shapeIndex];
            if (list == null)
            {
                list = new List<NativeList<ShapeVertexDelta>[]>();
                savedBlendShapeStates[shapeIndex] = list;
            }

            var blendShape = editedBlendShapes[shapeIndex];
            NativeList<ShapeVertexDelta>[] state;
            if (slot < 0 || slot >= list.Count)
            {
                slot = list.Count;
                state = new NativeList<ShapeVertexDelta>[blendShape.frames.Length];
                list.Add(state);
            } 
            else
            {
                state = list[slot];
            }

            int indexOffset = editedBlendShapeStartIndices[shapeIndex];
            for(int a = 0; a < state.Length; a++)
            {
                var frame = blendShape.frames[a];
                var frameData = state[a];
                if (!frameData.IsCreated) frameData = new NativeList<ShapeVertexDelta>(vertexCount, Allocator.Persistent);
                frameData.Clear();

                int indexStart = indexOffset + vertexCount * a;
                for (int b = 0; b < vertexCount; b++) frameData.Add(editedBlendShapeDeltas[indexStart + b]); 

                state[a] = frameData;
            }

            return slot;
        }
        public void SaveBlendShapeStates(ICollection<int> shapeIndices, int slot = -1)
        {
            foreach (var shapeIndex in shapeIndices) SaveBlendShapeState(shapeIndex, slot);
        }
        public void RestoreBlendShapeState(int shapeIndex, int slot, float mix)
        {
            if (editedMesh == null || shapeIndex < 0 || shapeIndex > savedBlendShapeStates.Count || !TargetVertexIndices.IsCreated) return;

            int vertexCount = editedMesh.vertexCount; 
            var list = savedBlendShapeStates[shapeIndex];
            if (list == null || slot < 0 || slot >= list.Count) return;

            var blendShape = editedBlendShapes[shapeIndex];
            NativeList<ShapeVertexDelta>[] state = list[slot];         

            int indexOffset = editedBlendShapeStartIndices[shapeIndex];
            for (int a = 0; a < state.Length; a++)
            {
                var frame = blendShape.frames[a];
                var frameData = state[a];
                if (!frameData.IsCreated) continue;

                int indexStart = indexOffset + vertexCount * a;
                for (int b = 0; b < targetVertexIndices.Length; b++) 
                {
                    int vertexIndex = targetVertexIndices[b];
                    int ind = indexStart + vertexIndex;
                    editedBlendShapeDeltas[ind] = ShapeVertexDelta.Lerp(editedBlendShapeDeltas[ind], frameData[vertexIndex], mix);
                }
            }
        }
        public void RestoreBlendShapeStates(ICollection<int> shapeIndices, int slot, float mix)
        {
            foreach (var shapeIndex in shapeIndices) RestoreBlendShapeState(shapeIndex, slot, mix);
        }

        public bool BlendShapeEditsAreInitialized => editedBlendShapeDeltas.IsCreated;
        protected NativeList<ShapeVertexDelta> editedBlendShapeDeltas;
        protected NativeList<ShapeVertexDelta> previousEditedBlendShapeDeltas;
        protected bool dirtyBlendShapeDeltas;
        public bool BlendShapeDeltasAreDirty => dirtyBlendShapeDeltas;
        public void MarkBlendShapeDeltasAsDirty()
        {
            dirtyBlendShapeDeltas = true;
        }
        public void CleanBlendShapeDeltas()
        {
            dirtyBlendShapeDeltas = false;

            if (!editedBlendShapeDeltas.IsCreated) return;
            if (!previousEditedBlendShapeDeltas.IsCreated) previousEditedBlendShapeDeltas = new NativeList<ShapeVertexDelta>(editedMesh.vertexCount, Allocator.Persistent);

            previousEditedBlendShapeDeltas.Clear();
            previousEditedBlendShapeDeltas.AddRange(editedBlendShapeDeltas.AsArray()); 
        }
        protected NativeList<int> editedBlendShapeFrameCounts;
        protected NativeList<int> editedBlendShapeStartIndices;

        public int BlendShapeCount => !editedBlendShapeFrameCounts.IsCreated ? 0 : editedBlendShapeFrameCounts.Length;
        public int GetBlendShapeFrameCount(int blendShapeIndex)
        {
            if (!editedBlendShapeFrameCounts.IsCreated || blendShapeIndex < 0 || blendShapeIndex >= editedBlendShapeFrameCounts.Length) return 0;
            return editedBlendShapeFrameCounts[blendShapeIndex];
        }

        public void InitializeBlendShapeEdits()
        {
            if (editedMesh == null) return;

            if (!editedBlendShapeDeltas.IsCreated) editedBlendShapeDeltas = new NativeList<ShapeVertexDelta>(editedMesh.vertexCount, Allocator.Persistent);
            if (!editedBlendShapeFrameCounts.IsCreated) editedBlendShapeFrameCounts = new NativeList<int>(0, Allocator.Persistent);
            if (!editedBlendShapeStartIndices.IsCreated) editedBlendShapeStartIndices = new NativeList<int>(0, Allocator.Persistent); 

            editedBlendShapeDeltas.Clear();
            editedBlendShapeFrameCounts.Clear();
            editedBlendShapeStartIndices.Clear();

            editedBlendShapes.Clear();
            editedMesh.GetBlendShapes(editedBlendShapes);
            
            for(int a = 0; a < editedBlendShapes.Count; a++)
            {
                editedBlendShapeStartIndices.Add(editedBlendShapeDeltas.Length);

                var shape = editedBlendShapes[a];

                int frameCount = shape.frames.Length;
                editedBlendShapeFrameCounts.Add(frameCount);

                for(int b = 0; b < frameCount; b++)
                {
                    var frame = shape.frames[b];

                    for(int c = 0; c < editedMesh.vertexCount; c++) 
                    {
                        editedBlendShapeDeltas.Add(new ShapeVertexDelta()
                        {
                            deltaPosition = frame.deltaVertices[c],
                            deltaNormal = frame.deltaNormals[c],
                            deltaTangent = frame.deltaTangents[c],
                        });
                    }
                }
            }

            CleanBlendShapeDeltas();
        }
        public void ApplyBlendShapeDataEdits()
        {
            if (editedMesh == null || !editedBlendShapeDeltas.IsCreated || !editedBlendShapeFrameCounts.IsCreated || !editedBlendShapeStartIndices.IsCreated) return;

            for (int shapeIndex = 0; shapeIndex < editedBlendShapeStartIndices.Length; shapeIndex++)
            {
                var shape = editedBlendShapes[shapeIndex];

                int shapeIndexStart = editedBlendShapeStartIndices[shapeIndex];
                int frameCount = editedBlendShapeFrameCounts[shapeIndex];
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var frame = shape.frames[frameIndex];

                    int frameIndexStart = shapeIndexStart + frameIndex * editedMesh.vertexCount;
                    for (int v = 0; v < editedMesh.vertexCount; v++)
                    {
                        var deltas = editedBlendShapeDeltas[frameIndexStart + v];

                        frame.deltaVertices[v] = deltas.deltaPosition;
                        frame.deltaNormals[v] = deltas.deltaNormal;
                        frame.deltaTangents[v] = deltas.deltaTangent;
                    }
                }
            }
        }
        public void RefreshMeshBlendShapes()
        {
            if (editedMesh == null) return;

            editedMesh.ClearBlendShapes();
            foreach(var shape in editedBlendShapes) shape.AddToMesh(editedMesh);
        }

        private JobHandle currentJobHandle;

        public JobHandle SmoothPreserveShapeDeltaVolumes(float factor, NativeList<float> mask, ICollection<int> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach(var targetShape in targetShapes)
            {
                var frameCount = editedBlendShapeFrameCounts[targetShape];
                for(int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = SmoothPreserveShapeDeltaVolume(factor, mask, targetShape, a, affectedVerticesOnly, inputDeps, false);
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle SmoothPreserveShapeDeltaVolumes(float factor, NativeList<float> mask, ICollection<int2> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                currentJobHandle = SmoothPreserveShapeDeltaVolume(factor, mask, targetShape.x, targetShape.y, affectedVerticesOnly, inputDeps, false);
                inputDeps = currentJobHandle;
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle SmoothPreserveShapeDeltaVolumes(float factor, NativeList<float> mask, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (!BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            for(int targetShape = 0; targetShape < editedBlendShapes.Count; targetShape++)
            {
                var frameCount = editedBlendShapeFrameCounts[targetShape];
                for (int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = SmoothPreserveShapeDeltaVolume(factor, mask, targetShape, a, affectedVerticesOnly, inputDeps, false); 
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle SmoothPreserveShapeDeltaVolume(float factor, NativeList<float> mask, int targetShapeIndex, int targetShapeFrame, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (editedMesh == null || !BlendShapeEditsAreInitialized) return default;

            if (!VertexConnectionsAreInitialized) InitializeVertexConnections();

            currentJobHandle.Complete();
            currentJobHandle = new MeshEditing.SmoothPreserveShapeDeltaVolumeJob()
            {
                mask = mask,
                targetBlendShapeIndex = targetShapeIndex,
                targetBlendShapeFrame = targetShapeFrame,
                vertexCount = editedMesh.vertexCount,
                factor = factor,
                affectedVerticesOnly = affectedVerticesOnly,
                localVertexIndices = targetVertexIndices,
                localVertexConnections = vertexConnections,
                localVertexConnectionCounts = vertexConnectionCounts,
                localVertexConnectionStartIndices = vertexConnectionStartIndices,
                originalLocalBlendShapeData = previousEditedBlendShapeDeltas,
                //localBlendShapeFrameCounts = editedBlendShapeFrameCounts,
                localBlendShapeStartIndices = editedBlendShapeStartIndices,
                editedLocalBlendShapeData = editedBlendShapeDeltas
            }.Schedule(targetVertexIndices.Length, 4, inputDeps);

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }

        public JobHandle PreserveShapeDeltaVolumes(float factor, NativeList<float> mask, ICollection<int> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                var frameCount = editedBlendShapeFrameCounts[targetShape];
                for (int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = PreserveShapeDeltaVolume(factor, mask, targetShape, a, affectedVerticesOnly, inputDeps, false);
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle PreserveShapeDeltaVolumes(float factor, NativeList<float> mask, ICollection<int2> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                currentJobHandle = PreserveShapeDeltaVolume(factor, mask, targetShape.x, targetShape.y, affectedVerticesOnly, inputDeps, false); 
                inputDeps = currentJobHandle;
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle PreserveShapeDeltaVolumes(float factor, NativeList<float> mask, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (!BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            for (int targetShape = 0; targetShape < editedBlendShapes.Count; targetShape++)
            {
                var frameCount = editedBlendShapeFrameCounts[targetShape];
                for (int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = PreserveShapeDeltaVolume(factor, mask, targetShape, a, affectedVerticesOnly, inputDeps, false);
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle PreserveShapeDeltaVolume(float factor, NativeList<float> mask, int targetShapeIndex, int targetShapeFrame, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true)
        {
            if (editedMesh == null || !BlendShapeEditsAreInitialized) return default;

            if (!VertexConnectionsAreInitialized) InitializeVertexConnections();

            currentJobHandle.Complete();
            currentJobHandle = new MeshEditing.PreserveShapeDeltaVolumeJob()
            {
                mask = mask,
                targetBlendShapeIndex = targetShapeIndex,
                targetBlendShapeFrame = targetShapeFrame,
                vertexCount = editedMesh.vertexCount,
                factor = factor,
                affectedVerticesOnly = affectedVerticesOnly,
                localVertexIndices = targetVertexIndices,
                localVertexData = BaseVertexData,
                localVertexConnections = vertexConnections,
                localVertexConnectionCounts = vertexConnectionCounts,
                localVertexConnectionStartIndices = vertexConnectionStartIndices,
                originalLocalBlendShapeData = previousEditedBlendShapeDeltas,
                //localBlendShapeFrameCounts = editedBlendShapeFrameCounts,
                localBlendShapeStartIndices = editedBlendShapeStartIndices,
                editedLocalBlendShapeData = editedBlendShapeDeltas
            }.Schedule(targetVertexIndices.Length, 4, inputDeps);

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }

        [Serializable]
        public struct DepenetrationTarget
        {
            public int2 indices;
            public int2 collisionIndices;
            public float factor;
            public float localCollisionFactor;
            public float thickness;
        }
        protected readonly List<DepenetrationTarget> currentDepenetrationShapeTargets = new List<DepenetrationTarget>();

        /// <summary>
        /// targetShapes.x -> local blend shape index
        /// targetShapes.y -> collision blend shape index
        /// </summary>
        public JobHandle DepenetrateShapeDeltas(float factor, NativeList<float> mask, float localCollisionFactor, float thickness, ICollection<int2> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true, bool appendToRepetitionList = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default; 

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                var frameCount = Mathf.Min(editedBlendShapeFrameCounts[targetShape.x], collisionBlendShapeFrameCounts[targetShape.y]);
                for (int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = DepenetrateShapeDeltas(factor, mask, localCollisionFactor, thickness, targetShape.x, a, targetShape.y, a, affectedVerticesOnly, inputDeps, false, appendToRepetitionList);
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        /// <summary>
        /// targetShapes.x -> local blend shape index
        /// targetShapes.y -> local blend shape frame index
        /// targetShapes.z -> collision blend shape index
        /// targetShapes.w -> collision blend shape frame index
        /// </summary>
        public JobHandle DepenetrateShapeDeltas(float factor, NativeList<float> mask, float localCollisionFactor, float thickness, ICollection<int4> targetShapes, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true, bool appendToRepetitionList = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                currentJobHandle = DepenetrateShapeDeltas(factor, mask, localCollisionFactor, thickness, targetShape.x, targetShape.y, targetShape.z, targetShape.w, affectedVerticesOnly, inputDeps, false, appendToRepetitionList);
                inputDeps = currentJobHandle;
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle DepenetrateShapeDeltas(ICollection<DepenetrationTarget> targetShapes, NativeList<float> mask, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true, bool appendToRepetitionList = true)
        {
            if (targetShapes == null || !BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            foreach (var targetShape in targetShapes)
            {
                currentJobHandle = DepenetrateShapeDeltas(targetShape.factor, mask, targetShape.localCollisionFactor, targetShape.thickness, targetShape.indices.x, targetShape.indices.y, targetShape.collisionIndices.x, targetShape.collisionIndices.y, affectedVerticesOnly, inputDeps, false, appendToRepetitionList);
                inputDeps = currentJobHandle;
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        public JobHandle DepenetrateShapeDeltas(float factor, NativeList<float> mask, float localCollisionFactor, float thickness, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true, bool appendToRepetitionList = true)
        {
            if (!BlendShapeEditsAreInitialized) return default;

            currentJobHandle.Complete();
            for (int targetShapeIndex = 0; targetShapeIndex < editedBlendShapes.Count; targetShapeIndex++)
            {
                var targetShape = editedBlendShapes[targetShapeIndex];
                var collisionShapeIndex = FindCollisionShapeIndex(targetShape.name);
                if (collisionShapeIndex < 0) continue;

                var frameCount = Mathf.Min(editedBlendShapeFrameCounts[targetShapeIndex], collisionBlendShapeFrameCounts[collisionShapeIndex]); 
                for (int a = 0; a < frameCount; a++)
                {
                    currentJobHandle = DepenetrateShapeDeltas(factor, mask, localCollisionFactor, thickness, targetShapeIndex, a, collisionShapeIndex, a, affectedVerticesOnly, inputDeps, false, appendToRepetitionList);
                    inputDeps = currentJobHandle;
                }
            }

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            return currentJobHandle;
        }
        unsafe public JobHandle DepenetrateShapeDeltas(float factor, NativeList<float> mask, float localCollisionFactor, float thickness, int targetShapeIndex, int targetShapeFrame, int targetCollisionShapeIndex, int targetCollisionShapeFrame, bool affectedVerticesOnly, JobHandle inputDeps = default, bool appendCleaningJob = true, bool appendToRepetitionList = true)
        {
            if (editedMesh == null || !BlendShapeEditsAreInitialized || !HasCollisionData) return default;
            
            if (!VertexTrianglesAreInitialized) InitializeVertexTriangles();

            currentJobHandle.Complete();
            currentJobHandle = new MeshEditing.DepenetrateShapeDeltaJob()
            {
                mask = mask,

                //penetrationCounter = penetrationCounter,
                depenetrationMask = DepenetrationMask,
                targetBlendShapeIndex = targetShapeIndex,
                targetBlendShapeFrame = targetShapeFrame,
                vertexCount = editedMesh.vertexCount,

                targetCollisionBlendShapeIndex = targetCollisionShapeIndex,
                targetCollisionBlendShapeFrame = targetCollisionShapeFrame,
                collisionVertexCount = collisionVertexData.Length,

                factor = factor,
                affectedVerticesOnly = affectedVerticesOnly,
                localCollisionFactor = localCollisionFactor,
                thickness = thickness,

                collisionTriangles = collisionTriangles,
                collisionVertexData = collisionVertexData,
                collisionVertexSkinning = collisionVertexSkinning,

                collisionBlendShapeData = collisionBlendShapeData,
                //collisionBlendShapeFrameCounts = collisionBlendShapeFrameCounts,
                collisionBlendShapeStartIndices = collisionBlendShapeStartIndices,

                nearbyCollisionTriangles = nearbyCollisionTriangles,
                nearbyCollisionTrianglesCounts = nearbyCollisionTriangleCounts,
                nearbyCollisionTrianglesStartIndices = nearbyCollisionTriangleStartIndices,

                localVertexIndices = targetVertexIndices,
                localVertexData = BaseVertexData,
                localVertexSkinning = PerVertexSkinningMatrices,
                localVertexSkinningInverse = PerVertexSkinningMatricesInverse,
                localVertexTriangles = vertexTriangles,
                localVertexTriangleCounts = vertexTriangleCounts,
                localVertexTriangleStartIndices = vertexTriangleStartIndices,
                originalLocalBlendShapeData = previousEditedBlendShapeDeltas,
                //localBlendShapeFrameCounts = editedBlendShapeFrameCounts,
                localBlendShapeStartIndices = editedBlendShapeStartIndices,
                editedLocalBlendShapeData = editedBlendShapeDeltas
            }.Schedule(targetVertexIndices.Length, 1, inputDeps); 

            if (appendCleaningJob) currentJobHandle = new CopyListJob<ShapeVertexDelta>() { listA = editedBlendShapeDeltas, listB = previousEditedBlendShapeDeltas }.Schedule(currentJobHandle);

            if (appendToRepetitionList) currentDepenetrationShapeTargets.Add(new DepenetrationTarget() { indices = new int2(targetShapeIndex, targetShapeFrame), collisionIndices = new int2(targetCollisionShapeIndex, targetCollisionShapeFrame), factor = factor, thickness = thickness });

            return currentJobHandle;
        }

        unsafe protected void RequeueDepenetrationJobsIfRequired()
        {
            currentJobHandle.Complete();
            /*if (penetrationCounter.Counter != null && *(penetrationCounter.Counter) > 0)
            {
                penetrationCounter.Reset(0);
                if (currentDepenetrationShapeTargets.Count > 0)
                {
                    DepenetrateShapeDeltas(currentDepenetrationShapeTargets, default, true, false);
                }
            } 
            else */if (currentDepenetrationShapeTargets.Count > 0)
            {
                currentDepenetrationShapeTargets.Clear(); 
                ApplyBlendShapeDataEdits();
                RefreshMeshBlendShapes();
            }
        }

        protected void Update()
        {
            RequeueDepenetrationJobsIfRequired();
        }

        protected void LateUpdate()
        {
            currentJobHandle.Complete();
            currentJobHandle = default;

            if (BlendShapeDeltasAreDirty) CleanBlendShapeDeltas();
        }

        protected struct CopyArrayJob<T> : IJob where T : struct
        {
            public NativeArray<T> arrayA, arrayB;

            public void Execute()
            {
                arrayB.CopyFrom(arrayA);
            }
        }
        protected struct CopyListJob<T> : IJob where T : unmanaged
        {
            public NativeList<T> listA, listB;

            public void Execute()
            {
                listB.CopyFrom(listA);
            }
        }
    }

    public static class MeshEditing
    {
        public static SkinnedVertex[] GetSkinnedVertexData(SkinnedMeshRenderer smr, Vector3[] verts, SkinnedVertex[] outputArray = null)
        {
            var skinning = RealtimeMesh.GetPerVertexSkinningMatrices(smr);
            return GetSkinnedVertexData(smr.sharedMesh, verts, skinning, outputArray);
        }
        public static SkinnedVertex[] GetSkinnedVertexData(SkinnedMeshRenderer smr, Matrix4x4[] perVertexSkinning, SkinnedVertex[] outputArray = null)
        {
            var verts = smr.sharedMesh.vertices;
            return GetSkinnedVertexData(smr.sharedMesh, verts, perVertexSkinning, outputArray);
        }
        public static SkinnedVertex[] GetSkinnedVertexData(SkinnedMeshRenderer smr, SkinnedVertex[] outputArray = null)
        {
            var verts = smr.sharedMesh.vertices;
            var skinning = RealtimeMesh.GetPerVertexSkinningMatrices(smr);
            return GetSkinnedVertexData(smr.sharedMesh, verts, skinning, outputArray);
        }
        public static SkinnedVertex[] GetSkinnedVertexData(Mesh mesh, Vector3[] verts, Matrix4x4[] perVertexSkinning, SkinnedVertex[] outputArray = null)
        {
            using (var boneWeights = mesh.GetAllBoneWeights())
            {
                using (var boneCounts = mesh.GetBonesPerVertex())
                {
                    return GetSkinnedVertexData(verts, boneWeights, boneCounts, perVertexSkinning, outputArray);
                }
            }
        }
        public static SkinnedVertex[] GetSkinnedVertexData(Vector3[] verts, NativeArray<BoneWeight1> boneWeights, NativeArray<byte> boneCounts, Matrix4x4[] perVertexSkinning, SkinnedVertex[] outputArray = null)
        {
            if (outputArray == null) outputArray = new SkinnedVertex[verts.Length];

            int boneWeightIndex = 0;
            for (int a = 0; a < verts.Length; a++)
            {
                var skinnedData = new SkinnedVertex() { skinningMatrix = perVertexSkinning[a], worldPosition = perVertexSkinning[a].MultiplyPoint(verts[a]) };

                var boneWeightCount = boneCounts[a];
                skinnedData.boneWeights = new VertexBoneWeight[boneWeightCount];
                for (int b = 0; b < boneWeightCount; b++) skinnedData.boneWeights[b] = new VertexBoneWeight() { boneIndex = boneWeights[boneWeightIndex + b].boneIndex, weight = boneWeights[boneWeightIndex + b].weight };
                boneWeightIndex += boneWeightCount;

                outputArray[a] = skinnedData;
            }

            return outputArray;
        }

        private delegate void AddSkinnedVertex8DataElementToNativeCollection(int index, SkinnedVertex8Reference data);
        private static void AddSkinnedVertex8DataToNativeCollection(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, Matrix4x4[] perVertexSkinning, AddSkinnedVertex8DataElementToNativeCollection setElement)
        {
            using (var boneWeights = smr.sharedMesh.GetAllBoneWeights())
            {
                using (var boneCounts = smr.sharedMesh.GetBonesPerVertex())
                {
                    AddSkinnedVertex8DataToNativeCollection(smr, vertices, boneWeights, boneCounts, perVertexSkinning, setElement);
                }
            }
        }
        private static void AddSkinnedVertex8DataToNativeCollection(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, NativeArray<BoneWeight1> boneWeights, NativeArray<byte> boneCounts, Matrix4x4[] perVertexSkinning, AddSkinnedVertex8DataElementToNativeCollection setElement)
        {
            if (vertices == null) vertices = smr.sharedMesh.vertices;
            if (perVertexSkinning == null) perVertexSkinning = RealtimeMesh.GetPerVertexSkinningMatrices(smr.bones, smr.sharedMesh.bindposes, boneWeights, boneCounts);

            int boneWeightIndex = 0;
            int vIndex = 0;
            foreach (var vert in vertices)
            {
                var skinnedData = new SkinnedVertex8() { skinningMatrix = perVertexSkinning[vIndex], worldPosition = perVertexSkinning[vIndex].MultiplyPoint(vert) };

                var boneWeightCount = boneCounts[vIndex];
                var localBoneWeights = VertexBoneWeight8.Empty;

                if (boneWeightCount > 0)
                {
                    localBoneWeights.boneIndex.x = boneWeights[boneWeightIndex].boneIndex;
                    localBoneWeights.weight.x = boneWeights[boneWeightIndex].weight;
                }
                if (boneWeightCount > 1)
                {
                    localBoneWeights.boneIndex.y = boneWeights[boneWeightIndex + 1].boneIndex;
                    localBoneWeights.weight.y = boneWeights[boneWeightIndex + 1].weight;
                }
                if (boneWeightCount > 2)
                {
                    localBoneWeights.boneIndex.z = boneWeights[boneWeightIndex + 2].boneIndex;
                    localBoneWeights.weight.z = boneWeights[boneWeightIndex + 2].weight;
                }
                if (boneWeightCount > 3)
                {
                    localBoneWeights.boneIndex.w = boneWeights[boneWeightIndex + 3].boneIndex;
                    localBoneWeights.weight.w = boneWeights[boneWeightIndex + 3].weight;
                }

                if (boneWeightCount > 4)
                {
                    localBoneWeights.boneIndex2.x = boneWeights[boneWeightIndex + 4].boneIndex;
                    localBoneWeights.weight2.x = boneWeights[boneWeightIndex + 4].weight;
                }
                if (boneWeightCount > 5)
                {
                    localBoneWeights.boneIndex2.y = boneWeights[boneWeightIndex + 5].boneIndex;
                    localBoneWeights.weight2.y = boneWeights[boneWeightIndex + 5].weight;
                }
                if (boneWeightCount > 6)
                {
                    localBoneWeights.boneIndex2.z = boneWeights[boneWeightIndex + 6].boneIndex;
                    localBoneWeights.weight2.z = boneWeights[boneWeightIndex + 6].weight;
                }
                if (boneWeightCount > 7)
                {
                    localBoneWeights.boneIndex2.w = boneWeights[boneWeightIndex + 7].boneIndex;
                    localBoneWeights.weight2.w = boneWeights[boneWeightIndex + 7].weight;
                }

                skinnedData.boneWeights = localBoneWeights;
                boneWeightIndex += boneWeightCount;

                setElement(vIndex, new SkinnedVertex8Reference() { vertexIndex = vIndex, vertex = skinnedData });
                vIndex++;
            }
        }
        public static NativeArray<SkinnedVertex8Reference> GetSkinnedVertex8Data(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, NativeArray<BoneWeight1> boneWeights, NativeArray<byte> boneCounts, Matrix4x4[] perVertexSkinning, NativeArray<SkinnedVertex8Reference> outputArray = default)
        {
            if (!outputArray.IsCreated) outputArray = new NativeArray<SkinnedVertex8Reference>(smr.sharedMesh.vertexCount, Allocator.Persistent);
            AddSkinnedVertex8DataToNativeCollection(smr, vertices, boneWeights, boneCounts, perVertexSkinning, (int index, SkinnedVertex8Reference data) => outputArray[index] = data);
            return outputArray;
        }
        public static NativeArray<SkinnedVertex8Reference> GetSkinnedVertex8Data(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, Matrix4x4[] perVertexSkinning, NativeArray<SkinnedVertex8Reference> outputArray = default)
        {
            if (!outputArray.IsCreated) outputArray = new NativeArray<SkinnedVertex8Reference>(smr.sharedMesh.vertexCount, Allocator.Persistent);
            AddSkinnedVertex8DataToNativeCollection(smr, vertices, perVertexSkinning, (int index, SkinnedVertex8Reference data) => outputArray[index] = data);
            return outputArray;
        }
        public static NativeArray<SkinnedVertex8Reference> GetSkinnedVertex8Data(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, NativeArray<SkinnedVertex8Reference> outputArray = default) => GetSkinnedVertex8Data(smr, vertices, null, outputArray);
        public static NativeArray<SkinnedVertex8Reference> GetSkinnedVertex8Data(SkinnedMeshRenderer smr, Matrix4x4[] perVertexSkinning, NativeArray<SkinnedVertex8Reference> outputArray = default) => GetSkinnedVertex8Data(smr, null, perVertexSkinning, outputArray);
        public static NativeArray<SkinnedVertex8Reference> GetSkinnedVertex8Data(SkinnedMeshRenderer smr, NativeArray<SkinnedVertex8Reference> outputArray = default) => GetSkinnedVertex8Data(smr, null, null, outputArray);
        public static NativeList<SkinnedVertex8Reference> GetSkinnedVertex8DataAsList(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, NativeArray<BoneWeight1> boneWeights, NativeArray<byte> boneCounts, Matrix4x4[] perVertexSkinning, NativeList<SkinnedVertex8Reference> outputList = default)
        {
            if (!outputList.IsCreated) outputList = new NativeList<SkinnedVertex8Reference>(smr.sharedMesh.vertexCount, Allocator.Persistent);
            AddSkinnedVertex8DataToNativeCollection(smr, vertices, boneWeights, boneCounts, perVertexSkinning, (int index, SkinnedVertex8Reference data) => outputList.Add(data));
            return outputList;
        }
        public static NativeList<SkinnedVertex8Reference> GetSkinnedVertex8DataAsList(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, Matrix4x4[] perVertexSkinning, NativeList<SkinnedVertex8Reference> outputList = default)
        {
            if (!outputList.IsCreated) outputList = new NativeList<SkinnedVertex8Reference>(smr.sharedMesh.vertexCount, Allocator.Persistent);
            AddSkinnedVertex8DataToNativeCollection(smr, vertices, perVertexSkinning, (int index, SkinnedVertex8Reference data) => outputList.Add(data));
            return outputList;
        }
        public static NativeList<SkinnedVertex8Reference> GetSkinnedVertex8DataAsList(SkinnedMeshRenderer smr, ICollection<Vector3> vertices, NativeList<SkinnedVertex8Reference> outputList = default) => GetSkinnedVertex8DataAsList(smr, vertices, null, outputList);
        public static NativeList<SkinnedVertex8Reference> GetSkinnedVertex8DataAsList(SkinnedMeshRenderer smr, Matrix4x4[] perVertexSkinning, NativeList<SkinnedVertex8Reference> outputList = default) => GetSkinnedVertex8DataAsList(smr, null, perVertexSkinning, outputList);
        public static NativeList<SkinnedVertex8Reference> GetSkinnedVertex8DataAsList(SkinnedMeshRenderer smr, NativeList<SkinnedVertex8Reference> outputList = default) => GetSkinnedVertex8DataAsList(smr, null, null, outputList);

        [Serializable]
        public struct ScoredVertex
        {
            public SkinnedVertex cachedVertex;
            public VertexInfluence influence;
        }
        private static readonly List<ScoredVertex> scoredData = new List<ScoredVertex>();
        public static List<VertexInfluence2> CalculateVertexInfluence2(ICollection<SkinnedVertex> clothingWorldVertices, ICollection<SkinnedVertex> referenceWorldVertices, ICollection<MeshDataTools.MergedVertex> referenceMergedVertices, List<VertexInfluence2> outputList = null, float maxDistance = 1) => CalculateVertexInfluence2(clothingWorldVertices, new ICollection<SkinnedVertex>[] { referenceWorldVertices }, new ICollection<MeshDataTools.MergedVertex>[] { referenceMergedVertices }, outputList, maxDistance);
        public static List<VertexInfluence2> CalculateVertexInfluence2(ICollection<SkinnedVertex> clothingWorldVertices, ICollection<ICollection<SkinnedVertex>> referenceWorldVertices, ICollection<ICollection<MeshDataTools.MergedVertex>> referenceMergedVertices, List<VertexInfluence2> outputList = null, float maxDistance = 1, float distanceBindingWeight = 0.2f)
        {
            if (outputList == null) outputList = new List<VertexInfluence2>();

            int vIndex = 0;
            foreach (var clothingVert in clothingWorldVertices)
            {
                scoredData.Clear();
                int referenceIndex = 0;

                using (var enu1 = referenceWorldVertices.GetEnumerator())
                {
                    using (var enu2 = referenceMergedVertices.GetEnumerator())
                    {
                        while (enu1.MoveNext() && enu2.MoveNext())
                        {
                            var reference = enu1.Current;
                            var referenceMerged = enu2.Current;

                            using (var enu3 = reference.GetEnumerator())
                            {
                                using (var enu4 = referenceMerged.GetEnumerator())
                                {
                                    int vIndex2 = 0;
                                    while (enu3.MoveNext() && enu4.MoveNext())
                                    {
                                        if (enu4.Current.firstIndex == vIndex2) // only consider the first vertex index of a merged vertex
                                        {
                                            var referenceVert = enu3.Current;

                                            float distance = Vector3.Distance(clothingVert.worldPosition, referenceVert.worldPosition);
                                            if (distance <= maxDistance)
                                            {
                                                scoredData.Add(new ScoredVertex() { cachedVertex = referenceVert, influence = new VertexInfluence() { meshIndex = referenceIndex, vertexIndex = vIndex2, weight = distance, score = distance } });
                                            }
                                        }

                                        vIndex2++;
                                    }
                                }
                            }

                            referenceIndex++;
                        }
                    }
                }

                for (int a = 0; a < scoredData.Count; a++)
                {
                    var data = scoredData[a];
                    var inf = data.influence;
                    inf.weight = inf.score = clothingVert.ComparisonScore(data.cachedVertex, data.influence.score, distanceBindingWeight);
                    data.influence = inf;
                    scoredData[a] = data;
                }
                
                var scoreOrderedData = scoredData.OrderByDescending(x => x.influence.weight);

                var influence = new VertexInfluence2();
                influence.influenceA = influence.influenceB = new VertexInfluence() { meshIndex = -1, vertexIndex = -1, weight = 0 };

                using (var enu = scoreOrderedData.GetEnumerator())
                {
                    if (enu.MoveNext()) influence.influenceA = enu.Current.influence;
                    if (enu.MoveNext()) influence.influenceB = enu.Current.influence;
                }

                float totalWeight = influence.influenceA.weight + influence.influenceB.weight;
                if (totalWeight > 0)
                {
                    influence.influenceA.weight = influence.influenceA.weight / totalWeight;
                    influence.influenceB.weight = influence.influenceB.weight / totalWeight;
                }

                outputList.Add(influence);
                vIndex++;
            }

            return outputList;
        }

        [BurstCompile]
        public struct CalculateVertexInfluence2Job : IJobParallelFor
        {

            public float maxDistance;

            public float distanceBindingWeight;

            public int referenceIndex;

            [ReadOnly]
            public NativeList<float> mask;

            [ReadOnly]
            public NativeList<SkinnedVertex8Reference> referenceSkinnedVertices;
            [ReadOnly]
            public NativeList<int> referenceVertexIndices;

            [ReadOnly]
            public NativeList<SkinnedVertex8Reference> localSkinnedVertices;
            [ReadOnly]
            public NativeList<int> localVertexIndices;

            [NativeDisableParallelForRestriction]
            public NativeList<VertexInfluence2> influences;

            public void Execute(int index)
            {
                int localIndex = localVertexIndices[index];
                SkinnedVertex8Reference localVertex = localSkinnedVertices[localIndex];

                VertexInfluence2 influence = influences[localIndex];
                VertexInfluence infA = influence.influenceA;
                VertexInfluence infB = influence.influenceB;

                for (int a = 0; a < referenceVertexIndices.Length; a++)
                {
                    var refVertIndex = referenceVertexIndices[a];
                    var refVertex = referenceSkinnedVertices[refVertIndex];

                    float distance = math.distance(localVertex.vertex.worldPosition, refVertex.vertex.worldPosition);
                    if (distance > maxDistance) continue; 

                    float score = localVertex.vertex.ComparisonScore(refVertex.vertex, distance, distanceBindingWeight);
                    if (score > infA.score)
                    {
                        if (refVertIndex != infB.vertexIndex || referenceIndex != infB.meshIndex) // prevent merged vertices from being chosen multiple times
                        {
                            if (infA.score > infB.score)
                            {
                                infB.score = infA.score;
                                infB.meshIndex = infA.meshIndex;
                                infB.vertexIndex = infA.vertexIndex;
                                infB.weight = infA.weight;
                            }

                            infA.score = score;
                            infA.meshIndex = referenceIndex;
                            infA.vertexIndex = refVertIndex;
                            infA.weight = 0;
                        }
                    } 
                    else if (score > infB.score && (refVertIndex != infA.vertexIndex || referenceIndex != infA.meshIndex)) // prevent merged vertices from being chosen multiple times
                    {
                        infB.score = score;
                        infB.meshIndex = referenceIndex;
                        infB.vertexIndex = refVertIndex;
                        infB.weight = 0;
                    }
                }

                float totalScore = infA.score + infB.score;
                infA.weight = math.select(0, infA.score / totalScore, totalScore > 0) * mask[localIndex];
                infB.weight = math.select(0, infB.score / totalScore, totalScore > 0) * mask[localIndex];
                influence.influenceA = infA;
                influence.influenceB = infB;
                influences[localIndex] = influence;
            }
        }

        [BurstCompile]
        public struct SmoothPreserveShapeDeltaVolumeJob : IJobParallelFor
        {

            public int targetBlendShapeIndex;
            public int targetBlendShapeFrame;
            public int vertexCount;

            public float factor;

            public bool affectedVerticesOnly;

            [ReadOnly]
            public NativeList<float> mask;

            [ReadOnly]
            public NativeList<int> localVertexIndices;

            [ReadOnly]
            public NativeList<MeshDataTools.WeightedVertexConnection> localVertexConnections;
            [ReadOnly]
            public NativeList<int> localVertexConnectionCounts;
            [ReadOnly]
            public NativeList<int> localVertexConnectionStartIndices;
            
            [ReadOnly]
            public NativeList<ShapeVertexDelta> originalLocalBlendShapeData;
            //[ReadOnly]
            //public NativeList<int> localBlendShapeFrameCounts;
            [ReadOnly]
            public NativeList<int> localBlendShapeStartIndices;

            [NativeDisableParallelForRestriction]
            public NativeList<ShapeVertexDelta> editedLocalBlendShapeData;

            public void Execute(int index)
            {
                int localIndex = localVertexIndices[index];

                int vertexConnectionsIndex = localVertexConnectionStartIndices[localIndex];
                int vertexConnectionCount = localVertexConnectionCounts[localIndex];

                int shapeIndex = localBlendShapeStartIndices[targetBlendShapeIndex] + targetBlendShapeFrame * vertexCount;

                int shapeRelativeIndex = shapeIndex + localIndex;
                var finalShapeDeltas = originalLocalBlendShapeData[shapeRelativeIndex];

                float maskWeight = mask[localIndex];
                if (maskWeight <= 0f || (affectedVerticesOnly && math.length(finalShapeDeltas.deltaPosition) < 0.0001f)) return;

                var editedShapeDeltas = new ShapeVertexDelta();
                float totalWeight = 0f;
                for (int a = 0; a < vertexConnectionCount; a++)
                {
                    var connection = localVertexConnections[vertexConnectionsIndex + a];

                    int shapeRelativeConnectionIndex = shapeIndex + connection.index;
                    var shapeDeltas = originalLocalBlendShapeData[shapeRelativeConnectionIndex];

                    float connectionMaskWeight = mask[connection.index];
                    if (connectionMaskWeight <= 0f || (affectedVerticesOnly && math.length(shapeDeltas.deltaPosition) < 0.0001f)) continue;

                    float connectionWeight = connection.weight * connectionMaskWeight;
                    editedShapeDeltas.deltaPosition = editedShapeDeltas.deltaPosition + shapeDeltas.deltaPosition * connectionWeight;
                    editedShapeDeltas.deltaNormal = editedShapeDeltas.deltaNormal + shapeDeltas.deltaNormal * connectionWeight;
                    editedShapeDeltas.deltaTangent = editedShapeDeltas.deltaTangent + shapeDeltas.deltaTangent * connectionWeight;

                    totalWeight += connectionWeight;
                }
                totalWeight = 1f - math.min(1f, totalWeight);
                editedShapeDeltas.deltaPosition = editedShapeDeltas.deltaPosition + finalShapeDeltas.deltaPosition * totalWeight;
                editedShapeDeltas.deltaNormal = editedShapeDeltas.deltaNormal + finalShapeDeltas.deltaNormal * totalWeight;
                editedShapeDeltas.deltaTangent = editedShapeDeltas.deltaTangent + finalShapeDeltas.deltaTangent * totalWeight;

                float mix = factor * maskWeight;
                finalShapeDeltas.deltaPosition = math.lerp(finalShapeDeltas.deltaPosition, editedShapeDeltas.deltaPosition, mix);
                finalShapeDeltas.deltaNormal = math.lerp(finalShapeDeltas.deltaNormal, editedShapeDeltas.deltaNormal, mix);
                finalShapeDeltas.deltaTangent = math.lerp(finalShapeDeltas.deltaTangent, editedShapeDeltas.deltaTangent, mix); 

                editedLocalBlendShapeData[shapeRelativeIndex] = finalShapeDeltas;
            }
        }

        [BurstCompile]
        public struct PreserveShapeDeltaVolumeJob : IJobParallelFor
        {

            public int targetBlendShapeIndex;
            public int targetBlendShapeFrame;
            public int vertexCount;

            public float factor;

            public bool affectedVerticesOnly;

            [ReadOnly]
            public NativeList<float> mask;

            [ReadOnly]
            public NativeList<int> localVertexIndices;

            [ReadOnly]
            public NativeList<BaseVertexData> localVertexData;

            [ReadOnly]
            public NativeList<MeshDataTools.WeightedVertexConnection> localVertexConnections;
            [ReadOnly]
            public NativeList<int> localVertexConnectionCounts;
            [ReadOnly]
            public NativeList<int> localVertexConnectionStartIndices;

            [ReadOnly]
            public NativeList<ShapeVertexDelta> originalLocalBlendShapeData;
            //[ReadOnly]
            //public NativeList<int> localBlendShapeFrameCounts;
            [ReadOnly]
            public NativeList<int> localBlendShapeStartIndices;

            [NativeDisableParallelForRestriction]
            public NativeList<ShapeVertexDelta> editedLocalBlendShapeData;

            public void Execute(int index)
            {
                int localIndex = localVertexIndices[index];

                var localBaseVertex = localVertexData[localIndex];

                int vertexConnectionsIndex = localVertexConnectionStartIndices[localIndex];
                int vertexConnectionCount = localVertexConnectionCounts[localIndex];

                int shapeIndex = localBlendShapeStartIndices[targetBlendShapeIndex] + targetBlendShapeFrame * vertexCount;

                int shapeRelativeIndex = shapeIndex + localIndex;
                var finalShapeDeltas = originalLocalBlendShapeData[shapeRelativeIndex];

                float maskWeight = mask[localIndex];
                if (maskWeight <= 0f || (affectedVerticesOnly && math.length(finalShapeDeltas.deltaPosition) < 0.0001f)) return;

                var localShapeVertex = localBaseVertex + finalShapeDeltas;

                var editedShapeDeltas = new ShapeVertexDelta();
                float totalWeight = 0f;
                for (int a = 0; a < vertexConnectionCount; a++)
                {
                    var connection = localVertexConnections[vertexConnectionsIndex + a];

                    var baseVertex = localVertexData[connection.index];

                    int shapeRelativeConnectionIndex = shapeIndex + connection.index;
                    var shapeDeltas = originalLocalBlendShapeData[shapeRelativeConnectionIndex];

                    float connectionMaskWeight = mask[connection.index];
                    if (connectionMaskWeight <= 0f || (affectedVerticesOnly && math.length(shapeDeltas.deltaPosition) < 0.0001f)) continue;

                    var shapeVertex = baseVertex + shapeDeltas;

                    var originalOffsetPos = localBaseVertex.position - baseVertex.position;
                    var originalOffsetNorm = Maths.FromToRotation(baseVertex.normal, localBaseVertex.normal);
                    var originalOffsetTan = Maths.FromToRotation(baseVertex.tangent.xyz, localBaseVertex.tangent.xyz);

                    var shapeOffsetRot = Maths.FromToRotation(baseVertex.normal, shapeVertex.normal);

                    shapeVertex.position = shapeVertex.position + math.rotate(shapeOffsetRot, originalOffsetPos);
                    shapeVertex.normal = math.rotate(originalOffsetNorm, shapeVertex.normal);
                    shapeVertex.tangent.xyz = math.rotate(originalOffsetTan, shapeVertex.tangent.xyz);

                    float connectionWeight = connection.weight * connectionMaskWeight;
                    editedShapeDeltas.deltaPosition = editedShapeDeltas.deltaPosition + (shapeVertex.position - localBaseVertex.position) * connectionWeight;
                    editedShapeDeltas.deltaNormal = editedShapeDeltas.deltaNormal + (shapeVertex.normal - localBaseVertex.normal) * connectionWeight;
                    editedShapeDeltas.deltaTangent = editedShapeDeltas.deltaTangent + (shapeVertex.tangent.xyz - localBaseVertex.tangent.xyz) * connectionWeight;

                    totalWeight += connectionWeight;
                }
                totalWeight = 1f - math.min(1f, totalWeight);
                editedShapeDeltas.deltaPosition = editedShapeDeltas.deltaPosition + finalShapeDeltas.deltaPosition * totalWeight;
                editedShapeDeltas.deltaNormal = editedShapeDeltas.deltaNormal + finalShapeDeltas.deltaNormal * totalWeight;
                editedShapeDeltas.deltaTangent = editedShapeDeltas.deltaTangent + finalShapeDeltas.deltaTangent * totalWeight;

                float mix = factor * maskWeight;
                finalShapeDeltas.deltaPosition = math.lerp(finalShapeDeltas.deltaPosition, editedShapeDeltas.deltaPosition, factor * mix);
                finalShapeDeltas.deltaNormal = math.lerp(finalShapeDeltas.deltaNormal, editedShapeDeltas.deltaNormal, factor * mix);
                finalShapeDeltas.deltaTangent = math.lerp(finalShapeDeltas.deltaTangent, editedShapeDeltas.deltaTangent, factor * mix); 

                editedLocalBlendShapeData[shapeRelativeIndex] = finalShapeDeltas;
            }
        }

        [BurstCompile]
        public struct DepenetrateShapeDeltaJob : IJobParallelFor
        {

            public int targetBlendShapeIndex;
            public int targetBlendShapeFrame;
            public int vertexCount;

            public int targetCollisionBlendShapeIndex;
            public int targetCollisionBlendShapeFrame;
            public int collisionVertexCount;

            public float factor;
            public float localCollisionFactor;
            public float thickness;

            public bool affectedVerticesOnly;

            [ReadOnly]
            public NativeList<float> mask;

            [ReadOnly]
            public NativeList<MeshDataTools.Triangle> collisionTriangles;
            [ReadOnly]
            public NativeList<BaseVertexData> collisionVertexData;
            [ReadOnly]
            public NativeList<float4x4> collisionVertexSkinning;
            [ReadOnly]
            public NativeList<ShapeVertexDelta> collisionBlendShapeData;
            //[ReadOnly]
            //public NativeList<int> collisionBlendShapeFrameCounts;
            [ReadOnly]
            public NativeList<int> collisionBlendShapeStartIndices;

            [ReadOnly]
            public NativeList<int> nearbyCollisionTriangles;
            [ReadOnly]
            public NativeList<int> nearbyCollisionTrianglesCounts;
            [ReadOnly]
            public NativeList<int> nearbyCollisionTrianglesStartIndices;

            [ReadOnly]
            public NativeList<int> localVertexIndices;

            [ReadOnly]
            public NativeList<BaseVertexData> localVertexData;
            [ReadOnly]
            public NativeList<float4x4> localVertexSkinning;
            [ReadOnly]
            public NativeList<float4x4> localVertexSkinningInverse;

            [ReadOnly]
            public NativeList<MeshDataTools.Triangle> localVertexTriangles;
            [ReadOnly]
            public NativeList<int> localVertexTriangleCounts;
            [ReadOnly]
            public NativeList<int> localVertexTriangleStartIndices;

            [ReadOnly]
            public NativeList<ShapeVertexDelta> originalLocalBlendShapeData;
            //[ReadOnly]
            //public NativeList<int> localBlendShapeFrameCounts;
            [ReadOnly]
            public NativeList<int> localBlendShapeStartIndices;

            [NativeDisableParallelForRestriction]
            public NativeList<ShapeVertexDelta> editedLocalBlendShapeData;

            [NativeDisableParallelForRestriction]
            public NativeList<bool> depenetrationMask;

            public bool CheckPenetration(float4x4 skinningInverse, int ownerIndex, int vertIndexA, int vertIndexB, int shapeStartIndex, int collisionShapeStartIndex, out float3 localDisplacement)
            {
                localDisplacement = float3.zero;

                var vertexA = localVertexData[vertIndexA];
                var vertexB = localVertexData[vertIndexB];
                var shapeDeltasA = originalLocalBlendShapeData[shapeStartIndex + vertIndexA];
                var shapeDeltasB = originalLocalBlendShapeData[shapeStartIndex + vertIndexB];

                float maskWeightA = mask[vertIndexA];
                float maskWeightB = mask[vertIndexB];
                if (affectedVerticesOnly && (maskWeightA <= 0f || maskWeightB <= 0f || math.length(shapeDeltasA.deltaPosition) < 0.0001f || math.length(shapeDeltasB.deltaPosition) < 0.0001f)) return false;

                vertexA.position = (vertexA.position + shapeDeltasA.deltaPosition) - math.normalize(vertexA.normal + shapeDeltasA.deltaNormal) * thickness;
                vertexB.position = (vertexB.position + shapeDeltasB.deltaPosition) - math.normalize(vertexB.normal + shapeDeltasB.deltaNormal) * thickness;

                var skinningA = localVertexSkinning[vertIndexA];
                var skinningB = localVertexSkinning[vertIndexB];

                var worldVertexA = math.transform(skinningA, vertexA.position);
                var worldVertexB = math.transform(skinningB, vertexB.position);

                var nearbyTrisStartIndex = nearbyCollisionTrianglesStartIndices[ownerIndex];
                var nearbyTrisCount = nearbyCollisionTrianglesCounts[ownerIndex];
                for (int a = 0; a < nearbyTrisCount; a++)
                {
                    var colTriIndex = nearbyCollisionTriangles[nearbyTrisStartIndex + a];
                    var colTri = collisionTriangles[colTriIndex];

                    var colVert1 = collisionVertexData[colTri.i0];
                    var colVert2 = collisionVertexData[colTri.i1];
                    var colVert3 = collisionVertexData[colTri.i2];
                    var colShapeDeltas1 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i0];
                    var colShapeDeltas2 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i1];
                    var colShapeDeltas3 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i2];

                    colVert1.position = (colVert1.position + colShapeDeltas1.deltaPosition);
                    colVert2.position = (colVert2.position + colShapeDeltas2.deltaPosition);
                    colVert3.position = (colVert3.position + colShapeDeltas3.deltaPosition);

                    var colSkinning1 = collisionVertexSkinning[colTri.i0];
                    var colSkinning2 = collisionVertexSkinning[colTri.i1];
                    var colSkinning3 = collisionVertexSkinning[colTri.i2];

                    var colWorldVertex1 = math.transform(colSkinning1, colVert1.position);
                    var colWorldVertex2 = math.transform(colSkinning2, colVert2.position);
                    var colWorldVertex3 = math.transform(colSkinning3, colVert3.position);

                    /*if (Maths.IntersectSegmentTriangle(worldVertexA, worldVertexB, colWorldVertex1, colWorldVertex2, colWorldVertex3, out var hit))
                    {
                        //localDisplacement = math.rotate(skinningInverse, hit.normal * math.length(hit.point - worldVertexB));
                        float3 offset = hit.point - worldVertexB;
                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal));  
                        return true; 
                    }*/

                    float3 rayOffset = worldVertexB - worldVertexA;
                    if (Maths.seg_intersect_triangle(worldVertexA, rayOffset, colWorldVertex1, colWorldVertex2, colWorldVertex3, out Maths.RaycastHitResult hit))
                    {
                        float3 offsetA = (hit.point - worldVertexA) * maskWeightA;
                        float3 offsetB = (hit.point - worldVertexB) * maskWeightB;

                        float dA = math.dot(offsetA, hit.normal);
                        float dB = math.dot(offsetB, hit.normal);

                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.select(dB, dA, dA >= 0f));

                        return true;
                    }
                }

                return false;
            }

            public bool CheckPenetration2(float4x4 skinningInverse, int ownerIndex, MeshDataTools.Triangle localTriangle, int shapeStartIndex, int collisionShapeStartIndex, out float3 localDisplacement)
            {
                localDisplacement = float3.zero;

                var vertex0 = localVertexData[localTriangle.i0];
                var vertex1 = localVertexData[localTriangle.i1];
                var vertex2 = localVertexData[localTriangle.i2];
                var shapeDeltas0 = originalLocalBlendShapeData[shapeStartIndex + localTriangle.i0];
                var shapeDeltas1 = originalLocalBlendShapeData[shapeStartIndex + localTriangle.i1];
                var shapeDeltas2 = originalLocalBlendShapeData[shapeStartIndex + localTriangle.i2];

                float maskWeight0 = mask[localTriangle.i0];
                float maskWeight1 = mask[localTriangle.i1];
                float maskWeight2 = mask[localTriangle.i2];
                if (affectedVerticesOnly && (maskWeight0 <= 0f || maskWeight1 <= 0f || maskWeight2 <= 0f || math.length(shapeDeltas0.deltaPosition) < 0.0001f || math.length(shapeDeltas1.deltaPosition) < 0.0001f || math.length(shapeDeltas2.deltaPosition) < 0.0001f)) return false; 

                vertex0.position = (vertex0.position + shapeDeltas0.deltaPosition) - math.normalize(vertex0.normal + shapeDeltas0.deltaNormal) * thickness;
                vertex1.position = (vertex1.position + shapeDeltas1.deltaPosition) - math.normalize(vertex1.normal + shapeDeltas1.deltaNormal) * thickness;
                vertex2.position = (vertex2.position + shapeDeltas2.deltaPosition) - math.normalize(vertex2.normal + shapeDeltas2.deltaNormal) * thickness;

                var skinning0 = localVertexSkinning[localTriangle.i0];
                var skinning1 = localVertexSkinning[localTriangle.i1];
                var skinning2 = localVertexSkinning[localTriangle.i2];

                var worldVertex0 = math.transform(skinning0, vertex0.position);
                var worldVertex1 = math.transform(skinning1, vertex1.position);
                var worldVertex2 = math.transform(skinning2, vertex2.position);

                var nearbyTrisStartIndex = nearbyCollisionTrianglesStartIndices[ownerIndex];
                var nearbyTrisCount = nearbyCollisionTrianglesCounts[ownerIndex];
                for (int a = 0; a < nearbyTrisCount; a++)
                {
                    var colTriIndex = nearbyCollisionTriangles[nearbyTrisStartIndex + a];
                    var colTri = collisionTriangles[colTriIndex];

                    var colVert1 = collisionVertexData[colTri.i0];
                    var colVert2 = collisionVertexData[colTri.i1];
                    var colVert3 = collisionVertexData[colTri.i2];
                    var colShapeDeltas1 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i0];
                    var colShapeDeltas2 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i1];
                    var colShapeDeltas3 = collisionBlendShapeData[collisionShapeStartIndex + colTri.i2];

                    colVert1.position = (colVert1.position + colShapeDeltas1.deltaPosition);
                    colVert2.position = (colVert2.position + colShapeDeltas2.deltaPosition);
                    colVert3.position = (colVert3.position + colShapeDeltas3.deltaPosition); 

                    var colSkinning1 = collisionVertexSkinning[colTri.i0];
                    var colSkinning2 = collisionVertexSkinning[colTri.i1];
                    var colSkinning3 = collisionVertexSkinning[colTri.i2];

                    var colWorldVertex1 = math.transform(colSkinning1, colVert1.position);
                    var colWorldVertex2 = math.transform(colSkinning2, colVert2.position); 
                    var colWorldVertex3 = math.transform(colSkinning3, colVert3.position);

                    if (Maths.IntersectSegmentTriangle(colWorldVertex1, colWorldVertex2, worldVertex0, worldVertex1, worldVertex2, out float3 ab, out float3 ac, out float3 triNormal, out var hit))
                    {
                        //float3 offset = colWorldVertex1 - hit.point;
                        //localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);
                        
                        float3 offsetA = hit.point - colWorldVertex1;
                        float3 offsetB = hit.point - colWorldVertex2;

                        float dA = math.dot(offsetA, hit.normal);
                        float dB = math.dot(offsetB, hit.normal);

                        float3 coords = hit.barycentricCoordinate;
                        float weight = (maskWeight0 * coords.x) + (maskWeight1 * coords.y) + (maskWeight2 * coords.z);

                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.select(dB, dA, dA >= 0f) * localCollisionFactor * weight);

                        return true;
                    }
                    /*else if (Maths.IntersectSegmentTriangle(colWorldVertex2, colWorldVertex1, worldVertex0, worldVertex1, worldVertex2, ab, ac, triNormal, out hit))
                    {
                        float3 offset = colWorldVertex2 - hit.point;
                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);
                        return true;
                    }*/

                    else if (Maths.IntersectSegmentTriangle(colWorldVertex1, colWorldVertex3, worldVertex0, worldVertex1, worldVertex2, ab, ac, triNormal, out hit))
                    {
                        //float3 offset = colWorldVertex1 - hit.point;
                        //localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);

                        float3 offsetA = hit.point - colWorldVertex1;
                        float3 offsetB = hit.point - colWorldVertex3;

                        float dA = math.dot(offsetA, hit.normal);
                        float dB = math.dot(offsetB, hit.normal);

                        float3 coords = hit.barycentricCoordinate;
                        float weight = (maskWeight0 * coords.x) + (maskWeight1 * coords.y) + (maskWeight2 * coords.z);

                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.select(dB, dA, dA >= 0f) * localCollisionFactor * weight);

                        return true;
                    }
                    /*else if (Maths.IntersectSegmentTriangle(colWorldVertex3, colWorldVertex1, worldVertex0, worldVertex1, worldVertex2, ab, ac, triNormal, out hit))
                    {
                        float3 offset = colWorldVertex3 - hit.point;
                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);
                        return true;
                    }*/

                    else if (Maths.IntersectSegmentTriangle(colWorldVertex2, colWorldVertex3, worldVertex0, worldVertex1, worldVertex2, ab, ac, triNormal, out hit))
                    {
                        //float3 offset = colWorldVertex2 - hit.point;
                        //localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);

                        float3 offsetA = hit.point - colWorldVertex2;
                        float3 offsetB = hit.point - colWorldVertex3;

                        float dA = math.dot(offsetA, hit.normal);
                        float dB = math.dot(offsetB, hit.normal);

                        float3 coords = hit.barycentricCoordinate;
                        float weight = (maskWeight0 * coords.x) + (maskWeight1 * coords.y) + (maskWeight2 * coords.z);

                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.select(dB, dA, dA >= 0f) * localCollisionFactor * weight); 

                        return true;
                    }
                    /*else if (Maths.IntersectSegmentTriangle(colWorldVertex3, colWorldVertex2, worldVertex0, worldVertex1, worldVertex2, ab, ac, triNormal, out hit))
                    {
                        float3 offset = colWorldVertex3 - hit.point;
                        localDisplacement = math.rotate(skinningInverse, hit.normal * math.dot(offset, hit.normal) * localCollisionFactor);
                        return true;
                    }*/
                }

                return false;
            }

            public void Execute(int index)
            {
                int localIndex = localVertexIndices[index];

                int vertexTrianglesIndex = localVertexTriangleStartIndices[localIndex];
                int vertexTrianglesCount = localVertexTriangleCounts[localIndex];

                int shapeIndex = localBlendShapeStartIndices[targetBlendShapeIndex] + targetBlendShapeFrame * vertexCount;
                int collisionShapeIndex = collisionBlendShapeStartIndices[targetCollisionBlendShapeIndex] + targetCollisionBlendShapeFrame * collisionVertexCount;

                int shapeRelativeIndex = shapeIndex + localIndex;
                var finalShapeDeltas = originalLocalBlendShapeData[shapeRelativeIndex];

                float maskWeight = mask[localIndex];
                if (affectedVerticesOnly && (maskWeight <= 0f || math.length(finalShapeDeltas.deltaPosition) < 0.0001f)) return;

                bool penetrated = false;
                var editedShapeDeltas = finalShapeDeltas;
                for (int a = 0; a < vertexTrianglesCount; a++)
                {
                    var triangle = localVertexTriangles[vertexTrianglesIndex + a];

                    float4x4 skinningInverse = float4x4.identity;

                    float3 localDisplacement;
                    if (triangle.ownerIndex == MeshDataTools.TriangleOwnerIndex.A) 
                    {
                        skinningInverse = localVertexSkinningInverse[triangle.i0]; 
                        
                        if (CheckPenetration(skinningInverse, triangle.i0, triangle.i1, triangle.i0, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i0, triangle.i2, triangle.i0, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i0, triangle.i1, triangle.i2, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                    }
                    else if (triangle.ownerIndex == MeshDataTools.TriangleOwnerIndex.B)
                    {
                        skinningInverse = localVertexSkinningInverse[triangle.i1];

                        if (CheckPenetration(skinningInverse, triangle.i1, triangle.i0, triangle.i1, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i1, triangle.i2, triangle.i1, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i1, triangle.i2, triangle.i0, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                    }
                    else if (triangle.ownerIndex == MeshDataTools.TriangleOwnerIndex.C)
                    {
                        skinningInverse = localVertexSkinningInverse[triangle.i2];

                        if (CheckPenetration(skinningInverse, triangle.i2, triangle.i0, triangle.i2, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i2, triangle.i1, triangle.i2, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                        else if (CheckPenetration(skinningInverse, triangle.i2, triangle.i0, triangle.i1, shapeIndex, collisionShapeIndex, out localDisplacement))
                        {
                            penetrated = true;
                            editedShapeDeltas.deltaPosition += localDisplacement;
                            break;
                        }
                    }

                    if (CheckPenetration2(skinningInverse, localIndex, triangle, shapeIndex, collisionShapeIndex, out localDisplacement)) 
                    {
                        penetrated = true;
                        editedShapeDeltas.deltaPosition += localDisplacement; 
                        break;
                    }
                }

                float mix = factor * maskWeight;
                finalShapeDeltas.deltaPosition = math.lerp(finalShapeDeltas.deltaPosition, editedShapeDeltas.deltaPosition, mix);
                //finalShapeDeltas.deltaNormal = math.lerp(finalShapeDeltas.deltaNormal, editedShapeDeltas.deltaNormal, mix);
                //finalShapeDeltas.deltaTangent = math.lerp(finalShapeDeltas.deltaTangent, editedShapeDeltas.deltaTangent, mix);

                editedLocalBlendShapeData[shapeRelativeIndex] = finalShapeDeltas;

                if (penetrated) depenetrationMask[localIndex] = true; 
            }
        }

    }

    [Serializable]
    public struct BaseVertexData
    {
        public float3 position;
        public float3 normal;
        public float4 tangent;
    
        public static BaseVertexData operator +(BaseVertexData left, BaseVertexData right)
        {
            var output = left;

            output.position = left.position + right.position;
            output.normal = left.normal + right.normal;
            output.tangent = left.tangent + right.tangent;

            return output;
        }
        public static BaseVertexData operator -(BaseVertexData left, BaseVertexData right)
        {
            var output = left;

            output.position = left.position - right.position;
            output.normal = left.normal - right.normal;
            output.tangent = left.tangent - right.tangent;

            return output;
        }
        public static BaseVertexData operator +(BaseVertexData left, ShapeVertexDelta delta)
        {
            var output = left;

            output.position = left.position + delta.deltaPosition;
            output.normal = left.normal + delta.deltaNormal;
            output.tangent.xyz = left.tangent.xyz + delta.deltaTangent;

            return output;
        }
        public static BaseVertexData operator -(BaseVertexData left, ShapeVertexDelta delta)
        {
            var output = left;

            output.position = left.position - delta.deltaPosition;
            output.normal = left.normal - delta.deltaNormal;
            output.tangent.xyz = left.tangent.xyz - delta.deltaTangent;

            return output;
        }
        public static BaseVertexData operator *(BaseVertexData delta, float scalar)
        {
            delta.position *= scalar;
            delta.normal *= scalar;
            delta.tangent *= scalar;

            return delta;
        }
        public static BaseVertexData operator /(BaseVertexData delta, float scalar)
        {
            delta.position *= scalar;
            delta.normal *= scalar;
            delta.tangent *= scalar;

            return delta;
        }
    }

    [Serializable]
    public struct VertexInfluence
    {
        public int meshIndex;
        public int vertexIndex;
        public float weight;
        public float score;
    }
    [Serializable]
    public struct VertexInfluence2
    {
        public VertexInfluence influenceA;
        public VertexInfluence influenceB;
    }
    [Serializable]
    public struct VertexInfluence3
    {
        public VertexInfluence influenceA;
        public VertexInfluence influenceB;
        public VertexInfluence influenceC;
    }
    [Serializable]
    public struct VertexInfluence4
    {
        public VertexInfluence influenceA;
        public VertexInfluence influenceB;
        public VertexInfluence influenceC;
        public VertexInfluence influenceD;
    }

    [Serializable]
    public struct VertexBoneWeight
    {
        public int boneIndex;
        public float weight;
    }
    [Serializable]
    public struct VertexBoneWeight8
    {
        public int4 boneIndex;
        public float4 weight;

        public int4 boneIndex2;
        public float4 weight2;

        public static VertexBoneWeight8 Empty => new VertexBoneWeight8()
        {
            boneIndex = -1,
            weight = 0,
            boneIndex2 = -1,
            weight2 = 0
        };
    }
    [Serializable]
    public struct SkinnedVertex
    {
        public Matrix4x4 skinningMatrix;
        public Vector3 worldPosition;
        public VertexBoneWeight[] boneWeights;

        public float ComparisonScore(SkinnedVertex comparableVertex, float distanceBindingWeight)
        {
            float distance = Vector3.Distance(worldPosition, comparableVertex.worldPosition);
            return ComparisonScore(comparableVertex, distance, distanceBindingWeight);
        }
        public float ComparisonScore(SkinnedVertex comparableVertex, float distance, float distanceBindingWeight)
        {
            float score = 10;

            if (distance <= 0) return float.MaxValue;

            if (boneWeights != null && comparableVertex.boneWeights != null)
            {
                for (int a = 0; a < boneWeights.Length; a++)
                {
                    var bwA = boneWeights[a];
                    if (bwA.weight <= 0) continue;

                    for (int b = 0; b < comparableVertex.boneWeights.Length; b++)
                    {
                        var bwB = comparableVertex.boneWeights[b];
                        if (bwA.boneIndex == bwB.boneIndex && bwB.weight > 0)
                        {
                            score = score + Mathf.Lerp(500, 0, Mathf.Abs(bwA.weight - bwB.weight));
                        }
                    }
                }
            }
            else return 99999 / distance;

            return Mathf.LerpUnclamped(score, score / distance, distanceBindingWeight); 
        }
    }
    [Serializable]
    public struct SkinnedVertex8
    {
        public float4x4 skinningMatrix;
        public float3 worldPosition;
        public VertexBoneWeight8 boneWeights;

        public float ComparisonScore(SkinnedVertex8 comparableVertex, float distanceBindingWeight)
        {
            float distance = math.distance(worldPosition, comparableVertex.worldPosition);
            return ComparisonScore(comparableVertex, distance, distanceBindingWeight);
        }
        public float ComparisonScore(SkinnedVertex8 comparableVertex, float distance, float distanceBindingWeight)
        {
            float score = 10;

            if (distance <= 0) return float.MaxValue;
          
            bool4 weightCheckAA = boneWeights.weight > 0;
            bool4 weightCheckAB = boneWeights.weight2 > 0;

            bool4 weightCheckBA = comparableVertex.boneWeights.weight > 0;
            bool4 weightCheckBB = comparableVertex.boneWeights.weight2 > 0;

            float4 wAA = math.abs(boneWeights.weight - comparableVertex.boneWeights.weight);
            float4 wAB = math.abs(boneWeights.weight - comparableVertex.boneWeights.weight2); 
            float4 scoreAA = math.select(0, math.lerp(500, 0, wAA), (boneWeights.boneIndex == comparableVertex.boneWeights.boneIndex) & weightCheckAA & weightCheckBA);
            float4 scoreAB = math.select(0, math.lerp(500, 0, wAB), (boneWeights.boneIndex == comparableVertex.boneWeights.boneIndex2) & weightCheckAA & weightCheckBB);
            float4 scoreA = scoreAA + scoreAB;

            float4 wBA = math.abs(boneWeights.weight2 - comparableVertex.boneWeights.weight);
            float4 wBB = math.abs(boneWeights.weight2 - comparableVertex.boneWeights.weight2);
            float4 scoreBA = math.select(0, math.lerp(500, 0, wBA), (boneWeights.boneIndex2 == comparableVertex.boneWeights.boneIndex) & weightCheckAB & weightCheckBA);
            float4 scoreBB = math.select(0, math.lerp(500, 0, wBB), (boneWeights.boneIndex2 == comparableVertex.boneWeights.boneIndex2) & weightCheckAB & weightCheckBB);
            float4 scoreB = scoreBA + scoreBB;

            scoreA = scoreA + scoreB;
            score = score + scoreA.x + scoreA.y + scoreA.z + scoreA.w;

            //if (debug == 0 && score > 0) Debug.Log(scoreAA + " : " + scoreAB + " : " + scoreBA + " : " + scoreBB + " :: " + score);

            return math.lerp(score, score / distance, distanceBindingWeight);
        }
    }

    [Serializable]
    public struct SkinnedVertex8Reference
    {
        public int vertexIndex;
        public SkinnedVertex8 vertex;
    }

    [Serializable]
    public struct ShapeVertexDelta
    {
        public float3 deltaPosition;
        public float3 deltaNormal;
        public float3 deltaTangent;

        public static ShapeVertexDelta Lerp(ShapeVertexDelta A, ShapeVertexDelta B, float t)
        {
            var result = A;

            math.lerp(A.deltaPosition, B.deltaPosition, t);
            math.lerp(A.deltaNormal, B.deltaNormal, t);
            math.lerp(A.deltaTangent, B.deltaTangent, t);

            return result;
        }

        public static ShapeVertexDelta operator*(ShapeVertexDelta delta, float scalar)
        {
            delta.deltaPosition *= scalar;
            delta.deltaNormal *= scalar;
            delta.deltaTangent *= scalar;

            return delta;
        }
        public static ShapeVertexDelta operator /(ShapeVertexDelta delta, float scalar)
        {
            delta.deltaPosition *= scalar;
            delta.deltaNormal *= scalar;
            delta.deltaTangent *= scalar;

            return delta;
        }
    }

    public abstract class MeshDataTracker : IDisposable
    {
        public virtual void Dispose()
        {
            if (boneWeights.IsCreated)
            {
                boneWeights.Dispose();
                boneWeights = default;
            }
            if (boneCounts.IsCreated)
            {
                boneCounts.Dispose();
                boneCounts = default;
            }

            if (collisionTriangles.IsCreated)
            {
                collisionTriangles.Dispose();
                collisionTriangles = default;
            }
            if (collisionVertexData.IsCreated)
            {
                collisionVertexData.Dispose();
                collisionVertexData = default;
            }
            if (collisionVertexSkinningData.IsCreated)
            {
                collisionVertexSkinningData.Dispose();
                collisionVertexSkinningData = default;
            }
            if (collisionBlendShapeData.IsCreated)
            {
                collisionBlendShapeData.Dispose();
                collisionBlendShapeData = default;
            }
            if (collisionBlendShapeFrameCounts.IsCreated)
            {
                collisionBlendShapeFrameCounts.Dispose();
                collisionBlendShapeFrameCounts = default;
            }
            if (collisionBlendShapeStartIndices.IsCreated)
            {
                collisionBlendShapeStartIndices.Dispose();
                collisionBlendShapeStartIndices = default;
            }
        }

        protected Vector3[] vertices;
        public Vector3[] Vertices => vertices;
        protected Vector3[] normals;
        public Vector3[] Normals => normals;
        protected Vector4[] tangents;
        public Vector4[] Tangents => tangents;

        protected int[] triangles;

        protected Transform[] bones;
        protected Matrix4x4[] bindposes;
        protected NativeArray<BoneWeight1> boneWeights;
        protected NativeArray<byte> boneCounts;

        protected Matrix4x4[] boneMatrices;
        public Matrix4x4[] BoneMatrices => boneMatrices;
        public Matrix4x4[] GetUpToDateBoneMatrices() => boneMatrices = RealtimeMesh.GetBoneSkinningMatrices(bones, bindposes, boneMatrices);

        protected Matrix4x4[] perVertexSkinningMatrices;
        public Matrix4x4[] PerVertexSkinningMatrices => perVertexSkinningMatrices;
        public Matrix4x4[] GetUpToDatePerVertexSkinningMatrices() => perVertexSkinningMatrices = RealtimeMesh.GetPerVertexSkinningMatrices(GetUpToDateBoneMatrices(), boneWeights, boneCounts, perVertexSkinningMatrices);

        protected List<BlendShape> blendShapes;
        public int BlendShapeCount => blendShapes == null ? 0 : blendShapes.Count;
        public BlendShape GetBlendShape(int index) => blendShapes == null || index < 0 || index >= blendShapes.Count ? null : blendShapes[index];
        public string GetBlendShapeName(int index) => blendShapes == null || index < 0 || index >= blendShapes.Count ? null : blendShapes[index].name;
        public List<string> GetBlendShapeNames(List<string> outputList = null)
        {
            if (outputList == null) outputList = new List<string>();

            for (int a = 0; a < BlendShapeCount; a++) outputList.Add(blendShapes[a].name);

            return outputList;
        }

        #region Collision Data

        protected NativeList<MeshDataTools.Triangle> collisionTriangles;
        public NativeList<MeshDataTools.Triangle> CollisionTriangles => collisionTriangles;

        protected NativeList<BaseVertexData> collisionVertexData;
        public NativeList<BaseVertexData> CollisionVertexData => collisionVertexData;

        protected NativeList<float4x4> collisionVertexSkinningData;
        public NativeList<float4x4> CollisionVertexSkinningData => collisionVertexSkinningData;

        protected NativeList<ShapeVertexDelta> collisionBlendShapeData;
        public NativeList<ShapeVertexDelta> CollisionBlendShapeData => collisionBlendShapeData;

        protected NativeList<int> collisionBlendShapeFrameCounts;
        public NativeList<int> CollisionBlendShapeFrameCounts => collisionBlendShapeFrameCounts;

        protected NativeList<int> collisionBlendShapeStartIndices;
        public NativeList<int> CollisionBlendShapeStartIndices => collisionBlendShapeStartIndices;

        public bool HasCollisionData => collisionTriangles.IsCreated;
        public virtual void BuildCollisionData()
        {
            if (!collisionTriangles.IsCreated) collisionTriangles = new NativeList<MeshDataTools.Triangle>(0, Allocator.Persistent);
            collisionTriangles.Clear();
            if (triangles != null) for (int a = 0; a < triangles.Length; a += 3) collisionTriangles.Add(new MeshDataTools.Triangle() { i0 = triangles[a], i1 = triangles[a + 1], i2 = triangles[a + 2] });

            if (!collisionVertexData.IsCreated) collisionVertexData = new NativeList<BaseVertexData>(0, Allocator.Persistent);
            collisionVertexData.Clear();
            if (vertices != null) for (int a = 0; a < vertices.Length; a++) collisionVertexData.Add(new BaseVertexData() { position = vertices[a], normal = normals[a], tangent = tangents[a] });

            if (!collisionVertexSkinningData.IsCreated) collisionVertexSkinningData = new NativeList<float4x4>(0, Allocator.Persistent);
            collisionVertexSkinningData.Clear();
            var perVertexSkinning = GetUpToDatePerVertexSkinningMatrices();
            if (perVertexSkinning != null) for (int a = 0; a < perVertexSkinning.Length; a++) collisionVertexSkinningData.Add(perVertexSkinning[a]);

            if (!collisionBlendShapeData.IsCreated) collisionBlendShapeData = new NativeList<ShapeVertexDelta>(0, Allocator.Persistent);
            if (!collisionBlendShapeFrameCounts.IsCreated) collisionBlendShapeFrameCounts = new NativeList<int>(0, Allocator.Persistent);
            if (!collisionBlendShapeStartIndices.IsCreated) collisionBlendShapeStartIndices = new NativeList<int>(0, Allocator.Persistent);

            collisionBlendShapeData.Clear();
            collisionBlendShapeFrameCounts.Clear();
            collisionBlendShapeStartIndices.Clear();

            if (blendShapes != null)
            {
                for(int a = 0; a < BlendShapeCount; a++)
                {
                    collisionBlendShapeStartIndices.Add(collisionBlendShapeData.Length);

                    var shape = GetBlendShape(a);
                    int frameCount = shape.frames.Length;
                    collisionBlendShapeFrameCounts.Add(frameCount);
                    for(int b = 0; b < frameCount; b++)
                    {
                        var frame = shape.frames[b];
                        for(int c = 0; c < frame.deltaVertices.Length; c++) 
                        {
                            collisionBlendShapeData.Add(new ShapeVertexDelta()
                            {
                                deltaPosition = frame.deltaVertices[c],
                                deltaNormal = frame.deltaNormals[c],
                                deltaTangent = frame.deltaTangents[c]
                            });
                        }
                    }
                }
            }
        }
        #endregion
    }

    public class SkinnedMeshDataTracker : MeshDataTracker
    {
        protected Mesh mesh;

        public SkinnedMeshDataTracker(SkinnedMeshRenderer renderer)
        {
            mesh = renderer.sharedMesh;

            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;

            triangles = mesh.triangles;

            bones = renderer.bones;
            bindposes = mesh.bindposes;
            using (var boneWeights_ = mesh.GetAllBoneWeights())
            {
                using (var boneCounts_ = mesh.GetBonesPerVertex())
                {
                    boneWeights = new NativeArray<BoneWeight1>(boneWeights_, Allocator.Persistent);
                    boneCounts = new NativeArray<byte>(boneCounts_, Allocator.Persistent);
                }
            }

            boneMatrices = GetUpToDateBoneMatrices();
            perVertexSkinningMatrices = GetUpToDatePerVertexSkinningMatrices();

            blendShapes = mesh.GetBlendShapes();
        }
    }

    public class AmalgamatedSkinnedMeshDataTracker : MeshDataTracker
    {
        public struct MeshInput
        {
            public int indexOffset;
            public int[] indexMask;
            public SkinnedMeshRenderer renderer;

            public bool ContainsIndex(int index)
            {
                if (indexMask == null) return true;

                foreach (var index_ in indexMask) if (index_ == index) return true;
                return false;
            }
        }

        private MeshInput[] inputs;

        private static readonly List<int> _tempInts = new List<int>();
        private static readonly List<Vector3> _tempV3 = new List<Vector3>();
        private static readonly List<Vector4> _tempV4 = new List<Vector4>();
        private static readonly Dictionary<string, int> _tempIndexIdentifiers = new Dictionary<string, int>();
        private static readonly List<Transform> _tempTransforms = new List<Transform>();
        private static readonly List<Matrix4x4> _tempMatrices = new List<Matrix4x4>();
        private static readonly List<BoneWeight1> _tempBoneWeights = new List<BoneWeight1>();
        private static readonly List<byte> _tempBonesPerVertex = new List<byte>();
        private static readonly Dictionary<string, BlendShape> _tempBlendShapes = new Dictionary<string, BlendShape>();
        public AmalgamatedSkinnedMeshDataTracker(MeshInput[] inputs)
        {
            if (inputs == null) return;

            this.inputs = inputs;

            _tempV3.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                input.indexOffset = _tempV3.Count;

                var data = input.renderer.sharedMesh.vertices;
                if (input.indexMask == null)
                {
                    _tempV3.AddRange(data);
                }
                else
                {
                    for (int b = 0; b < input.indexMask.Length; b++)
                    {
                        var index = input.indexMask[b];
                        _tempV3.Add(data[index]);
                    }
                }

                inputs[a] = input;
            }
            vertices = _tempV3.ToArray();

            _tempV3.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                var data = input.renderer.sharedMesh.normals;
                if (input.indexMask == null)
                {
                    _tempV3.AddRange(data);
                }
                else
                {
                    for (int b = 0; b < input.indexMask.Length; b++)
                    {
                        var index = input.indexMask[b];
                        _tempV3.Add(data[index]);
                    }
                }
            }
            normals = _tempV3.ToArray();

            _tempV4.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                var data = input.renderer.sharedMesh.tangents;
                if (input.indexMask == null)
                {
                    _tempV4.AddRange(data);
                }
                else
                {
                    for (int b = 0; b < input.indexMask.Length; b++)
                    {
                        var index = input.indexMask[b];
                        _tempV4.Add(data[index]);
                    }
                }
            }
            tangents = _tempV4.ToArray();

            _tempInts.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                var data = input.renderer.sharedMesh.triangles;
                if (input.indexMask == null)
                {
                    for (int b = 0; b < data.Length; b++)
                    {
                        _tempInts.Add(data[b] + input.indexOffset);
                    }
                }
                else
                {
                    for (int b = 0; b < data.Length; b++)
                    {
                        var index = data[b];
                        if (!input.ContainsIndex(index)) continue;

                        _tempInts.Add(index + input.indexOffset);
                    }
                }
            }
            triangles = _tempInts.ToArray();

            _tempIndexIdentifiers.Clear();
            _tempTransforms.Clear();
            _tempMatrices.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                var data = input.renderer.bones;
                var data2 = input.renderer.sharedMesh.bindposes;
                for (int b = 0; b < data.Length; b++)
                {
                    var bone = data[b];
                    var bindpose = data2[b];

                    if (!_tempIndexIdentifiers.ContainsKey(bone.name))
                    {
                        _tempIndexIdentifiers[bone.name] = _tempTransforms.Count;
                        _tempTransforms.Add(bone);
                        _tempMatrices.Add(bindpose);
                    }
                }
            }
            bones = _tempTransforms.ToArray();
            bindposes = _tempMatrices.ToArray();

            _tempBoneWeights.Clear();
            _tempBonesPerVertex.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                using (var boneWeights_ = input.renderer.sharedMesh.GetAllBoneWeights())
                {
                    using (var boneCounts_ = input.renderer.sharedMesh.GetBonesPerVertex())
                    {
                        if (input.indexMask == null)
                        {
                            int boneWeightIndex = 0;
                            for (int b = 0; b < boneCounts_.Length; b++)
                            {
                                var count = boneCounts_[b];
                                _tempBonesPerVertex.Add(count);
                                for (int c = 0; c < count; c++)
                                {
                                    _tempBoneWeights.Add(boneWeights_[boneWeightIndex + c]);
                                }

                                boneWeightIndex = boneWeightIndex + count;
                            }
                        }
                        else
                        {
                            int boneWeightIndex = 0;
                            for (int b = 0; b < boneCounts_.Length; b++)
                            {
                                var count = boneCounts_[b];

                                if (input.ContainsIndex(b))
                                {
                                    _tempBonesPerVertex.Add(count);
                                    for (int c = 0; c < count; c++)
                                    {
                                        _tempBoneWeights.Add(boneWeights_[boneWeightIndex + c]);
                                    }
                                }

                                boneWeightIndex = boneWeightIndex + count;
                            }
                        }
                    }
                }
            }
            boneWeights = new NativeArray<BoneWeight1>(_tempBoneWeights.ToArray(), Allocator.Persistent);
            boneCounts = new NativeArray<byte>(_tempBonesPerVertex.ToArray(), Allocator.Persistent);

            boneMatrices = GetUpToDateBoneMatrices();
            perVertexSkinningMatrices = GetUpToDatePerVertexSkinningMatrices(); 

            _tempBlendShapes.Clear();
            for (int a = 0; a < inputs.Length; a++)
            {
                var input = inputs[a];
                 
                for(int b = 0; b < input.renderer.sharedMesh.blendShapeCount; b++)
                {
                    string shapeName = input.renderer.sharedMesh.GetBlendShapeName(b);
                    if (_tempBlendShapes.ContainsKey(shapeName)) continue;

                    int frameCount = input.renderer.sharedMesh.GetBlendShapeFrameCount(b);

                    var shape = new BlendShape(shapeName);
                    for (int c = 0; c < frameCount; c++) shape.AddFrame(input.renderer.sharedMesh.GetBlendShapeFrameWeight(b, c), new Vector3[vertices.Length], new Vector3[vertices.Length], new Vector3[vertices.Length]);

                    _tempBlendShapes[shapeName] = shape;
                }
            }

            blendShapes = new List<BlendShape>(_tempBlendShapes.Values);
            for (int a = 0; a < blendShapes.Count; a++)
            {
                var shape = blendShapes[a];

                for (int f = 0; f < shape.frames.Length; f++)
                {
                    var frame = shape.frames[f];

                    for (int b = 0; b < inputs.Length; b++)
                    {
                        var input = inputs[b]; 

                        int localShapeIndex = input.renderer.sharedMesh.GetBlendShapeIndex(shape.name);
                        if (localShapeIndex < 0) continue;

                        int localFrameCount = input.renderer.sharedMesh.GetBlendShapeFrameCount(localShapeIndex);
                        if (f >= localFrameCount) continue; 

                        var tempDeltaVertices = new Vector3[input.renderer.sharedMesh.vertexCount];
                        var tempDeltaNormals = new Vector3[tempDeltaVertices.Length];
                        var tempDeltaTangents = new Vector3[tempDeltaVertices.Length];
                        input.renderer.sharedMesh.GetBlendShapeFrameVertices(localShapeIndex, f, tempDeltaVertices, tempDeltaNormals, tempDeltaTangents);

                        if (input.indexMask == null)
                        {
                            for(int c = 0; c < input.renderer.sharedMesh.vertexCount; c++)
                            {
                                int ind = input.indexOffset + c;

                                frame.deltaVertices[ind] = tempDeltaVertices[c];
                                frame.deltaNormals[ind] = tempDeltaNormals[c];
                                frame.deltaTangents[ind] = tempDeltaTangents[c];
                            }
                        }
                        else
                        {
                            for (int c = 0; c < input.indexMask.Length; c++)
                            {
                                int ind = input.indexOffset + c;
                                int index = input.indexMask[c];

                                frame.deltaVertices[ind] = tempDeltaVertices[index];
                                frame.deltaNormals[ind] = tempDeltaNormals[index];
                                frame.deltaTangents[ind] = tempDeltaTangents[index];
                            }
                        }
                    }
                }
            }
            _tempBlendShapes.Clear();


        }
    }

}

#endif