#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

namespace Swole.API.Unity
{

    public class PersistentAudioPlayer : SingletonBehaviour<PersistentAudioPlayer>
    {

        public override bool DestroyOnLoad => false;
        public override bool ExecuteInStack => false;

        [SerializeField]
        protected AudioSource source;
        public AudioSource Source => source;

        protected override void OnAwake()
        {
            if (source == null) source = gameObject.AddOrGetComponent<AudioSource>();
        }

        protected override void OnInit()
        {
            OnAwake();
        }

        public override void OnUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            ReleaseAllUnusedSources();
        }
        protected virtual void LateUpdate() => OnLateUpdate();

        public override void OnFixedUpdate()
        {
        }

        protected void ReleaseAllUnusedSources()
        {
            ReleaseUnusedSources2D();
        }

        public class AudioSourceClaim
        {
            public bool persistent;

            public bool IsValid => source != null && !invalid;
            protected bool invalid;
            public void Release()
            {
                invalid = true;
                if (source != null)
                {
                    source.Stop();
                    if (pool != null) pool.Release(source.gameObject);
                    source = null;
                }
            }

            protected AudioSource source;
            public AudioSource Source => source;

            protected PrefabPool pool;

            public AudioSourceClaim(PrefabPool pool, AudioSource source, bool persistent = false)
            {
                this.pool = pool;
                this.source = source;
                this.persistent = persistent;
            }
        }

        private List<AudioSource> sourcesTemp = new List<AudioSource>();
        private List<AudioSourceClaim> claimsTemp = new List<AudioSourceClaim>();

        private PrefabPool sourcePool2D;
        private AudioSource sourcePrefab2D;
        private List<AudioSourceClaim> claims2D = new List<AudioSourceClaim>();

        protected void InitializeSourcePool2D()
        {
            if (sourcePool2D == null)
            {
                sourcePool2D = new GameObject("sources_2D").AddComponent<PrefabPool>();
                sourcePool2D.transform.SetParent(transform, false);
            }

            if (claims2D == null) claims2D = new List<AudioSourceClaim>();

            if (sourcePrefab2D == null)
            {
                sourcePrefab2D = new GameObject("source_2D").AddComponent<AudioSource>();
                sourcePrefab2D.transform.SetParent(transform, false);
                sourcePrefab2D.spatialBlend = 0;
                sourcePrefab2D.spatialize = false;

                claims2D.Clear();

                sourcePool2D.Reinitialize(sourcePrefab2D.gameObject, PoolGrowthMethod.Incremental, 1, 1, 1024);
                sourcePool2D.SetContainerTransform(transform, true, true, false);
            }
        }
        protected void ReleaseUnusedSources2D()
        {
            if (claims2D == null || sourcePool2D == null) return;

            claimsTemp.Clear();
            claims2D.RemoveAll(i => i == null);
            foreach (var claim in claims2D) if (claim.Source == null || (!claim.persistent && !claim.Source.isPlaying)) claimsTemp.Add(claim);
            foreach (var claim in claimsTemp) 
            { 
                claims2D.RemoveAll(i => ReferenceEquals(claim, i));
                claim.Release();
            }
            claimsTemp.Clear();
        }
        public static AudioSourceClaim Get2DSourceAndPlay(AudioClip clip, float volume = 1, float pitch = 1, AudioMixerGroup mixerGroup = null, bool invalidateOnEnd = true)
        {
            var instance = Instance;
            if (clip == null || instance == null) return null; 

            instance.InitializeSourcePool2D();

            AudioSourceClaim claim = null;
            if (instance.sourcePool2D.TryGetNewInstance(out GameObject inst))
            {
                claim = new AudioSourceClaim(instance.sourcePool2D, inst.GetComponent<AudioSource>(), invalidateOnEnd);
                instance.claims2D.Add(claim);

                try
                {
                    claim.Source.timeSamples = 0;
                    claim.Source.volume = 1;
                    claim.Source.pitch = pitch;
                    if (mixerGroup == null) mixerGroup = ResourceLib.DefaultAudioMixer == null || ResourceLib.DefaultAudioMixer.Mixer == null ? null : ResourceLib.DefaultAudioMixer.Mixer.outputAudioMixerGroup;
                    claim.Source.outputAudioMixerGroup = mixerGroup;

                    inst.SetActive(true);
                    claim.Source.clip = clip;
                    claim.Source.Play();
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }
            }

            return claim;
        }

    }

}


#endif