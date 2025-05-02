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
            public string SerializedName => id;

            public int editIndex;

            public bool visible;
            public bool locked;

            public BoneGroup[] boneGroups;

            public IKGroup[] ikGroups;

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
            s.locked = source.locked;
            s.boneGroups = source.boneGroups;
            s.ikGroups = source.ikGroups;
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
            locked = serializable.locked;
            boneGroups = serializable.boneGroups;
            ikGroups = serializable.ikGroups;
            selectedBoneIndices = serializable.selectedBoneIndices;

            if (serializable.animationBank != null)
            {
                animationBank = new List<AnimationSource>();
                foreach (var source in serializable.animationBank) animationBank.Add(source.AsOriginalType(packageInfo));
                foreach (var source in animationBank) source.ApplyBaseDataSourceFromLoadedName(animationBank);  
            }

            lastPose = serializable.lastPose.AsOriginalType(packageInfo);
            referencePose = serializable.referencePose.AsOriginalType(packageInfo);

            Initialize(AnimationLibrary.FindAnimatable(id));

        }

        #endregion

        [Serializable]
        public struct AnimatablePropertyInfo
        {
            public string id;
            public string displayName;
            public float defaultValue;
            public bool isDynamic;
        }

        protected readonly List<AnimatablePropertyInfo> animatableProperties = new List<AnimatablePropertyInfo>();
        public int AnimatablePropertyCount => animatableProperties.Count;
        public AnimatablePropertyInfo GetAnimatablePropertyUnsafe(int index) => animatableProperties[index];
        public AnimatablePropertyInfo GetAnimatableProperty(int index) => index < 0 || index >= animatableProperties.Count ? default : animatableProperties[index]; 
        public bool TryGetAnimatablePropertyInfo(string id, out AnimatablePropertyInfo info)
        {
            info = default;

            foreach (var info_ in animatableProperties) if (info.id == id)
                {
                    info = info_; 
                    return true;
                }

            return false;
        }
        public void SetAnimatablePropertyInfo(string id, AnimatablePropertyInfo info)
        {
            for (int a = 0; a < animatableProperties.Count; a++)
            {
                var info_ = animatableProperties[a];
                if (info_.id == id)
                {
                    animatableProperties[a] = info;
                    return;
                }
            }
            animatableProperties.Add(info);
        }

        public void Initialize(AnimatableAsset asset)
        {
            AnimationEditor.AddAnimatableToScene(asset, out instance, out animator);

            if (instance != null)
            {
                //var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                //foreach (var smr in smrs) smr.bounds = new Bounds(instance.transform.position, new Vector3(10000, 10000, 10000)); 

                muscleController = instance.GetComponentInChildren<MuscularRenderedCharacter>();
                muscleProxy = instance.GetComponentInChildren<MuscleFlexProxy>();
                cameraProxy = instance.GetComponentInChildren<CameraProxy>();
            }

            DisablePlaybackOnlyDevices();

            Transform rootTransform = null;
            if (animator != null)
            {
                rootTransform = animator.transform;

                restPose = new AnimationUtils.Pose(animator);

                if (animator.avatar != null && animator.avatar.Poseable != null) 
                {
                    var poseable = animator.avatar.Poseable;

                    // Create bone groups and transfer any existing settings
                    var boneGroups_ = new BoneGroup[poseable.boneGroups.Length];
                    for (int a = 0; a < boneGroups_.Length; a++) boneGroups_[a] = new BoneGroup() { id = poseable.boneGroups[a].name, active = true };
                    if (boneGroups != null)
                    {
                        for (int a = 0; a < boneGroups_.Length; a++)
                        {
                            var bg_ = boneGroups_[a];
                            foreach(var bg in boneGroups)
                            {
                                if (bg == null || string.IsNullOrWhiteSpace(bg.id) || bg.id.AsID() != bg_.id.AsID()) continue;
                                bg_.active = bg.active;
                                break;
                            }
                        }
                    }
                    boneGroups = boneGroups_;

                }

                if (animator.IkManager != null)
                {

                    // Create ik groups and transfer any existing settings
                    var ikManager = animator.IkManager;
                    var ikGroups_ = new IKGroup[ikManager.ControllerCount];
                    for (int a = 0; a < ikGroups_.Length; a++) ikGroups_[a] = new IKGroup() { id = ikManager[a].name, active = false };
                    if (ikGroups != null)
                    {
                        for (int a = 0; a < ikGroups_.Length; a++)
                        {
                            var ikG_ = ikGroups_[a];
                            foreach (var ikG in ikGroups)
                            {
                                if (ikG == null || string.IsNullOrWhiteSpace(ikG.id) || ikG.id.AsID() != ikG_.id.AsID()) continue;
                                ikG_.active = ikG.active;
                                break;
                            }
                        }

                    }
                    ikGroups = ikGroups_;

                    ikManager.PostLateUpdate += animator.SyncIKFKOffsets; 
                }
            }
            else if (instance != null)
            {
                rootTransform = instance.transform;
                restPose = new AnimationUtils.Pose(instance.transform, animator == null ? null : animator.avatar);
            }

            if (rootTransform != null)
            {
                animatableProperties.Clear();

                var components = rootTransform.GetComponentsInChildren<Component>();
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        var compType = component.GetType();

                        bool isRoot = component.transform == rootTransform;

                        string prefix = string.Empty;
                        if (Attribute.IsDefined(compType, typeof(AnimatablePropertyPrefixAttribute)))
                        {
                            var attr = (AnimatablePropertyPrefixAttribute)Attribute.GetCustomAttribute(compType, typeof(AnimatablePropertyPrefixAttribute));
                            if (attr != null) prefix = attr.prefix;
                        }

                        var props = compType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        foreach (var prop in props)
                        {
                            if (prop == null || !Attribute.IsDefined(prop, typeof(AnimatablePropertyAttribute))) continue;

                            var attr = (AnimatablePropertyAttribute)Attribute.GetCustomAttribute(prop, typeof(AnimatablePropertyAttribute));

                            string displayName = $"{prefix}{(isRoot ? string.Empty : (component.name + "."))}{prop.Name}";
                            string id = $"{(isRoot ? IAnimator._animatorTransformPropertyStringPrefix : component.name)}.{compType.Name}.{prop.Name}";
                            float defaultValue = 0;
                            if (attr.hasDefaultValue)
                            {
                                defaultValue = attr.defaultValue;
                            }
                            else
                            {
                                try
                                {
                                    defaultValue = CustomAnimator.PropertyState.GetValue(prop, component, 0);
                                } 
                                catch(Exception e)
                                {
                                    swole.LogError(e);
                                    defaultValue = 0;
                                }
                            }

                            animatableProperties.Add(new AnimatablePropertyInfo() { id = id, displayName = displayName, defaultValue = defaultValue });
                        }

                        var fields = compType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        foreach (var field in fields)
                        {
                            if (field == null || !Attribute.IsDefined(field, typeof(AnimatablePropertyAttribute))) continue;

                            var attr = (AnimatablePropertyAttribute)Attribute.GetCustomAttribute(field, typeof(AnimatablePropertyAttribute));

                            string displayName = $"{prefix}{(isRoot ? string.Empty : (component.name + "."))}{field.Name}";
                            string id = $"{(isRoot ? IAnimator._animatorTransformPropertyStringPrefix : component.name)}.{compType.Name}.{field.Name}";
                            float defaultValue = 0;
                            if (attr.hasDefaultValue)
                            {
                                defaultValue = attr.defaultValue;
                            }
                            else
                            {
                                try
                                {
                                    defaultValue = CustomAnimator.PropertyState.GetValue(field, component, 0); 
                                }
                                catch (Exception e)
                                {
                                    swole.LogError(e);
                                    defaultValue = 0;
                                }
                            }

                            animatableProperties.Add(new AnimatablePropertyInfo() { id = id, displayName = displayName, defaultValue = defaultValue });
                        }
                    }
                }

                var dynamicAnimationPropertyComponents = rootTransform.GetComponentsInChildren<DynamicAnimationProperties>();
                foreach(var dap in dynamicAnimationPropertyComponents)
                {
                    var compType = dap.GetType();
                    bool isRoot = dap.transform == rootTransform;

                    for (int a = 0; a < dap.PropertyCount; a++)
                    {
                        var prop = dap.GetPropertyUnsafe(a);
                        if (prop == null) continue;

                        string id = $"{(isRoot ? IAnimator._animatorTransformPropertyStringPrefix : dap.name)}.{compType.Name}.{prop.name}"; 
                        string displayName = $"{(isRoot ? string.Empty : (dap.name + "."))}{prop.DisplayName}";
                        animatableProperties.Add(new AnimatablePropertyInfo() { id = id, displayName = displayName, defaultValue = prop.defaultValue, isDynamic = true });  
                    }
                }

                if (restPose != null)
                {
                    foreach(var prop in animatableProperties)
                    {
                        restPose.ReplaceElement(new AnimationUtils.AnimatableElement(prop.id, prop.defaultValue));
                    }
                }
            }

            IEnumerator WaitToApplyLastPose()
            {
                yield return null;
                yield return null;

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
                else if (referencePose != null)
                {
                    if (animator != null)
                    {
                        referencePose.ApplyTo(animator);
                    }
                    else if (instance != null)
                    {
                        referencePose.ApplyTo(instance.transform);
                    }
                }
            }

            CoroutineProxy.Start(WaitToApplyLastPose());  

        }

        public ImportedAnimatable() : base(default) { }

        [NonSerialized]
        public int index;

        public string displayName;

        public string id;
        public override string SerializedName => id;

        protected bool visible;
        public bool Visible
        {
            get => visible;
        }

        protected bool locked;
        public bool IsLocked => locked;

        public VoidParameterlessDelegate refreshVisibilityButtons;

        public struct UndoableSetVisibilityOfAnimatable : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public int animatableIndex;
            public bool prevVisible, visible;
            public bool prevLocked, locked;

            public UndoableSetVisibilityOfAnimatable(AnimationEditor editor, int animatableIndex, bool prevVisible, bool prevLocked, bool visible, bool locked)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;

                this.prevVisible = prevVisible;
                this.prevLocked = prevLocked;
                this.visible = visible;
                this.locked = locked;

                undoState = false;
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null)
                {
                    obj.SetVisibility(editor, visible, locked, false);
                }
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null)
                {
                    obj.SetVisibility(editor, prevVisible, prevLocked, false);
                }
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public void SetVisibility(AnimationEditor editor, bool visible, bool locked, bool undoable)
        {
            if (editor.IsPlayingPreview) editor.Pause(undoable);

            bool prevVisible = this.visible;
            bool prevLocked = this.locked;

            this.visible = visible;
            this.locked = locked;
            if (instance != null) instance.SetActive(visible);

            if (undoable && (prevVisible != visible || prevLocked != locked)) editor.RecordRevertibleAction(new UndoableSetVisibilityOfAnimatable(editor, index, prevVisible, prevLocked, visible, locked)); 

            refreshVisibilityButtons?.Invoke(); 
        }

        public void SetVisibilityOfSource(AnimationEditor editor, AnimationSource source, bool visible, bool locked, bool undoable)
        {
            if (source == null || animationBank == null) return;

            if (source.index < 0 || source.index >= animationBank.Count) source.index = animationBank.IndexOf(source);

            SetVisibilityOfSource(editor, source.index, visible, locked, undoable);
        }
        public void SetVisibilityOfSource(AnimationEditor editor, int sourceIndex, bool visible, bool locked, bool undoable)
        {
            if (animationBank == null) return;

            var source = animationBank[sourceIndex];
            if (source == null) return;
            
            source.SetVisibility(editor, this, visible, locked, undoable); 
        }

        public struct UndoableSetCurrentlyEditedSource : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public int animatableIndex, oldEditIndex, newEditIndex;

            public UndoableSetCurrentlyEditedSource(AnimationEditor editor, int animatableIndex, int oldEditIndex, int newEditIndex)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;
                this.oldEditIndex = oldEditIndex;
                this.newEditIndex = newEditIndex;

                undoState = false;
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null)
                {
                    obj.SetCurrentlyEditedSource(editor, newEditIndex, false);
                    editor.SetActiveObject(animatableIndex, true, true);
                }
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession; 
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null)
                {
                    obj.SetCurrentlyEditedSource(editor, oldEditIndex, false);
                    editor.SetActiveObject(animatableIndex, true, true);
                }
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public int editIndex;
        public void SetCurrentlyEditedSource(AnimationEditor editor, int sourceIndex, bool undoable)
        {
            int prevEditIndex = editIndex;

            if (editor.IsPlayingPreview && editIndex != sourceIndex) editor.Stop(undoable);
            editIndex = sourceIndex;
            editor.RefreshTimeline();
            
            editor.RefreshAnimationSettingsWindow(CurrentSource);
            
            if (undoable) editor.RecordRevertibleAction(new UndoableSetCurrentlyEditedSource(editor, index, prevEditIndex, editIndex)); 
        }

        [Serializable]
        public class BoneGroup
        {
            public string id;
            public bool active;
        }

        public BoneGroup[] boneGroups;
        public int BoneGroupCount => boneGroups == null ? 0 : boneGroups.Length;
        public BoneGroup GetBoneGroup(int index)
        {
            if (boneGroups == null || index < 0 || index >= boneGroups.Length) return null;
            return boneGroups[index];
        }
        public bool TryGetBoneGroup(string id, out BoneGroup group)
        {
            group = null;
            if (boneGroups == null) return false;

            id = id.AsID();
            foreach (var g in boneGroups) if (g.id.AsID() == id) 
                {
                    group = g;
                    return true;
                }

            return false;
        }

        [Serializable]
        public class IKGroup
        {
            public string id;
            public bool active;
        }

        public IKGroup[] ikGroups; 
        public bool TryGetIKGroup(string id, out IKGroup group)
        {
            group = null;
            if (ikGroups == null) return false;

            id = id.AsID();
            foreach (var g in ikGroups) if (g.id.AsID() == id)
                {
                    group = g;
                    return true;
                }

            return false;
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
        [NonSerialized]
        public MuscularRenderedCharacter muscleController;
        [NonSerialized]
        public MuscleFlexProxy muscleProxy;
        [NonSerialized]
        public CameraProxy cameraProxy;

        protected void TogglePlaybackOnlyDevices(bool enabled)
        {
            if (muscleProxy != null)
            {
                muscleProxy.disableMassOffsetsAsEditor = !enabled;
                muscleProxy.disableTrembling = !enabled;
            }
        }
        public void EnablePlaybackOnlyDevices() => TogglePlaybackOnlyDevices(true);      
        public void DisablePlaybackOnlyDevices() => TogglePlaybackOnlyDevices(false);
        

        public GameObject CreateSnapshot() => CreateSnapshot(instance.name + "_lastPose", lastPose);
        public GameObject CreateSnapshot(string name, AnimationUtils.Pose pose)
        {
            if (animator == null) return null;

            var copy = GameObject.Instantiate(animator.gameObject).GetComponent<CustomAnimator>();
            copy.name = name;
            IEnumerator Wait()
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                pose.ApplyTo(copy);
            }
            CoroutineProxy.Start(Wait());  

            return copy.gameObject;
        }

        public List<int> selectedBoneIndices;

        public List<AnimationSource> animationBank;
        public int SourceCount => animationBank == null ? 0 : animationBank.Count;
        public AnimationSource this[int index] => animationBank == null || index < 0 || index >= animationBank.Count ? null : animationBank[index];

        /// <summary>
        /// Used by the animation editor to create a collection of animation sources to compile.
        /// </summary>
        [NonSerialized]
        public List<AnimationSource> compilationList = new List<AnimationSource>();

        public AnimationSource CreateNewAnimationSource(AnimationEditor editor, string animationName, float length, bool undoable)
        {
            var source = new AnimationSource() { DisplayName = animationName, timelineLength = length, playbackSpeed = 1, Mix = 1, rawAnimation = new CustomAnimation(animationName, string.Empty, DateTime.Now, DateTime.Now, string.Empty, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null) };
            source.SetVisibility(editor, this, true, false, false);
            source.MarkAsDirty();
            AddNewAnimationSource(editor, source, undoable);   
            return source;
        }
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, string animationName, bool undoable) => LoadNewAnimationSource(editor, animationName, AnimationLibrary.FindAnimation(animationName), undoable);
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, PackageIdentifier package, string animationName, bool undoable) => LoadNewAnimationSource(editor, animationName, AnimationLibrary.FindAnimation(package, animationName), undoable);
        public AnimationSource LoadNewAnimationSource(AnimationEditor editor, string displayName, CustomAnimation animationToLoad, bool undoable)
        {
            if (animationToLoad == null) return null;

            var source = new AnimationSource() { DisplayName = displayName, timelineLength = animationToLoad.LengthInSeconds, playbackSpeed = 1, Mix = 1, rawAnimation = animationToLoad.Duplicate() };
            source.SetVisibility(editor, this, true, false, false);
            AddNewAnimationSource(editor, source, undoable);
            return source;
        }
        public void AddNewAnimationSource(AnimationEditor editor, AnimationSource source, bool undoable, int index = -1, bool setAsActiveSource = true, bool addToList = true)
        {

            if (source == null) return; 

            if (animationBank == null) animationBank = new List<AnimationSource>();

            if (addToList)
            {
                if (animationBank.Contains(source)) return;

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
            source.SetVisibility(editor, this, setAsActiveSource || source.Visible, source.IsLocked, false);  

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

            if (undoable) editor.RecordRevertibleAction(new AnimationEditor.UndoableCreateNewAnimationSource(editor, this, source, source.index, editIndex));
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

        public void RepositionSource(AnimationEditor editor, int sourceIndex, int newIndex, bool undoable = true)
        {
            if (animationBank == null || sourceIndex < 0) return;

            RepositionSource(editor, animationBank[sourceIndex], newIndex);
        }
        public void RepositionSource(AnimationEditor editor, AnimationSource source, int newIndex, bool undoable = true)
        {
            if (animationBank == null || source == null || newIndex < 0) return;

            if (editor.IsPlayingPreview) editor.Pause(undoable);

            int sourceIndex = animationBank.IndexOf(source);
            if (sourceIndex == newIndex || sourceIndex < 0)
            {
                source.index = sourceIndex;
                return;
            }

            if (newIndex >= animationBank.Count) newIndex = animationBank.Count - 1;

            var swappedSource = animationBank[newIndex];
            animationBank[newIndex] = source;
            animationBank[sourceIndex] = swappedSource;

            RefreshBankIndices(0);
            RebuildController();
             
            if (editIndex == sourceIndex) editIndex = source.index; else if (editIndex == newIndex) editIndex = swappedSource.index;

            source.RefreshListMemberPosition();
            swappedSource.RefreshListMemberPosition();

            editor.MarkSessionDirty();

            if (undoable) editor.RecordRevertibleAction(new AnimationEditor.UndoableSwapAnimationSourceIndex(editor, index, sourceIndex, newIndex));  
        }
        public void RemoveAnimationSource(AnimationEditor editor, AnimationSource source, bool undoable = true)
        {
            if (source == null || animationBank == null) return;

            /*if (source.index < 0 || source.index >= animationBank.Count)*/ source.index = animationBank.IndexOf(source);

            RemoveAnimationSource(editor, source.index, undoable);
        }
        public void RemoveAnimationSource(AnimationEditor editor, int index, bool undoable = true)
        {
            if (animationBank == null || index < 0 || index >= animationBank.Count) return;

            if (editor.IsPlayingPreview) editor.Pause(undoable);

            var source = animationBank[index];
            if (source != null)
            {
                if (source.listMember != null)
                {
                    editor.importedAnimatablesList.RemoveListMember(source.listMember);
                    source.listMember = null;
                }

                if (!undoable) source.Destroy();
            }
            animationBank.RemoveAt(index);

            RefreshBankIndices(index);
            RebuildController();

            editor.MarkSessionDirty();      

            if (undoable) editor.RecordRevertibleAction(new AnimationEditor.UndoableRemoveAnimationSource(editor, this, source, index, editIndex)); 

            if (editIndex == index)
            {
                if (editIndex >= animationBank.Count)
                {
                    editIndex = animationBank.Count - 1;
                }

                SetCurrentlyEditedSource(editor, editIndex, false);
            }
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
                    restPose.Insert(animator == null ? null : animator.avatar, animation, 0, restPose);
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
                lastPose = new AnimationUtils.Pose(instance.transform, null);
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

                    previewLayer.SetActive(source.Visible);
                    previewLayer.name = $"{a}";
                    previewLayer.blendParameterIndex = -1;
                    previewLayer.entryStateIndex = 0;
                    previewLayer.mix = source.GetMix(); 
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