#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.UI;
using Swole.Animation;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class ImportedAnimatable : SwoleObject<ImportedAnimatable, ImportedAnimatable.Serialized>
    {

        public void Destroy()
        {
            if (previewController != null)
            {
                if (animator != null) animator.RemoveControllerData(previewController);
                GameObject.Destroy(previewController);
                previewController = null;
            }

            if (animationBank != null)
            {
                foreach (var source in animationBank)
                {
                    if (source == null) continue;
                    source.Destroy();
                }
                animationBank = null;
            }

            DestroyRestPoseController();

            if (compilationList != null)
            {
                compilationList.Clear();
                compilationList = null;
            }

            if (instance != null)
            {
                GameObject.Destroy(instance);
                instance = null;
            }
        }

        #region Serialization

        public override Serialized AsSerializableStruct() => this;
        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<ImportedAnimatable, ImportedAnimatable.Serialized>
        {

            public string displayName;

            public string id;

            public int editIndex;

            public bool visible;

            public List<int> selectedBoneIndices;

            public List<AnimationSource.Serialized> animationBank;

            public AnimationUtils.Pose.Serialized lastPose;
            public AnimationUtils.Pose.Serialized referencePose;

            public ImportedAnimatable AsOriginalType(PackageInfo packageInfo = default) => new ImportedAnimatable(this, packageInfo);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
            public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
        }

        public static implicit operator Serialized(ImportedAnimatable source)
        {
            Serialized s = new Serialized();

            s.displayName = source.displayName;
            s.id = source.id;
            s.editIndex = source.editIndex;
            s.visible = source.visible;
            s.selectedBoneIndices = source.selectedBoneIndices;
            if (source.animationBank != null)
            {
                s.animationBank = new List<AnimationSource.Serialized>();
                foreach (var anim in source.animationBank) s.animationBank.Add(anim);
            }
            s.lastPose = source.lastPose;
            s.referencePose = source.referencePose;

            return s;
        }

        public ImportedAnimatable(ImportedAnimatable.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {

            displayName = serializable.displayName;
            id = serializable.id;
            editIndex = serializable.editIndex;
            visible = serializable.visible;
            selectedBoneIndices = serializable.selectedBoneIndices;

            if (serializable.animationBank != null)
            { 
                animationBank = new List<AnimationSource>();
                foreach (var source in serializable.animationBank) animationBank.Add(source.AsOriginalType(packageInfo));
            }

            lastPose = serializable.lastPose.AsOriginalType(packageInfo);
            referencePose = serializable.referencePose.AsOriginalType(packageInfo);

            AnimationEditor.AddAnimatableToScene(AnimationLibrary.FindAnimatable(id), out instance, out animator);

            if (animator != null)
            {
                restPose = new AnimationUtils.Pose(animator);
            }
            else if (instance != null)
            {
                restPose = new AnimationUtils.Pose(instance.transform);
            }

            if (lastPose != null)
            {
                if (animator != null)
                {
                    lastPose.ApplyTo(animator);
                }
                else if (instance != null)
                {
                    lastPose.ApplyTo(instance.transform);
                }
            }

        }

        #endregion

        public ImportedAnimatable() : base(default) { }

        [NonSerialized]
        public int index;

        public string displayName;

        public string id;

        public bool visible;
        public void SetVisibility(AnimationEditor editor, bool visible)
        {
            if (editor.IsPlayingPreview) editor.Pause();

            this.visible = visible;
            if (instance != null) instance.SetActive(visible);
        }

        public void SetVisibilityOfSource(AnimationEditor editor, AnimationSource source, bool visible)
        {
            if (source == null || animationBank == null) return;

            if (source.index < 0 || source.index >= animationBank.Count) source.index = animationBank.IndexOf(source);

            SetVisibilityOfSource(editor, source.index, visible);
        }
        public void SetVisibilityOfSource(AnimationEditor editor, int sourceIndex, bool visible)
        {
            if (animationBank == null) return;

            var source = animationBank[sourceIndex];
            if (source == null) return;

            source.SetVisibility(editor, visible);
        }

        public int editIndex;
        public void SetCurrentlyEditedSource(AnimationEditor editor, int sourceIndex)
        {
            editIndex = sourceIndex;
            editor.RefreshTimeline();
        }

        /// <summary>
        /// The root game object instance
        /// </summary>
        [NonSerialized]
        public GameObject instance;
        [NonSerialized]
        public UICategorizedList.Category listCategory;
        [NonSerialized]
        public CustomAnimator animator;

        public List<int> selectedBoneIndices;

        public List<AnimationSource> animationBank;
        public int SourceCount => animationBank == null ? 0 : animationBank.Count;
        public AnimationSource this[int index] => animationBank == null || index < 0 || index >= animationBank.Count ? null : animationBank[index];

        /// <summary>
        /// Used by the animation editor to create a collection of animation sources to compile.
        /// </summary>
        [NonSerialized]
        public List<AnimationSource> compilationList = new List<AnimationSource>();

        public AnimationSource CreateNewAnimationSource(AnimationEditor editor, string animationName, float length)
        {
            var source = new AnimationSource() { DisplayName = animationName, timelineLength = length, playbackSpeed = 1, rawAnimation = new CustomAnimation(animationName, string.Empty, DateTime.Now, DateTime.Now, string.Empty, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null) };
            source.MarkAsDirty();
            AddNewAnimationSource(editor, source);
            return source;
        }
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, string animationName) => LoadNewAnimationSource(editor, animationName, AnimationLibrary.FindAnimation(animationName));
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, PackageIdentifier package, string animationName) => LoadNewAnimationSource(editor, animationName, AnimationLibrary.FindAnimation(package, animationName));
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, string displayName, CustomAnimation animationToLoad)
        {
            if (animationToLoad == null) return null;

            var source = new AnimationSource() { DisplayName = displayName, timelineLength = animationToLoad.LengthInSeconds, playbackSpeed = 1, rawAnimation = animationToLoad.Duplicate() };
            AddNewAnimationSource(editor, source);
            return source;
        }
        public void AddNewAnimationSource(AnimationEditor editor, AnimationSource source, int index = -1, bool setAsActiveSource = true, bool addToList = true)
        {

            if (source == null) return;

            if (animationBank == null) animationBank = new List<AnimationSource>();

            if (addToList)
            {
                string name = source.DisplayName;
                bool flag = true;
                int i = 0;
                while (flag)
                {
                    source.DisplayName = i > 0 ? $"{name} ({i})" : name;

                    flag = false;
                    foreach (var source_ in animationBank)
                    {
                        if (source_ == null) continue;
                        if (source_.DisplayName == source.DisplayName)
                        {
                            i++;
                            flag = true;
                        }
                    }
                }

                if (index < 0 || index >= animationBank.Count)
                {
                    index = animationBank.Count;
                    animationBank.Add(source);
                } 
                else
                {
                    animationBank.Insert(index, source);
                    RefreshBankIndices(index);
                }
            }

            source.index = index;
            source.previewAnimator = animator;
            source.visible = true;

            if (setAsActiveSource) editIndex = source.index;

            source.CreateListMember(editor, this, true); 

            editor.RefreshAnimatableListUI();
            source.ClearListeners(); 
            source.OnBecomeDirty += editor.RefreshPackageLinksUI;
            source.OnMarkDirty += editor.MarkSessionDirty;
            if (editor.ActiveAnimatable == this) editor.RefreshTimeline();

            if (addToList) 
            {
                RebuildController();
                editor.MarkSessionDirty(); 
            }
        }

        public void RefreshBankIndices(int startIndex = 0)
        {
            if (animationBank == null) return;

            for (int i = startIndex; i < animationBank.Count; i++)
            {
                var anim = animationBank[i];
                if (anim == null) continue;
                animationBank[i].index = i;
            }
        }
        public void RemoveAnimationSource(AnimationEditor editor, AnimationSource source)
        {
            if (source == null || animationBank == null) return;

            if (source.index < 0 || source.index >= animationBank.Count) source.index = animationBank.IndexOf(source);

            RemoveAnimationSource(editor, source.index);
        }
        public void RemoveAnimationSource(AnimationEditor editor, int index)
        {
            if (animationBank == null || index < 0 || index >= animationBank.Count) return;

            if (editor.IsPlayingPreview) editor.Pause();

            var source = animationBank[index];
            if (source != null)
            {
                if (source.listMember != null)
                {
                    editor.importedAnimatablesList.RemoveListMember(source.listMember);
                    source.listMember = null;
                }

                source.Destroy();
            }
            animationBank.RemoveAt(index);

            RefreshBankIndices(index);
            RebuildController();
        }

        [NonSerialized]
        private AnimationUtils.Pose restPose;
        [NonSerialized]
        private CustomAnimationController restPoseController;

        public void DestroyRestPoseController()
        {
            if (restPoseController == null) return;
            if (animator != null) animator.RemoveControllerData(restPoseController);
            GameObject.Destroy(restPoseController);
            restPoseController = null;
        }
        public AnimationUtils.Pose RestPose
        {
            get => restPose;
            set
            {
                DestroyRestPoseController();

                restPose = value;
                if (restPose != null)
                {
                    restPoseController = ScriptableObject.CreateInstance<CustomAnimationController>();
                    restPoseController.name = AnimationEditor.restPosePrefix;

                    var animation = new CustomAnimation("restPoseBase", null, DateTime.MinValue, DateTime.MinValue, null, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null);
                    restPose.Insert(animation, 0, restPose);
                    var animations = new CustomMotionController.AnimationReference[] { (new CustomMotionController.AnimationReference($"restPoseBase", animation, AnimationLoopMode.Loop)) };

                    CustomAnimationLayer previewLayer = new CustomAnimationLayer();
                    var layers = new CustomAnimationLayer[] { previewLayer };

                    previewLayer.SetActive(true);
                    previewLayer.name = AnimationEditor.restPoseLayer_Main; 
                    previewLayer.blendParameterIndex = -1;
                    previewLayer.entryStateIndex = 0;
                    previewLayer.mix = 1;
                    previewLayer.IsAdditive = false;
                    previewLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = 0, type = MotionControllerType.AnimationReference } };

                    CustomStateMachine previewStateMachine = new CustomStateMachine() { name = "restPoseBaseState", motionControllerIndex = 0 };
                    previewLayer.stateMachines = new CustomStateMachine[] { previewStateMachine };

                    restPoseController.animationReferences = animations;
                    restPoseController.layers = layers;
                }
            }
        }
        public CustomAnimationController RestPoseController
        {
            get
            {
                if (restPoseController == null && restPose != null) RestPose = restPose; // Forces controller to be created
                return restPoseController;
            }
        }
        public void AddRestPoseAnimationToAnimator(CustomAnimator animator = null)
        {
            if (RestPoseController == null) return;
            if (animator == null) animator = this.animator;
            if (animator == null) return;

            animator.ApplyController(restPoseController);
        }
        public void RemoveRestPoseAnimationFromAnimator(CustomAnimator animator = null)
        {
            if (restPoseController == null) return;
            if (animator == null) animator = this.animator;
            if (animator == null) return;

            animator.RemoveControllerData(restPoseController);
        }
        public CustomAnimationLayer GetRestPoseAnimationLayer(CustomAnimator animator = null)
        {
            if (restPoseController == null) return null;
            if (animator == null) animator = this.animator;
            if (animator == null) return null;

            return animator.FindTypedLayer(restPoseController.Prefix + AnimationEditor.restPoseLayer_Main);
        }


        /// <summary>
        /// The last registered pose of the animatable in the scene.
        /// </summary>
        public AnimationUtils.Pose lastPose;
        public void RefreshLastPose()
        {
            if (animator != null)
            {
                lastPose = new AnimationUtils.Pose(animator);
            } 
            else if (instance != null)
            {
                lastPose = new AnimationUtils.Pose(instance.transform);
            }
        }
        /// <summary>
        /// The pose from which to determine what data has changed when adding a keyframe.
        /// </summary>
        public AnimationUtils.Pose referencePose;

        [NonSerialized]
        private AnimationUtils.Pose tempPose;
        /// <summary>
        /// A pose object that can be used for anything temporary.
        /// </summary>
        public AnimationUtils.Pose TempPose
        {
            get
            {
                if (tempPose == null) tempPose = new AnimationUtils.Pose();
                return tempPose;
            }
        }

        /// <summary>
        /// Used by animation editor to record the animatable's next pose as the reference pose at the end of the frame.
        /// </summary>
        [NonSerialized]
        public bool setNextPoseAsReference;
        public void SetCurrentPoseAsReference()
        {
            if (animator != null)
            {
                referencePose = new AnimationUtils.Pose(animator);
            } 
            else if (instance != null)
            {
                referencePose = new AnimationUtils.Pose(instance.transform);
            }
            if (referencePose != null) lastPose = referencePose.Duplicate(); 
        }

        public AnimationSource CurrentSource
        {
            get
            {
                if (editIndex < 0 || animationBank == null || editIndex >= animationBank.Count) return null;
                return animationBank[editIndex];
            }
        }

        public CustomAnimation CurrentCompiledAnimation
        {
            get
            {
                var source = CurrentSource;
                if (source == null) return null;
                return source.CompiledAnimation;
            }
        }

        public void RebuildController()
        {
            if (animator != null && previewController != null) animator.RemoveControllerData(previewController);

            if (previewController == null)
            {
                previewController = ScriptableObject.CreateInstance<CustomAnimationController>();
                previewController.name = AnimationEditor.previewPrefix; 
            }

            List<CustomMotionController.AnimationReference> animations = new List<CustomMotionController.AnimationReference>();
            List<CustomAnimationLayer> layers = new List<CustomAnimationLayer>();
            if (animationBank != null)
            {
                for (int a = 0; a < animationBank.Count; a++)
                {
                    var source = animationBank[a];
                    animations.Add(new CustomMotionController.AnimationReference($"{a}", source.PreviewAsset, AnimationLoopMode.Loop));

                    CustomAnimationLayer previewLayer = new CustomAnimationLayer();
                    source.previewLayer = previewLayer;
                    layers.Add(previewLayer);

                    previewLayer.SetActive(source.visible);
                    previewLayer.name = $"{a}";
                    previewLayer.blendParameterIndex = -1;
                    previewLayer.entryStateIndex = 0;
                    previewLayer.mix = source.GetMixFromSlider(); 
                    previewLayer.IsAdditive = true;
                    previewLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = a, type = MotionControllerType.AnimationReference } };

                    CustomStateMachine previewStateMachine = new CustomStateMachine() { name = "animationPreviewState", motionControllerIndex = 0 };
                    previewLayer.stateMachines = new CustomStateMachine[] { previewStateMachine };
                }
            }

            previewController.animationReferences = animations.ToArray(); 
            previewController.layers = layers.ToArray();

            if (animator != null) 
            { 
                animator.ApplyController(previewController);
                if (animationBank != null)
                {
                    for (int a = 0; a < animationBank.Count; a++)
                    {
                        var source = animationBank[a];
                        if (source == null || source.previewLayer == null) continue;
                        source.previewLayer = animator.FindTypedLayer(previewController.Prefix + source.previewLayer.name);
                    }
                }
            }
        } 
        public void RebuildControllerIfNull()
        {
            if (previewController == null) RebuildController();
        }
        [NonSerialized]
        private CustomAnimationController previewController;
        public CustomAnimationController PreviewController
        {
            get
            {
                if (previewController == null) RebuildController();
                return previewController;
            }
        }

    }
}

#endif