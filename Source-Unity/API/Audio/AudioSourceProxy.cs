#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{

    public interface IAudioSourceProxy : IAudioSource
    {
        public AudioSource Source { get; set; }  
    }
    [Serializable]
    public struct AudioSourceProxy : IAudioSourceProxy
    {

        public Type EngineComponentType => GetType();

        #region IEngineObject

        public string name
        {
            get
            {
                if (source == null) return default;
                return source.name;
            }
            set
            {
                if (source == null) return;
                source.name = value;
            }
        }

        public EngineInternal.GameObject baseGameObject
        {
            get
            {
                if (source == null) return default;
                return UnityEngineHook.AsSwoleGameObject(source.gameObject);
            }
        }

        public object Instance => this;

        public int InstanceID
        {
            get
            {
                if (source == null) return default;
                return source.GetInstanceID();
            }
        }

        public bool IsDestroyed => source == null;

        public bool HasEventHandler => false;

        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(source, timeDelay);

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(source, timeDelay);

        #endregion

        [SwoleScriptIgnore]
        public AudioSource source;
        [SwoleScriptIgnore]
        public AudioSource Source
        {
            get => source;
            set => source = value;
        }

        public AudioSourceProxy(AudioSource source)
        {
            this.source = source;
        }

        public static bool operator ==(AudioSourceProxy lhs, object rhs)
        {
            if (ReferenceEquals(rhs, null)) return lhs.source == null;
            if (rhs is AudioSourceProxy ts) return lhs.source == ts.source;
            return ReferenceEquals(lhs.source, rhs);
        }
        public static bool operator !=(AudioSourceProxy lhs, object rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return source == null ? obj == null : source.Equals(obj);
        }
        public override int GetHashCode()
        {
            return source == null ? base.GetHashCode() : source.GetHashCode();
        }

        #region IAudioSource

        public bool bypassEffects
        {
            get
            {
                if (source == null) return default;
                return source.bypassEffects;
            }
            set
            {
                if (source == null) return;
                source.bypassEffects = value;
            }
        }
        public bool bypassListenerEffects
        {
            get
            {
                if (source == null) return default;
                return source.bypassListenerEffects;
            }
            set
            {
                if (source == null) return;
                source.bypassListenerEffects = value;
            }
        }
        public bool bypassReverbZones
        {
            get
            {
                if (source == null) return default;
                return source.bypassReverbZones;
            }
            set
            {
                if (source == null) return;
                source.bypassReverbZones = value;
            }
        }

        [SwoleScriptIgnore]
        public IAudioAsset clip
        {
            get
            {
                if (source == null) return default;
                return new TempAudioClipProxy(source.clip);
            }
            set
            {
                if (source == null) return;
                if (value is IAudioClipProxy prox) source.clip = prox.Clip;
            }
        }

        public float dopplerLevel
        {
            get
            {
                if (source == null) return default;
                return source.dopplerLevel;
            }
            set
            {
                if (source == null) return;
                source.dopplerLevel = value;
            }
        }
        public bool ignoreListenerPause
        {
            get
            {
                if (source == null) return default;
                return source.ignoreListenerPause;
            }
            set
            {
                if (source == null) return;
                source.ignoreListenerPause = value;
            }
        }
        public bool ignoreListenerVolume
        {
            get
            {
                if (source == null) return default;
                return source.ignoreListenerVolume;
            }
            set
            {
                if (source == null) return;
                source.ignoreListenerVolume = value;
            }
        }
        public bool isPlaying
        {
            get
            {
                if (source == null) return default;
                return source.isPlaying;
            }
        }
        public bool isVirtual
        {
            get
            {
                if (source == null) return default;
                return source.isVirtual;
            }
        }
        public bool loop
        {
            get
            {
                if (source == null) return default;
                return source.loop;
            }
            set
            {
                if (source == null) return;
                source.loop = value;
            }
        }
        public float maxDistance
        {
            get
            {
                if (source == null) return default;
                return source.maxDistance;
            }
            set
            {
                if (source == null) return;
                source.maxDistance = value;
            }
        }
        public float minDistance
        {
            get
            {
                if (source == null) return default;
                return source.minDistance;
            }
            set
            {
                if (source == null) return;
                source.minDistance = value;
            }
        }
        public bool mute
        {
            get
            {
                if (source == null) return default;
                return source.mute;
            }
            set
            {
                if (source == null) return;
                source.mute = value;
            }
        }

        [SwoleScriptIgnore]
        public IAudioMixer outputAudioMixerGroup
        {
            get
            {
                if (source == null) return default;
                return new TempAudioMixerProxy(source.outputAudioMixerGroup);
            }
            set
            {
                if (source == null) return;
                if (value is IAudioMixerProxy prox) source.outputAudioMixerGroup = prox.Mixer;
            }
        }

        public float panStereo
        {
            get
            {
                if (source == null) return default;
                return source.panStereo;
            }
            set
            {
                if (source == null) return;
                source.panStereo = value;
            }
        }
        public float pitch
        {
            get
            {
                if (source == null) return default;
                return source.pitch;
            }
            set
            {
                if (source == null) return;
                source.pitch = value;
            }
        }
        public int priority
        {
            get
            {
                if (source == null) return default;
                return source.priority;
            }
            set
            {
                if (source == null) return;
                source.priority = value;
            }
        }
        public float reverbZoneMix
        {
            get
            {
                if (source == null) return default;
                return source.reverbZoneMix;
            }
            set
            {
                if (source == null) return;
                source.reverbZoneMix = value;
            }
        }
        public string rolloffMode
        {
            get
            {
                if (source == null) return default;
                return source.rolloffMode.ToString();
            }
            set
            {
                if (source == null) return;
                if (Enum.TryParse<AudioRolloffMode>(value, out var res)) source.rolloffMode = res;
            }
        }
        public float spatialBlend
        {
            get
            {
                if (source == null) return default;
                return source.spatialBlend;
            }
            set
            {
                if (source == null) return;
                source.spatialBlend = value;
            }
        }
        public float spread
        {
            get
            {
                if (source == null) return default;
                return source.spread;
            }
            set
            {
                if (source == null) return;
                source.spread = value;
            }
        }
        public float time
        {
            get
            {
                if (source == null) return default;
                return source.time;
            }
            set
            {
                if (source == null) return;
                source.time = value;
            }
        }
        public int timeSamples
        {
            get
            {
                if (source == null) return default;
                return source.timeSamples;
            }
            set
            {
                if (source == null) return;
                source.timeSamples = value;
            }
        }
        public string velocityUpdateMode
        {
            get
            {
                if (source == null) return default;
                return source.velocityUpdateMode.ToString();
            }
            set
            {
                if (source == null) return;
                if (Enum.TryParse<AudioVelocityUpdateMode>(value, out var res)) source.velocityUpdateMode = res;
            }
        }
        public float volume
        {
            get
            {
                if (source == null) return default;
                return source.volume;
            }
            set
            {
                if (source == null) return;
                source.volume = value;
            }
        }

        public void Pause()
        {
            if (source == null) return;
            source.Pause();
        }
        public void Play(int delay = 0)
        {
            if (source == null) return;
            source.Play((ulong)delay);
        }
        public void PlayDelayed(float delay)
        {
            if (source == null) return;
            source.PlayDelayed(delay);
        }
        public void PlayOneShot(IAudioAsset asset, float volumeScale = 1.0F)
        {
            if (source == null) return;
            if (asset is IAudioClipProxy prox && prox.Clip != null) source.PlayOneShot(prox.Clip, volumeScale);  
        }
        public void PlayScheduled(double time)
        {
            if (source == null) return;
            source.PlayScheduled(time);
        }
        public void SetScheduledEndTime(double time)
        {
            if (source == null) return;
            source.SetScheduledEndTime(time);
        }
        public void SetScheduledStartTime(double time)
        {
            if (source == null) return;
            source.SetScheduledStartTime(time);
        }
        public void Stop()
        {
            if (source == null) return;
            source.Stop();
        }
        public void UnPause()
        {
            if (source == null) return;
            source.UnPause();
        }

        #endregion

    }
    public class ExternalAudioSource : MonoBehaviour, IAudioSourceProxy
    {

        public Type EngineComponentType => GetType();

        #region IEngineObject

        public EngineInternal.GameObject baseGameObject
        {
            get
            {
                return UnityEngineHook.AsSwoleGameObject(gameObject);
            }
        }

        public object Instance => this;

        public int InstanceID
        {
            get
            {
                return GetInstanceID();
            }
        }

        public bool IsDestroyed => proxy.source == null || this == null;

        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0) 
        {
            proxy.Destroy(timeDelay);
            swole.Engine.Object_Destroy(this, timeDelay); 
        }
        public void AdminDestroy(float timeDelay = 0) 
        {
            proxy.AdminDestroy(timeDelay);
            swole.Engine.Object_AdminDestroy(this, timeDelay); 
        }

        #endregion

        [SwoleScriptIgnore]
        public AudioSourceProxy proxy;

        #region IAudioSourceProxy

        [SwoleScriptIgnore]
        public AudioSource Source
        {
            get => proxy.source;
            set => proxy.source = value;
        }

        #endregion

        #region IAudioSource

        public bool bypassEffects
        {
            get
            {
                if (proxy == null) return default;
                return proxy.bypassEffects;
            }
            set
            {
                if (proxy == null) return;
                proxy.bypassEffects = value;
            }
        }
        public bool bypassListenerEffects
        {
            get
            {
                if (proxy == null) return default;
                return proxy.bypassListenerEffects;
            }
            set
            {
                if (proxy == null) return;
                proxy.bypassListenerEffects = value;
            }
        }
        public bool bypassReverbZones
        {
            get
            {
                if (proxy == null) return default;
                return proxy.bypassReverbZones;
            }
            set
            {
                if (proxy == null) return;
                proxy.bypassReverbZones = value;
            }
        }

        [SwoleScriptIgnore]
        public IAudioAsset clip
        {
            get
            {
                if (proxy == null) return default;
                return proxy.clip;
            }
            set
            {
                if (proxy == null) return;
                proxy.clip = value;
            }
        }

        public float dopplerLevel
        {
            get
            {
                if (proxy == null) return default;
                return proxy.dopplerLevel;
            }
            set
            {
                if (proxy == null) return;
                proxy.dopplerLevel = value;
            }
        }
        public bool ignoreListenerPause
        {
            get
            {
                if (proxy == null) return default;
                return proxy.ignoreListenerPause;
            }
            set
            {
                if (proxy == null) return;
                proxy.ignoreListenerPause = value;
            }
        }
        public bool ignoreListenerVolume
        {
            get
            {
                if (proxy == null) return default;
                return proxy.ignoreListenerVolume;
            }
            set
            {
                if (proxy == null) return;
                proxy.ignoreListenerVolume = value;
            }
        }
        public bool isPlaying
        {
            get
            {
                if (proxy == null) return default;
                return proxy.isPlaying;
            }
        }
        public bool isVirtual
        {
            get
            {
                if (proxy == null) return default;
                return proxy.isVirtual;
            }
        }
        public bool loop
        {
            get
            {
                if (proxy == null) return default;
                return proxy.loop;
            }
            set
            {
                if (proxy == null) return;
                proxy.loop = value;
            }
        }
        public float maxDistance
        {
            get
            {
                if (proxy == null) return default;
                return proxy.maxDistance;
            }
            set
            {
                if (proxy == null) return;
                proxy.maxDistance = value;
            }
        }
        public float minDistance
        {
            get
            {
                if (proxy == null) return default;
                return proxy.minDistance;
            }
            set
            {
                if (proxy == null) return;
                proxy.minDistance = value;
            }
        }
        public bool mute
        {
            get
            {
                if (proxy == null) return default;
                return proxy.mute;
            }
            set
            {
                if (proxy == null) return;
                proxy.mute = value;
            }
        }

        [SwoleScriptIgnore]
        public IAudioMixer outputAudioMixerGroup
        {
            get
            {
                if (proxy == null) return default;
                return proxy.outputAudioMixerGroup;
            }
            set
            {
                if (proxy == null) return;
                proxy.outputAudioMixerGroup = value;
            }
        }

        public float panStereo
        {
            get
            {
                if (proxy == null) return default;
                return proxy.panStereo;
            }
            set
            {
                if (proxy == null) return;
                proxy.panStereo = value;
            }
        }
        public float pitch
        {
            get
            {
                if (proxy == null) return default;
                return proxy.pitch;
            }
            set
            {
                if (proxy == null) return;
                proxy.pitch = value;
            }
        }
        public int priority
        {
            get
            {
                if (proxy == null) return default;
                return proxy.priority;
            }
            set
            {
                if (proxy == null) return;
                proxy.priority = value;
            }
        }
        public float reverbZoneMix
        {
            get
            {
                if (proxy == null) return default;
                return proxy.reverbZoneMix;
            }
            set
            {
                if (proxy == null) return;
                proxy.reverbZoneMix = value;
            }
        }
        public string rolloffMode
        {
            get
            {
                if (proxy == null) return default;
                return proxy.rolloffMode;
            }
            set
            {
                if (proxy == null) return;
                proxy.rolloffMode = value;
            }
        }
        public float spatialBlend
        {
            get
            {
                if (proxy == null) return default;
                return proxy.spatialBlend;
            }
            set
            {
                if (proxy == null) return;
                proxy.spatialBlend = value;
            }
        }
        public float spread
        {
            get
            {
                if (proxy == null) return default;
                return proxy.spread;
            }
            set
            {
                if (proxy == null) return;
                proxy.spread = value;
            }
        }
        public float time
        {
            get
            {
                if (proxy == null) return default;
                return proxy.time;
            }
            set
            {
                if (proxy == null) return;
                proxy.time = value;
            }
        }
        public int timeSamples
        {
            get
            {
                if (proxy == null) return default;
                return proxy.timeSamples;
            }
            set
            {
                if (proxy == null) return;
                proxy.timeSamples = value;
            }
        }
        public string velocityUpdateMode
        {
            get
            {
                if (proxy == null) return default;
                return proxy.velocityUpdateMode;
            }
            set
            {
                if (proxy == null) return;
                proxy.velocityUpdateMode = value;
            }
        }
        public float volume
        {
            get
            {
                if (proxy == null) return default;
                return proxy.volume;
            }
            set
            {
                if (proxy == null) return;
                proxy.volume = value;
            }
        }
         
        public void Pause()
        {
            if (proxy == null) return;
            proxy.Pause();
        }
        public void Play(int delay = 0)
        {
            if (proxy == null) return;
            proxy.Play(delay);
        }
        public void PlayDelayed(float delay)
        {
            if (proxy == null) return;
            proxy.PlayDelayed(delay);
        }
        public void PlayOneShot(IAudioAsset asset, float volumeScale = 1.0F)
        {
            if (proxy == null) return;
            proxy.PlayOneShot(asset, volumeScale);
        }
        public void PlayScheduled(double time)
        {
            if (proxy == null) return;
            proxy.PlayScheduled(time);
        }
        public void SetScheduledEndTime(double time)
        {
            if (proxy == null) return;
            proxy.SetScheduledEndTime(time);
        }
        public void SetScheduledStartTime(double time)
        {
            if (proxy == null) return;
            proxy.SetScheduledStartTime(time);
        }
        public void Stop()
        {
            if (proxy == null) return;
            proxy.Stop();
        }
        public void UnPause()
        {
            if (proxy == null) return;
            proxy.UnPause();
        }

        #endregion
    }
}

#endif