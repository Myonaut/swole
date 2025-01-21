#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity
{
     
    public class AudibleObject : MonoBehaviour, IAudibleObject
    {

        public System.Type EngineComponentType => GetType();

        #region IEngineObject

        public EngineInternal.GameObject baseGameObject => UnityEngineHook.AsSwoleGameObject(gameObject);

        public object Instance => this;
        public int InstanceID => GetInstanceID();

        public bool IsDestroyed => this == null;
        public bool HasEventHandler => false;
        public IRuntimeEventHandler EventHandler => null;

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(this, timeDelay);
        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(this, timeDelay);

        #endregion

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
                return source.clip;
            }
            set
            {
                if (source == null) return;
                source.clip = value;
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
        public IAudioMixerGroup outputAudioMixerGroup
        {
            get
            {
                if (source == null) return default;
                return source.outputAudioMixerGroup;
            }
            set
            {
                if (source == null) return;
                source.outputAudioMixerGroup = value;
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
                return source.rolloffMode;
            }
            set
            {
                if (source == null) return;
                source.rolloffMode = value;
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
                return source.velocityUpdateMode;
            }
            set
            {
                if (source == null) return;
                source.velocityUpdateMode = value;
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
            source.Play(delay);
        }
        public void PlayDelayed(float delay)
        {
            if (source == null) return;
            source.PlayDelayed(delay);
        }
        public void PlayOneShot(IAudioAsset asset, float volumeScale = 1.0F)
        {
            if (source == null) return;
            source.PlayOneShot(asset, volumeScale);
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

        public bool muted;

        public void SetMuted(bool muted)
        {

            this.muted = muted;

            m_source.Stop();

        }

        public AudioSource m_source;
        public IAudioSource source
        {
            get => new AudioSourceProxy(m_source);
            set
            {
                if (value is IAudioSourceProxy prox) m_source = prox.Source;
            }
        }

        public AudioClip[] m_clips;
        public IAudioAsset[] clips
        {
            get
            {
                if (m_clips == null) return null;
                var array = new IAudioAsset[m_clips.Length];
                for (int a = 0; a < m_clips.Length; a++) array[a] = new TempAudioClipProxy(m_clips[a]);
                return array;
            }
            set
            {
                if (value == null)
                {
                    m_clips = null;
                    return;
                }
                m_clips = new AudioClip[value.Length];
                for(int a = 0; a < value.Length; a++)
                {
                    if (value[a] is IAudioClipProxy prox) m_clips[a] = prox.Clip;
                }
            }
        }

        [System.Serializable]
        public class AudioBundle : IAudioBundle
        {

            public AudioClip m_clip;
            public IAudioAsset clip
            {
                get
                {
                    return new TempAudioClipProxy(m_clip);
                }
                set
                {
                    if (value is IAudioClipProxy prox) m_clip = prox.Clip;
                }
            }

            public float m_volume = 1;
            public float volume
            {
                get => m_volume;
                set => m_volume = value;
            }

            public float m_pitch = 1;
            public float pitch
            {
                get => pitch;
                set => pitch = value;
            }

            public bool m_stopBeforePlay = false;
            public bool stopBeforePlay
            {
                get => m_stopBeforePlay;
                set => m_stopBeforePlay = value;
            }

            public AudioSource m_source;
            public IAudioSource source
            {
                get => new AudioSourceProxy(m_source);
                set
                {
                    if (value is IAudioSourceProxy prox) m_source = prox.Source;
                }
            }

            public UnityEvent OnPlay;
            private event RuntimeEventListenerDelegate onPlay;
            public void InvokeOnPlay()
            {
                OnPlay?.Invoke();
                onPlay?.Invoke("play", 0, this);
            }

            public void SubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                onPlay += listener;
            }
            public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                onPlay -= listener;
            }

            public void SubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                onPlay += listener;
            }
            public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                onPlay -= listener;
            }
        }

        public AudioBundle[] m_bundles;
        public IAudioBundle[] bundles
        {
            get => m_bundles;
            set
            {
                if (value == null)
                {
                    m_bundles = null;
                    return;
                }
                m_bundles = new AudioBundle[value.Length];
                for (int a = 0; a < value.Length; a++)
                {
                    if (value[a] is AudioBundle bundle) m_bundles[a] = bundle;
                }
            }
        }


        private float defaultPitch;

        private float defaultVolume;

        private void Awake()
        {

            if (m_source == null) m_source = gameObject.GetComponent<AudioSource>();

            if (m_source == null) m_source = gameObject.AddComponent<AudioSource>();

            defaultPitch = m_source.pitch;

            defaultVolume = m_source.volume;

            if (m_bundles != null)
            {

                for (int a = 0; a < m_bundles.Length; a++)
                {

                    var bundle = m_bundles[a];

                    if (bundle.volume <= 0) Debug.LogWarning("Bundle " + a + " on AudibleObject '" + gameObject.name + "' has a volume of less than or equalt to zero and won't be heard.");
                    if (bundle.pitch <= 0) Debug.LogWarning("Bundle " + a + " on AudibleObject '" + gameObject.name + "' has a pitch of less than or equalt to zero and won't be heard.");

                }

            }

        }

#if SWOLE_ENV
        public void FadeVolume(float time, float targetVolume, IAudioSource audioSource = null, bool easeIn = false, bool easeOut = false) => FadeVolumeLT(time, targetVolume, audioSource is IAudioSourceProxy prox ? prox.Source : null, easeIn, easeOut);
        public LTDescr FadeVolumeLT(float time, float targetVolume, AudioSource audioSource = null, bool easeIn = false, bool easeOut = false)
        {

            if (audioSource == null) audioSource = m_source;

            var tween = LeanTween.value(gameObject, audioSource.volume, targetVolume, time).setOnUpdate((float volume) => { m_source.volume = volume; });

            if (targetVolume <= 0) tween = tween.setOnComplete(() => { audioSource.Stop(); });

            if (easeIn) tween = tween.setEaseInExpo();
            if (easeOut) tween = tween.setEaseOutExpo();

            return tween;

        }

        public void FadeIn(float time, float targetVolume, IAudioSource audioSource = null, float startVolume = 0, bool easeIn = false, bool easeOut = false) => FadeInLT(time, targetVolume, audioSource is IAudioSourceProxy prox ? prox.Source : null, startVolume, easeIn, easeOut);
        public LTDescr FadeInLT(float time, float targetVolume, AudioSource audioSource = null, float startVolume = 0, bool easeIn = false, bool easeOut = false)
        {

            if (audioSource == null) audioSource = m_source;

            m_source.volume = startVolume;

            return FadeVolumeLT(time, targetVolume, audioSource, easeIn, easeOut);

        }

        public void FadeOut(float time, float targetVolume, IAudioSource audioSource = null, float startVolume = -1, bool easeIn = false, bool easeOut = false) => FadeOutLT(time, targetVolume, audioSource is IAudioSourceProxy prox ? prox.Source : null, startVolume, easeIn, easeOut);
        public LTDescr FadeOutLT(float time, float targetVolume, AudioSource audioSource = null, float startVolume = -1, bool easeIn = false, bool easeOut = false)
        {

            if (audioSource == null) audioSource = m_source;

            if (startVolume >= 0) m_source.volume = startVolume;

            return FadeVolumeLT(time, targetVolume, audioSource, easeIn, easeOut);

        }

        public void FadeIn(float time)
        {

            FadeIn(time, 1);

        }

        public void FadeOut(float time)
        {

            FadeOut(time, 0);

        }
#endif

        private int lastPlayedClip;
        private int lastPlayedBundle;

        public void AudioPlayOneShot(int id)
        {

            if (muted) return;

            m_source.loop = false;

            m_source.pitch = defaultPitch;

            m_source.PlayOneShot(m_clips[id], defaultVolume);

            lastPlayedClip = id;

        }

        public void AudioBundlePlayOneShot(int id)
        {

            AudioBundle bundle = m_bundles[id];

            AudioSource source = bundle.source == null ? this.m_source : bundle.m_source;

            lastPlayedBundle = id;

            source.loop = false;

            if (bundle.stopBeforePlay) source.Stop();

            if (muted) return;

            source.pitch = bundle.pitch;

            source.PlayOneShot(bundle.m_clip, bundle.volume);

            bundle.InvokeOnPlay();

        }

        public void AudioPlayLooping(int id)
        {

            m_source.Stop();

            lastPlayedClip = id;

            if (muted) return;

            m_source.pitch = defaultPitch;

            m_source.clip = m_clips[id];
            m_source.volume = defaultVolume;
            m_source.loop = true;

            m_source.Play();

        }

        public void AudioBundlePlayLooping(int id)
        {

            AudioBundle bundle = m_bundles[id];

            AudioSource source = bundle.source == null ? this.m_source : bundle.m_source;

            source.Stop();

            lastPlayedBundle = id;

            if (muted) return;

            source.pitch = bundle.pitch;

            source.clip = bundle.m_clip;
            source.volume = bundle.volume;
            source.loop = true;

            source.Play();

            bundle.InvokeOnPlay();

        }

        public void AudioPlayLoopingIfNotPlaying(int id)
        {

            if (lastPlayedClip == id && m_source.isPlaying) return;

            AudioPlayLooping(id);

        }

        public void AudioBundlePlayLoopingIfNotPlaying(int id)
        {

            if (lastPlayedBundle == id && (m_bundles[id].source == null ? m_source.isPlaying : m_bundles[id].source.isPlaying)) return;

            AudioBundlePlayLooping(id);

        }

        public void AudioPlayOneShotPersistent(int id)
        {

            if (muted) return;

            PersistentAudioPlayer player = PersistentAudioPlayer.Instance;

            player.Source.pitch = defaultPitch;

            player.Source.PlayOneShot(m_clips[id], defaultVolume);

            lastPlayedClip = id;

        }

        public void AudioBundlePlayOneShotPersistent(int id)
        {

            if (muted) return;

            PersistentAudioPlayer player = PersistentAudioPlayer.Instance;

            AudioBundle bundle = m_bundles[id];

            if (bundle.stopBeforePlay) player.Source.Stop();

            player.Source.pitch = bundle.pitch;

            player.Source.PlayOneShot(bundle.m_clip, bundle.volume);

            lastPlayedBundle = id;

            bundle.InvokeOnPlay();

        }

        private int GetRandomIndex(int idMin, int idMax)
        {

            return Random.Range(idMin, idMax + 1);

        }

        public void AudioPlayRandomOneShot(int idMin, int idMax)
        {

            int index = GetRandomIndex(idMin, idMax);

            while (idMax - idMin > 1 && index == lastPlayedClip) index = GetRandomIndex(idMin, idMax);

            AudioPlayOneShot(index);

        }

        public void AudioBundlePlayRandomOneShot(int idMin, int idMax)
        {

            int index = GetRandomIndex(idMin, idMax);

            while (idMax - idMin > 1 && index == lastPlayedBundle) index = GetRandomIndex(idMin, idMax);

            AudioBundlePlayOneShot(index);

        }

        public void AudioPlayRandomOneShotPersistent(int idMin, int idMax)
        {

            AudioPlayOneShotPersistent(Random.Range(idMin, idMax + 1));

        }

        public void AudioBundlePlayRandomOneShotPersistent(int idMin, int idMax)
        {

            AudioBundlePlayOneShotPersistent(Random.Range(idMin, idMax + 1));

        }

        public void AudioPlayAnyOneShot()
        {

            AudioPlayRandomOneShot(0, m_clips.Length - 1);

        }

        public void AudioBundlePlayAnyOneShot()
        {

            AudioBundlePlayRandomOneShot(0, m_bundles.Length - 1);

        }

        public void AudioPlayAnyOneShotPersistent()
        {

            AudioPlayRandomOneShotPersistent(0, m_clips.Length - 1);

        }

        public void AudioBundlePlayAnyOneShotPersistent()
        {

            AudioBundlePlayRandomOneShotPersistent(0, m_bundles.Length - 1);

        }

        protected float defaultFadeInTime = 0.25f;

        public void SetDefaultFadeInTime(float fadeInTime)
        {

            defaultFadeInTime = fadeInTime;

        }

        public void AudioPlayOneShotFadeIn(int id)
        {

            AudioPlayOneShot(id);

            FadeIn(defaultFadeInTime);

        }

        public void AudioBundlePlayOneShotFadeIn(int id)
        {

            AudioBundlePlayOneShot(id);

            FadeIn(defaultFadeInTime);

        }

        public void AudioPlayLoopingFadeIn(int id)
        {

            AudioPlayLooping(id);

            FadeIn(defaultFadeInTime);

        }

        public void AudioBundlePlayLoopingFadeIn(int id)
        {

            AudioBundlePlayLooping(id);

            FadeIn(defaultFadeInTime);

        }

        public void AudioPlayLoopingFadeInIfNotPlaying(int id)
        {

            if (lastPlayedClip == id && m_source.isPlaying) return;

            AudioPlayLoopingFadeIn(id);

        }

        public void AudioBundlePlayLoopingFadeInIfNotPlaying(int id)
        {

            if (lastPlayedBundle == id && (m_bundles[id].source == null ? m_source.isPlaying : m_bundles[id].source.isPlaying)) return;

            AudioBundlePlayLoopingFadeIn(id);

        }

    }

}

#endif