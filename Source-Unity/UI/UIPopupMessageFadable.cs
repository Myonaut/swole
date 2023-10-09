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

        public float fadeOutTime = 1f;

        public float displayTime = 3.5f;

        private List<TMP_Text> text;

        private List<Image> images;

        private List<Color> og_text_color;

        private List<Color> og_image_color;

        private bool initialized = false;

        private void Awake()
        {

            if (!initialized)
            {

                initialized = true;

                text = new List<TMP_Text>();

                images = new List<Image>();

                og_text_color = new List<Color>();

                og_image_color = new List<Color>();

                text.AddRange(gameObject.GetComponentsInChildren<TMP_Text>());

                images.AddRange(gameObject.GetComponentsInChildren<Image>());

                for (int a = 0; a < text.Count; a++) og_text_color.Add(text[a].color);

                for (int a = 0; a < images.Count; a++) og_image_color.Add(images[a].color);

            }

        }

        private void UpdateAlphas(float alpha)
        {

            for (int a = 0; a < text.Count; a++) text[a].color = Color.Lerp(new Color(og_text_color[a].r, og_text_color[a].g, og_text_color[a].b, 0), og_text_color[a], alpha);

            for (int a = 0; a < images.Count; a++) images[a].color = Color.Lerp(new Color(og_image_color[a].r, og_image_color[a].g, og_image_color[a].b, 0), og_image_color[a], alpha);

        }

        private void Start()
        {

            UpdateAlphas(0);

            LeanTween.value(gameObject, 0, 1, fadeInTime).setOnUpdate(UpdateAlphas);

            LeanTween.delayedCall(displayTime, () => { LeanTween.value(gameObject, 1, 0, fadeOutTime).setEaseInExpo().setOnUpdate(UpdateAlphas).setOnComplete(() => { GameObject.DestroyImmediate(gameObject); }); });

        }

    }

}

#endif
