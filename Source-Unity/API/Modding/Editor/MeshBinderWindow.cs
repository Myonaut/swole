#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Swole.Modding
{

    public class MeshBinderWindow : EditorWindow
    {
        private Mesh sourceMesh;
        private Mesh targetMesh;

        private float normalPenalty;

        private Vector3 sourceToTargetRot; 
        private bool useCustomSkinning;

        private MeshBindingData editingAsset;

        [MenuItem("Swole/Mesh Binding Baker")]
        public static void ShowWindow()
        {
            GetWindow<MeshBinderWindow>("Mesh Baker");
        }

        [MenuItem("Swole/Open All Bound Mesh Assets")]
        public static void OpenAllBoundMeshAssets()
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(MeshBindingData).Name}");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                MeshBindingData asset = AssetDatabase.LoadAssetAtPath<MeshBindingData>(assetPath);

                if (asset != null)
                {
                    MeshBinderWindow window = ScriptableObject.CreateInstance<MeshBinderWindow>();

                    window.titleContent = new GUIContent(asset.name);
                    window.Show();
                    window.LoadFromAsset(asset); 
                }
            }
        }

        [MenuItem("CONTEXT/MeshBindingData/Edit Binding Data")]
        public static void EditBindingData(MenuCommand command)
        {
            var data = command.context as MeshBindingData;
            if (data == null) return;

            var window = GetWindow<MeshBinderWindow>("Mesh Baker");
            window.LoadFromAsset(data);
        }

        private void LoadFromAsset(MeshBindingData asset)
        {
            if (asset == null) return;

            editingAsset = asset;
            var settings = asset.GetGenerationSettings();
            if (settings == null) return;

            sourceMesh = asset.OriginalMesh;
            targetMesh = settings.targetMesh;
            normalPenalty = settings.normalPenalty;
            sourceToTargetRot = asset.eulerOffset;
            useCustomSkinning = asset.useCustomSkinning;
        }

        private void OnGUI()
        {
            GUILayout.Label("Mesh Binding Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            var newEditingAsset = (MeshBindingData)EditorGUILayout.ObjectField("Load Existing Asset", editingAsset, typeof(MeshBindingData), false);
            if (EditorGUI.EndChangeCheck() && newEditingAsset != editingAsset)
            {
                if (newEditingAsset != null) LoadFromAsset(newEditingAsset); else editingAsset = null;
            }

            EditorGUILayout.Space();

            sourceMesh = (Mesh)EditorGUILayout.ObjectField("Source Mesh", sourceMesh, typeof(Mesh), true);
            targetMesh = (Mesh)EditorGUILayout.ObjectField("Bind Target Mesh", targetMesh, typeof(Mesh), true); 

            normalPenalty = EditorGUILayout.FloatField("Normal Penalty", normalPenalty);

            sourceToTargetRot = EditorGUILayout.Vector3Field("Source to Target Rotation", sourceToTargetRot);

            useCustomSkinning = EditorGUILayout.Toggle("Use Custom Skinning", useCustomSkinning);

            EditorGUILayout.Space();

            if (GUILayout.Button("Bake Binding Data"))
            {
                if (sourceMesh == null || targetMesh == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both source and target meshes.", "OK");
                    return;
                }

                BakeData();
            }
        }

        private void BakeData()
        {
            EditorUtility.DisplayProgressBar("Baking Mesh Binding Data", "Running parallel proximity job...", 0.5f);

            var asset = MeshDataTools.GenerateBindingDataAsset(sourceMesh, targetMesh, normalPenalty, sourceToTargetRot);
            asset.useCustomSkinning = useCustomSkinning;
            asset.SetGenerationSettings(new MeshBindingData.GenerationSettings
            {
                targetMesh = targetMesh,
                normalPenalty = normalPenalty
            });

            EditorUtility.ClearProgressBar();

            string defaultSaveDir = null;
            if (sourceMesh != null)
            {
                string meshPath = AssetDatabase.GetAssetPath(sourceMesh);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    defaultSaveDir = System.IO.Path.GetDirectoryName(meshPath);
                }
            }

            // Save the Asset file
            string path;
            if (editingAsset != null)
            {
                path = AssetDatabase.GetAssetPath(editingAsset);
            }
            else
            {
                path = EditorUtility.SaveFilePanelInProject("Save Binding Data", sourceMesh.name + "_ProxyBinding", "asset", "Save your binding data asset", defaultSaveDir);
            }
            if (!string.IsNullOrEmpty(path))
            {
                // Check if asset already exists at this path
                var existingAsset = AssetDatabase.LoadAssetAtPath<MeshBindingData>(path);

                if (existingAsset != null)
                {
                    // Use reflection to copy all fields from asset to existingAsset
                    var type = typeof(MeshBindingData);
                    var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var field in fields)
                    {
                        // Skip Unity internal fields
                        if (field.Name.StartsWith("m_") && field.DeclaringType == typeof(UnityEngine.Object))
                            continue;

                        var value = field.GetValue(asset);
                        field.SetValue(existingAsset, value);
                    }

                    EditorUtility.SetDirty(existingAsset);
                    DestroyImmediate(asset);
                    editingAsset = existingAsset;
                    Selection.activeObject = existingAsset;
                }
                else
                {
                    AssetDatabase.CreateAsset(asset, path);
                    editingAsset = asset;
                    Selection.activeObject = asset;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"Successfully baked {asset.bindings.Length} vertices to asset!", "OK");
            }
        }
    }

}

#endif