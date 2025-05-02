#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI 
{

    public class UIPopupMessageFadable : MonoBehaviour
    {

        public bool destroyOnEnd = true;

        public float fadeInTime = 0.25f;
        private float defaultFadeInTime;
        public float DefaultFadeInTime => defaultFadeInTime;

        public float fadeOutTime = 1f;
        private float defaultFadeOutTime;
        public float DefaultFadeOutTime => defaultFadeOutTime;

        public float displayTime = 3.5f;
        private float defaultDisplayTime;
        public float DefaultDisplayTime => defaultDisplayTime;

        private List<Text> text;

        private List<TMP_Text> tmpText;

        private List<Image> images;

        private List<Color> og_text_color;
        private List<Color> og_tmpText_color;

        private List<Color> og_image_color;

        private bool initialized = false;

        public void Initialize()
        {

            if (!initialized)
            {

                defaultFadeInTime = fadeInTime;
                defaultFadeOutTime = fadeOutTime;
                defaultDisplayTime = displayTime;

                initialized = true;

                text = new List<Text>();
                tmpText = new List<TMP_Text>();

                images = new List<Image>();

                og_tmpText_color = new List<Color>();

                og_image_color = new List<Color>();

                text.AddRange(gameObject.GetComponentsInChildren<Text>(true));
                tmpText.AddRange(gameObject.GetComponentsInChildren<TMP_Text>(true));

                images.AddRange(gameObject.GetComponentsInChildren<Image>(true));

                for (int a = 0; a < text.Count; a++) og_text_color.Add(text[a].color);
                for (int a = 0; a < tmpText.Count; a++) og_tmpText_color.Add(tmpText[a].color);

                for (int a = 0; a < images.Count; a++) og_image_color.Add(images[a].color);

            }

        }

        private void Awake()
        {
            Initialize();
        }

        private void UpdateAlphas(float alpha)
        {

            for (int a = 0; a < text.Count; a++) text[a].color = Color.Lerp(new Color(og_text_color[a].r, og_text_color[a].g, og_text_color[a].b, 0), og_text_color[a], alpha);
            for (int a = 0; a < tmpText.Count; a++) tmpText[a].color = Color.Lerp(new Color(og_tmpText_color[a].r, og_tmpText_color[a].g, og_tmpText_color[a].b, 0), og_tmpText_color[a], alpha);

            for (int a = 0; a < images.Count; a++) images[a].color = Color.Lerp(new Color(og_image_color[a].r, og_image_color[a].g, og_image_color[a].b, 0), og_image_color[a], alpha);

        }

        private void Start()
        {

            if (enabled && gameObject.activeInHierarchy) Show(); 

        }

        public void Close() => Close(false);
        public void Close(bool immediate)
        {

            if (destroyOnEnd) 
            { 
                if (immediate) GameObject.DestroyImmediate(gameObject); else GameObject.Destroy(gameObject); 
            } 
            else 
            {             
                gameObject.SetActive(false);    
            }

        }

        public UIPopupMessageFadable Show()
        {
#if SWOLE_ENV
            LeanTween.cancel(gameObject);
#endif

            Initialize();

            gameObject.SetActive(true);
            enabled = true;

            UpdateAlphas(0);
#if SWOLE_ENV
            LeanTween.value(gameObject, 0, 1, fadeInTime).setOnUpdate(UpdateAlphas);
            LeanTween.delayedCall(gameObject, displayTime, () => { LeanTween.value(gameObject, 1, 0, fadeOutTime).setEaseInExpo().setOnUpdate(UpdateAlphas).setOnComplete(() => { Close(); }); });
#endif
            return this;

        }

        public UIPopupMessageFadable SetMessage(string message)
        {

            Initialize();

            if (text != null)
            {
                foreach (Text t in text) if (t != null && t.name.ToLower() == "message") t.text = message;
            }
            if (tmpText != null)
            {
                foreach (TMP_Text t in tmpText) if (t != null && t.name.ToLower() == "message") t.SetText(message);
            }

            return this;

        }

        public UIPopupMessageFadable ResetFadeInTime()
        {
            if (!initialized) return this;
            fadeInTime = defaultFadeInTime;
            return this;
        }
        public UIPopupMessageFadable ResetFadeOutTime()
        {
            if (!initialized) return this;
            fadeOutTime = defaultFadeOutTime;
            return this;
        }
        public UIPopupMessageFadable ResetDisplayTime()
        {
            if (!initialized) return this;
            displayTime = defaultDisplayTime;
            return this;
        }
        public UIPopupMessageFadable SetFadeInTime(float time)
        {
            Initialize();
            fadeInTime = time;
            return this;
        }
        public UIPopupMessageFadable SetFadeOutTime(float time)
        {
            Initialize();
            fadeOutTime = time;
            return this;
        }
        public UIPopupMessageFadable SetDisplayTime(float time)
        {
            Initialize();
            displayTime = time;
            return this;
        }

        public UIPopupMessageFadable SetMessageAndShow(string message) => SetMessage(message).Show();

        public UIPopupMessageFadable SetMessageAndShowFor(string message, float displayTime) => SetMessage(message).SetDisplayTime(displayTime).Show();

    }

}

#endif
