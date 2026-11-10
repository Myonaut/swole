using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Swole
{
    [System.Serializable]
    public enum TargetAspectRatio
    {
        Standard_4_3,
        Widescreen_16_9,
        Ultrawide_21_9,
        Vertical_9_16,
        Square_1_1
    }

    [System.Serializable]
    public struct GraphicTintSettings
    {
        public Graphic uiGraphic;
        public Color targetColor;
    }

    [System.Serializable]
    public class VideoSettings
    {
        public string clipName;
        public VideoClip videoClip;
        public bool useFadeAfterThisClip;
        public float fadeDuration;
        public float volumeMultiplier = 1f;

        [Header("Aspect Ratio Settings")]
        public TargetAspectRatio aspectRatioMode = TargetAspectRatio.Widescreen_16_9;

        public bool useCustomAspectRatio;

        [Tooltip("Enter a custom ratio float value (e.g., 1.777 for 16:9, 1.333 for 4:3)")]
        public float customAspectRatio = 1.777778f;

        [Header("Timestamp Settings")]
        public bool overrideTimestamps = false;
        [Tooltip("Start time in seconds.")]
        public float startTime = 0f;
        [Tooltip("End time in seconds. Must be greater than start time and less than clip duration.")]
        public float endTime = 0f;

        [Header("UI Tint Settings")]
        public GraphicTintSettings[] graphicsToTint;
        public float tintTransitionDuration = 1.0f;
    }

    public class VideoPlaylistManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private AspectRatioFitter aspectRatioFitter;

        [Header("Playlist Settings")]
        public float globalVolume = 1f;
        [SerializeField] private VideoSettings[] videoPlaylist;
        [SerializeField] private bool loopPlaylist = true;

        private int currentVideoIndex = 0;
        private bool isTransitioning = false;
        private bool hasStartedFadeOut = false;
        private Coroutine currentTintCoroutine;

        void Start()
        {
            if (videoPlayer == null || videoPlaylist.Length == 0)
            {
                Debug.LogError("VideoPlayer or VideoPlaylist is missing!");
                return;
            }

            if (aspectRatioFitter == null)
            {
                Debug.LogError("AspectRatioFitter component is not assigned!");
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }

            videoPlayer.isLooping = false;
            PlayVideoAtIndex(currentVideoIndex);
        }

        void Update()
        {
            if (isTransitioning || !videoPlayer.isPlaying) return;

            VideoSettings currentSettings = videoPlaylist[currentVideoIndex];

            // Determine what the absolute boundary end time is for calculations
            double absoluteEndTime = currentSettings.overrideTimestamps && currentSettings.endTime > currentSettings.startTime
                ? currentSettings.endTime
                : videoPlayer.length;

            double timeRemaining = absoluteEndTime - videoPlayer.time;

            if (currentSettings.useFadeAfterThisClip && !hasStartedFadeOut)
            {
                float maxAllowedDuration = currentSettings.overrideTimestamps
                    ? (currentSettings.endTime - currentSettings.startTime)
                    : (float)videoPlayer.length;

                float targetFadeDuration = Mathf.Min(currentSettings.fadeDuration, maxAllowedDuration);

                if (timeRemaining <= targetFadeDuration)
                {
                    hasStartedFadeOut = true;
                    StartCoroutine(FadeOutAndTransition(targetFadeDuration, currentSettings.volumeMultiplier));
                }
            }
            else if (!currentSettings.useFadeAfterThisClip)
            {
                if (timeRemaining <= 0.05f || (videoPlayer.frame > 0 && !videoPlayer.isPlaying))
                {
                    StartCoroutine(TransitionToNextVideoImmediate());
                }
            }
        }

        private IEnumerator FadeOutAndTransition(float duration, float startVolume)
        {
            yield return StartCoroutine(FadeMedia(0f, 1f, startVolume, 0f, duration));
            AdvancePlaylistIndex();
        }

        private IEnumerator TransitionToNextVideoImmediate()
        {
            isTransitioning = true;
            AdvancePlaylistIndex();
            yield return null;
        }

        private void AdvancePlaylistIndex()
        {
            isTransitioning = true;

            int nextIndex = currentVideoIndex + 1;
            if (nextIndex >= videoPlaylist.Length)
            {
                if (loopPlaylist)
                {
                    nextIndex = 0;
                }
                else
                {
                    videoPlayer.Stop();
                    isTransitioning = false;
                    return;
                }
            }

            currentVideoIndex = nextIndex;
            PlayVideoAtIndex(currentVideoIndex);
            StartCoroutine(FadeInNewVideo());
        }

        private IEnumerator FadeInNewVideo()
        {
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            VideoSettings nextSettings = videoPlaylist[currentVideoIndex];

            if (nextSettings.useFadeAfterThisClip && fadeCanvasGroup != null)
            {
                float maxAllowedDuration = nextSettings.overrideTimestamps
                    ? (nextSettings.endTime - nextSettings.startTime)
                    : (float)videoPlayer.length;

                float targetFadeDuration = Mathf.Min(nextSettings.fadeDuration, maxAllowedDuration);
                yield return StartCoroutine(FadeMedia(1f, 0f, 0f, nextSettings.volumeMultiplier, targetFadeDuration)); 
            }
            else
            {
                if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
                SetVideoVolume(nextSettings.volumeMultiplier);
            }

            hasStartedFadeOut = false;
            isTransitioning = false;
        }

        private void PlayVideoAtIndex(int index)
        {
            VideoSettings settings = videoPlaylist[index];
            videoPlayer.clip = settings.videoClip;

            SetVideoVolume(settings.useFadeAfterThisClip && currentVideoIndex != 0 ? 0f : 1f);
            if (settings.useCustomAspectRatio)
            {
                StartCoroutine(SetAspectRatioDelayed(settings.customAspectRatio, 1));
            }
            else
            {
                StartCoroutine(SetAspectRatioDelayed(settings.aspectRatioMode, 1));  
            }

            if (currentTintCoroutine != null)
            {
                StopCoroutine(currentTintCoroutine);
            }
            currentTintCoroutine = StartCoroutine(TransitionUIColors(settings));

            videoPlayer.Play();

            // If time offsets are enabled, apply the skip instruction instantly
            if (settings.overrideTimestamps && settings.startTime > 0f)
            {
                videoPlayer.time = settings.startTime;
            }
        }

        private void SetAspectRatio(TargetAspectRatio mode)
        {
            if (aspectRatioFitter == null) return;

            switch (mode)
            {
                case TargetAspectRatio.Standard_4_3: aspectRatioFitter.aspectRatio = 4f / 3f; break;
                case TargetAspectRatio.Widescreen_16_9: aspectRatioFitter.aspectRatio = 16f / 9f; break;
                case TargetAspectRatio.Ultrawide_21_9: aspectRatioFitter.aspectRatio = 21f / 9f; break;
                case TargetAspectRatio.Vertical_9_16: aspectRatioFitter.aspectRatio = 9f / 16f; break;
                case TargetAspectRatio.Square_1_1: aspectRatioFitter.aspectRatio = 1f / 1f; break;
            }
        }
        private void SetAspectRatio(float ratio)
        {
            if (aspectRatioFitter == null) return;

            aspectRatioFitter.aspectRatio = ratio;
        }

        private IEnumerator SetAspectRatioDelayed(TargetAspectRatio mode, int frameDelay)
        {
            for (int i = 0; i < frameDelay; i++)
            {
                yield return null;
            }
            SetAspectRatio(mode);
        }

        private IEnumerator SetAspectRatioDelayed(float ratio, int frameDelay)
        {
            for (int i = 0; i < frameDelay; i++)
            {
                yield return null;
            }
            SetAspectRatio(ratio);
        }

        private IEnumerator FadeMedia(float startAlpha, float endAlpha, float startVolume, float endVolume, float duration)
        {
            if (duration <= 0f)
            {
                if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = endAlpha;
                SetVideoVolume(endVolume);
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                SetVideoVolume(Mathf.Lerp(startVolume, endVolume, t));
                yield return null;
            }

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = endAlpha;
            SetVideoVolume(endVolume);
        }

        private IEnumerator TransitionUIColors(VideoSettings settings)
        {
            if (settings.graphicsToTint == null || settings.graphicsToTint.Length == 0) yield break;

            float duration = settings.tintTransitionDuration;
            if (duration <= 0f)
            {
                foreach (GraphicTintSettings tintSetting in settings.graphicsToTint)
                {
                    if (tintSetting.uiGraphic != null) tintSetting.uiGraphic.color = tintSetting.targetColor;
                }
                yield break;
            }

            Color[] initialColors = new Color[settings.graphicsToTint.Length];
            for (int i = 0; i < settings.graphicsToTint.Length; i++)
            {
                if (settings.graphicsToTint[i].uiGraphic != null) initialColors[i] = settings.graphicsToTint[i].uiGraphic.color;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                for (int i = 0; i < settings.graphicsToTint.Length; i++)
                {
                    GraphicTintSettings tintSetting = settings.graphicsToTint[i];
                    if (tintSetting.uiGraphic != null)
                    {
                        tintSetting.uiGraphic.color = Color.Lerp(initialColors[i], tintSetting.targetColor, t);
                    }
                }
                yield return null;
            }

            for (int i = 0; i < settings.graphicsToTint.Length; i++)
            {
                GraphicTintSettings tintSetting = settings.graphicsToTint[i];
                if (tintSetting.uiGraphic != null) tintSetting.uiGraphic.color = tintSetting.targetColor;
            }
        }

        private void SetVideoVolume(float volume)
        {
            if (videoPlayer != null && videoPlayer.audioTrackCount > 0 && videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
            {
                videoPlayer.SetDirectAudioVolume(0, volume * globalVolume);
            }
        }
    }
}