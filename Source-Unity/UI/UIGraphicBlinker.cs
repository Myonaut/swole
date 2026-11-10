#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swole.UI
{
    public class UIGraphicBlinker : MonoBehaviour
    {

        [SerializeField]
        protected Graphic graphic;

        [SerializeField]
        protected CanvasGroup canvasGroup;

        [SerializeField]
        protected AnimationCurve blinkCurve;

        public float blinkSpeed = 1f;
        private float blinkTime = 0f;
        private bool blinkDir;

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

        private void Update()
        {
            blinkTime = Mathf.Clamp01(blinkDir ? blinkTime - (Time.deltaTime * blinkSpeed) : blinkTime + (Time.deltaTime * blinkSpeed));
            if (blinkTime <= 0f)
            {
                blinkDir = false;
            }
            else if (blinkTime >= 1f)
            {
                blinkDir = true; 
            }

            float alpha = blinkCurve == null || blinkCurve.length <= 0 ? blinkTime : blinkCurve.Evaluate(blinkTime);
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