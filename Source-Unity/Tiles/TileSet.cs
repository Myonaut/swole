#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if BULKOUT_ENV
using RLD;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole
{

    [CreateAssetMenu(fileName = "TileSet", menuName = "Environment/TileSet", order = 2)]
    public class TileSet : ScriptableObject
    {

        public const int maxTileMeshCount = 16;

        [Serializable, Flags]
        public enum SubModelID
        {

            None = 0, A = 1, B = 2, C = 4, D = 8, E = 16, F = 32, G = 64, H = 128, I = 256, J = 512, K = 1024, L = 2048, M = 4096, N = 8192, O = 16384, P = 32768

        }

        private const string resourceFolderName = "Resources/";
#if BULKOUT_ENV
        public static TileSet Create(TileSetSource source, Material material, Tile[] tiles, PrefabPreviewLookAndFeel previewLookAndFeel, string path, string fileName, bool incrementIfExists = false, bool forceRefreshTilePreviews = false, bool saveTilePreviewsAsAssets = true, bool deleteExistingTilePreviewAssets = false)
#else
        public static TileSet Create(TileSetSource source, Material material, Tile[] tiles, string path, string fileName, bool incrementIfExists = false, bool forceRefreshTilePreviews = false, bool saveTilePreviewsAsAssets = true, bool deleteExistingTilePreviewAssets = false)
#endif
        {

            if (string.IsNullOrEmpty(path)) path = "Assets/";
            if (!path.StartsWith("Assets/")) path = "Assets/" + path;

            if (source == null)
            {

                Debug.LogError($"[{nameof(TileSet)}] Tile set source was null.");

                return null;

            }

            TileSet asset = ScriptableObject.CreateInstance<TileSet>();

            asset.material = material;
            asset.tiles = tiles;

            if (source.sourceMeshes != null && source.sourceMeshes.Length > 0)
            {

                Mesh mesh = source.sourceMeshes[0];

                if (source.sourceMeshes.Length > 1)
                {

                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = null;
                    List<Vector4> tangents = null;

                    List<Color> colors = null;

                    List<Vector4> uv0 = null;
                    List<Vector4> uv1 = null;
                    List<Vector4> uv2 = null;
                    List<Vector4> uv3 = new List<Vector4>(); // Use uv3.z for indexing each sub mesh

                    List<int> triangles = new List<int>();

                    // Combine Source Meshes

                    int indexStart = 0;
                    for (int a = 0; a < Mathf.Min(maxTileMeshCount, source.sourceMeshes.Length); a++)
                    {

                        Mesh sourceMesh = source.sourceMeshes[a];
                        if (sourceMesh == null) continue;

                        int[] sourceTriangles = sourceMesh.triangles;

                        if (sourceTriangles == null || sourceTriangles.Length == 0) continue;

                        int sourceVertexCount = sourceMesh.vertexCount;

                        Vector3[] sourceVertices = sourceMesh.vertices;
                        Vector3[] sourceNormals = sourceMesh.normals;
                        Vector4[] sourceTangents = sourceMesh.tangents;

                        Color[] sourceColors = sourceMesh.colors;

                        Vector4[] sourceUV0 = sourceMesh.GetUVsByChannelV4(0);
                        Vector4[] sourceUV1 = sourceMesh.GetUVsByChannelV4(1);
                        Vector4[] sourceUV2 = sourceMesh.GetUVsByChannelV4(2);
                        Vector4[] sourceUV3 = sourceMesh.GetUVsByChannelV4(3);

                        if (sourceVertices != null && sourceVertices.Length > 0 && vertices == null) vertices = new List<Vector3>(new Vector3[indexStart]);
                        if (sourceNormals != null && sourceNormals.Length > 0 && normals == null) normals = new List<Vector3>(new Vector3[indexStart]);
                        if (sourceTangents != null && sourceTangents.Length > 0 && tangents == null) tangents = new List<Vector4>(new Vector4[indexStart]);

                        if (sourceColors != null && sourceColors.Length > 0 && colors == null) colors = new List<Color>(new Color[indexStart]);

                        if (sourceUV0 != null && sourceUV0.Length > 0 && uv0 == null) uv0 = new List<Vector4>(new Vector4[indexStart]);
                        if (sourceUV1 != null && sourceUV1.Length > 0 && uv1 == null) uv1 = new List<Vector4>(new Vector4[indexStart]);
                        if (sourceUV2 != null && sourceUV2.Length > 0 && uv2 == null) uv2 = new List<Vector4>(new Vector4[indexStart]);
                        if (sourceUV3 != null && sourceUV3.Length > 0 && uv3 == null) uv3 = new List<Vector4>(new Vector4[indexStart]);


                        if (sourceVertices == null || sourceVertices.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) vertices?.Add(Vector3.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) vertices?.Add(sourceVertices[b]); }

                        if (sourceNormals == null || sourceNormals.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) normals?.Add(Vector3.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) normals?.Add(sourceNormals[b]); }

                        if (sourceTangents == null || sourceTangents.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) tangents?.Add(Vector4.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) tangents?.Add(sourceTangents[b]); }

                        if (sourceColors == null || sourceColors.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) colors?.Add(Color.clear); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) colors?.Add(sourceColors[b]); }

                        if (sourceUV0 == null || sourceUV0.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) uv0?.Add(Vector4.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) uv0?.Add(sourceUV0[b]); }

                        if (sourceUV1 == null || sourceUV1.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) uv1?.Add(Vector4.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) uv1?.Add(sourceUV1[b]); }

                        if (sourceUV2 == null || sourceUV2.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) uv2?.Add(Vector4.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) uv2?.Add(sourceUV2[b]); }

                        if (sourceUV3 == null || sourceUV3.Length < sourceVertexCount)
                        { for (int b = 0; b < sourceVertexCount; b++) uv3?.Add(Vector4.zero); }
                        else
                        { for (int b = 0; b < sourceVertexCount; b++) uv3?.Add(sourceUV3[b]); }

                        for (int b = 0; b < sourceVertexCount; b++)
                        {

                            var uv3_data = uv3[b + indexStart];
                            uv3_data.z = a + 1; // Use uv3.z for indexing each sub mesh
                            uv3[b + indexStart] = uv3_data;

                        }

                        for (int b = 0; b < sourceTriangles.Length; b++) triangles.Add(sourceTriangles[b] + indexStart);

                        indexStart += sourceVertexCount;

                    }

                    //

                    mesh = new Mesh();
                    mesh.name = fileName + "_tsmesh";

                    mesh.indexFormat = vertices.Count >= 65534 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

                    if (vertices != null && vertices.Count > 0) mesh.SetVertices(vertices);
                    if (normals != null && normals.Count > 0) mesh.SetNormals(normals);
                    if (tangents != null && tangents.Count > 0) mesh.SetTangents(tangents);

                    if (colors != null && colors.Count > 0) mesh.SetColors(colors);

                    if (uv0 != null && uv0.Count > 0) mesh.SetUVs(0, uv0);
                    if (uv1 != null && uv1.Count > 0) mesh.SetUVs(1, uv1);
                    if (uv2 != null && uv2.Count > 0) mesh.SetUVs(2, uv2);
                    if (uv3 != null && uv3.Count > 0) mesh.SetUVs(3, uv3);

                    mesh.subMeshCount = 1;
                    mesh.SetTriangles(triangles, 0);

                    mesh.RecalculateBounds();

                }

                asset.mesh = mesh;

#if UNITY_EDITOR
                string fullPathMesh = $"{(path + (path.EndsWith('/') ? "" : "/"))}{mesh.name}.asset";
                if (incrementIfExists) fullPathMesh = AssetDatabase.GenerateUniqueAssetPath(fullPathMesh);
                AssetDatabase.CreateAsset(mesh, fullPathMesh);
#endif

            }

            if (tiles != null && tiles.Length > 0)
            {

#if BULKOUT_ENV
                var previewGen = new EditorPrefabPreviewGen();

                previewGen.BeginGenSession(previewLookAndFeel);
                for (int i = 0; i < tiles.Length; i++)
                {

                    var tile = tiles[i];

                    if (tile == null) continue;

                    bool previewIsGenerated = true;
                    string previewTexturePath = "";
#if UNITY_EDITOR
                    if (tile.previewTexture != null)
                    {
                        previewTexturePath = AssetDatabase.GetAssetPath(tile.previewTexture);
                        if (!string.IsNullOrEmpty(previewTexturePath)) previewIsGenerated = false;
                    }
#endif
                    if (!forceRefreshTilePreviews && !previewIsGenerated) continue;

                    GameObject tempInstance = asset.CreatePreRuntimeTilePrefab(i, source);
                    tempInstance.name = tile.name + "_preview";

#if UNITY_EDITOR
                    EditorUtility.DisplayProgressBar("Creating tile previews...", tile.name, (i + 1f) / tiles.Length);

                    if (tile.previewTexture != null)
                    {
                        if (previewIsGenerated) Texture2D.DestroyImmediate(tile.previewTexture); else if (deleteExistingTilePreviewAssets) AssetDatabase.DeleteAsset(previewTexturePath);
                    }
#endif

                    tile.previewTexture = previewGen.Generate(tempInstance);
                    tile.previewTexture.name = fileName + "_" + tile.name + "_preview";

#if UNITY_EDITOR
                    if (saveTilePreviewsAsAssets)
                    {
                        string fullPathPreview = $"{(path + (path.EndsWith('/') ? "" : "/"))}{tile.previewTexture.name}.asset";
                        if (incrementIfExists) fullPathPreview = AssetDatabase.GenerateUniqueAssetPath(fullPathPreview);
                        AssetDatabase.CreateAsset(tile.previewTexture, fullPathPreview);
                    }
#endif

                    GameObject.DestroyImmediate(tempInstance);

                }
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
                previewGen.EndGenSession();
#else
                // TODO: Alternative for generating tile previews?
#endif

            }

#if UNITY_EDITOR

            asset.sourcePath = AssetDatabase.GetAssetPath(source);
            int startIndex = asset.sourcePath.IndexOf(resourceFolderName);
            if (startIndex >= 0)
            {
                startIndex = startIndex + resourceFolderName.Length;
                if (startIndex < asset.sourcePath.Length)
                {
                    asset.sourcePath = asset.sourcePath.Substring(startIndex);
                }
                else
                {
                    Debug.LogError($"[{nameof(TileSet)}] Invalid tile set source path '{asset.sourcePath}'.");
                    asset.sourcePath = "";
                }

            }
            int extensionIndex = asset.sourcePath.LastIndexOf('.');
            if (extensionIndex >= 0) asset.sourcePath = asset.sourcePath.Substring(0, extensionIndex);

            string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
            if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
#endif

            return asset;
        }

        public string sourcePath;
        public bool HasSource => !string.IsNullOrEmpty(sourcePath);

        public Mesh mesh;
        public Material material;

        public Tile[] tiles;
        public int TileCount => tiles == null ? 0 : tiles.Length;

        public Tile this[int tileIndex] => tiles == null || tileIndex < 0 || tileIndex >= tiles.Length ? null : tiles[tileIndex];


        /// <summary>
        /// TODO: Add functionality to load from user file system if source is from a mod. Source path could start with "~USER/" for example to differentiate it from embedded files 
        /// </summary>
        public TileSetSource Source
        {

            get
            {

                if (HasSource) return Resources.Load<TileSetSource>(sourcePath);

                return null;

            }

        }

        public GameObject CreateNewTileInstance(int tileIndex)
        {

            if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length) return null;

            var tile = tiles[tileIndex];
            if (tile == null) return null;

            GameObject instance;

            if (tile.prefabBase == null)
            {

                instance = new GameObject(tile.name);

            }
            else
            {

                instance = Instantiate(tile.prefabBase);

            }
            instance.name = tile.name;

            var transform = instance.transform;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            return instance;

        }

        public GameObject CreatePreRuntimeTilePrefab(int tileIndex, TileSetSource source)
        {

            if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length) return null;

            var tile = tiles[tileIndex];
            if (tile == null) return null;

            if (source == null) source = Source;

            GameObject prefab = CreateNewTileInstance(tileIndex);
            prefab.name = tile.name;

            int meshMask = (int)tile.subModelId;

            if (source != null && source.sourceMeshes != null)
            {

                for (int a = 0; a < source.sourceMeshes.Length; a++)
                {

                    var sourceMesh = source.sourceMeshes[a];
                    if (sourceMesh == null) continue;

                    if (((a + 1) & meshMask) == 0) continue; // Exclude mesh

                    GameObject meshInstance = new GameObject($"mesh_{a}");
                    meshInstance.transform.SetParent(prefab.transform, false);
                    meshInstance.transform.localPosition = tile.positionOffset;
                    meshInstance.transform.localRotation = Quaternion.Euler(tile.initialRotationEuler);
                    meshInstance.transform.localScale = tile.initialScale;

                    MeshFilter filter = meshInstance.AddComponent<MeshFilter>();
                    MeshRenderer renderer = meshInstance.AddComponent<MeshRenderer>();

                    filter.sharedMesh = sourceMesh;
                    renderer.sharedMaterial = material;

                }

            }

            return prefab;

        }

    }

}

#endif
