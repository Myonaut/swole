#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewAudioCollection", menuName = "Swole/Audio/AudioCollection", order = 3)]
    public class AudioCollection : ScriptableObject
    {

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (scanFolder)
            {
                scanFolder = false;

                string localFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));

                List<InternalResources.Locator> validAssets = new List<InternalResources.Locator>();

                if (clips != null)
                {
                    foreach (var clip in clips)
                    {
                        if (string.IsNullOrEmpty(clip.path)) continue;
                        validAssets.Add(clip);
                    }
                }
                InternalResources.GetResourcesInFolder<AudioClip>(localFolder, includeChildFolders, validAssets);
                clips = validAssets.ToArray();

                validAssets.Clear();
                if (mixers != null)
                {
                    foreach (var mixer in mixers)
                    {
                        if (string.IsNullOrEmpty(mixer.path)) continue;
                        validAssets.Add(mixer);
                    }
                }
                InternalResources.GetResourcesInFolder<AudioMixer>(localFolder, includeChildFolders, validAssets);
                mixers = validAssets.ToArray();

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

        public AudioCollectionCategory category;

#if UNITY_EDITOR
        public bool scanFolder;
        public bool includeChildFolders;
#endif

        #region Audio Clips

        //public AudioClip[] clips;
        [SerializeField]
        protected InternalResources.Locator[] clips;
        public int ClipCount
        {
            get
            {
                int count = 0;

                if (clips != null) count += clips.Length;

                if (subCollections != null)
                {
                    foreach (var collection in subCollections) if (collection != null) count += collection.ClipCount;
                }

                return count;
            }
        }

        [NonSerialized]
        protected AudioClipProxy[] cachedProxies;

        public AudioClipProxy GetAudioClip(int index)
        {
            if (index < 0) return null;

            if (clips != null && index < clips.Length)
            {
                var locator = clips[index];
                if (!string.IsNullOrWhiteSpace(locator.path))
                {
                    var clip = Resources.Load<AudioClip>(locator.path);
                    if (clip == null) return null;

                    if (cachedProxies == null) cachedProxies = new AudioClipProxy[clips.Length];
                    var proxy = cachedProxies[index];
                    if (proxy == null)
                    {
                        proxy = ScriptableObject.CreateInstance<AudioClipProxy>();
                        cachedProxies[index] = proxy;
                    }
                    proxy.clip = clip;

                    proxy.CollectionID = id;
                    proxy.IsInternalAsset = true;

                    return proxy;
                }
            } 
            else
            {
                if (subCollections != null)
                {
                    int i = clips == null ? 0 : clips.Length;
                    for(int a = 0; a < subCollections.Length; a++)
                    {
                        var subCollection = subCollections[a];
                        if (subCollection == null) continue;

                        for (int b = 0; b < subCollection.ClipCount; b++)
                        {
                            if (i == index) return subCollection.GetAudioClip(b);
                            i++;
                        }
                    }
                }
            }

            return null;
        }
        public int IndexOfClip(string clipId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(clipId)) return -1;

            if (!caseSensitive) clipId = clipId.AsID();

            int i = 0;
            if (clips != null)
            {
                for(int a = 0; a < clips.Length; a++)
                {
                    var asset = clips[a];
                    if (string.IsNullOrWhiteSpace(asset.id) || (caseSensitive ? asset.id : asset.id.AsID()) != clipId) 
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

                        int c = subCollection.IndexOfClip(clipId, caseSensitive);
                        if (c >= 0) return i + c;

                        i += subCollection.ClipCount;
                    }
                }
            }

            return -1;
        }
        public AudioClipProxy LoadAudioClip(string id, bool caseSensitive = false)
        {
            int index = IndexOfClip(id, caseSensitive);
            if (index < 0) return null;

            return GetAudioClip(index);
        }
        public bool HasAudioClip(string id, bool caseSensitive = false) => IndexOfClip(id, caseSensitive) >= 0;

        #endregion

        #region Audio Mixers
         
        [SerializeField]
        protected InternalResources.Locator[] mixers;
        public int MixerCount
        {
            get
            {
                int count = 0;

                if (mixers != null) count += mixers.Length;

                if (subCollections != null)
                {
                    foreach (var collection in subCollections) if (collection != null) count += collection.MixerCount;
                }

                return count;
            }
        }

        [NonSerialized]
        protected AudioMixerProxy[] cachedMixerProxies;

        public AudioMixerProxy GetAudioMixer(int index)
        {
            if (index < 0) return null;

            if (mixers != null && index < mixers.Length)
            {
                var locator = mixers[index];
                if (!string.IsNullOrWhiteSpace(locator.path))
                {
                    var mixer = Resources.Load<AudioMixer>(locator.path);
                    if (mixer == null) return null;

                    if (cachedMixerProxies == null) cachedMixerProxies = new AudioMixerProxy[mixers.Length];
                    var proxy = cachedMixerProxies[index];
                    if (proxy == null)
                    {
                        proxy = ScriptableObject.CreateInstance<AudioMixerProxy>();
                        cachedMixerProxies[index] = proxy;
                    }
                    proxy.mixer = mixer;

                    proxy.CollectionID = id;
                    //proxy.IsInternalAsset = true;

                    return proxy;
                }
            } 
            else
            {
                if (subCollections != null)
                {
                    int i = mixers == null ? 0 : mixers.Length;
                    for (int a = 0; a < subCollections.Length; a++)
                    {
                        var subCollection = subCollections[a];
                        if (subCollection == null) continue;

                        for (int b = 0; b < subCollection.MixerCount; b++)
                        {
                            if (i == index) return subCollection.GetAudioMixer(b);
                            i++;
                        }
                    }
                }
            }

            return null;
        }
        public int IndexOfMixer(string mixerId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(mixerId)) return -1;

            if (!caseSensitive) mixerId = mixerId.AsID();

            int i = 0;
            if (mixers != null)
            {
                for (int a = 0; a < mixers.Length; a++)
                {
                    var asset = mixers[a];
                    if (string.IsNullOrWhiteSpace(asset.id) || (caseSensitive ? asset.id : asset.id.AsID()) != mixerId)
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

                        int c = subCollection.IndexOfMixer(mixerId, caseSensitive);
                        if (c >= 0) return i + c;

                        i += subCollection.MixerCount; 
                    }
                }
            }

            return -1;
        }
        public AudioMixerProxy LoadAudioMixer(string id, bool caseSensitive = false)
        {
            int index = IndexOfMixer(id, caseSensitive);
            if (index < 0) return null;

            return GetAudioMixer(index);
        }
        public bool HasAudioMixer(string id, bool caseSensitive = false) => IndexOfMixer(id, caseSensitive) >= 0;

        #endregion

        #region Sub Collection

        [SerializeField]
        protected AudioCollection[] subCollections;

        #endregion

    }
}

#endif