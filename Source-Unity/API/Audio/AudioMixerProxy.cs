#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System;

using UnityEngine;
using UnityEngine.Audio;

namespace Swole
{
    public interface IAudioMixerProxy : IAudioMixer
    {
        public AudioMixerGroup Mixer { get; set; }
    }
    [Serializable, CreateAssetMenu(fileName = "audioMixerProxy", menuName = "Audio/AudioMixerProxy", order = 2)]
    public class AudioMixerProxy : ScriptableObject, IAudioMixerProxy
    {
        public AudioMixerGroup mixer; 
        public AudioMixerGroup Mixer
        {
            get => mixer;
            set => mixer = value;
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

    }

    [Serializable]
    public struct TempAudioMixerProxy : IAudioMixerProxy
    {
        public AudioMixerGroup mixer;
        public AudioMixerGroup Mixer
        {
            get => mixer;
            set => mixer = value;
        }
        public TempAudioMixerProxy(AudioMixerGroup mixer)
        {
            this.mixer = mixer;
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

    }
}

#endif