#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static Swole.API.Unity.ProxyBoneJobs;

namespace Swole.API.Unity
{
    [ExecuteInEditMode]
    public class ProxyBone : MonoBehaviour
    {

        [Serializable]
        public class BoneBinding
        {

            public Transform bone;

            public Binding binding = new Binding()
            {

                relative = false,
                baseWeight = 1,
                rotationBindingOrder = BindingOrder.YXZ,
                rotationWeights = 1,
                positionWeights = 1

            };

        }

#if UNITY_EDITOR
        public bool refreshStartPose;
#endif

        [SerializeField, HideInInspector]
        public bool hasStartingPose;

        public BoneBinding[] bindings;

        protected int registeredIndex = -1; 

        protected void Awake()
        {

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                registeredIndex = ProxyBoneJobs.Register(this);

#if UNITY_EDITOR
            }
#endif

        }

#if UNITY_EDITOR

        public void OnValidate()
        {

            refreshStartPose = false;

            if (bindings != null && !Application.isPlaying)
            {

                foreach (var boneBinding in bindings)
                {

                    if (boneBinding == null) continue;

                    var binding = boneBinding.binding;

                    binding.startProxyPosition = transform.localPosition;
                    binding.startProxyRotation = transform.localRotation;

                    if (boneBinding.bone != null)
                    {

                        binding.startPosition = boneBinding.bone.localPosition;
                        binding.startRotation = boneBinding.bone.localRotation;

                    }

                    boneBinding.binding = binding;

                }

                hasStartingPose = true;

            }

        }

#endif

        protected void OnDestroy()
        {

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                ProxyBoneJobs.Unregister(registeredIndex); 
                registeredIndex = -1; 

#if UNITY_EDITOR
            }
#endif

        }

    }
}

#endif