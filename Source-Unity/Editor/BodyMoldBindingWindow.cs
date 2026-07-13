#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Swole.Modding;
using Swole.Morphing;

public class BodyMoldBindingWindow : EditorWindow
{
    SkinnedMeshRenderer clothingRenderer; 
    // Select Mesh assets or CustomizableCharacterMeshV2 references for body meshes
    [Serializable]
    public class BodyMeshAsset
    {
        public SkinnedMeshRenderer renderer; 
        public float weight = 1f;

        public Mesh Mesh => renderer != null ? renderer.sharedMesh : null;
    }
    bool dynamicBoneWeights;
    Vector3 eulerOffset;
    List<BodyMeshAsset> bodyMeshAssets = new List<BodyMeshAsset>();
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

        EditorGUILayout.LabelField("Dynamic Bone Weights");
        dynamicBoneWeights = EditorGUILayout.ToggleLeft("", dynamicBoneWeights);  

        EditorGUILayout.LabelField("Euler Offset");
        eulerOffset = EditorGUILayout.Vector3Field("", eulerOffset); 

        EditorGUILayout.LabelField("Body Meshes");
        int removeAssetIdx = -1;
        for (int i = 0; i < bodyMeshAssets.Count; i++)
        {
            var entry = bodyMeshAssets[i];
            if (entry == null) entry = new BodyMeshAsset();
            EditorGUILayout.BeginHorizontal();
            entry.renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(entry.renderer, typeof(SkinnedMeshRenderer), true);
            entry.weight = EditorGUILayout.FloatField("Weight", entry.weight);
            if (GUILayout.Button("-", GUILayout.Width(20))) removeAssetIdx = i;
            EditorGUILayout.EndHorizontal();
            bodyMeshAssets[i] = entry;
        }
        if (removeAssetIdx >= 0) bodyMeshAssets.RemoveAt(removeAssetIdx);

        if (GUILayout.Button("Add Body Mesh")) bodyMeshAssets.Add(new BodyMeshAsset());

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
                    weight = bodyList[i].weight
                };
            }
            BodyMoldBindingGenerator.GenerateBindingsForBodies(clothingRenderer, weightedBodies, Quaternion.Euler(eulerOffset), out var localIndicesPerBody, out var indicesPerBody, out var weightsPerBody, out var trisPerBody, out var pushBackVerts, out var maskedVerts, includeCollisionTriangles, includePushBackVertices, includeCoverageMask, clothingVertexMaskName, distanceBindingWeight);

            // Create temporary asset with new data
            var tempAsset = ScriptableObject.CreateInstance<Swole.Modding.BodyMoldBindings>();
            tempAsset.SetOriginalMesh(clothingRenderer.sharedMesh);
            tempAsset.dynamicBoneWeights = dynamicBoneWeights;
            tempAsset.eulerOffset = eulerOffset;
            tempAsset.perBodyBindings = new BodyMoldBindings.BodyMeshBinding[bodyList.Count];

            for (int i = 0; i < bodyList.Count; i++)
            {
                tempAsset.perBodyBindings[i] = new Swole.Modding.BodyMoldBindings.BodyMeshBinding
                {
                    mesh = bodyList[i].Mesh,
                    vertexBindingLocalIndices = localIndicesPerBody[i],
                    vertexBindingIndices = indicesPerBody[i],
                    vertexBindingWeights = weightsPerBody[i],
                    collisionTriangles = trisPerBody[i],
                    pushBackVertices = pushBackVerts[i],
                    maskedVertices = maskedVerts[i]
                };
            }

            tempAsset.InitializeVertexConnections();

            // Save or update asset
            string path = EditorUtility.SaveFilePanelInProject("Save Bindings", clothingRenderer.name + "_Bindings", "asset", "Choose where to save the bindings asset");
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(tempAsset);
                return;
            }

            // Check if asset already exists at this path
            var existingAsset = AssetDatabase.LoadAssetAtPath<Swole.Modding.BodyMoldBindings>(path);

            if (existingAsset != null)
            {
                // Use reflection to copy all fields from tempAsset to existingAsset
                var type = typeof(Swole.Modding.BodyMoldBindings);
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
