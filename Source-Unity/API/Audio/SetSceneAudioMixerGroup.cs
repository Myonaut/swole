#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

namespace Swole.API.Unity
{

    [ExecuteInEditMode]
    public class SetSceneAudioMixerGroup : MonoBehaviour
    {

        public bool apply;

        public bool applyAndDestroyOnAwake = false;

        public bool ignoreSourcesWithAGroup = true;

        public AudioMixerGroup group;

        private void Apply()
        {

            apply = false;

            AudioSource[] sources = FindObjectsOfType<AudioSource>(true);

            foreach (var source in sources)
            {

                if (source == null || (source.outputAudioMixerGroup != null && ignoreSourcesWithAGroup)) continue;

                source.outputAudioMixerGroup = group;

            }

        }

        public void Awake()
        {

            if (Application.isPlaying && applyAndDestroyOnAwake)
            {

                Apply();

                Destroy(this);

            }

        }

        public void Update()
        {

            if (apply)
            {

                Apply();

            }

        }

    }

}

#endif