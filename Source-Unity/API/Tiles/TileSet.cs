#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if BULKOUT_ENV
using RLD;
#endif

using Swole.Script;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "TileSet", menuName = "Swole/Environment/TileSet", order = 2)]
    public class TileSet : ScriptableObject, ITileSet
    {

        public System.Type AssetType => GetType(); 
        public object Asset => this;

        protected bool isNotInternalAsset;
        public bool IsInternalAsset
        {
            get => !isNotInternalAsset;
            set => isNotInternalAsset = !value;
        }

        public bool IsValid => !disposed && this != null;
        protected bool disposed;
        public void Dispose()
        {
            if (!IsInternalAsset && !disposed)
            {
                GameObject.Destroy(this); 
                disposed = true;
            }
        }
        public void DisposeSelf() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => ReferenceEquals(this, asset);

        public static TileSet NewExternalInstance()
        {
            var inst = ScriptableObject.CreateInstance<TileSet>();
            inst.isNotInternalAsset = true;
            return inst;
        }

        #region IEngineObject

        public string Name => name;
        public object Instance => this;
        public int InstanceID => GetInstanceID();
        public bool IsDestroyed => false;

        public void Destroy(float timeDelay = 0) => EngineInternal.EngineObject.Destroy(this, timeDelay);
        public void AdminDestroy(float timeDelay = 0) => EngineInternal.EngineObject.AdminDestroy(this, timeDelay);

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        #endregion

        public const int maxTileMeshCount = 16;

        private const string resourceFolderName = "Resources/";
#if BULKOUT_ENV
        /// <summary>
        /// Uses Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
        /// </summary>
        public static TileSet Create(TileSetSource source, Material material, Material outlineMaterial, Tile[] tiles, PrefabPreviewLookAndFeel previewLookAndFeel, string path, string fileName, bool incrementIfExists = false, bool forceRefreshTilePreviews = false, bool saveTilePreviewsAsAssets = true, bool deleteExistingTilePreviewAssets = false)
#else
        public static TileSet Create(TileSetSource source, Material material, Material outlineMaterial, Tile[] tiles, string path, string fileName, bool incrementIfExists = false, bool forceRefreshTilePreviews = false, bool saveTilePreviewsAsAssets = true, bool deleteExistingTilePreviewAssets = false)
#endif
        {

            if (string.IsNullOrEmpty(path)) path = "Assets/";
            if (!path.StartsWith("Assets/")) path = "Assets/" + path;

            if (source == null)
            {

                swole.LogError($"[{nameof(TileSet)}] Tile set source was null.");

                return null; 

            } 

            TileSet asset = CreateInstance<TileSet>();

            asset.id = fileName;
            asset.material = material;
            asset.outlineMaterial = outlineMaterial; 
            foreach (var tile in tiles) if (tile != null && tile.initialScale == Vector3.zero) tile.initialScale = Vector3.one; // likely the scale initialized as zero and the user didn't notice 
            asset.tiles = tiles;

            if (source.sourceMeshes != null && source.sourceMeshes.Length > 0) 
            {

                Mesh mesh = source.sourceMeshes[0];

                if (source.sourceMeshes.Length > 1) // Merge meshes into one
                {

                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = null;
                    List<Vector4> tangents = null;

                    List<Color> colors = null;

                    List<Vector4> uv0 = null;
                    List<Vector4> uv1 = null;
                    List<Vector4> uv2 = null;
                    List<Vector4> uv3 = new List<Vector4>(); // Use uv3.y for indexing each sub mesh. z and w are used for delta normals

                    List<int> triangles = new List<int>();

                    // Combine Source Meshes

                    int indexStart = 0;
                    for (int a = 0; a < Mathf.Min(maxTileMeshCount, source.sourceMeshes.Length); a++)
                    {

                        Mesh sourceMesh = source.sourceMeshes[a];
                        if (sourceMesh == null) continue;

                        int id = a == 0 ? 1 : a * 2;

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
                            uv3_data.y = id; // Use uv3.y for indexing each sub mesh. z and w are used for delta normals
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

#if UNITY_EDITOR
                    string fullPathMesh = $"{path + (path.EndsWith('/') ? "" : "/")}{mesh.name}.asset";
                    if (incrementIfExists) fullPathMesh = AssetDatabase.GenerateUniqueAssetPath(fullPathMesh);
                    AssetDatabase.CreateAsset(mesh, fullPathMesh); 
#endif

                } 
                else 
                {
                    // Only one mesh so don't use mesh masking

                    /*if (asset.tiles != null)
                    {
                        for(int a = 0; a < asset.tiles.Length; a++)
                        {
                            var tile = asset.tiles[a].Duplicate(); 
                            tile.subModelId = SubModelID.None;
                            asset.tiles[a] = tile;
                        }
                    }*/
                    asset.ignoreMeshMasking = true; // handled elsewhere
                }

                asset.mesh = mesh;

            }

            if (tiles != null && tiles.Length > 0)
            {

#if BULKOUT_ENV // Uses Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
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

                    try
                    {
#if UNITY_EDITOR
                        EditorUtility.DisplayProgressBar("Creating tile previews...", tile.name, (i + 1f) / tiles.Length);



                        if (tile.previewTexture != null)
                        {
                            if (previewIsGenerated) DestroyImmediate(tile.previewTexture); else if (deleteExistingTilePreviewAssets) AssetDatabase.DeleteAsset(previewTexturePath);
                        }
#endif

                        tile.previewTexture = previewGen.Generate(tempInstance);
                        tile.previewTexture.name = fileName + "_" + tile.name + "_preview";

#if UNITY_EDITOR
                        if (saveTilePreviewsAsAssets)
                        {
                            string fullPathPreview = $"{path + (path.EndsWith('/') ? "" : "/")}{tile.previewTexture.name}.asset";
                            if (incrementIfExists) fullPathPreview = AssetDatabase.GenerateUniqueAssetPath(fullPathPreview);
                            AssetDatabase.CreateAsset(tile.previewTexture, fullPathPreview);
                        }
#endif
                    }
                    catch(Exception ex)
                    {
                        Debug.LogException(ex); 
                    }

                    DestroyImmediate(tempInstance);

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
                    swole.LogError($"[{nameof(TileSet)}] Invalid tile set source path '{asset.sourcePath}'.");
                    asset.sourcePath = "";
                }

            }
            int extensionIndex = asset.sourcePath.LastIndexOf('.');
            if (extensionIndex >= 0) asset.sourcePath = asset.sourcePath.Substring(0, extensionIndex);

            string fullPath = $"{path + (path.EndsWith('/') ? "" : "/")}{fileName}.asset";
            if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
#endif

            return asset;
        }

        [SerializeField]
        protected string id;
        public string ID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(id)) return name;
                return id; 
            }
        }

        public string sourcePath;
        public bool HasSource => !string.IsNullOrWhiteSpace(sourcePath);

        [SerializeField, HideInInspector]
        public bool ignoreMeshMasking;
        public bool IgnoreMeshMasking
        {
            get => ignoreMeshMasking;
            set => ignoreMeshMasking = value;
        }

        [SerializeField]
        protected string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(collectionId);

        public Mesh mesh;
        public Material material;
        public Material outlineMaterial;

        // TODO: Fully implement ITileSet
        public IMeshAsset TileMesh
        {
            get => null;
            set { }
        }
        public IMaterialAsset TileMaterial
        {
            get => null;
            set { }
        }
        public IMaterialAsset TileOutlineMaterial
        {
            get => null; 
            set { }
        }

        public Tile[] tiles;
        public ITile[] Tiles
        {
            get
            {
                if (tiles == null) return null;
                var array = new ITile[tiles.Length];
                for (int a = 0; a < tiles.Length; a++) array[a] = tiles[a];
                return array;
            }
            set
            {
                if (value == null) 
                {
                    tiles = null;
                    return;
                }
                tiles = new Tile[value.Length];
                for (int a = 0; a < tiles.Length; a++) if (value[a] is Tile tile) tiles[a] = tile;
            }
        }

        public int TileCount => tiles == null ? 0 : tiles.Length;
        public ITile this[int tileIndex] => tiles == null || tileIndex < 0 || tileIndex >= tiles.Length ? null : tiles[tileIndex];


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

        /// <summary>
        /// Used to create a prefab object for tile editors
        /// </summary>
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

                    int id = a == 0 ? 1 : a * 2;

                    if ((id & meshMask) == 0) continue; // Exclude mesh
                      
                    GameObject meshInstance = new GameObject($"mesh_{a}");
                    meshInstance.transform.SetParent(prefab.transform, false);
                    meshInstance.transform.localPosition = tile.positionOffset;
                    meshInstance.transform.localRotation = Quaternion.Euler(tile.initialRotationEuler);  
                    meshInstance.transform.localScale = tile.initialScale;

                    MeshFilter filter = meshInstance.AddComponent<MeshFilter>();
                    MeshRenderer renderer = meshInstance.AddComponent<MeshRenderer>();

                    filter.sharedMesh = sourceMesh;
                    if (outlineMaterial != null)
                    {
                        renderer.sharedMaterials = new Material[] { material, outlineMaterial }; 
                    }
                    else
                    {
                        renderer.sharedMaterial = material;
                    }

                }

            }

            return prefab;

        }

    }

}

#endif
