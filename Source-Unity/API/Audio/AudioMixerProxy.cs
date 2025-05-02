#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System;

using UnityEngine;
using UnityEngine.Audio;

namespace Swole
{
    public interface IAudioMixerProxy : IAudioMixer
    {
        public AudioMixer Mixer { get; set; }
    }
    [Serializable, CreateAssetMenu(fileName = "audioMixerProxy", menuName = "Swole/Audio/AudioMixerProxy", order = 2)]
    public class AudioMixerProxy : ScriptableObject, IAudioMixerProxy
    {
        public AudioMixer mixer; 
        public AudioMixer Mixer
        {
            get => mixer;
            set => mixer = value;
        }

        [SerializeField]
        protected string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(CollectionID);

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

        public string Name => mixer == null ? string.Empty : mixer.name;

        public System.Type AssetType => typeof(AudioMixer);
        public object Asset => mixer;

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
        public void DisposeSelf() => Dispose();
        public void Delete() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => ReferenceEquals(this, asset);

        #endregion

    }

    [Serializable]
    public struct TempAudioMixerProxy : IAudioMixerProxy
    {
        public AudioMixer mixer;
        public AudioMixer Mixer
        {
            get => mixer;
            set => mixer = value;
        }

        public string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(collectionId);

        public TempAudioMixerProxy(AudioMixer mixer, string collectionId = null)
        {
            this.mixer = mixer;
            this.collectionId = collectionId;
        }

        #region IEngineObject

        public string name => mixer == null ? string.Empty : mixer.name;

        public object Instance => mixer;
        public int InstanceID => mixer == null ? 0 : mixer.GetInstanceID();

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public bool IsDestroyed => mixer == null;

        public void AdminDestroy(float timeDelay = 0) { } // don't destroy mixers... right?
        public void Destroy(float timeDelay = 0) { }  // don't destroy mixers... right?

        #endregion

        #region ISwoleAsset

        public string Name => mixer == null ? string.Empty : mixer.name;

        public System.Type AssetType => typeof(AudioMixer);
        public object Asset => mixer;

        public bool IsInternalAsset { get => true; set { } }

        public bool IsValid => mixer != null;
        public void Dispose() { }
        public void DisposeSelf() { }
        public void Delete() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => (asset is IAudioMixer x && ReferenceEquals(x.Instance, Instance));

        #endregion

    }
}

#endif