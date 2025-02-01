#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using static Swole.API.Unity.ProxyBoneJobs;

namespace Swole.API.Unity
{
    [ExecuteAlways]
    public class ProxyBone : MonoBehaviour
    {

        public delegate void BindingChangeDelegate(Binding newBinding);

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

            public void ReinitializeStartPosition(Transform proxyTransform, bool notifyListeners = true) => this.binding = ReinitializeStartPositionForNewBinding(proxyTransform, notifyListeners);

            public Binding ReinitializeStartPositionForNewBinding(Transform proxyTransform, bool notifyListeners = true)
            {

                Binding binding = this.binding;

                binding.positionOffset = float3.zero;
                binding.rotationOffset = quaternion.identity;
                binding.scaleOffset = float3.zero;

                binding.startProxyPosition = proxyTransform.localPosition;
                binding.startProxyRotation = proxyTransform.localRotation;
                binding.startProxyScale = proxyTransform.localScale;

                if (bone != null)
                {

                    if (binding.applyInWorldSpace)
                    {
                        binding.startPosition = proxyTransform.parent == null ? bone.position : proxyTransform.parent.InverseTransformPoint(bone.position);
                        binding.startRotation = proxyTransform.parent == null ? bone.rotation : (Quaternion.Inverse(proxyTransform.parent.rotation) * bone.rotation);
                    }
                    else
                    {
                        binding.startPosition = bone.localPosition;
                        binding.startRotation = bone.localRotation;
                    }

                    binding.startScale = bone.localScale;

                }

                if (notifyListeners) OnReinitBinding?.Invoke(binding);

                return binding;

            }


            public void ReinitializeLocalStartPosition(Transform proxyTransform, bool notifyListeners = true) => this.binding = ReinitializeLocalStartPositionForNewBinding(proxyTransform, notifyListeners);

            public Binding ReinitializeLocalStartPositionForNewBinding(Transform proxyTransform, bool notifyListeners = true)
            {

                Binding binding = this.binding;

                binding.positionOffset = float3.zero;
                binding.rotationOffset = quaternion.identity;
                binding.scaleOffset = float3.zero;

                if (bone != null)
                {

                    if (binding.applyInWorldSpace)
                    {
                        binding.startPosition = proxyTransform.parent == null ? bone.position : proxyTransform.parent.InverseTransformPoint(bone.position);
                        binding.startRotation = proxyTransform.parent == null ? bone.rotation : (Quaternion.Inverse(proxyTransform.parent.rotation) * bone.rotation);
                    }
                    else
                    {
                        binding.startPosition = bone.localPosition;
                        binding.startRotation = bone.localRotation;
                    }

                    binding.startScale = bone.localScale;

                }

                if (notifyListeners) OnReinitBinding?.Invoke(binding);

                return binding;

            }


            public void ResetBinding(bool notifyListeners = true) => this.binding = ResetNewBinding(notifyListeners);

            public Binding ResetNewBinding(bool notifyListeners = true)
            {

                Binding binding = this.binding;

                binding.positionOffset = float3.zero;
                binding.rotationOffset = quaternion.identity;
                binding.scaleOffset = float3.zero;

                if (notifyListeners) OnReinitBinding?.Invoke(binding);

                return binding;

            }

            public event BindingChangeDelegate OnReinitBinding;

            public void ClearAllListeners()
            {
                OnReinitBinding = null;
            }

        }

#if UNITY_EDITOR
        public bool refreshStartPose;
#endif

        [SerializeField, HideInInspector]
        public bool hasStartingPose;

        public BoneBinding[] bindings;

        protected ProxyBoneJobs.ProxyBoneIndex registeredIndex = null;

        [SerializeField]
        public bool skipAutoRegister;

        public void Register()
        {
            if (registeredIndex == null) registeredIndex = ProxyBoneJobs.Register(this);
        }

        protected void Start()
        {

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                if (!skipAutoRegister) Register(); 

#if UNITY_EDITOR
            }
#endif

        }

        public void ReinitializeBindingStartPositions()
        {
            foreach (var boneBinding in bindings)
            {

                if (boneBinding == null) continue;

                boneBinding.ReinitializeStartPosition(transform); 

            }
        }

        public void ReinitializeBindingLocalStartPositions()
        {
            foreach (var boneBinding in bindings)
            {

                if (boneBinding == null) continue;

                boneBinding.ReinitializeLocalStartPosition(transform);

            }
        }

#if UNITY_EDITOR

        public void OnValidate()
        {

            if (bindings != null && !Application.isPlaying && refreshStartPose)
            {

                refreshStartPose = false;

                ReinitializeBindingStartPositions();

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
                if (registeredIndex != null)
                {
                    ProxyBoneJobs.Unregister(registeredIndex);
                    registeredIndex = null;
                }

                if (bindings != null)
                {
                    foreach (var binding in bindings) if (binding != null) binding.ClearAllListeners();
                }

#if UNITY_EDITOR
            }
#endif

        }

    }
}

#endif