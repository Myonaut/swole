using Swole.Script;

namespace Swole
{
    public interface IAudioSource : EngineInternal.IComponent
    {
        public bool bypassEffects { get; set; }
        public bool bypassListenerEffects { get; set; }
        public bool bypassReverbZones { get; set; }

        [SwoleScriptIgnore]
        public IAudioAsset clip { get; set; }

        public float dopplerLevel { get; set; }
        public bool ignoreListenerPause { get; set; }
        public bool ignoreListenerVolume { get; set; }
        public bool isPlaying { get; }
        public bool isVirtual { get; }
        public bool loop { get; set; }
        public float maxDistance { get; set; }
        public float minDistance { get; set; }
        public bool mute { get; set; }

        [SwoleScriptIgnore]
        public IAudioMixerGroup outputAudioMixerGroup { get; set; }

        public float panStereo { get; set; }
        public float pitch { get; set; }
        public int priority { get; set; }
        public float reverbZoneMix { get; set; }
        public string rolloffMode { get; set; }
        public float spatialBlend { get; set; }
        public float spread { get; set; }
        public float time { get; set; }
        public int timeSamples { get; set; }
        public string velocityUpdateMode { get; set; }
        public float volume { get; set; }

        public void Pause();
        public void Play(int delay = 0);
        public void PlayDelayed(float delay); 
        public void PlayOneShot(IAudioAsset asset, float volumeScale = 1.0F);
        public void PlayScheduled(double time);
        public void SetScheduledEndTime(double time);
        public void SetScheduledStartTime(double time);
        public void Stop();
        public void UnPause();

    }
}
