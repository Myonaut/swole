#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swole
{
    public class SceneSwap : MonoBehaviour
    {

        public string sceneName;

        public string[] swappableScenes;

        public void Swap()
        {

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        }

        public void Swap(int swapId)
        {

            if (swappableScenes == null) return;

            SceneManager.LoadScene(swappableScenes[swapId], LoadSceneMode.Single);

        }

    }
}

#endif
