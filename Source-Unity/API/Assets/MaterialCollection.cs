#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewMaterialCollection", menuName = "Swole/Materials/MaterialCollection")]
    public class MaterialCollection : ScriptableObject
    {

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (scanFolder)
            {
                scanFolder = false;

                string localFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

                List<InternalResources.Locator> validAssets = new List<InternalResources.Locator>();

                if (materials != null)
                {
                    foreach (var asset in materials)
                    {
                        if (string.IsNullOrEmpty(asset.path)) continue; 
                        validAssets.Add(asset);
                    }
                }

                InternalResources.GetResourcesInFolder<Material>(localFolder, includeChildFolders, validAssets);
                InternalResources.GetResourcesInFolder<MaterialAsset>(localFolder, includeChildFolders, validAssets);

                materials = validAssets.ToArray();
                validAssets.Clear();
            }
        }
#endif

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

        public MaterialCollectionCategory category;

#if UNITY_EDITOR
        public bool scanFolder;
        public bool includeChildFolders;
#endif

        #region Materials

        [SerializeField]
        protected InternalResources.Locator[] materials;
        public int MaterialCount
        {
            get
            {
                int count = 0;

                if (materials != null) count += materials.Length;

                if (subCollections != null)
                {
                    foreach (var collection in subCollections) if (collection != null) count += collection.MaterialCount;
                }

                return count;
            }
        }

        [NonSerialized]
        protected MaterialAsset[] cachedAssets;

        public MaterialAsset GetMaterial(int index)
        {
            if (index < 0) return null;

            if (materials != null && index < materials.Length)
            {
                var locator = materials[index];

                if (cachedAssets == null) cachedAssets = new MaterialAsset[materials.Length];

                var swoleAsset = cachedAssets[index];
                if (swoleAsset != null) return swoleAsset;

                if (!string.IsNullOrWhiteSpace(locator.path))
                {
                    var unityAsset = Resources.Load<Material>(locator.path);
                    if (unityAsset != null)
                    {
                        swoleAsset = MaterialAsset.NewInstance(default, unityAsset);
                    } 
                    else
                    {
                        swoleAsset = Resources.Load<MaterialAsset>(locator.path);
                    }

                    if (swoleAsset != null)
                    {
                        swoleAsset.CollectionID = id;
                        swoleAsset.IsInternalAsset = true;
                    }

                    cachedAssets[index] = swoleAsset;

                    return swoleAsset;
                }
            }
            else
            {
                if (subCollections != null)
                {
                    int i = materials == null ? 0 : materials.Length;
                    for (int a = 0; a < subCollections.Length; a++)
                    {
                        var subCollection = subCollections[a];
                        if (subCollection == null) continue;

                        for (int b = 0; b < subCollection.MaterialCount; b++)
                        {
                            if (i == index) return subCollection.GetMaterial(b);
                            i++;
                        }
                    }
                }
            }

            return null;
        }
        public int IndexOfMaterial(string assetId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(assetId)) return -1;

            if (!caseSensitive) assetId = assetId.AsID();

            int i = 0;
            if (materials != null)
            {
                for (int a = 0; a < materials.Length; a++)
                {
                    var asset = materials[a];
                    if (string.IsNullOrWhiteSpace(asset.id) || (caseSensitive ? asset.id : asset.id.AsID()) != assetId)
                    {
                        i++;
                        continue;
                    }

                    return i;
                }

                if (subCollections != null)
                {
                    for (int b = 0; b < subCollections.Length; b++)
                    {
                        var subCollection = subCollections[b];
                        if (subCollection == null) continue;

                        int c = subCollection.IndexOfMaterial(assetId, caseSensitive);
                        if (c >= 0) return i + c;

                        i += subCollection.MaterialCount;
                    }
                }
            }

            return -1;
        }
        public MaterialAsset LoadMaterial(string id, bool caseSensitive = false)
        {
            int index = IndexOfMaterial(id, caseSensitive);
            if (index < 0) return null;

            return GetMaterial(index);
        }
        public bool HasMaterial(string id, bool caseSensitive = false) => IndexOfMaterial(id, caseSensitive) >= 0;

        #endregion

        #region Sub Collection

        [SerializeField]
        protected MaterialCollection[] subCollections;

        #endregion

    }
}

#endif