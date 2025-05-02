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

    [CreateAssetMenu(fileName = "NewImageCollection", menuName = "Swole/Images/ImageCollection")]
    public class ImageCollection : ScriptableObject
    {

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (scanFolder)
            {
                scanFolder = false;

                string localFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

                List<InternalResources.Locator> validAssets = new List<InternalResources.Locator>();

                if (images != null)
                {
                    foreach (var asset in images)
                    {
                        if (string.IsNullOrEmpty(asset.path)) continue;
                        validAssets.Add(asset);
                    }
                }

                InternalResources.GetResourcesInFolder<Texture2D>(localFolder, includeChildFolders, validAssets);
                //InternalResources.GetResourcesInFolder<ImageAsset>(localFolder, includeChildFolders, validAssets);

                images = validAssets.ToArray();
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

        public ImageCollectionCategory category;

#if UNITY_EDITOR
        public bool scanFolder;
        public bool includeChildFolders;
#endif

        #region Materials

        [SerializeField]
        protected InternalResources.Locator[] images;
        public int ImageCount
        {
            get
            {
                int count = 0;

                if (images != null) count += images.Length;

                if (subCollections != null)
                {
                    foreach (var collection in subCollections) if (collection != null) count += collection.ImageCount;
                }

                return count;
            }
        }

        [NonSerialized]
        protected ImageAsset[] cachedAssets;

        public ImageAsset GetImage(int index)
        {
            if (index < 0) return null;

            if (images != null && index < images.Length)
            {
                var locator = images[index];

                if (cachedAssets == null) cachedAssets = new ImageAsset[images.Length];

                var swoleAsset = cachedAssets[index];
                if (swoleAsset != null) return swoleAsset;

                if (!string.IsNullOrWhiteSpace(locator.path))
                {
                    var unityAsset = Resources.Load<Texture2D>(locator.path);
                    if (unityAsset != null)
                    {
                        swoleAsset = new ImageAsset(default, unityAsset, false, default, TextureUtils.IsLinear(unityAsset));
                    }
                    else
                    {
                        //swoleAsset = Resources.Load<ImageAsset>(locator.path);
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
                    int i = images == null ? 0 : images.Length;
                    for (int a = 0; a < subCollections.Length; a++)
                    {
                        var subCollection = subCollections[a];
                        if (subCollection == null) continue;

                        for (int b = 0; b < subCollection.ImageCount; b++)
                        {
                            if (i == index) return subCollection.GetImage(b);
                            i++;
                        }
                    }
                }
            }

            return null;
        }
        public int IndexOfImage(string assetId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(assetId)) return -1;

            if (!caseSensitive) assetId = assetId.AsID();

            int i = 0;
            if (images != null)
            {
                for (int a = 0; a < images.Length; a++)
                {
                    var asset = images[a];
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

                        int c = subCollection.IndexOfImage(assetId, caseSensitive);
                        if (c >= 0) return i + c;

                        i += subCollection.ImageCount;
                    }
                }
            }

            return -1;
        }
        public ImageAsset LoadImage(string id, bool caseSensitive = false)
        {
            int index = IndexOfImage(id, caseSensitive);
            if (index < 0) return null;

            return GetImage(index);
        }
        public bool HasImage(string id, bool caseSensitive = false) => IndexOfImage(id, caseSensitive) >= 0;

        #endregion

        #region Sub Collection

        [SerializeField]
        protected ImageCollection[] subCollections;

        #endregion

    }
}

#endif