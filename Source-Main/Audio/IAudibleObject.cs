using Swole.Script;

namespace Swole
{
    public interface IAudibleObject : IAudioSource
    {

        public void SetMuted(bool muted);

        [SwoleScriptIgnore]
        public IAudioSource source { get; set; }

        [SwoleScriptIgnore]
        public IAudioAsset[] clips { get; set; }

        [SwoleScriptIgnore]
        public IAudioBundle[] bundles { get; set; }

#if SWOLE_ENV
        public void FadeVolume(float time, float targetVolume, IAudioSource audioSource = null, bool easeIn = false, bool easeOut = false);
        public void FadeIn(float time, float targetVolume, IAudioSource audioSource = null, float startVolume = 0, bool easeIn = false, bool easeOut = false);
        public void FadeOut(float time, float targetVolume, IAudioSource audioSource = null, float startVolume = -1, bool easeIn = false, bool easeOut = false);

        public void FadeIn(float time);
        public void FadeOut(float time);
#endif

        public void AudioPlayOneShot(int id);
        public void AudioBundlePlayOneShot(int id);

        public void AudioPlayLooping(int id);
        public void AudioBundlePlayLooping(int id);

        public void AudioPlayLoopingIfNotPlaying(int id);
        public void AudioBundlePlayLoopingIfNotPlaying(int id);

        public void AudioPlayOneShotPersistent(int id);
        public void AudioBundlePlayOneShotPersistent(int id);

        public void AudioPlayRandomOneShot(int idMin, int idMax);
        public void AudioBundlePlayRandomOneShot(int idMin, int idMax);

        public void AudioPlayRandomOneShotPersistent(int idMin, int idMax);
        public void AudioBundlePlayRandomOneShotPersistent(int idMin, int idMax);

        public void AudioPlayAnyOneShot();
        public void AudioBundlePlayAnyOneShot();

        public void AudioPlayAnyOneShotPersistent();
        public void AudioBundlePlayAnyOneShotPersistent();

        public void SetDefaultFadeInTime(float fadeInTime);
        public void AudioPlayOneShotFadeIn(int id);
        public void AudioBundlePlayOneShotFadeIn(int id);
        public void AudioPlayLoopingFadeIn(int id);
        public void AudioBundlePlayLoopingFadeIn(int id);
        public void AudioPlayLoopingFadeInIfNotPlaying(int id);
        public void AudioBundlePlayLoopingFadeInIfNotPlaying(int id);

    }

    public interface IAudioBundle : IRuntimeEventHandler
    {
        [SwoleScriptIgnore]
        public IAudioAsset clip { get; set; }

        public float volume { get; set; }

        public float pitch { get; set; }

        public bool stopBeforePlay { get; set; }

        [SwoleScriptIgnore]
        public IAudioSource source { get; set; }
    }

}
