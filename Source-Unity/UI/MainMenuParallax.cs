using Swole;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuParallax : MonoBehaviour
{

    public static bool playedIntro;

    public AudioSource introSong;

    [Serializable]
    public class ParallaxObject
    {
        public RectTransform rT;
        [Tooltip("A ratio of the set parallaxOffsetDistance properties.")]
        public float parallaxAmount;

        [NonSerialized]
        public Vector3 startPosition;
    }

    public float refResolutionX = 1920;
    public float refResolutionY = 1080;

    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float introOffsetX = -400;
    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float introOffsetY = -200;
    protected float introTime;

    public float introCenteringTime = 4;

    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float cursorCenterOffsetMaxDistanceX = 800;
    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float parallaxOffsetDistanceX = 100;

    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float cursorCenterOffsetMaxDistanceY = 500;
    [Tooltip("In pixels, will be scaled based on the reference resolution.")]
    public float parallaxOffsetDistanceY = 30;

    public float introOffsetScale = 1f;
    public float finalOffsetScale = 1f;

    [Tooltip("A ratio of the intro time below which the cursor offset starts to fade in."), Range(0f, 1f)]
    public float cursorOffsetFadeInTimeScale = 0.15f;

    public ParallaxObject[] parallaxObjects; 

    void Awake()
    {
        if (!playedIntro)
        {
            playedIntro = true;
            if (introSong != null) introSong.Play();

            introTime = introCenteringTime;
        }

        float refAspectRatio = refResolutionX / refResolutionY;
        float aspectRatio = Screen.width / Screen.height;

        float scaleY = refAspectRatio / aspectRatio;

        if (parallaxObjects != null)
        {
            foreach(var obj in parallaxObjects)
            {
                if (obj == null || obj.rT == null) continue;

                obj.startPosition = obj.rT.localPosition;
                obj.startPosition.y = obj.startPosition.y * scaleY;
            }
        }
    }

    void Update()
    {
        if (parallaxObjects != null)
        {

            introTime -= Time.deltaTime;

            var cursorPos = CursorProxy.ScreenPosition;
            cursorPos.x = (cursorPos.x / Screen.width) * refResolutionX;
            cursorPos.y = (cursorPos.y / Screen.height) * refResolutionY;

            float refAspectRatio = refResolutionX / refResolutionY; 
            float aspectRatio = Screen.width / Screen.height;

            float scaleY = aspectRatio / refAspectRatio;

            float t = introTime / introCenteringTime;
            var screenCenterX = (refResolutionX * 0.5f) + Mathf.SmoothStep(0, introOffsetX * introOffsetScale, t);
            var screenCenterY = (refResolutionY * 0.5f) + Mathf.SmoothStep(0, introOffsetY * introOffsetScale * scaleY, t);  

            float offsetX = (Mathf.SmoothStep(cursorPos.x - screenCenterX, -screenCenterX, Mathf.Clamp01(t / cursorOffsetFadeInTimeScale)) / (cursorCenterOffsetMaxDistanceX)) * finalOffsetScale;
            float offsetY = (Mathf.SmoothStep(cursorPos.y - screenCenterY, -screenCenterY, Mathf.Clamp01(t / cursorOffsetFadeInTimeScale)) / (cursorCenterOffsetMaxDistanceY * scaleY)) * finalOffsetScale;  

            foreach (var obj in parallaxObjects)
            {
                if (obj == null || obj.rT == null) continue; 

                obj.rT.localPosition = new Vector3(obj.startPosition.x, obj.startPosition.y * scaleY, 0f) - new Vector3(offsetX * parallaxOffsetDistanceX * obj.parallaxAmount, offsetY * parallaxOffsetDistanceY * scaleY * obj.parallaxAmount, 0); 
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

}
