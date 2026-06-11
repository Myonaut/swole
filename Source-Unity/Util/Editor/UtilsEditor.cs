#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Swole.Unity.Editor
{
    public static class UtilsEditor
    {
        /// <summary>
        /// Finds and returns all project assets of a specified type.
        /// </summary>
        public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
        {
            List<T> assetList = new List<T>();

            // 1. Search using the type token "t:TypeName"
            // This returns the unique GUIDs of matching files
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                // 2. Convert the asset GUID to its relative project path
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 3. Load the actual asset from that path
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    assetList.Add(asset);
                }
            }

            return assetList;
        }

    }
}

#endif