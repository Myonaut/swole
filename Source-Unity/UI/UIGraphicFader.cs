#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swole.UI
{
    public class UIGraphicFader : MonoBehaviour
    {
        [SerializeField]
        protected Graphic graphic;

        [SerializeField]
        protected CanvasGroup canvasGroup;

        [SerializeField]
        protected AnimationCurve fadeCurve;

        public float fade;

        [Serializable]
        public enum FadeOnStartBehaviour
        {
            None,
            FadeIn,
            FadeOut
        }

        public FadeOnStartBehaviour fadeOnStart = FadeOnStartBehaviour.None;
        public float fadeOnStartDuration = 1f;

        public UnityEvent OnFadeInStart;
        public UnityEvent OnFadeInComplete;

        public UnityEvent OnFadeOutStart;
        public UnityEvent OnFadeOutComplete;

        public UnityEvent<float> SyncFade;
        public UnityEvent<float> SyncInvertedFade;

        private void Awake()
        {
            if (graphic == null)
            {
                graphic = GetComponent<Graphic>();
            }
            if (graphic == null && canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            switch (fadeOnStart)
            {
                case FadeOnStartBehaviour.FadeIn:
                    FadeIn(fadeOnStartDuration);
                    break;
                case FadeOnStartBehaviour.FadeOut:
                    FadeOut(fadeOnStartDuration);
                    break;
            }
        } 

        public void FadeOut(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine(duration));
        }
        private IEnumerator FadeOutRoutine(float duration)
        {
            OnFadeOutStart?.Invoke();

            float startFade = Mathf.Clamp01(fade);
            float elapsed = startFade * duration;
            while (elapsed < duration)
            {
                fade = Mathf.Clamp01(elapsed / duration); 
                SetAlphaFromFade(fade);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fade = 1f;
            SetAlphaFromFade(fade);

            OnFadeOutComplete?.Invoke();
        }

        public void FadeIn(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine(duration));
        }
        private IEnumerator FadeInRoutine(float duration)
        {
            OnFadeInStart?.Invoke();

            float startFade = Mathf.Clamp01(fade);
            float elapsed = (1f - startFade) * duration;
            while (elapsed < duration)
            {
                fade = Mathf.Clamp01(1f - (elapsed / duration)); 
                SetAlphaFromFade(fade);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fade = 0f;
            SetAlphaFromFade(fade);

            OnFadeInComplete?.Invoke(); 
        }

        private void SetAlphaFromFade(float fade)
        {
            float alpha = 1f - Mathf.Clamp01(fadeCurve == null || fadeCurve.length <= 0 ? fade : fadeCurve.Evaluate(fade)); 
            SyncFade?.Invoke(alpha);
            SyncInvertedFade?.Invoke(1f - alpha);

            if (graphic != null)
            {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

    }
}

#endif