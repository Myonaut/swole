#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Swole.API.Unity.Animation;

using static Swole.API.Unity.RenderedCharacter.BlendShapeWeight;

namespace Swole.API.Unity
{
    public class RenderedCharacterUpdater : SingletonBehaviour<RenderedCharacterUpdater>
    {
        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1; // update after animation
        public override int Priority => ExecutionPriority;
        public override bool DestroyOnLoad => false; 

        protected readonly List<RenderedCharacter> characters = new List<RenderedCharacter>();
        public static bool Register(RenderedCharacter character)
        {
            var instance = Instance;
            if (character == null || instance == null) return false;

            if (!instance.characters.Contains(character)) instance.characters.Add(character);
            return true;
        }
        public static bool Unregister(RenderedCharacter character)
        {
            var instance = Instance;
            if (character == null || instance == null) return false;

            return instance.characters.Remove(character);
        } 

        public override void OnFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            foreach (var character in characters) if (character != null) character.LateUpdateStep();
            characters.RemoveAll(i => i == null || i.OverrideUpdateCalls);
        }

        public override void OnUpdate()
        {
            foreach (var character in characters) if (character != null) character.UpdateStep();
        }
    }
    public class RenderedCharacter : MonoBehaviour
    {

        public List<RenderedCharacter> children;

        public BlendShapeWeight[] blendShapeWeights;

        //

        public Transform rootTransform;

        public SkinnedMeshRenderer[] inputSurfaceRenderers;

        public virtual void Initialize()
        {

            InitializationStart();

            if (inputSurfaceRenderers != null && m_Surfaces == null)
            {

                m_Surfaces = new CharacterSurface[inputSurfaceRenderers.Length];

                for (int a = 0; a < inputSurfaceRenderers.Length; a++) m_Surfaces[a] = new CharacterSurface(this, inputSurfaceRenderers[a]);

            }

        }

        protected virtual void Awake()
        {

            Initialize();

            SetOverrideUpdateCalls(OverrideUpdateCalls); // Force register to updater

        }

        //

        protected SkinnedMeshRenderer[] m_skinnedRenderers;
        protected CustomSkinnedMeshRenderer[] m_customSkinnedRenderers;

        [Serializable]
        public class BlendShapeWeight
        {

            public string name;

            [Range(-1, 2)]
            public float weight;

            [HideInInspector]
            protected float prevWeight;

            public bool HasUpdated(bool reset = true)
            {

                bool updated = weight != prevWeight;

                if (reset) prevWeight = weight;

                return updated;

            }

            [Serializable]
            public struct LocalShape
            {

                public int index;
                public float maxWeight;

            }

            [HideInInspector]
            public LocalShape[] shapeInSkinnedRenderers;
            [HideInInspector]
            public LocalShape[] shapeInCustomSkinnedRenderers;

            public void UpdateRenderers(SkinnedMeshRenderer[] skinnedRenderers, CustomSkinnedMeshRenderer[] customSkinnedRenderers, bool force = false)
            {

                if (!force && !HasUpdated()) return;

                for (int a = 0; a < skinnedRenderers.Length; a++)
                {
                    var renderer = skinnedRenderers[a];
                    if (renderer == null) continue;
                    var localShape = shapeInSkinnedRenderers[a];
                    if (localShape.index < 0) continue;
                    renderer.SetBlendShapeWeight(localShape.index, localShape.maxWeight * weight);
                }
                for (int a = 0; a < customSkinnedRenderers.Length; a++)
                {
                    var renderer = customSkinnedRenderers[a];
                    if (renderer == null) continue;
                    var localShape = shapeInCustomSkinnedRenderers[a];
                    if (localShape.index < 0) continue;
                    renderer.SetBlendShapeWeight(localShape.index, localShape.maxWeight * weight);
                }

            }

        }

        public int GetBlendShapeIndex(string blendShapeName)
        {

            if (blendShapeWeights == null) return -1;

            for (int a = 0; a < blendShapeWeights.Length; a++) if (blendShapeWeights[a].name == blendShapeName) return a;

            blendShapeName = blendShapeName.ToLower().Trim();

            for (int a = 0; a < blendShapeWeights.Length; a++) if (blendShapeWeights[a].name.ToLower().Trim() == blendShapeName) return a;

            return -1;

        }

        public string GetBlendShapeName(int shapeIndex)
        {

            if (blendShapeWeights == null || shapeIndex < 0 || shapeIndex >= blendShapeWeights.Length) return "";

            return blendShapeWeights[shapeIndex].name;

        }

        public void SetBlendShapeWeight(string blendShapeName, float weight)
        {

            SetBlendShapeWeight(GetBlendShapeIndex(blendShapeName), weight);

        }

        public void SetBlendShapeWeight(int blendShapeIndex, float weight)
        {

            if (blendShapeWeights == null || blendShapeIndex < 0 || blendShapeIndex >= blendShapeWeights.Length) return;

            var shape = blendShapeWeights[blendShapeIndex];

            shape.weight = weight;
            shape.UpdateRenderers(m_skinnedRenderers, m_customSkinnedRenderers);

            if (children != null)
            {
                foreach(var child in children) if (child != null) child.SetBlendShapeWeight(child.GetBlendShapeIndex(shape.name), weight);
            }

        }

#if UNITY_EDITOR

        protected virtual void OnValidate()
        {

            if (blendShapeWeights == null || m_skinnedRenderers == null || m_customSkinnedRenderers == null) return;

            foreach (BlendShapeWeight weight in blendShapeWeights) weight.UpdateRenderers(m_skinnedRenderers, m_customSkinnedRenderers);

        }

#endif

        protected virtual void InitializationStart()
        {

            if (rootTransform == null) rootTransform = transform;

            m_skinnedRenderers = rootTransform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true); 
            m_customSkinnedRenderers = rootTransform.gameObject.GetComponentsInChildren<CustomSkinnedMeshRenderer>(true);

            Dictionary<string, BlendShapeWeight> blendShapeWeightsCreator = new Dictionary<string, BlendShapeWeight>();

            BlendShapeWeight GetBlendShapeWeightWrapper(string shapeName)
            {

                if (string.IsNullOrEmpty(shapeName)) return null;

                if (!blendShapeWeightsCreator.TryGetValue(shapeName, out BlendShapeWeight bsw))
                {

                    if (!blendShapeWeightsCreator.TryGetValue(shapeName.ToLower().Trim(), out bsw))
                    {

                        bsw = new BlendShapeWeight() { name = shapeName, shapeInSkinnedRenderers = new LocalShape[m_skinnedRenderers.Length], shapeInCustomSkinnedRenderers = new LocalShape[m_customSkinnedRenderers.Length] };
                        for (int a = 0; a < m_skinnedRenderers.Length; a++) bsw.shapeInSkinnedRenderers[a] = new LocalShape() { index = -1 };
                        for (int a = 0; a < m_customSkinnedRenderers.Length; a++) bsw.shapeInCustomSkinnedRenderers[a] = new LocalShape() { index = -1 };

                        blendShapeWeightsCreator[shapeName] = bsw;

                    }

                }

                return bsw;

            }

            for (int a = 0; a < m_skinnedRenderers.Length; a++)
            {

                var renderer = m_skinnedRenderers[a];

                if (renderer == null || renderer.sharedMesh == null) continue;

                var mesh = renderer.sharedMesh;

                for (int b = 0; b < mesh.blendShapeCount; b++)
                {

                    string shapeName = mesh.GetBlendShapeName(b);

                    var bsw = GetBlendShapeWeightWrapper(shapeName);

                    if (bsw == null) continue;

                    bsw.shapeInSkinnedRenderers[a] = new LocalShape() { index = b, maxWeight = mesh.GetBlendShapeFrameWeight(b, mesh.GetBlendShapeFrameCount(b) - 1) };

                }

            }

            for (int a = 0; a < m_customSkinnedRenderers.Length; a++)
            {

                var renderer = m_customSkinnedRenderers[a];

                if (renderer == null) continue;

                for (int b = 0; b < renderer.BlendShapeCount; b++)
                {

                    string shapeName = renderer.GetBlendShapeName(b);

                    var bsw = GetBlendShapeWeightWrapper(shapeName);

                    if (bsw == null) continue;

                    bsw.shapeInCustomSkinnedRenderers[a] = new LocalShape() { index = b, maxWeight = renderer.GetBlendShapeFrameWeight(b, renderer.GetBlendShapeFrameCount(b) - 1) };

                }

            }

            blendShapeWeights = blendShapeWeightsCreator.Values.ToArray();

        }

        protected CharacterSurface[] m_Surfaces;

        public int SurfaceCount => m_Surfaces == null ? 0 : m_Surfaces.Length;
        public CharacterSurface GetSurface(int index) => m_Surfaces == null || index < 0 || index > m_Surfaces.Length ? null : GetSurfaceUnsafe(index);
        public CharacterSurface GetSurfaceUnsafe(int index) => m_Surfaces[index];
        public int FindSurfaceIndex(string surfaceName)
        {

            if (m_Surfaces == null || string.IsNullOrEmpty(surfaceName)) return -1;

            for (int a = 0; a < m_Surfaces.Length; a++)
            {

                var surface = m_Surfaces[a];

                if (surface == null) continue;

                if (surface.Name == surfaceName) return a;

            }

            surfaceName = surfaceName.ToLower().Trim();

            for (int a = 0; a < m_Surfaces.Length; a++)
            {

                var surface = m_Surfaces[a];

                if (surface == null) continue;

                if (surface.Name.ToLower().Trim() == surfaceName) return a;

            }

            return -1;

        }

        public int IndexOfSurface(string surfaceName)
        {

            if (m_Surfaces != null)
            {

                for (int a = 0; a < m_Surfaces.Length; a++)
                {

                    var surface = m_Surfaces[a];

                    if (surface == null) continue;

                    if (surface.Name == surfaceName) return a;

                }

                string genericName = surfaceName.ToLower().Trim();

                for (int a = 0; a < m_Surfaces.Length; a++)
                {

                    var surface = m_Surfaces[a];

                    if (surface == null) continue;

                    if (surface.Name.ToLower().Trim() == genericName) return a;

                }

                for (int a = 0; a < m_Surfaces.Length; a++)
                {

                    var surface = m_Surfaces[a];

                    if (surface == null) continue;

                    if (surface.Name.ToLower().Trim().StartsWith(genericName)) return a;

                }

            }

            return -1;

        }

        protected virtual void OnDestroy()
        {

            if (m_Surfaces != null)
            {

                foreach (var surface in m_Surfaces) if (surface != null) surface.Dispose();

            }

            m_Surfaces = null;

        }

        [SerializeField]
        protected bool overrideUpdateCalls;
        public bool OverrideUpdateCalls
        {
            get => overrideUpdateCalls;
            set => SetOverrideUpdateCalls(value);
        }
        public void SetOverrideUpdateCalls(bool value)
        {
            overrideUpdateCalls = value;
            if (value)
            {
                RenderedCharacterUpdater.Unregister(this);
            }
            else
            {
                RenderedCharacterUpdater.Register(this);
            }
        }

        public virtual void UpdateStep()
        {

            /*if (m_Surfaces != null)
            {

                foreach (var surface in m_Surfaces) if (surface != null) surface.Refresh();

            }*/

        }

        public virtual void LateUpdateStep()
        {

            /*if (m_Surfaces != null)
            {

                foreach (var surface in m_Surfaces) if (surface != null) surface.CompleteJobs();

            }*/

            if (m_Surfaces != null)
            {
                foreach (var surface in m_Surfaces) if (surface != null) EndFrameJobWaiter.WaitFor(surface.Refresh(true));
            }

        }

    }

}

#endif