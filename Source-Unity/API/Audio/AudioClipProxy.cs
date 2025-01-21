#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public interface IAudioClipProxy : IAudioAsset
    {
        public AudioClip Clip { get; set; }
    }
    [Serializable, CreateAssetMenu(fileName = "audioClipProxy", menuName = "Audio/AudioClipProxy", order = 1)]
    public class AudioClipProxy : ScriptableObject, IAudioClipProxy
    {

        public System.Type AssetType => typeof(AudioClip);
        public object Asset => clip;

        protected bool disposed;
        public bool isNotInternalAsset;
        public bool IsInternalAsset
        {
            get => !isNotInternalAsset;
            set => isNotInternalAsset = !value;
        }

        public bool IsValid => !disposed;
        public void Dispose() 
        { 
            if (!disposed)
            {
                GameObject.Destroy(this);
            }
            disposed = true;
        }
        public void Delete() => Dispose();

        public AudioClip clip;
        public AudioClip Clip
        {
            get => clip;
            set => clip = value; 
        }

        public string collectionId;

        #region IContent

        public PackageInfo PackageInfo => default;
        public ContentInfo ContentInfo => default;

        public string Author => string.Empty;
        public string CreationDate => default;
        public string LastEditDate => default;
        public string Description => default;
        public string OriginPath => default;
        public string RelativePath => default;
        public string Name => clip == null ? string.Empty : clip.name;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => this;
        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public IContent SetOriginPath(string path) => this;
        public IContent SetRelativePath(string path) => this;

        #endregion
    }
    [Serializable]
    public struct TempAudioClipProxy : IAudioClipProxy
    {

        public System.Type AssetType => typeof(AudioClip);
        public object Asset => clip;

        public bool IsInternalAsset { get => true; set { } }

        public bool IsValid => clip != null;
        public void Dispose() { }
        public void Delete() => Dispose();

        public AudioClip clip;
        public AudioClip Clip
        {
            get => clip;
            set => clip = value;
        }

        public string collectionId;

        public TempAudioClipProxy(AudioClip clip, string collectionId = null)
        {
            this.clip = clip;
            this.collectionId = collectionId;
        }

        #region IContent

        public PackageInfo PackageInfo => default;
        public ContentInfo ContentInfo => default;

        public string Author => string.Empty;
        public string CreationDate => default;
        public string LastEditDate => default;
        public string Description => default;
        public string OriginPath => default;
        public string RelativePath => default;
        public string Name => clip == null ? string.Empty : clip.name;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => this;
        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public IContent SetOriginPath(string path) => this;
        public IContent SetRelativePath(string path) => this;

        #endregion
    }
}

#endif