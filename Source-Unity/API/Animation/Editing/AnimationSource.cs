#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Audio;

using Unity.Mathematics;

using Swole.UI;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class AnimationSource : SwoleObject<AnimationSource, AnimationSource.Serialized>
    {

        [SerializeField]
        protected bool invalid;
        public bool IsValid => !invalid;
        public void Destroy()
        {
            invalid = true;
            if (previewAsset != null)
            {
                GameObject.Destroy(previewAsset);
                previewAsset = null;
            }
            if (rawAnimation != null) 
            { 
                rawAnimation.FlushJobData(); 
                rawAnimation = null;
            }
            if (compiledAnimation != null)
            {
                compiledAnimation.FlushJobData();
                compiledAnimation = null;
            }

            ClearListeners();
        }
        public void ClearListeners()
        {
            OnBecomeDirty = null;
            OnMarkDirty = null;
        }

        #region Serialization

        public override AnimationSource.Serialized AsSerializableStruct() => this;
        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<AnimationSource, AnimationSource.Serialized>
        {
            public float timelineLength;

            public float lastPlaybackPosition;
            public float lastPlaybackSpeed;

            public float mix;

            public PackageIdentifier package;
            public string syncedName;
            public string animationName;
            public string SerializedName => animationName;

            public bool visible;
            public bool locked;

            public bool isDirty;

            public List<AudioSyncImport> audioSyncImports;

            public BaseDataType baseDataMode;
            public string baseDataSource;
            public float baseDataReferenceTime;

            public List<AnimationUtils.LoopPoint> loopPoints;

            public CustomAnimation.Serialized sessionLocalAnimation;

            public AnimationSource AsOriginalType(PackageInfo packageInfo = default) => new AnimationSource(this, packageInfo);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo); 

            public string AsJSON(bool prettyPrint = false) => swole.ToJson(this, prettyPrint);
            public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
        }

        public static implicit operator Serialized(AnimationSource source)
        {
            Serialized s = new Serialized();

            s.timelineLength = source.timelineLength;

            s.lastPlaybackPosition = source.playbackPosition;
            s.lastPlaybackSpeed = source.playbackSpeed;

            s.mix = source.Mix;

            s.package = source.package;
            s.syncedName = source.syncedName;
            s.animationName = source.displayName;

            s.visible = source.visible;
            s.locked = source.locked;

            s.isDirty = source.isDirty;

            s.audioSyncImports = source.audioSyncImports;

            s.baseDataMode = source.baseDataMode;
            s.baseDataSource = source.baseDataSource == null ? string.Empty : source.baseDataSource.displayName; 
            s.baseDataReferenceTime = source.baseDataReferenceTime;

            s.loopPoints = source.loopPoints;
            if (source.rawAnimation != null) s.sessionLocalAnimation = source.rawAnimation.AsSerializableStruct();

            return s;
        }

        public AnimationSource(AnimationSource.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {
            timelineLength = serializable.timelineLength;

            playbackPosition = serializable.lastPlaybackPosition;
            playbackSpeed = serializable.lastPlaybackSpeed;

            mix = serializable.mix;

            package = serializable.package;
            syncedName = serializable.syncedName;
            displayName = serializable.animationName; 

            visible = serializable.visible;
            locked = serializable.locked;

            isDirty = serializable.isDirty;

            audioSyncImports = serializable.audioSyncImports;

            baseDataMode = serializable.baseDataMode;
            baseDataSourceLoadedName = serializable.baseDataSource;
            baseDataReferenceTime = serializable.baseDataReferenceTime;  

            loopPoints = serializable.loopPoints;

            rawAnimation = serializable.sessionLocalAnimation.AsOriginalType(packageInfo);
        }

        #endregion

        #region Package Linking

        public bool PushToPackage(SwoleLogger logger = null)
        {
            if (ContentManager.TryFindLocalPackage(package, out ContentManager.LocalPackage pkg)) // Checks for null
            {
                if (string.IsNullOrWhiteSpace(syncedName)) syncedName = DisplayName;

                if (!IsCompiledWithTimeCurve) Compile(); // force recompilation if time curve has been removed

                SwolePackage editPkg = SwolePackage.Create(pkg); 
                if (pkg.Content.TryFind<CustomAnimation>(out var toReplace, syncedName))
                {
                    editPkg.Replace(toReplace, CompiledAnimation);
                }
                else
                {
                    editPkg.Add(CompiledAnimation);
                }

                pkg.Content = editPkg; 
                if (ContentManager.SavePackage(pkg, logger))  
                {
                    syncedName = DisplayName;
                    isDirty = false;
                    return true;
                }
            }

            return false;
        }
        public bool PullFromPackage()
        {
            if (ContentManager.TryFindLocalPackage(package, out ContentManager.LocalPackage pkg)) // Checks for null
            {
                if (string.IsNullOrWhiteSpace(syncedName)) syncedName = DisplayName;

                if (pkg.Content.TryFind<CustomAnimation>(out var newContent, syncedName))
                {
                    rawAnimation = newContent.Duplicate();
                    displayName = rawAnimation.Name;
                    isCompiled = false;
                    isDirty = false;
                    return true;
                }
            }

            return false;
        }
        public bool BindToPackage(string packageName, string packageVersion) => BindToPackage(new PackageIdentifier(packageName, packageVersion), out _);
        public bool BindToPackage(string packageName, string packageVersion, out ContentManager.LocalPackage localPackage) => BindToPackage(new PackageIdentifier(packageName, packageVersion), out localPackage);
        public bool BindToPackage(PackageIdentifier package) => BindToPackage(package, out _);
        public bool BindToPackage(PackageIdentifier package, out ContentManager.LocalPackage localPackage)
        {
            if (!ContentManager.TryFindLocalPackage(package, out localPackage)) return false;

            this.package = package;
            return true;
        }

        #endregion

        #region State

        [Serializable]
        public struct StateData
        {
            public AnimationCurveEditor.State timeCurveState;

            public CustomAnimation.CurveInfoPair[] transformAnimationCurves;
            public CustomAnimation.CurveInfoPair[] propertyAnimationCurves;

            public TransformCurveState[] transformCurveStates;
            public TransformLinearCurveState[] transformLinearCurveStates;

            public PropertyCurveState[] propertyCurveStates;
            public PropertyLinearCurveState[] propertyLinearCurveStates;

            public AnimationEventsState[] animationEventStates;
        }

        [Serializable]
        public struct TransformCurveState
        {
            public TransformCurve.StateData state;

            [NonSerialized]
            public TransformCurve reference;

            public TransformCurve Apply()
            {
                if (reference != null)
                {
                    reference.State = state;
                }

                return reference;
            }
            public static implicit operator TransformCurveState(TransformCurve curve)
            {
                var state = new TransformCurveState();

                if (curve != null)
                {
                    state.state = curve.State;
                    state.reference = curve;
                }

                return state;
            }
        }
        [Serializable]
        public struct TransformLinearCurveState
        {
            public TransformLinearCurve.StateData state;

            [NonSerialized]
            public TransformLinearCurve reference;

            public TransformLinearCurve Apply()
            {
                if (reference != null)
                {
                    reference.State = state;
                }

                return reference;
            }
            public static implicit operator TransformLinearCurveState(TransformLinearCurve curve)
            {
                var state = new TransformLinearCurveState();

                if (curve != null)
                {
                    state.state = curve.State;
                    state.reference = curve;
                }

                return state;
            }
        }

        [Serializable]
        public struct PropertyCurveState
        {
            public PropertyCurve.StateData state;

            [NonSerialized]
            public PropertyCurve reference;

            public PropertyCurve Apply()
            {
                if (reference != null)
                {
                    reference.State = state;
                }

                return reference;
            }
            public static implicit operator PropertyCurveState(PropertyCurve curve)
            {
                var state = new PropertyCurveState();

                if (curve != null)
                {
                    state.state = curve.State;
                    state.reference = curve;
                }

                return state;
            }
        }
        [Serializable]
        public struct PropertyLinearCurveState
        {
            public PropertyLinearCurve.StateData state;

            [NonSerialized]
            public PropertyLinearCurve reference;

            public PropertyLinearCurve Apply()
            {
                if (reference != null)
                {
                    reference.State = state;
                }

                return reference;
            }
            public static implicit operator PropertyLinearCurveState(PropertyLinearCurve curve)
            {
                var state = new PropertyLinearCurveState();

                if (curve != null)
                {
                    state.state = curve.State;
                    state.reference = curve;
                }

                return state;
            }
        }

        [Serializable]
        public struct AnimationEventsState
        {
            public CustomAnimation.Event.Serialized state;

            [NonSerialized]
            public CustomAnimation.Event reference;

            public CustomAnimation.Event Apply()
            {
                if (reference != null)
                {
                    reference.State = state; 
                }

                return reference;
            }
            public static implicit operator AnimationEventsState(CustomAnimation.Event curve)
            {
                var state = new AnimationEventsState();

                if (curve != null)
                {
                    state.state = curve.State;
                    state.reference = curve;
                }

                return state;
            }
        }

        public StateData State
        {
            get 
            {
                var state = new StateData();

                state.timeCurveState = rawAnimation.timeCurve == null ? default : rawAnimation.timeCurve.CloneState();

                state.transformAnimationCurves = rawAnimation.transformAnimationCurves == null ? null : (CustomAnimation.CurveInfoPair[])rawAnimation.transformAnimationCurves.Clone();
                state.propertyAnimationCurves = rawAnimation.propertyAnimationCurves == null ? null : (CustomAnimation.CurveInfoPair[])rawAnimation.propertyAnimationCurves.Clone(); 

                if (rawAnimation.transformCurves != null && rawAnimation.transformCurves.Length > 0) 
                { 
                    state.transformCurveStates = new TransformCurveState[rawAnimation.transformCurves.Length];
                    for(int a = 0; a < rawAnimation.transformCurves.Length; a++)
                    {
                        var curve = rawAnimation.transformCurves[a];
                        if (curve == null) continue;

                        state.transformCurveStates[a] = curve; 
                    }
                }
                if (rawAnimation.transformLinearCurves != null && rawAnimation.transformLinearCurves.Length > 0)
                {
                    state.transformLinearCurveStates = new TransformLinearCurveState[rawAnimation.transformLinearCurves.Length];
                    for (int a = 0; a < rawAnimation.transformLinearCurves.Length; a++)
                    {
                        var curve = rawAnimation.transformLinearCurves[a];
                        if (curve == null) continue;

                        state.transformLinearCurveStates[a] = curve;
                    }
                }

                if (rawAnimation.propertyCurves != null && rawAnimation.propertyCurves.Length > 0)
                {
                    state.propertyCurveStates = new PropertyCurveState[rawAnimation.propertyCurves.Length];
                    for (int a = 0; a < rawAnimation.propertyCurves.Length; a++)
                    {
                        var curve = rawAnimation.propertyCurves[a];
                        if (curve == null) continue;

                        state.propertyCurveStates[a] = curve;
                    }
                }
                if (rawAnimation.propertyLinearCurves != null && rawAnimation.propertyLinearCurves.Length > 0)
                {
                    state.propertyLinearCurveStates = new PropertyLinearCurveState[rawAnimation.propertyLinearCurves.Length];
                    for (int a = 0; a < rawAnimation.propertyLinearCurves.Length; a++)
                    {
                        var curve = rawAnimation.propertyLinearCurves[a];
                        if (curve == null) continue;

                        state.propertyLinearCurveStates[a] = curve;
                    }
                }

                if (rawAnimation.events != null && rawAnimation.events.Length > 0)
                {
                    state.animationEventStates = new AnimationEventsState[rawAnimation.events.Length];
                    for (int a = 0; a < rawAnimation.events.Length; a++)
                    {
                        var event_ = rawAnimation.events[a];
                        if (event_ == null) continue;

                        state.animationEventStates[a] = event_;
                    }
                }

                return state;
            }

            set 
            {

                if (value.timeCurveState.keyframes != null && value.timeCurveState.keyframes.Length > 0)
                {
                    if (rawAnimation.timeCurve == null) rawAnimation.timeCurve = new EditableAnimationCurve();
                    rawAnimation.timeCurve.State = value.timeCurveState; 
                }

                rawAnimation.transformAnimationCurves = value.transformAnimationCurves == null ? null : (CustomAnimation.CurveInfoPair[])value.transformAnimationCurves.Clone();
                rawAnimation.propertyAnimationCurves = value.propertyAnimationCurves == null ? null : (CustomAnimation.CurveInfoPair[])value.propertyAnimationCurves.Clone();

                if (value.transformCurveStates == null)
                {
                    rawAnimation.transformCurves = null;
                } 
                else
                {
                    rawAnimation.transformCurves = new TransformCurve[ value.transformCurveStates.Length];
                    for (int a = 0; a < value.transformCurveStates.Length; a++) rawAnimation.transformCurves[a] = value.transformCurveStates[a].Apply();
                }

                if (value.transformLinearCurveStates == null)
                {
                    rawAnimation.transformLinearCurves = null;
                }
                else
                {
                    rawAnimation.transformLinearCurves = new TransformLinearCurve[value.transformLinearCurveStates.Length];
                    for (int a = 0; a < value.transformLinearCurveStates.Length; a++) rawAnimation.transformLinearCurves[a] = value.transformLinearCurveStates[a].Apply();
                }

                if (value.propertyCurveStates == null)
                {
                    rawAnimation.propertyCurves = null;
                }
                else
                {
                    rawAnimation.propertyCurves = new PropertyCurve[value.propertyCurveStates.Length];
                    for (int a = 0; a < value.propertyCurveStates.Length; a++) rawAnimation.propertyCurves[a] = value.propertyCurveStates[a].Apply();
                }

                if (value.propertyLinearCurveStates == null)
                {
                    rawAnimation.propertyLinearCurves = null; 
                }
                else
                {
                    rawAnimation.propertyLinearCurves = new PropertyLinearCurve[value.propertyLinearCurveStates.Length];
                    for (int a = 0; a < value.propertyLinearCurveStates.Length; a++) rawAnimation.propertyLinearCurves[a] = value.propertyLinearCurveStates[a].Apply();
                }

                if (value.animationEventStates == null)
                {
                    rawAnimation.events = null;
                }
                else if (rawAnimation.events != null)
                {
                    rawAnimation.events = new CustomAnimation.Event[value.animationEventStates.Length];
                    for (int a = 0; a < value.animationEventStates.Length; a++) rawAnimation.events[a] = value.animationEventStates[a].Apply();  
                }
            }
        }

        #endregion

        public AnimationSource() : base(default) { }

        /// <summary>
        /// This source's index in the owning animatable's animation bank.
        /// </summary>
        [NonSerialized]
        public int index;

        /// <summary>
        /// An animator that could be currently playing the animation. Used to make sure animation jobs are done before flushing data.
        /// </summary>
        public CustomAnimator previewAnimator;

        /// <summary>
        /// A layer in the owning animatable's animator. It is used to control the preview playback of this animation source.
        /// </summary>
        [NonSerialized]
        public CustomAnimationLayer previewLayer;
        /// <summary>
        /// The state machine that controls playback in the preview layer.
        /// </summary>
        public CustomAnimationLayerState PreviewState
        {
            get
            {
                if (previewLayer == null) return null;
                return previewLayer.ActiveStateTyped; 
            }
        }

        protected float mix;
        public float Mix
        {
            get => GetMix();
            set => SetMix(null, value);
        }
        public struct UndoableSetAnimationSourceMix : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public AnimationSource source;
            public float prevMix, newMix;

            public UndoableSetAnimationSourceMix(AnimationEditor editor, AnimationSource source, float prevMix, float newMix)
            {
                this.editor = editor;
                this.source = source;
                this.prevMix = prevMix;
                this.newMix = newMix;

                undoState = false;
            }

            public void Reapply()
            {
                if (source == null) return;
                source.SetMix(editor, newMix, false, true, false);
            }

            public void Revert()
            {
                if (source == null) return;
                source.SetMix(editor, prevMix, false, true, false);
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
        public float GetMix()
        {
            if (listMember != null && listMember.rectTransform != null)
            {
                var slider = listMember.rectTransform.GetComponentInChildren<Slider>();
                if (slider != null) mix = slider.value;              
            }

            return mix;
        }
        public float GetMixRaw() => mix;
        public void SetMix(AnimationEditor editor, float value, bool notifyListeners = true, bool updateSlider = true, bool undoable = true)
        {
            float prevMix = mix;

            mix = value;
            if (listMember != null && listMember.rectTransform != null)
            {
                if (updateSlider)
                {
                    var slider = listMember.rectTransform.GetComponentInChildren<Slider>();
                    if (slider != null)
                    {
                        if (notifyListeners) slider.value = mix; else slider.SetValueWithoutNotify(mix);
                    }
                }
            } 

            if (previewLayer != null) previewLayer.mix = mix;

            if (undoable && editor != null) editor.RecordRevertibleAction(new UndoableSetAnimationSourceMix(editor, this, prevMix, mix)); 
        }

        /// <summary>
        /// The length to set the animation editor's timeline to.
        /// </summary>
        public float timelineLength;

        /// <summary>
        /// The current preview position in the animation editor timeline.
        /// </summary>
        protected float playbackPosition;
        /// <summary>
        /// The current preview position in the animation editor timeline.
        /// </summary>
        public float PlaybackPosition
        {
            get => playbackPosition;
            set
            {
                if (locked) return;
                playbackPosition = value;  
            }
        }
        public float playbackSpeed = 1;

        /// <summary>
        /// The bound package where the compiled animation will be exported to.
        /// </summary>
        public PackageIdentifier package;
        public string syncedName;
        [SerializeField]
        protected string displayName;
        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value;
                listMember?.SetName(value); 

                if (rawAnimation != null) 
                {
                    var info = rawAnimation.ContentInfo;
                    info.name = displayName;
                    rawAnimation = (CustomAnimation)rawAnimation.CreateShallowCopyAndReplaceContentInfo(info);
                }
                if (compiledAnimation != null)
                {
                    var info = compiledAnimation.ContentInfo;
                    info.name = displayName;
                    compiledAnimation = (CustomAnimation)compiledAnimation.CreateShallowCopyAndReplaceContentInfo(info);
                }

                MarkAsDirty();
            }
        }
        public override string SerializedName => DisplayName;
        public int SampleRate
        {
            get => rawAnimation == null ? CustomAnimation.DefaultJobCurveSampleRate : rawAnimation.jobCurveSampleRate;
            set
            {
                if (rawAnimation == null) return;

                if (rawAnimation != null) rawAnimation.jobCurveSampleRate = value;
                if (compiledAnimation != null) compiledAnimation.jobCurveSampleRate = value;
                
                MarkAsDirty();
            }
        }

        protected bool visible;
        public bool Visible => visible;

        protected bool locked;
        public bool IsLocked => locked;

        public VoidParameterlessDelegate refreshVisibilityButtons;

        public struct UndoableSetVisibilityOfSource : IRevertableAction
        {

            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public int animatableIndex, sourceIndex;
            public bool prevVisible, visible;
            public bool prevLocked, locked;

            public UndoableSetVisibilityOfSource(AnimationEditor editor, int animatableIndex, int sourceIndex, bool prevVisible, bool prevLocked, bool visible, bool locked)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;
                this.sourceIndex = sourceIndex;

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
                    obj.SetVisibilityOfSource(editor, sourceIndex, visible, locked, false);
                }
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null)
                {
                    obj.SetVisibilityOfSource(editor, sourceIndex, prevVisible, prevLocked, false);
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
        public void SetVisibility(AnimationEditor editor, ImportedAnimatable animatable, bool visible, bool locked, bool undoable)
        {
            if (editor != null && editor.IsPlayingPreview) editor.Pause(undoable); 

            bool prevVisible = this.visible;
            bool prevLocked = this.locked;

            this.visible = visible;
            this.locked = locked;
            if (previewLayer != null) previewLayer.SetActive(visible);

            if (undoable && (prevVisible != visible || prevLocked != locked)) editor.RecordRevertibleAction(new UndoableSetVisibilityOfSource(editor, animatable.index, index, prevVisible, prevLocked, visible, locked));

            refreshVisibilityButtons?.Invoke();
        }

        [SerializeField]
        private bool isDirty;
        /// <summary>
        /// Has the source data changed since it was last saved to a package?
        /// </summary>
        public bool IsDirty => isDirty;

        public event VoidParameterlessDelegate OnBecomeDirty;
        public event VoidParameterlessDelegate OnMarkDirty;
        public bool MarkAsDirty()
        {
            bool wasDirty = isDirty;

            isDirty = true;
            MarkForRecompilation(); 

            if (!wasDirty) OnBecomeDirty?.Invoke();
            OnMarkDirty?.Invoke();
            return wasDirty;
        }

        [NonSerialized]
        public UICategorizedList.Member listMember;
        public void RefreshListMemberPosition()
        {
            if (listMember == null) return;
            listMember.Index = index;
        }

        protected const string _packageLinkButtonTag = "link";
        protected const string _deleteButtonTag = "Delete";
        public void CreateListMember(AnimationEditor editor, ImportedAnimatable animatable, bool force = false)
        {
            if (editor == null || editor.importedAnimatablesList == null || animatable == null || animatable.listCategory == null || (listMember != null && !force)) return;

            var session = editor.CurrentSession; 
            listMember = editor.importedAnimatablesList.AddNewListMember(displayName, animatable.listCategory, null);

            if (listMember != null)
            {
                if (listMember.buttonRT != null)
                {
                    var pointerProxy = listMember.buttonRT.gameObject.AddOrGetComponent<PointerEventsProxy>();

                    if (pointerProxy.OnLeftClick == null) pointerProxy.OnLeftClick = new UnityEvent(); else pointerProxy.OnLeftClick.RemoveAllListeners();
                    pointerProxy.OnLeftClick.AddListener(() =>
                    {
                        if (session == null || editor.CurrentSession != session) return;
                        if (animatable != null) animatable.SetCurrentlyEditedSource(editor, index, true);
                        session.SetActiveObject(editor, animatable.index, true, false);
                        editor.RefreshAnimatableListUI();
                    });

                    if (pointerProxy.OnRightClick == null) pointerProxy.OnRightClick = new UnityEvent(); else pointerProxy.OnRightClick.RemoveAllListeners();
                    pointerProxy.OnRightClick.AddListener(() =>
                    {
                        if (session == null || editor.CurrentSession != session) return;
                        if (animatable != null) animatable.SetCurrentlyEditedSource(editor, index, true);
                        session.SetActiveObject(editor, animatable.index, true, false);
                        editor.RefreshAnimatableListUI();

                        // Open context menu when source list object is right clicked
                        if (editor.contextMenuMain != null)
                        {
                            editor.OpenContextMenuMain();

                            var moveUp = editor.contextMenuMain.FindDeepChildLiberal(AnimationEditor._moveUpContextMenuOptionName);
                            if (moveUp != null)
                            {
                                CustomEditorUtils.SetButtonOnClickAction(moveUp, () =>
                                {
                                    if (animatable != null && animatable.animationBank != null)
                                    {
                                        animatable.RefreshBankIndices(0);
                                        if (index > 0 && index < animatable.animationBank.Count && ReferenceEquals(this, animatable.animationBank[index]))
                                        {
                                            animatable.RepositionSource(editor, this, index - 1); 
                                        }
                                    }
                                });
                                moveUp.gameObject.SetActive(index > 0);
                            }
                            var moveDown = editor.contextMenuMain.FindDeepChildLiberal(AnimationEditor._moveDownContextMenuOptionName); 
                            if (moveDown != null)
                            {
                                CustomEditorUtils.SetButtonOnClickAction(moveDown, () =>
                                {
                                    if (animatable != null && animatable.animationBank != null)
                                    {
                                        animatable.RefreshBankIndices(0);
                                        if (index >= 0 && index < animatable.animationBank.Count - 1 && ReferenceEquals(this, animatable.animationBank[index]))
                                        {
                                            animatable.RepositionSource(editor, this, index + 1); 
                                        }
                                    }
                                });
                                moveDown.gameObject.SetActive(index < animatable.animationBank.Count - 1); 
                            }

                            var edit = editor.contextMenuMain.FindDeepChildLiberal(AnimationEditor._editSettingsContextMenuOptionName);
                            if (edit != null)
                            {
                                edit.gameObject.SetActive(true);
                                CustomEditorUtils.SetButtonOnClickAction(edit, () =>
                                {
                                    editor.OpenAnimationSettingsWindow();
                                });
                            }

                        }

                    });
                }

                if (listMember.rectTransform != null)
                {
                    var rt_visOn = listMember.rectTransform.FindDeepChildLiberal(AnimationEditor._visibilityOnTag);
                    var rt_locked = listMember.rectTransform.FindDeepChildLiberal(AnimationEditor._lockedTag);
                    var rt_visOff = listMember.rectTransform.FindDeepChildLiberal(AnimationEditor._visibilityOffTag);

                    void RefreshVisibilityButtons()
                    {
                        if (rt_visOn != null) rt_visOn.gameObject.SetActive(Visible && !locked);
                        if (rt_locked != null) rt_locked.gameObject.SetActive(locked);
                        if (rt_visOff != null) rt_visOff.gameObject.SetActive(!Visible && !locked);
                    }

                    refreshVisibilityButtons = RefreshVisibilityButtons;
                    CustomEditorUtils.SetButtonOnClickAction(rt_visOn, () => 
                    {
                        SetVisibility(editor, animatable, true, true, true);
                    }, true, true, false);
                    CustomEditorUtils.SetButtonOnClickAction(rt_locked, () => 
                    { 
                        SetVisibility(editor, animatable, false, false, true);
                    }, true, true, false);
                    CustomEditorUtils.SetButtonOnClickAction(rt_visOff, () => 
                    {
                        SetVisibility(editor, animatable, true, false, true);
                    }, true, true, false);
                    RefreshVisibilityButtons();  

                    var slider = listMember.rectTransform.GetComponentInChildren<Slider>();
                    if (slider != null)
                    {
                        slider.minValue = 0;
                        slider.maxValue = 1;
                        if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                        slider.onValueChanged.RemoveAllListeners();
                        slider.SetValueWithoutNotify(GetMixRaw());
                        LTDescr setMixDelayed = null;
                        float prevMix = 0;
                        slider.onValueChanged.AddListener((float value) =>
                        {
                            if (setMixDelayed != null /*&& LeanTween.isTweening(setMixDelayed.uniqueId)*/) 
                            {
                                LeanTween.cancel(setMixDelayed.uniqueId);
                                setMixDelayed = null;
                            } 
                            else
                            {
                                prevMix = mix;
                            }

                            SetMix(editor, value, false, false, false);
                            setMixDelayed = LeanTween.delayedCall(0.2f, () =>
                            {
                                editor.RecordRevertibleAction(new UndoableSetAnimationSourceMix(editor, this, prevMix, mix)); 
                                editor.MarkSessionDirty();
                                setMixDelayed = null;
                                prevMix = 0;
                            });
                        });
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(listMember.rectTransform, _packageLinkButtonTag, () => editor.OpenPackageLinkWindow(this));

                    void AskToDeleteSource()
                    {
                        void Delete() => animatable.RemoveAnimationSource(editor, this, true);
                        editor.ShowPopupConfirmation($"Are you sure you want to delete '{DisplayName}'?", Delete);
                    }

                    CustomEditorUtils.SetButtonOnClickActionByName(listMember.rectTransform, _deleteButtonTag, AskToDeleteSource);

                }
            }
        }

        [NonSerialized]
        private CustomAnimationAsset previewAsset;
        public CustomAnimationAsset PreviewAsset
        {
            get
            {
                if (previewAsset == null) 
                { 
                    previewAsset = ScriptableObject.CreateInstance<CustomAnimationAsset>();
                    previewAsset.name = $"previewAsset:{previewAsset.GetInstanceID()}"; 
                }
                return previewAsset;
            }
        }

        [NonSerialized]
        public CustomAnimation rawAnimation;
        public CustomAnimation GetOrCreateRawData()
        {
            if (rawAnimation == null) 
            { 
                rawAnimation = new CustomAnimation(displayName, string.Empty, DateTime.Now, DateTime.Now, string.Empty, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null);
                MarkAsDirty();
            }
            return rawAnimation;
        }
        [NonSerialized]
        private CustomAnimation compiledAnimation;
        public CustomAnimation CompiledAnimation
        {
            get
            {
                if (!IsCompiled) Compile();
                return compiledAnimation;
            }
        }
        [NonSerialized]
        private bool isCompiled;
        public void MarkForRecompilation()
        {
            isCompiled = false;
        }
        /// <summary>
        /// Have the latest changes to the source data been compiled?
        /// </summary>
        public bool IsCompiled => isCompiled && compiledAnimation != null;
        public bool HasTimeCurve => rawAnimation != null && rawAnimation.timeCurve != null;
        public void RemoveCompiledTimeCurve()
        {
            if (compiledAnimation == null || compiledAnimation.timeCurve == null) return;
            compiledAnimation.timeCurve = null;
            compiledAnimation.FlushJobData();
        }
        public void RestoreCompiledTimeCurve()
        {
            if (compiledAnimation == null || compiledAnimation.timeCurve != null || rawAnimation == null || (rawAnimation.timeCurve == null && compiledAnimation.timeCurve == null)) return; 
            compiledAnimation.timeCurve = rawAnimation.timeCurve;  
            compiledAnimation.FlushJobData();  
        }
        public bool IsCompiledWithTimeCurve => rawAnimation != null && compiledAnimation != null && ((rawAnimation.timeCurve == null && compiledAnimation.timeCurve == null) || compiledAnimation.timeCurve != null);
        public bool IsCompiledWithoutTimeCurve => compiledAnimation != null && compiledAnimation.timeCurve == null;

        [Serializable]
        public class AudioSyncImport
        {

            public PersistentAudioPlayer.AudioSourceClaim sourceClaim;

            public string path;
            [NonSerialized]
            private string prevPath;

            public float playTime;

            public string mixerPath;
            [NonSerialized]
            private string prevMixerPath;

            [NonSerialized]
            private AudioClip clip; 
            public AudioClip Clip
            {
                get
                {
                    if (clip == null || prevPath != path)
                    {
                        prevPath = path;
                        clip = null;

                        var asset = swole.Engine.FindAsset<IAudioAsset>(path, swole.DefaultHost);
                        if (asset != null)
                        {
                            if (asset is IAudioClipProxy proxy) clip = proxy.Clip; else if (asset.Asset is AudioClip clip_) clip = clip_;
                        }
                    }

                    return clip;
                }
            }

            [NonSerialized]
            private AudioMixerGroup mixerGroup;
            public AudioMixerGroup MixerGroup
            {
                get
                {
                    if (mixerGroup == null || prevMixerPath != mixerPath)
                    {
                        prevMixerPath = mixerPath;
                        mixerGroup = null;

                        var mainMixer = ResourceLib.DefaultAudioMixer; 

                        if (mainMixer != null && mainMixer.Mixer != null)
                        {

                            if (string.IsNullOrEmpty(mixerPath))
                            {
                                mixerGroup = mainMixer.Mixer.outputAudioMixerGroup; 
                            }
                            else
                            {
                                var matches = mainMixer.Mixer.FindMatchingGroups(mixerPath);
                                if (matches.Length > 0) mixerGroup = matches[0];
                            }
                        }
                    }

                    return mixerGroup;
                }
            }
        }
        protected List<AudioSyncImport> audioSyncImports = new List<AudioSyncImport>();
        public int AudioSyncImportsCount => audioSyncImports == null ? 0 : audioSyncImports.Count;
        public AudioSyncImport GetAudioSyncImport(int index) => audioSyncImports == null || index < 0 || index >= audioSyncImports.Count ? null : audioSyncImports[index];
        public void AddAudioSyncImport(AudioSyncImport audioSyncImport)
        {
            if (audioSyncImports == null) audioSyncImports = new List<AudioSyncImport>(); 

            audioSyncImports.Add(audioSyncImport);
            MarkAsDirty();
        }
        public bool RemoveAudioSyncImport(AudioSyncImport audioSyncImport)
        {
            if (audioSyncImports == null) return false;

            if (audioSyncImports.RemoveAll(i => ReferenceEquals(audioSyncImport, i)) > 0)
            {
                MarkAsDirty();
                return true;
            }

            return false;
        }

        [Serializable]
        public enum BaseDataType
        {
            Default, AnimationSource, AnimationSourceSnapshot
        }

        protected BaseDataType baseDataMode;
        public BaseDataType BaseDataMode
        {
            get => baseDataMode;
            set
            {
                if (value != baseDataMode)
                {
                    baseDataMode = value;
                    MarkAsDirty();
                }
            }
        }
        [NonSerialized]
        protected AnimationSource baseDataSource;
        protected string baseDataSourceLoadedName;
        public AnimationSource BaseDataSource
        {
            get 
            {
                if (baseDataSource != null && ReferenceEquals(this, baseDataSource.baseDataSource))
                {
                    swole.LogWarning($"{nameof(BaseDataSource)} for '{DisplayName}' was '{baseDataSource.DisplayName}', but '{DisplayName}' is the {nameof(BaseDataSource)} for '{baseDataSource.DisplayName}' already and would create a reference loop during compilation; so the reference was removed.");
                    baseDataSource = null;
                    MarkAsDirty();
                }

                return baseDataSource;
            }
            set
            {
                if (value != null && ReferenceEquals(this, value.baseDataSource))
                {
                    swole.LogWarning($"Tried to set {nameof(BaseDataSource)} for '{DisplayName}' to '{value.DisplayName}', but '{DisplayName}' is the {nameof(BaseDataSource)} for '{value.DisplayName}' and would create a reference loop during compilation!");
                    value = null;
                }
                else if (ReferenceEquals(this, value))
                {
                    swole.LogWarning($"Tried to set {nameof(BaseDataSource)} for '{DisplayName}' to itself!");      
                    value = null;
                }

                baseDataSource = value;
                if (baseDataSource != null) baseDataSourceLoadedName = baseDataSource.displayName;
                MarkAsDirty(); 
            }
        } 
        public void ApplyBaseDataSourceFromLoadedName(ICollection<AnimationSource> sources)
        {
            if (string.IsNullOrEmpty(baseDataSourceLoadedName) || sources == null) return;

            foreach(var source in sources)
            {
                if (source == null) continue;
                if (source.DisplayName == baseDataSourceLoadedName)
                {
                    baseDataSource = source;
                    return;
                }
            }

            var temp = baseDataSourceLoadedName.AsID();
            foreach (var source in sources)
            {
                if (source == null) continue;
                if (source.DisplayName.AsID() == temp)
                {
                    baseDataSource = source;
                    return;
                }
            }
        }
        protected float baseDataReferenceTime; 
        public float BaseDataReferenceTime
        {
            get => baseDataReferenceTime;
            set
            {
                if (baseDataReferenceTime != value)
                {
                    baseDataReferenceTime = value;
                    MarkAsDirty();
                }
            }
        }

        public List<AnimationUtils.LoopPoint> loopPoints;

        public List<AnimationUtils.LoopPoint> GetLoopPoints(string id, List<AnimationUtils.LoopPoint> outputList = null)
        {
            if (outputList == null) outputList = new List<AnimationUtils.LoopPoint>();
            if (loopPoints == null) return outputList;

            id = id.AsID();

            for (int a = 0; a < loopPoints.Count; a++)
            {
                var lp = loopPoints[a];
                if (lp == null) continue;
                if (lp.id.AsID() == id) outputList.Add(lp);
            }

            outputList.Sort((x, y) => (int)Mathf.Sign(x.position - y.position));

            return outputList;
        }

        private readonly List<AnimationUtils.LoopPoint> tempLPs = new List<AnimationUtils.LoopPoint>();
        private readonly List<TransformLinearCurve> transformLinearCurves = new List<TransformLinearCurve>();
        private readonly List<TransformCurve> transformCurves = new List<TransformCurve>();
        private readonly List<PropertyLinearCurve> propertyLinearCurves = new List<PropertyLinearCurve>();
        private readonly List<PropertyCurve> propertyCurves = new List<PropertyCurve>();
        private readonly List<CustomAnimation.CurveInfoPair> transformCurveInfo = new List<CustomAnimation.CurveInfoPair>();
        private readonly List<CustomAnimation.CurveInfoPair> propertyCurveInfo = new List<CustomAnimation.CurveInfoPair>();
        private readonly List<AnimationCurveEditor.KeyframeStateRaw> tempKeyframes = new List<AnimationCurveEditor.KeyframeStateRaw>();
        private readonly List<ITransformCurve.Frame> tempTransformFrames = new List<ITransformCurve.Frame>();
        private readonly List<IPropertyCurve.Frame> tempPropertyFrames = new List<IPropertyCurve.Frame>();
        public CustomAnimation Compile(bool removeEmptyCurves=true)
        {
            if (rawAnimation != null)
            {
                isCompiled = false;

                bool isNew = false;
                if (compiledAnimation == null)
                {
                    isNew = true;
                    compiledAnimation = new CustomAnimation(rawAnimation.ContentInfo, rawAnimation.framesPerSecond, rawAnimation.jobCurveSampleRate, rawAnimation.timeCurve, null, null, null, null, rawAnimation.transformAnimationCurves, rawAnimation.propertyAnimationCurves, rawAnimation.events, rawAnimation.PackageInfo);
                }
                 
                compiledAnimation.framesPerSecond = rawAnimation.framesPerSecond;
                compiledAnimation.jobCurveSampleRate = rawAnimation.jobCurveSampleRate;
                compiledAnimation.timeCurve = rawAnimation.timeCurve;
                compiledAnimation.transformAnimationCurves = rawAnimation.transformAnimationCurves;
                compiledAnimation.propertyAnimationCurves = rawAnimation.propertyAnimationCurves;
                compiledAnimation.events = rawAnimation.events; 

                //TransformLinearCurve[] transformLinearCurves = rawAnimation.transformLinearCurves == null ? new TransformLinearCurve[0] : (TransformLinearCurve[])rawAnimation.transformLinearCurves.Clone();
                //TransformCurve[] transformCurves = rawAnimation.transformCurves == null ? new TransformCurve[0] : (TransformCurve[])rawAnimation.transformCurves.Clone();
                //PropertyLinearCurve[] propertyLinearCurves = rawAnimation.propertyLinearCurves == null ? new PropertyLinearCurve[0] : (PropertyLinearCurve[])rawAnimation.propertyLinearCurves.Clone();
                //PropertyCurve[] propertyCurves = rawAnimation.propertyCurves == null ? new PropertyCurve[0] : (PropertyCurve[])rawAnimation.propertyCurves.Clone();

                transformLinearCurves.Clear();
                if (rawAnimation.transformLinearCurves != null) transformLinearCurves.AddRange(rawAnimation.transformLinearCurves);
                transformCurves.Clear();
                if (rawAnimation.transformCurves != null) transformCurves.AddRange(rawAnimation.transformCurves);
                propertyLinearCurves.Clear();
                if (rawAnimation.propertyLinearCurves != null) propertyLinearCurves.AddRange(rawAnimation.propertyLinearCurves);
                propertyCurves.Clear();
                if (rawAnimation.propertyCurves != null) propertyCurves.AddRange(rawAnimation.propertyCurves); 

                transformCurveInfo.Clear();
                if (rawAnimation.transformAnimationCurves != null) transformCurveInfo.AddRange(rawAnimation.transformAnimationCurves);

                propertyCurveInfo.Clear();
                if (rawAnimation.propertyAnimationCurves != null) propertyCurveInfo.AddRange(rawAnimation.propertyAnimationCurves);
                 
                #region Replace Base Data

                CustomAnimation compiledBase;
                switch (BaseDataMode)
                {
                    default:
                        break;

                    case BaseDataType.AnimationSource:
                        if (baseDataSource != null)
                        {
                            compiledBase = baseDataSource.CompiledAnimation;                            
                            if (compiledBase != null && compiledBase.transformAnimationCurves != null)
                            {
                                for (int a = 0; a < transformCurveInfo.Count; a++)
                                {
                                    var info = transformCurveInfo[a];
                                    if (info.infoBase.curveIndex < 0) continue;

                                    ITransformCurve currentBaseCurve = info.infoBase.isLinear ? transformLinearCurves[info.infoBase.curveIndex] : transformCurves[info.infoBase.curveIndex];
                                    if (currentBaseCurve == null) continue;

                                    foreach (var baseInfo in compiledBase.transformAnimationCurves)
                                    {
                                        if (baseInfo.infoMain.curveIndex < 0) continue;

                                        // TODO: Add some kind of internal animation curve storage to animations, where external time curves can be stored and referenced by TransformCurves and the like; then base curves can use their original animation's time curve properly without requiring resampling of the base curve.
                                        // For now, base curves do not reference their original time curves at all, so any dependent additive animations will not sync correctly with the parent animation if that parent uses a time curve.
                                        ITransformCurve newBaseCurve = baseInfo.infoMain.isLinear ? compiledBase.transformLinearCurves[baseInfo.infoMain.curveIndex] : compiledBase.transformCurves[baseInfo.infoMain.curveIndex];
                                        if (newBaseCurve == null || currentBaseCurve.TransformName.AsID() != newBaseCurve.TransformName.AsID()) continue;

                                        newBaseCurve = (ITransformCurve)newBaseCurve.Clone();
                                        if (newBaseCurve is TransformCurve tc)
                                        {
                                            if (tc.HasKeyframes)
                                            {
                                                tc = tc.Duplicate().RebuildCurveFromReference(currentBaseCurve, rawAnimation.framesPerSecond, true); 
                                                if (info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = false;
                                                    info_.curveIndex = transformCurves.Count;
                                                    info.infoBase = info_;

                                                    transformCurves.Add(tc);
                                                }
                                                else
                                                {
                                                    transformCurves[info.infoBase.curveIndex] = tc;
                                                }
                                            }
                                        }
                                        else if (newBaseCurve is TransformLinearCurve tlc)
                                        {
                                            if (tlc.frames != null || tlc.frames.Length > 0)
                                            {
                                                if (!info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = true;
                                                    info_.curveIndex = transformLinearCurves.Count;
                                                    info.infoBase = info_;

                                                    transformLinearCurves.Add(tlc);
                                                }
                                                else
                                                {
                                                    transformLinearCurves[info.infoBase.curveIndex] = tlc;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                    transformCurveInfo[a] = info;
                                }

                                for (int a = 0; a < propertyCurveInfo.Count; a++)
                                {
                                    var info = propertyCurveInfo[a];
                                    if (info.infoBase.curveIndex < 0) continue;

                                    IPropertyCurve currentBaseCurve = info.infoBase.isLinear ? propertyLinearCurves[info.infoBase.curveIndex] : propertyCurves[info.infoBase.curveIndex];
                                    if (currentBaseCurve == null) continue;

                                    foreach (var baseInfo in compiledBase.propertyAnimationCurves)
                                    {
                                        if (baseInfo.infoMain.curveIndex < 0) continue;

                                        IPropertyCurve newBaseCurve = baseInfo.infoMain.isLinear ? compiledBase.propertyLinearCurves[baseInfo.infoMain.curveIndex] : compiledBase.propertyCurves[baseInfo.infoMain.curveIndex];
                                        if (newBaseCurve == null || currentBaseCurve.PropertyString != newBaseCurve.PropertyString) continue;

                                        newBaseCurve = (IPropertyCurve)newBaseCurve.Clone();
                                        if (newBaseCurve is PropertyCurve pc)
                                        {
                                            if (pc.propertyValueCurve != null && pc.propertyValueCurve.length > 0)
                                            {
                                                if (info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = false;
                                                    info_.curveIndex = propertyCurves.Count;  
                                                    info.infoBase = info_;

                                                    propertyCurves.Add(pc);
                                                }
                                                else
                                                {
                                                    propertyCurves[info.infoBase.curveIndex] = pc;
                                                }
                                            }
                                        }
                                        else if (newBaseCurve is PropertyLinearCurve plc)
                                        {
                                            if (plc.frames != null && plc.frames.Length > 0)
                                            {
                                                if (!info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = true;
                                                    info_.curveIndex = propertyLinearCurves.Count;
                                                    info.infoBase = info_;

                                                    propertyLinearCurves.Add(plc);
                                                }
                                                else
                                                {
                                                    propertyLinearCurves[info.infoBase.curveIndex] = plc;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                    propertyCurveInfo[a] = info;
                                }
                            }
                            else
                            {
                                swole.LogWarning($"Failed to compile base source '{baseDataSource.DisplayName}' for animation '{DisplayName}'");
                            }
                        }
                        break;

                    case BaseDataType.AnimationSourceSnapshot:
                        if (baseDataSource != null)
                        {
                            compiledBase = baseDataSource.CompiledAnimation;
                            if (compiledBase != null && compiledBase.transformAnimationCurves != null)
                            {
                                for (int a = 0; a < transformCurveInfo.Count; a++)
                                {
                                    var info = transformCurveInfo[a];
                                    if (info.infoBase.curveIndex < 0) continue;

                                    ITransformCurve currentBaseCurve = info.infoBase.isLinear ? transformLinearCurves[info.infoBase.curveIndex] : transformCurves[info.infoBase.curveIndex];
                                    if (currentBaseCurve == null) continue;
                                    var origBaseLength = currentBaseCurve.GetLengthInSeconds(rawAnimation.framesPerSecond);

                                    foreach (var baseInfo in compiledBase.transformAnimationCurves)
                                    {
                                        if (baseInfo.infoMain.curveIndex < 0) continue;
                                        
                                        ITransformCurve newBaseCurve = baseInfo.infoMain.isLinear ? compiledBase.transformLinearCurves[baseInfo.infoMain.curveIndex] : compiledBase.transformCurves[baseInfo.infoMain.curveIndex];
                                        if (newBaseCurve == null || currentBaseCurve.TransformName.AsID() != newBaseCurve.TransformName.AsID()) continue;

                                        var baseLength = newBaseCurve.GetLengthInSeconds(compiledAnimation.framesPerSecond);
                                        if (newBaseCurve is TransformCurve tc)
                                        {
                                            var newCurve = TransformCurve.NewInstance;
                                            newCurve.name = newBaseCurve.TransformName;

                                            float newT = baseLength > 0 ? (BaseDataReferenceTime / baseLength) : 0;
                                            float origT = origBaseLength > 0 ? (BaseDataReferenceTime / origBaseLength) : 0;
                                            if (compiledAnimation.timeCurve != null && compiledAnimation.timeCurve.length > 0) newT = compiledAnimation.timeCurve.Evaluate(newT);

                                            var snapshot = tc.Evaluate(newT);
                                            var origSnapshot = currentBaseCurve.Evaluate(origT);

                                            newCurve.localPositionCurveX.AddKey(0, tc.localPositionCurveX == null || tc.localPositionCurveX.length <= 0 ? origSnapshot.localPosition.x : snapshot.localPosition.x);
                                            newCurve.localPositionCurveY.AddKey(0, tc.localPositionCurveY == null || tc.localPositionCurveY.length <= 0 ? origSnapshot.localPosition.y : snapshot.localPosition.y);
                                            newCurve.localPositionCurveZ.AddKey(0, tc.localPositionCurveZ == null || tc.localPositionCurveZ.length <= 0 ? origSnapshot.localPosition.z : snapshot.localPosition.z);

                                            newCurve.localRotationCurveX.AddKey(0, tc.localRotationCurveX == null || tc.localRotationCurveX.length <= 0 ? origSnapshot.localRotation.value.x : snapshot.localRotation.value.x);
                                            newCurve.localRotationCurveY.AddKey(0, tc.localRotationCurveY == null || tc.localRotationCurveY.length <= 0 ? origSnapshot.localRotation.value.y : snapshot.localRotation.value.y);
                                            newCurve.localRotationCurveZ.AddKey(0, tc.localRotationCurveZ == null || tc.localRotationCurveZ.length <= 0 ? origSnapshot.localRotation.value.z : snapshot.localRotation.value.z);
                                            newCurve.localRotationCurveW.AddKey(0, tc.localRotationCurveW == null || tc.localRotationCurveW.length <= 0 ? origSnapshot.localRotation.value.w : snapshot.localRotation.value.w);

                                            newCurve.localScaleCurveX.AddKey(0, tc.localScaleCurveX == null || tc.localScaleCurveX.length <= 0 ? origSnapshot.localScale.x : snapshot.localScale.x);
                                            newCurve.localScaleCurveY.AddKey(0, tc.localScaleCurveY == null || tc.localScaleCurveY.length <= 0 ? origSnapshot.localScale.y : snapshot.localScale.y);
                                            newCurve.localScaleCurveZ.AddKey(0, tc.localScaleCurveZ == null || tc.localScaleCurveZ.length <= 0 ? origSnapshot.localScale.z : snapshot.localScale.z);  

                                            /*
                                            if (tc.localPositionCurveX == null || tc.localPositionCurveX.length <= 0) Debug.Log($"Empty {nameof(tc.localPositionCurveX)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localPositionCurveY == null || tc.localPositionCurveY.length <= 0) Debug.Log($"Empty {nameof(tc.localPositionCurveY)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localPositionCurveZ == null || tc.localPositionCurveZ.length <= 0) Debug.Log($"Empty {nameof(tc.localPositionCurveZ)} for base source at {tc.TransformName}, using default instead.");

                                            if (tc.localRotationCurveX == null || tc.localRotationCurveX.length <= 0) Debug.Log($"Empty {nameof(tc.localRotationCurveX)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localRotationCurveY == null || tc.localRotationCurveY.length <= 0) Debug.Log($"Empty {nameof(tc.localRotationCurveY)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localRotationCurveZ == null || tc.localRotationCurveZ.length <= 0) Debug.Log($"Empty {nameof(tc.localRotationCurveZ)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localRotationCurveW == null || tc.localRotationCurveW.length <= 0) Debug.Log($"Empty {nameof(tc.localRotationCurveW)} for base source at {tc.TransformName}, using default instead.");

                                            if (tc.localScaleCurveX == null || tc.localScaleCurveX.length <= 0) Debug.Log($"Empty {nameof(tc.localScaleCurveX)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localScaleCurveY == null || tc.localScaleCurveY.length <= 0) Debug.Log($"Empty {nameof(tc.localScaleCurveY)} for base source at {tc.TransformName}, using default instead.");
                                            if (tc.localScaleCurveZ == null || tc.localScaleCurveZ.length <= 0) Debug.Log($"Empty {nameof(tc.localScaleCurveZ)} for base source at {tc.TransformName}, using default instead.");
                                            */

                                            if (info.infoBase.isLinear)
                                            {
                                                var info_ = info.infoBase;
                                                info_.isLinear = false;
                                                info_.curveIndex = transformCurves.Count;
                                                info.infoBase = info_;

                                                transformCurves.Add(newCurve);
                                            }
                                            else
                                            {
                                                transformCurves[info.infoBase.curveIndex] = newCurve;
                                            }
                                        }
                                        else if (newBaseCurve is TransformLinearCurve tlc)
                                        {
                                            if (tlc.frames != null && tlc.frames.Length > 0)
                                            {
                                                var newCurve = new TransformLinearCurve();
                                                newCurve.name = newBaseCurve.TransformName;

                                                float newT = baseLength > 0 ? (BaseDataReferenceTime / baseLength) : 0;
                                                if (compiledAnimation.timeCurve != null && compiledAnimation.timeCurve.length > 0) newT = compiledAnimation.timeCurve.Evaluate(newT);

                                                var snapshot = tlc.Evaluate(newT);
                                                newCurve.AddFrame(new ITransformCurve.Frame() { timelinePosition = 0, data = snapshot }); 

                                                if (!info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = true;
                                                    info_.curveIndex = transformLinearCurves.Count;
                                                    info.infoBase = info_;

                                                    transformLinearCurves.Add(newCurve);
                                                }
                                                else
                                                {
                                                    transformLinearCurves[info.infoBase.curveIndex] = newCurve;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                    transformCurveInfo[a] = info;
                                }

                                for (int a = 0; a < propertyCurveInfo.Count; a++)
                                {
                                    var info = propertyCurveInfo[a];
                                    if (info.infoBase.curveIndex < 0) continue;

                                    IPropertyCurve currentBaseCurve = info.infoBase.isLinear ? propertyLinearCurves[info.infoBase.curveIndex] : propertyCurves[info.infoBase.curveIndex];
                                    if (currentBaseCurve == null) continue;

                                    foreach (var baseInfo in compiledBase.propertyAnimationCurves)
                                    {
                                        if (baseInfo.infoMain.curveIndex < 0) continue;

                                        IPropertyCurve newBaseCurve = baseInfo.infoMain.isLinear ? compiledBase.propertyLinearCurves[baseInfo.infoMain.curveIndex] : compiledBase.propertyCurves[baseInfo.infoMain.curveIndex];
                                        if (newBaseCurve == null || currentBaseCurve.PropertyString != newBaseCurve.PropertyString) continue;

                                        var baseLength = newBaseCurve.GetLengthInSeconds(compiledAnimation.framesPerSecond);
                                        if (newBaseCurve is PropertyCurve pc)
                                        {
                                            if (pc.propertyValueCurve != null && pc.propertyValueCurve.length > 0)
                                            {
                                                var newCurve = PropertyCurve.NewInstance;
                                                newCurve.name = newBaseCurve.PropertyString;

                                                float newT = baseLength > 0 ? (BaseDataReferenceTime / baseLength) : 0;
                                                if (compiledAnimation.timeCurve != null && compiledAnimation.timeCurve.length > 0) newT = compiledAnimation.timeCurve.Evaluate(newT);

                                                var snapshot = pc.Evaluate(newT); 
                                                newCurve.propertyValueCurve.AddKey(0, snapshot);

                                                if (info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = false;
                                                    info_.curveIndex = propertyCurves.Count;
                                                    info.infoBase = info_;

                                                    propertyCurves.Add(newCurve);
                                                }
                                                else
                                                {
                                                    propertyCurves[info.infoBase.curveIndex] = newCurve;
                                                }
                                            }
                                        }
                                        else if (newBaseCurve is PropertyLinearCurve plc)
                                        {
                                            if (plc.frames != null && plc.frames.Length > 0)
                                            {
                                                var newCurve = new PropertyLinearCurve();
                                                newCurve.name = newBaseCurve.PropertyString;

                                                float newT = baseLength > 0 ? (BaseDataReferenceTime / baseLength) : 0;
                                                if (compiledAnimation.timeCurve != null && compiledAnimation.timeCurve.length > 0) newT = compiledAnimation.timeCurve.Evaluate(newT);

                                                var snapshot = plc.Evaluate(newT);
                                                newCurve.AddFrame(new IPropertyCurve.Frame() { timelinePosition = 0, value = snapshot });

                                                if (!info.infoBase.isLinear)
                                                {
                                                    var info_ = info.infoBase;
                                                    info_.isLinear = true;
                                                    info_.curveIndex = propertyLinearCurves.Count;
                                                    info.infoBase = info_;

                                                    propertyLinearCurves.Add(newCurve);
                                                }
                                                else
                                                {
                                                    propertyLinearCurves[info.infoBase.curveIndex] = newCurve;
                                                }
                                            }
                                        }
                                        break;
                                    }

                                    propertyCurveInfo[a] = info;
                                }
                            }
                            else
                            {
                                swole.LogWarning($"Failed to compile base source '{baseDataSource.DisplayName}' for animation '{DisplayName}'");
                            }
                        }
                        break;
                }

                #endregion

                if (removeEmptyCurves)
                {
                    #region Remove Empty & Unused Curves
                    bool IsEmptyTransformCurve(CustomAnimation.CurveInfo info)
                    {
                        if (info.isLinear && info.curveIndex >= 0 && info.curveIndex < transformLinearCurves.Count)
                        {
                            var curve = transformLinearCurves[info.curveIndex];
                            if (curve == null || curve.frames == null || curve.frames.Length <= 0) return true; else return false;
                        }
                        else if (!info.isLinear && info.curveIndex >= 0 && info.curveIndex < transformCurves.Count)
                        {
                            var curve = transformCurves[info.curveIndex];
                            if (curve == null || (!math.any(curve.ValidityPosition) && !math.any(curve.ValidityRotation) && !math.any(curve.ValidityScale))) return true; else return false;
                        }

                        return true;
                    }
                    bool IsEmptyPropertyCurve(CustomAnimation.CurveInfo info)
                    {
                        if (info.isLinear && info.curveIndex >= 0 && info.curveIndex < propertyLinearCurves.Count)
                        {
                            var curve = propertyLinearCurves[info.curveIndex];
                            if (curve == null || curve.frames == null || curve.frames.Length <= 0) return true; else return false;
                        }
                        else if (!info.isLinear && info.curveIndex >= 0 && info.curveIndex < propertyCurves.Count)
                        {
                            var curve = propertyCurves[info.curveIndex];
                            if (curve == null || (curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 0)) return true; else return false;
                        }

                        return true;
                    }

                    bool DeleteTransformLinearCurve(int index)
                    {
                        foreach (var info in transformCurveInfo)
                        {
                            if (info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyTransformCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyTransformCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        transformCurveInfo.RemoveAll(i => (i.infoBase.isLinear && i.infoBase.curveIndex == index) || (i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < transformCurveInfo.Count; a++)
                        {
                            var info = transformCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            transformCurveInfo[a] = info;
                        }
                        transformLinearCurves.RemoveAt(index);
                        return true;
                    }
                    bool DeleteTransformCurve(int index)
                    {
                        foreach (var info in transformCurveInfo)
                        {
                            if (!info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyTransformCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (!info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyTransformCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        transformCurveInfo.RemoveAll(i => (!i.infoBase.isLinear && i.infoBase.curveIndex == index) || (!i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < transformCurveInfo.Count; a++)
                        {
                            var info = transformCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (!infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (!infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            transformCurveInfo[a] = info;
                        }
                        transformCurves.RemoveAt(index);
                        return true;
                    }

                    bool DeletePropertyLinearCurve(int index)
                    {
                        foreach (var info in propertyCurveInfo)
                        {
                            if (info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyPropertyCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyPropertyCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        propertyCurveInfo.RemoveAll(i => (i.infoBase.isLinear && i.infoBase.curveIndex == index) || (i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < propertyCurveInfo.Count; a++)
                        {
                            var info = propertyCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            propertyCurveInfo[a] = info;
                        }
                        propertyLinearCurves.RemoveAt(index);
                        return true;
                    }
                    bool DeletePropertyCurve(int index)
                    {
                        foreach (var info in propertyCurveInfo)
                        {
                            if (!info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyPropertyCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (!info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyPropertyCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        propertyCurveInfo.RemoveAll(i => (!i.infoBase.isLinear && i.infoBase.curveIndex == index) || (!i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < propertyCurveInfo.Count; a++)
                        {
                            var info = propertyCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (!infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (!infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            propertyCurveInfo[a] = info;
                        }
                        propertyCurves.RemoveAt(index);
                        return true;
                    }

                    int i;

                    i = 0;
                    while (i < transformLinearCurves.Count)
                    {
                        bool flag = true;
                        var curve = transformLinearCurves[i];
                        if (curve.frames == null || curve.frames.Length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in transformCurveInfo) // Check for references
                            {
                                if (info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeleteTransformLinearCurve(i);
                        if (flag) i++;
                    }
                    i = 0;
                    while (i < transformCurves.Count)
                    {
                        bool flag = true;
                        var curve = transformCurves[i];
                        if (!math.any(curve.ValidityPosition) && !math.any(curve.ValidityRotation) && !math.any(curve.ValidityScale)) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in transformCurveInfo) // Check for references
                            {
                                if (!info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (!info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeleteTransformCurve(i);
                        if (flag) i++;
                    }

                    i = 0;
                    while (i < propertyLinearCurves.Count)
                    {
                        bool flag = true;
                        var curve = propertyLinearCurves[i];
                        if (curve.frames == null || curve.frames.Length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in propertyCurveInfo) // Check for references
                            {
                                if (info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeletePropertyLinearCurve(i);
                        if (flag) i++;
                    }
                    i = 0;
                    while (i < propertyCurves.Count)
                    {
                        bool flag = true;
                        var curve = propertyCurves[i];
                        if (curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in propertyCurveInfo) // Check for references
                            {
                                if (!info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (!info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeletePropertyCurve(i);
                        if (flag) i++;
                    }
                    #endregion
                }

                #region Bake Loop Points

                AnimationCurveEditor.KeyframeStateRaw[] LoopCurve(List<AnimationUtils.LoopPoint> loopPoints, AnimationCurveEditor.KeyframeStateRaw[] oldKeyframes)
                {
                    if (oldKeyframes.Length <= 1 || loopPoints.Count <= 0) return null;

                    List<AnimationCurveEditor.KeyframeStateRaw> keyframes = new List<AnimationCurveEditor.KeyframeStateRaw>(oldKeyframes);
                    var firstFrame = keyframes[0];

                    for (int a = 0; a < loopPoints.Count; a++)
                    {
                        var lp = loopPoints[a];
                        if (lp == null) continue;

                        var key = firstFrame;
                        key.time = lp.position / (float)rawAnimation.framesPerSecond;
                        // Prevent overlapping keyframes
                        bool overlapping = false;
                        foreach (var kf in keyframes)
                        {
                            if (kf.time == key.time)
                            {
                                overlapping = true;
                                break;
                            }
                        }
                        //
                        if (!overlapping) keyframes.Add(key);
                    }

                    keyframes.Sort((x, y) => (int)Mathf.Sign(x.time - y.time));
                    return keyframes.ToArray();
                }

                EditableAnimationCurve LoopAnimationCurve(List<AnimationUtils.LoopPoint> loopPoints, EditableAnimationCurve curve)
                {
                    if (curve == null) return null;

                    AnimationCurveEditor.KeyframeStateRaw[] newKeys = LoopCurve(loopPoints, curve.Keys);
                    if (newKeys != null)
                    {
                        curve = curve.AsSerializableStruct();
                        curve.Keys = newKeys;
                    }
                    return curve;
                }

                TransformCurve LoopTransformCurve(TransformCurve curve)
                {
                    if (curve == null) return null;

                    bool cloned = false;
                    EditableAnimationCurve LoopInnerCurve(EditableAnimationCurve innerCurve, string suffix)
                    {
                        tempLPs.Clear();
                        GetLoopPoints(curve.name + suffix, tempLPs);
                        if (tempLPs.Count > 0)
                        {
                            if (!cloned)
                            {
                                curve = (TransformCurve)curve.Clone();
                                cloned = true;
                            }
                            return LoopAnimationCurve(tempLPs, innerCurve);
                        }
                        return innerCurve;
                    }

                    EditableAnimationCurve curveOut;

                    curveOut = LoopInnerCurve(curve.localPositionCurveX, ".localPosition.x");
                    curve.localPositionCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localPositionCurveY, ".localPosition.y");
                    curve.localPositionCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localPositionCurveZ, ".localPosition.z");
                    curve.localPositionCurveZ = curveOut;

                    curveOut = LoopInnerCurve(curve.localRotationCurveX, ".localRotation.x");
                    curve.localRotationCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveY, ".localRotation.y");
                    curve.localRotationCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveZ, ".localRotation.z");
                    curve.localRotationCurveZ = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveW, ".localRotation.w");
                    curve.localRotationCurveW = curveOut;

                    curveOut = LoopInnerCurve(curve.localScaleCurveX, ".localScale.x");
                    curve.localScaleCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localScaleCurveY, ".localScale.y");
                    curve.localScaleCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localScaleCurveZ, ".localScale.z");
                    curve.localScaleCurveZ = curveOut;

                    return curve;
                }

                for (int a = 0; a < transformLinearCurves.Count; a++)
                {
                    var curve = transformLinearCurves[a];
                    if (curve == null || curve.frames == null || curve.frames.Length <= 1) continue;

                    string id = curve.name + ".linear";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (TransformLinearCurve)curve.Clone();
                        List<ITransformCurve.Frame> keyframes = new List<ITransformCurve.Frame>(curve.frames);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.timelinePosition == lp.position)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(new ITransformCurve.Frame() { timelinePosition = lp.position, data = firstFrame.data, interpolationCurve = firstFrame.interpolationCurve });
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                            curve.frames = keyframes.ToArray();

                            transformLinearCurves[a] = curve;
                        }
                    }

                }

                for (int a = 0; a < transformCurves.Count; a++) transformCurves[a] = LoopTransformCurve(transformCurves[a]);

                for (int a = 0; a < propertyLinearCurves.Count; a++)
                {
                    var curve = propertyLinearCurves[a];
                    if (curve == null || curve.frames == null || curve.frames.Length <= 1) continue;

                    string id = curve.name + ".linear";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (PropertyLinearCurve)curve.Clone();
                        List<IPropertyCurve.Frame> keyframes = new List<IPropertyCurve.Frame>(curve.frames);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.timelinePosition == lp.position)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(new IPropertyCurve.Frame() { timelinePosition = lp.position, value = firstFrame.value, interpolationCurve = firstFrame.interpolationCurve });
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                            curve.frames = keyframes.ToArray();

                            propertyLinearCurves[a] = curve;
                        }
                    }

                }

                for (int a = 0; a < propertyCurves.Count; a++)
                {
                    var curve = propertyCurves[a];
                    if (curve == null || curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 1) continue;

                    string id = curve.name + ".value";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (PropertyCurve)curve.Clone();
                        List<Keyframe> keyframes = new List<Keyframe>(curve.propertyValueCurve.keys);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                var key = firstFrame;
                                key.time = lp.position / (float)rawAnimation.framesPerSecond;
                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.time == key.time)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(key);
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.time - y.time));
                            curve.propertyValueCurve.keys = keyframes.ToArray();

                            propertyCurves[a] = curve;
                        }
                    }
                }

                #endregion            

                #region Ensure Animation Length

                float currentLength = 0;

                foreach (var curve in transformLinearCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in transformCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in propertyLinearCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in propertyCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));

                /*List<Swole.Debugging.AnimationCurveInspector.CurveView> debugTransformCurves = new List<Debugging.AnimationCurveInspector.CurveView>(); // DEBUGGING (VIEWING COMPILED CURVES)
                var temp = new GameObject($"VIEWER").AddComponent<Swole.Debugging.AnimationCurveInspector>();
                foreach (var curve in transformCurves) 
                {  
                    if (curve.localPositionCurveX != null && curve.localPositionCurveX.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localPositionCurveX), curve = curve.localPositionCurveX });
                    if (curve.localPositionCurveY != null && curve.localPositionCurveY.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localPositionCurveY), curve = curve.localPositionCurveY });
                    if (curve.localPositionCurveZ != null && curve.localPositionCurveZ.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localPositionCurveZ), curve = curve.localPositionCurveZ });

                    if (curve.localRotationCurveX != null && curve.localRotationCurveX.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localRotationCurveX), curve = curve.localRotationCurveX });
                    if (curve.localRotationCurveY != null && curve.localRotationCurveY.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localRotationCurveY), curve = curve.localRotationCurveY });
                    if (curve.localRotationCurveZ != null && curve.localRotationCurveZ.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localRotationCurveZ), curve = curve.localRotationCurveZ });
                    if (curve.localRotationCurveW != null && curve.localRotationCurveW.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localRotationCurveW), curve = curve.localRotationCurveW });

                    if (curve.localScaleCurveX != null && curve.localScaleCurveX.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localScaleCurveX), curve = curve.localScaleCurveX });
                    if (curve.localScaleCurveY != null && curve.localScaleCurveY.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localScaleCurveY), curve = curve.localScaleCurveY });
                    if (curve.localScaleCurveZ != null && curve.localScaleCurveZ.length > 0) debugTransformCurves.Add(new Debugging.AnimationCurveInspector.CurveView() { name = curve.TransformName + "." + nameof(curve.localScaleCurveZ), curve = curve.localScaleCurveZ }); 
                }
                temp.curveViews = debugTransformCurves.ToArray();*/

                if (currentLength < timelineLength)
                {
                    bool LengthenCurve(EditableAnimationCurve curve, out EditableAnimationCurve outCurve)
                    {
                        outCurve = curve;
                        if (curve == null || curve.length <= 0 || !(curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once)) return false;

                        outCurve = new EditableAnimationCurve();
                        outCurve.preWrapMode = curve.preWrapMode;
                        outCurve.postWrapMode = curve.postWrapMode;

                        tempKeyframes.Clear();
                        for (int a = 0; a < curve.length; a++) tempKeyframes.Add(curve.GetKey(a));

                        var finalKey = tempKeyframes[tempKeyframes.Count - 1];
                        finalKey.time = timelineLength;

                        //finalKey = AnimationCurveEditor.CalculateLinearInTangent(finalKey, tempKeyframes[tempKeyframes.Count - 1]);
                        var tempKey = AnimationCurveEditor.CalculateLinearInTangent(finalKey, tempKeyframes[tempKeyframes.Count - 1]);
                        finalKey.tangentMode = (int)AnimationCurveEditor.TangentMode.Broken; 
                        finalKey.inTangentMode = (int)AnimationCurveEditor.BrokenTangentMode.Linear; 
                        finalKey.inWeight = tempKey.inWeight;
                        finalKey.inTangent = tempKey.inTangent;

                        //tempKeyframes[tempKeyframes.Count - 1] = AnimationCurveEditor.CalculateLinearOutTangent(tempKeyframes[tempKeyframes.Count - 1], finalKey);
                        var prevFinalKey = tempKeyframes[tempKeyframes.Count - 1];
                        tempKey = AnimationCurveEditor.CalculateLinearOutTangent(prevFinalKey, finalKey);
                        prevFinalKey.tangentMode = (int)AnimationCurveEditor.TangentMode.Broken;
                        prevFinalKey.outTangentMode = (int)AnimationCurveEditor.BrokenTangentMode.Linear;
                        prevFinalKey.outWeight = tempKey.outWeight;
                        prevFinalKey.outTangent = tempKey.outTangent;
                        tempKeyframes[tempKeyframes.Count - 1] = prevFinalKey; 

                        tempKeyframes.Add(finalKey);
                        outCurve.Keys = tempKeyframes.ToArray();

                        //var temp = new GameObject($"LENGTHENED CURVE VIEWER").AddComponent<Swole.Debugging.AnimationCurveInspector>();
                        //temp.curve = outCurve;  

                        return true;
                    }

                    bool flag = true;
                    for(int a = 0; a < transformCurves.Count; a++)
                    {
                        var curve = transformCurves[a];
                        if (curve != null)
                        {
                            EditableAnimationCurve editedCurve;
                            if (LengthenCurve(curve.localPositionCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localPositionCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localPositionCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveZ = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveZ = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveW, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveW = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveZ = editedCurve;
                            }

                            if (!flag)
                            {
                                transformCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < propertyCurves.Count; a++)
                        {
                            var curve = propertyCurves[a];
                            if (curve != null && LengthenCurve(curve.propertyValueCurve, out EditableAnimationCurve editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.propertyValueCurve = editedCurve;
                                propertyCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < transformLinearCurves.Count; a++)
                        {
                            var curve = transformLinearCurves[a];
                            if (curve != null && curve.frames != null && curve.frames.Length > 0 && (curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();

                                tempTransformFrames.Clear();
                                tempTransformFrames.AddRange(curve.frames);
                                tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                                var lastFrame = tempTransformFrames[tempTransformFrames.Count - 1];
                                lastFrame.timelinePosition = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                                tempTransformFrames.Add(lastFrame);
                                curve.frames = tempTransformFrames.ToArray(); 

                                transformLinearCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < propertyLinearCurves.Count; a++)
                        {
                            var curve = propertyLinearCurves[a];
                            if (curve != null && curve.frames != null && curve.frames.Length > 0 && (curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once))
                            {
                                curve = curve.ShallowCopy();

                                tempPropertyFrames.Clear();
                                tempPropertyFrames.AddRange(curve.frames);
                                tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                                var lastFrame = tempPropertyFrames[tempPropertyFrames.Count - 1];
                                lastFrame.timelinePosition = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                                tempPropertyFrames.Add(lastFrame);
                                curve.frames = tempPropertyFrames.ToArray();

                                propertyLinearCurves[a] = curve;
                                flag = false;
                                break;
                            }
                        }
                    }
                }
                else if (currentLength > timelineLength)
                {

                    bool ShortenCurve(EditableAnimationCurve curve, out EditableAnimationCurve outCurve)
                    {
                        outCurve = curve;
                        if (curve == null || curve.length <= 0 || curve[curve.length - 1].time <= timelineLength) return false;

                        outCurve = new EditableAnimationCurve();//new AnimationCurve();
                        outCurve.preWrapMode = curve.preWrapMode;
                        outCurve.postWrapMode = curve.postWrapMode;

                        tempKeyframes.Clear();
                        for (int a = 0; a < curve.length; a++) tempKeyframes.Add(curve.GetKey(a));

                        while (tempKeyframes.Count > 1 && tempKeyframes[tempKeyframes.Count - 2].time > timelineLength) tempKeyframes.RemoveAt(tempKeyframes.Count - 1);
                        var finalKey = tempKeyframes[tempKeyframes.Count - 1];
                        finalKey.time = timelineLength;
                        tempKeyframes[tempKeyframes.Count - 1] = finalKey;

                        outCurve.Keys = tempKeyframes.ToArray();
                        return true;
                    }

                    for (int a = 0; a < transformCurves.Count; a++)
                    {
                        var curve = transformCurves[a];
                        if (curve != null)
                        {
                            EditableAnimationCurve editedCurve;
                            bool canCopy = true;
                            if (ShortenCurve(curve.localPositionCurveX, out editedCurve))
                            {
                                if (canCopy) 
                                { 
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localPositionCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localPositionCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveZ = editedCurve;
                            }

                            if (ShortenCurve(curve.localRotationCurveX, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveZ = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveW, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveW = editedCurve;
                            }

                            if (ShortenCurve(curve.localScaleCurveX, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localScaleCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localScaleCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveZ = editedCurve;
                            }

                            transformCurves[a] = curve;
                        }
                    }

                    for (int a = 0; a < propertyCurves.Count; a++)
                    {
                        var curve = propertyCurves[a];
                        if (curve != null && ShortenCurve(curve.propertyValueCurve, out EditableAnimationCurve editedCurve))
                        {
                            curve = curve.ShallowCopy();
                            curve.propertyValueCurve = editedCurve;
                            propertyCurves[a] = curve; 
                        }
                    }

                    int frameLength = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                    for (int a = 0; a < transformLinearCurves.Count; a++)
                    {
                        var curve = transformLinearCurves[a];
                        if (curve != null && curve.GetLengthInSeconds(rawAnimation.framesPerSecond) > timelineLength)
                        {
                            curve = curve.ShallowCopy();

                            tempTransformFrames.Clear();
                            tempTransformFrames.AddRange(curve.frames);
                            tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                            while (tempTransformFrames.Count > 1 && tempTransformFrames[tempTransformFrames.Count - 2].timelinePosition > frameLength) tempTransformFrames.RemoveAt(tempTransformFrames.Count - 1);
                            tempTransformFrames[tempTransformFrames.Count - 1].timelinePosition = frameLength; 

                            curve.frames = tempTransformFrames.ToArray();
                             
                            transformLinearCurves[a] = curve;
                        }
                    }
                      
                    for (int a = 0; a < propertyLinearCurves.Count; a++)
                    {
                        var curve = propertyLinearCurves[a];
                        if (curve != null && curve.GetLengthInSeconds(rawAnimation.framesPerSecond) > timelineLength)
                        {
                            curve = curve.ShallowCopy();

                            tempPropertyFrames.Clear();
                            tempPropertyFrames.AddRange(curve.frames);
                            tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                            while (tempPropertyFrames.Count > 1 && tempPropertyFrames[tempPropertyFrames.Count - 2].timelinePosition > frameLength) tempPropertyFrames.RemoveAt(tempPropertyFrames.Count - 1);
                            tempPropertyFrames[tempPropertyFrames.Count - 1].timelinePosition = frameLength;

                            curve.frames = tempPropertyFrames.ToArray();

                            propertyLinearCurves[a] = curve; 
                        }
                    }
                }

                #endregion

                //compiledAnimation.transformLinearCurves = transformLinearCurves;
                //compiledAnimation.transformCurves = transformCurves;
                //compiledAnimation.propertyLinearCurves = propertyLinearCurves;
                //compiledAnimation.propertyCurves = propertyCurves;

                compiledAnimation.transformLinearCurves = transformLinearCurves.ToArray();
                compiledAnimation.transformCurves = transformCurves.ToArray();
                compiledAnimation.propertyLinearCurves = propertyLinearCurves.ToArray();
                compiledAnimation.propertyCurves = propertyCurves.ToArray();

                compiledAnimation.transformAnimationCurves = transformCurveInfo.ToArray();
                compiledAnimation.propertyAnimationCurves = propertyCurveInfo.ToArray();

                transformLinearCurves.Clear();
                transformCurves.Clear();
                propertyLinearCurves.Clear();
                propertyCurves.Clear();

                transformCurveInfo.Clear();
                propertyCurveInfo.Clear();

                isCompiled = true;

                if (!isNew)
                {
                    try
                    {
                        if (previewAnimator != null) previewAnimator.OutputDependency.Complete();
                        compiledAnimation.FlushJobData();
                    }
                    catch (Exception e)
                    {
                        swole.LogError(e.Message);
                    }
                }

                PreviewAsset.Animation = compiledAnimation;
            }

            return compiledAnimation;
        }

        public bool ContainsCurve(EditableAnimationCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (ReferenceEquals(rawAnimation.timeCurve, curve)) return true;
            if (rawAnimation.transformCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformCurves) 
                {
                    if (ocurve == null) continue;

                    if (ReferenceEquals(ocurve.localPositionTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveZ, curve)) return true;

                    if (ReferenceEquals(ocurve.localRotationTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveZ, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveW, curve)) return true;

                    if (ReferenceEquals(ocurve.localScaleTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveZ, curve)) return true;
                }
            }
            if (rawAnimation.propertyCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyCurves)
                {
                    if (ocurve == null) continue;

                    if (ReferenceEquals(ocurve.propertyValueCurve, curve)) return true;
                }
            }
            if (rawAnimation.transformLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformLinearCurves)
                {
                    if (ocurve == null || ocurve.frames == null) continue;
                    foreach (var frame in ocurve.frames) if (frame != null && ReferenceEquals(frame.interpolationCurve, curve)) return true;
                }
            }
            if (rawAnimation.propertyLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyLinearCurves)
                {
                    if (ocurve == null || ocurve.frames == null) continue;
                    foreach (var frame in ocurve.frames) if (frame != null && ReferenceEquals(frame.interpolationCurve, curve)) return true;
                }
            }

            return false;
        }
        public bool ContainsCurve(ITransformCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (rawAnimation.transformCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }
            if (rawAnimation.transformLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformLinearCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }

            return false;
        }
        public bool ContainsCurve(IPropertyCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (rawAnimation.propertyCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }
            if (rawAnimation.propertyLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyLinearCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }

            return false;
        }

        public void InsertKeyframes(bool createFromPreviousKeyIfApplicable, bool useReferencePose, ImportedAnimatable animatable, float time, IntFromDecimalDelegate getFrameIndex = null, bool onlyInsertChangedData = true, List<Transform> transformMask = null, WrapMode preWrapMode = WrapMode.Clamp, WrapMode postWrapMode = WrapMode.Clamp, bool verbose=false, bool allowTranslation = true, bool allowRotation = true, bool allowScaling = true)
        {

            if (animatable == null) return;
            if (animatable.RestPose == null) 
            {
                swole.LogWarning($"Tried to insert keyframes into source '{displayName}' using animatable '{animatable.displayName}', but the animatable has no rest pose!");
                return;
            }
               
            AnimationUtils.Pose originalPoseAtTime = animatable.RestPose.Duplicate();

            if (rawAnimation != null)
            {
                var compAnim = CompiledAnimation;  
                if (compAnim != null) originalPoseAtTime.ApplyAnimation(compAnim, time, false, 1, null, preWrapMode, postWrapMode); // Get the current pose at this point in the animation
            }

            var newPose = originalPoseAtTime;   

            animatable.RefreshLastPose();
            if (animatable.lastPose != null) 
            {
                if (animatable.referencePose != null && useReferencePose)
                {
                    //animatable.CreateSnapshot("referencePose", animatable.referencePose); // Debug 
                    //animatable.CreateSnapshot("lastPose", animatable.lastPose);
                    //animatable.CreateSnapshot("originalPoseAtTime", originalPoseAtTime);
                    // Applies the pose change in an additive fashion to the current pose at time in the animation. Allows for correct pose changes even if other animations have been played on the rig (in theory)
                    var difference = animatable.referencePose.GetDifference(animatable.lastPose); // lastPose - referencePose
                    newPose = originalPoseAtTime.Duplicate().ApplyDeltas(difference);
                    //animatable.CreateSnapshot("newPose", newPose);  
                    if (verbose) swole.Log($"Inserting key at [{time}] using difference between current pose and animator reference pose.");
                } 
                else 
                {
                    newPose = animatable.lastPose.Duplicate();
                    if (verbose) swole.Log($"Inserting key at [{time}] using current pose.");
                }
            }

            InsertKeyframes(animatable.animator == null ? null : animatable.animator.avatar, createFromPreviousKeyIfApplicable, time, getFrameIndex, newPose, animatable.RestPose, onlyInsertChangedData ? originalPoseAtTime : null, transformMask, preWrapMode, postWrapMode, verbose, allowTranslation, allowRotation, allowScaling);
        }
        public void InsertKeyframes(CustomAvatar avatar, bool createFromPreviousKeyIfApplicable, float timelinePosition, IntFromDecimalDelegate getFrameIndex, AnimationUtils.Pose pose, AnimationUtils.Pose restPose, AnimationUtils.Pose originalPose = null, List<Transform> transformMask = null, WrapMode preWrapMode = WrapMode.Clamp, WrapMode postWrapMode = WrapMode.Clamp, bool verbose = false, bool allowTranslation = true, bool allowRotation = true, bool allowScaling = true)
        {
            var anim = GetOrCreateRawData(); 
            if (anim != null)
            {
                if (restPose != null)
                {
                    if (createFromPreviousKeyIfApplicable && anim.HasKeyframes)
                    {
                        float prevTime = anim.GetClosestKeyframeTime(timelinePosition, false, getFrameIndex); // Find the time of the key that comes before timelinePosition
                        //Debug.Log(prevTime + " -> " + timelinePosition); 
                        var prevKeyPose = restPose.Duplicate().ApplyAnimation(anim, prevTime, false, 1, null, preWrapMode, postWrapMode); // Get the pose of the key at prevTime
                        var difference = prevKeyPose.GetDifference(pose); // Get the difference between the previous key and the new key (pose - prevKeyPose)
                        pose = prevKeyPose.ApplyDeltas(difference); // Apply the new pose on top of the previous so that the shortest rotation between the two will be used  
                    }
                }
                
                pose.Insert(avatar, anim, timelinePosition, restPose, originalPose, transformMask, getFrameIndex != null, getFrameIndex, verbose, allowTranslation, allowRotation, allowScaling);
            }
            MarkAsDirty();
        }
    }

}

#endif