#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{
    public class IconsFromScreen : MonoBehaviour
    {

        public bool executeAll;
        public string defaultSavePath;

        new public Camera camera;
        public Color cameraBG_color = Color.clear;

        [Serializable]
        public class Capture
        {
            public string name;

            public float aspectRatio;

            [Range(-1f, 1f)]
            public float screenCenterX;
            [Range(-1f, 1f)]
            public float screenCenterY;

            [Range(0f, 0.99f)]
            public float crop;

            public float outputWidth; 

            public bool mipMaps;

            public bool showUI;

            public bool execute;
            public bool packAsJpg;
            public bool packAsPng;

            public bool overrideSavePath;
            public string savePath;

            public string textureName;

            public Texture2D output;

            public List<GameObject> objectsToEnable;
            public List<GameObject> objectsToDisable;

        }

        public List<Capture> captures;

        [NonSerialized]
        private List<Capture> toCapture = new List<Capture>();

        [NonSerialized]
        private bool capturing;

        public void ExecuteCaptures(List<Capture> toCapture)
        {
            var tempCaptures = toCapture.ToArray();

            IEnumerator Execute()
            {
                while (capturing) yield return null;

                capturing = true;

                foreach (var capture in tempCaptures)
                {
                    yield return null;

                    try
                    {
                        if (capture.objectsToEnable != null)
                        {
                            foreach (var obj in capture.objectsToEnable) if (obj != null) obj.SetActive(true);
                        }
                        if (capture.objectsToDisable != null)
                        {
                            foreach (var obj in capture.objectsToDisable) if (obj != null) obj.SetActive(false);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    yield return null;

                    bool received = false;
                    try
                    {
                        float screenAspectRatio = Screen.width / (float)Screen.height;

                        Vector2 center = new Vector2((capture.screenCenterX + 1f) * 0.5f, (capture.screenCenterY + 1f) * 0.5f);

                        float width = center.x;
                        width = Mathf.Min(width, 1f - width);

                        float height = center.y;
                        height = Mathf.Min(height, 1f - height);

                        float ratioDiff = capture.aspectRatio / screenAspectRatio;
                        if (ratioDiff < 1f)
                        {
                            width = width * ratioDiff;
                        }
                        else
                        {
                            height = height / ratioDiff;
                        }

                        width = width * (1f - capture.crop);
                        height = height * (1f - capture.crop);

                        Vector2 startCoords = new Vector2(center.x - width, center.y - height);
                        Vector2 endCoords = new Vector2(center.x + width, center.y + height);

                        void Callback(Texture2D tex)
                        {
                            capture.output = tex;
                            received = true; 
                        }
                        
                        Utils.TakeScreenshot(Callback, startCoords, endCoords, !capture.showUI, capture.mipMaps, Mathf.CeilToInt(capture.outputWidth), Mathf.CeilToInt(capture.outputWidth / capture.aspectRatio), camera, cameraBG_color);
                    }
                    catch (Exception e)
                    {
                        received = true;
                        Debug.LogError(e);
                    }

                    while (!received) yield return null;

                    try
                    {
#if UNITY_EDITOR

                        string savePath = capture.overrideSavePath ? capture.savePath : string.Empty;
                        if (string.IsNullOrEmpty(savePath)) savePath = defaultSavePath;
                        if (string.IsNullOrEmpty(savePath)) savePath = "";

                        if (capture.packAsJpg)
                        {
                            var _savePath = savePath;
                            _savePath = Extensions.CreateUnityAssetPathString(_savePath, capture.textureName, "jpg");

                            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                            directoryInfo = directoryInfo.Parent;

                            var bytes = capture.output.EncodeToJPG(100);
                            File.WriteAllBytes(Path.Join(directoryInfo.FullName, _savePath), bytes);

                            UnityEditor.AssetDatabase.Refresh();
                        }
                        if (capture.packAsPng)
                        {
                            var _savePath = savePath;
                            _savePath = Extensions.CreateUnityAssetPathString(_savePath, capture.textureName, "png");

                            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                            directoryInfo = directoryInfo.Parent;

                            var bytes = capture.output.EncodeToPNG();
                            File.WriteAllBytes(Path.Join(directoryInfo.FullName, _savePath), bytes);

                            UnityEditor.AssetDatabase.Refresh();
                        }

                        if (!capture.packAsJpg && !capture.packAsPng)
                        {
                            var _savePath = savePath;
                            capture.output = capture.output.CreateOrReplaceAsset(capture.output.CreateUnityAssetPathString(_savePath, "asset"));
                            UnityEditor.AssetDatabase.SaveAssetIfDirty(capture.output);
                        }

#endif
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    yield return null;

                    try
                    {
                        if (capture.objectsToEnable != null)
                        {
                            foreach (var obj in capture.objectsToEnable) if (obj != null) obj.SetActive(false);
                        }
                        if (capture.objectsToDisable != null)
                        {
                            foreach (var obj in capture.objectsToDisable) if (obj != null) obj.SetActive(true);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    yield return null;

                }

                capturing = false;
            }

            StartCoroutine(Execute());
        }

        public void Update()
        {
            toCapture.Clear();
            foreach (var capture in captures)
            {
                if (capture.execute || executeAll)
                {
                    capture.execute = false;
                    toCapture.Add(capture);
                }
            }
            executeAll = false;

            if (toCapture.Count > 0) ExecuteCaptures(toCapture);
        }

    }
}

#endif