#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "NewAudioCollection", menuName = "Audio/AudioCollection", order = 3)]
    public class AudioCollection : ScriptableObject
    {

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

        //public AudioClip[] clips;
        [SerializeField]
        protected InternalResources.Locator[] clips;
        public int ClipCount => clips == null ? 0 : clips.Length;

        [NonSerialized]
        protected AudioClipProxy[] cachedProxies;

        public AudioClipProxy GetAudioClip(int index)
        {
            if (clips == null || index < 0 || index >= clips.Length) return null;

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
                proxy.collectionId = id;
                return proxy;
            }

            return null;
        }
        public int IndexOf(string clipId, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(clipId)) return -1;

            if (!caseSensitive) clipId = clipId.AsID();
            for (int a = 0; a < ClipCount; a++)
            {
                var asset = clips[a];
                if (string.IsNullOrWhiteSpace(asset.id) || (caseSensitive ? asset.id : asset.id.AsID()) != clipId) continue;
                return a;
            }

            return -1;
        }
        public AudioClipProxy LoadAudioClip(string id, bool caseSensitive = false)
        {
            int index = IndexOf(id, caseSensitive);
            if (index < 0) return null;

            return GetAudioClip(index);
        }
        public bool HasAudioClip(string id, bool caseSensitive = false) => IndexOf(id, caseSensitive) >= 0;

    }
}

#endif