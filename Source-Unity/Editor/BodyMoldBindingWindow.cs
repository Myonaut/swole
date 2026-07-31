#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Swole.Modding;
using Swole.Morphing;
using Swole.API.Unity;
using Swole.DataStructures;
using Swole;

public class BodyMoldBindingWindow : EditorWindow
{
    SkinnedMeshRenderer clothingRenderer;
    float mainClothingMeshScreenRelativeTransitionHeight;
    List<MeshLOD> additionalClothingLODs = new List<MeshLOD>();
    List<Material> materials = new List<Material>();

    // Select Mesh assets or CustomizableCharacterMeshV2 references for body meshes
    [Serializable]
    public class BodyMeshAsset
    {
        public SkinnedMeshRenderer renderer;
        public bool maskOnly;
        public float weight = 1f;

        public Mesh Mesh => renderer != null ? renderer.sharedMesh : null;
    }
    string bindingShapeName;
    float bindingShapeWeight = 100f;
    bool dynamicBoneWeights;
    RGBAChannel vertexIndexChannel = RGBAChannel.R;
    bool useUVsToFindClosestVertex = true;
    UVChannelURP nearestVertexUVChannel = UVChannelURP.UV0;
    Vector3 eulerOffset;
    List<BodyMeshAsset> bodyMeshAssets = new List<BodyMeshAsset>();
    bool maskByDistanceToFirst = true;
    string clothingVertexMaskName = null;
    float distanceBindingWeight = 0.1f;
    bool includeCollisionTriangles = true;
    bool includePushBackVertices = true;
    bool includeCoverageMask = true;

    [MenuItem("Swole/Body Mold Binding Generator")]
    public static void ShowWindow()
    {
        GetWindow<BodyMoldBindingWindow>("Body Mold Bindings");
    }

    void OnGUI()
    {
        GUILayout.Label("Body Mold Binding Generator", EditorStyles.boldLabel);
        clothingRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Clothing Mesh", clothingRenderer, typeof(SkinnedMeshRenderer), true);
        mainClothingMeshScreenRelativeTransitionHeight = EditorGUILayout.FloatField("Main LOD Transition Height", mainClothingMeshScreenRelativeTransitionHeight <= 0 ? 1f : mainClothingMeshScreenRelativeTransitionHeight);

        EditorGUILayout.LabelField("LOD Meshes");
        int removeAssetIdx = -1;
        for (int i = 0; i < additionalClothingLODs.Count; i++)
        {
            var entry = additionalClothingLODs[i];
            EditorGUILayout.BeginHorizontal();
            entry.mesh = (Mesh)EditorGUILayout.ObjectField(entry.mesh, typeof(Mesh), true);
            entry.screenRelativeTransitionHeight = EditorGUILayout.FloatField("Transition Height", entry.screenRelativeTransitionHeight);
            if (GUILayout.Button("-", GUILayout.Width(20))) removeAssetIdx = i;
            EditorGUILayout.EndHorizontal();
            additionalClothingLODs[i] = entry;
        }
        if (removeAssetIdx >= 0) additionalClothingLODs.RemoveAt(removeAssetIdx);
        if (GUILayout.Button("Add LOD Mesh")) additionalClothingLODs.Add(new MeshLOD() { screenRelativeTransitionHeight = additionalClothingLODs.Count <= 0 ? (mainClothingMeshScreenRelativeTransitionHeight - 0.1f) : (additionalClothingLODs[additionalClothingLODs.Count - 1].screenRelativeTransitionHeight - 0.1f) });

        if (materials == null) materials = new List<Material>();
        if (materials.Count <= 0 && clothingRenderer != null) materials.AddRange(clothingRenderer.sharedMaterials);
        removeAssetIdx = -1;
        for (int i = 0; i < materials.Count; i++)
        {
            var entry = materials[i];
            EditorGUILayout.BeginHorizontal();
            entry = (Material)EditorGUILayout.ObjectField(entry, typeof(Material), true);
            if (GUILayout.Button("-", GUILayout.Width(20))) removeAssetIdx = i;
            EditorGUILayout.EndHorizontal();
            materials[i] = entry;
        }
        if (removeAssetIdx >= 0) materials.RemoveAt(removeAssetIdx);
        if (GUILayout.Button("Add Material")) materials.Add(null); 

        bindingShapeName = EditorGUILayout.TextField("Binding Shape Name", bindingShapeName);
        if (!string.IsNullOrWhiteSpace(bindingShapeName)) bindingShapeWeight = EditorGUILayout.FloatField("Binding Shape Weight", bindingShapeWeight);

        EditorGUILayout.LabelField("Dynamic Bone Weights");
        dynamicBoneWeights = EditorGUILayout.ToggleLeft("", dynamicBoneWeights);

        vertexIndexChannel = (RGBAChannel)EditorGUILayout.EnumPopup("Vertex Index Channel", vertexIndexChannel); 
        useUVsToFindClosestVertex = EditorGUILayout.ToggleLeft("Use UVs to Find Closest Vertex", useUVsToFindClosestVertex);
        if (useUVsToFindClosestVertex)
        {
            nearestVertexUVChannel = (UVChannelURP)EditorGUILayout.EnumPopup("Nearest Vertex UV Channel", nearestVertexUVChannel); 
        } 

        EditorGUILayout.LabelField("Euler Offset");
        eulerOffset = EditorGUILayout.Vector3Field("", eulerOffset);  

        EditorGUILayout.LabelField("Body Meshes");
        removeAssetIdx = -1;
        for (int i = 0; i < bodyMeshAssets.Count; i++)
        {
            var entry = bodyMeshAssets[i];
            if (entry == null) entry = new BodyMeshAsset();
            EditorGUILayout.BeginHorizontal();
            entry.renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(entry.renderer, typeof(SkinnedMeshRenderer), true);
            entry.maskOnly = EditorGUILayout.ToggleLeft("Mask Only", entry.maskOnly, GUILayout.Width(100));
            if (!entry.maskOnly)
            {
                entry.weight = EditorGUILayout.FloatField("Weight", entry.weight);
            }
            if (GUILayout.Button("-", GUILayout.Width(20))) removeAssetIdx = i;
            EditorGUILayout.EndHorizontal();
            bodyMeshAssets[i] = entry;
        }
        if (removeAssetIdx >= 0) bodyMeshAssets.RemoveAt(removeAssetIdx);
        if (GUILayout.Button("Add Body Mesh")) bodyMeshAssets.Add(new BodyMeshAsset());

        if (bodyMeshAssets.Count > 1) maskByDistanceToFirst = EditorGUILayout.ToggleLeft("Mask By Distance To First", maskByDistanceToFirst); 
        clothingVertexMaskName = EditorGUILayout.TextField("Clothing Vertex Mask", clothingVertexMaskName);
        distanceBindingWeight = EditorGUILayout.FloatField("Distance Binding Weight", distanceBindingWeight);
        includeCollisionTriangles = EditorGUILayout.ToggleLeft("Include Collision Tris", includeCollisionTriangles);
        includePushBackVertices = EditorGUILayout.ToggleLeft("Include Push Back Vertices", includePushBackVertices);
        includeCoverageMask = EditorGUILayout.ToggleLeft("Include Coverage Mask", includeCoverageMask); 

        GUILayout.Space(8);
        if (GUILayout.Button("Generate Bindings and Save Asset"))
        {
            if (clothingRenderer == null || bodyMeshAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Assign clothing mesh and at least one body mesh asset.", "OK");
            }
            else
            {
                GenerateAndSave();
            }
        }
    }

    void GenerateAndSave()
    {
        try
        {
            // Build body mesh array from selected BodyMeshAsset entries
            var bodyList = new List<BodyMeshAsset>();
            foreach (var entry in bodyMeshAssets)
            {
                if (entry == null || entry.renderer == null || entry.Mesh == null) continue;
                bodyList.Add(entry);
            }

            // Generate per-body vertex bindings
            ClothingEditor.WeightedRenderer[] weightedBodies = new ClothingEditor.WeightedRenderer[bodyList.Count]; 
            for(int i = 0; i < bodyList.Count; i++)
            {
                weightedBodies[i] = new ClothingEditor.WeightedRenderer
                {
                    renderer = bodyList[i].renderer,
                    weight = bodyList[i].maskOnly ? -9999f : bodyList[i].weight
                };
            }
            BodyMoldBindingGenerator.GenerateBindingsForBodies(clothingRenderer, bindingShapeName, bindingShapeWeight, weightedBodies, Quaternion.Euler(eulerOffset), out var localIndicesPerBody, out var indicesPerBody, out var weightsPerBody, out var trisPerBody, out var pushBackVerts, out var maskedVerts, includeCollisionTriangles, includePushBackVertices, includeCoverageMask, clothingVertexMaskName, distanceBindingWeight, maskByDistanceToFirst);

            // Create temporary asset with new data
            var tempAsset = ScriptableObject.CreateInstance<BodyMoldBindings>();
            BodyMoldBindings.GeneratedMeshLOD[] lods = new BodyMoldBindings.GeneratedMeshLOD[additionalClothingLODs.Count + 1];
            lods[0] = new BodyMoldBindings.GeneratedMeshLOD
            {
                originalMesh = clothingRenderer.sharedMesh,
                meshLod = new MeshLOD() { screenRelativeTransitionHeight = mainClothingMeshScreenRelativeTransitionHeight }
            };
            for (int a = 0; a < additionalClothingLODs.Count; a++)
            {
                var meshLod = additionalClothingLODs[a];
                var origMesh = meshLod.mesh;
                meshLod.mesh = null;
                lods[a + 1] = new BodyMoldBindings.GeneratedMeshLOD
                {
                    originalMesh = origMesh,
                    meshLod = meshLod
                };
            }
            tempAsset.SetGeneratedMeshes( lods );
            tempAsset.SetMaterials(materials.ToArray());
            tempAsset.vertexIndexChannel = vertexIndexChannel;
            tempAsset.useUVsToFindClosestVertex = useUVsToFindClosestVertex;
            tempAsset.nearestVertexUVChannel = nearestVertexUVChannel;
            tempAsset.dynamicBoneWeights = dynamicBoneWeights;
            tempAsset.eulerOffset = eulerOffset;
            tempAsset.perBodyBindings = new BodyMoldBindings.BodyMeshBinding[bodyList.Count];

            for (int i = 0; i < bodyList.Count; i++)
            {
                tempAsset.perBodyBindings[i] = new BodyMoldBindings.BodyMeshBinding
                {
                    mesh = bodyList[i].Mesh,
                    vertexBindingLocalIndices = localIndicesPerBody[i],
                    vertexBindingIndices = indicesPerBody[i],
                    vertexBindingWeights = weightsPerBody[i],
                    collisionTriangles = trisPerBody[i],
                    pushBackVertices = pushBackVerts[i],
                    maskedVertices = maskedVerts[i],
                    maskOnly = bodyList[i].maskOnly
                };
            }

            tempAsset.InitializeVertexConnections();
            tempAsset.InitializeVertexWelding();

            string defaultSaveDir = null;
            if (clothingRenderer != null && clothingRenderer.sharedMesh != null)
            {
                string meshPath = AssetDatabase.GetAssetPath(clothingRenderer.sharedMesh);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    defaultSaveDir = System.IO.Path.GetDirectoryName(meshPath);
                }
            }

            // Save or update asset
            string path = EditorUtility.SaveFilePanelInProject("Save Bindings", clothingRenderer.name + "_Bindings", "asset", "Choose where to save the bindings asset", defaultSaveDir);
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(tempAsset);
                return;
            }

            // Check if asset already exists at this path
            var existingAsset = AssetDatabase.LoadAssetAtPath<BodyMoldBindings>(path);

            if (existingAsset != null)
            {
                // Use reflection to copy all fields from tempAsset to existingAsset
                var type = typeof(BodyMoldBindings);
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // Skip Unity internal fields
                    if (field.Name.StartsWith("m_") && field.DeclaringType == typeof(UnityEngine.Object)) 
                        continue;

                    var value = field.GetValue(tempAsset);
                    field.SetValue(existingAsset, value);
                }

                EditorUtility.SetDirty(existingAsset);
                DestroyImmediate(tempAsset);
                Selection.activeObject = existingAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(tempAsset, path);
                Selection.activeObject = tempAsset;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();

            EditorUtility.DisplayDialog("Success", "Bindings generated and saved.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }
}
#endif
