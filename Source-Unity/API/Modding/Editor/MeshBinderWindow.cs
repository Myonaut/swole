#if UNITY_2017_1_OR_NEWER

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

        [MenuItem("Swole/Mesh Binding Baker")]
        public static void ShowWindow()
        {
            GetWindow<MeshBinderWindow>("Mesh Baker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Mesh Binding Baker", EditorStyles.boldLabel);
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
            string path = EditorUtility.SaveFilePanelInProject("Save Binding Data", sourceMesh.name + "_ProxyBinding", "asset", "Save your binding data asset", defaultSaveDir); 
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
                    Selection.activeObject = existingAsset;
                }
                else
                {
                    AssetDatabase.CreateAsset(asset, path);
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