#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swole
{
    public class SceneSwap : MonoBehaviour
    {

        public static void ReturnToMainMenu() { } 
        public static void ReturnToCreateMenu() { }
        public static void ReturnToPackageMenu() { }

        public static void SwapToScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return; 
            try
            {
                var currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                if (currentScene != null) previousScene = currentScene.name; 
            }
            catch (Exception e)
            {
                swole.LogError(e); 
            }
        }

        public static string previousScene;
        public static void SwapToPreviousScene() => SwapToScene(previousScene);    
        public static void SwapToPreviousSceneOrDefault(string defaultScene)
        {
            if (string.IsNullOrWhiteSpace(previousScene)) SwapToScene(defaultScene); else SwapToPreviousScene();
        }

        public void SwapToPrevious() => SwapToPreviousScene();
        public void SwapToPreviousOrDefault(string defaultScene) => SwapToPreviousSceneOrDefault(defaultScene);
        public void SwapToPreviousOrDefault(int defaultScene) => SwapToPreviousSceneOrDefault(swappableScenes == null || defaultScene < 0 || defaultScene >= swappableScenes.Length ? string.Empty : swappableScenes[defaultScene]);  

        public string sceneName;

        public string[] swappableScenes;

        public void Swap() => Swap(sceneName);
        public void Swap(int swapId)
        {
            if (swappableScenes == null) return;
            Swap(swappableScenes[swapId]);
        }
        public void Swap(string sceneName) => SwapToScene(sceneName);

    }
}

#endif
