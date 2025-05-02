#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System;

using UnityEngine;
using UnityEngine.Audio;

namespace Swole
{
    public interface IAudioMixerGroupProxy : IAudioMixerGroup
    {
        public AudioMixerGroup MixerGroup { get; set; }
    }
    [Serializable, CreateAssetMenu(fileName = "audioMixerGroupProxy", menuName = "Swole/Audio/AudioMixerGroupProxy", order = 3)]
    public class AudioMixerGroupProxy : ScriptableObject, IAudioMixerGroupProxy
    {
        public AudioMixerGroup mixerGroup; 
        public AudioMixerGroup MixerGroup
        {
            get => mixerGroup;
            set => mixerGroup = value;
        }

        #region IEngineObject

        public object Instance => this;
        public int InstanceID => GetInstanceID();

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public bool IsDestroyed => this == null;

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(this, timeDelay);
        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(this, timeDelay);

        #endregion

        #region ISwoleAsset

        public string Name => mixerGroup == null ? string.Empty : mixerGroup.name;

        public System.Type AssetType => typeof(AudioMixerGroup);
        public object Asset => mixerGroup;

        protected bool disposed;
        public bool isNotInternalAsset;
        public bool IsInternalAsset
        {
            get => !isNotInternalAsset;
            set => isNotInternalAsset = !value;
        }

        [SerializeField]
        protected string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(CollectionID);

        public bool IsValid => !disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                GameObject.Destroy(this); 
            }
            disposed = true;
        }
        public void DisposeSelf() => Dispose();
        public void Delete() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => ReferenceEquals(this, asset);

        #endregion

    }

    [Serializable]
    public struct TempAudioMixerGroupProxy : IAudioMixerGroupProxy
    {
        public AudioMixerGroup mixerGroup;
        public AudioMixerGroup MixerGroup
        {
            get => mixerGroup;
            set => mixerGroup = value;
        }
        public TempAudioMixerGroupProxy(AudioMixerGroup mixer)
        {
            this.mixerGroup = mixer;
        }

        #region IEngineObject

        public string name => mixerGroup == null ? string.Empty : mixerGroup.name;

        public object Instance => mixerGroup;
        public int InstanceID => mixerGroup == null ? 0 : mixerGroup.GetInstanceID();

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public bool IsDestroyed => mixerGroup == null;

        public void AdminDestroy(float timeDelay = 0) { } // don't destroy mixers... right?
        public void Destroy(float timeDelay = 0) { }  // don't destroy mixers... right?

        #endregion

        #region ISwoleAsset

        public string Name => mixerGroup == null ? string.Empty : mixerGroup.name;

        public System.Type AssetType => typeof(AudioMixerGroup);
        public object Asset => mixerGroup;

        public bool IsInternalAsset { get => true; set { } }

        public string CollectionID
        {
            get => string.Empty;
            set { }
        }
        public bool HasCollectionID => false;

        public bool IsValid => mixerGroup != null;
        public void Dispose() { }
        public void DisposeSelf() { }
        public void Delete() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => (asset is IAudioMixerGroup x && ReferenceEquals(x.Instance, Instance));

        #endregion

    }
}

#endif