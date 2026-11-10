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
    string bindingShapeName = "bind";
    float bindingShapeWeight = 100f;
    bool dynamicBoneWeights;
    UVChannelURP vertexIndexUVChannel = UVChannelURP.UV3;
    RGBAChannel vertexIndexUVChannelElement = RGBAChannel.R;
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

    BodyMoldBindings editingAsset;

    [MenuItem("Swole/Body Mold Binding Generator")]
    public static void ShowWindow()
    {
        GetWindow<BodyMoldBindingWindow>("Body Mold Bindings");
    }

    [MenuItem("Swole/Open All Body Mold Binding Assets")]
    public static void OpenAllBodyMoldBindingAssets()
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(BodyMoldBindings).Name}");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            BodyMoldBindings asset = AssetDatabase.LoadAssetAtPath<BodyMoldBindings>(assetPath);

            if (asset != null)
            {
                BodyMoldBindingWindow window = ScriptableObject.CreateInstance<BodyMoldBindingWindow>();

                window.titleContent = new GUIContent(asset.name);
                window.Show();
                window.LoadFromAsset(asset); 
            }
        }
    }

    [MenuItem("CONTEXT/BodyMoldBindings/Edit Bindings")]
    public static void EditBindings(MenuCommand command)
    {
        var bindings = command.context as BodyMoldBindings;
        if (bindings == null) return;

        var window = GetWindow<BodyMoldBindingWindow>("Body Mold Bindings");
        window.LoadFromAsset(bindings);
    }

    void LoadFromAsset(BodyMoldBindings asset)
    {
        if (asset == null) return;

        editingAsset = asset;
        var settings = asset.GetGenerationSettings();
        if (settings == null) return;

        clothingRenderer = settings.clothingRenderer != null ? settings.clothingRenderer.Resolve() : null;
        mainClothingMeshScreenRelativeTransitionHeight = settings.mainClothingMeshScreenRelativeTransitionHeight;

        additionalClothingLODs.Clear();
        if (settings.additionalClothingLODs != null)
        {
            foreach (var lod in settings.additionalClothingLODs)
            {
                if (lod == null) continue;
                additionalClothingLODs.Add(new MeshLOD() { mesh = lod.mesh, screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight });
            }
        }

        materials.Clear();
        if (settings.materials != null) materials.AddRange(settings.materials);

        bindingShapeName = settings.bindingShapeName;
        bindingShapeWeight = settings.bindingShapeWeight;
        dynamicBoneWeights = settings.dynamicBoneWeights;
        vertexIndexUVChannel = settings.vertexIndexUVChannel;
        vertexIndexUVChannelElement = settings.vertexIndexUVChannelElement;
        useUVsToFindClosestVertex = settings.useUVsToFindClosestVertex;
        nearestVertexUVChannel = settings.nearestVertexUVChannel;
        eulerOffset = settings.eulerOffset;

        bodyMeshAssets.Clear();
        if (settings.bodyMeshAssets != null)
        {
            foreach (var body in settings.bodyMeshAssets)
            {
                if (body == null) continue;
                bodyMeshAssets.Add(new BodyMeshAsset() { renderer = body.renderer != null ? body.renderer.Resolve() : null, maskOnly = body.maskOnly, weight = body.weight });
            }
        }

        maskByDistanceToFirst = settings.maskByDistanceToFirst;
        clothingVertexMaskName = settings.clothingVertexMaskName;
        distanceBindingWeight = settings.distanceBindingWeight;
        includeCollisionTriangles = settings.includeCollisionTriangles;
        includePushBackVertices = settings.includePushBackVertices;
        includeCoverageMask = settings.includeCoverageMask;
    }

    void OnGUI()
    {
        GUILayout.Label("Body Mold Binding Generator", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var newEditingAsset = (BodyMoldBindings)EditorGUILayout.ObjectField("Load Existing Asset", editingAsset, typeof(BodyMoldBindings), false);
        if (EditorGUI.EndChangeCheck() && newEditingAsset != editingAsset)
        {
            if (newEditingAsset != null) LoadFromAsset(newEditingAsset); else editingAsset = null;
        }

        GUILayout.Space(4);
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

        vertexIndexUVChannel = (UVChannelURP)EditorGUILayout.EnumPopup("Vertex Index UV Channel", vertexIndexUVChannel);
        vertexIndexUVChannelElement = (RGBAChannel)EditorGUILayout.EnumPopup("Vertex Index UV Channel Element", vertexIndexUVChannelElement); 
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
            tempAsset.vertexIndexUVChannel = vertexIndexUVChannel;
            tempAsset.vertexIndexUVChannelElement = vertexIndexUVChannelElement;
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

            // Store the settings used to generate these bindings so the window can be reopened later with the same targets/settings
            var generationSettings = new BodyMoldBindings.GenerationSettings
            {
                clothingRenderer = BodyMoldBindings.SceneRendererRef.Create(clothingRenderer),
                mainClothingMeshScreenRelativeTransitionHeight = mainClothingMeshScreenRelativeTransitionHeight,
                additionalClothingLODs = new BodyMoldBindings.GenerationSettings.LODEntry[additionalClothingLODs.Count],
                materials = materials.ToArray(),
                bindingShapeName = bindingShapeName,
                bindingShapeWeight = bindingShapeWeight,
                dynamicBoneWeights = dynamicBoneWeights,
                vertexIndexUVChannel = vertexIndexUVChannel,
                vertexIndexUVChannelElement = vertexIndexUVChannelElement,
                useUVsToFindClosestVertex = useUVsToFindClosestVertex,
                nearestVertexUVChannel = nearestVertexUVChannel,
                eulerOffset = eulerOffset,
                bodyMeshAssets = new BodyMoldBindings.GenerationSettings.BodyMeshEntry[bodyMeshAssets.Count],
                maskByDistanceToFirst = maskByDistanceToFirst,
                clothingVertexMaskName = clothingVertexMaskName,
                distanceBindingWeight = distanceBindingWeight,
                includeCollisionTriangles = includeCollisionTriangles,
                includePushBackVertices = includePushBackVertices,
                includeCoverageMask = includeCoverageMask
            };
            for (int a = 0; a < additionalClothingLODs.Count; a++)
            {
                var lod = additionalClothingLODs[a];
                generationSettings.additionalClothingLODs[a] = new BodyMoldBindings.GenerationSettings.LODEntry { mesh = lod.mesh, screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight };
            }
            for (int a = 0; a < bodyMeshAssets.Count; a++)
            {
                var body = bodyMeshAssets[a];
                generationSettings.bodyMeshAssets[a] = new BodyMoldBindings.GenerationSettings.BodyMeshEntry { renderer = BodyMoldBindings.SceneRendererRef.Create(body.renderer), maskOnly = body.maskOnly, weight = body.weight };
            }
            tempAsset.SetGenerationSettings(generationSettings);

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
            string path;
            if (editingAsset != null)
            {
                path = AssetDatabase.GetAssetPath(editingAsset);
            }
            else
            {
                path = EditorUtility.SaveFilePanelInProject("Save Bindings", clothingRenderer.name + "_Bindings", "asset", "Choose where to save the bindings asset", defaultSaveDir);
            }
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(tempAsset); 
                return;
            }
             
            if (tempAsset.LodCount > 0)
            {
                for (int a = 0; a < tempAsset.LodCount; a++) tempAsset.GetMeshLOD(a); // force generation of edited meshes before saving
            }

            // Check if asset already exists at this path
            var existingAssetAtPath = AssetDatabase.LoadAssetAtPath<BodyMoldBindings>(path);

            if (existingAssetAtPath != null)
            {
                // Use reflection to copy all fields from tempAsset to existingAssetAtPath
                var type = typeof(BodyMoldBindings);
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // Skip Unity internal fields
                    if (field.Name.StartsWith("m_") && field.DeclaringType == typeof(UnityEngine.Object)) 
                        continue;

                    var value = field.GetValue(tempAsset);
                    field.SetValue(existingAssetAtPath, value);
                }

                EditorUtility.SetDirty(existingAssetAtPath);
                DestroyImmediate(tempAsset);
                editingAsset = existingAssetAtPath;
                Selection.activeObject = existingAssetAtPath;
            }
            else
            {
                AssetDatabase.CreateAsset(tempAsset, path);
                editingAsset = tempAsset;
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
