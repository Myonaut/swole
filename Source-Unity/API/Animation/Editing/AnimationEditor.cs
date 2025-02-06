#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using Unity.Mathematics;

using TMPro;

using Swole.UI;
using Swole.Animation;

#if BULKOUT_ENV
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

using static Swole.API.Unity.SwoleCurveEditor;
using static Swole.API.Unity.CustomEditorUtils;
using Swole.Script;

namespace Swole.API.Unity.Animation
{
    public class AnimationEditor : MonoBehaviour, IExecutableBehaviour
    {

        public virtual int Priority => CustomAnimatorUpdater.ExecutionPriority; // Same as animators
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority); 

        #region Input

        public bool CursorIsInTimeline
        {
            get
            {
                return timelineWindow.ContainerTransform.ContainsWorldPosition(timelineWindow.ScreenPositionToWorldPosition(CursorProxy.ScreenPosition));
            }
        }
        public bool CursorIsOnTimeline
        {
            get
            {
                GameObject obj = CursorProxy.FirstObjectUnderCursor;
                return (obj == timelineWindow.gameObject || obj == timelineWindow.ContainerTransform.gameObject); 
            }
        }
        public bool CursorIsOverTimelineObject
        {
            get
            {
                GameObject obj = CursorProxy.FirstObjectUnderCursor;
                if (obj != null)
                {
                    Transform tr = obj.transform;
                    return tr.IsChildOf(timelineWindow.transform) || tr.IsChildOf(timelineWindow.ContainerTransform);
                }

                return false;
            }
        }

        protected void InputStep()
        {

            if (InputProxy.Modding_PrimeActionKey)
            {
                if (InputProxy.Modding_UndoKeyDown)
                {
                    if (InputProxy.Modding_ModifyActionKey)
                    {
                        Redo();
                    } 
                    else
                    {
                        Undo();
                    }
                } 
                else if (InputProxy.Modding_SaveKeyDown)
                {
                    QuickSaveCurrentSession(); 
                }
            }

            if (InputProxy.Modding_SelectAllKeyDown)
            {
                if (CursorIsInTimeline) 
                {
                    if (SelectedKeyframeCount > 0) DeselectAllKeyframes(); else SelectAllKeyframes();
                }
            }
            if (InputProxy.CursorPrimaryButtonDown)
            {
                if (CursorIsOnTimeline) DeselectAllKeyframes();
            }
            if (InputProxy.Modding_DeleteKeyDown)
            {
                if (CursorIsOverTimelineObject) DeleteSelectedKeyframes();    
            }

            if (InputProxy.Modding_PrimeActionKey)
            {
                if (InputProxy.Modding_CopyKeyDown)
                {
                    if (CursorIsOverTimelineObject) CopyKeyframes();
                } 
                else if (InputProxy.Modding_PasteKeyDown && timelineWindow != null) 
                {
                    if (CursorIsOverTimelineObject) PasteKeyframes(timelineWindow.ScrubFrame); 
                }
            }
        }

        #endregion

        #region Undo System

        protected readonly ActionBasedUndoSystem undoSystem = new ActionBasedUndoSystem();
        protected int ignoreUndoHistoryRecording;
        public void IncrementIgnoreUndoHistoryRecording(int increment) => ignoreUndoHistoryRecording = Mathf.Max(0, ignoreUndoHistoryRecording + increment);
        public void RecordRevertibleAction(IRevertableAction revertable) 
        {
            if (ignoreUndoHistoryRecording > 0)
            {
                ignoreUndoHistoryRecording--;
                return;
            }

            undoSystem.AddHistory(revertable); 
#if UNITY_EDITOR
            Debug.Log($"Added undo history {undoSystem.Count}");
#endif
        }

        public void Undo() => undoSystem.Undo(); 
        public void Redo() => undoSystem.Redo();

        public void ClearUndoHistory() 
        {
            DiscardAnimationEditRecord();
            undoSystem.ClearHistory();
            RefreshUndoButtons();
        }

        public void RefreshUndoButtons()
        {
            if (undoButton != null)
            { 
                CustomEditorUtils.SetButtonInteractable(undoButton, undoSystem.HistoryPosition >= 0 && undoSystem.Count > 0); 
            }
            if (redoButton != null)
            {
                CustomEditorUtils.SetButtonInteractable(redoButton, undoSystem.HistoryPosition < undoSystem.Count - 1 && undoSystem.Count > 0);
            }
        }

        public struct UndoableImportAnimatable : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => false; 

            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public int importIndex;
            public int activeIndex;

            public UndoableImportAnimatable(AnimationEditor editor, int importIndex, int activeIndex, ImportedAnimatable animatable)
            {
                this.editor = editor;
                this.importIndex = importIndex;
                this.activeIndex = activeIndex;
                this.animatable = animatable;
                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.AddAnimatable(animatable, importIndex, false, true);
                editor.SetActiveObject(activeIndex, true, true, true);
            }
            
            public void Revert()
            {
                if (editor == null) return;
                editor.RemoveAnimatable(animatable, false, false);
                editor.SetActiveObject(activeIndex, true, true, true);  
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() 
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                if (!editor.RemoveAnimatable(animatable, true)) animatable.Destroy(); 
                animatable = null;
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public struct UndoableRemoveAnimatable : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => false;

            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public int index;
            public int activeIndex;

            public UndoableRemoveAnimatable(AnimationEditor editor, int index, int activeIndex, ImportedAnimatable animatable)
            {
                this.editor = editor;
                this.index = index;
                this.activeIndex = activeIndex;
                this.animatable = animatable;

                undoState = false;
            }

            public void Perpetuate()
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                if (!editor.RemoveAnimatable(animatable, true, false)) animatable.Destroy();  
                animatable = null;
            }
            public void PerpetuateUndo()
            {
            }

            public void Reapply()
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                editor.RemoveAnimatable(animatable, false, false);
                editor.SetActiveObject(activeIndex, true, true, true);
            } 

            public void Revert()
            {
                if (editor == null || animatable == null) return;
                editor.AddAnimatable(animatable, index);
                editor.SetActiveObject(activeIndex, true, true, true);
            }
            
            public bool undoState;
            public bool GetUndoState() => undoState;

            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        public struct UndoableCreateNewAnimationSource : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public AnimationSource source;
            public int sourceIndex;
            public int editIndex;

            public UndoableCreateNewAnimationSource(AnimationEditor editor, ImportedAnimatable animatable, AnimationSource source, int sourceIndex, int editIndex)
            {
                this.editor = editor;
                this.animatable = animatable;
                this.source = source;
                this.sourceIndex = sourceIndex;
                this.editIndex = editIndex;

                undoState = false;
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                if (animatable != null)
                {
                    animatable.AddNewAnimationSource(editor, source, false, sourceIndex, false, true);
                    animatable.SetCurrentlyEditedSource(editor, editIndex, false);
                }
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                if (animatable != null)
                {
                    animatable.RemoveAnimationSource(editor, source, false);
                    animatable.SetCurrentlyEditedSource(editor, editIndex, false);
                }
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() 
            {
                if (source == null) return;

                source.Destroy();
                source = null;
            }
            
            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public struct UndoableRemoveAnimationSource : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public AnimationSource source;
            public int sourceIndex;
            public int editIndex;

            public UndoableRemoveAnimationSource(AnimationEditor editor, ImportedAnimatable animatable, AnimationSource source, int sourceIndex, int editIndex)
            {
                this.editor = editor;
                this.animatable = animatable;
                this.source = source;
                this.sourceIndex = sourceIndex;
                this.editIndex = editIndex;

                undoState = false;
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                if (animatable != null)
                {
                    animatable.RemoveAnimationSource(editor, source, false);
                    animatable.SetCurrentlyEditedSource(editor, editIndex, false);
                }
            }
            
            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                if (animatable != null)
                {
                    animatable.AddNewAnimationSource(editor, source, false, sourceIndex, false, true);
                    animatable.SetCurrentlyEditedSource(editor, editIndex, false);
                }
            }

            public void Perpetuate() 
            {
                if (source == null) return;

                source.Destroy();
                source = null;
            }
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

        public struct UndoableSwapAnimationSourceIndex : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => false;

            public AnimationEditor editor;
            public int animatableIndex, oldSourceIndex, newSourceIndex;

            public UndoableSwapAnimationSourceIndex(AnimationEditor editor, int animatableIndex, int oldSourceindex, int newSourceIndex)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;
                this.oldSourceIndex = oldSourceindex;
                this.newSourceIndex = newSourceIndex; 

                undoState = false; 
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;
                 
                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null && obj.animationBank != null)
                {
                    var source = obj.animationBank[oldSourceIndex];
                    obj.RepositionSource(editor, source, newSourceIndex, false); 
                }

                editor.RefreshAnimatableListUI();
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                var obj = sesh.GetAnimatable(animatableIndex);
                if (obj != null && obj.animationBank != null)
                {
                    var source = obj.animationBank[newSourceIndex];
                    obj.RepositionSource(editor, source, oldSourceIndex, false); 
                }
                
                editor.RefreshAnimatableListUI();
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

        public struct UndoableStartEditingCurve : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public EditableAnimationCurve curve;

            public UndoableStartEditingCurve(AnimationEditor editor, EditableAnimationCurve curve)
            {
                this.editor = editor;
                this.curve = curve;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.StartEditingCurve(curve, false, false);
            }

            public void Revert()
            {
                if (editor == null) return;
                editor.StopEditingCurve(false);
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
        public struct UndoableStopEditingCurve : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => false;

            public AnimationEditor editor;
            public EditableAnimationCurve curve;

            public UndoableStopEditingCurve(AnimationEditor editor, EditableAnimationCurve curve)
            {
                this.editor = editor;
                this.curve = curve;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.StopEditingCurve(false);
            }
            
            public void Revert()
            {
                if (editor == null) return;
                editor.StartEditingCurve(curve, false, false);
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

        public struct PoseTransformState
        {
            public Transform transform;
            public TransformState state;
        }
        public struct TransformCurveEditState
        {
            public string transformName;
            public TransformCurve.StateData state;
        }
        public struct RawTransformCurveEditState
        {
            public TransformCurve curve;
            public TransformCurve.StateData state;
        }
        public struct TransformLinearCurveEditState
        {
            public string transformName;
            public TransformLinearCurve.StateData state;
        }
        public struct RawTransformLinearCurveEditState
        {
            public TransformLinearCurve curve;
            public TransformLinearCurve.StateData state;
        }

        public struct PropertyCurveEditState
        {
            public string propertyName;
            public PropertyCurve.StateData state;
        }
        public struct RawPropertyCurveEditState
        {
            public PropertyCurve curve;
            public PropertyCurve.StateData state;
        }
        public struct PropertyLinearCurveEditState
        {
            public string propertyName;
            public PropertyLinearCurve.StateData state;
        }
        public struct RawPropertyLinearCurveEditState
        {
            public PropertyLinearCurve curve;
            public PropertyLinearCurve.StateData state;
        }

        public struct RawCurveEditState
        {
            public EditableAnimationCurve curve;
            public AnimationCurveEditor.State state;
        }

        public struct AnimationEventEditState
        {
            public CustomAnimation.Event instance;
            public CustomAnimation.Event.Serialized state;
        }

        public class UndoableEditAnimationSourceData : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public AnimationSource source;

            public float originalPlaybackPosition;
            public float playbackPosition;

            #region Original States

            private List<PoseTransformState> originalPose;

            public void SetOriginalPoseTransformState(Transform transform)
            {
                if (transform == null) return;

                if (originalPose == null) originalPose = new List<PoseTransformState>();

                for (int a = 0; a < originalPose.Count; a++)
                {
                    var state_ = originalPose[a];
                    if (state_.transform == transform) return; // exit if the original transform data has already been set
                }

                originalPose.Add(new PoseTransformState() { transform = transform, state = new TransformState(transform, false) });
            }

            private CustomAnimation.Event[] originalEvents;
            public void SetOriginalEvents(CustomAnimation.Event[] events)
            {
                if (originalEvents != null) return;

                originalEvents = events;
            }
            private List<AnimationEventEditState> eventOriginalStates;
            public void SetOriginalEventState(CustomAnimation.Event event_)
            {
                if (event_ == null) return;

                if (eventOriginalStates == null) eventOriginalStates = new List<AnimationEventEditState>();

                for (int a = 0; a < eventOriginalStates.Count; a++)
                {
                    var state_ = eventOriginalStates[a];
                    if ((ReferenceEquals(state_.instance, event_) || state_.instance.CachedId == event_.CachedId)) return; 
                }

                eventOriginalStates.Add(new AnimationEventEditState() { instance = event_, state = event_.State });
            }

            private List<RawCurveEditState> rawCurveOriginalStates;

            public void SetOriginalRawCurveState(EditableAnimationCurve curve)
            {
                if (curve == null) return;

                if (rawCurveOriginalStates == null) rawCurveOriginalStates = new List<RawCurveEditState>();

                for (int a = 0; a < rawCurveOriginalStates.Count; a++)
                {
                    var state_ = rawCurveOriginalStates[a];
                    if (ReferenceEquals(state_.curve, curve)) return;
                }

                rawCurveOriginalStates.Add(new RawCurveEditState() { curve = curve, state = curve.State });
            }

            private List<RawTransformCurveEditState> rawTransformCurveOriginalStates;
            private List<RawPropertyCurveEditState> rawPropertyCurveOriginalStates;

            public void SetOriginalRawTransformCurveState(TransformCurve curve)
            {
                if (curve == null) return;
                
                if (rawTransformCurveOriginalStates == null) rawTransformCurveOriginalStates = new List<RawTransformCurveEditState>();              

                for (int a = 0; a < rawTransformCurveOriginalStates.Count; a++)
                {
                    var state_ = rawTransformCurveOriginalStates[a];
                    if (ReferenceEquals(state_.curve, curve)) return;
                }

                rawTransformCurveOriginalStates.Add(new RawTransformCurveEditState() { curve = curve, state = curve.State });
            }
            public void SetOriginalRawPropertyCurveState(PropertyCurve curve)
            {
                if (curve == null) return;
                
                if (rawPropertyCurveOriginalStates == null) rawPropertyCurveOriginalStates = new List<RawPropertyCurveEditState>();          

                for (int a = 0; a < rawPropertyCurveOriginalStates.Count; a++)
                {
                    var state_ = rawPropertyCurveOriginalStates[a];
                    if (ReferenceEquals(state_.curve, curve)) return;
                }

                rawPropertyCurveOriginalStates.Add(new RawPropertyCurveEditState() { curve = curve, state = curve.State });
            }

            private List<RawTransformLinearCurveEditState> rawTransformLinearCurveOriginalStates;
            private List<RawPropertyLinearCurveEditState> rawPropertyLinearCurveOriginalStates;

            public void SetOriginalRawTransformLinearCurveState(TransformLinearCurve curve)
            {
                if (curve == null) return;

                if (rawTransformLinearCurveOriginalStates == null) rawTransformLinearCurveOriginalStates = new List<RawTransformLinearCurveEditState>();

                for (int a = 0; a < rawTransformLinearCurveOriginalStates.Count; a++)
                {
                    var state_ = rawTransformLinearCurveOriginalStates[a];
                    if (ReferenceEquals(state_.curve, curve)) return;
                }

                rawTransformLinearCurveOriginalStates.Add(new RawTransformLinearCurveEditState() { curve = curve, state = curve.State });
            }
            public void SetOriginalRawPropertyLinearCurveState(PropertyLinearCurve curve)
            {
                if (curve == null) return;

                if (rawPropertyLinearCurveOriginalStates == null) rawPropertyLinearCurveOriginalStates = new List<RawPropertyLinearCurveEditState>();

                for (int a = 0; a < rawPropertyLinearCurveOriginalStates.Count; a++)
                {
                    var state_ = rawPropertyLinearCurveOriginalStates[a];
                    if (ReferenceEquals(state_.curve, curve)) return;
                }

                rawPropertyLinearCurveOriginalStates.Add(new RawPropertyLinearCurveEditState() { curve = curve, state = curve.State });
            }

            private List<TransformCurveEditState> transformCurveMainOriginalStates;
            private List<TransformCurveEditState> transformCurveBaseOriginalStates;

            private List<PropertyCurveEditState> propertyCurveMainOriginalStates;
            private List<PropertyCurveEditState> propertyCurveBaseOriginalStates;

            private List<TransformLinearCurveEditState> transformLinearCurveMainOriginalStates;
            private List<TransformLinearCurveEditState> transformLinearCurveBaseOriginalStates;

            private List<PropertyLinearCurveEditState> propertyLinearCurveMainOriginalStates;
            private List<PropertyLinearCurveEditState> propertyLinearCurveBaseOriginalStates;

            public void SetOriginalTransformCurveState(TransformCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<TransformCurveEditState> list; 
                if (isBaseCurve)
                {
                    if (transformCurveBaseOriginalStates == null) transformCurveBaseOriginalStates = new List<TransformCurveEditState>();
                    list = transformCurveBaseOriginalStates;
                }
                else
                {
                    if (transformCurveMainOriginalStates == null) transformCurveMainOriginalStates = new List<TransformCurveEditState>();
                    list = transformCurveMainOriginalStates;
                }

                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.transformName == curve.TransformName) return;
                }
                
                list.Add(new TransformCurveEditState() { transformName = curve.TransformName, state = curve.State });
            }
            public void SetOriginalPropertyCurveState(PropertyCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<PropertyCurveEditState> list;
                if (isBaseCurve)
                {
                    if (propertyCurveBaseOriginalStates == null) propertyCurveBaseOriginalStates = new List<PropertyCurveEditState>();
                    list = propertyCurveBaseOriginalStates;
                }
                else
                {
                    if (propertyCurveMainOriginalStates == null) propertyCurveMainOriginalStates = new List<PropertyCurveEditState>();
                    list = propertyCurveMainOriginalStates;
                }

                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.propertyName == curve.PropertyString) return;
                }
                
                list.Add(new PropertyCurveEditState() { propertyName = curve.PropertyString, state = curve.State });
            }

            public void SetOriginalTransformLinearCurveState(TransformLinearCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<TransformLinearCurveEditState> list;
                if (isBaseCurve)
                {
                    if (transformLinearCurveBaseOriginalStates == null) transformLinearCurveBaseOriginalStates = new List<TransformLinearCurveEditState>();
                    list = transformLinearCurveBaseOriginalStates;
                }
                else
                {
                    if (transformCurveMainOriginalStates == null) transformLinearCurveMainOriginalStates = new List<TransformLinearCurveEditState>();
                    list = transformLinearCurveMainOriginalStates;
                }

                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.transformName == curve.TransformName) return;
                }
                
                list.Add(new TransformLinearCurveEditState() { transformName = curve.TransformName, state = curve.State });
            }
            public void SetOriginalPropertyLinearCurveState(PropertyLinearCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<PropertyLinearCurveEditState> list;
                if (isBaseCurve)
                {
                    if (propertyLinearCurveBaseOriginalStates == null) propertyLinearCurveBaseOriginalStates = new List<PropertyLinearCurveEditState>();
                    list = propertyLinearCurveBaseOriginalStates;
                }
                else
                {
                    if (propertyLinearCurveMainOriginalStates == null) propertyLinearCurveMainOriginalStates = new List<PropertyLinearCurveEditState>();
                    list = propertyLinearCurveMainOriginalStates;
                }

                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.propertyName == curve.PropertyString) return;
                }

                list.Add(new PropertyLinearCurveEditState() { propertyName = curve.PropertyString, state = curve.State });
            }

            #endregion

            #region New States

            private List<PoseTransformState> newPose;

            public void RecordPoseTransformState(Transform transform)
            {
                if (transform == null) return;

                if (newPose == null) newPose = new List<PoseTransformState>();

                var state = new PoseTransformState() { transform = transform, state = new TransformState(transform, false) };

                bool flag = true;
                for (int a = 0; a < newPose.Count; a++)
                {
                    var state_ = newPose[a];
                    if (state_.transform == state.transform)
                    {
                        newPose[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) newPose.Add(state); 
            }

            private CustomAnimation.Event[] editedEvents;
            public void SetEditedEvents(CustomAnimation.Event[] events)
            {
                editedEvents = events;
            }
            private List<AnimationEventEditState> eventEditStates;
            public void RecordEventStateEdit(CustomAnimation.Event event_)
            {
                if (event_ == null) return;

                if (eventEditStates == null) eventEditStates = new List<AnimationEventEditState>();

                var state = new AnimationEventEditState() { instance = event_, state = event_.State };

                bool flag = true;
                for (int a = 0; a < eventEditStates.Count; a++)
                {
                    var state_ = eventEditStates[a];
                    if (ReferenceEquals(state_.instance, event_) || state_.instance.CachedId == event_.CachedId)
                    {
                        eventEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) eventEditStates.Add(state);
            }

            private List<RawCurveEditState> rawCurveEditStates;

            public void RecordRawCurveEdit(EditableAnimationCurve curve)
            {
                if (curve == null) return;

                if (rawCurveEditStates == null) rawCurveEditStates = new List<RawCurveEditState>();

                var state = new RawCurveEditState() { curve = curve, state = curve.State };

                bool flag = true;
                for (int a = 0; a < rawCurveEditStates.Count; a++)
                {
                    var state_ = rawCurveEditStates[a];
                    if (ReferenceEquals(state_.curve, curve))
                    {
                        rawCurveEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) rawCurveEditStates.Add(state);
            }

            private List<RawTransformCurveEditState> rawTransformCurveEditStates;
            private List<RawPropertyCurveEditState> rawPropertyCurveEditStates; 

            public void RecordRawTransformCurveEdit(TransformCurve curve)
            {
                if (curve == null) return;

                if (rawTransformCurveEditStates == null) rawTransformCurveEditStates = new List<RawTransformCurveEditState>();

                var state = new RawTransformCurveEditState() { curve = curve, state = curve.State };

                bool flag = true;
                for (int a = 0; a < rawTransformCurveEditStates.Count; a++)
                {
                    var state_ = rawTransformCurveEditStates[a];
                    if (ReferenceEquals(state_.curve, curve))
                    {
                        rawTransformCurveEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) rawTransformCurveEditStates.Add(state);
            }
            public void RecordRawPropertyCurveEdit(PropertyCurve curve)
            {
                if (curve == null) return;

                if (rawPropertyCurveEditStates == null) rawPropertyCurveEditStates = new List<RawPropertyCurveEditState>();

                var state = new RawPropertyCurveEditState() { curve = curve, state = curve.State };

                bool flag = true;
                for (int a = 0; a < rawPropertyCurveEditStates.Count; a++)
                {
                    var state_ = rawPropertyCurveEditStates[a];
                    if (ReferenceEquals(state_.curve, curve))
                    {
                        rawPropertyCurveEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) rawPropertyCurveEditStates.Add(state);
            }

            private List<RawTransformLinearCurveEditState> rawTransformLinearCurveEditStates;
            private List<RawPropertyLinearCurveEditState> rawPropertyLinearCurveEditStates;

            public void RecordRawTransformLinearCurveEdit(TransformLinearCurve curve)
            {
                if (curve == null) return;

                if (rawTransformLinearCurveEditStates == null) rawTransformLinearCurveEditStates = new List<RawTransformLinearCurveEditState>();

                var state = new RawTransformLinearCurveEditState() { curve = curve, state = curve.State };

                bool flag = true;
                for (int a = 0; a < rawTransformLinearCurveEditStates.Count; a++)
                {
                    var state_ = rawTransformLinearCurveEditStates[a];
                    if (ReferenceEquals(state_.curve, curve))
                    {
                        rawTransformLinearCurveEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) rawTransformLinearCurveEditStates.Add(state);
            }
            public void RecordRawPropertyLinearCurveEdit(PropertyLinearCurve curve)
            {
                if (curve == null) return;

                if (rawPropertyLinearCurveEditStates == null) rawPropertyLinearCurveEditStates = new List<RawPropertyLinearCurveEditState>();

                var state = new RawPropertyLinearCurveEditState() { curve = curve, state = curve.State };

                bool flag = true;
                for (int a = 0; a < rawPropertyLinearCurveEditStates.Count; a++)
                {
                    var state_ = rawPropertyLinearCurveEditStates[a];
                    if (ReferenceEquals(state_.curve, curve))
                    {
                        rawPropertyLinearCurveEditStates[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) rawPropertyLinearCurveEditStates.Add(state);
            }

            private List<TransformCurveEditState> transformCurveMainEditStates;
            private List<TransformCurveEditState> transformCurveBaseEditStates;

            private List<PropertyCurveEditState> propertyCurveMainEditStates;
            private List<PropertyCurveEditState> propertyCurveBaseEditStates;

            private List<TransformLinearCurveEditState> transformLinearCurveMainEditStates;
            private List<TransformLinearCurveEditState> transformLinearCurveBaseEditStates;

            private List<PropertyLinearCurveEditState> propertyLinearCurveMainEditStates;
            private List<PropertyLinearCurveEditState> propertyLinearCurveBaseEditStates;

            public void RecordTransformCurveEdit(TransformCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<TransformCurveEditState> list;
                if (isBaseCurve)
                {
                    if (transformCurveBaseEditStates == null) transformCurveBaseEditStates = new List<TransformCurveEditState>();
                    list = transformCurveBaseEditStates;
                }
                else
                {
                    if (transformCurveMainEditStates == null) transformCurveMainEditStates = new List<TransformCurveEditState>();
                    list = transformCurveMainEditStates;
                }

                var state = new TransformCurveEditState() { transformName = curve.TransformName, state = curve.State };  

                bool flag = true;
                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.transformName == state.transformName)
                    {
                        list[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) list.Add(state);
            }
            public void RecordPropertyCurveEdit(PropertyCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<PropertyCurveEditState> list;
                if (isBaseCurve)
                {
                    if (propertyCurveBaseEditStates == null) propertyCurveBaseEditStates = new List<PropertyCurveEditState>();
                    list = propertyCurveBaseEditStates;
                }
                else
                {
                    if (propertyCurveMainEditStates == null) propertyCurveMainEditStates = new List<PropertyCurveEditState>();
                    list = propertyCurveMainEditStates;
                }

                var state = new PropertyCurveEditState() { propertyName = curve.PropertyString, state = curve.State };

                bool flag = true;
                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.propertyName == state.propertyName)
                    {
                        list[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) list.Add(state);
            }

            public void RecordTransformLinearCurveEdit(TransformLinearCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<TransformLinearCurveEditState> list;
                if (isBaseCurve)
                {
                    if (transformLinearCurveBaseEditStates == null) transformLinearCurveBaseEditStates = new List<TransformLinearCurveEditState>();
                    list = transformLinearCurveBaseEditStates;
                }
                else
                {
                    if (transformCurveMainEditStates == null) transformLinearCurveMainEditStates = new List<TransformLinearCurveEditState>();
                    list = transformLinearCurveMainEditStates;
                }

                var state = new TransformLinearCurveEditState() { transformName = curve.TransformName, state = curve.State };

                bool flag = true;
                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.transformName == state.transformName)
                    {
                        list[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) list.Add(state);
            }
            public void RecordPropertyLinearCurveEdit(PropertyLinearCurve curve, bool isBaseCurve)
            {
                if (curve == null) return;

                List<PropertyLinearCurveEditState> list;
                if (isBaseCurve)
                {
                    if (propertyLinearCurveBaseEditStates == null) propertyLinearCurveBaseEditStates = new List<PropertyLinearCurveEditState>();
                    list = propertyLinearCurveBaseEditStates;
                }
                else
                {
                    if (propertyLinearCurveMainEditStates == null) propertyLinearCurveMainEditStates = new List<PropertyLinearCurveEditState>();
                    list = propertyLinearCurveMainEditStates;
                }

                var state = new PropertyLinearCurveEditState() { propertyName = curve.PropertyString, state = curve.State };

                bool flag = true;
                for (int a = 0; a < list.Count; a++)
                {
                    var state_ = list[a];
                    if (state_.propertyName == state.propertyName)
                    {
                        list[a] = state;
                        flag = false;
                        break;
                    }
                }

                if (flag) list.Add(state);
            }

            #endregion

            public UndoableEditAnimationSourceData(AnimationEditor editor, ImportedAnimatable animatable, AnimationSource source)
            {
                this.editor = editor;
                this.animatable = animatable;
                this.source = source;

                this.originalPlaybackPosition = this.playbackPosition = editor.PlaybackPosition;
                
                undoState = false; 
            }

            public void Reapply()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                editor.PlaybackPosition = playbackPosition;

                editor.nextRecordStepDelay = 2;
                if (newPose != null)
                {
                    foreach (var p in newPose) if (p.transform != null) p.state.Apply(p.transform, false);
                }

                if (source != null && source.rawAnimation != null)
                {
                    if (editedEvents != null)
                    {
                        //if (editor.animationEventsWindow != null) editor.animationEventsWindow.gameObject.SetActive(false);
                        //if (editor.animationEventEditWindow != null) editor.animationEventEditWindow.gameObject.SetActive(false);

                        source.rawAnimation.events = editedEvents;
                    }
                    if (eventEditStates != null)
                    {
                        //if (editor.animationEventsWindow != null) editor.animationEventsWindow.gameObject.SetActive(false);
                        //if (editor.animationEventEditWindow != null) editor.animationEventEditWindow.gameObject.SetActive(false);

                        foreach (var s in eventEditStates)
                        {
                            if (s.instance != null) s.instance.SetStateForThisAndOriginals(s.state); 
                        }
                    }

                    if (rawCurveEditStates != null)
                    {
                        foreach (var s in rawCurveEditStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (rawTransformCurveEditStates != null)
                    {
                        foreach (var s in rawTransformCurveEditStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }
                    if (rawPropertyCurveEditStates != null)
                    {
                        foreach (var s in rawPropertyCurveEditStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (rawTransformLinearCurveEditStates != null)
                    {
                        foreach (var s in rawTransformLinearCurveEditStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }
                    if (rawPropertyLinearCurveEditStates != null)
                    {
                        foreach (var s in rawPropertyLinearCurveEditStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (transformCurveMainEditStates != null)
                    {
                        foreach (var s in transformCurveMainEditStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is TransformCurve tc) tc.State = s.state;
                            }
                        }
                    }
                    if (transformCurveBaseEditStates != null)
                    {
                        foreach (var s in transformCurveBaseEditStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is TransformCurve tc) tc.State = s.state; 
                            }
                        }
                    }
                    if (transformLinearCurveMainEditStates != null)
                    {
                        foreach (var s in transformLinearCurveMainEditStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is TransformLinearCurve tc) tc.State = s.state;
                            }
                        }
                    }
                    if (transformLinearCurveBaseEditStates != null)
                    {
                        foreach (var s in transformLinearCurveBaseEditStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is TransformLinearCurve tc) tc.State = s.state;
                            }
                        }
                    }

                    if (propertyCurveMainEditStates != null)
                    {
                        foreach (var s in propertyCurveMainEditStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is PropertyCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyCurveBaseEditStates != null)
                    {
                        foreach (var s in propertyCurveBaseEditStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is PropertyCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyLinearCurveMainEditStates != null)
                    {
                        foreach (var s in propertyLinearCurveMainEditStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is PropertyLinearCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyLinearCurveBaseEditStates != null)
                    {
                        foreach (var s in propertyLinearCurveBaseEditStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is PropertyLinearCurve pc) pc.State = s.state;
                            }
                        }
                    }
                }

                if (animatable != null && source != null) animatable.SetCurrentlyEditedSource(editor, source.index, false);

                editor.RefreshIKControllerActivation(true);
                editor.ForceApplyTransformStateChanges(3);
                
                editor.RefreshTimeline();
            }

            public void Revert()
            {
                var sesh = editor.CurrentSession;
                if (sesh == null) return;

                editor.PlaybackPosition = originalPlaybackPosition;

                editor.nextRecordStepDelay = 2;
                if (originalPose != null)
                {
                    foreach (var p in originalPose) if (p.transform != null) p.state.Apply(p.transform, false);
                }

                if (source != null && source.rawAnimation != null)
                {
                    if (originalEvents != null)
                    {
                        //if (editor.animationEventsWindow != null) editor.animationEventsWindow.gameObject.SetActive(false); 
                        //if (editor.animationEventEditWindow != null) editor.animationEventEditWindow.gameObject.SetActive(false);

                        source.rawAnimation.events = originalEvents;
                    }
                    if (eventOriginalStates != null)
                    {
                        //if (editor.animationEventsWindow != null) editor.animationEventsWindow.gameObject.SetActive(false);
                        //if (editor.animationEventEditWindow != null) editor.animationEventEditWindow.gameObject.SetActive(false);

                        foreach (var s in eventOriginalStates)
                        {
                            if (s.instance != null) s.instance.SetStateForThisAndOriginals(s.state);
                        }
                    }

                    if (rawCurveOriginalStates != null)
                    {
                        foreach (var s in rawCurveOriginalStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (rawTransformCurveOriginalStates != null)
                    {
                        foreach (var s in rawTransformCurveOriginalStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }
                    if (rawPropertyCurveOriginalStates != null)
                    {
                        foreach (var s in rawPropertyCurveOriginalStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (rawTransformLinearCurveOriginalStates != null)
                    {
                        foreach (var s in rawTransformLinearCurveOriginalStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }
                    if (rawPropertyLinearCurveOriginalStates != null)
                    {
                        foreach (var s in rawPropertyLinearCurveOriginalStates)
                        {
                            if (s.curve != null) s.curve.State = s.state;
                        }
                    }

                    if (transformCurveMainOriginalStates != null)
                    {
                        foreach (var s in transformCurveMainOriginalStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is TransformCurve tc) tc.State = s.state;
                            }
                        }
                    }
                    if (transformCurveBaseOriginalStates != null)
                    {
                        foreach (var s in transformCurveBaseOriginalStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is TransformCurve tc) tc.State = s.state;
                            }
                        }
                    }
                    if (transformLinearCurveMainOriginalStates != null)
                    {
                        foreach (var s in transformLinearCurveMainOriginalStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is TransformLinearCurve tc) tc.State = s.state;
                            }
                        }
                    }
                    if (transformLinearCurveBaseOriginalStates != null)
                    {
                        foreach (var s in transformLinearCurveBaseOriginalStates)
                        {
                            if (source.rawAnimation.TryGetTransformCurves(s.transformName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is TransformLinearCurve tc) tc.State = s.state;
                            }
                        }
                    }

                    if (propertyCurveMainOriginalStates != null)
                    {
                        foreach (var s in propertyCurveMainOriginalStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is PropertyCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyCurveBaseOriginalStates != null)
                    {
                        foreach (var s in propertyCurveBaseOriginalStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is PropertyCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyLinearCurveMainOriginalStates != null)
                    {
                        foreach (var s in propertyLinearCurveMainOriginalStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out var mainCurve, out var _))
                            {
                                if (mainCurve is PropertyLinearCurve pc) pc.State = s.state;
                            }
                        }
                    }
                    if (propertyLinearCurveBaseOriginalStates != null)
                    {
                        foreach (var s in propertyLinearCurveBaseOriginalStates)
                        {
                            if (source.rawAnimation.TryGetPropertyCurves(s.propertyName, out _, out _, out _, out var baseCurve))
                            {
                                if (baseCurve is PropertyLinearCurve pc) pc.State = s.state; 
                            }
                        }
                    }
                }

                if (animatable != null && source != null) animatable.SetCurrentlyEditedSource(editor, source.index, false);

                editor.RefreshIKControllerActivation(true);
                editor.ForceApplyTransformStateChanges(3);

                editor.RefreshTimeline();
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

        private UndoableEditAnimationSourceData animationEditRecord;
        public UndoableEditAnimationSourceData BeginNewAnimationEditRecord()
        {
            if (animationEditRecord != null) CommitAnimationEditRecord();

            animationEditRecord = BeginNewAnimationEditRecord(ActiveAnimatable, CurrentSource);
            return animationEditRecord;
        }
        public UndoableEditAnimationSourceData BeginNewAnimationEditRecord(ImportedAnimatable animatable, AnimationSource animationSource)
        {
            if (animatable == null || animationSource == null) return null;

            return new UndoableEditAnimationSourceData(this, animatable, animationSource);  
        }
        [Tooltip("The current animation edit record, if there is one.")]
        public UndoableEditAnimationSourceData CurrentAnimationEditRecord => animationEditRecord;

        [Tooltip("Refers to the current animation edit record, or creates a new one if it doesn't exist.")]
        public UndoableEditAnimationSourceData AnimationEditRecord
        {
            get
            {
                if (animationEditRecord == null) BeginNewAnimationEditRecord();
                return animationEditRecord;
            }
        }
        public void CommitAnimationEditRecord()
        {
            CommitAnimationEditRecord(animationEditRecord);
            animationEditRecord = null;
        }
        public void CommitAnimationEditRecord(UndoableEditAnimationSourceData editRecord)
        {
            if (editRecord == null) return;

            editRecord.playbackPosition = PlaybackPosition;
            RecordRevertibleAction(editRecord);
        }
        public void DiscardAnimationEditRecord()
        {
            animationEditRecord = null;
        }

        public struct UndoableSetPlaybackPosition : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;

            public int animatableIndex;
            public int sourceIndex;

            public float previousPlaybackPosition;
            public float newPlaybackPosition;

            public UndoableSetPlaybackPosition(AnimationEditor editor, int animatableIndex, int sourceIndex, float previousPlaybackPosition, float newPlaybackPosition)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;
                this.sourceIndex = sourceIndex;

                this.previousPlaybackPosition = previousPlaybackPosition;
                this.newPlaybackPosition = newPlaybackPosition; 

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                if (editor.IsPlayingEntirety) editor.Stop(false);  
                editor.SetActiveObject(animatableIndex, true, true, false);
                var activeObj = editor.ActiveAnimatable;
                if (activeObj != null)
                {
                    activeObj.SetCurrentlyEditedSource(editor, sourceIndex, false);
                    editor.PlaySnapshotUnclamped(newPlaybackPosition, 0, false);
                }
            }

            public void Revert()
            {
                if (editor == null) return;

                if (editor.IsPlayingEntirety) editor.Stop(false);
                editor.SetActiveObject(animatableIndex, true, true, false); 
                var activeObj = editor.ActiveAnimatable;
                if (activeObj != null)
                {
                    activeObj.SetCurrentlyEditedSource(editor, sourceIndex, false); 
                    editor.PlaySnapshotUnclamped(previousPlaybackPosition, 0, false);
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

        #endregion

        [Serializable]
        public enum EditMode
        {
            GLOBAL_TIME, GLOBAL_KEYS, SELECTED_BONE_KEYS, BONE_KEYS, BONE_CURVES, PROPERTY_CURVES, ANIMATION_EVENTS
        }

        [Header("Runtime"), SerializeField]
        protected EditMode editMode;
        public struct UndoableSetEditMode : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public EditMode prevEditMode, editMode;

            public UndoableSetEditMode(AnimationEditor editor, EditMode prevEditMode, EditMode editMode)
            {
                this.editor = editor;
                this.prevEditMode = prevEditMode;
                this.editMode = editMode;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                editor.SetEditMode(editMode, false);
            }

            public void Revert()
            {
                if (editor == null) return;

                editor.SetEditMode(prevEditMode, false);
            }

            public void Perpetuate() { }
            public void PerpetuateUndo()
            {
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public void SetEditMode(EditMode mode) => SetEditMode(mode, true);
        public void SetEditMode(EditMode mode, bool undoable)
        {
            undoable = false; // no reason for the ability to undo setting the edit mode at the moment

            var prevEditMode = editMode;
            editMode = mode;
            switch (editMode)
            {
                case EditMode.GLOBAL_TIME:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.GLOBAL_KEYS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.SELECTED_BONE_KEYS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(true);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.BONE_KEYS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(true);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(true);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.BONE_CURVES:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(true);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(true);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.PROPERTY_CURVES:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(true);
                    break;

                case EditMode.ANIMATION_EVENTS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;
            }

            RefreshTimeline();

            if (undoable) RecordRevertibleAction(new UndoableSetEditMode(this, prevEditMode, editMode));
        }
        public void SetEditMode(int mode) => SetEditMode((EditMode)mode);

        #region Mirroring Mode
        [Serializable]
        public enum MirroringMode
        {
            Off, Relative, Absolute
        }
        public MirroringMode MirrorMode
        {
            get => CurrentSession == null ? MirroringMode.Off : CurrentSession.mirroringMode;
            set => SetMirroringMode(value);
        }
        public void SetMirroringMode(MirroringMode mode) 
        {
            if (CurrentSession == null) return;
            CurrentSession.SetMirroringMode(mode);
            if (mirroringWindow != null)
            {
                var dropdown = mirroringWindow.gameObject.GetComponentInChildren<UIDynamicDropdown>();
                if (dropdown != null) dropdown.SetSelectionText(mode.ToString().ToLower());  
            }
        }
        public void SetMirroringMode(int mode) => SetMirroringMode((MirroringMode)mode);
        public void SetMirroringMode(string mode)
        {
            if (Enum.TryParse<MirroringMode>(mode, out var result)) SetMirroringMode(result);
        }
        #endregion

#if BULKOUT_ENV
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "sc_RLD-Add"; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#else
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "";
#endif
        [Tooltip("Primary object used for manipulating the scene.")] 
        public RuntimeEditor runtimeEditor;

        public void RefreshRootObjects()
        {
            if (runtimeEditor == null) return;

            _tempGameObjects.Clear();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(_tempGameObjects);
            runtimeEditor.SetExternalRootObjects(_tempGameObjects);
            _tempGameObjects.Clear();
        }

        [Tooltip("The UI element used to display the list of imported animatables and their animations.")]
        public UICategorizedList importedAnimatablesList;

        public SceneSwap sceneSwapper; 
        public void QuitToPreviousScene(int fallbackSceneId)
        {
            void Swap()
            {
                sceneSwapper.SwapToPreviousOrDefault(fallbackSceneId);
            }
            void SaveSwap()
            {
                SaveSessionInDefaultDirectory(CurrentSession);  
                Swap();
            }

            if (CurrentSession != null && CurrentSession.IsDirty)
            {
                ShowPopupYesNoCancel("UNSAVED CHANGES", $"You have unsaved changes. Do you want to save them before leaving?", SaveSwap, Swap, null); 
            }
            else Swap();
        }

        [Header("Windows")]
        public AnimationTimeline timelineWindow;
        public RectTransform animatableImporterWindow;
        public RectTransform newAnimationWindow;
        public RectTransform browseAnimationsWindow;
        public RectTransform compilationWindow;
        public RectTransform curveEditorWindow;
        protected AnimationCurveEditor curveEditor;
        public AnimationCurveEditor CurveEditor
        {
            get
            {
                if (curveEditor == null && curveEditorWindow != null) curveEditor = curveEditorWindow.GetComponentInChildren<AnimationCurveEditor>(true);
                return curveEditor;
            }
        }
        public void RedrawAllCurves()
        {
            if (curveRenderer != null && curveRenderer.gameObject.activeSelf) RefreshCurveRenderer(true);
            if (curveEditorWindow != null && curveEditorWindow.gameObject.activeSelf && curveEditor != null) curveEditor.Redraw();         
        }
        public RectTransform physiqueEditorWindow;
        protected PhysiqueEditorWindow physiqueEditor;
        public PhysiqueEditorWindow PhysiqueEditor
        {
            get
            {
                if (physiqueEditor == null && physiqueEditorWindow != null) physiqueEditor = physiqueEditorWindow.GetComponentInChildren<PhysiqueEditorWindow>(true);
                return physiqueEditor;
            }
        }
        public void OpenPhysiqueEditor()
        {
            if (physiqueEditorWindow == null) return;
            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            physiqueEditorWindow.gameObject.SetActive(true);
            physiqueEditorWindow.SetAsLastSibling();

            if (activeObj.muscleController == null)
            {
                physiqueEditorWindow.gameObject.SetActive(false);
            }
            else
            {
                var editor = PhysiqueEditor;
                if (editor != null) editor.Character = activeObj.muscleController;              
            }

        }

        public RectTransform animationSettingsWindow;
        protected const string str_sampleRate = "SampleRate";
        protected const string str_baseData = "BaseData";
        protected const string str_default = "Default";
        protected const string str_target = "Target";
        protected const string str_targetSnapshot = "TargetSnapshot";
        protected const string str_time = "Time";
        protected const string str_currentText1 = "Current-Text";
        public void OpenAnimationSettingsWindow() => OpenAnimationSettingsWindow(animationSettingsWindow);
        public void OpenAnimationSettingsWindow(AnimationSource source) => OpenAnimationSettingsWindow(animationSettingsWindow, source);
        public void OpenAnimationSettingsWindow(RectTransform animationSettingsWindow) => OpenAnimationSettingsWindow(animationSettingsWindow, CurrentSource); 
        public void OpenAnimationSettingsWindow(RectTransform animationSettingsWindow, AnimationSource source)
        {
            if (animationSettingsWindow == null) return;

            animationSettingsWindow.gameObject.SetActive(source != null);
            if (source == null) return;
            animationSettingsWindow.SetAsLastSibling();

            RefreshAnimationSettingsWindow(animationSettingsWindow, source);
        }

        public struct UndoableSetAnimationSourceName : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public AnimationSource source;
            public string prevName, newName;

            public UndoableSetAnimationSourceName(AnimationEditor editor, AnimationSource source, string prevName, string newName)
            {
                this.editor = editor;
                this.source = source;
                this.prevName = prevName;
                this.newName = newName;
                undoState = false;
            }

            public void Reapply()
            {
                if (source == null) return;
                source.DisplayName = newName;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
            }

            public void Revert()
            {
                if (source == null) return;
                source.DisplayName = prevName;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
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
        public struct UndoableSetAnimationSourceSampleRate : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public AnimationSource source;
            public int prevSampleRate, sampleRate;

            public UndoableSetAnimationSourceSampleRate(AnimationEditor editor, AnimationSource source, int prevSampleRate, int sampleRate)
            {
                this.editor = editor;
                this.source = source;
                this.prevSampleRate = prevSampleRate;
                this.sampleRate = sampleRate;
                undoState = false;
            }

            public void Reapply()
            {
                if (source == null) return;
                source.SampleRate = sampleRate;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
            }

            public void Revert()
            {
                if (source == null) return;
                source.SampleRate = prevSampleRate;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
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
        public struct UndoableSetBaseData : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public AnimationSource source;
            public AnimationSource.BaseDataType prevDataType, newDataType;
            public AnimationSource prevBaseDataTarget, newBaseDataTarget;
            public float prevBaseDataReferenceTime, newBaseDataReferenceTime;

            public UndoableSetBaseData(AnimationEditor editor, AnimationSource source, AnimationSource.BaseDataType prevDataType, AnimationSource.BaseDataType newDataType, AnimationSource prevBaseDataTarget, AnimationSource newBaseDataTarget, float prevBaseDataReferenceTime, float newBaseDataReferenceTime)
            {
                this.editor = editor;
                this.source = source;
                this.prevDataType = prevDataType;
                this.newDataType = newDataType;
                this.prevBaseDataTarget = prevBaseDataTarget;
                this.newBaseDataTarget = newBaseDataTarget;
                this.prevBaseDataReferenceTime = prevBaseDataReferenceTime;
                this.newBaseDataReferenceTime = newBaseDataReferenceTime;

                undoState = false;
            }

            public void Reapply()
            {
                if (source == null) return;

                source.BaseDataMode = newDataType;
                source.BaseDataReferenceTime = newBaseDataReferenceTime;
                source.BaseDataSource = newBaseDataTarget;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
            }

            public void Revert()
            {
                if (source == null) return;

                source.BaseDataMode = prevDataType;
                source.BaseDataReferenceTime = prevBaseDataReferenceTime;
                source.BaseDataSource = prevBaseDataTarget;
                if (editor != null) editor.OpenAnimationSettingsWindow(source);
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
        public void RefreshAnimationSettingsWindow(AnimationSource source) => RefreshAnimationSettingsWindow(animationSettingsWindow, source); 
        public void RefreshAnimationSettingsWindow(RectTransform animationSettingsWindow, AnimationSource source)
        {
            if (animationSettingsWindow == null || source == null || !animationSettingsWindow.gameObject.activeSelf) return;

            var animatable = ActiveAnimatable;
            if (animatable == null) return;

            CustomEditorUtils.SetInputFieldTextByName(animationSettingsWindow, _nameTag, source.DisplayName);
            CustomEditorUtils.SetInputFieldTextByName(animationSettingsWindow, str_sampleRate, (source.rawAnimation == null ? CustomAnimation.DefaultJobCurveSampleRate : source.rawAnimation.jobCurveSampleRate).ToString());

            CustomEditorUtils.SetInputFieldOnEndEditActionByName(animationSettingsWindow, _nameTag, (string name) =>
            {
                string prevName = source.DisplayName;
                source.DisplayName = name;
                RecordRevertibleAction(new UndoableSetAnimationSourceName(this, source, prevName, source.DisplayName));
            });
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(animationSettingsWindow, str_sampleRate, (string value) =>
            {
                if (source.rawAnimation != null && int.TryParse(value, out var valInt))
                {
                    int prevSampleRate = source.SampleRate;
                    source.SampleRate = valInt;
                    RecordRevertibleAction(new UndoableSetAnimationSourceSampleRate(this, source, prevSampleRate, source.SampleRate));
                }
            });

            var baseData = animationSettingsWindow.FindDeepChildLiberal(str_baseData);
            if (baseData != null)
            {
                var modeDropdown = baseData.FindDeepChildLiberal(str_mode);
                if (modeDropdown != null)
                {

                    var target = baseData.FindDeepChildLiberal(str_target);
                    var time = baseData.FindDeepChildLiberal(str_time); 

                    void SetSourceBaseDataMode(AnimationSource.BaseDataType mode, bool undoable)
                    {
                        var undo = new UndoableSetBaseData(this, source, source.BaseDataMode, mode, source.BaseDataSource, null, source.BaseDataReferenceTime, 0);

                        source.BaseDataMode = mode;
                        CustomEditorUtils.SetComponentTextByName(modeDropdown, str_currentText1, source.BaseDataMode == AnimationSource.BaseDataType.AnimationSource ? "target" : source.BaseDataMode == AnimationSource.BaseDataType.AnimationSourceSnapshot ? "target_snapshot" : "default");
                        
                        if (target != null)
                        { 
                            CustomEditorUtils.SetInputFieldText(target, source.BaseDataSource == null ? string.Empty : source.BaseDataSource.DisplayName, true, true);   
                            CustomEditorUtils.SetInputFieldOnEndEditAction(target, (string val) =>
                            {
                                var undo = new UndoableSetBaseData(this, source, source.BaseDataMode, mode, source.BaseDataSource, null, source.BaseDataReferenceTime, 0);

                                if (string.IsNullOrEmpty(val))
                                {
                                    source.BaseDataSource = null;
                                } 
                                else if (animatable.animationBank != null)
                                {
                                    val = val.Trim();
                                    bool flag = true;
                                    foreach (var source_ in animatable.animationBank)
                                    {
                                        if (source_ == null || source_.DisplayName.Trim() != val) continue;

                                        source.BaseDataSource = source_; 
                                        flag = false;
                                        break;
                                    }

                                    if (flag) source.BaseDataSource = null;
                                }

                                undo.newDataType = source.BaseDataMode;
                                undo.newBaseDataTarget = source.BaseDataSource;
                                undo.newBaseDataReferenceTime = source.BaseDataReferenceTime;
                                RecordRevertibleAction(undo);
                            });
                            target.gameObject.SetActive(mode == AnimationSource.BaseDataType.AnimationSource || mode == AnimationSource.BaseDataType.AnimationSourceSnapshot);
                        }

                        if (time != null)
                        { 
                            CustomEditorUtils.SetInputFieldText(time, source.BaseDataReferenceTime.ToString(), true, true); 
                            CustomEditorUtils.SetInputFieldOnEndEditAction(time, (string val) => 
                            {
                                if (float.TryParse(val, out float timeVal)) 
                                {
                                    var undo = new UndoableSetBaseData(this, source, source.BaseDataMode, mode, source.BaseDataSource, null, source.BaseDataReferenceTime, 0);

                                    source.BaseDataReferenceTime = timeVal;

                                    undo.newDataType = source.BaseDataMode;
                                    undo.newBaseDataTarget = source.BaseDataSource;
                                    undo.newBaseDataReferenceTime = source.BaseDataReferenceTime;
                                    RecordRevertibleAction(undo);
                                }
                            });
                            time.gameObject.SetActive(mode == AnimationSource.BaseDataType.AnimationSourceSnapshot);
                        }

                        if (undoable)
                        {
                            undo.newDataType = source.BaseDataMode;
                            undo.newBaseDataTarget = source.BaseDataSource;
                            undo.newBaseDataReferenceTime = source.BaseDataReferenceTime;
                            RecordRevertibleAction(undo);
                        }
                    }

                    SetSourceBaseDataMode(source.BaseDataMode, false); 

                    CustomEditorUtils.SetButtonOnClickActionByName(modeDropdown, str_default, () => SetSourceBaseDataMode(AnimationSource.BaseDataType.Default, true), true, true, false); 
                    CustomEditorUtils.SetButtonOnClickActionByName(modeDropdown, str_target, () => SetSourceBaseDataMode(AnimationSource.BaseDataType.AnimationSource, true), true, true, false);
                    CustomEditorUtils.SetButtonOnClickActionByName(modeDropdown, str_targetSnapshot, () => SetSourceBaseDataMode(AnimationSource.BaseDataType.AnimationSourceSnapshot, true), true, true, false); 
                }
            }

        }

        public GameObject promptMessageYesNo;
        public GameObject promptMessageYesNoCancel;
        public RectTransform confirmationMessageWindow; 

        protected const string _messageTag = "Message";
        protected const string _confirmTag = "Confirm";
        protected const string _cancelTag = "Cancel";
        protected const string _closeTag = "Close";
        private const string _actionNameTag = "ActionName";
        private const string _yesTag = "Yes";
        private const string _noTag = "No";

        public bool ShowPopupYesNo(string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo) => ShowPopupYesNo(promptMessageYesNo, actionName, message, onYes, onNo);
        public static bool ShowPopupYesNo(GameObject promptMessageYesNo, string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo)
        {
            if (promptMessageYesNo.gameObject.activeSelf) return false;

            CustomEditorUtils.SetComponentTextByName(promptMessageYesNo, _actionNameTag, actionName);
            CustomEditorUtils.SetComponentTextByName(promptMessageYesNo, _messageTag, message);
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, _yesTag, () => { promptMessageYesNo.gameObject.SetActive(false); onYes?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, _noTag, () => { promptMessageYesNo.gameObject.SetActive(false); onNo?.Invoke(); });

            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, _closeTag, () => { promptMessageYesNo.gameObject.SetActive(false); onNo?.Invoke(); });

            promptMessageYesNo.gameObject.SetActive(true);
            promptMessageYesNo.transform.SetAsLastSibling();

            return true;
        }
        public bool ShowPopupYesNoCancel(string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo, VoidParameterlessDelegate onCancel) => ShowPopupYesNoCancel(promptMessageYesNoCancel, actionName, message, onYes, onNo, onCancel);
        public static bool ShowPopupYesNoCancel(GameObject promptMessageYesNoCancel, string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo, VoidParameterlessDelegate onCancel)
        {
            if (promptMessageYesNoCancel.gameObject.activeSelf) return false;

            CustomEditorUtils.SetComponentTextByName(promptMessageYesNoCancel, _actionNameTag, actionName);
            CustomEditorUtils.SetComponentTextByName(promptMessageYesNoCancel, _messageTag, message);
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, _yesTag, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onYes?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, _noTag, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onNo?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, _cancelTag, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onCancel?.Invoke(); });

            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, _closeTag, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onCancel?.Invoke(); });

            promptMessageYesNoCancel.gameObject.SetActive(true);
            promptMessageYesNoCancel.transform.SetAsLastSibling();

            return true;
        }
        public void ShowPopupConfirmation(string message, VoidParameterlessDelegate onConfirm, VoidParameterlessDelegate onCancel = null) => ShowPopupConfirmation(confirmationMessageWindow, message, onConfirm, onCancel);
        public static void ShowPopupConfirmation(RectTransform confirmationMessageWindow, string message, VoidParameterlessDelegate onConfirm, VoidParameterlessDelegate onCancel = null)
        {
            if (confirmationMessageWindow == null) return;

            void Confirm()
            {
                confirmationMessageWindow.gameObject.SetActive(false);
                onConfirm?.Invoke();
            }

            confirmationMessageWindow.gameObject.SetActive(true);

            CustomEditorUtils.SetComponentTextByName(confirmationMessageWindow, _messageTag, message);

            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _confirmTag, Confirm);
            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _cancelTag, () => onCancel?.Invoke());
            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _closeTag, () => onCancel?.Invoke(), false, true, false); 
        }

        #region Package Linking

        public RectTransform packageLinkWindow;
        public Color colorUnbound = Color.red;
        public Color colorNotSynced = Color.yellow;
        public Color colorOutdated = Color.cyan;
        public Color colorSynced = Color.green;

        protected const string _sourceNameTag = "Name";
        protected const string _linkIconTag = "LinkIcon";
        protected const string _infoTag = "Info";
        protected const string _bindButtonTag = "BindButton";
        protected const string _pushButtonTag = "PushButton";
        protected const string _pullButtonTag = "PullButton";

        protected const string _unboundText = "UNBOUND";
        protected const string _localIsNewerText = "UNSYNCED CHANGES";
        protected const string _externalIsNewerText = "LINKED ASSET IS NEWER";
        protected const string _syncedText = "UP TO DATE";
        public void EvaluateAnimationAssetLink(AnimationSource target, CustomAnimation linkedAnim, VoidParameterlessDelegate onSynced, VoidParameterlessDelegate onLocalNewer, VoidParameterlessDelegate onLinkedNewer)
        {
            if (target.rawAnimation == null)
            {
                onLinkedNewer?.Invoke(); 
            }
            else
            {
                int dateComparison = linkedAnim.LastEditDate().CompareTo(target.rawAnimation.LastEditDate());
                if (target.IsDirty)
                {
                    if (dateComparison < 0)
                    {
                        onLocalNewer?.Invoke();
                    }
                    else if (dateComparison > 0)
                    {
                        onLinkedNewer?.Invoke();
                    }
                    else
                    {
                        onLocalNewer?.Invoke();
                    }
                }
                else
                {
                    if (dateComparison > 0)
                    {
                        onLinkedNewer?.Invoke();
                    }
                    else
                    {
                        onSynced?.Invoke();
                    }
                }
            }
        }

        protected VoidParameterlessDelegate syncPackageLinkWindow;
        public void SyncPackageLinkWindow()
        {
            if (syncPackageLinkWindow != null)
            {
                if (packageLinkWindow == null || !packageLinkWindow.gameObject.activeInHierarchy)
                {
                    syncPackageLinkWindow = null;
                } 
                else
                {
                    syncPackageLinkWindow();
                }
            }
        }
        public void RefreshPackageLinksUI()
        {
            SyncPackageLinkWindow();
            RefreshAnimatableListUI(); 
        }
        public void OpenPackageLinkWindow(AnimationSource target)
        {
            if (target == null || packageLinkWindow == null) return; 

            packageLinkWindow.gameObject.SetActive(true);
            packageLinkWindow.SetAsLastSibling();
            CustomEditorUtils.SetInputFieldOnValueChangeAction(packageLinkWindow, null);

            var rT_name = packageLinkWindow.FindDeepChildLiberal(_sourceNameTag);
            var rT_icon = packageLinkWindow.FindDeepChildLiberal(_linkIconTag);

            var rT_info = packageLinkWindow.FindDeepChildLiberal(_infoTag);

            var rT_bindButton = packageLinkWindow.FindDeepChildLiberal(_bindButtonTag);
            var rT_pushButton = packageLinkWindow.FindDeepChildLiberal(_pushButtonTag);
            var rT_pullButton = packageLinkWindow.FindDeepChildLiberal(_pullButtonTag);

            var errorMessage = packageLinkWindow.GetComponentInChildren<UIPopupMessageFadable>(true);
            void ShowError(string message)
            {
                if (errorMessage == null) return;
                errorMessage.SetMessage(message).SetDisplayTime(errorMessage.DefaultDisplayTime).Show(); 
            }

            exportTarget = target;

            VarRef<bool> tempFlag = new VarRef<bool>(true);
            void SetPushPullMode(ContentManager.LocalPackage localPackage)
            {
                tempFlag.value = true;
                CustomEditorUtils.SetInputFieldText(packageLinkWindow, localPackage.GetIdentityString());
                CustomEditorUtils.SetInputFieldOnValueChangeAction(packageLinkWindow, (string value) =>
                {
                    if (ContentManager.TryFindLocalPackage(value, out ContentManager.LocalPackage localPackage) && localPackage.GetIdentity().Equals(target.package))
                    {
                        if (!tempFlag) 
                        {
                            target.package = localPackage.GetIdentity(); 
                            SetPushPullMode(localPackage); 
                        }
                    } 
                    else if (tempFlag)
                    {
                        tempFlag.value = false;
                        SetBindMode();
                    }
                });

                void IsSynced()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _syncedText, colorSynced);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorSynced);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorSynced;
                    }
                }
                void IsNotSynced()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _localIsNewerText, colorNotSynced);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorNotSynced);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorNotSynced;
                    }
                }
                void IsOutdated()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _externalIsNewerText, colorOutdated);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorOutdated);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorOutdated;
                    }
                }
                void CheckSync()
                {
                    if (target == null || !target.IsValid)
                    {
                        if (packageLinkWindow != null) packageLinkWindow.gameObject.SetActive(false);
                        return;
                    }

                    if (!ContentManager.TryFindLocalPackage(target.package, out ContentManager.LocalPackage localPackage)) // Checks for null
                    {
                        CustomEditorUtils.SetInputFieldText(packageLinkWindow, string.Empty); 
                        SetBindMode(); 
                        return; 
                    }

                    if (localPackage.Content.TryFind<CustomAnimation>(out var linkedAnim, target.syncedName))
                    {
                        EvaluateAnimationAssetLink(target, linkedAnim, IsSynced, IsNotSynced, IsOutdated);
                        if (rT_pullButton != null) CustomEditorUtils.SetButtonInteractable(rT_pullButton, true);
                    }
                    else
                    {
                        IsNotSynced();
                        if (rT_pullButton != null) CustomEditorUtils.SetButtonInteractable(rT_pullButton, false);
                    }
                }
                syncPackageLinkWindow = CheckSync;
                CheckSync();

                if (rT_bindButton != null) rT_bindButton.gameObject.SetActive(false);
                if (rT_pushButton != null) 
                {
                    rT_pushButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_pushButton, () =>
                    {
                        PushTargetToPackage(target);
                    });
                }
                if (rT_pullButton != null) 
                {
                    rT_pullButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_pullButton, () =>
                    {
                        void DoIt()
                        {
                            if (PullTargetFromPackage(target))
                            {
                                IsSynced();
                                RefreshTimeline();
                            }
                        }
                        if (target.IsDirty)
                        {
                            ShowPopupConfirmation($"This pull will overwrite your current changes to '{target.DisplayName}' — are you sure?", DoIt);
                        }
                        else 
                        { 
                            DoIt(); 
                        }
                    });
                    CustomEditorUtils.SetButtonInteractable(rT_pullButton, localPackage.Content.Contains(target.syncedName, typeof(CustomAnimation)));
                }
            }
            void SetBindMode()
            {
                if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _unboundText, colorUnbound);
                if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorUnbound);
                if (rT_icon != null)
                {
                    var img = rT_icon.GetComponentInChildren<Image>();
                    if (img != null) img.color = colorUnbound;
                }

                if (rT_bindButton != null)
                {
                    rT_bindButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_bindButton, () =>
                    {
                        string targetPackageID = CustomEditorUtils.GetInputFieldText(packageLinkWindow);
                        var temp = new PackageIdentifier(targetPackageID);
                        var result = BindTargetToPackage(target, new PackageIdentifier(targetPackageID), out ContentManager.LocalPackage package);
                        RefreshAnimatableListUI(); 
                        if (result)
                        {
                            SetPushPullMode(package);
                        } 
                        else
                        {
                            ShowError($"Failed to bind to package '{targetPackageID}'");        
                        }
                    });
                }

                if (rT_pushButton != null) rT_pushButton.gameObject.SetActive(false);
                if (rT_pullButton != null) rT_pullButton.gameObject.SetActive(false);
            }

            if (ContentManager.TryFindLocalPackage(target.package, out ContentManager.LocalPackage localPackage))
            {
                SetPushPullMode(localPackage);
            } 
            else
            {
                CustomEditorUtils.SetInputFieldText(packageLinkWindow, string.Empty);
                SetBindMode(); 
            }
        }

        public void CreateNewProject(GameObject newProjectWindow)
        {

        }

        protected AnimationSource exportTarget;
        protected PackageIdentifier packageTarget;

        public void BindTargetToPackage() => BindTargetToPackage(exportTarget, packageTarget);
        public bool BindTargetToPackage(AnimationSource exportTarget, PackageIdentifier packageTarget) => BindTargetToPackage(exportTarget, packageTarget, out _);
        public bool BindTargetToPackage(AnimationSource exportTarget, PackageIdentifier packageTarget, out ContentManager.LocalPackage localPackage)
        {
            localPackage = default;
            if (exportTarget == null) return false;

            this.exportTarget = exportTarget;
            this.packageTarget = packageTarget;

            return exportTarget.BindToPackage(packageTarget, out localPackage);
        }

        public void PushTargetToPackage() => PushTargetToPackage(exportTarget);
        public bool PushTargetToPackage(AnimationSource target)
        {
            try
            {
                this.exportTarget = target;
                if (target.PushToPackage())
                {
                    RefreshPackageLinksUI(); 
                    return true;
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while attempting to push content to package '{target.package}'");
                swole.LogError(ex);
            }

            return false;
        }
        public void PullTargetFromPackage() => PullTargetFromPackage(exportTarget);
        public bool PullTargetFromPackage(AnimationSource target)
        {
            try
            {
                this.exportTarget = target;
                if (target.PullFromPackage())
                {
                    RefreshPackageLinksUI();
                    return true;
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while attempting to pull content from package '{target.package}'");
                swole.LogError(ex);
            }

            return false;
        }

        #endregion

        #region Save/Load Session Window

        public static string AnimationEditorSessionDirectoryPath => Path.Combine(swole.AssetDirectory.FullName, ContentManager.folderNames_Editors, ContentManager.folderNames_AnimationEditor, ContentManager.folderNames_Sessions);

        protected const string _saveTag = "Save";

        public RectTransform saveSessionWindow;
        public static bool CheckIfSessionFileExists(string sessionName)
        {
            string path = AnimationEditorSessionDirectoryPath;
            if (Directory.Exists(path))
            {
                if (File.Exists(Path.Combine(path, $"{sessionName}.{ContentManager.fileExtension_Generic}"))) return true;  
            }
            return false;
        }
        public static bool SaveSessionInDefaultDirectory(Session session)
        {
            if (session == null) return false;

            try
            {
                var dir = Directory.CreateDirectory(AnimationEditorSessionDirectoryPath);
                if (SaveSession(dir, session, swole.DefaultLogger))
                {
                    session.IsDirty = false;
                    return true;
                }
            } 
            catch(Exception ex)
            {
                swole.LogError($"Encountered exception while trying to save session '{session.name}' to default directory");
                swole.LogError(ex);
            }
            return false;
        }
        public void SaveCurrentSessionInDefaultDirectory() 
        {
            SaveSessionInDefaultDirectory(currentSession);
            SyncSaveButton(currentSession);
        }
        public void OpenSessionSaveWindow() => OpenSessionSaveWindow(saveSessionWindow);
        public void OpenSessionSaveWindow(RectTransform window)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            SyncSessionSaveWindow(window);

            CustomEditorUtils.SetButtonOnClickActionByName(window, _saveTag, () =>
            {
                var session = currentSession;
                if (session != null)
                {
                    string sessionName = CustomEditorUtils.GetInputFieldText(window);
                    if (string.IsNullOrWhiteSpace(sessionName)) sessionName = session.name;  

                    void Save()
                    {
                        session.name = sessionName;
                        SaveSessionInDefaultDirectory(session);
                        SyncSessionSaveWindow();
                        SyncSaveButton(session);
                    }

                    if (session.name != sessionName && CheckIfSessionFileExists(sessionName))
                    {
                        ShowPopupConfirmation($"A session with the name '{sessionName}' already exists. Are you sure you want to overwrite it?", Save);
                    }
                    else Save();
                }
            });
        }
        protected void SyncSessionSaveWindow() => SyncSessionSaveWindow(saveSessionWindow);
        protected void SyncSessionSaveWindow(RectTransform window)
        {
            CustomEditorUtils.SetInputFieldText(window, currentSession == null ? "null session" : currentSession.name);
            CustomEditorUtils.SetComponentTextAndColorByName(window, _infoTag, currentSession == null ? "NULL" : (currentSession.IsDirty ? "UNSAVED CHANGES" : "SAVED"), currentSession == null ? colorUnbound : (currentSession.IsDirty ? colorNotSynced : colorSynced));

            CustomEditorUtils.SetButtonInteractableByName(window, _saveTag, currentSession != null); 
        }
        public void QuickSaveCurrentSession()
        {
            var currentSession = CurrentSession;

            if (string.IsNullOrWhiteSpace(currentSession.name) || !CheckIfSessionFileExists(currentSession.name))
            {
                OpenSessionSaveWindow();
                return;
            }

            SaveSessionInDefaultDirectory(currentSession);
            SyncSaveButton(currentSession);
        }
        public RectTransform saveButton;
        public void SyncSaveButton() => SyncSaveButton(CurrentSession);
        public void SyncSaveButton(Session session)
        {
            if (saveButton == null) return;

            bool savedBefore = !(session == null || string.IsNullOrWhiteSpace(session.name) || !CheckIfSessionFileExists(session.name));
            if (!savedBefore) session.IsDirty = true; // set raw to avoid calling other methods 

            var notSaved = saveButton.FindDeepChildLiberal("NotSaved");
            if (notSaved != null) notSaved.gameObject.SetActive(!savedBefore);

            var unSaved = saveButton.FindDeepChildLiberal("Unsaved");
            if (unSaved != null) unSaved.gameObject.SetActive(savedBefore && session.IsDirty);

            var saved = saveButton.FindDeepChildLiberal("Saved");
            if (saved != null) saved.gameObject.SetActive(savedBefore && !session.IsDirty);
        }

        public RectTransform loadSessionWindow;
        public void OpenSessionLoadWindow() => OpenSessionLoadWindow(loadSessionWindow);
        protected static readonly List<FileInfo> _tempFiles = new List<FileInfo>(); 
        public void OpenSessionLoadWindow(RectTransform window)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            try
            {
                var list = window.GetComponentInChildren<UIRecyclingList>();
                if (list == null) return;

                list.Clear();

                var dir = Directory.CreateDirectory(AnimationEditorSessionDirectoryPath);
                if (dir != null)
                {
                    var files = dir.GetFiles($"*.{ContentManager.fileExtension_Generic}", SearchOption.TopDirectoryOnly);
                    _tempFiles.Clear();
                    _tempFiles.AddRange(files);
                    _tempFiles.Sort((FileInfo x, FileInfo y) => y.LastWriteTime.CompareTo(x.LastWriteTime)); 
                     
                    foreach(var file in _tempFiles)
                    {
                        var temp = list.AddNewMember(Path.GetFileNameWithoutExtension(file.FullName), () =>
                        {
                            void Load()
                            {
                                var session = LoadSessionFromFile(file, swole.DefaultLogger);
                                if (session != null) 
                                { 
                                    LoadSession(session);
                                    window.gameObject.SetActive(false);
                                } 
                                else
                                {
                                    var errorMsg = window.GetComponentInChildren<UIPopupMessageFadable>();
                                    if (errorMsg != null) errorMsg.SetMessage("Failed to load session.").SetDisplayTime(errorMsg.DefaultDisplayTime).Show();
                                }
                            }

                            if (currentSession != null && currentSession.IsDirty)
                            {
                                ShowPopupConfirmation("Current session has unsaved changes that will be lost. Are you sure?", Load);
                            }
                            else Load();
                        }, false, null, file.FullName);
                    }
                    _tempFiles.Clear();
                }

                list.Refresh();
                for(int a = 0; a < list.VisibleMemberInstanceCount; a++)
                {
                    var inst = list.GetVisibleMemberInstance(a);
                    if (inst == null || inst.gameObject == null) continue;

                    CustomEditorUtils.SetButtonOnClickActionByName(inst.gameObject, "Delete", () =>
                    {
                        var visIndex = list.IndexOfVisibleMemberInstance(inst);
                        if (visIndex < 0) return;

                        var memIndex = list.GetMemberIndexFromVisibleIndex(visIndex);
                        if (memIndex < 0) return;

                        var mem = list.GetMember(memIndex);
                        if (mem.storage is string path)
                        {
                            void Delete()
                            {
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }

                                list.RemoveMember(mem);
                                list.Refresh();

                                SyncSaveButton();
                            }

                            ShowPopupConfirmation($"Are you sure you want to delete the session '{mem.name}'? This cannot be undone!", Delete); 
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while trying to open load session window");
                swole.LogError(ex);
            }
        }

        #endregion

        #region Animation Events

        protected const string str_name = "name";
        protected const string str_frame = "frame";
        protected const string str_edit = "edit";
        protected const string str_up = "up";
        protected const string str_down = "down";
        protected const string str_delete = "delete";
        protected const string str_addEvent = "addEvent";
        protected const string str_addEventToFrame = "addEventToFrame";
        private static string GetFrameHeader(int frameIndex) => $"{str_frame} {frameIndex}".ToUpper();
        public RectTransform animationEventsWindow;
        private static int FrameListSort(UIRecyclingList.MemberData memA, UIRecyclingList.MemberData memB)
        {
            int frameA = -1;
            int frameB = -1;
            if (memA.storage is int) frameA = (int)memA.storage;
            if (memB.storage is int) frameB = (int)memB.storage; 

            return (int)Mathf.Sign(frameA - frameB);
        }
        protected void RefreshAnimationEventKeyframes()
        {
            if (editMode != EditMode.ANIMATION_EVENTS) return; 
            RefreshKeyframes();
        }
        public void OpenAnimationEventsWindow() => OpenAnimationEventsWindow(animationEventsWindow);
        public void OpenAnimationEventsWindow(RectTransform window)
        {
            if (window == null) return;
            window.gameObject.SetActive(true);
            window.SetAsLastSibling(); 

            RefreshAnimationEventsWindow(window, ActiveAnimatable, CurrentSource);
        }

        public RectTransform animationEventEditWindow;
        public void OpenAnimationEventEditWindow(ImportedAnimatable animatable, AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList parentList = null) => OpenAnimationEventEditWindow(animationEventEditWindow, animatable, source, _event, list, listMember, parentList);
        public void OpenAnimationEventEditWindow(RectTransform window, ImportedAnimatable animatable, AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList parentList = null)
        {
            if (window == null) return;
            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            RefreshAnimationEventEditWindow(window, animatable, source, _event, list, listMember, parentList);
        }

        public void RefreshAnimationEventsWindow()
        {
            if (animationEventsWindow == null || !animationEventsWindow.gameObject.activeInHierarchy) return;
            RefreshAnimationEventsWindow(animationEventsWindow, ActiveAnimatable, CurrentSource); 
             
            if (animationEventEditWindow != null && animationEventEditWindow.gameObject.activeInHierarchy)
            {
                RefreshAnimationEventEditWindow(eventEditWindow_animatable, eventEditWindow_source, eventEditWindow_event, eventEditWindow_list, eventEditWindow_listMember, eventEditWindow_parentList);
            }
        }
        private readonly List<CustomAnimation.Event> _orderedEvents = new List<CustomAnimation.Event>();
        private static readonly List<CustomAnimation.Event> _tempEvents = new List<CustomAnimation.Event>();
        private static UIRecyclingList.MemberData GetFrameEventList(UIRecyclingList list, int frameIndex, out bool isNew)
        {
            isNew = false;

            string frameHeader = GetFrameHeader(frameIndex);
            var mem = list.FindMember(frameHeader);
            if (mem.id == null)
            {
                mem.name = frameHeader;
                mem.storage = frameIndex;
                mem.id = list.AddOrUpdateMember(mem, false);
                list.Sort(FrameListSort);
                isNew = true;
            }

            return mem;
        }
        public void RefreshAnimationEventsWindow(RectTransform window, ImportedAnimatable animatable, AnimationSource source)
        {
            if (window == null || !window.gameObject.activeInHierarchy) return;  
            var mainList = window.gameObject.GetComponentInChildren<UIRecyclingList>(); 
            if (mainList == null) return;
            mainList.autoRefreshChildLists = false;
            mainList.membersAreNotButtons = true; 

            CustomAnimation.Event[] eventArray = source == null ? null : source.GetOrCreateRawData().events; // for checking if events have been altered
            CustomAnimation.Event NewEventAtIndex(int frameIndex)
            {
                var anim = source.GetOrCreateRawData();

                var editRecord = BeginNewAnimationEditRecord(animatable, source);
                editRecord?.SetOriginalEvents(anim.events);

                var _events = new CustomAnimation.Event[anim.events == null ? 1 : anim.events.Length + 1];
                if (anim.events != null) anim.events.CopyTo(_events, 0);
                var _event = new CustomAnimation.Event("new_event_name", (float)AnimationTimeline.FrameToTimelinePosition(frameIndex, source.rawAnimation.framesPerSecond), 0, null);
                _event.Priority = 999999; // Forces the event to the end of the frame event list it gets added to (value will get replaced after by its index in the list)
                _events[_events.Length - 1] = _event; 
                anim.events = _events;

                source.MarkAsDirty();

                RefreshAnimationEventKeyframes();

                editRecord?.SetEditedEvents(anim.events);
                CommitAnimationEditRecord(editRecord);

                return _event; 
            }

            int prevStartIndex = -3;
            int prevEndIndex = -3;
            bool validRefresh = false;
            void Refreshed()
            {
                if ((mainList.VisibleRangeStart == prevStartIndex && mainList.VisibleRangeEnd == prevEndIndex) && !mainList.IsDirty && (source == null || source.rawAnimation == null || ReferenceEquals(eventArray, source.rawAnimation.events))) return;

                validRefresh = true;
                prevStartIndex = mainList.VisibleRangeStart;
                prevEndIndex = mainList.VisibleRangeEnd; 

                mainList.Clear();
                if (source != null && source.rawAnimation != null && source.rawAnimation.events != null)
                {
                    _orderedEvents.Clear();
                    eventArray = source.rawAnimation.events; 
                    _orderedEvents.AddRange(eventArray);
                    _orderedEvents.Sort(CustomAnimation.SortEventsAscending);   
                    foreach (var _event in _orderedEvents)
                    {
                        var frameEventListData = GetFrameEventList(mainList, AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, source.rawAnimation.framesPerSecond), out bool isNewList);

                        if (isNewList)
                        {
                            void OnRefreshEventList(UIRecyclingList.MemberData data, GameObject inst) // Called by the parent list per member when the parent list is refreshed
                            {
                                if (!validRefresh) return;
                                if (data.storage is not int frameIndex) 
                                {
                                    swole.LogError($"[{nameof(AnimationEditor)}] Something went wrong with frameIndex storage"); 
                                    return; 
                                }

                                var frameEventList = inst.GetComponentInChildren<UIRecyclingList>(true);
                                if (frameEventList == null) return;

                                frameEventList.Clear();

                                void OnRefreshEvent(UIRecyclingList.MemberData data, GameObject inst) // Called by the frame list per member when that frame list is refreshed
                                {
                                    if (source.rawAnimation == null || data.storage is not CustomAnimation.Event _event) return;

                                    void RefreshEventName(string name)
                                    {
                                        _event.Name = data.name = name;
                                        data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                        source?.MarkAsDirty();
                                    }

                                    CustomEditorUtils.SetInputFieldTextByName(inst, str_name, _event.Name);
                                    CustomEditorUtils.SetInputFieldOnValueChangeActionByName(inst, str_name, RefreshEventName);

                                    void StartEditing() => OpenAnimationEventEditWindow(animationEventEditWindow, animatable, source, _event, frameEventList, data, mainList);
                                    CustomEditorUtils.SetButtonOnClickActionByName(inst, str_edit, StartEditing);

                                    void MoveUp()
                                    {
                                        int pos = data.id.index;
                                        int swapPos = pos + 1;

                                        var swapData = frameEventList.GetMember(swapPos);
                                        if (swapData.id != null)
                                        {
                                            data.id.index = swapPos;
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);
                                            data.id.index = swapPos; // do it again incase id reference changed
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                            swapData.id.index = pos;
                                            frameEventList.AddOrUpdateMember(swapData, true);

                                            var editRecord = BeginNewAnimationEditRecord(animatable, source);
                                            editRecord?.SetOriginalEventState(_event);
                                            _event.Priority = swapPos;
                                            editRecord?.RecordEventStateEdit(_event);
                                            CommitAnimationEditRecord(editRecord);
                                        }

                                        source?.MarkAsDirty();
                                    }
                                    void MoveDown()
                                    {
                                        int pos = data.id.index;
                                        int swapPos = pos - 1;

                                        var swapData = frameEventList.GetMember(swapPos);
                                        if (swapData.id != null)
                                        {
                                            data.id.index = swapPos;
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);
                                            data.id.index = swapPos; // do it again incase id reference changed
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                            swapData.id.index = pos;
                                            frameEventList.AddOrUpdateMember(swapData, true);

                                            var editRecord = BeginNewAnimationEditRecord(animatable, source);
                                            editRecord?.SetOriginalEventState(_event);
                                            _event.Priority = swapPos;
                                            editRecord?.RecordEventStateEdit(_event);
                                            CommitAnimationEditRecord(editRecord);
                                        }

                                        source?.MarkAsDirty();
                                    }

                                    if (_event.Priority > 0) CustomEditorUtils.SetButtonOnClickActionByName(inst, str_up, MoveUp); else CustomEditorUtils.SetButtonInteractableByName(inst, str_up, false);
                                    _tempEvents.Clear();
                                    if (_event.Priority < source.rawAnimation.GetEventsAtFrame((decimal)_event.TimelinePosition, _tempEvents).Count - 1) CustomEditorUtils.SetButtonOnClickActionByName(inst, str_down, MoveDown); else CustomEditorUtils.SetButtonInteractableByName(inst, str_down, false);
                                    _tempEvents.Clear(); 
                                }

                                foreach(var __event in _orderedEvents) // Add the events to the visible list
                                {
                                    int frameIndex_ = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)__event.TimelinePosition, source.rawAnimation.framesPerSecond);
                                    if (frameIndex_ != frameIndex) continue;

                                    UIRecyclingList.MemberID memId = null; 
                                    void StartEditing() => OpenAnimationEventEditWindow(animationEventEditWindow, animatable, source, __event, frameEventList, frameEventList.GetMember(memId), mainList);
                                    memId = frameEventList.AddNewMember(__event.Name, StartEditing, false, OnRefreshEvent, __event); 
                                    __event.Priority = memId.index; 
                                }

                                if (source != null)
                                {
                                    void CreateNewEvent()
                                    {
                                        var _event = NewEventAtIndex(frameIndex);
                                        mainList.MarkAsDirty(); // Make sure list refreshes fully
                                        mainList.Refresh();
                                        OpenAnimationEventEditWindow(animationEventEditWindow, animatable, source, _event, null, new UIRecyclingList.MemberData() { name = _event.Name, storage = _event }, mainList);
                                    }
                                    CustomEditorUtils.SetButtonOnClickActionByName(inst, str_addEventToFrame, CreateNewEvent);
                                }

                            }

                            frameEventListData.onRefresh = OnRefreshEventList;
                            frameEventListData.id = mainList.AddOrUpdateMember(frameEventListData, false); 
                        }
                    }
                }
            } 
            void PostRefresh()
            {
                if (!validRefresh) return;
                var subLists = mainList.Container.GetComponentsInChildren<UIRecyclingList>();
                foreach (var subList in subLists) if (subList != mainList) subList.Refresh();
                validRefresh = false;
            }

            mainList.ClearAllListeners();
            mainList.OnRefresh += Refreshed; 
            mainList.AfterRefresh += PostRefresh;
            mainList.Refresh(); 

            if (source != null)
            {
                void CreateNewEvent()
                {
                    var _event = NewEventAtIndex(timelineWindow == null ? 0 : AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)timelineWindow.ScrubPosition, source.GetOrCreateRawData().framesPerSecond));
                    mainList.MarkAsDirty(); // Make sure list refreshes fully
                    mainList.Refresh(); 
                    OpenAnimationEventEditWindow(animationEventEditWindow, animatable, source, _event, null, new UIRecyclingList.MemberData() { name = _event.Name, storage = _event }, mainList); 
                }
                CustomEditorUtils.SetButtonOnClickActionByName(window, str_addEvent, CreateNewEvent);  
            }
        }

        private ImportedAnimatable eventEditWindow_animatable;
        private AnimationSource eventEditWindow_source;
        private CustomAnimation.Event eventEditWindow_event;
        private UIRecyclingList eventEditWindow_list;
        private UIRecyclingList.MemberData eventEditWindow_listMember;
        private UIRecyclingList eventEditWindow_parentList;
        public void RefreshAnimationEventEditWindow(ImportedAnimatable animatable, AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList eventList = null) => RefreshAnimationEventEditWindow(animationEventEditWindow, animatable, source, _event, list, listMember, eventList); 
        public void RefreshAnimationEventEditWindow(RectTransform window, ImportedAnimatable animatable_, AnimationSource source_, CustomAnimation.Event _event_, UIRecyclingList list_ = null, UIRecyclingList.MemberData listMember_ = default, UIRecyclingList parentList_ = null)
        {
            if (window == null || !window.gameObject.activeInHierarchy || source_ == null || _event_ == null) return;
            
            eventEditWindow_animatable = animatable_;
            eventEditWindow_source = source_;
            eventEditWindow_event = _event_;
            eventEditWindow_list = list_;
            eventEditWindow_listMember = listMember_;
            eventEditWindow_parentList = parentList_;

            int frameRate = eventEditWindow_source.rawAnimation == null ? CustomAnimation.DefaultFrameRate : eventEditWindow_source.rawAnimation.framesPerSecond;

            var codeEditor = window.GetComponentInChildren<ICodeEditor>();
            if (codeEditor != null)
            {
                codeEditor.ClearAllListeners(); 

                var popup = window.GetComponentInChildren<UIPopup>();
                if (popup == null) popup = window.GetComponentInParent<UIPopup>();
                if (popup != null)
                {
                    if (popup.OnClose == null) popup.OnClose = new UnityEvent(); else popup.OnClose.RemoveListener(codeEditor.SpoofClose);
                    popup.OnClose.AddListener(codeEditor.SpoofClose);
                }

                codeEditor.Code = string.IsNullOrWhiteSpace(eventEditWindow_event.Source) ? string.Empty : eventEditWindow_event.Source;

                void SetEventCode(string code) => eventEditWindow_event.Source = code;
                codeEditor.ListenForChanges(SetEventCode);
                codeEditor.ListenForClosure(SetEventCode);
            }

            void SetEventName(string name, bool undoable) 
            {
                UndoableEditAnimationSourceData editRecord = null;
                if (undoable)
                {
                    editRecord = BeginNewAnimationEditRecord(eventEditWindow_animatable, eventEditWindow_source);
                    editRecord?.SetOriginalEventState(eventEditWindow_event);
                }

                eventEditWindow_event.Name = name; 
                if (eventEditWindow_list != null)
                {
                    if (eventEditWindow_list.TryGetMember(eventEditWindow_listMember.id, out var tempMem) || eventEditWindow_list.TryGetMemberByStorageComparison(eventEditWindow_listMember, out tempMem)) eventEditWindow_listMember = tempMem;
                    eventEditWindow_listMember.name = name;
                    eventEditWindow_listMember.id = eventEditWindow_list.AddOrUpdateMemberWithStorageComparison(eventEditWindow_listMember); 
                }

                if (editRecord != null)
                {
                    editRecord.RecordEventStateEdit(eventEditWindow_event);
                    CommitAnimationEditRecord(editRecord);
                }

                eventEditWindow_source?.MarkAsDirty();
            }
            void SetEventName2(string name) => SetEventName(name, true);
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(window, str_name, SetEventName2);

            void SetEventFrame(int frame, bool force, bool undoable)
            {
                bool changed = force || AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)eventEditWindow_event.TimelinePosition, frameRate) != frame;
                if (changed)
                {
                    if (force) CustomEditorUtils.SetInputFieldTextByName(window, str_frame, frame.ToString()); // Wasn't changed by input field so update it

                    UndoableEditAnimationSourceData editRecord = null;
                    if (undoable)
                    {
                        editRecord = BeginNewAnimationEditRecord(eventEditWindow_animatable, eventEditWindow_source);
                        editRecord?.SetOriginalEventState(eventEditWindow_event);
                    }

                    bool refreshParent = false;
                    eventEditWindow_event.TimelinePosition = (float)AnimationTimeline.FrameToTimelinePosition(frame, frameRate);
                    if (eventEditWindow_list != null) 
                    {
                        eventEditWindow_list.RemoveMember(eventEditWindow_listMember);
                        eventEditWindow_list.Refresh();
                        if (eventEditWindow_list.Count <= 0) refreshParent = true;                     
                    }
                    if (eventEditWindow_listMember.id != null) eventEditWindow_listMember.id.index = -1;

                    if (eventEditWindow_parentList != null)
                    {
                        eventEditWindow_parentList.MarkAsDirty();  
                        string header = GetFrameHeader(frame);
                        var tempData = eventEditWindow_parentList.FindMember(header);
                        tempData.storage = frame;

                        if (tempData.id == null)
                        {
                            tempData.name = header;
                            tempData.id = eventEditWindow_parentList.AddOrUpdateMemberWithStorageComparison(tempData, true);
                            refreshParent = false;

                            if (tempData.id == null || tempData.id.index < 0)
                            {
                                tempData = eventEditWindow_parentList.FindMember(header); 
                            }
                        }
                        if (eventEditWindow_parentList.TryGetVisibleMemberInstance(tempData, out GameObject inst)) 
                        {
                            eventEditWindow_list = inst.GetComponentInChildren<UIRecyclingList>(true);

                            if (eventEditWindow_list != null)
                            {
                                eventEditWindow_listMember = eventEditWindow_list.AddOrGetMemberWithStorageComparison(eventEditWindow_listMember, true);
                                eventEditWindow_event.Priority = eventEditWindow_listMember.id.index; 
                            }
                        }
                        if (refreshParent) eventEditWindow_parentList.Refresh();
                    }

                    if (editRecord != null)
                    {
                        editRecord.RecordEventStateEdit(eventEditWindow_event);
                        CommitAnimationEditRecord(editRecord);
                    }

                    RefreshAnimationEventKeyframes();
                    eventEditWindow_source?.MarkAsDirty();
                }
            }

            void SetEventFrameFromString(string frameStr)
            {
                if (int.TryParse(frameStr, out int frame)) SetEventFrame(frame, false, true);
            }
            CustomEditorUtils.SetInputFieldOnEndEditActionByName(window, str_frame, SetEventFrameFromString);

            void PromptDelete()
            {
                void Delete()
                {
                    if (eventEditWindow_event != null && eventEditWindow_source != null && eventEditWindow_source.rawAnimation != null && eventEditWindow_source.rawAnimation.events != null)
                    {
                        var editRecord = BeginNewAnimationEditRecord(eventEditWindow_animatable, eventEditWindow_source);
                        editRecord.SetOriginalEvents(eventEditWindow_source.rawAnimation.events);

                        _tempEvents.Clear();
                        _tempEvents.AddRange(eventEditWindow_source.rawAnimation.events);
                        _tempEvents.RemoveAll(i => ReferenceEquals(eventEditWindow_event, i));
                        eventEditWindow_source.rawAnimation.events = _tempEvents.ToArray();
                        _tempEvents.Clear();

                        if (eventEditWindow_parentList != null)
                        {
                            int frame = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)eventEditWindow_event.TimelinePosition, eventEditWindow_source.rawAnimation.framesPerSecond);
                            string header = GetFrameHeader(frame);
                            var tempData = eventEditWindow_parentList.FindMember(header);
                            tempData.storage = frame;
                            if (eventEditWindow_parentList.TryGetVisibleMemberInstance(tempData, out GameObject inst))
                            {
                                eventEditWindow_list = inst.GetComponentInChildren<UIRecyclingList>(true);    
                            }
                        }

                        editRecord.SetEditedEvents(eventEditWindow_source.rawAnimation.events);
                        CommitAnimationEditRecord(editRecord);
                    }

                    window.gameObject.SetActive(false); 
                    if (eventEditWindow_list != null) 
                    {
                        eventEditWindow_list.RemoveMember(eventEditWindow_listMember);
                        eventEditWindow_list.Refresh();

                        if (eventEditWindow_list.Count <= 0 && eventEditWindow_parentList != null)
                        {
                            eventEditWindow_parentList.Refresh(); 
                        }
                    }

                    RefreshAnimationEventKeyframes();
                    eventEditWindow_source?.MarkAsDirty();  
                }
                ShowPopupConfirmation($"Are you sure you want to delete '{(eventEditWindow_event == null ? "null" : eventEditWindow_event.Name)}'?", Delete);
            }
             
            CustomEditorUtils.SetInputFieldTextByName(window, str_name, eventEditWindow_event.Name);
            CustomEditorUtils.SetButtonOnClickActionByName(window, str_delete, PromptDelete);

            int frameIndex = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)eventEditWindow_event.TimelinePosition, frameRate);
            if (eventEditWindow_list == null)
            { 
                SetEventFrame(frameIndex, true, false);  
                SetEventName(eventEditWindow_event.Name, false);
            } 
            else
            {
                CustomEditorUtils.SetInputFieldTextByName(window, str_frame, frameIndex.ToString()); 
            }
        }

        #endregion

        #region Bone Grouping

        protected const string _nameTag = "Name";
        protected const string _colorTag = "Color";
        protected const string _showTag = "Show";
        protected const string _hideTag = "Hide";

        public RectTransform boneGroupWindow;
        private readonly List<GameObject> boneGroupWindowInstances = new List<GameObject>();
        public void OpenBoneGroupWindow() 
        {
            if (boneGroupWindow == null) return;

            var window = boneGroupWindow.gameObject;
            window.SetActive(true);
            boneGroupWindow.SetAsLastSibling();

            var pool = window.GetComponentInChildren<PrefabPool>();
            if (pool == null || pool.Prototype == null) return;

            pool.Prototype.SetActive(false); 

            foreach (var inst in boneGroupWindowInstances) if (inst != null) 
                { 
                    pool.Release(inst);
                    inst.SetActive(false);
                }
            boneGroupWindowInstances.Clear();

            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            var layout = window.GetComponentInChildren<LayoutGroup>();
            if (layout == null) return;

            if (activeObj.animator != null && activeObj.animator.avatar != null)
            {
                for(int a = 0; a < activeObj.animator.avatar.BoneGroupCount; a++)
                {
                    var group = activeObj.animator.avatar.GetBoneGroup(a);
                    if (activeObj.TryGetBoneGroup(group.name, out var localGroup) && pool.TryGetNewInstance(out GameObject inst))
                    {
                        boneGroupWindowInstances.Add(inst); 

                        var instT = inst.transform;
                        inst.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(inst, _nameTag, group.name); 

                        var colorObj = instT.FindDeepChildLiberal(_colorTag);
                        if (colorObj != null)
                        {
                            var img = colorObj.GetComponentInChildren<Image>();
                            if (img != null) img.color = group.color;
                        }

                        void SetVisibility(bool value)
                        {
                            localGroup.active = value;
                            RefreshBoneGroupVisibility();
                        }

                        var toggle = inst.GetComponentInChildren<Toggle>();
                        if (toggle == null)
                        {
                            var hideObj = instT.FindDeepChildLiberal(_hideTag);
                            if (hideObj == null) continue;
                            var showObj = instT.FindDeepChildLiberal(_showTag);
                            if (showObj == null) continue;

                            CustomEditorUtils.SetButtonOnClickAction(hideObj, () => {
                                SetVisibility(false);
                                if (showObj != null) showObj.gameObject.SetActive(true);
                                hideObj.gameObject.SetActive(false);
                            }); 
                            CustomEditorUtils.SetButtonOnClickAction(showObj, () => {
                                SetVisibility(true);
                                if (hideObj != null) hideObj.gameObject.SetActive(true);  
                                showObj.gameObject.SetActive(false);  
                            });

                            hideObj.gameObject.SetActive(localGroup.active);
                            showObj.gameObject.SetActive(!localGroup.active); 

                        }
                        else
                        {
                            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                            toggle.SetIsOnWithoutNotify(localGroup.active);
                            toggle.onValueChanged.AddListener(SetVisibility);
                        }

                    }
                }
            }
        }

        public void RefreshBoneGroupVisibility() 
        {
            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            if (activeObj.animator != null && activeObj.animator.avatar != null)
            {
                var ikManager = activeObj.animator.IkManager;
                bool IsBoneVisibleIK(PoseableBone bone, out bool isIkBone, out bool isFkIk)
                {
                    isIkBone = isFkIk = false;
                    if (bone == null || bone.transform == null) return true;

                    bool invert = false;
                    string boneName = bone.name;
                    if (!activeObj.animator.avatar.IsIkBone(boneName))
                    {
                        if (activeObj.animator.avatar.IsIkBoneFkEquivalent(boneName, out var ikBone) && ikBone.type == CustomAvatar.IKBoneType.Target) // Hide fk bone when ik is active (for Target ik bone types only)
                        {
                            isFkIk = true;

                            int ind = FindPoseableBoneIndex(ikBone.name);
                            if (ind >= 0)
                            {
                                bone = poseableBones[ind]; // set to ik equivalent
                                invert = true; // if the ik controller is active this will hide the fk bone
                            }
                        }
                        else return true;
                    } 
                    else
                    {
                        isIkBone = true;
                    }


                    for (int a = 0; a < ikManager.ControllerCount; a++)
                    {
                        var controller = ikManager[a];
                        if (controller == null || !controller.CanBeToggled) continue;  

                        if (controller.IsDependentOn(bone.transform)) return invert ? !controller.IsActive : controller.IsActive;
                    }

                    return true;
                }

                foreach(var bone in poseableBones)
                {
                    if (bone == null || bone.transform == null || !activeObj.animator.avatar.TryGetBoneInfo(bone.name, out var boneInfo)) continue;
                    var boneGroup = activeObj.animator.avatar.GetBoneGroup(bone.transform);//activeObj.animator.avatar.GetBoneGroup(boneInfo.boneGroup);
                    bool activate = true;
                    if (activeObj.TryGetBoneGroup(boneGroup.name, out var localBoneGroup)) activate = localBoneGroup.active; 
                    bool visIk = IsBoneVisibleIK(bone, out bool isIkBone, out bool isFkIk);  
                    activate = activate && visIk;
                    bone.SetActive(activate);
                    if (bone.parent >= 0)
                    {
                        var parent = poseableBones[bone.parent];
                        if (parent != null && activeObj.animator.avatar.TryGetBoneInfo(parent.name, out boneInfo)) 
                        {
                            boneGroup = activeObj.animator.avatar.GetBoneGroup(parent.transform);//activeObj.animator.avatar.GetBoneGroup(boneInfo.boneGroup);

                            bool activateLeaf = activate;
                            if (activeObj.TryGetBoneGroup(boneGroup.name, out localBoneGroup)) activateLeaf = localBoneGroup.active;
                            if (isIkBone) activateLeaf = activateLeaf && visIk; 

                            parent.SetLeafActive(bone.transform, activateLeaf, isFkIk);  
                        } 
                    }
                }
            }
             
            RefreshRootObjects(); // make any new visible bone objects selectable
        }

        #endregion

        #region IK Posing

        protected const string _activateTag = "Activate";
        protected const string _deactivateTag = "Deactivate";

        protected const string _lockTag = "Lock";
        protected const string _unlockTag = "Unlock";

        public RectTransform ikGroupWindow;
        private readonly List<GameObject> ikGroupWindowInstances = new List<GameObject>();
        public void OpenIKGroupWindow()
        {
            if (ikGroupWindow == null) return;

            var window = ikGroupWindow.gameObject;
            window.SetActive(true);
            ikGroupWindow.SetAsLastSibling(); 

            var pool = window.GetComponentInChildren<PrefabPool>();
            if (pool == null || pool.Prototype == null) return;

            pool.Prototype.SetActive(false);

            foreach (var inst in ikGroupWindowInstances) 
                if (inst != null)
                {
                    pool.Release(inst);
                    inst.SetActive(false);
                }
            ikGroupWindowInstances.Clear();

            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            var layout = window.GetComponentInChildren<LayoutGroup>();
            if (layout == null) return;

            if (activeObj.animator != null && activeObj.animator.IkManager != null && activeObj.animator.avatar != null)
            {

                var ikManager = activeObj.animator.IkManager;
                for (int a = 0; a < ikManager.ControllerCount; a++)
                {
                    var ik = ikManager[a];
                    if (ik == null || !ik.CanBeToggled) continue;

                    if (activeObj.TryGetIKGroup(ik.name, out var localGroup) && pool.TryGetNewInstance(out GameObject inst))
                    {
                        ikGroupWindowInstances.Add(inst);

                        var instT = inst.transform;
                        inst.SetActive(true);
                        CustomEditorUtils.SetComponentTextByName(inst, _nameTag, ik.name);

                        void SetActive(bool value)
                        {
                            localGroup.active = value;
                            RefreshIKControllerActivation();
                        }

                        var toggle = inst.GetComponentInChildren<Toggle>();
                        if (toggle == null)
                        {
                            var deactivateObj = instT.FindDeepChildLiberal(_deactivateTag);
                            if (deactivateObj == null) continue;
                            var activateObj = instT.FindDeepChildLiberal(_activateTag);
                            if (activateObj == null) continue;

                            CustomEditorUtils.SetButtonOnClickAction(deactivateObj, () => {
                                SetActive(false);
                                if (activateObj != null) activateObj.gameObject.SetActive(true);
                                deactivateObj.gameObject.SetActive(false);
                            });
                            CustomEditorUtils.SetButtonOnClickAction(activateObj, () => {
                                SetActive(true);
                                if (deactivateObj != null) deactivateObj.gameObject.SetActive(true);
                                activateObj.gameObject.SetActive(false);
                            });

                            deactivateObj.gameObject.SetActive(localGroup.active);
                            activateObj.gameObject.SetActive(!localGroup.active);

                        }
                        else
                        {
                            if (toggle.onValueChanged == null) toggle.onValueChanged = new Toggle.ToggleEvent(); else toggle.onValueChanged.RemoveAllListeners();
                            toggle.SetIsOnWithoutNotify(localGroup.active);
                            toggle.onValueChanged.AddListener(SetActive);
                        }

                        var lockObj = instT.FindDeepChildLiberal(_lockTag);
                        if (lockObj == null) continue;
                        var unlockObj = instT.FindDeepChildLiberal(_unlockTag);
                        if (unlockObj == null) continue;

                        CustomEditorUtils.SetButtonOnClickAction(lockObj, () => {
                            LockTransform(ik.Target);
                            LockTransform(ik.BendGoal);
                            if (unlockObj != null) unlockObj.gameObject.SetActive(true);
                            lockObj.gameObject.SetActive(false);
                        });
                        CustomEditorUtils.SetButtonOnClickAction(unlockObj, () => { 
                            UnlockTransform(ik.Target);
                            UnlockTransform(ik.BendGoal);
                            if (lockObj != null) lockObj.gameObject.SetActive(true);
                            unlockObj.gameObject.SetActive(false);  
                        });

                        bool isLocked = IsTransformLocked(ik.Target);
                        lockObj.gameObject.SetActive(!isLocked);
                        unlockObj.gameObject.SetActive(isLocked);  

                    }
                }
            }
        }

        public void RefreshIKControllerActivation(bool force = false)
        {
            if ((IsPlayingPreview && !force) || CurrentSession == null) return;

            foreach (var obj in CurrentSession.importedObjects)
            {
                if (obj == null || obj.animator == null) continue;

                var ikManager = obj.animator.IkManager; 
                if (ikManager == null) continue;

                for(int a = 0; a < ikManager.ControllerCount; a++)
                {
                    var ik = ikManager[a];
                    if (ik == null || !ik.CanBeToggled) continue;

                    bool activate = false;
                    if (obj.TryGetIKGroup(ik.name, out var group)) activate = group.active;

                    ik.SetActive(activate);
                    ik.SetWeight(1);
                    ik.SetPositionWeight(1);
                    ik.SetRotationWeight(1);
                    ik.SetBendGoalWeight(1); 
                    void SetActiveIKTransform(Transform ikTransform)
                    {
                        if (ikTransform == null) return;

                        bool boneVisible = activate;
                        if (boneVisible)
                        {
                            if (obj.animator.avatar != null && obj.animator.avatar.TryGetBoneInfo(ikTransform.name, out var boneInfo))
                            {
                                var boneGroup = obj.animator.avatar.GetBoneGroup(boneInfo.boneGroup);
                                if (obj.TryGetBoneGroup(boneGroup.name, out var localBoneGroup)) boneVisible = localBoneGroup.active;
                            }
                        }

                        var poseableIndex = FindPoseableBoneIndex(ikTransform);
                        if (poseableIndex >= 0) poseableBones[poseableIndex].SetActive(boneVisible);

                        if (obj.animator.avatar != null && obj.animator.avatar.IsIkBone(obj.animator.avatar.Remap(ikTransform.name), out var ikBone) && ikBone.type == CustomAvatar.IKBoneType.Target && !string.IsNullOrWhiteSpace(ikBone.fkParent)) // Hide fk bone when ik is active, and vice versa (for Target ik bone types only)
                        {
                            poseableIndex = FindPoseableBoneIndex(ikBone.fkParent);
                            if (poseableIndex >= 0) poseableBones[poseableIndex].SetActive(group != null ? (!boneVisible && !group.active) : !boneVisible);     
                        }
                    }

                    SetActiveIKTransform(ik.BendGoal);
                    SetActiveIKTransform(ik.Target);
                }
            }

            ForceApplyTransformStateChanges(3);
            IEnumerator Wait() // Prevent any snapping ik from being recorded
            {
                yield return null;
                yield return null;
                ForceApplyTransformStateChanges();
            }
            StartCoroutine(Wait());
             
            RefreshRootObjects(); // make any new visible ik objects selectable
        }

        #endregion

        public RectTransform mirroringWindow;

        #region Audio Syncing

        private const string str_import = "Import";
        private const string str_play = "Play";

        private const string str_addClip = "AddClip";
        private const string str_syncList = "SyncList";
        private const string str_startTime = "StartTime";
        private const string str_mixer = "Mixer";
        private const string str_importAudio = "ImportAudio"; 

        public RectTransform audioSyncingWindow;

        public void OpenAudioSyncingWindow() => OpenAudioSyncingWindow(audioSyncingWindow);
        public void OpenAudioSyncingWindow(RectTransform audioSyncingWindow)
        {
            if (audioSyncingWindow == null) return;

            audioSyncingWindow.gameObject.SetActive(true);
            audioSyncingWindow.SetAsLastSibling();

            RefreshAudioSyncingWindow(audioSyncingWindow);
        }

        public void RefreshAudioSyncingWindow() => RefreshAudioSyncingWindow(audioSyncingWindow);
        public void RefreshAudioSyncingWindow(RectTransform audioSyncingWindow)
        {
            if (audioSyncingWindow == null || !audioSyncingWindow.gameObject.activeSelf) return;

            Transform windowChild = null;

            windowChild = audioSyncingWindow.FindDeepChildLiberal(str_syncList);
            if (windowChild == null) return;

            var syncList = windowChild.GetComponentInChildren<UIRecyclingList>();
            if (syncList == null) return;

            syncList.Clear();

            var audioImportWindow = audioSyncingWindow.FindDeepChildLiberal(str_importAudio);

            var activeSource = CurrentSource;
            if (activeSource != null)
            {

                CustomEditorUtils.SetButtonOnClickActionByName(audioSyncingWindow, str_addClip, () =>
                {
                    activeSource.AddAudioSyncImport(new AnimationSource.AudioSyncImport());
                    RefreshAudioSyncingWindow(audioSyncingWindow);
                });

                activeSource.RemoveAudioSyncImport(null);
                for (int a = 0; a < activeSource.AudioSyncImportsCount; a++)
                {
                    var asi = activeSource.GetAudioSyncImport(a);
                    void OnRefresh(UIRecyclingList.MemberData memberData, GameObject instance)
                    {
                        CustomEditorUtils.SetInputFieldTextByName(instance, str_name, asi.path);
                        CustomEditorUtils.SetInputFieldOnValueChangeActionByName(instance, str_name, (string val) =>
                        {
                            asi.path = val; 
                            activeSource.MarkAsDirty();  
                        });

                        CustomEditorUtils.SetInputFieldTextByName(instance, str_startTime, asi.playTime.ToString());
                        CustomEditorUtils.SetInputFieldOnValueChangeActionByName(instance, str_startTime, (string val) =>
                        {
                            if (float.TryParse(val, out float time)) 
                            { 
                                asi.playTime = time;
                                activeSource.MarkAsDirty();
                            }
                        });

                        CustomEditorUtils.SetInputFieldTextByName(instance, str_mixer, asi.mixerPath);
                        CustomEditorUtils.SetInputFieldOnValueChangeActionByName(instance, str_mixer, (string val) =>
                        {
                            asi.mixerPath = val;
                            activeSource.MarkAsDirty();   
                        });

                        CustomEditorUtils.SetButtonOnClickActionByName(instance, str_import, () =>
                        {
                            if (audioImportWindow != null) OpenAudioImportWindow((RectTransform)audioImportWindow, activeSource, asi, audioSyncingWindow);
                        });
                        CustomEditorUtils.SetButtonOnClickActionByName(instance, str_play, () => PersistentAudioPlayer.Get2DSourceAndPlay(asi.Clip, 1, 1, asi.MixerGroup));
                        CustomEditorUtils.SetButtonOnClickActionByName(instance, str_delete, () =>
                        {
                            activeSource.RemoveAudioSyncImport(asi);
                            RefreshAudioSyncingWindow(audioSyncingWindow); 
                        });
                    }
                    syncList.AddNewMember(asi.path, null, false, OnRefresh, asi);
                }
            }

            syncList.Refresh();

        }

        protected void OpenAudioImportWindow(RectTransform audioImportWindow, AnimationSource source, AnimationSource.AudioSyncImport asi, RectTransform audioSyncingWindow)
        {
            if (audioImportWindow == null) return;

            audioImportWindow.gameObject.SetActive(true); 

            var libraryList = audioImportWindow.GetComponentInChildren<UICategorizedList>(); 
            if (libraryList == null) return;

            libraryList.Clear();
            var internalAudioCollections = ResourceLib.GetAllAudioCollections(); 
            foreach (var library in internalAudioCollections) 
            {
                if (library.ClipCount <= 0) continue;

                var category = libraryList.AddOrGetCategory(library.name); 
                for(int a = 0; a < library.ClipCount; a++)
                {
                    var clip = library.GetAudioClip(a);
                    if (clip == null) continue;

                    libraryList.AddNewListMember(clip.Name, category, () =>
                    {
                        asi.path = clip.Name;
                        source.MarkAsDirty(); 
                        audioImportWindow.gameObject.SetActive(false);
                        RefreshAudioSyncingWindow(audioSyncingWindow);  
                    });
                }
            }

        }

        protected static void PlayAudioSyncImports(AnimationSource source, float prevTime, float currentTime, float deltaTime)
        {
            try
            {
                float timeDelta = currentTime - prevTime;
                if (timeDelta == 0 || deltaTime == 0) return; 

                bool reversing = timeDelta < 0;

                for (int a = 0; a < source.AudioSyncImportsCount; a++)
                {
                    var asi = source.GetAudioSyncImport(a);
                    if (asi == null || asi.Clip == null/* || (!reversing && (prevTime != 0 || asi.playTime != 0) && (prevTime >= asi.playTime || currentTime < asi.playTime)) || (reversing && (prevTime <= asi.playTime || currentTime > asi.playTime))*/) continue;

                    if (asi.sourceClaim == null || !asi.sourceClaim.IsValid) asi.sourceClaim = PersistentAudioPlayer.Get2DSourceAndPlay(asi.Clip, 1, 1, asi.MixerGroup);

                    /*
                    if (reversing)
                    {
                        asi.sourceClaim.Source.pitch = -1;
                        asi.sourceClaim.Source.timeSamples = asi.sourceClaim.Source.clip.samples - 1;
                    }

                    asi.sourceClaim.Source.time = asi.sourceClaim.Source.time + (currentTime - asi.playTime);
                    */
                    
                    asi.sourceClaim.Source.pitch = timeDelta / deltaTime;             

                    if (currentTime >= asi.playTime)   
                    {
                        if (!asi.sourceClaim.Source.isPlaying)
                        {
                            asi.sourceClaim.Source.clip = asi.Clip; 
                            asi.sourceClaim.Source.Play();
                        }
                    } 
                    else
                    {
                        asi.sourceClaim.Source.Pause();
                    }
                    asi.sourceClaim.Source.timeSamples = Mathf.Clamp(Mathf.FloorToInt(((currentTime - asi.playTime) / asi.Clip.length) * asi.Clip.samples), 0, asi.Clip.samples - 1);     
                }
            } 
            catch(Exception ex)
            {
                swole.LogError(ex);
            }
        }
        protected static void PauseAudioSyncImports(AnimationSource source)
        {
            try
            {
                if (source == null) return;

                for (int a = 0; a < source.AudioSyncImportsCount; a++)
                {
                    var asi = source.GetAudioSyncImport(a);
                    if (asi == null || asi.sourceClaim == null || !asi.sourceClaim.IsValid) continue; 

                    asi.sourceClaim.Source.Pause();
                }
            }
            catch (Exception ex)
            {
                swole.LogError(ex);
            }
        }
        protected static void StopAudioSyncImports(AnimationSource source)
        {
            try
            {
                if (source == null) return; 

                for (int a = 0; a < source.AudioSyncImportsCount; a++)
                {
                    var asi = source.GetAudioSyncImport(a);
                    if (asi == null || asi.sourceClaim == null) continue;

                    asi.sourceClaim.Release();
                    asi.sourceClaim = null;
                }
            }
            catch (Exception ex)
            {
                swole.LogError(ex);
            }
        }

        #endregion

        #region Camera Settings

        private const string str_mode = "mode";
        private const string str_speed = "speed";
        private const string str_fov = "fov";

        public RectTransform cameraSettingsWindow;

        [Serializable]
        public enum CameraMode
        {
            Editor, Game
        }

        [Serializable]
        public struct CameraSettings
        {
            public CameraMode mode;
            public float fieldOfView;
            public float speed;

            public override bool Equals(object obj)
            {
                if (obj is CameraSettings settings) return this == settings;
                return base.Equals(obj);
            }

            public override int GetHashCode() => base.GetHashCode();

            public static bool operator ==(CameraSettings settA, CameraSettings settB) 
            {
                if (settA.mode != settB.mode) return false;
                if (settA.fieldOfView != settB.fieldOfView) return false;
                if (settA.speed != settB.speed) return false;

                return true;
            }
            public static bool operator !=(CameraSettings settA, CameraSettings settB) => !(settA == settB);
        }

        public void SetCameraMode(CameraMode mode)
        {
            var settings = GetCameraSettings();
            settings.mode = mode;
            SetCameraSettings(settings);
        }
        public void SetCameraMode(string mode)
        {
            if (Enum.TryParse<CameraMode>(mode, true, out var val)) SetCameraMode(val); 
        }

        public void SetCameraSpeed(float speed)
        {
            var settings = GetCameraSettings();
            settings.speed = speed;
            SetCameraSettings(settings);
        }
        public void SetCameraSpeed(string speed)
        {
            if (float.TryParse(speed, out var val)) SetCameraSpeed(val);
        }

        public void SetCameraFOV(float fieldOfView)
        {
            var settings = GetCameraSettings();
            settings.fieldOfView = fieldOfView;
            SetCameraSettings(settings);
        }
        public void SetCameraFOV(string fieldOfView)
        {
            if (float.TryParse(fieldOfView, out var val)) SetCameraFOV(val); 
        }

        public void OpenCameraSettingsWindow(RectTransform window)
        {
            var sesh = CurrentSession;
            if (sesh == null) return;

            window.gameObject.SetActive(true);
            SyncCameraSettingsWindow(window, CurrentSession.CameraSettings);
        }
        public void SyncCameraSettingsWindow(RectTransform window, CameraSettings settings)
        {
            if (window != null)
            {
                var mode = window.FindDeepChildLiberal(str_mode);
                if (mode != null)
                {
                    var dropdown = mode.GetComponentInChildren<UIDynamicDropdown>(true);
                    if (dropdown != null) dropdown.SetSelectionText(settings.mode.ToString().ToLower(), false);
                }

                CustomEditorUtils.SetInputFieldTextByName(window, str_speed, settings.speed.ToString(), true, true);
                CustomEditorUtils.SetInputFieldTextByName(window, str_fov, settings.fieldOfView.ToString(), true, true);
            }
        }
        public void SetCameraSettings(CameraSettings settings)
        {
            if (settings.speed <= 0) settings.speed = 1;
            if (settings.fieldOfView <= 0) settings.fieldOfView = CameraProxy._defaultFieldOfView;

            if (runtimeEditor != null)
            {
                runtimeEditor.CameraSpeed = settings.speed;
                runtimeEditor.CameraFOV = settings.fieldOfView;
            }

            var session = CurrentSession;
            if (session != null) session.CameraSettings = settings;

            SyncCameraSettingsWindow(cameraSettingsWindow, settings); 
        }
        public CameraSettings GetCameraSettings()
        {
            var session = CurrentSession;
            if (session != null)
            {
               return session.CameraSettings;
            }

            return new CameraSettings() { fieldOfView = CameraProxy._defaultFieldOfView, speed = 1 };
        }

        #endregion

        #region Misc Settings

        public Button floorOnButton;
        public Button floorOffButton;
        
        public void SetMiscSettings(Session.MiscSettings settings)
        {
            if (floorOnButton != null) floorOnButton.gameObject.SetActive(!settings.hideFloor); 
            if (floorOffButton != null) floorOffButton.gameObject.SetActive(settings.hideFloor);
            if (floorTransform != null) floorTransform.gameObject.SetActive(!settings.hideFloor);

            var session = CurrentSession;
            if (session != null) session.Settings = settings;
        }
        public Session.MiscSettings GetSetMiscSettings()
        {
            var session = CurrentSession;
            if (session != null)
            {
                return session.Settings;
            }
            
            return Session.MiscSettings.Default;
        }

        #endregion

        [Header("Elements")]
        public GameObject playModeDropdownRoot;
        public GameObject keyframePrototype;
        protected PrefabPool keyframePool;
        [Range(0, 1)]
        public float keyframeAnchorY = 0.5f; 

        public RectTransform contextMenuMain;
        public const string _moveUpContextMenuOptionName = "MoveUp";
        public const string _moveDownContextMenuOptionName = "MoveDown";
        public const string _editPhysiqueContextMenuOptionName = "EditPhysique";
        public const string _editSettingsContextMenuOptionName = "EditSettings";
        public const string _alignToViewContextMenuOptionName = "AlignToView";
        public const string _alignViewContextMenuOptionName = "AlignView";
        public void OpenContextMenuMain() => OpenContextMenuMain(CursorProxy.ScreenPosition);
        public void OpenContextMenuMain(Vector3 cursorScreenPosition)
        {
            if (contextMenuMain == null) return;

            contextMenuMain.gameObject.SetActive(true);

            var canvas = contextMenuMain.GetComponentInParent<Canvas>(true);
            contextMenuMain.position = canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, cursorScreenPosition));

            var menuTransform = contextMenuMain.transform;
            for(int a = 0; a < menuTransform.childCount; a++)
            {
                var child = menuTransform.GetChild(a);
                child.gameObject.SetActive(false);
            }
        }

        public GameObject boneDropdownListRoot;
        protected UIRecyclingList boneDropdownList;
        public UIRecyclingList BoneDropdownList
        {
            get
            {
                if (boneDropdownList == null) boneDropdownList = boneDropdownListRoot == null ? null : boneDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneDropdownList;
            }
        }
        public void RefreshBoneDropdown()
        {
            if (BoneDropdownList == null || !BoneCurveDropdownList.gameObject.activeSelf) return;
            boneDropdownList.Clear();

            var activeAnimator = ActiveAnimator;

            for (int a = 0; a < poseableBones.Count; a++)
            {
                int i = a;
                var bone = poseableBones[a];
                if (bone.transform == null) continue;
                boneDropdownList.AddNewMember(bone.name, () => SetSelectedBoneInUI(i));  
            }
            boneDropdownList.Refresh();

            string selectedBoneName = "null";
            if (selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count) 
            {
                var bone = poseableBones[selectedBoneUI];
                if (bone != null && bone.transform != null) selectedBoneName = bone.name; 
            }
            SetComponentTextByName(boneDropdownListRoot, _currentTextObjName, selectedBoneName);
        }

        public GameObject boneCurveDropdownListRoot;
        protected UIRecyclingList boneCurveDropdownList;
        public UIRecyclingList BoneCurveDropdownList
        {
            get
            {
                if (boneCurveDropdownList == null) boneCurveDropdownList = boneCurveDropdownListRoot == null ? null : boneCurveDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneCurveDropdownList;
            }
        }
        [Serializable]
        public enum TransformProperty
        {
            localPosition_x, localPosition_y, localPosition_z, localRotation_x, localRotation_y, localRotation_z, localRotation_w, localScale_x, localScale_y, localScale_z
        }
        protected TransformProperty selectedTransformPropertyUI;
        public void SetSelectedTransformPropertyUI(TransformProperty property)
        {
            selectedTransformPropertyUI = property;
            SetComponentTextByName(boneCurveDropdownListRoot, _currentTextObjName, property.ToString());
            RefreshTimeline();
        }
        public void RefreshBoneCurveDropdown()
        {
            if (BoneCurveDropdownList == null) return;
            boneCurveDropdownList.Clear();
            
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_z));

            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_z));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_w.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_w));

            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_z));

            boneCurveDropdownList.Refresh();

            SetComponentTextByName(boneCurveDropdownListRoot, _currentTextObjName, selectedTransformPropertyUI.ToString());
        }
        public void RefreshBoneCurveDropdowns()
        {
            RefreshBoneDropdown();
            RefreshBoneCurveDropdown();
        }

        protected int selectedBoneUI;
        public int SelectedBoneUI
        {
            get => selectedBoneUI;
            set => SetSelectedBoneInUI(value);
        }
        protected const string _currentTextObjName = "Current-Text"; 
        public void SetSelectedBoneInUI(int index)
        {
            selectedBoneUI = index;

            if (boneDropdownListRoot != null)
            {
                string boneName = "null";
                if (poseableBones != null && index >= 0 && index < poseableBones.Count)
                {
                    var bone = poseableBones[index];
                    if (bone != null) boneName = bone.name;
                }
                SetComponentTextByName(boneDropdownListRoot, _currentTextObjName, boneName); 
            }

            RefreshTimeline(); 
        }

        public GameObject boneGroupDropdownListRoot;
        protected UIRecyclingList boneGroupDropdownList;
        public UIRecyclingList BoneGroupDropdownList
        {
            get
            {
                if (boneGroupDropdownList == null) boneGroupDropdownList = boneGroupDropdownListRoot == null ? null : boneGroupDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneGroupDropdownList;
            }
        }
        [Serializable]
        public enum BoneKeyGroup
        {
            All, LocalPosition, LocalRotation, LocalScale
        }
        protected BoneKeyGroup selectedBoneKeyGroupUI;
        public void SetSelectedBoneKeyGroupUI(BoneKeyGroup group)
        {
            selectedBoneKeyGroupUI = group;
            SetComponentTextByName(boneGroupDropdownListRoot, _currentTextObjName, group.ToString());
            RefreshTimeline();
        }
        public void RefreshBoneKeyGroupDropdown()
        {
            if (BoneGroupDropdownList == null) return;
            boneGroupDropdownList.Clear();

            boneGroupDropdownList.AddNewMember(BoneKeyGroup.All.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.All));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalPosition.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalPosition));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalRotation.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalRotation));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalScale.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalScale));

            boneGroupDropdownList.Refresh();

            SetComponentTextByName(boneGroupDropdownListRoot, _currentTextObjName, selectedBoneKeyGroupUI.ToString());
        }
        public void RefreshBoneKeyDropdowns()
        {
            RefreshBoneDropdown();
            RefreshBoneKeyGroupDropdown();
        }

        public GameObject propertyDropdownListRoot;
        protected UIRecyclingList propertyDropdownList;
        public UIRecyclingList PropertyDropdownList
        {
            get
            {
                if (propertyDropdownList == null) propertyDropdownList = propertyDropdownListRoot == null ? null : propertyDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return propertyDropdownList;
            }
        }

        public void RefreshPropertyDropdown()
        {
            if (PropertyDropdownList == null) return;
            propertyDropdownList.Clear();

            var animatable = ActiveAnimatable;

            if (animatable != null)
            {
                /*if (animatable.animator != null)
                {
                    if (animatable.animator.IkManager != null)
                    {
                        var ikProxy = animatable.animator.IkManager.GetComponent<IKControlProxy>();
                        if (ikProxy != null) 
                        {
                            var proxyType = ikProxy.GetType();
                            var props = proxyType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                            foreach (var prop in props)
                            {
                                if (prop == null || !Attribute.IsDefined(prop, typeof(AnimatablePropertyAttribute))) continue; 

                                var attr = (AnimatablePropertyAttribute)Attribute.GetCustomAttribute(prop, typeof(AnimatablePropertyAttribute));

                                string displayName = ikPropertyPrefix + prop.Name;
                                string id = $"{IAnimator._animatorTransformPropertyStringPrefix}.{proxyType.Name}.{prop.Name}"; 
                                propertyDropdownList.AddNewMember(displayName, () => SetSelectedComponentPropertyUI(displayName, id));

                                string lower_name = prop.Name.ToLower();
                                if (attr.hasDefaultValue) 
                                { 
                                    propertyValueDefaults[id] = attr.defaultValue; 
                                }
                                else
                                {
                                    propertyValueDefaults[id] = attr.defaultValue;
                                }
                            }
                        }
                    }

                    var flexProxy = animatable.animator.GetComponent<MuscleFlexProxy>();
                    if (flexProxy != null)
                    {
                        var props = flexProxy.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        //string prefix = flexPropertyPrefix.ToLower();
                        foreach(var prop in props)
                        {
                            if (prop == null || !Attribute.IsDefined(prop, typeof(AnimatablePropertyAttribute))) continue;
                            //int prefixStart = prop.Name.ToLower().IndexOf(prefix); 
                            //if (prefixStart < 0) continue;
                            string displayName = prop.Name;//.Substring(prefixStart + prefix.Length);
                            string id = $"{IAnimator._animatorTransformPropertyStringPrefix}.{nameof(MuscleFlexProxy)}.{prop.Name}";
                            propertyDropdownList.AddNewMember(displayName, () => SetSelectedComponentPropertyUI(displayName, id));
                        }
                    }
                }*/
                for (int a = 0; a < animatable.AnimatablePropertyCount; a++)  
                {
                    var prop = animatable.GetAnimatableProperty(a);
                    if (string.IsNullOrEmpty(prop.id) || string.IsNullOrEmpty(prop.displayName)) continue;  

                    propertyDropdownList.AddNewMember(prop.displayName, () => SetSelectedComponentPropertyUI(prop.displayName, prop.id));   
                }
            }
            
            propertyDropdownList.Refresh();

            SetComponentTextByName(propertyDropdownListRoot, _currentTextObjName, string.IsNullOrWhiteSpace(selectedPropertyUI) ? "null" : selectedPropertyUI);
        }
        protected string selectedPropertyUI;
        protected string selectedPropertyId;
        public void SetSelectedComponentPropertyUI(string displayName, string id)
        {
            selectedPropertyUI = displayName;
            selectedPropertyId = id;
            SetComponentTextByName(propertyDropdownListRoot, _currentTextObjName, string.IsNullOrWhiteSpace(selectedPropertyUI) ? "null" : selectedPropertyUI);
            RefreshTimeline();
        }

        public static AnimationCurve DefaultLinearTimeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public UIAnimationCurveRenderer curveRenderer;
        public void RefreshCurveRenderer(bool rebuild=false)
        {
            if (curveRenderer == null) return;

            if (timelineWindow != null)
            {
                float start = 0;
                float end = timelineWindow.Length;

                if (editMode != EditMode.GLOBAL_TIME && curveRenderer.curve != null && curveRenderer.curve.length > 1)
                {
                    var keyStart = curveRenderer.curve[0];
                    var keyEnd = curveRenderer.curve[curveRenderer.curve.length - 1];

                    start = keyStart.time;
                    end = keyEnd.time;
                }

                float p1 = timelineWindow.GetVisibleTimelineNormalizedPosition(start);
                float p2 = timelineWindow.GetVisibleTimelineNormalizedPosition(end);

                var rT = curveRenderer.RectTransform;
                rT.anchorMin = new Vector2(p1, 0);
                rT.anchorMax = new Vector2(p2, 1);
                var sizeDelta = rT.sizeDelta;
                sizeDelta.x = 0;
                rT.sizeDelta = sizeDelta;
                var pos = rT.anchoredPosition3D;
                pos.x = 0;
                rT.anchoredPosition3D = pos;
            }

            if (rebuild) curveRenderer.Rebuild();
        }
        public void RefreshTimeline()
        {
            if (timelineWindow == null) return;

            var animatable = ActiveAnimatable;
            bool flag = animatable == null || animatable.editIndex < 0 || animatable.animationBank == null || animatable.editIndex >= animatable.animationBank.Count;
            var source = flag ? null : animatable.animationBank[animatable.editIndex];
            flag = flag || source == null;

            if (!flag) timelineWindow.SetLength(source.timelineLength, false);

            var anim = flag ? null : source.rawAnimation;
            flag = flag || anim == null;

            if (flag || editMode == EditMode.GLOBAL_TIME || editMode == EditMode.ANIMATION_EVENTS)
            {
                timelineWindow.SetTimeCurve(null);
            }
            else
            {
                timelineWindow.SetTimeCurve(!useTimeCurve || anim.timeCurve == null || anim.timeCurve.length <= 0 ? DefaultLinearTimeCurve : anim.timeCurve);  
            }

            switch (editMode)
            {
                case EditMode.GLOBAL_TIME:
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(true);
                        curveRenderer.SetLineColor(globalTimeCurveColor);
                        curveRenderer.curve = timelineWindow.TimeCurve;
                        RedrawAllCurves();

                        if (anim != null)
                        {
                            SetButtonOnClickAction(curveRenderer.gameObject, () =>
                            {
                                if (anim.timeCurve == null || anim.timeCurve.length <= 0) 
                                { 
                                    curveRenderer.curve = timelineWindow.TimeCurve = anim.timeCurve = EditableAnimationCurve.Linear(0, 0, 1, 1);// AnimationCurve.Linear(0, 0, 1, 1);
                                    source.MarkAsDirty(); 
                                }
                                StartEditingCurve(anim.timeCurve, true, true);
                            });
                        } 
                        else
                        {
                            SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                        }
                    }
                    break;
                case EditMode.GLOBAL_KEYS:
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(false);
                        SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false); 
                    }
                    break;
                case EditMode.SELECTED_BONE_KEYS:
                case EditMode.BONE_KEYS:
                    RefreshBoneKeyDropdowns();
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(false);
                        SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                    }
                    break;
                case EditMode.BONE_CURVES:
                    RefreshBoneCurveDropdowns();
                    if (curveRenderer != null)
                    {
                        flag = selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count && animatable != null && source != null;
                        var bone = flag ? poseableBones[selectedBoneUI] : null;
                        flag = flag && bone != null && bone.transform != null; 
                        if (flag)
                        {
                            AnimationUtils.GetOrCreateTransformCurve(out var transformCurve, bone.name, source.GetOrCreateRawData(), animatable.RestPose);

                            curveRenderer.gameObject.SetActive(true);
                            curveRenderer.SetLineColor(rawCurveColor);

                            EditableAnimationCurve animCurve = null;
                            if (transformCurve != null && typeof(TransformCurve).IsAssignableFrom(transformCurve.GetType()))
                            {
                                TransformCurve transformCurve_ = (TransformCurve)transformCurve;
                                switch (selectedTransformPropertyUI)
                                {
                                    case TransformProperty.localPosition_x:
                                        animCurve = transformCurve_.localPositionCurveX;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localPositionCurveX = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localPosition_y:
                                        animCurve = transformCurve_.localPositionCurveY;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localPositionCurveY = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localPosition_z:
                                        animCurve = transformCurve_.localPositionCurveZ;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localPositionCurveZ = animCurve;
                                        }
                                        break;

                                    case TransformProperty.localRotation_x:
                                        animCurve = transformCurve_.localRotationCurveX;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localRotationCurveX = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localRotation_y:
                                        animCurve = transformCurve_.localRotationCurveY;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localRotationCurveY = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localRotation_z:
                                        animCurve = transformCurve_.localRotationCurveZ;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localRotationCurveZ = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localRotation_w:
                                        animCurve = transformCurve_.localRotationCurveW;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localRotationCurveW = animCurve;
                                        }
                                        break;

                                    case TransformProperty.localScale_x:
                                        animCurve = transformCurve_.localScaleCurveX;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localScaleCurveX = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localScale_y:
                                        animCurve = transformCurve_.localScaleCurveY;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localScaleCurveY = animCurve;
                                        }
                                        break;
                                    case TransformProperty.localScale_z:
                                        animCurve = transformCurve_.localScaleCurveZ;
                                        if (animCurve == null)
                                        {
                                            animCurve = new EditableAnimationCurve();
                                            transformCurve_.localScaleCurveZ = animCurve;
                                        }
                                        break;
                                }
                            } 

                            curveRenderer.curve = animCurve;
                            RedrawAllCurves();

                            if (animCurve != null)
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, () =>
                                {
                                    StartEditingCurve(animCurve, true, true);
                                });
                            }
                            else
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                            }
                        } 
                        else
                        {
                            curveRenderer.curve = null;
                            RedrawAllCurves();
                            curveRenderer.gameObject.SetActive(false);
                        }
                    }
                    break;
                case EditMode.PROPERTY_CURVES:
                    RefreshPropertyDropdown();
                    if (curveRenderer != null)
                    {
                        flag = !string.IsNullOrWhiteSpace(selectedPropertyId) && animatable != null && source != null;
                        if (flag)
                        {
                            AnimationUtils.GetOrCreatePropertyCurve(out var propertyCurve, selectedPropertyId, source.GetOrCreateRawData(), animatable.RestPose);  

                            curveRenderer.gameObject.SetActive(true);
                            curveRenderer.SetLineColor(rawCurveColor);

                            EditableAnimationCurve animCurve = null;
                            if (propertyCurve != null && typeof(PropertyCurve).IsAssignableFrom(propertyCurve.GetType()))
                            {
                                PropertyCurve propertyCurve_ = (PropertyCurve)propertyCurve;
                                animCurve = propertyCurve_.propertyValueCurve;
                                if (animCurve == null)
                                {
                                    animCurve = new EditableAnimationCurve();
                                    propertyCurve_.propertyValueCurve = animCurve;
                                }
                            }

                            curveRenderer.curve = animCurve;
                            RedrawAllCurves();

                            if (animCurve != null)
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, () =>
                                {
                                    StartEditingCurve(animCurve, true, true); 
                                });
                            }
                            else
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, null);
                            }
                        }
                        else
                        {
                            curveRenderer.curve = null;
                            RedrawAllCurves();
                            curveRenderer.gameObject.SetActive(false);
                        }
                    }
                    break;
                case EditMode.ANIMATION_EVENTS:
                    if (curveRenderer != null)
                    {
                        SetButtonOnClickAction(curveRenderer.gameObject, OpenAnimationEventsWindow); // Use curve renderer object as a button to open animation event window, even though we're not rendering a curve.

                        curveRenderer.curve = null;
                        RedrawAllCurves();
                        curveRenderer.gameObject.SetActive(true);
                    }
                    break;
            }

            RefreshKeyframes();
        }
        protected void OnSetTimelineSize()
        {
            RefreshCurveRenderer(false);
            RefreshKeyframePositions();
        }
        protected void OnSetTimelineLength(float length)
        {
            RefreshKeyframes();

            var source = CurrentSource;
            if (source != null) source.timelineLength = length;
        }

        protected void StartEditingCurve(EditableAnimationCurve curve, bool notifyListeners = true, bool undoable=false)
        {
            if (curve == null) return;
            var editor = CurveEditor;
            if (editor == null) return;

            if (undoable) RecordRevertibleAction(new UndoableStartEditingCurve() { editor = this, curve = curve });
            if (curveEditorWindow != null)
            {
                ignoreUndoHistoryRecording = 2;
                curveEditorWindow.gameObject.SetActive(true);
                ignoreUndoHistoryRecording = 0;
                var popup = curveEditorWindow.GetComponent<UIPopup>();
                if (popup != null)
                {
                    if (popup.OnClose == null) popup.OnClose = new UnityEvent(); else popup.OnClose.RemoveAllListeners();
                    popup.OnClose.AddListener(() => StopEditingCurve(true));
                }
            }
            //if (editor.Curve == curve.Instance) return;
            if (editor is SwoleCurveEditor swoleCurveEditor) 
            {
                ignoreUndoHistoryRecording = 2;
                swoleCurveEditor.SetCurve(curve, notifyListeners);
                ignoreUndoHistoryRecording = 0;
            }
            else
            {
                ignoreUndoHistoryRecording = 3;
                editor.SetCurve(curve);
                editor.SetState(curve, false, true);
                ignoreUndoHistoryRecording = 0;
            }
        }
        protected void StopEditingCurve(bool undoable = false)
        {
            if (curveEditorWindow != null) 
            {
                if (undoable && CurveEditor != null) 
                {
                    if (curveEditor is SwoleCurveEditor sce && sce.EditableCurve != null)
                    {
                        RecordRevertibleAction(new UndoableStopEditingCurve() { editor = this, curve = sce.EditableCurve }); 
                    }
                    else
                    {
                        RecordRevertibleAction(new UndoableStopEditingCurve() { editor = this, curve = new EditableAnimationCurve(curveEditor.CurrentState, curveEditor.Curve) });
                    }
                }
                curveEditorWindow.gameObject.SetActive(false); 
            }
            lastEditedCurve = null; 
        }

        [SerializeField]
        protected RectTransform keyContainerTransform;
        public RectTransform KeyContainerTransform
        {
            get
            {
                if (keyContainerTransform == null) 
                {
                    if (timelineWindow != null) keyContainerTransform = timelineWindow.ContainerTransform;
                    if (keyContainerTransform == null) keyContainerTransform = gameObject.AddOrGetComponent<RectTransform>();
                }
                return keyContainerTransform;
            }
        }

        public GameObject loopButton;
        public GameObject timeCurveButton; 
        public GameObject recordButton;
        public GameObject playButton;
        public GameObject pauseButton;
        public GameObject stopButton;
        public GameObject firstFrameButton;
        public GameObject prevFrameButton;
        public GameObject nextFrameButton;
        public GameObject lastFrameButton;

        public GameObject undoButton;
        public GameObject redoButton;

        [Header("Bone Rendering")]
        public int boneOverlayLayer = 29; 

        public GameObject boneRootPrototype;
        public GameObject boneLeafPrototype;

        private PrefabPool boneRootPool;
        private PrefabPool boneLeafPool;

        [Header("Events"), SerializeField]
        private UnityEvent OnSetupSuccess = new UnityEvent();
        [SerializeField]
        private UnityEvent OnSetupFail = new UnityEvent();

        [Header("Misc")]
        public Transform floorTransform;
        public void SetFloorVisibility(bool visible)
        {
            var settings = GetSetMiscSettings();
            settings.hideFloor = !visible;
            SetMiscSettings(settings);
        }

        public Color globalTimeCurveColor = Color.white;
        public Color rawCurveColor = Color.white;

        public Color keyColor = Color.white;
        public Color selectedKeyColor = Color.cyan;

        public Text setupErrorTextOutput;
        public TMP_Text setupErrorTextOutputTMP;

        public Sprite icon_humanoidRig;
        public Sprite icon_mechanicalRig;

        protected Sprite GetRigIcon(AnimatableAsset.ObjectType type)
        {
            switch (type)
            {
                case AnimatableAsset.ObjectType.Humanoid:
                    return icon_humanoidRig;

                case AnimatableAsset.ObjectType.Mechanical:
                    return icon_mechanicalRig;
            }

            return null;
        }

        protected void ShowSetupError(string msg)
        {

            if (setupErrorTextOutput != null)
            {
                setupErrorTextOutput.gameObject.SetActive(true);
                setupErrorTextOutput.enabled = true;
                setupErrorTextOutput.text = msg;
            }

            if (setupErrorTextOutputTMP != null)
            {
                setupErrorTextOutputTMP.gameObject.SetActive(true);
                setupErrorTextOutputTMP.enabled = true;
                setupErrorTextOutputTMP.text = msg;
            }

        }

        protected EditableAnimationCurve lastEditedCurve;

        protected bool initialized;
        public bool IsInitialized => initialized;

        protected virtual void Awake()
        {

            swole.Register(this);

            void FailSetup(string msg)
            {
                swole.LogError(msg); 
                OnSetupFail?.Invoke();
                ShowSetupError(msg);
                Destroy(this);
            }

            if (animatableImporterWindow == null)
            {
                FailSetup($"Animatable Importer Window not set for Animation Editor '{name}'");
                return;
            }

            if (importedAnimatablesList == null)
            {
                FailSetup($"Imported Animatables List not set for Animation Editor '{name}'");
                return;
            }

            if (boneRootPrototype == null)
            {
                FailSetup($"Bone Root Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (boneLeafPrototype == null)
            {
                FailSetup($"Bone Leaf Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (keyframePrototype == null)
            {
                FailSetup($"Keyframe Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (CurveEditor == null)
            {
                FailSetup($"Curve Editor not set for Animation Editor '{name}'");
                return;
            }
            
            if (curveEditor.OnStateChange == null) curveEditor.OnStateChange = new UnityEvent<AnimationCurveEditor.State, AnimationCurveEditor.State>();
            curveEditor.OnStateChange.AddListener((AnimationCurveEditor.State oldState, AnimationCurveEditor.State newState) =>
            {
                EditableAnimationCurve editedCurve = null;// curveEditor.Curve;
                if (curveEditor is SwoleCurveEditor sce)
                {
                    //bool setCurve = sce.EditableCurve == null;
                    //editedCurve = setCurve ? new EditableAnimationCurve(newState, sce.Curve) : sce.EditableCurve;
                    //if (setCurve) sce.SetCurve(editedCurve, false);
                    editedCurve = sce.EditableCurve;
                }
                else
                {
                    editedCurve = new EditableAnimationCurve(newState, curveEditor.Curve);
                }

                RecordRevertibleAction(new ChangeStateAction() 
                { 
                    
                    curveEditor = curveEditor, oldState = oldState, newState = newState,

                    onChange = (bool undo) =>
                    {
                        if (editedCurve != null) 
                        { 
                            StartEditingCurve(editedCurve, false, false);

                            IEnumerator Delayed()
                            {
                                yield return null;
                                RefreshCurveRenderer(true);
                                if (editMode == EditMode.BONE_CURVES || editMode == EditMode.PROPERTY_CURVES)
                                {
                                    RefreshKeyframes();
                                }
                            }

                            StartCoroutine(Delayed());
                        }
                    }
                
                });
                
                if (ReferenceEquals(lastEditedCurve, editedCurve))
                {
                    var activeSource = CurrentSource;
                    if (activeSource != null && activeSource.ContainsCurve(editedCurve))
                    {
                        activeSource.MarkAsDirty(); 
                    }
                }
                lastEditedCurve = editedCurve;

                IEnumerator Delayed()
                {
                    yield return null;
                    RefreshCurveRenderer(true);
                    if (editMode == EditMode.BONE_CURVES || editMode == EditMode.PROPERTY_CURVES)
                    {
                        RefreshKeyframes();
                    }
                }

                StartCoroutine(Delayed());

            }); 

            if (timelineWindow != null)
            {
                if (timelineWindow.OnSetScrubPosition == null) timelineWindow.OnSetScrubPosition = new UnityEvent<float>();
                timelineWindow.OnSetScrubPosition.AddListener(PlaySnapshotUnclamped);

                if (timelineWindow.OnResize == null) timelineWindow.OnResize = new UnityEvent();
                timelineWindow.OnResize.AddListener(OnSetTimelineSize);

                if (timelineWindow.OnSetLength == null) timelineWindow.OnSetLength = new UnityEvent<float>();
                timelineWindow.OnSetLength.AddListener(OnSetTimelineLength);
            }

            if (boneRootPool == null) boneRootPool = new GameObject("boneRootPool").AddComponent<PrefabPool>();
            boneRootPool.transform.SetParent(transform, false);
            boneRootPool.Reinitialize(boneRootPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);

            if (boneLeafPool == null) boneLeafPool = new GameObject("boneLeafPool").AddComponent<PrefabPool>();
            boneLeafPool.transform.SetParent(transform, false);
            boneLeafPool.Reinitialize(boneLeafPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);

            if (keyframePool == null) keyframePool = new GameObject("keyframePool").AddComponent<PrefabPool>();
            keyframePool.transform.SetParent(transform, false);
            keyframePool.Reinitialize(keyframePrototype, PoolGrowthMethod.Incremental, 1, 1, 1024);
            keyframePool.SetContainerTransform(KeyContainerTransform);

            if (loopButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(loopButton, ToggleLooping, true, true, false); 
                SetLooping(false, false);
            }
            if (timeCurveButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(timeCurveButton, ToggleUseTimeCurve, true, true, false);
                SetUseTimeCurve(true, false);  
            }
            if (recordButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(recordButton, ToggleRecording, true, true, false);
                SetRecording(false, false);
            }
            if (playButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(playButton, PlayDefault, true, true, false);
                pauseButton.SetActive(true);
            }
            if (pauseButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(pauseButton, Pause, true, true, false); 
                pauseButton.SetActive(false);
            }
            if (stopButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(stopButton, StopAndReset, true, true, false);
                stopButton.SetActive(false);
            }
            if (firstFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(firstFrameButton, SkipToFirstFrame, true, true, false);
            }
            if (prevFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(prevFrameButton, SkipToPreviousFrame, true, true, false);
            }
            if (nextFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(nextFrameButton, SkipToNextFrame, true, true, false);
            }
            if (lastFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(lastFrameButton, SkipToFinalFrame, true, true, false);
            }

            if (string.IsNullOrEmpty(additiveEditorSetupScene))
            {
                string msg = "No additive editor setup scene was set. There will be no way to control the scene camera or to load and manipulate prefabs without it!";
                swole.LogWarning(msg);
                OnSetupFail?.Invoke();
                ShowSetupError(msg); 
            }
            else
            {

                try
                {

                    Camera mainCam = Camera.main;
                    EventSystem[] eventSystems = GameObject.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                    SceneManager.LoadScene(additiveEditorSetupScene, LoadSceneMode.Additive);
                    var scn = SceneManager.GetSceneByName(additiveEditorSetupScene);
                    if (scn.IsValid())
                    {

                        bool hadExistingCamera = false;
                        Vector3 existingCameraPos = Vector3.zero;
                        Quaternion existingCameraRot = Quaternion.identity;
                        if (mainCam != null)
                        {
                            hadExistingCamera = true;
                            var camT = mainCam.transform;
                            existingCameraPos = camT.position;
                            existingCameraRot = camT.rotation;
                            Destroy(mainCam.gameObject); // Make room for new main camera in setup scene
                        }
                        foreach (var system in eventSystems) if (system != null) Destroy(system.gameObject); // Remove any existing event systems and use the one in the setup scene

                        IEnumerator FindRuntimeEditor()
                        {
                            while (!scn.isLoaded)
                            {
                                yield return null;
                                scn = SceneManager.GetSceneByBuildIndex(scn.buildIndex); 
                            }

                            if (runtimeEditor == null) runtimeEditor = GameObject.FindFirstObjectByType<RuntimeEditor>();
                            if (runtimeEditor == null)
                            {
                                FailSetup("No RuntimeEditor object was found! Objects cannot be edited without it. The additive editor setup scene should contain one.");
                            }
                            else
                            {
                                runtimeEditor.OnPreSelect += OnPreSelectCustomize;
                                runtimeEditor.OnSelectionChanged += OnSelectionChange;
                                runtimeEditor.OnBeginManipulateTransforms += BeginManipulationAction;
                                runtimeEditor.OnManipulateTransformsStep += ManipulationActionStep;
                                runtimeEditor.OnManipulateTransforms += RecordManipulationAction; 
                                
                                runtimeEditor.DisableGrid = true;
                                runtimeEditor.DisableUndoRedo = true;
                                runtimeEditor.DisableGroupSelect = true;
                                runtimeEditor.DisableSelectionBoundingBox = true;

#if BULKOUT_ENV
                                var rtSelect = RTObjectSelection.Get;
                                if (rtSelect != null)
                                {
                                    rtSelect.Settings.SetObjectLayerSelectable(boneOverlayLayer, true); // Make bone overlay layer selectable 
                                }
#endif
                                 
                                if (hadExistingCamera)
                                {
                                    mainCam = Camera.main;
                                    if (mainCam != null)
                                    {
                                        var camT = mainCam.transform;
                                        camT.SetPositionAndRotation(existingCameraPos, existingCameraRot);
                                    }
                                }

                                initialized = true;
                                OnSetupSuccess.Invoke();
                            }

                        }

                        StartCoroutine(FindRuntimeEditor()); // Wait for scene to fully load, then find the runtime editor.

                    }
                    else
                    {
                        ShowSetupError($"Invalid scene name '{additiveEditorSetupScene}'");
                    }

                }
                catch (Exception ex)
                {
                    ShowSetupError(ex.Message);
                    throw ex;
                } 

            }

            SetEditMode(editMode, false);
            SetPlaybackMode(PlaybackMode, false); 

            RenderingControl.SetRenderAnimationBones(true);

            if (undoButton != null) CustomEditorUtils.SetButtonOnClickAction(undoButton, Undo);
            if (redoButton != null) CustomEditorUtils.SetButtonOnClickAction(redoButton, Redo);
            undoSystem.OnChangeHistoryPosition += (int a, int b) => RefreshUndoButtons();
            RefreshUndoButtons();  
        }
        protected virtual void Start()
        {
            PlaybackPosition = 0;

            RefreshRootObjects();

            ClearUndoHistory();
            undoSystem.maxHistorySize = 50;
        }

        protected void OnDestroy()
        {

            swole.Unregister(this); 

            if (currentSession != null) currentSession.Destroy();

            if (runtimeEditor != null)
            {
                runtimeEditor.OnPreSelect -= OnPreSelectCustomize;
                runtimeEditor = null;
            }

            DisposeColoredMeshes();

        }

        #region Keyframe Logic

        //public float KeyframeChunkSize => timelineWindow == null ? 0 : ((1f / timelineWindow.LengthInFrames) * timelineWindow.Length);

        protected static readonly List<ITransformCurve.Frame> _tempTransformFrames = new List<ITransformCurve.Frame>();
        protected static readonly List<IPropertyCurve.Frame> _tempPropertyFrames = new List<IPropertyCurve.Frame>();
        protected static readonly List<AnimationCurveEditor.KeyframeStateRaw> _tempKeyframes = new List<AnimationCurveEditor.KeyframeStateRaw>();
        protected static readonly List<EditableAnimationCurve> _tempCurves = new List<EditableAnimationCurve>();
        //protected static readonly List<CustomAnimation.Event> _tempEvents = new List<CustomAnimation.Event>();

        protected static readonly List<TransformCurve> _tempTransformCurves = new List<TransformCurve>();
        protected static readonly List<TransformLinearCurve> _tempTransformLinearCurves = new List<TransformLinearCurve>();  

        public delegate void EvaluateTransformLinearCurveKey(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited);
        public delegate void EvaluatePropertyLinearCurveKey(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited);
        public delegate void EvaluateAnimationCurveKey(EditableAnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<AnimationCurveEditor.KeyframeStateRaw> keyframesEdited);
        public delegate void EvaluateAnimationEvent(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited);

        public static void IterateTransformLinearCurves(ICollection<TransformLinearCurve> transformLinearCurves, EvaluateTransformLinearCurveKey evaluateKey, UndoableEditAnimationSourceData editRecord)
        {
            if (transformLinearCurves != null && evaluateKey != null)
            {
                foreach (var curve in transformLinearCurves)
                {
                    if (curve == null || curve.frames == null) continue;

                    editRecord?.SetOriginalRawTransformLinearCurveState(curve);
                    _tempTransformFrames.Clear();
                    _tempTransformFrames.AddRange(curve.frames);

                    evaluateKey.Invoke(curve, _tempTransformFrames);

                    _tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                    curve.frames = _tempTransformFrames.ToArray();
                    editRecord?.RecordRawTransformLinearCurveEdit(curve);
                }
                _tempTransformFrames.Clear();
            }
        }
        public static void IteratePropertyLinearCurves(ICollection<PropertyLinearCurve> propertyLinearCurves, EvaluatePropertyLinearCurveKey evaluateKey, UndoableEditAnimationSourceData editRecord)
        {
            if (propertyLinearCurves != null && evaluateKey != null)
            {
                foreach (var curve in propertyLinearCurves)
                {
                    if (curve == null || curve.frames == null) continue;

                    editRecord?.SetOriginalRawPropertyLinearCurveState(curve);
                    _tempPropertyFrames.Clear();
                    _tempPropertyFrames.AddRange(curve.frames);

                    evaluateKey.Invoke(curve, _tempPropertyFrames);

                    _tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                    curve.frames = _tempPropertyFrames.ToArray();
                    editRecord?.RecordRawPropertyLinearCurveEdit(curve);
                }
                _tempPropertyFrames.Clear();
            }
        }
        public static void IterateTransformCurves(ICollection<TransformCurve> transformCurves, EvaluateAnimationCurveKey evaluateKey, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord)
        {
            if (transformCurves != null && evaluateKey != null)
            {
                foreach (var curve in transformCurves) 
                {
                    if (curve == null) continue;

                    editRecord?.SetOriginalRawTransformCurveState(curve);
                    _tempCurves.Clear();

                    _tempCurves.Add(curve.localPositionCurveX);
                    _tempCurves.Add(curve.localPositionCurveY);
                    _tempCurves.Add(curve.localPositionCurveZ);

                    _tempCurves.Add(curve.localRotationCurveX);
                    _tempCurves.Add(curve.localRotationCurveY);
                    _tempCurves.Add(curve.localRotationCurveZ);
                    _tempCurves.Add(curve.localRotationCurveW); 

                    _tempCurves.Add(curve.localScaleCurveX);
                    _tempCurves.Add(curve.localScaleCurveY);
                    _tempCurves.Add(curve.localScaleCurveZ);

                    IterateCurves(_tempCurves, evaluateKey, getFrameIndex, null);

                    _tempCurves.Clear();
                    editRecord?.RecordRawTransformCurveEdit(curve);
                }
            }
        }
        public static void IteratePropertyCurves(ICollection<PropertyCurve> propertyCurves, EvaluateAnimationCurveKey evaluateKey, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord)
        {
            if (propertyCurves != null && evaluateKey != null)
            {
                foreach (var curve in propertyCurves) 
                {
                    if (curve == null) continue;

                    editRecord?.SetOriginalRawPropertyCurveState(curve);
                    _tempCurves.Clear();

                    _tempCurves.Add(curve.propertyValueCurve);   

                    IterateCurves(_tempCurves, evaluateKey, getFrameIndex, null);  

                    _tempCurves.Clear();
                    editRecord?.RecordRawPropertyCurveEdit(curve);
                }
            }
        }
        public static void IterateCurves(ICollection<EditableAnimationCurve> curves, EvaluateAnimationCurveKey evaluateKey, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord)
        {
            if (curves != null && evaluateKey != null)
            {
                foreach (var curve in curves)
                {
                    if (curve == null || curve.length <= 0) continue;

                    editRecord?.SetOriginalRawCurveState(curve);
                    _tempKeyframes.Clear();
                    _tempKeyframes.AddRange(curve.Keys);

                    evaluateKey.Invoke(curve, getFrameIndex, _tempKeyframes);

                    _tempKeyframes.Sort((AnimationCurveEditor.KeyframeStateRaw x, AnimationCurveEditor.KeyframeStateRaw y) => (int)Mathf.Sign(x.time - y.time));
                    curve.Keys = _tempKeyframes.ToArray();
                    editRecord?.RecordRawCurveEdit(curve);
                }
                _tempKeyframes.Clear();
            }
        }
        public static void IterateEvents(CustomAnimation animationEventSource, EvaluateAnimationEvent evaluateEvent, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord)
        {
            if (animationEventSource != null && evaluateEvent != null)
            {
                editRecord?.SetOriginalEvents(animationEventSource.events);
                _tempEvents.Clear();
                _tempEvents.AddRange(animationEventSource.events);
                foreach (var _event in animationEventSource.events)
                {
                    if (_event == null) continue;

                    editRecord?.SetOriginalEventState(_event);
                    evaluateEvent.Invoke(_event, getFrameIndex, _tempEvents);
                    editRecord?.RecordEventStateEdit(_event);
                }
                animationEventSource.events = _tempEvents.ToArray();
                _tempEvents.Clear();
                editRecord?.SetEditedEvents(animationEventSource.events);
            }
        }

        protected class TimelineKeyframe
        {
            public AnimationEditor editor;

            public int timelinePosition;

            protected bool selected;
            public bool IsSelected
            {
                get => selected;
                set
                {
                    if (value) Select(); else Deselect();
                }
            }
            public void Select() 
            { 
                selected = true;
                if (editor != null) Image.color = editor.selectedKeyColor;
            }
            public void Deselect()
            { 
                selected = false;
                if (editor != null) Image.color = editor.keyColor;
            }
            
            public GameObject instance;
            protected RectTransform rectTransform;
            public RectTransform RectTransform
            {
                get
                {
                    if (rectTransform == null) rectTransform = instance == null ? null : instance.GetComponent<RectTransform>();
                    return rectTransform;
                }
            }
            protected Image image;
            public Image Image
            {
                get
                {
                    if (image == null) image = instance == null ? null : instance.GetComponent<Image>();
                    return image;
                }
            }

            public List<TransformLinearCurve> transformLinearCurves;
            public List<PropertyLinearCurve> propertyLinearCurves;
            public List<EditableAnimationCurve> curves;
            public CustomAnimation animationEventSource;

            public void IterateTransformLinearCurves(EvaluateTransformLinearCurveKey evaluateKey, UndoableEditAnimationSourceData editRecord) => AnimationEditor.IterateTransformLinearCurves(transformLinearCurves, evaluateKey, editRecord);

            public void IteratePropertyLinearCurves(EvaluatePropertyLinearCurveKey evaluateKey, UndoableEditAnimationSourceData editRecord) => AnimationEditor.IteratePropertyLinearCurves(propertyLinearCurves, evaluateKey, editRecord);
            public void IterateCurves(EvaluateAnimationCurveKey evaluateKey, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord) => AnimationEditor.IterateCurves(curves, evaluateKey, getFrameIndex, editRecord);
            public void IterateEvents(EvaluateAnimationEvent evaluateEvent, TimelinePositionToFrameIndex getFrameIndex, UndoableEditAnimationSourceData editRecord) => AnimationEditor.IterateEvents(animationEventSource, evaluateEvent, getFrameIndex, editRecord);

            public void Relocate(UndoableEditAnimationSourceData editRecord, int newTimelinePosition, TimelinePositionToFrameIndex getFrameIndex, FrameIndexToTimelinePosition getTimelinePos)
            {
                if (transformLinearCurves != null)
                {
                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in keyframesEdited) if (frame.timelinePosition == timelinePosition) frame.timelinePosition = newTimelinePosition;
                    }
                    IterateTransformLinearCurves(Evaluate, editRecord);
                }
                if (propertyLinearCurves != null)
                {
                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in keyframesEdited) if (frame.timelinePosition == timelinePosition) frame.timelinePosition = newTimelinePosition;
                    }
                    IteratePropertyLinearCurves(Evaluate, editRecord);
                }

                float offset = (float)(getTimelinePos(newTimelinePosition) - getTimelinePos(timelinePosition));
                if (curves != null)
                {
                    void Evaluate(EditableAnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<AnimationCurveEditor.KeyframeStateRaw> keyframesEdited)
                    {
                        for (int a = 0; a < keyframesEdited.Count; a++)
                        {
                            var kf = keyframesEdited[a];
                            if (getFrameIndex((decimal)kf.time) == timelinePosition)
                            {
                                kf.time = kf.time + offset;
                                keyframesEdited[a] = kf;
                            }
                        }
                    }
                    IterateCurves(Evaluate, getFrameIndex, editRecord);
                }
                if (animationEventSource != null)
                {
                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (getFrameIndex((decimal)_event.TimelinePosition) == timelinePosition)
                        {
                            _event.TimelinePosition = _event.TimelinePosition + offset; 
                        }
                    }
                    IterateEvents(Evaluate, getFrameIndex, editRecord);
                }

                timelinePosition = newTimelinePosition;
            }
            public void Delete(UndoableEditAnimationSourceData editRecord, TimelinePositionToFrameIndex getFrameIndex)
            {
                if (transformLinearCurves != null)
                {
                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => i.timelinePosition == timelinePosition);
                    }
                    IterateTransformLinearCurves(Evaluate, editRecord);
                }
                if (propertyLinearCurves != null)
                {
                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => i.timelinePosition == timelinePosition);
                    }
                    IteratePropertyLinearCurves(Evaluate, editRecord);
                }
                if (curves != null)
                {
                    void Evaluate(EditableAnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<AnimationCurveEditor.KeyframeStateRaw> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => getFrameIndex((decimal)i.time) == timelinePosition);
                    }
                    IterateCurves(Evaluate, getFrameIndex, editRecord);
                }
                if (animationEventSource != null)
                {
                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (_event == null) return;
                        eventsEdited.RemoveAll(i => getFrameIndex((decimal)_event.TimelinePosition) == timelinePosition);
                    }
                    IterateEvents(Evaluate, getFrameIndex, editRecord);
                }
            }
            public void CopyPaste(UndoableEditAnimationSourceData editRecord, TimelineKeyframe copy, float offset, TimelinePositionToFrameIndex getFrameIndex)
            {
                int frameOffset = copy.timelinePosition - timelinePosition;

                if (transformLinearCurves != null)
                {
                    if (copy.transformLinearCurves == null) copy.transformLinearCurves = new List<TransformLinearCurve>();
                    copy.transformLinearCurves.AddRange(transformLinearCurves);

                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in curve.frames)
                        {
                            if (frame.timelinePosition != timelinePosition) continue;

                            var frameCopy = frame.Duplicate();
                            frameCopy.timelinePosition = frameCopy.timelinePosition + frameOffset;
                            keyframesEdited.Add(frameCopy); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IterateTransformLinearCurves(Evaluate, editRecord);
                }
                if (propertyLinearCurves != null)
                {
                    if (copy.propertyLinearCurves == null) copy.propertyLinearCurves = new List<PropertyLinearCurve>();
                    copy.propertyLinearCurves.AddRange(propertyLinearCurves);

                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in curve.frames)
                        {
                            if (frame.timelinePosition != timelinePosition) continue;

                            var frameCopy = frame.Duplicate();
                            frameCopy.timelinePosition = frameCopy.timelinePosition + frameOffset;
                            keyframesEdited.Add(frameCopy); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IteratePropertyLinearCurves(Evaluate, editRecord);
                }
                if (curves != null)
                {
                    if (copy.curves == null) copy.curves = new List<EditableAnimationCurve>();
                    copy.curves.AddRange(curves);

                    void Evaluate(EditableAnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<AnimationCurveEditor.KeyframeStateRaw> keyframesEdited)
                    {
                        for (int a = 0; a < curve.length; a++)
                        {
                            var key = curve[a];
                            if (getFrameIndex((decimal)key.time) != timelinePosition) continue;

                            key.time = key.time + offset;
                            keyframesEdited.Add(key); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IterateCurves(Evaluate, getFrameIndex, editRecord);
                }
                if (animationEventSource != null)
                {
                    if (copy.animationEventSource == null) copy.animationEventSource = animationEventSource;

                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (_event == null || getFrameIndex((decimal)_event.TimelinePosition) != timelinePosition) return;

                        var eventCopy = _event.Duplicate(false);
                        eventCopy.TimelinePosition = _event.TimelinePosition + offset; 
                        eventsEdited.Add(eventCopy);
                    }
                    IterateEvents(Evaluate, getFrameIndex, editRecord);
                }
            }
        }
        protected readonly Dictionary<int, TimelineKeyframe> keyframeInstances = new Dictionary<int, TimelineKeyframe>();
        protected void ClearKeyframes()
        {
            ClearClipboard();
            foreach (var pair in keyframeInstances)
            {
                var key = pair.Value;
                if (key == null) continue;
                if (key.instance != null)
                {
                    keyframePool.Release(key.instance);
                    key.instance.SetActive(false);
                }

                key.transformLinearCurves?.Clear();
                key.propertyLinearCurves?.Clear();
                key.curves?.Clear();

                key.editor = null;
                key.instance = null;
                key.transformLinearCurves = null;
                key.propertyLinearCurves = null;
                key.curves = null;
                key.animationEventSource = null;  
            }
            keyframeInstances.Clear();
        }
        protected void RefreshKeyframePosition(TimelineKeyframe key)
        {
            if (timelineWindow == null) return;

            var timelineTransform = timelineWindow.ContainerTransform;
            RefreshKeyframePosition(key, timelineTransform); 
        }
        protected void RefreshKeyframePosition(TimelineKeyframe key, RectTransform timelineTransform)
        {
            if (key.instance == null) return;
            var rT = key.RectTransform;
            rT.SetParent(timelineTransform);
            rT.anchorMin = rT.anchorMax = new Vector2(timelineWindow.GetVisibleTimelineNormalizedFramePosition(key.timelinePosition), keyframeAnchorY);
            rT.anchoredPosition3D = Vector3.zero;
        }
        public void RefreshKeyframePositions()
        {
            if (timelineWindow == null)
            {
                ClearKeyframes();
                return;
            }

            var timelineTransform = KeyContainerTransform;
            foreach (var element in keyframeInstances) RefreshKeyframePosition(element.Value, timelineTransform);
        }
        private static readonly List<TimelineKeyframe> tempKeys = new List<TimelineKeyframe>();
        public bool RelocateSelectedkeyframes(int offset)
        {
            if (offset == 0) return true;
            tempKeys.Clear();
            foreach (var pair in keyframeInstances) 
            {
                if (pair.Value.IsSelected) tempKeys.Add(pair.Value);       
            }

            foreach (var key in tempKeys) 
            {
                int newFrame = key.timelinePosition + offset; 
                if (newFrame < 0 || newFrame >= timelineWindow.LengthInFrames) return false; // Cancel due to key out of range
                if (keyframeInstances.TryGetValue(newFrame, out TimelineKeyframe existingKey) && !existingKey.IsSelected) return false; // Cancel due to overlapping key
            } 

            if (tempKeys.Count > 0)
            {
                var activeSource = CurrentSource;
                if (activeSource == null) return false;

                activeSource.MarkAsDirty();
            }

            var editRecord = BeginNewAnimationEditRecord();

            var timelineTransform = KeyContainerTransform;
            foreach (var key in tempKeys) keyframeInstances.Remove(key.timelinePosition);

            if (offset < 0)
            {
                for (int i = 0; i < tempKeys.Count; i++)
                {
                    var key = tempKeys[i]; 
                    int newTimelinePosition = key.timelinePosition + offset;
                    keyframeInstances[newTimelinePosition] = key;
                    key.Relocate(editRecord, newTimelinePosition, timelineWindow.CalculateFrameAtTimelinePosition, timelineWindow.FrameToTimelinePosition);
                    RefreshKeyframePosition(key, timelineTransform);
                }
            } 
            else
            {
                for (int i = (tempKeys.Count - 1); i >= 0; i--) 
                {
                    var key = tempKeys[i];
                    int newTimelinePosition = key.timelinePosition + offset;
                    keyframeInstances[newTimelinePosition] = key;
                    key.Relocate(editRecord, newTimelinePosition, timelineWindow.CalculateFrameAtTimelinePosition, timelineWindow.FrameToTimelinePosition);
                    RefreshKeyframePosition(key, timelineTransform);
                }
            }
            
            tempKeys.Clear();

            CommitAnimationEditRecord();

            RedrawAllCurves();
            if (editMode == EditMode.ANIMATION_EVENTS) RefreshAnimationEventsWindow();

            return true;
        }
        public int SelectedKeyframeCount
        {
            get
            {
                int count = 0;
                foreach (var pair in keyframeInstances) if (pair.Value.IsSelected) count++;
                return count;
            }
        }
        public void DeselectAllKeyframes()
        {
            foreach (var pair in keyframeInstances) pair.Value.Deselect();
        }
        public void SelectAllKeyframes()
        {
            foreach (var pair in keyframeInstances) pair.Value.Select(); 
        }
        public void DeleteSelectedKeyframes()
        {
            var editRecord = BeginNewAnimationEditRecord();

            bool flag = false;
            foreach (var pair in keyframeInstances) if (pair.Value.IsSelected) 
                { 
                    pair.Value.Delete(editRecord, timelineWindow.CalculateFrameAtTimelinePosition);
                    flag = true;
                }

            if (flag)
            {
                RefreshKeyframes();
                RedrawAllCurves();

                var source = CurrentSource;
                if (source != null) source.MarkAsDirty();

                CommitAnimationEditRecord();
            } 
            else
            {
                DiscardAnimationEditRecord();
            }
        }
        protected int copyFromFrameIndex = -1;
        protected readonly List<int> keysToCopy = new List<int>();
        public void ClearClipboard()
        {
            copyFromFrameIndex = -1;
            keysToCopy.Clear();
        }
        public void CopyKeyframes()
        {
            ClearClipboard();

            foreach (var pair in keyframeInstances)
            {
                var key = pair.Value;
                if (key == null || !key.IsSelected) continue; 

                keysToCopy.Add(pair.Key);
            }

            if (keysToCopy.Count > 0) copyFromFrameIndex = timelineWindow == null ? keyframeInstances[keysToCopy[0]].timelinePosition : timelineWindow.ScrubFrame; 
        }
        public void PasteKeyframes(int pasteToFrameIndex) 
        {
            if (timelineWindow == null || copyFromFrameIndex < 0 || pasteToFrameIndex == copyFromFrameIndex || pasteToFrameIndex < 0) return;

            int frameOffset = pasteToFrameIndex - copyFromFrameIndex;
            float offset = (float)(timelineWindow.FrameToTimelinePosition(pasteToFrameIndex) - timelineWindow.FrameToTimelinePosition(copyFromFrameIndex));

            tempKeys.Clear();
            foreach(var frameIndex in keysToCopy) 
            {
                if (!keyframeInstances.TryGetValue(frameIndex, out var key)) continue;
                tempKeys.Add(key); 
            }

            if (tempKeys.Count > 0)
            {
                var activeSource = CurrentSource;
                if (activeSource == null) return;

                activeSource.MarkAsDirty(); 
            }

            var editRecord = BeginNewAnimationEditRecord();

            foreach (var toCopy in tempKeys)
            {
                var copy = GetOrCreateKeyframe(toCopy.timelinePosition + frameOffset);
                if (copy == null) continue;

                toCopy.CopyPaste(editRecord, copy, offset, timelineWindow.CalculateFrameAtTimelinePosition);  
            }
            tempKeys.Clear();

            RefreshKeyframePositions();

            CommitAnimationEditRecord();
        }
        protected TimelineKeyframe GetOrCreateKeyframe(int timelinePosition)
        {
            if (keyframeInstances.TryGetValue(timelinePosition, out TimelineKeyframe key)) return key;

            if (timelinePosition > timelineWindow.LengthInFrames || !keyframePool.TryGetNewInstance(out GameObject keyObj)) return null;

            keyObj.SetActive(true);

            key = new TimelineKeyframe();
            key.editor = this;
            keyframeInstances[timelinePosition] = key;
            key.timelinePosition = timelinePosition;
            key.instance = keyObj;
            key.Deselect();

            var draggable = keyObj.AddOrGetComponent<UIDraggable>();
            draggable.freeze = true;
            draggable.navigation = default;
            if (draggable.OnClick == null) draggable.OnClick = new UnityEvent(); else draggable.OnClick.RemoveAllListeners();
            draggable.OnClick.AddListener(() =>
            {
                if (InputProxy.Modding_ModifyActionKey)
                {
                    key.IsSelected = !key.IsSelected;
                }
                else
                {
                    foreach (var pair in keyframeInstances) pair.Value.Deselect(); // Deselect all other keys
                    key.Select();
                }
            });
            VarRef<bool> newlySelectedFlag = new VarRef<bool>(false);
            if (draggable.OnDragStart == null) draggable.OnDragStart = new UnityEvent(); else draggable.OnDragStart.RemoveAllListeners();
            draggable.OnDragStart.AddListener(() =>
            {
                draggable.cancelNextClick = true;
                newlySelectedFlag.value = !key.IsSelected;
                key.Select();
            });
            if (draggable.OnDragStop == null) draggable.OnDragStop = new UnityEvent(); else draggable.OnDragStop.RemoveAllListeners();
            draggable.OnDragStop.AddListener(() =>
            {
                if (newlySelectedFlag) key.Deselect();
            });
            if (draggable.OnDragStep == null) draggable.OnDragStep = new UnityEvent(); else draggable.OnDragStep.RemoveAllListeners();
            if (timelineWindow != null)
            {
                draggable.OnDragStep.AddListener(() =>
                {
                    var canvas = timelineWindow.Canvas;
                    Vector3 cursorPos = timelineWindow.ContainerTransform.InverseTransformPoint(canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, CursorProxy.ScreenPosition)));
                    float posInContainer = timelineWindow.GetNormalizedPositionFromLocalPosition(cursorPos);

                    int newFrame = timelineWindow.CalculateFrameAtTimelinePosition((decimal)timelineWindow.GetTimeFromNormalizedPosition(posInContainer, true));
                    if (newFrame != key.timelinePosition) RelocateSelectedkeyframes(newFrame - key.timelinePosition);
                });
            }

            return key;
        }
        public void RefreshKeyframes()
        {
            ClearKeyframes();

            var animatable = ActiveAnimatable;
            if (animatable == null) return;
            var activeSource = animatable.CurrentSource;
            if (activeSource == null || activeSource.rawAnimation == null) return;

            void TryAddCurve(EditableAnimationCurve curve)
            {
                if (curve == null || curve.length <= 0) return;

                for(int a = 0; a < curve.length; a++)
                {
                    var keyframe = curve[a];
                    //var key = GetOrCreateKeyframe(Mathf.FloorToInt(keyframe.time * activeSource.rawAnimation.framesPerSecond));
                    var key = GetOrCreateKeyframe(timelineWindow.CalculateFrameAtTimelinePosition((decimal)keyframe.time));
                    if (key != null) 
                    {
                        if (key.curves == null) key.curves = new List<EditableAnimationCurve>();
                        key.curves.Add(curve); 
                    }
                }
            }
            void TryAddTransformLinearCurve(TransformLinearCurve curve)
            {
                if (curve == null || curve.FrameLength <= 0) return;

                foreach(var frame in curve.frames)
                {
                    var key = GetOrCreateKeyframe(frame.timelinePosition);
                    if (key != null)
                    {
                        if (key.transformLinearCurves == null) key.transformLinearCurves = new List<TransformLinearCurve>();
                        key.transformLinearCurves.Add(curve);
                    }
                }
            }
            void TryAddPropertyLinearCurve(PropertyLinearCurve curve)
            {
                if (curve == null || curve.FrameLength <= 0) return;

                foreach (var frame in curve.frames)
                {
                    var key = GetOrCreateKeyframe(frame.timelinePosition);
                    if (key != null)
                    {
                        if (key.propertyLinearCurves == null) key.propertyLinearCurves = new List<PropertyLinearCurve>();
                        key.propertyLinearCurves.Add(curve);
                    }
                }
            }
            void TrySetAnimationEventSource(CustomAnimation eventSource)
            {
                if (eventSource == null || eventSource.events == null) return;

                foreach(var _event in eventSource.events)
                {
                    if (_event == null) continue;

                    var key = GetOrCreateKeyframe(timelineWindow.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition));
                    if (key != null)
                    {
                        key.animationEventSource = eventSource;
                    }
                }
            }

            switch (editMode)
            {
                case EditMode.GLOBAL_KEYS:
                    if (activeSource.rawAnimation.transformAnimationCurves != null)
                    {
                        foreach (var curveInfo in activeSource.rawAnimation.transformAnimationCurves)
                        {
                            if (curveInfo.infoMain.isLinear)
                            {
                                if (activeSource.rawAnimation.transformLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformLinearCurves.Length) continue;
                                var curve = activeSource.rawAnimation.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddTransformLinearCurve(curve);
                            } 
                            else
                            {
                                if (activeSource.rawAnimation.transformCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformCurves.Length) continue;
                                var curve = activeSource.rawAnimation.transformCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddCurve(curve.localPositionCurveX);
                                TryAddCurve(curve.localPositionCurveY);
                                TryAddCurve(curve.localPositionCurveZ);

                                TryAddCurve(curve.localRotationCurveX);
                                TryAddCurve(curve.localRotationCurveY);
                                TryAddCurve(curve.localRotationCurveZ);
                                TryAddCurve(curve.localRotationCurveW);

                                TryAddCurve(curve.localScaleCurveX);
                                TryAddCurve(curve.localScaleCurveY);
                                TryAddCurve(curve.localScaleCurveZ);
                            }
                        }
                    }

                    if (activeSource.rawAnimation.propertyAnimationCurves != null)
                    {
                        foreach (var curveInfo in activeSource.rawAnimation.propertyAnimationCurves)
                        {
                            if (curveInfo.infoMain.isLinear)
                            {
                                if (activeSource.rawAnimation.propertyLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.propertyLinearCurves.Length) continue;
                                var curve = activeSource.rawAnimation.propertyLinearCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddPropertyLinearCurve(curve);
                            }
                            else
                            {
                                if (activeSource.rawAnimation.propertyCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.propertyCurves.Length) continue;
                                var curve = activeSource.rawAnimation.propertyCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddCurve(curve.propertyValueCurve);
                            }
                        }
                    }
                    break;

                case EditMode.SELECTED_BONE_KEYS:
                case EditMode.BONE_KEYS:
                    if (activeSource.rawAnimation.transformAnimationCurves != null)
                    {

                        void AddBoneKeys(PoseableBone bone)
                        {
                            if (bone != null && bone.transform != null)
                            {
                                string boneName = bone.name.AsID();
                                foreach (var curveInfo in activeSource.rawAnimation.transformAnimationCurves)
                                {
                                    if (curveInfo.infoMain.isLinear)
                                    {
                                        if (activeSource.rawAnimation.transformLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformLinearCurves.Length) continue;
                                        var curve = activeSource.rawAnimation.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                        if (curve == null || curve.frames == null || curve.TransformName.AsID() != boneName) continue;

                                        TryAddTransformLinearCurve(curve);
                                    }
                                    else
                                    {
                                        if (activeSource.rawAnimation.transformCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformCurves.Length) continue;
                                        var curve = activeSource.rawAnimation.transformCurves[curveInfo.infoMain.curveIndex];
                                        if (curve == null || curve.TransformName.AsID() != boneName) continue;

                                        if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalPosition)
                                        {
                                            TryAddCurve(curve.localPositionCurveX);
                                            TryAddCurve(curve.localPositionCurveY);
                                            TryAddCurve(curve.localPositionCurveZ);
                                        }
                                        if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalRotation)
                                        {
                                            TryAddCurve(curve.localRotationCurveX);
                                            TryAddCurve(curve.localRotationCurveY);
                                            TryAddCurve(curve.localRotationCurveZ);
                                            TryAddCurve(curve.localRotationCurveW);
                                        }
                                        if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalScale)
                                        {
                                            TryAddCurve(curve.localScaleCurveX);
                                            TryAddCurve(curve.localScaleCurveY);
                                            TryAddCurve(curve.localScaleCurveZ);
                                        }
                                    }
                                }
                            }
                        }

                        if (editMode == EditMode.BONE_KEYS)
                        {
                            if (selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count) AddBoneKeys(poseableBones[selectedBoneUI]);
                        } 
                        else if (editMode == EditMode.SELECTED_BONE_KEYS)
                        {
                            _tempPoseableBones.Clear();
                            GetSelectedPoseableBones(null, _tempPoseableBones);

                            foreach(var bone in _tempPoseableBones) AddBoneKeys(bone);                           
                        }

                    }
                    break;

                case EditMode.BONE_CURVES:
                    if (selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count)
                    {
                        var bone = poseableBones[selectedBoneUI];
                        if (bone != null && bone.transform != null)
                        {
                            activeSource.rawAnimation.TryGetTransformCurves(bone.name, out _, out _, out ITransformCurve mainCurve, out _);
                            if (mainCurve != null)
                            {
                                if (mainCurve is TransformLinearCurve linearCurve)
                                {
                                    TryAddTransformLinearCurve(linearCurve);
                                }
                                else if (mainCurve is TransformCurve curve)
                                {
                                    switch(selectedTransformPropertyUI)
                                    {
                                        case TransformProperty.localPosition_x:
                                            TryAddCurve(curve.localPositionCurveX);
                                            break;
                                        case TransformProperty.localPosition_y:
                                            TryAddCurve(curve.localPositionCurveY);
                                            break;
                                        case TransformProperty.localPosition_z:
                                            TryAddCurve(curve.localPositionCurveZ);
                                            break;

                                        case TransformProperty.localRotation_x:
                                            TryAddCurve(curve.localRotationCurveX);
                                            break;
                                        case TransformProperty.localRotation_y:
                                            TryAddCurve(curve.localRotationCurveY);
                                            break;
                                        case TransformProperty.localRotation_z:
                                            TryAddCurve(curve.localRotationCurveZ);
                                            break;
                                        case TransformProperty.localRotation_w:
                                            TryAddCurve(curve.localRotationCurveW);
                                            break;

                                        case TransformProperty.localScale_x:
                                            TryAddCurve(curve.localScaleCurveX);
                                            break;
                                        case TransformProperty.localScale_y:
                                            TryAddCurve(curve.localScaleCurveY);
                                            break;
                                        case TransformProperty.localScale_z:
                                            TryAddCurve(curve.localScaleCurveZ);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case EditMode.PROPERTY_CURVES:
                    if (!string.IsNullOrWhiteSpace(selectedPropertyId))
                    {
                        activeSource.rawAnimation.TryGetPropertyCurves(selectedPropertyId, out _, out _, out IPropertyCurve mainCurve, out _);
                        if (mainCurve != null)
                        {
                            if (mainCurve is PropertyLinearCurve linearCurve)
                            {
                                TryAddPropertyLinearCurve(linearCurve);
                            }
                            else if (mainCurve is PropertyCurve curve)
                            {
                                TryAddCurve(curve.propertyValueCurve);
                            }
                        }
                    }
                    break;

                case EditMode.ANIMATION_EVENTS:
                    TrySetAnimationEventSource(activeSource.rawAnimation);
                    break;
            }

            RefreshKeyframePositions();

            if (animationEventsWindow != null && animationEventsWindow.gameObject.activeInHierarchy)
            {
                RefreshAnimationEventsWindow(); 
            }
        }

        #endregion

        private List<AnimatableAsset> animatablesCache = new List<AnimatableAsset>();
        public void OpenAnimatableImporter()
        {
            if (animatableImporterWindow == null) return;

            UIPopup popup = animatableImporterWindow.GetComponentInChildren<UIPopup>(true);

            UICategorizedList list = animatableImporterWindow.GetComponentInChildren<UICategorizedList>(true);
            if (list != null) 
            {
                list.Clear(false); 

                animatablesCache.Clear(); 
                animatablesCache = AnimationLibrary.GetAllAnimatables(animatablesCache);

                foreach(var animatable in animatablesCache)
                {
                    list.AddNewListMember(animatable.name, animatable.type.ToString().ToUpper(), () => {
                        
                        if (popup != null) popup.Close();
                        ImportNewAnimatable(animatable, true, true);
                    
                    }, GetRigIcon(animatable.type)); 
                }
            } 

            animatableImporterWindow.gameObject.SetActive(true);
            animatableImporterWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();
        }

        public static void DisableAllProceduralAnimationComponents(GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var component in components)
            {
                var type = component.GetType();
                while (type != null)
                {
                    if (type.Name.IndexOf("Cloth") >= 0)
                    {
                        component.enabled = false; 
                        break;
                    }
                    type = type.BaseType;
                }
            }
        }

        public static void AddAnimatableToScene(AnimatableAsset animatable, out GameObject instance, out CustomAnimator animator)
        {
            instance = null;
            animator = null;

            if (animatable == null) return;

            instance = GameObject.Instantiate(animatable.prefab);
            DisableAllProceduralAnimationComponents(instance);

            Transform transform = instance.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            animator = instance.GetComponentInChildren<CustomAnimator>();
            if (animator != null)
            {
                animator.OverrideUpdateCalls = true;
                animator.disableMultithreading = false;
                animator.enabled = false;

                animator.Initialize();  
            }
        }

        public static ImportedAnimatable AddAnimatableToScene(AnimatableAsset animatable, int copy=0)
        {
            if (animatable == null) return null; 

            ImportedAnimatable obj = new ImportedAnimatable() { id = animatable.name, displayName = animatable.name + (copy <= 0 ? "" : $" ({copy})"), index = -1 };

            obj.Initialize(animatable);

            return obj;
        }

        public const string previewPrefix = "preview";
        public const string previewLayer_Main = "main";

        public const string restPosePrefix = "restPose";
        public const string restPoseLayer_Main = "main";

        [Serializable]
        public class Session : SwoleObject<Session, Session.Serialized>
        {

            [NonSerialized]
            protected bool isDirty;
            public bool IsDirty
            {
                get => isDirty;
                set => isDirty = value;
            }

            #region Serialization

            public override Serialized AsSerializableStruct() => this;
            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

            [Serializable]
            public struct Serialized : ISerializableContainer<Session, Session.Serialized>
            {

                public string name;
                public string SerializedName => name;

                public int playbackMode;

                public MiscSettings miscSettings;
                public CameraSettings cameraSettings;

                public int activeObjectIndex;
                public MirroringMode mirroringMode;
                public ImportedAnimatable.Serialized[] importedObjects;

                public Session AsOriginalType(PackageInfo packageInfo = default) => new Session(this, packageInfo);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
                public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
            }

            public static implicit operator Serialized(Session source)
            {
                Serialized s = new Serialized();

                s.name = source.name;
                s.playbackMode = (int)source.playbackMode;
                s.miscSettings = source.miscSettings;
                s.cameraSettings = source.cameraSettings;
                s.activeObjectIndex = source.activeObjectIndex;
                s.mirroringMode = source.mirroringMode;

                if (source.importedObjects != null)
                {
                    s.importedObjects = new ImportedAnimatable.Serialized[source.importedObjects.Count];
                    for (int a = 0; a < source.importedObjects.Count; a++) s.importedObjects[a] = source.importedObjects[a];
                }
                return s;
            }

            public Session(Session.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
            {
                this.name = serializable.name;
                this.playbackMode = (PlayMode)serializable.playbackMode;
                this.miscSettings = serializable.miscSettings;
                this.cameraSettings = serializable.cameraSettings;
                this.activeObjectIndex = serializable.activeObjectIndex;
                this.mirroringMode = serializable.mirroringMode;

                if (serializable.importedObjects != null)
                {
                    if (this.importedObjects == null) this.importedObjects = new List<ImportedAnimatable>();
                    this.importedObjects.Clear();

                    for (int a = 0; a < serializable.importedObjects.Length; a++) this.importedObjects.Add(serializable.importedObjects[a].AsOriginalType(packageInfo)); 
                }
            }

            #endregion

            public string name;
            public override string SerializedName => name;

            public Session(string name) : base(default)
            {
                this.name = name;
            }

            public void Destroy()
            {
                if (importedObjects != null)
                {
                    foreach (var obj in importedObjects) if (obj != null) obj.Destroy();
                    importedObjects = null;
                }
            }

            public PlayMode playbackMode;

            [Serializable]
            public struct MiscSettings
            {

                public static MiscSettings Default => new MiscSettings() { hideFloor = false };

                public bool hideFloor; 

                public override bool Equals(object obj)
                {
                    if (obj is MiscSettings settings) return this == settings;
                    return base.Equals(obj);
                }

                public override int GetHashCode() => base.GetHashCode();

                public static bool operator ==(MiscSettings settA, MiscSettings settB)
                {
                    if (settA.hideFloor != settB.hideFloor) return false;

                    return true;
                }
                public static bool operator !=(MiscSettings settA, MiscSettings settB) => !(settA == settB); 
            }
            protected MiscSettings miscSettings;
            public MiscSettings Settings
            {
                get => miscSettings;
                set
                {
                    if (miscSettings != value)
                    {
                        miscSettings = value;
                        IsDirty = true;
                    }
                }
            }

            protected CameraSettings cameraSettings;
            public CameraSettings CameraSettings
            {
                get => cameraSettings;
                set
                {
                    if (cameraSettings != value)
                    {
                        cameraSettings = value;
                        IsDirty = true;
                    }
                }
            }

            public int activeObjectIndex=-1;
            public List<ImportedAnimatable> importedObjects = new List<ImportedAnimatable>();
            public ImportedAnimatable GetAnimatable(int index)
            {
                if (index < 0 || importedObjects == null || index >= importedObjects.Count) return null;
                return importedObjects[index];
            }

            public ImportedAnimatable ActiveObject
            {
                get
                {
                    if (importedObjects == null || activeObjectIndex < 0 || activeObjectIndex >= importedObjects.Count) return null;
                    return importedObjects[activeObjectIndex];
                }
            }

            public void SetVisibilityOfObject(AnimationEditor editor, int index, bool visible, bool locked, bool undoable)
            {
                if (importedObjects == null || index < 0 || index >= importedObjects.Count) return;
                SetVisibilityOfObject(editor, importedObjects[index], visible, locked, undoable);
            }
            public void SetVisibilityOfObject(AnimationEditor editor, ImportedAnimatable obj, bool visible, bool locked, bool undoable)
            {
                if (obj == null) return;
                obj.SetVisibility(editor, visible, locked, undoable);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, int animatableIndex, int sourceIndex, bool visible, bool locked, bool undoable)
            {
                if (importedObjects == null || animatableIndex < 0 || animatableIndex >= importedObjects.Count) return;

                var obj = importedObjects[animatableIndex];
                if (obj == null) return;

                SetVisibilityOfSource(editor, obj, sourceIndex, visible, locked, undoable);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, ImportedAnimatable obj, int sourceIndex, bool visible, bool locked, bool undoable)
            {
                if (obj == null) return;
                obj.SetVisibilityOfSource(editor, sourceIndex, visible, locked, undoable);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, ImportedAnimatable obj, AnimationSource source, bool visible, bool locked, bool undoable)
            {
                if (obj == null) return;
                obj.SetVisibilityOfSource(editor, source, visible, locked, undoable);
            }
            
            public void SetActiveObject(AnimationEditor editor, int index, bool skipIfAlreadyActive = true, bool updateUI = true, bool stopPlayback = true)
            {
                if (importedObjects == null) return;
                index = Mathf.Min(index, importedObjects.Count - 1); 

                ImportedAnimatable importedObj = null;

                if (index >= 0) importedObj = importedObjects[index];
                if (importedObj != null && importedObj.listCategory != null) importedObj.listCategory.Expand(); 

                if ((skipIfAlreadyActive && activeObjectIndex != index) || !skipIfAlreadyActive)
                {

                    if (stopPlayback && editor.IsPlayingEntirety && activeObjectIndex != index) editor.Stop(false); 
                    activeObjectIndex = index;

                    if (importedObj != null)
                    {
                        importedObj.SetVisibility(editor, true, importedObj.IsLocked, false);
                        editor.SetActiveAnimator(importedObj.animator);
                    } 
                    else
                    {
                        editor.SetActiveAnimator(null);
                    }

                    editor.OnSetActiveObject(importedObj);
                }

                if (updateUI) editor.RefreshAnimatableListUI();

                editor.RefreshBoneGroupVisibility();

                editor.RefreshRootObjects();
            }

            [Obsolete]
            public void Reload(AnimationEditor editor)
            {
                if (importedObjects == null) return;

                foreach (var obj in importedObjects)
                {
                    if (obj == null) continue;
                    if (obj.instance != null)
                    {
                        GameObject.Destroy(obj.instance);
                    }
                    if (editor.importedAnimatablesList != null && obj.listCategory != null)
                    {
                        editor.importedAnimatablesList.DeleteCategory(obj.listCategory);
                    }
                }

                foreach (var obj in importedObjects)
                {
                    if (obj == null) continue;
                    var asset = AnimationLibrary.FindAnimatable(obj.id);
                    if (asset == null) continue;
                    obj.instance = GameObject.Instantiate(asset.prefab);
                    Transform transform = obj.instance.transform;
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                }
                
                SetActiveObject(editor, activeObjectIndex);
            }

            public void ImportNewAnimatable(AnimationEditor editor, string id, bool setAsActiveObject = true, bool undoable=false, bool markSessionDirty = true) => ImportNewAnimatable(editor, AnimationLibrary.FindAnimatable(id), setAsActiveObject, undoable, markSessionDirty);
            public void ImportNewAnimatable(AnimationEditor editor, AnimatableAsset animatable, bool setAsActiveObject=true, bool undoable=false, bool markSessionDirty = true)
            {
                if (animatable != null)
                {
                    if (importedObjects == null) importedObjects = new List<ImportedAnimatable>(); 

                    int i = 0;
                    foreach (var iobj in importedObjects) if (iobj != null && iobj.displayName.AsID().StartsWith(animatable.name.AsID())) i++;

                    var instance = AnimationEditor.AddAnimatableToScene(animatable, i);
                    AddAnimatable(editor, instance, -1, setAsActiveObject, markSessionDirty);
                    
                    editor.RefreshRootObjects();

                    if (undoable && instance != null)
                    {
                        editor.RecordRevertibleAction(new UndoableImportAnimatable(editor, instance.index, activeObjectIndex, instance));
                    }
                }
            }

            public void AddAnimatable(AnimationEditor editor, ImportedAnimatable animatable, int index = -1, bool setAsActiveObject = false, bool addToList=true, bool markSessionDirty = true)
            {
                if (animatable == null) return;
                if (importedObjects == null) importedObjects = new List<ImportedAnimatable>();

                if (animatable != null && animatable.instance != null)
                {
                    animatable.instance.SetActive(true);

                    var category = animatable.listCategory == null ? editor.importedAnimatablesList.AddNewCategory(animatable.displayName) : animatable.listCategory;
                    if (category == null)
                    {
                        GameObject.Destroy(animatable.instance);
                    }
                    else
                    {
                        if (addToList)
                        {
                            if (index < 0 || index >= importedObjects.Count)
                            {
                                animatable.index = importedObjects.Count;
                                importedObjects.Add(animatable);
                            }
                            else
                            {
                                animatable.index = index;
                                importedObjects.Insert(index, animatable);
                                RefreshImportedIndices(index);
                                if (index <= activeObjectIndex) activeObjectIndex++; 
                            }

                            editor.importedAnimatablesList.MoveCategory(category, animatable.index); 
                        }

                        animatable.listCategory = category;
                        int sourceCount = animatable.SourceCount;
                        if (sourceCount > 0)
                        {
                            for(int a = 0; a < sourceCount; a++)
                            {
                                var source = animatable.animationBank[a];
                                if (source == null) continue; 

                                source.index = a;
                                //source.CreateListMember(editor, animatable);
                                animatable.AddNewAnimationSource(editor, source, false, a, false, false); 
                            }
                        }
                        category.Expand();

                        var pointerProxy = category.gameObject.AddOrGetComponent<PointerEventsProxy>();
                        if (pointerProxy.OnRightClick == null) pointerProxy.OnRightClick = new UnityEvent(); else pointerProxy.OnRightClick.RemoveAllListeners();

                        #region Animatable Content Menu

                        pointerProxy.OnRightClick.AddListener(() =>
                        {
                            // Open context menu when animatable list object is right clicked
                            if (editor.contextMenuMain != null)
                            {
                                editor.OpenContextMenuMain();

                                var editPhysique = editor.contextMenuMain.FindDeepChildLiberal(_editPhysiqueContextMenuOptionName);
                                if (editPhysique != null)
                                {
                                    SetButtonOnClickAction(editPhysique, () =>
                                    {
                                        SetActiveObject(editor, animatable.index);
                                        editor.OpenPhysiqueEditor();
                                    });
                                    editPhysique.gameObject.SetActive(animatable.muscleController != null); 
                                }

                                var alignToView = editor.contextMenuMain.FindDeepChildLiberal(_alignToViewContextMenuOptionName);
                                if (alignToView != null)
                                {
                                    SetButtonOnClickAction(alignToView, () =>
                                    {
                                        SetActiveObject(editor, animatable.index);
                                        if (animatable.instance != null && editor.runtimeEditor != null && editor.runtimeEditor.EditorCamera != null)
                                        {
                                            editor.runtimeEditor.EditorCamera.transform.GetPositionAndRotation(out var pos, out var rot);
                                            animatable.instance.transform.SetPositionAndRotation(pos, rot);
                                            if (animatable.cameraProxy != null) animatable.cameraProxy.fieldOfView = CameraProxy._defaultFieldOfView;
                                            var scale = animatable.instance.transform.localScale;
                                            animatable.instance.transform.localScale = new Vector3(scale.x, scale.y, editor.GetCameraSettings().fieldOfView / CameraProxy._defaultFieldOfView); 
                                        }
                                    });
                                    alignToView.gameObject.SetActive(animatable.cameraProxy != null);
                                }

                                var alignView = editor.contextMenuMain.FindDeepChildLiberal(_alignViewContextMenuOptionName);
                                if (alignView != null)
                                {
                                    SetButtonOnClickAction(alignView, () =>
                                    {
                                        SetActiveObject(editor, animatable.index);
                                        if (animatable.instance != null && editor.runtimeEditor != null && editor.runtimeEditor.EditorCamera != null)
                                        {
                                            animatable.instance.transform.GetPositionAndRotation(out var pos, out var rot);
                                            editor.runtimeEditor.EditorCamera.transform.SetPositionAndRotation(pos, rot);
                                            if (animatable.cameraProxy != null) editor.SetCameraFOV(animatable.cameraProxy.ScaledFieldOfView); 
                                        }
                                    });
                                    alignView.gameObject.SetActive(animatable.cameraProxy != null);
                                }
                            }

                        });

                        #endregion

                        var button = category.gameObject.GetComponent<Button>();
                        var tabButton = category.gameObject.GetComponent<UITabButton>();
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                SetActiveObject(editor, animatable.index);
                            });
                        }
                        if (tabButton != null)
                        {
                            if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent();
                            tabButton.OnClick.RemoveAllListeners();
                            tabButton.OnClick.AddListener(() =>
                            {
                                SetActiveObject(editor, animatable.index);
                            });
                        }

                        var expand = category.rectTransform.FindDeepChildLiberal("Expand");
                        if (expand != null)
                        {
                            SetButtonOnClickAction(expand, () =>
                            {
                                expand.gameObject.SetActive(false);
                                category.Expand();
                            });
                            expand.gameObject.SetActive(false);
                        }
                        var retract = category.rectTransform.FindDeepChildLiberal("Retract");
                        if (retract != null)
                        {
                            SetButtonOnClickAction(retract, () =>
                            {
                                retract.gameObject.SetActive(false);
                                category.Retract();
                            });
                            expand.gameObject.SetActive(true);
                        }

                        var options = category.rectTransform.FindDeepChildLiberal("Options");
                        if (options != null)
                        {
                            SetButtonOnClickActionByName(options, "Add", () =>
                            {
                                SetActiveObject(editor, animatable.index);
                                editor.OpenNewAnimationWindow();
                            });
                            SetButtonOnClickActionByName(options, "Delete", () =>
                            {
                                void Delete()
                                {
                                    editor.RemoveAnimatable(animatable, false, true); 
                                }

                                if (animatable.animationBank != null && animatable.animationBank.Count > 0)
                                {
                                    editor.ShowPopupYesNo($"DELETE {animatable.displayName}", $"Are you sure you want to delete {animatable.displayName} and its {(animatable.animationBank == null ? 0 : animatable.animationBank.Count)} animations?", Delete, null);
                                } 
                                else
                                {
                                    Delete();
                                }

                            });
                        }

                        var rt_visOn = category.rectTransform.FindDeepChildLiberal(AnimationEditor._visibilityOnTag);
                        var rt_locked = category.rectTransform.FindDeepChildLiberal(AnimationEditor._lockedTag);
                        var rt_visOff = category.rectTransform.FindDeepChildLiberal(AnimationEditor._visibilityOffTag);
                        void RefreshVisibilityButtons()
                        {
                            if (rt_visOn != null) rt_visOn.gameObject.SetActive(animatable.Visible && !animatable.IsLocked);
                            if (rt_locked != null) rt_locked.gameObject.SetActive(animatable.IsLocked);
                            if (rt_visOff != null) rt_visOff.gameObject.SetActive(!animatable.Visible && !animatable.IsLocked);  
                        }

                        animatable.refreshVisibilityButtons = RefreshVisibilityButtons;
                        CustomEditorUtils.SetButtonOnClickAction(rt_visOn, () =>
                        {
                            animatable.SetVisibility(editor, true, true, true);
                        }, true, true, false);
                        CustomEditorUtils.SetButtonOnClickAction(rt_locked, () =>
                        {
                            animatable.SetVisibility(editor, false, false, true);
                        }, true, true, false);
                        CustomEditorUtils.SetButtonOnClickAction(rt_visOff, () =>
                        {
                            animatable.SetVisibility(editor, true, false, true);
                        }, true, true, false);

                        animatable.SetVisibility(editor, animatable.Visible, animatable.IsLocked, false);

                        if (setAsActiveObject) SetActiveObject(editor, animatable.index, true, false);
                        editor.RefreshAnimatableListUI();
                    }

                    if (markSessionDirty) editor.MarkSessionDirty(); 
                }
            }
            public void RefreshImportedIndices(int startIndex = 0)
            {
                if (importedObjects == null) return;

                for (int i = startIndex; i < importedObjects.Count; i++)
                {
                    importedObjects[i].index = i;
                }
            }
            public bool RemoveAnimatable(AnimationEditor editor, ImportedAnimatable animatable, bool destroy, bool undoable = false)
            {
                if (animatable == null || importedObjects == null) return false;

                if (animatable.index < 0 || animatable.index >= importedObjects.Count) animatable.index = importedObjects.IndexOf(animatable);         

                return RemoveAnimatable(editor, animatable.index, destroy, undoable);
            }
            public bool RemoveAnimatable(AnimationEditor editor, int index, bool destroy, bool undoable = false)
            {
                if (importedObjects == null || index < 0 || index >= importedObjects.Count) return false;

                if (editor.IsPlayingEntirety) editor.Pause(false);

                var obj = importedObjects[index];
                if (obj != null)
                {
                    if (obj.listCategory != null) 
                    { 
                        editor.importedAnimatablesList.DeleteCategory(obj.listCategory);
                        obj.listCategory = null;
                    }
                    int sourceCount = obj.SourceCount;
                    if (obj.SourceCount > 0)
                    {
                        for(int a = 0; a < sourceCount; a++)
                        {
                            var source = obj.animationBank[a];
                            if (source == null) continue;

                            source.listMember = null;
                        }
                    }

                    obj.index = -1;
                    if (destroy) 
                    {
                        obj.Destroy();   
                    } 
                    else
                    {
                        if (obj.instance != null) obj.instance.SetActive(false);
                        if (undoable) editor.RecordRevertibleAction(new UndoableRemoveAnimatable(editor, index, activeObjectIndex, obj));
                    }
                    
                }
                importedObjects.RemoveAt(index);

                RefreshImportedIndices(index);
                if (activeObjectIndex == index) SetActiveObject(editor, index); else if (index < activeObjectIndex) activeObjectIndex--;
                 
                editor.MarkSessionDirty();

                return true;
            }

            public AnimationSource CreateNewAnimationSource(AnimationEditor editor, string animationName, float length, bool undoable) => CreateNewAnimationSource(editor, activeObjectIndex, animationName, length, undoable);
            public AnimationSource CreateNewAnimationSource(AnimationEditor editor, int animatableIndex, string animationName, float length, bool undoable)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.CreateNewAnimationSource(editor, animationName, length, undoable);
            }

            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, string displayName, CustomAnimation animationToLoad, bool undoable)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, displayName, animationToLoad, undoable);
            }
            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, string animationName, bool undoable) 
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, animationName, undoable);
            }
            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, PackageIdentifier package, string animationName, bool undoable) 
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, package, animationName, undoable);
            }
            public void AddNewAnimationSource(AnimationEditor editor, int animatableIndex, AnimationSource source, bool undoable)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.AddNewAnimationSource(editor, source, undoable);
            }

            public void RemoveAnimationSource(AnimationEditor editor, int animatableIndex, AnimationSource source, bool undoable)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.RemoveAnimationSource(editor, source, undoable);
            }
            public void RemoveAnimationSource(AnimationEditor editor, int animatableIndex, int index, bool undoable)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.RemoveAnimationSource(editor, index, undoable);
            }

            public void End(AnimationEditor editor)
            {
                if (editor == null) return;

                if (editor.runtimeEditor != null) editor.runtimeEditor.DeselectAll();
                if (editor.importedAnimatablesList != null) editor.importedAnimatablesList.Clear();

                if (importedObjects != null)
                {
                    foreach(var obj in importedObjects)
                    {
                        if (obj == null) continue;

                        if (obj.animationBank != null)
                        {
                            foreach (var source in obj.animationBank) source.listMember = null;
                        }
                        obj.listCategory = null;
                        obj.Destroy();
                    }
                }
            }
              
            #region Mirroring Mode
            public MirroringMode mirroringMode;
            public void SetMirroringMode(MirroringMode mode) => mirroringMode = mode;
            public void SetMirroringMode(int mode) => mirroringMode = (MirroringMode)mode;
            public void SetMirroringMode(string mode)
            {
                if (Enum.TryParse<MirroringMode>(mode, out var result)) mirroringMode = result;
            }
            #endregion

        }

        public const string _sessionNameDateFormat = "MM-dd-yyyy_HH-mm";
        public void StartFreshSession() 
        {
            if (currentSession != null && currentSession.IsDirty)
            {
                ShowPopupConfirmation("Current session has unsaved changes that will be lost. Are you sure?", () => StartNewSession());
            }
            else StartNewSession(); 
        }
        public Session StartNewSession(string name = null)
        {
            if (currentSession != null) EndCurrentSession();

            if (string.IsNullOrWhiteSpace(name)) name = DateTime.Now.ToString(_sessionNameDateFormat);

            var session = new Session(name);
            CurrentSession = new Session(name);

            RefreshRootObjects();
            SetActiveObject(-1, false, true);

            ClearUndoHistory();

            SyncSaveButton(session);

            return session;
        }
        public Session LoadSession(Session.Serialized session) => LoadSession(session.AsOriginalType());
        public Session LoadSession(Session session)
        {
            if (currentSession != null) EndCurrentSession();
            
            CurrentSession = session;
            currentSession.RefreshImportedIndices();
            if (currentSession.importedObjects != null)
            {
                for(int a = 0; a < currentSession.importedObjects.Count; a++)
                {
                    var animatable = currentSession.importedObjects[a];
                    if (animatable == null) continue; 
                    currentSession.AddAnimatable(this, animatable, a, false, false, false); // creates list members and listeners                
                }
            }
            
            RefreshRootObjects();

            SetPlaybackMode(currentSession.playbackMode, false); 
            SetActiveObject(currentSession.activeObjectIndex, false);
            
            ClearUndoHistory();

            SyncSaveButton(currentSession);

            return currentSession;
        }
        public void EndCurrentSession()
        {
            if (currentSession != null) currentSession.End(this);
            currentSession = null;
        }
        public void MarkSessionDirty()
        {
            if (currentSession != null) currentSession.IsDirty = true;
            SyncSaveButton();
        }

        public static Session LoadSessionFromFile(FileInfo file, SwoleLogger logger = null) => LoadSessionFromFile(file.FullName, logger);
        public static Session LoadSessionFromFile(string path, SwoleLogger logger = null) => LoadSessionFromFileInternal(true, path, logger).GetAwaiter().GetResult();
        public static Task<Session> LoadSessionFromFileAsync(FileInfo file, SwoleLogger logger = null) => LoadSessionFromFileAsync(file.FullName, logger);
        public static Task<Session> LoadSessionFromFileAsync(string path, SwoleLogger logger = null) => LoadSessionFromFileInternal(false, path, logger);
        async static private Task<Session> LoadSessionFromFileInternal(bool sync, string path, SwoleLogger logger = null)
        {
            try
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                ContentManager.TryLoadType<Session>(out Session session, default, data, Path.GetDirectoryName(path), null, null, null, logger);

                return session;
            } 
            catch(Exception ex)
            {
                logger?.LogError($"Error while attempting to load session from '{path}'");
                logger?.LogError(ex);
            }

            return null;
        }

        public static bool SaveSession(DirectoryInfo dir, Session session, SwoleLogger logger = null) => SaveSession(dir.FullName, session, logger);
        public static bool SaveSession(string directoryPath, Session session, SwoleLogger logger = null) => SaveSessionInternal(true, directoryPath, session, logger).GetAwaiter().GetResult();
        public static Task<bool> SaveSessionAsync(DirectoryInfo dir, Session session, SwoleLogger logger = null) => SaveSessionAsync(dir.FullName, session, logger);
        public static Task<bool> SaveSessionAsync(string directoryPath, Session session, SwoleLogger logger = null) => SaveSessionInternal(false, directoryPath, session, logger);
        async private static Task<bool> SaveSessionInternal(bool sync, string directoryPath, Session session, SwoleLogger logger = null)
        {

            if (session == null)
            {
                logger?.LogWarning($"Tried to save null session to '{directoryPath}'");
                return false; 
            }

            try
            {

                if (string.IsNullOrWhiteSpace(session.name)) session.name = DateTime.Now.ToString(_sessionNameDateFormat);

                string fullPath = Path.Combine(directoryPath, $"{session.name}.{ContentManager.fileExtension_Generic}");

                string json = swole.Engine.ToJson(session, true);
                byte[] bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);

                if (sync) File.WriteAllBytes(fullPath, bytes); else await File.WriteAllBytesAsync(fullPath, bytes);
                return true;

            }
            catch (Exception ex)
            {

                logger?.LogError($"Error while attempting to save session '{session.name}' to '{directoryPath}'");
                logger?.LogError(ex);

            }
            return false;
        }

        public const float maxAnimationLength = 600;
        public void OpenNewAnimationWindow()
        {
            if (newAnimationWindow == null) return;

            InputField[] fields = newAnimationWindow.gameObject.GetComponentsInChildren<InputField>(true);
            foreach (var field in fields) if (field != null && field.contentType != InputField.ContentType.DecimalNumber && field.contentType != InputField.ContentType.IntegerNumber) field.text = string.Empty;

            TMP_InputField[] fieldsTMP = newAnimationWindow.gameObject.GetComponentsInChildren<TMP_InputField>(true);
            foreach (var field in fieldsTMP) if (field != null && field.contentType != TMP_InputField.ContentType.DecimalNumber && field.contentType != TMP_InputField.ContentType.IntegerNumber) field.text = string.Empty;

            UIPopupMessageFadable errorMessage = newAnimationWindow.GetComponentInChildren<UIPopupMessageFadable>(true);
            UIPopup popup = newAnimationWindow.GetComponentInChildren<UIPopup>(true);

            var create = newAnimationWindow.FindDeepChildLiberal("Create");
            void Create()
            {
                var dropdown = newAnimationWindow.FindDeepChildLiberal("SourceDropDown");
                if (dropdown != null)
                {
                    var selection = dropdown.FindDeepChildLiberal("Current-Text");
                    if (selection != null)
                    {
                        var selectionText = GetComponentText(selection);
                        if (selectionText == "new")
                        {
                            var selectionRoot = newAnimationWindow.FindDeepChildLiberal("NewSource");
                            if (selectionRoot != null)
                            {
                                string animName = "null";
                                float animLength = 1f;

                                var name = selectionRoot.FindDeepChildLiberal("Name");
                                if (name != null) animName = GetInputFieldText(name);
                                var length = selectionRoot.FindDeepChildLiberal("LengthInput");
                                if (length != null)
                                {
                                    string animLengthText = GetInputFieldText(length);
                                    if (!float.TryParse(animLengthText, out animLength)) animLength = 1f;
                                }

                                if (string.IsNullOrWhiteSpace(animName))
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation name cannot be empty!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                                else if (animLength <= 0)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation length must be greater than zero!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                                else if (animLength > maxAnimationLength)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation length cannot be greater than 600 (10 minutes)!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }

                                var source = CreateNewAnimationSource(animName, animLength, true);
                                if (source == null)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("An unkown error occurred").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                            }
                        }
                        else if (selectionText == "import")
                        {
                            string packageString = string.Empty;
                            string animName = string.Empty;

                            var selectionRoot = newAnimationWindow.FindDeepChildLiberal("ImportSource");
                            if (selectionRoot != null)
                            {
                                var name = selectionRoot.FindDeepChildLiberal("Name");
                                if (name != null) animName = GetInputFieldText(name);
                                var package = selectionRoot.FindDeepChildLiberal("Package");
                                if (package != null) packageString = GetInputFieldText(package);

                                if (string.IsNullOrWhiteSpace(animName))
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation name cannot be empty!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }

                                if (!LoadNewAnimationSource(animName, true, out AnimationSource source, new PackageIdentifier(packageString)) || source == null)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage($"Failed to load animation '{animName}'{(string.IsNullOrEmpty(packageString) ? "" : $" from package '{packageString}'")}!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                            }
                        }
                    }
                }

                popup.Close();
            }
            SetButtonOnClickAction(create, Create);

            SetButtonOnClickActionByName(newAnimationWindow, "BrowseAnimations", () =>
            {
                OpenBrowseAnimationsWindow((CustomAnimation toImport) =>
                {
                    var selectionRoot = newAnimationWindow.FindDeepChildLiberal("ImportSource");
                    if (selectionRoot != null)
                    {
                        var name = selectionRoot.FindDeepChildLiberal("Name");
                        if (name != null) SetInputFieldText(name, toImport.Name);
                        var package = selectionRoot.FindDeepChildLiberal("Package");
                        if (package != null) SetInputFieldText(package, toImport.PackageInfo.GetIdentityString());
                    }

                    browseAnimationsWindow.gameObject.SetActive(false);
                });
            });

            newAnimationWindow.gameObject.SetActive(true); 
            newAnimationWindow.SetAsLastSibling();
            popup?.Elevate();
        }

        public delegate void ImportAnimationDelegate(CustomAnimation anim);
        public void OpenBrowseAnimationsWindow(ImportAnimationDelegate callback) => OpenBrowseAnimationsWindow(browseAnimationsWindow, callback);
        public void OpenBrowseAnimationsWindow(RectTransform browseAnimationsWindow, ImportAnimationDelegate callback)
        {
            if (browseAnimationsWindow == null) return;

            browseAnimationsWindow.gameObject.SetActive(true); 
            browseAnimationsWindow.SetAsLastSibling();

            var animationsList = browseAnimationsWindow.GetComponentInChildren<UICategorizedList>(true);
            if (animationsList != null)
            {
                CustomEditorUtils.SetInputFieldOnValueChangeActionByName(browseAnimationsWindow, "search", (string str) =>
                {
                    animationsList.FilterMembersAndCategoriesByStartString(str, false);
                });

                animationsList.Clear(true);
                for (int a = 0; a < ContentManager.LocalPackageCount; a++)
                {
                    var pkg = ContentManager.GetLocalPackage(a);
                    if (pkg == null) continue;

                    var cat = animationsList.AddOrGetCategory(pkg.GetIdentityString());
                    foreach (var content in pkg.Content)
                    {
                        if (content == null || !typeof(CustomAnimation).IsAssignableFrom(content.AssetType)) continue;

                        var contentRef = content;
                        void OnClick()
                        {
                            callback?.Invoke(content.Asset as CustomAnimation); 
                        }

                        animationsList.AddNewListMember(contentRef.Name, cat, OnClick);
                    }
                    if (cat.members == null || cat.members.Count <= 0) animationsList.DeleteCategory(cat);
                }
                for (int a = 0; a < ContentManager.ExternalPackageCount; a++)
                {
                    var pkg = ContentManager.GetExternalPackage(a);
                    if (pkg.content == null) continue;

                    var cat = animationsList.AddOrGetCategory(pkg.GetIdentityString());
                    foreach (var content in pkg.Content)
                    {
                        if (content == null || !typeof(CustomAnimation).IsAssignableFrom(content.AssetType)) continue;

                        var contentRef = content;
                        void OnClick()
                        {
                            callback?.Invoke(content.Asset as CustomAnimation);
                        }

                        animationsList.AddNewListMember(contentRef.Name, cat, OnClick);
                    }
                    if (cat.members == null || cat.members.Count <= 0) animationsList.DeleteCategory(cat); 
                }
            }
        }

        public void ImportNewAnimatable(string id, bool setAsActiveObject = true, bool undoable=false, bool markSessionDirty = true)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.ImportNewAnimatable(this, id, setAsActiveObject, undoable, markSessionDirty);
        }
        public void ImportNewAnimatable(AnimatableAsset animatable, bool setAsActiveObject = true, bool undoable=false, bool markSessionDirty = true)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.ImportNewAnimatable(this, animatable, setAsActiveObject, undoable, markSessionDirty);
        }
        public void AddAnimatable(ImportedAnimatable animatable, int index = -1, bool setAsActiveObject = false, bool addToList=true, bool markSessionDirty = true)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.AddAnimatable(this, animatable, index, setAsActiveObject, addToList, markSessionDirty);
        }
        public void RefreshImportedIndices(int startIndex = 0)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.RefreshImportedIndices(startIndex);
        }
        public bool RemoveAnimatable(ImportedAnimatable animatable, bool destroy, bool undoable=false)
        {
            if (currentSession == null) return false;
            return currentSession.RemoveAnimatable(this, animatable, destroy, undoable);
        }
        public bool RemoveAnimatable(int index, bool destroy, bool undoable = false)
        {
            if (currentSession == null) return false;
            return currentSession.RemoveAnimatable(this, index, destroy, undoable);
        }

        protected Session currentSession;
        public Session CurrentSession
        {
            get => currentSession;
            set
            {
                currentSession = value;
                if (currentSession != null)
                {
                    SetMirroringMode(currentSession.mirroringMode);
                    SetMiscSettings(currentSession.Settings);
                    SetCameraSettings(currentSession.CameraSettings);
                }
            }
        }

        public void SetActiveObject(int index, bool skipIfAlreadyActive = true, bool updateUI = true, bool stopPlayback = true)
        {
            if (currentSession == null) return;
            currentSession.SetActiveObject(this, index, skipIfAlreadyActive, updateUI, stopPlayback);
        }

        protected readonly List<GameObject> tempGameObjects = new List<GameObject>();
        /// <summary>
        /// Called by the current session whenever the active object is changed
        /// </summary>
        protected void OnSetActiveObject(ImportedAnimatable activeObj) 
        {
            if (physiqueEditorWindow != null)
            {
                if (activeObj == null || activeObj.instance == null)
                {
                    physiqueEditorWindow.gameObject.SetActive(false);
                } 
                else if (physiqueEditorWindow.gameObject.activeSelf)
                {
                    var character = activeObj.instance.GetComponentInChildren<MuscularRenderedCharacter>(true);
                    if (character == null)
                    {
                        physiqueEditorWindow.gameObject.SetActive(false);
                    } 
                    else 
                    {
                        var editor = PhysiqueEditor;
                        if (editor != null) editor.Character = character;
                    }
                }
            }

            if (activeObj != null)
            {
                if (activeObj.selectedBoneIndices != null)
                {
                    tempGameObjects.Clear(); 
                    foreach (var selectedBoneIndex in activeObj.selectedBoneIndices) if (selectedBoneIndex >= 0 && selectedBoneIndex < poseableBones.Count && poseableBones[selectedBoneIndex].transform != null) tempGameObjects.Add(poseableBones[selectedBoneIndex].transform.gameObject);

                    runtimeEditor.DeselectAll();
                    runtimeEditor.Select(tempGameObjects);

                }
            }

            RefreshTimeline();
        }

        public const string _activeTag = "active";
        public const string _activeMainTag = "activemain";
        public const string _syncedTag = "synced";
        public const string _outofsyncTag = "outofsync";
        public const string _outdatedTag = "outdated";
        public const string _unboundTag = "unbound";
        public const string _lockedTag = "locked";
        public const string _visibilityOnTag = "visibilityOn";
        public const string _visibilityOffTag = "visibilityOff";
        public void RefreshAnimatableListUI()
        {
            if (importedAnimatablesList == null) return;

            for (int a = 0; a < importedAnimatablesList.CategoryCount; a++)
            {
                var category = importedAnimatablesList[a];
                if (category == null || category.rectTransform == null) continue;

                var childT = category.rectTransform.FindDeepChildLiberal(_activeTag);
                if (childT != null) childT.gameObject.SetActive(false); 

                if (category.members == null) continue;
                for (int b = 0; b < category.members.Count; b++)
                {
                    var member = category.members[b];
                    if (member == null || member.rectTransform == null) continue;

                    childT = member.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (childT != null) childT.gameObject.SetActive(false);

                    childT = member.rectTransform.FindDeepChildLiberal(_activeMainTag);
                    if (childT != null) childT.gameObject.SetActive(false);

                    childT = member.rectTransform.FindDeepChildLiberal(_syncedTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_outofsyncTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_outdatedTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_unboundTag);
                    if (childT != null) childT.gameObject.SetActive(true); 
                }
            }

            if (currentSession != null && currentSession.importedObjects != null)
            {
                for (int a = 0; a < currentSession.importedObjects.Count; a++)
                {
                    var obj = currentSession.importedObjects[a];
                    if (obj == null) continue;

                    if (obj.listCategory != null && obj.listCategory.rectTransform != null)
                    {
                        var queryT = obj.listCategory.rectTransform.FindDeepChildLiberal(_visibilityOnTag);
                        if (queryT != null) queryT.gameObject.SetActive(obj.Visible && !obj.IsLocked); 
                        queryT = obj.listCategory.rectTransform.FindDeepChildLiberal(_lockedTag);
                        if (queryT != null) queryT.gameObject.SetActive(obj.IsLocked);
                        queryT = obj.listCategory.rectTransform.FindDeepChildLiberal(_visibilityOffTag);
                        if (queryT != null) queryT.gameObject.SetActive(!obj.Visible && !obj.IsLocked); 
                    }  

                    if (obj.animationBank != null)
                    {
                        for (int b = 0; b < obj.animationBank.Count; b++)
                        {
                            var source = obj.animationBank[b];
                            if (source == null) continue;

                            source.SetMix(null, source.GetMixRaw()/*source.previewLayer == null ? 1 : source.previewLayer.mix*/, true, true, false);

                            if (source.listMember == null || source.listMember.rectTransform == null) continue;
                            var rT = source.listMember.rectTransform;

                            Transform childT;

                            childT = source.listMember.rectTransform.FindDeepChildLiberal(_visibilityOnTag);
                            if (childT != null) childT.gameObject.SetActive(source.Visible && !source.IsLocked);
                            childT = source.listMember.rectTransform.FindDeepChildLiberal(_lockedTag);
                            if (childT != null) childT.gameObject.SetActive(source.IsLocked);
                            childT = source.listMember.rectTransform.FindDeepChildLiberal(_visibilityOffTag);
                            if (childT != null) childT.gameObject.SetActive(!source.Visible && !source.IsLocked);

                            if (swole.FindContentPackage(source.package, out ContentPackage pkg, out _) == swole.PackageActionResult.Success) // Change icon of save button based on the source's export state
                            {
                                void IsSynced()
                                {
                                    childT = rT.FindDeepChildLiberal(_syncedTag); // Assets are synced
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }
                                void IsNotSynced()
                                {
                                    childT = rT.FindDeepChildLiberal(_outofsyncTag); // Changes need to be synced
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }
                                void IsOutdated()
                                {
                                    childT = rT.FindDeepChildLiberal(_outdatedTag); // Linked content is newer
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }

                                if (string.IsNullOrWhiteSpace(source.syncedName)) source.syncedName = source.DisplayName;

                                if (pkg.TryFind<CustomAnimation>(out var linkedAnim, source.syncedName))
                                {
                                    EvaluateAnimationAssetLink(source, linkedAnim, IsSynced, IsNotSynced, IsOutdated);
                                }
                                else
                                {
                                    IsNotSynced();
                                }

                                childT = rT.FindDeepChildLiberal(_unboundTag);
                                if (childT != null) childT.gameObject.SetActive(false);
                            }

                        }
                    }

                    var activeSource = obj.CurrentSource;
                    if (activeSource == null || activeSource.listMember == null || activeSource.listMember.rectTransform == null) continue; 

                    var active = activeSource.listMember.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (active != null) active.gameObject.SetActive(true);

                }

                var activeObj = currentSession.ActiveObject;
                if (activeObj != null && activeObj.listCategory != null)
                {
                    var active = activeObj.listCategory.rectTransform == null ? null : activeObj.listCategory.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (active != null) active.gameObject.SetActive(true);

                    var activeAnim = activeObj.CurrentSource;
                    if (activeAnim != null && activeAnim.listMember != null)
                    {
                        active = activeAnim.listMember.rectTransform == null ? null : activeAnim.listMember.rectTransform.FindDeepChildLiberal(_activeMainTag);
                        if (active != null) active.gameObject.SetActive(true);
                    }

                }
            }
        }

        protected void DeselectAll()
        {
            runtimeEditor.DeselectAll();
        }

        protected void OnSelectionChange(List<GameObject> fullSelection, List<GameObject> newlySelected, List<GameObject> deselected)
        {
            bool refreshTimeline = false;
            if (newlySelected != null && newlySelected.Count > 0)
            {
                GameObject lastSelected = null;
                int i = newlySelected.Count - 1;
                while(lastSelected == null && i >= 0)
                {
                    lastSelected = newlySelected[i];
                    i--;
                }
                if (lastSelected != null)
                {
                    int boneIndex = FindPoseableBoneIndex(lastSelected); 
                    if (boneIndex >= 0)
                    {
                        selectedBoneUI = boneIndex;
                        refreshTimeline = true;
                    }
                }
            }

            if ((newlySelected != null && newlySelected.Count > 0) || (deselected != null && deselected.Count > 0)) refreshTimeline = true;  

            var activeObj = ActiveAnimatable;
            if (activeObj != null && fullSelection != null)
            {
                if (activeObj.selectedBoneIndices == null) activeObj.selectedBoneIndices = new List<int>(); 
                activeObj.selectedBoneIndices.Clear();

                foreach (var selectedObj in fullSelection)
                {
                    int boneIndex = FindPoseableBoneIndex(selectedObj);
                    if (boneIndex >= 0)
                    {
                        activeObj.selectedBoneIndices.Add(boneIndex);
                    }
                }
            }

            if (refreshTimeline)
            {
                RefreshTimeline();
            }
        }

        private readonly HashSet<GameObject> objectSetA = new HashSet<GameObject>();
        protected void OnPreSelectCustomize(int selectReason, List<GameObject> toSelect, List<GameObject> toIgnore)
        {

            objectSetA.Clear();
            
            foreach(var obj in toSelect)
            {
                if (obj == null) continue;

                var proxy = obj.GetComponent<SelectionProxy>();
                if (proxy == null && obj.transform.parent != null) proxy = obj.transform.parent.GetComponent<SelectionProxy>();
                if (proxy != null)
                {
                    if (proxy.includeSelf && (!proxy.onlyActive || (proxy.onlyActive && obj.activeSelf))) objectSetA.Add(obj);
                    foreach (var selected in proxy.toSelect) if (!proxy.onlyActive || (proxy.onlyActive && selected.activeSelf)) objectSetA.Add(selected);
                } 
                else
                {
                    objectSetA.Add(obj); 
                }
            }

            toSelect.Clear();
            toSelect.AddRange(objectSetA);
        }

        public class PoseableBone
        {
            public string name;
            public int parent;
            public Transform transform;
            public Transform rootSelectable;
            public ChildBone[] children;

            public void SetActive(bool active)
            {
                if (rootSelectable != null) rootSelectable.gameObject.SetActive(active);
                if (children != null)
                {
                    foreach (var child in children)
                        if (child.leafSelectable != null)
                        {
                            bool childActive = true;
                            if (child.rootSelectable != null) childActive = child.rootSelectable.gameObject.activeSelf;

                            child.leafSelectable.gameObject.SetActive(active && childActive); 
                        }
                }
            }
            public void SetLeafActive(Transform childBone, bool active, bool ignoreChildActiveState = false)
            {
                if (children != null)
                {
                    foreach (var child in children) 
                        if (child.leafSelectable != null && child.transform == childBone) 
                        {
                            bool childActive = true;
                            if (!ignoreChildActiveState && child.rootSelectable != null) 
                            {           
                                childActive = child.rootSelectable.gameObject.activeSelf; 
                            }

                            child.leafSelectable.gameObject.SetActive(active && childActive);
                            break;
                        }
                }
            }
        }

        public struct ChildBone
        {
            public int index;
            public Transform transform;
            public Transform rootSelectable;
            public Transform leafSelectable;
        }

        protected void AlignLeafTransform(Transform leaf, Transform childBone)
        {
            leaf.localPosition = Vector3.zero;
            leaf.localRotation = Quaternion.FromToRotation(Vector3.up, childBone.localPosition.normalized);
            leaf.localScale = new Vector3(1, childBone.localPosition.magnitude, 1);
        }

        private readonly List<PoseableBone> poseableBones = new List<PoseableBone>();
        public int FindPoseableBoneIndex(string boneName)
        {
            for (int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].name == boneName || poseableBones[i].transform.name == boneName) return i;
            boneName = boneName.AsID();
            for (int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].name.AsID() == boneName || poseableBones[i].transform.name.AsID() == boneName) return i; 
            return -1;
        }
        public int FindPoseableBoneIndex(GameObject obj) => FindPoseableBoneIndex(obj.transform);
        public int FindPoseableBoneIndex(Transform boneTransform)
        {
            for(int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].transform == boneTransform || poseableBones[i].rootSelectable == boneTransform) return i;
            return -1;
        }
        public PoseableBone FindPoseableBone(Transform boneTransform)
        {
            int i = FindPoseableBoneIndex(boneTransform);
            return i < 0 ? null : poseableBones[i];
        }
        private static readonly List<GameObject> _boneQuery = new List<GameObject>();
        public int SelectedBoneCount
        {
            get
            {
                if (runtimeEditor == null) return 0;
                _boneQuery.Clear();
                runtimeEditor.QueryActiveSelection(_boneQuery);

                int count = 0; 
                foreach (var bone in _boneQuery) if (bone != null && FindPoseableBoneIndex(bone) >= 0) count++;
                _boneQuery.Clear();

                return count;
            }
        }
        private readonly List<GameObject> visibleRootBones = new List<GameObject>();
        private readonly List<GameObject> visibleLeafBones = new List<GameObject>();

        public void IgnorePreviewObjects()
        {
            runtimeEditor.DisableSceneUpdates = true;
            runtimeEditor.DisableMultiSelect = true;
            if (currentSession == null) return;
#if BULKOUT_ENV
            var rtScene = RTScene.Get;
            if (rtScene != null)
            {
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj.animator == null) continue;
                        rtScene.SetRootObjectIgnored(obj.animator.gameObject, true); // Make RLD ignore the animation bones      
                    }
                }
            }
#endif
        }
        public void StopIgnoringPreviewObjects()
        {
            runtimeEditor.DisableSceneUpdates = false;
            runtimeEditor.DisableMultiSelect = false; 
            if (currentSession == null) return;
#if BULKOUT_ENV
            var rtScene = RTScene.Get;
            if (rtScene != null)
            {
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj.animator == null) continue;
                        rtScene.SetRootObjectIgnored(obj.animator.gameObject, false); // Make RLD no longer ignore the animation bones      
                    }
                }
            }
#endif
        }

        protected CustomAnimator activeAnimator;
        public CustomAnimator ActiveAnimator => activeAnimator;

        private readonly Dictionary<Color, Mesh> coloredRoots = new Dictionary<Color, Mesh>();
        private readonly Dictionary<Color, Mesh> coloredLeaves = new Dictionary<Color, Mesh>();
        public void DisposeColoredMeshes()
        {
            foreach (var pair in coloredRoots) if (pair.Value != null) Destroy(pair.Value);
            foreach (var pair in coloredLeaves) if (pair.Value != null) Destroy(pair.Value);

            coloredRoots.Clear();
            coloredLeaves.Clear();
        }
        public Mesh GetColoredRootMesh(Color color)
        {
            if (coloredRoots.TryGetValue(color, out var mesh)) return mesh;

            var filter = boneRootPrototype.GetComponentInChildren<MeshFilter>();
            mesh = MeshUtils.DuplicateMesh(filter == null || filter.sharedMesh == null ? UnityPrimitiveMesh.Get(PrimitiveType.Sphere) : filter.sharedMesh);
            Color[] vcolors = new Color[mesh.vertexCount];
            for (int a = 0; a < vcolors.Length; a++) vcolors[a] = color;
            mesh.colors = vcolors;
            coloredRoots[color] = mesh;

            return mesh;
        }
        public Mesh GetColoredLeafMesh(Color color)
        {
            if (coloredLeaves.TryGetValue(color, out var mesh)) return mesh;

            var filter = boneLeafPrototype.GetComponentInChildren<MeshFilter>();
            mesh = MeshUtils.DuplicateMesh(filter == null || filter.sharedMesh == null ? UnityPrimitiveMesh.Get(PrimitiveType.Cylinder) : filter.sharedMesh);
            Color[] vcolors = new Color[mesh.vertexCount];
            for (int a = 0; a < vcolors.Length; a++) vcolors[a] = color;
            mesh.colors = vcolors;
            coloredLeaves[color] = mesh;

            return mesh;
        }
        public void SetActiveAnimator(CustomAnimator animator)
        {
            if (activeAnimator != animator)
            {
                recordingStates.Clear(); 

                foreach (GameObject obj in visibleRootBones) if (obj != null)
                    {
                        boneRootPool.Release(obj);
                        obj.transform.SetParent(null);
                        obj.SetActive(false);
                    }
                foreach (GameObject obj in visibleLeafBones) if (obj != null)
                    {
                        boneLeafPool.Release(obj);
                        obj.transform.SetParent(null); 
                        obj.SetActive(false);
                    }

                visibleRootBones.Clear();
                visibleLeafBones.Clear();
            }
            else return;

            if (activeAnimator != null && activeAnimator.IkManager != null) activeAnimator.IkManager.PostLateUpdate -= RecordChanges;           

            overrideRecordCall = false;
            activeAnimator = animator;
            poseableBones.Clear();

            if (animator != null && animator.Bones != null)
            {
                if (animator.IkManager != null) 
                { 
                    animator.IkManager.PostLateUpdate += RecordChanges;
                    overrideRecordCall = true; 
                }

                if (animator.Bones != null)
                {
                    var bones = animator.Bones.bones;
                    if (bones != null)
                    {

                        for (int i = 0; i < bones.Length; i++)
                        {
                            var bone = bones[i];
                            if (bone == null || !animator.avatar.TryGetBoneInfo(bone.name, out var boneInfo)) continue;

                            if (boneRootPool.TryGetNewInstance(out GameObject rootInst))
                            {

                                var nonDefaultParentBoneInfo = animator.avatar.GetNonDefaultParentBoneInfo(bone, out var nonDefaultParentBone);

                                var boneGroup = animator.avatar.GetBoneGroup(boneInfo.isDefault && nonDefaultParentBoneInfo.defaultChildrenToSameGroup ? nonDefaultParentBoneInfo.boneGroup : boneInfo.boneGroup); 
                                var filter = rootInst.GetComponentInChildren<MeshFilter>();
                                if (filter != null) filter.sharedMesh = GetColoredRootMesh(boneGroup.color);  

                                var proxy = rootInst.AddOrGetComponent<SelectionProxy>();
                                proxy.includeSelf = false;
                                if (proxy.toSelect == null) proxy.toSelect = new List<GameObject>();
                                proxy.toSelect.Clear();
                                proxy.toSelect.Add(bone.gameObject);

                                visibleRootBones.Add(rootInst);
                                rootInst.SetLayerAllChildren(boneOverlayLayer);

                                var rootT = rootInst.transform;
                                rootT.SetParent(bone, false);
                                rootT.localPosition = boneInfo.offset;//Vector3.zero;
                                rootT.localRotation = Quaternion.identity;
                                float scale = nonDefaultParentBoneInfo.childScale * (boneInfo.isDefault ? nonDefaultParentBoneInfo.scale : boneInfo.scale);
                                rootT.localScale = new Vector3(scale, scale, scale);

                                int parentIndex = -1;
                                if (bone.parent != null) parentIndex = FindPoseableBoneIndex(bone.parent);
                                PoseableBone parentPoseable = null;
                                if (parentIndex >= 0)
                                {
                                    parentPoseable = poseableBones[parentIndex];
                                    if (parentPoseable.children != null)
                                    {
                                        for (int a = 0; a < parentPoseable.children.Length; a++)
                                        {
                                            var child = parentPoseable.children[a];
                                            if (child.transform != bone) continue;

                                            child.rootSelectable = rootT;
                                            parentPoseable.children[a] = child;
                                            break;
                                        }
                                    }
                                }

                                bool isIkFk = animator.avatar.IsIkBoneFkEquivalent(animator.avatar.Remap(bone.name), out var ikBone);
                                Transform ikBoneTransform = null;
                                if (isIkFk)
                                {
                                    int ind = FindPoseableBoneIndex(ikBone.name);
                                    if (ind >= 0)
                                    {
                                        var ikPosable = poseableBones[ind];
                                        if (ikPosable != null && ikPosable.transform != null) ikBoneTransform = ikPosable.transform; else isIkFk = false;
                                    }
                                    else
                                    {
                                        isIkFk = false;
                                    }
                                } 

                                boneInfo.dontDrawChildConnections = boneInfo.isDefault ? nonDefaultParentBoneInfo.dontDrawChildConnections : boneInfo.dontDrawChildConnections; // inherit from first non-default parent if not defined

                                ChildBone[] children = null;
                                int childCount = bone.childCount;
                                //if (!boneInfo.dontDrawChildConnections)
                                //{
                                    children = new ChildBone[childCount];
                                    for (int j = 0; j < children.Length; j++)
                                    {
                                        var childBone = bone.GetChild(j);

                                        if (childBone == null) continue;

                                        int childIndex = FindPoseableBoneIndex(childBone);
                                        PoseableBone childPoseable = null;
                                        if (childIndex >= 0)
                                        {
                                            childPoseable = poseableBones[childIndex];
                                            childPoseable.parent = poseableBones.Count;
                                        }

                                        if (!animator.avatar.IsPoseableBone(childBone.name) || !animator.avatar.TryGetBoneInfo(childBone.name, out var childBoneInfo) || (childBoneInfo.dontDrawConnection || (boneInfo.dontDrawChildConnections && childBoneInfo.isDefault))) continue; 

                                        if (boneLeafPool.TryGetNewInstance(out GameObject leafInst))
                                        {
                                            var filterLeaf = leafInst.GetComponentInChildren<MeshFilter>();
                                            if (filterLeaf != null) filterLeaf.sharedMesh = GetColoredLeafMesh(boneGroup.color); 

                                            var proxyLeaf = leafInst.AddOrGetComponent<SelectionProxy>();
                                            proxyLeaf.includeSelf = false;
                                            if (proxyLeaf.toSelect == null) proxyLeaf.toSelect = new List<GameObject>(); 
                                            proxyLeaf.toSelect.Clear();
                                            proxyLeaf.toSelect.Add(bone.gameObject);
                                            if (isIkFk)
                                            {
                                                proxyLeaf.onlyActive = true;
                                                proxyLeaf.toSelect.Add(ikBoneTransform.gameObject);
                                            }


                                            visibleLeafBones.Add(leafInst);
                                            leafInst.SetLayerAllChildren(boneOverlayLayer);

                                            var leafT = leafInst.transform;
                                            leafT.SetParent(bone, false);
                                            AlignLeafTransform(leafT, childBone);

                                            children[j] = new ChildBone() { index = childIndex, transform = childBone, leafSelectable = leafT, rootSelectable = childPoseable == null ? null : childPoseable.rootSelectable };
                                        }
                                    }
                                //}
                                //else
                                //{

                                //    for (int j = 0; j < childCount; j++)
                                //    {
                                //        var childBone = bone.GetChild(j);

                                //        if (childBone == null) continue;

                                //        int childIndex = FindPoseableBoneIndex(childBone);
                                //        if (childIndex >= 0) poseableBones[childIndex].parent = poseableBones.Count;
                                //    }
                                //}
                                
                                poseableBones.Add(new PoseableBone() { name = animator.RemapBoneName(bone.name), rootSelectable = rootT, transform = bone, parent = parentIndex, children = children });  
                            }
                        }
                    }
                }

            }

            RefreshBoneKeyDropdowns();
            RefreshBoneCurveDropdowns();
            RefreshTimeline();
        }

        public AnimationSource CreateNewAnimationSource(string animationName, float length, bool undoable)
        {
            if (currentSession == null) return null;
            return currentSession.CreateNewAnimationSource(this, animationName, length, undoable);
        }
        public bool LoadNewAnimationSource(string animationName, bool undoable, out AnimationSource source, PackageIdentifier package=default)
        {
            source = null;
            if (currentSession == null) return false;

            source = currentSession.LoadNewAnimationSource(this, currentSession.activeObjectIndex, package, animationName, undoable);
            return source != null;
        }

        public void RemoveAnimationSource(int animatableIndex, AnimationSource source, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.RemoveAnimationSource(this, animatableIndex, source, undoable);
        }
        public void RemoveAnimationSource(int animatableIndex, int index, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.RemoveAnimationSource(this, animatableIndex, index, undoable);
        }

        public void SetVisibilityOfObject(int index, bool visible, bool locked, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfObject(this, index, visible, locked, undoable);
        }
        public void SetVisibilityOfObject(ImportedAnimatable obj, bool visible, bool locked, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfObject(this, obj, visible, locked, undoable);
        }
        public void SetVisibilityOfSource(int animatableIndex, int sourceIndex, bool visible, bool locked, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, animatableIndex, sourceIndex, visible, locked, undoable);
        }
        public void SetVisibilityOfSource(ImportedAnimatable obj, int sourceIndex, bool visible, bool locked, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, obj, sourceIndex, visible, locked, undoable);
        }
        public void SetVisibilityOfSource(ImportedAnimatable obj, AnimationSource source, bool visible, bool locked, bool undoable)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, obj, source, visible, locked, undoable);  
        }

        protected struct LockedTransform
        {
            public Transform transform;
            public Vector3 lockedWorldPosition;
            public Quaternion lockedWorldRotation;
            public bool Snapback()
            {
                if (transform == null) return false;

                transform.GetPositionAndRotation(out var currentPos, out var currentRot);
                transform.SetPositionAndRotation(lockedWorldPosition, lockedWorldRotation);

                return currentPos != lockedWorldPosition || currentRot != lockedWorldRotation;
            }
        }

        protected List<LockedTransform> lockedTransforms = new List<LockedTransform>();
        public bool IsTransformLocked(Transform transform)
        {
            if (transform == null) return false;

            foreach (var lockedTransform in lockedTransforms) if (lockedTransform.transform == transform) return true;
            return false;
        }
        public void LockTransform(Transform transform)
        {
            if (transform == null) return;
            lockedTransforms.RemoveAll(i => i.transform == null || i.transform == transform);

            lockedTransforms.Add(new LockedTransform() { transform = transform, lockedWorldPosition = transform.position, lockedWorldRotation = transform.rotation });
        }
        public void UnlockTransform(Transform transform)
        {
            lockedTransforms.RemoveAll(i => i.transform == null || i.transform == transform);   
        }
        public void SnapbackLockedTransforms(List<Transform> affectedOutputTransforms = null/*, bool includeSelected = false*/) 
        {
            lockedTransforms.RemoveAll(i => i.transform == null);
             
            foreach (var lockedTransform in lockedTransforms)  
            {
                //if (lockedTransform.transform == null || (!includeSelected && runtimeEditor.IsSelected(lockedTransform.transform.gameObject))) continue;
                if (lockedTransform.Snapback()) 
                {
                    affectedOutputTransforms?.Add(lockedTransform.transform); 
                }
            }
        }

        #region Behaviour Loop

        public bool testSave;
        public bool testLoad;
        public string testPath;

        public bool testPlayback;
        public bool testPause;
        public bool testStop;
        public bool testScrub;
        public bool testInsert;
        public PlayMode testMode;

        public bool test;
        public bool testPlay;
        public bool testRunAnimator;

        public float testTime;

        public CustomAnimation testAnimation;

        private AnimationUtils.Pose restPose;
        private AnimationUtils.Pose lastPose;

        private CustomAnimationController testController;
        private bool playFlag;
        public void OnUpdate()
        {
            if (!IsInitialized) return;

            PrepRecordingStates();

            if (string.IsNullOrWhiteSpace(testPath)) testPath = Application.persistentDataPath;
            if (currentSession != null && testSave)
            {
                testSave = false;
                if (!string.IsNullOrEmpty(testPath))
                {
                    SaveSession(testPath, currentSession, swole.DefaultLogger);
                }
            }
            if (testLoad)
            {
                testLoad = false;
                if (!string.IsNullOrEmpty(testPath))
                {
                    var sesh = LoadSessionFromFile(testPath, swole.DefaultLogger);
                    if (sesh != null)
                    {
                        LoadSession(sesh); 
                    }
                }
            }

            if (testPlayback)
            {
                testPlayback = false;
                SetPlaybackMode(testMode); 
                Play(); 
            }
            if (testPause)
            {
                testPause = false;
                Pause();
            }
            if (testStop)
            {
                testStop = false;
                Stop();
            }
            if (testScrub)
            {
                testScrub = false;
                SetPlaybackMode(testMode);
                PlaySnapshot(testTime);
            }
            if (testInsert)
            {
                testInsert = false;
                var activeSource = CurrentSource;
                if (activeSource != null) activeSource.InsertKeyframes(true, true, ActiveAnimatable, testTime, (timelineWindow == null ? null : timelineWindow.CalculateFrameAtTimelinePosition));
            }
            Step();
            //
            if (test)
            {
                test = false;

                OpenAnimationEventsWindow(animationEventsWindow); 
                 
                /*var pose = new AnimationUtils.Pose(activeAnimator);
                if (restPose == null) restPose = pose;
                if (testAnimation == null) testAnimation = new CustomAnimation();

                pose.Insert(testAnimation, testTime, restPose, lastPose);
                lastPose = pose;
                testTime += 1;*/
            }
            if (testPlay)
            {
                testPlay = false;
                if (testAnimation != null && activeAnimator != null)
                {

                    if (testController != null) activeAnimator.RemoveControllerData(testController);
                    testAnimation.FlushJobData();

                    testController = ScriptableObject.CreateInstance<CustomAnimationController>();
                    testController.name = "test";
                    testController.animationReferences = new CustomMotionController.AnimationReference[] { new CustomMotionController.AnimationReference("testAnim", testAnimation, AnimationLoopMode.Loop) };

                    CustomAnimationLayer testLayer = new CustomAnimationLayer();
                    testLayer.name = "layer1";
                    testLayer.blendParameterIndex = -1;
                    testLayer.entryStateIndex = 0; 
                    testLayer.mix = 1;
                    testLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index=0,type= MotionControllerType.AnimationReference } };
                    testLayer.IsAdditive = false;

                    CustomStateMachine testStateMachine = new CustomStateMachine() { name="testMachine", motionControllerIndex = 0 };

                    testLayer.stateMachines = new CustomStateMachine[] { testStateMachine };

                    testController.layers = new CustomAnimationLayer[] { testLayer };

                    activeAnimator.ApplyController(testController); 

                }
            }
            if (testRunAnimator)
            {
                if (!playFlag)
                {
                    playFlag = true;
#if BULKOUT_ENV
                    RTScene.Get.SetRootObjectIgnored(activeAnimator.gameObject, true);
#endif
                }
                activeAnimator.UpdateStep(Time.deltaTime);
            } else if (playFlag)
            {
                playFlag = false;
#if BULKOUT_ENV
                RTScene.Get.SetRootObjectIgnored(activeAnimator.gameObject, false); 
#endif
            }
        }

        public virtual void OnLateUpdate()
        {
            if (!IsInitialized) return;

            LateStep();
            //
            if (testRunAnimator)
            {
                activeAnimator.LateUpdateStep(Time.deltaTime);
            }
            if (floorTransform != null)
            {
                Vector3 floorPos = floorTransform.position;

                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    Vector3 camPos = mainCam.transform.position;
                    floorPos.x = camPos.x;
                    floorPos.z = camPos.z;
                }

                floorTransform.position = floorPos;
            }

            if (clickableOptions != null) foreach (var option in clickableOptions) if (option != null) option.Refresh(this);

            var currentSession = CurrentSession;
            if (!IsPlayingPreview && currentSession != null && currentSession.importedObjects != null)
            {
                foreach (var obj in currentSession.importedObjects)
                {
                    if (obj == null || obj.animator == null) continue;

                    obj.animator.SyncIKFK(true);   
                }

                SnapbackLockedTransforms();
            }

            if (!overrideRecordCall) RecordChanges(); 
        } 

        public virtual void OnFixedUpdate(){}

        #endregion

        public ImportedAnimatable ActiveAnimatable
        {
            get
            {
                if (currentSession == null) return null;
                return currentSession.ActiveObject;
            }
        }
        public AnimationSource CurrentSource
        {
            get
            {
                var activeObj = ActiveAnimatable;
                if (activeObj == null) return null;

                return activeObj.CurrentSource;
            }
        }
        public CustomAnimation CurrentCompiledAnimation
        {
            get
            {
                var activeObj = ActiveAnimatable;
                if (activeObj == null) return null;

                return activeObj.CurrentCompiledAnimation;
            }
        }

        public CustomStateMachine ActivePreviewState
        {
            get
            {
                var source = CurrentSource;
                if (source == null) return null;

                return source.PreviewState;  
            }
        }

        [Serializable]
        public enum PlayMode
        {
            Active_Only, Last_Edited, All, Active_All
        }

        public PlayMode PlaybackMode 
        { 
            get
            {
                var sesh = CurrentSession;
                if (sesh == null) return PlayMode.Active_Only;

                return sesh.playbackMode;
            }

            set => SetPlaybackMode(value);
        }

        public struct UndoableSetPlaybackMode : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public PlayMode prevMode, mode;

            public UndoableSetPlaybackMode(AnimationEditor editor, PlayMode prevMode, PlayMode mode)
            {
                this.editor = editor;
                this.prevMode = prevMode;
                this.mode = mode;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                editor.SetPlaybackMode(mode, false);
            }

            public void Revert()
            {
                if (editor == null) return;

                editor.SetPlaybackMode(prevMode, false);
            }

            public void Perpetuate() { }
            public void PerpetuateUndo()
            {
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        public void SetPlaybackMode(int mode) => SetPlaybackMode(mode, true);
        public void SetPlaybackMode(int mode, bool undoable) => SetPlaybackMode((PlayMode)mode, undoable);
        public void SetPlaybackMode(PlayMode mode) => SetPlaybackMode(mode, true);
        public void SetPlaybackMode(PlayMode mode, bool undoable)
        {
            if (isPlaying) Pause(undoable);

            var sesh = CurrentSession;
            if (sesh == null) return;

            var prevPlaybackMode = sesh.playbackMode;
            sesh.playbackMode = mode;
 
            if (playModeDropdownRoot != null)
            {
                SetComponentTextByName(playModeDropdownRoot, _currentTextObjName, sesh.playbackMode.ToString().ToLower());
            }

            if (IsRecording) RefreshRecordingButton();

            if (undoable) RecordRevertibleAction(new UndoableSetPlaybackMode(this, prevPlaybackMode, sesh.playbackMode)); 
        }
        public void SetPlaybackActiveOnly() => SetPlaybackMode(PlayMode.Active_Only);
        public void SetPlaybackLastEdited() => SetPlaybackMode(PlayMode.Last_Edited); 
        public void SetPlaybackAll() => SetPlaybackMode(PlayMode.All);
        public void SetPlaybackActiveAll() => SetPlaybackMode(PlayMode.Active_All);

        [SerializeField]
        private float playbackPosition;
        public void SetPlaybackPosition(float pos)
        {
            playbackPosition = pos;
            if (timelineWindow != null) // Update timeline UI
            { 
                timelineWindow.SetScrubPosition(playbackPosition, false);
                timelineWindow.SetNonlinearScrubPosition(timelineWindow.GetNonlinearTimeFromTime(playbackPosition), false);
            } 
        }
        public float PlaybackPosition
        {
            get => playbackPosition;
            set => SetPlaybackPosition(value);
        }

        public float playbackSpeed = 1;

        private bool isPlaying;
        private bool isSnapshot;
        public bool IsPlayingPreview => isPlaying;
        public bool IsPlayingEntirety => isPlaying && !isSnapshot;
        public bool IsPlayingSnapshot => isPlaying && isSnapshot;

        protected bool loop;
        public bool IsLooping 
        {
            get => loop;
            set => SetLooping(value);
        }
        public struct UndoableSetLooping : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public bool prevMode, mode;

            public UndoableSetLooping(AnimationEditor editor, bool prevMode, bool mode)
            {
                this.editor = editor;
                this.prevMode = prevMode;
                this.mode = mode;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                editor.SetLooping(mode, false);
            }

            public void Revert()
            {
                if (editor == null) return;

                editor.SetLooping(prevMode, false);
            }

            public void Perpetuate() { }
            public void PerpetuateUndo()
            {
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone; 
                return newState;
            }
        }
        public void ToggleLooping() => SetLooping(!loop, true);
        public void SetLooping(bool looping) => SetLooping(looping, true);
        public void SetLooping(bool looping, bool undoable)
        {
            bool prevLooping = loop;
            loop = looping;
            if (loopButton != null)
            {
                var inactive = loopButton.transform.FindDeepChildLiberal("inactive");
                var active = loopButton.transform.FindDeepChildLiberal("active");

                if (loop)
                {
                    inactive.gameObject.SetActive(false);
                    active.gameObject.SetActive(true);
                }
                else
                {
                    inactive.gameObject.SetActive(true);
                    active.gameObject.SetActive(false);
                }
            }

            if (undoable) RecordRevertibleAction(new UndoableSetLooping(this, prevLooping, loop));
        }

        protected bool useTimeCurve;
        public bool IsUsingTimeCurve
        {
            get => useTimeCurve;
            set => SetUseTimeCurve(value);
        }
        public struct UndoableSetUseTimeCurve : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public bool prevMode, mode;

            public UndoableSetUseTimeCurve(AnimationEditor editor, bool prevMode, bool mode)
            {
                this.editor = editor;
                this.prevMode = prevMode;
                this.mode = mode;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                editor.SetUseTimeCurve(mode, false);
            }

            public void Revert()
            {
                if (editor == null) return;

                editor.SetUseTimeCurve(prevMode, false); 
            }

            public void Perpetuate() { }
            public void PerpetuateUndo()
            {
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public void ToggleUseTimeCurve() => SetUseTimeCurve(!useTimeCurve, true);
        public void SetUseTimeCurve(bool flag) => SetUseTimeCurve(flag, true);
        public void SetUseTimeCurve(bool flag, bool undoable)
        {
            if (IsPlayingEntirety) Pause(undoable);
            
            bool prevUseTimeCurve = useTimeCurve;
            useTimeCurve = flag;  
            
            if (timeCurveButton != null)
            {
                var inactive = timeCurveButton.transform.FindDeepChildLiberal("inactive");
                var active = timeCurveButton.transform.FindDeepChildLiberal("active"); 

                if (useTimeCurve)
                {
                    inactive.gameObject.SetActive(false);
                    active.gameObject.SetActive(true); 
                }
                else
                {
                    inactive.gameObject.SetActive(true);
                    active.gameObject.SetActive(false);
                }
            }

            RefreshTimeline();

            if (undoable) RecordRevertibleAction(new UndoableSetUseTimeCurve(this, prevUseTimeCurve, useTimeCurve));
        }

        private bool record;
        public bool IsRecording 
        {
            get => record;
            set => SetRecording(value, true); 
        }
        public bool RecordingIsPaused => record && PlaybackMode != PlayMode.Active_Only;
        public bool CanRecord => record && !RecordingIsPaused;

        public struct UndoableSetRecording : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationEditor editor;
            public bool prevMode, mode;

            public UndoableSetRecording(AnimationEditor editor, bool prevMode, bool mode)
            {
                this.editor = editor;
                this.prevMode = prevMode;
                this.mode = mode;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;

                editor.SetRecording(mode, false);
            }

            public void Revert()
            {
                if (editor == null) return;

                editor.SetRecording(prevMode, false); 
            }

            public void Perpetuate() { }
            public void PerpetuateUndo()
            {
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        public void ToggleRecording() => SetRecording(!record, true);

        public void SetRecording(bool recording) => SetRecording(recording, true);
        public void SetRecording(bool recording, bool undoable)
        {
            bool prevRecording = record;

            ForceApplyTransformStateChanges();
            record = recording;

            RefreshRecordingButton();

            if (undoable) RecordRevertibleAction(new UndoableSetRecording(this, prevRecording, record));
        }
        public void RefreshRecordingButton()
        {
            if (recordButton != null)
            {
                var inactive = recordButton.transform.FindDeepChildLiberal("inactive");
                var active = recordButton.transform.FindDeepChildLiberal("active");
                var paused = recordButton.transform.FindDeepChildLiberal("paused");

                if (record)
                {
                    inactive.gameObject.SetActive(false);
                    active.gameObject.SetActive(!RecordingIsPaused);  
                    paused.gameObject.SetActive(RecordingIsPaused);
                }
                else
                {
                    inactive.gameObject.SetActive(true);
                    active.gameObject.SetActive(false);
                    paused.gameObject.SetActive(false);
                }
            }
        }

        protected bool overrideRecordCall;
        protected class RecordingTransformState
        {
            public Transform transform;

            public Vector3 pos;
            public Quaternion rot;
            public Vector3 localScale;

            public int frameDelay;
            public int recordImmunityDelay;

            public RecordingTransformState(Transform t)
            {
                this.transform = t;
                t.GetLocalPositionAndRotation(out pos, out rot); 
                localScale = t.localScale; 
            }

            public bool HasChanged() => HasChanged(out _, out _, out _);
            public bool HasChanged(out Vector3 position, out Quaternion rotation, out Vector3 scale)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                scale = Vector3.one;
                if (transform == null) return false;

                transform.GetLocalPositionAndRotation(out position, out rotation);
                scale = transform.localScale;
                 
                if (pos != position || rot != rotation || localScale != scale) return true;  

                return false;
            }

            public bool CheckAndApplyChanges()
            {
                if (HasChanged(out var a, out var b, out var c))
                {
                    pos = a;
                    rot = b;
                    localScale = c;
                    return true; 
                }

                return false;
            }
        }
        protected readonly Dictionary<Transform, RecordingTransformState> recordingStates = new Dictionary<Transform, RecordingTransformState>();
        public void PrepRecordingStates()
        {
            if (IsPlayingPreview || !IsRecording) return;  

            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            foreach(var bone in poseableBones)
            {
                if (bone.transform == null) continue;

                if (!recordingStates.TryGetValue(bone.transform, out var state))
                {
                    state = new RecordingTransformState(bone.transform);
                    recordingStates[bone.transform] = state;
                }

                //state.CheckAndApplyChanges();
            }
        }
        protected void ForceApplyTransformStateChanges(int recordImmunityDelay = 2)
        {
            foreach (var pair in recordingStates) ForceApplyTransformStateChanges(pair.Key, recordImmunityDelay);
        }
        protected void ForceApplyTransformStateChanges(Transform transform, int recordImmunityDelay = 2)
        {
            if (recordingStates.TryGetValue(transform, out var state)) ForceApplyTransformStateChanges(state, recordImmunityDelay);
        }
        protected void ForceApplyTransformStateChanges(RecordingTransformState state, int recordImmunityDelay = 2)
        {
            state.frameDelay = 0;
            state.recordImmunityDelay = recordImmunityDelay;
            state.CheckAndApplyChanges();
        }
        /// <summary>
        /// Delay to wait until transform changes have stopped before recording them, instead of recording them every frame which causes stuttering
        /// </summary>
        public const int recordFrameDelay = 3;
        protected bool useReferencePoseForNextRecordStep;
        protected bool updateReferencePoseForNextRecordStep;
        protected bool nextRecordStepIsImmediate;
        protected int nextRecordStepDelay;
        public void RecordChanges() 
        {

            if (runtimeEditor == null) return;  

            if (nextRecordStepDelay > 0 || runtimeEditor.IsDraggingGizmo)
            {
                nextRecordStepDelay--; 
                return;
            }
            RecordChanges(!useReferencePoseForNextRecordStep, useReferencePoseForNextRecordStep, updateReferencePoseForNextRecordStep, nextRecordStepIsImmediate);
            useReferencePoseForNextRecordStep = false;
            updateReferencePoseForNextRecordStep = false;
            nextRecordStepIsImmediate = false;
        }
        public void RecordChanges(bool preUpdateReferencePose, bool useReferencePose, bool updateReferencePose, bool immediate)
        {

            if (preUpdateReferencePose) RefreshFlaggedReferencePoses();

            if (!IsPlayingPreview && CanRecord)
            {

                var activeObj = ActiveAnimatable;
                if (activeObj == null) return;

                _tempTransforms.Clear();
                foreach (var pair in recordingStates)
                    if (pair.Key != null)
                    {
                        var state = pair.Value;

                        if (state.recordImmunityDelay > 0)
                        {
                            state.CheckAndApplyChanges();
                            state.recordImmunityDelay--;
                            state.frameDelay = 0;
                            continue;
                        }

                        if (runtimeEditor.IsDraggingGizmo)
                        {
                            if (IsSelected(pair.Key))
                            {
                                state.CheckAndApplyChanges();
                                state.frameDelay = 0;
                                continue;
                            }
                        }

                        if (state.CheckAndApplyChanges())
                        {
                            if (immediate)
                            {
                                state.frameDelay = 0;
                                _tempTransforms.Add(pair.Key);
                            }
                            else
                            {
                                state.frameDelay = recordFrameDelay;
                            }
                        }
                        else
                        {
                            if (state.frameDelay > 0)
                            {
                                state.frameDelay--;
                                if (state.frameDelay <= 0) _tempTransforms.Add(pair.Key);
                            }
                        }
                    }
                if (_tempTransforms.Count > 0)
                {
                    BeginNewAnimationEditRecord(); 
                    RecordPose(_tempTransforms, useReferencePose/*true*/, updateReferencePose, true/*CurrentSource.GetOrCreateRawData().GetClosestKeyframeTime()*//*false*/, false); // using reference pose breaks with ik. only use reference pose when altering with transform gizmo
                    CommitAnimationEditRecord();
                }
                _tempTransforms.Clear();
            }
        }

        public void PlayOnce()
        {
            var activeAnim = CurrentSource;
            if (activeAnim != null) PlaybackPosition = activeAnim.PlaybackPosition;

            Play(false, PlaybackPosition);
        }
        public void PlayLooped() 
        {
            var activeAnim = CurrentSource;
            if (activeAnim != null) PlaybackPosition = activeAnim.PlaybackPosition;  

            Play(true, PlaybackPosition); 
        }
        private readonly List<SourceCollection> toCompile = new List<SourceCollection>();
        protected void CompileSourcesForPlayback(CompilationDelegate OnComplete = null, CompilationDelegate OnFail = null, CompilationDelegate OnBusy = null)
        {
            if (IsCompiling || IsPlayingPreview) 
            {
                OnBusy?.Invoke();
                return;
            }

            toCompile.Clear();
            if (currentSession.importedObjects != null)
            {
                var activeObj = currentSession.ActiveObject;
                if (activeObj != null) activeObj.SetVisibility(this, true, activeObj.IsLocked, false);
                switch (currentSession.playbackMode)
                {
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.Visible || obj.animationBank == null) continue;
                            if (obj.compilationList == null) obj.compilationList = new List<AnimationSource>();
                            obj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = obj.displayName, sources = obj.compilationList };
                            foreach (var anim in obj.animationBank)
                            {
                                if (anim == null || !anim.Visible) continue;
                                anim.previewAnimator = obj.animator;
                                obj.compilationList.Add(anim);
                            }
                            if (obj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Last_Edited:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.Visible || obj.animationBank == null) continue;
                            if (obj.compilationList == null) obj.compilationList = new List<AnimationSource>();
                            obj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = obj.displayName, sources = obj.compilationList };
                            if (obj.editIndex >= 0 && obj.editIndex < obj.animationBank.Count) 
                            {
                                var anim = obj.animationBank[obj.editIndex];
                                if (anim == null || (!anim.Visible && obj != activeObj)) continue;
                                anim.previewAnimator = obj.animator;
                                obj.compilationList.Add(anim);
                            }
                            if (obj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Active_All:
                        if (activeObj != null && activeObj.animationBank != null)
                        {
                            if (activeObj.compilationList == null) activeObj.compilationList = new List<AnimationSource>();
                            activeObj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = activeObj.displayName, sources = activeObj.compilationList };
                            foreach (var anim in activeObj.animationBank)
                            {
                                if (anim == null || !anim.Visible) continue;
                                anim.previewAnimator = activeObj.animator;
                                activeObj.compilationList.Add(anim);
                            }
                            if (activeObj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Active_Only:
                        if (activeObj != null && activeObj.animationBank != null)
                        {
                            if (activeObj.compilationList == null) activeObj.compilationList = new List<AnimationSource>();
                            activeObj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = activeObj.displayName, sources = activeObj.compilationList };
                            if (activeObj.editIndex >= 0 && activeObj.editIndex < activeObj.animationBank.Count) 
                            {
                                var anim = activeObj.animationBank[activeObj.editIndex];
                                anim.previewAnimator = activeObj.animator;
                                activeObj.compilationList.Add(anim); 
                            }
                            if (activeObj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                }
            }

            if (CompileAll(toCompile, null, false, OnComplete, OnFail)) 
            {
                foreach (var collection in toCompile)
                {
                    foreach (var anim in collection.sources)
                    {
                        if (useTimeCurve) anim.RestoreCompiledTimeCurve(); else anim.RemoveCompiledTimeCurve();  
                    }
                }
            } 
            else 
            {
                OnBusy?.Invoke();
            }
        }
        private void ArrangePreviewLayers()
        {
            if (currentSession != null && currentSession.importedObjects != null)
            {
                void ArrangeActiveSourceForObject(ImportedAnimatable obj, bool useRestPoseAsBase = true)
                {
                    if (obj == null || !obj.Visible || obj.animator == null) return;

                    obj.RebuildControllerIfNull();
                    obj.animator.ResetIKControllers();

                    var source = obj.CurrentSource;
                    if (source != null && source.previewLayer != null) 
                    { 
                        source.previewLayer.IsAdditive = useRestPoseAsBase;
                        source.previewLayer.mix = source.GetMix(); 
                    }

                    obj.RemoveRestPoseAnimationFromAnimator();
                    //if (useRestPoseAsBase) // nvm still useful for non additive blending
                    //{
                        obj.AddRestPoseAnimationToAnimator();
                        var rpLayer = obj.GetRestPoseAnimationLayer(); 
                        if (rpLayer != null)
                        {
                            rpLayer.IsAdditive = false;
                            rpLayer.mix = 1;
                            rpLayer.MoveNoRemap(0); // Make the rest pose base layer the first layer to get evaluated in the animation player.
                        }
                        else
                        {
                            obj.animator.RecalculateLayerIndicesNoRemap();
                        }
                    //}   
                }
                void ArrangeAllSourcesForObject(ImportedAnimatable obj)
                {
                    if (obj == null || !obj.Visible || obj.animator == null) return;

                    obj.RebuildControllerIfNull();
                    obj.animator.ResetIKControllers();

                    if (obj.animationBank != null)
                    {
                        for (int a = 0; a < obj.animationBank.Count; a++)
                        {
                            var source = obj.animationBank[a];
                            if (source == null || source.previewLayer == null) continue; 
                            source.previewLayer.IsAdditive = true;
                            source.previewLayer.mix = source.GetMix();
                            source.previewLayer.MoveNoRemap(a, false); // Make the preview layer arrangement match the arrangement of the ui list in the editor. Animations placed lower in the ui list are evaluated later, and are arranged deeper into the .
                        }
                    } 
                    
                    obj.RemoveRestPoseAnimationFromAnimator();
                    obj.AddRestPoseAnimationToAnimator();
                    var rpLayer = obj.GetRestPoseAnimationLayer();
                    if (rpLayer != null)
                    {
                        rpLayer.IsAdditive = false;
                        rpLayer.mix = 1;
                        rpLayer.MoveNoRemap(0, true); // Make the rest pose base layer the first layer to get evaluated in the animation player. Also recalculates layer indices after.
                    }
                    else
                    {
                        obj.animator.RecalculateLayerIndicesNoRemap();
                    }
                }

                switch (currentSession.playbackMode)
                {
                    case PlayMode.Last_Edited:
                        foreach (var obj in currentSession.importedObjects) ArrangeActiveSourceForObject(obj);
                        break;
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects) ArrangeAllSourcesForObject(obj);              
                        break;
                    case PlayMode.Active_Only:
                        ArrangeActiveSourceForObject(currentSession.ActiveObject, false); // play the animation without additive blending
                        //ArrangeActiveSourceForObject(currentSession.ActiveObject);
                        break;
                    case PlayMode.Active_All:
                        ArrangeAllSourcesForObject(currentSession.ActiveObject);
                        break;
                }
            }
        }

        private float prePlayPlaybackPosition;
        public void PlayDefault() => Play(IsLooping, -1);
        public virtual void Play(bool loop=false, float startPos=-1)
        {
            if (currentSession == null || IsPlayingPreview) return;

            prePlayPlaybackPosition = PlaybackPosition; 
            
            void Begin()
            {
                StopEditingCurve(); 

                var activeObj = ActiveAnimatable;
                if (activeObj != null && !activeObj.Visible) activeObj.SetVisibility(this, true, activeObj.IsLocked, false);

                RenderingControl.SetRenderAnimationBones(false);
                IgnorePreviewObjects(); 

                ArrangePreviewLayers();
                //if (CurrentSource != null) this.testAnimation = CurrentSource.CompiledAnimation; // TODO: REMOVE
                if (startPos >= 0) PlaybackPosition = startPos;
                isPlaying = true;
                isSnapshot = false;
                this.loop = loop;

                if (playButton != null) playButton.SetActive(false);
                if (pauseButton != null) pauseButton.SetActive(true);
                if (stopButton != null) stopButton.SetActive(true);
            }
            void Fail()
            {
                swole.LogWarning("Unable to initiate playback due to failed compilation.");
            }

            CompileSourcesForPlayback(Begin, Fail, () => { swole.LogWarning("Cannot start a new compilation job: compiler is busy or the editor is in playback mode."); });
        }

        private float playSnapshotEndTime;
        private int playSnapshotEndFrame = -1;
        public virtual void PlaySnapshotUnclamped(float time) => PlaySnapshot(time, false, 0.3f, true);
        public virtual void PlaySnapshotUnclamped(float time, float endDelay) => PlaySnapshot(time, false, endDelay, true);
        public virtual void PlaySnapshotUnclamped(float time, float endDelay, bool undoable) => PlaySnapshot(time, false, endDelay, undoable);
        public virtual void PlaySnapshot(float time, bool clampTime = true, float endDelay = 0.3f, bool undoable = true)
        {
            if (currentSession == null || IsPlayingEntirety) return;

            playSnapshotEndTime = endDelay;
            int frame = Time.frameCount;
            playSnapshotEndFrame = frame;

            if (!IsPlayingPreview) prePlayPlaybackPosition = PlaybackPosition; 
             
            void Playback()
            {
                try
                {
                    PlaybackPosition = time;
                    isPlaying = true;
                    isSnapshot = true;
                    PlayStep(time, 0, true);
                    PlayStepLate();
                    
                    IEnumerator WaitToEnd()
                    {
                        while (playSnapshotEndTime > 0)
                        {
                            if (playSnapshotEndFrame != frame) yield break;

                            yield return null;
                            playSnapshotEndTime -= Time.deltaTime;
                        }

                        RefreshIKControllerActivation(true);
                        ForceApplyTransformStateChanges(3);
                        IEnumerator RefreshReferencePoses()
                        {
                            RefreshIKControllerActivation(true);
                            yield return null;
                            RefreshIKControllerActivation(true);
                            yield return new WaitForEndOfFrame();
                            ForceApplyTransformStateChanges(2);
                            if (PlaybackPosition == time)
                            {
                                SetReferencePoseFlags();
                                RefreshFlaggedReferencePoses();
                            }
                            isPlaying = false;
                            isSnapshot = false;
                            RefreshIKControllerActivation(true);

                            yield return null;

                            RecordRevertibleAction(new UndoableSetPlaybackPosition(this, currentSession.activeObjectIndex, ActiveAnimatable.editIndex, prePlayPlaybackPosition, PlaybackPosition));
                        }
                        StartCoroutine(RefreshReferencePoses());
                    }

                    if (playSnapshotEndTime > 0) StartCoroutine(WaitToEnd()); else WaitToEnd();
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }
            }
            void Begin()
            {
                try
                {
                    var activeObj = ActiveAnimatable;
                    if (activeObj != null && !activeObj.Visible) activeObj.SetVisibility(this, true, activeObj.IsLocked, false);

                    var activeState = ActivePreviewState;
                    if (activeState != null)
                    {
                        if (clampTime) time = Mathf.Clamp(time, 0, activeState.GetEstimatedDuration() - Mathf.Epsilon); // Try to keep time within the active animation's timeline.
                    }

                    ArrangePreviewLayers();

                    Playback();
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }
            }
            void Fail()
            {
                swole.LogWarning("Unable to initiate playback due to failed compilation.");
            }

            if (IsPlayingPreview)
            {
                Playback(); 
            }
            else
            {
                CompileSourcesForPlayback(Begin, Fail, () => { swole.LogWarning("Cannot start a new compilation job: compiler is busy or the editor is in playback mode."); });
            }
        }

        protected float CurrentFrameStep
        {
            get
            {
                var source = CurrentSource;
                if (source == null || source.rawAnimation == null) return 0;

                return 1f / source.rawAnimation.framesPerSecond;
            }
        }
        public float CurrentLength
        {
            get
            {
                if (timelineWindow != null) return timelineWindow.Length;

                var source = CurrentSource;
                if (source == null || source.rawAnimation == null) return 0;

                return source.timelineLength;
            }
        }
        public void SkipToFirstFrame()
        {
            float targetTime = 0;

            if (keyframeInstances.Count > 0)
            {
                TimelineKeyframe firstFrame = null;
                int minPos = int.MaxValue;
                foreach(var pair in keyframeInstances) if (pair.Value != null && pair.Key < minPos)
                    {
                        minPos = pair.Key;
                        firstFrame = pair.Value;
                    }
                if (firstFrame != null) 
                { 
                    targetTime = minPos * CurrentFrameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            } 
            else
            {
                PlaySnapshot(targetTime); 
            }
        }
        public void SkipToFinalFrame()
        {
            float targetTime = CurrentLength;
            float frameStep = CurrentFrameStep;
            if (frameStep > 0) targetTime = Mathf.Floor(CurrentLength / frameStep) * frameStep;

            if (keyframeInstances.Count > 0)
            {
                TimelineKeyframe firstFrame = null;
                int maxPos = int.MinValue;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key > maxPos)
                    {
                        maxPos = pair.Key;
                        firstFrame = pair.Value;
                    }
                if (firstFrame != null)
                {
                    targetTime = maxPos * CurrentFrameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }
        public void SkipToNextFrame()
        {
            float frameStep = CurrentFrameStep;
            if (frameStep <= 0) return;

            float targetTime = PlaybackPosition;
            if (keyframeInstances.Count > 0)
            {
                int currentTimelinePosition = Mathf.FloorToInt((PlaybackPosition + (frameStep * 0.1f)) / frameStep);
                int nextTimelinePosition = int.MaxValue;

                bool flag = false;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key > currentTimelinePosition && pair.Key < nextTimelinePosition)
                    {
                        nextTimelinePosition = pair.Key;
                        flag = true;
                    }
                if (flag)
                {
                    targetTime = nextTimelinePosition * frameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }
        public void SkipToPreviousFrame()
        {
            float frameStep = CurrentFrameStep;
            if (frameStep <= 0) return;

            float targetTime = PlaybackPosition;
            if (keyframeInstances.Count > 0)
            {
                int currentTimelinePosition = Mathf.FloorToInt((PlaybackPosition - (frameStep * 0.1f)) / frameStep); 
                int prevTimelinePosition = int.MinValue;

                bool flag = false;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key < currentTimelinePosition && pair.Key > prevTimelinePosition)
                    {
                        prevTimelinePosition = pair.Key;
                        flag = true;
                    }
                if (flag)
                {
                    targetTime = prevTimelinePosition * frameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }

        public virtual void SetReferencePoseFlags()
        {
            if (currentSession != null && currentSession.importedObjects != null)
            {
                switch (currentSession.playbackMode)
                {
                    case PlayMode.Last_Edited:
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.Visible) continue;
                            obj.setNextPoseAsReference = true;
                        }
                        break;
                    case PlayMode.Active_Only:
                    case PlayMode.Active_All:
                        var activeObj = currentSession.ActiveObject;
                        if (activeObj != null) activeObj.setNextPoseAsReference = true;
                        break;

                }
            }
        }

        public void Pause() => Pause(true);
        public virtual void Pause(bool undoable)
        {
            if (!isPlaying) return;
            RenderingControl.SetRenderAnimationBones(true);
            StopIgnoringPreviewObjects();

            isPlaying = false;
            if (playButton != null) playButton.SetActive(true);
            if (pauseButton != null) pauseButton.SetActive(false);
            if (stopButton != null) stopButton.SetActive(false);

            foreach(var obj in currentSession.importedObjects)
            {
                if (obj == null) continue;
                obj.DisablePlaybackOnlyDevices();

                if (obj.animationBank == null) continue;
                foreach (var source in obj.animationBank)
                {
                    if (source == null) continue;
                    PauseAudioSyncImports(source);  
                }
            }
            var activeObj = currentSession.ActiveObject; 
            switch (currentSession.playbackMode) // Makes sure that the paused pose does not include any additive playback only effects
            {
                case PlayMode.Last_Edited:
                case PlayMode.All:
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj == null || !obj.Visible) continue;

                        // reset playback
                        if (obj.animator != null) obj.animator.UpdateStep(0);   
                        obj.setNextPoseAsReference = true;
                    }
                    break;
                case PlayMode.Active_Only:
                case PlayMode.Active_All:
                    if (activeObj != null)
                    {
                        // reset playback
                        if (activeObj.animator != null) activeObj.animator.UpdateStep(0);
                        activeObj.setNextPoseAsReference = true; 
                    }
                    break;
            }

            //SetReferencePoseFlags();
            RefreshIKControllerActivation();

            if (undoable) RecordRevertibleAction(new UndoableSetPlaybackPosition(this, currentSession.activeObjectIndex, ActiveAnimatable.editIndex, prePlayPlaybackPosition, PlaybackPosition)); 
        }
        public virtual void StopAndReset()
        {
            PlaybackPosition = 0;
            Stop();
        }
        public void Stop() => Stop(true);
        public virtual void Stop(bool undoable)
        {
            var activeObj = currentSession.ActiveObject;
            switch (currentSession.playbackMode)
            {
                case PlayMode.Last_Edited:
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj == null || obj.IsLocked || !obj.Visible) continue;

                        var source = obj.CurrentSource;
                        if (source == null || source.IsLocked) continue;
                        var state = source.PreviewState;
                        if (state == null) continue;
                        state.SetNormalizedTime(0);

                        // reset playback
                        //if (obj.animator != null) obj.animator.UpdateStep(0);
                        //obj.setNextPoseAsReference = true;

                        StopAudioSyncImports(source);
                    }
                    break;
                case PlayMode.All:
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj == null || obj.IsLocked || !obj.Visible || obj.animationBank == null) continue;
                        foreach (var source in obj.animationBank)
                        {
                            if (source == null || source.IsLocked) continue;
                            var state = source.PreviewState;
                            if (state == null) continue;
                            state.SetNormalizedTime(0);

                            StopAudioSyncImports(source);
                        }

                        // reset playback
                        //if (obj.animator != null) obj.animator.UpdateStep(0);
                        //obj.setNextPoseAsReference = true;
                    }
                    break;
                case PlayMode.Active_Only:
                    if (activeObj != null && !activeObj.IsLocked)
                    {
                        var source = activeObj.CurrentSource;
                        if (source != null && !source.IsLocked)
                        {
                            var state = source.PreviewState;
                            if (state != null)
                            {
                                state.SetNormalizedTime(0);

                                // reset playback
                                //if (activeObj.animator != null) activeObj.animator.UpdateStep(0);
                                //activeObj.setNextPoseAsReference = true;
                            }

                            StopAudioSyncImports(source);
                        }
                    }
                    break;
                case PlayMode.Active_All:
                    if (activeObj != null && activeObj.animationBank != null && !activeObj.IsLocked)
                    {
                        foreach (var source in activeObj.animationBank)
                        {
                            if (source == null || source.IsLocked) continue;
                            var state = source.PreviewState;
                            if (state == null) continue;
                            state.SetNormalizedTime(0);

                            StopAudioSyncImports(source);
                        }

                        // reset playback
                        //if (activeObj.animator != null) activeObj.animator.UpdateStep(0);
                        //activeObj.setNextPoseAsReference = true;
                    }
                    break;

            }

            Pause(undoable); 
        }

        private bool isCompiling;
        public bool IsCompiling => isCompiling;
        private bool cancelCompilation;
        public void CancelCompilation()
        {
            if (!isCompiling) return;
            cancelCompilation = true;
            if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);
        }
        public struct SourceCollection
        {
            public string name;
            public ICollection<AnimationSource> sources;
        }
        public delegate void CompilationDelegate();
        public bool CompileCollection(SourceCollection collection, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false) => CompileCollection(collection.name, collection.sources, callback, force, recompile, onComplete, onCancel, verbose);
        public bool CompileCollection(string collectionName, ICollection<AnimationSource> sources, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false) => Compile(sources, $"COMPILING ANIMATIONS FOR {collectionName}", callback, force, recompile, onComplete, onCancel, verbose);
        public bool Compile(ICollection<AnimationSource> sources, string message = null, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false)
        {
            if (IsPlayingPreview || (isCompiling && !force) || sources == null) return false;

            if (sources.Count <= 0)
            {
                onComplete?.Invoke();
                callback?.Invoke();
                return true;
            }

            isCompiling = true;
            cancelCompilation = false;

            UIProgressBar progressBar = null;
            if (compilationWindow != null) 
            {
                compilationWindow.gameObject.SetActive(true);

                progressBar = compilationWindow.GetComponentInChildren<UIProgressBar>(true);
                if (progressBar != null) progressBar.SetProgress(0);

                var info = compilationWindow.FindDeepChildLiberal("Info");
                if (info != null) CustomEditorUtils.SetComponentText(info, message == null ? "COMPILING ANIMATIONS" : message);

                var cancel = compilationWindow.FindDeepChildLiberal("Cancel");
                if (cancel != null) CustomEditorUtils.SetButtonOnClickAction(cancel, CancelCompilation);
            }
            
            IEnumerator Compilation()
            {
                int i = 0;
                foreach (var source in sources)
                {
                    if (cancelCompilation) 
                    { 
                        try
                        {
                            onCancel?.Invoke();
                        } 
                        catch (Exception e)
                        {
                            swole.LogError(e);
                        }
                        break; 
                    }
                    i++;
                    try
                    {
                        try
                        {
                            if (progressBar != null) progressBar.SetProgress(i / (float)sources.Count);
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e);
                        }

                        if (source == null || (source.IsCompiled && !recompile)) continue;
                        source.Compile();
                    }
                    catch (Exception e)
                    {
                        swole.LogError($"Encountered error while compiling '{source.DisplayName}'");
                        swole.LogError(e);
                    }
                    yield return null;
                }

                isCompiling = false;
                if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);

                try
                {
                    if (i >= sources.Count) onComplete?.Invoke();
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }
                try
                {
                    callback?.Invoke();
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }

                if (verbose) swole.Log($"Finished compilation task [{i}/{sources.Count} SOURCES COMPILED]");
            } 

            StartCoroutine(Compilation());
            return true;
        }
        public bool CompileAll(ICollection<SourceCollection> collections, CompilationDelegate callback = null, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false)
        {
            if (IsPlayingPreview || isCompiling || collections == null) return false;

            if (collections.Count <= 0)
            {
                onComplete?.Invoke();
                callback?.Invoke();
                return true;
            }

            isCompiling = true;
            cancelCompilation = false;
            bool pauseToken = false;

            IEnumerator Compilation()
            {
                int i = 0;
                foreach(var collection in collections)
                {
                    if (cancelCompilation)
                    {
                        try
                        {
                            onCancel?.Invoke();
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e.Message);
                        }
                        break;
                    }
                    i++;
                    if (collection.sources == null) continue;

                    pauseToken = true; 
                    if (!Compile(collection.sources, $"COMPILING {collection.name} [{i}/{collections.Count} TASKS]", () =>
                    {
                        pauseToken = false;
                        isCompiling = true;
                    }, true, recompile)) pauseToken = false;
                    while (pauseToken) 
                    {
                        if (cancelCompilation)
                        {
                            try
                            {
                                onCancel?.Invoke();
                            }
                            catch (Exception e)
                            {
                                swole.LogError(e);
                            }
                            break;
                        }
                        yield return null; 
                    }
                } 

                isCompiling = false;
                if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);

                try
                {
                    if (verbose) swole.Log($"Finished multi-compile [{i}/{collections.Count} TASKS COMPLETED]");
                    if (i >= collections.Count) onComplete?.Invoke();
                    callback?.Invoke();
                } 
                catch(Exception e)
                {
                    swole.LogError(e);
                }
            }
             
            StartCoroutine(Compilation());
            return true;
        }

        private readonly List<int> visibleSources = new List<int>();
        protected virtual void PlayStep(float previousPlaybackPosition, float deltaTime, bool disablePlaybackOnlyDevices = false)
        {
            if (IsCompiling || currentSession == null) return;

            var activeAnimatable = ActiveAnimatable;
            switch (currentSession.playbackMode)
            {
                case PlayMode.All: // Playback all visible animatables and their visible animation sources
                    if (currentSession.importedObjects != null)
                    {
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.Visible || obj.animator == null || obj.animationBank == null) continue;
                            if (disablePlaybackOnlyDevices) obj.DisablePlaybackOnlyDevices(); else obj.EnablePlaybackOnlyDevices();
                            AnimationSource startSource = obj.CurrentSource;
                            foreach (var source in obj.animationBank)
                            {
                                if (source == null) continue;
                                if (source.Visible)
                                {
                                    if (source.previewLayer != null) source.previewLayer.SetActive(true);
                                }
                                else
                                {
                                    if (source.previewLayer != null) source.previewLayer.SetActive(false);  
                                    continue;
                                }
                                var sourceState = source.PreviewState;
                                if (sourceState == null) continue;

                                var playPos = PlaybackPosition;
                                if (obj.IsLocked || source.IsLocked) playPos = source.PlaybackPosition;

                                PlayAudioSyncImports(source, previousPlaybackPosition, playPos, deltaTime);
                                sourceState.SetTime(playPos);
                                if (obj == activeAnimatable) source.PlaybackPosition = playPos;
                                if (startSource == null) startSource = source; 
                            }

                            obj.animator.UpdateStep(0); 
                        }
                    }
                    break;
                case PlayMode.Last_Edited:  // Playback all visible animatables and their last edited animation source
                    if (currentSession.importedObjects != null)
                    {
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.Visible || obj.animator == null || obj.animationBank == null) continue;
                            if (disablePlaybackOnlyDevices) obj.DisablePlaybackOnlyDevices(); else obj.EnablePlaybackOnlyDevices();

                            visibleSources.Clear();

                            for (int i = 0; i < obj.animationBank.Count; i++)
                            {
                                var source = obj.animationBank[i];
                                if (source == null || !source.Visible) continue;
                                if (i != obj.editIndex)
                                {
                                    if (source.previewLayer != null)
                                    {
                                        source.previewLayer.SetActive(false); // Temporarily disable the layer since it's not the one being edited
                                        visibleSources.Add(i);
                                    }
                                    continue;
                                }
                                else
                                {
                                    if (source.previewLayer != null) source.previewLayer.SetActive(true);
                                }
                                var sourceState = source.PreviewState;
                                if (sourceState == null) continue;

                                var playPos = PlaybackPosition;
                                if (obj.IsLocked || source.IsLocked) playPos = source.PlaybackPosition;

                                PlayAudioSyncImports(source, previousPlaybackPosition, playPos, deltaTime);
                                sourceState.SetTime(playPos);
                                if (obj == activeAnimatable) source.PlaybackPosition = playPos;
                            }

                            obj.animator.UpdateStep(0);

                            foreach (int index in visibleSources) obj.animationBank[index].previewLayer.SetActive(true); // Re-enable the layers that were temporarily disabled                            
                        }
                    }
                    break;
                case PlayMode.Active_All:  // Playback the active animatable and its visible animation sources
                    if (activeAnimatable != null && activeAnimatable.animator != null && activeAnimatable.animationBank != null)
                    {
                        if (disablePlaybackOnlyDevices) activeAnimatable.DisablePlaybackOnlyDevices(); else activeAnimatable.EnablePlaybackOnlyDevices();
                        AnimationSource startSource = activeAnimatable.CurrentSource; 
                        foreach (var source in activeAnimatable.animationBank)
                        {
                            if (source == null) continue;
                            if (source.Visible)
                            {
                                if (source.previewLayer != null) source.previewLayer.SetActive(true);
                            } 
                            else
                            {
                                if (source.previewLayer != null) source.previewLayer.SetActive(false);
                                continue;
                            }
                            var sourceState = source.PreviewState;
                            if (sourceState == null) continue;

                            var playPos = PlaybackPosition;
                            if (activeAnimatable.IsLocked || source.IsLocked) playPos = source.PlaybackPosition;

                            PlayAudioSyncImports(source, previousPlaybackPosition, playPos, deltaTime);
                            source.PlaybackPosition = playPos;
                            sourceState.SetTime(source.PlaybackPosition);
                            if (startSource == null) startSource = source;
                        }

                        activeAnimatable.animator.UpdateStep(0);
                    }
                    break;
                case PlayMode.Active_Only:  // Playback the active animatable and its last edited animation source
                    if (activeAnimatable != null && activeAnimatable.animator != null && activeAnimatable.animationBank != null)
                    {
                        if (disablePlaybackOnlyDevices) activeAnimatable.DisablePlaybackOnlyDevices(); else activeAnimatable.EnablePlaybackOnlyDevices();
                        visibleSources.Clear();

                        for (int i = 0; i < activeAnimatable.animationBank.Count; i++)
                        {
                            var source = activeAnimatable.animationBank[i];
                            if (source == null) continue; 
                            if (i != activeAnimatable.editIndex)
                            {
                                if (source.previewLayer != null)
                                {
                                    source.previewLayer.SetActive(false); // Temporarily disable the layer since it's not the one being edited
                                    if (source.Visible) visibleSources.Add(i);
                                }
                                continue;
                            } 
                            else
                            {
                                if (source.previewLayer != null) source.previewLayer.SetActive(true);                               
                            }
                            var sourceState = source.PreviewState;
                            if (sourceState == null) continue;

                            var playPos = PlaybackPosition;
                            if (activeAnimatable.IsLocked || source.IsLocked) playPos = source.PlaybackPosition;

                            PlayAudioSyncImports(source, previousPlaybackPosition, playPos, deltaTime);
                            source.PlaybackPosition = playPos;
                            sourceState.SetTime(source.PlaybackPosition); 
                        }

                        activeAnimatable.animator.UpdateStep(0);

                        foreach (int index in visibleSources) activeAnimatable.animationBank[index].previewLayer.SetActive(true); // Re-enable the layers that were temporarily disabled
                    }
                    break;
            }
        }
        public virtual void Step()
        {
            if (IsCompiling) return;

            InputStep();

            if (IsPlayingPreview)
            {
                if (!isSnapshot)
                {
                    if (currentSession == null)
                    {
                        Pause();
                    }
                    else
                    {
                        var state = ActivePreviewState;
                        if (loop && state != null)
                        {
                            var length = state.GetEstimatedDuration();
                            if (length > 0)
                            {
                                if (playbackSpeed >= 0)
                                {
                                    while (playbackPosition >= length)
                                    {
                                        playbackPosition -= length;
                                    }
                                }
                                else
                                {
                                    while (playbackPosition <= 0)
                                    {
                                        playbackPosition += length;
                                    }
                                }
                            }
                        }

                        float scaledDeltaTime = Time.deltaTime * playbackSpeed;
                        float prevPlaybackPos = PlaybackPosition;
                        PlaybackPosition = prevPlaybackPos + scaledDeltaTime;

                        PlayStep(prevPlaybackPos, scaledDeltaTime); 
                    }
                }
            } 
            else
            {
                EditStep();
            }
        } 
        protected virtual void EditStep()
        {
        }
        protected readonly List<Transform> tempTransforms = new List<Transform>();
        protected virtual void RecordPose(List<Transform> affectedTransforms, bool useReferencePose, bool updateReferencePose, bool updateRecordingStates = true, bool createFromPreviousKey = true, bool onlyRecordChangedData = true, bool undoable = true) => RecordPose(CurrentSource, affectedTransforms, useReferencePose, updateReferencePose, updateRecordingStates, createFromPreviousKey, onlyRecordChangedData, undoable);
        protected virtual void RecordPose(AnimationSource source, List<Transform> affectedTransforms, bool useReferencePose, bool updateReferencePose, bool updateRecordingStates = true, bool createFromPreviousKey = true, bool onlyRecordChangedData = true, bool undoable = true)
        {
            if (source == null) return;
            
            tempTransforms.Clear();
            foreach (var transform in affectedTransforms)
            {
                var bone = FindPoseableBone(transform);
                if (bone == null) continue;

                //tempTransforms.Add(transform);
                tempTransforms.Add(bone.transform);
                if (updateRecordingStates && recordingStates.TryGetValue(bone.transform, out var state)) state.CheckAndApplyChanges();  
            }

            UndoableEditAnimationSourceData editRecord = null;
            if (undoable && source.rawAnimation != null)
            {
                editRecord = CurrentAnimationEditRecord;
                if (editRecord != null)
                {
                    foreach (var transform in tempTransforms) 
                    { 
                        editRecord.SetOriginalPoseTransformState(transform); 

                        if (source.rawAnimation.TryGetTransformCurves(transform.name, out _, out _, out var mainCurve, out var baseCurve))
                        {
                            if (mainCurve is TransformCurve tc) editRecord.SetOriginalTransformCurveState(tc, false);
                            if (mainCurve is TransformLinearCurve tlc) editRecord.SetOriginalTransformLinearCurveState(tlc, false);

                            if (baseCurve is TransformCurve btc) editRecord.SetOriginalTransformCurveState(btc, true);
                            if (baseCurve is TransformLinearCurve btlc) editRecord.SetOriginalTransformLinearCurveState(btlc, true); 
                        }
                    }
                }
            }

            float time = PlaybackPosition;
            if (timelineWindow != null)
            {
                decimal epsilon = 1m / AnimationTimeline.frameRate;
                //time = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(timelineWindow.GetNonlinearTimeFromTime(time)));
                var tempTime = timelineWindow.CalculateFrameTimeAtTimelinePosition((decimal)timelineWindow.GetNonlinearTimeFromTime(time));
                time = (float)(Mathf.FloorToInt((float)(tempTime / epsilon)) * epsilon);
            }
            
            var animatable = ActiveAnimatable;
            source.InsertKeyframes(createFromPreviousKey, useReferencePose, animatable, time, (timelineWindow == null ? null : timelineWindow.CalculateFrameAtTimelinePosition), onlyRecordChangedData, tempTransforms);

            if (updateReferencePose)
            {
                animatable.setNextPoseAsReference = true; 
                RefreshFlaggedReferencePoses();
            }
            
            RefreshTimeline();

            if (undoable && editRecord != null && source.rawAnimation != null)
            {
                foreach (var transform in tempTransforms)
                {
                    editRecord.RecordPoseTransformState(transform);

                    if (source.rawAnimation.TryGetTransformCurves(transform.name, out _, out _, out var mainCurve, out var baseCurve))
                    {
                        if (mainCurve is TransformCurve tc) editRecord.RecordTransformCurveEdit(tc, false);
                        if (mainCurve is TransformLinearCurve tlc) editRecord.RecordTransformLinearCurveEdit(tlc, false);

                        if (baseCurve is TransformCurve btc) editRecord.RecordTransformCurveEdit(btc, true);
                        if (baseCurve is TransformLinearCurve btlc) editRecord.RecordTransformLinearCurveEdit(btlc, true);
                    }
                }
            } 
        }

        #region Realtime Mirroring
        protected readonly List<Transform> tempMirrorTransforms = new List<Transform>();
        protected readonly List<Transform> tempMirrorTransforms2 = new List<Transform>();
        protected virtual void GetMirrorTransforms(Transform root, List<Transform> transforms, List<Transform> outputList, bool ignoreTransformsAlreadyPresent = false)
        {
            if (outputList == null) outputList = new List<Transform>();

            foreach (var transform in transforms)
            {
                string mirroredName = Utils.GetMirroredName(transform.name);
                if (mirroredName != transform.name)
                {
                    var mirrorTransform = root.FindDeepChild(mirroredName);
                    if (mirrorTransform != null && (!ignoreTransformsAlreadyPresent || !transforms.Contains(mirrorTransform))) outputList.Add(mirrorTransform);
                }
            }
        }
        protected virtual void AppendMirrorTransforms(Transform root, List<Transform> transforms)
        {
            tempMirrorTransforms2.Clear();
            GetMirrorTransforms(root, transforms, tempMirrorTransforms2);

            foreach (var transform in tempMirrorTransforms2) if (!transforms.Contains(transform)) transforms.Add(transform);
        }
        protected virtual List<Transform> MirrorTransformManipulation(Transform root, List<Transform> affectedTransforms, bool absolute, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            tempMirrorTransforms.Clear();
            GetMirrorTransforms(root, affectedTransforms, tempMirrorTransforms, true);

            if (absolute)
            {
                AppendProxyBoneTargets(tempMirrorTransforms);
                FlipPose(null, root, tempMirrorTransforms, true, true, true, false, false);
            }
            else
            {
                Matrix4x4 rootL2W = Matrix4x4.identity;
                Matrix4x4 rootW2L = Matrix4x4.identity;
                Quaternion rootRot = Quaternion.identity;
                Quaternion iRootRot = Quaternion.identity;

                if (root != null)
                {
                    rootL2W = root.localToWorldMatrix;
                    rootW2L = root.worldToLocalMatrix;
                    rootRot = root.rotation;
                    iRootRot = Quaternion.Inverse(rootRot);
                }

                //relativeOffset = gizmoWorldRotation * relativeOffset;
                relativeOffsetWorld = iRootRot * relativeOffsetWorld; 
                //relativeRotation = gizmoWorldRotation * relativeRotation;
                relativeRotationWorld = iRootRot * relativeRotationWorld;  
                Maths.MirrorPositionAndRotationX(relativeOffsetWorld, relativeRotationWorld, out relativeOffsetWorld, out relativeRotationWorld);
                relativeOffsetWorld = rootRot * relativeOffsetWorld;
                relativeRotationWorld = rootRot * relativeRotationWorld; 

                foreach (var mirrorTransform in tempMirrorTransforms)
                {
                    if (mirrorTransform == null) continue;

                    mirrorTransform.GetPositionAndRotation(out var pos, out var rot);
                    pos = pos + relativeOffsetWorld;
                    rot = relativeRotationWorld * rot;
                    mirrorTransform.SetPositionAndRotation(pos, rot); 
                    mirrorTransform.localScale = Vector3.Scale(mirrorTransform.localScale, relativeScale);   
                }
            }

            tempMirrorTransforms.AddRange(affectedTransforms);
            return tempMirrorTransforms;
        }
        #endregion

        public virtual bool IsSelected(Transform t) => IsSelected(t == null ? null : t.gameObject);
        public virtual bool IsSelected(GameObject go)
        {
            if (go == null || runtimeEditor == null) return false;

            if (runtimeEditor.IsSelected(go)) return true;

            if (MirrorMode != MirroringMode.Off)
            {
                var obj = ActiveAnimatable; 
                if (obj != null)
                {
                    string mirroredName = Utils.GetMirroredName(go.name); 
                    if (mirroredName != go.name)
                    {
                        var mirrorTransform = obj.instance.transform.FindDeepChild(mirroredName);
                        if (mirrorTransform != null) return runtimeEditor.IsSelected(mirrorTransform.gameObject); 
                    }
                }
            }

            return false;
        }

        protected virtual void BeginManipulationAction(List<Transform> affectedTransforms)
        {
            var editRecord = BeginNewAnimationEditRecord();
            if (editRecord != null)
            {
                foreach (var transform in affectedTransforms)
                {
                    if (transform != null) editRecord.SetOriginalPoseTransformState(transform);
                }
            }
        }
        protected virtual void ManipulationActionStep(List<Transform> affectedTransforms, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            if (IsRecording) nextRecordStepDelay = 2;

            var obj = ActiveAnimatable;
            if (obj == null) return;

            if (obj.animator != null) affectedTransforms.RemoveAll(i => obj.animator.IsIKControlledBone(i) || obj.animator.IsFKControlledBone(i));  

            if (MirrorMode != MirroringMode.Off)
            {
                affectedTransforms = MirrorTransformManipulation(obj.animator == null ? (obj.instance == null ? null : obj.instance.transform) : obj.animator.RootBoneUnity, affectedTransforms, MirrorMode == MirroringMode.Absolute, relativeOffsetWorld, relativeRotationWorld, relativeScale, gizmoWorldPosition, gizmoWorldRotation);
            }

            foreach (var transform in affectedTransforms)
            { 
                if (IsTransformLocked(transform)) LockTransform(transform); // Update the transform's locked position/rotation to the manipulated one 
            } 
        }
        protected virtual void RecordManipulationAction(List<Transform> affectedTransforms, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            var obj = ActiveAnimatable;
            if (obj == null) return;

            var editRecord = CurrentAnimationEditRecord;

            if (obj.animator != null) affectedTransforms.RemoveAll(i => obj.animator.IsIKControlledBone(i) || obj.animator.IsFKControlledBone(i));  
            if (MirrorMode != MirroringMode.Off)
            {
                tempMirrorTransforms.Clear();
                tempMirrorTransforms.AddRange(affectedTransforms);
                AppendMirrorTransforms(obj.instance == null ? null : obj.instance.transform, tempMirrorTransforms);
                affectedTransforms = tempMirrorTransforms; 
            }
            foreach(var transform in affectedTransforms)
            {
                if (transform != null)
                {
                    editRecord?.RecordPoseTransformState(transform);
                    if (IsTransformLocked(transform)) LockTransform(transform);  // Update the transform's locked position/rotation to the manipulated one 
                }
            }
            
            if (CanRecord && affectedTransforms.Count > 0) 
            {
                nextRecordStepDelay = 1;

                bool useReferencePose = obj.referencePose != null;

                RecordPose(affectedTransforms, useReferencePose, /*useReferencePose*/false, true, true, true, true);

                if (useReferencePose)
                {
                    useReferencePoseForNextRecordStep = useReferencePose; 
                    updateReferencePoseForNextRecordStep = true;
                    nextRecordStepIsImmediate = true; 
                }
            }

            CommitAnimationEditRecord();
        }

        protected List<CameraProxy> cameraProxies = new List<CameraProxy>();
        protected virtual void PlayStepLate()
        {
            if (currentSession != null)
            {
                var activeAnimatable = ActiveAnimatable;

                int nonRelativeCamera = -1;
                cameraProxies.Clear();

                switch (currentSession.playbackMode)
                {
                    case PlayMode.All:
                    case PlayMode.Last_Edited:
                        if (currentSession.importedObjects != null)
                        {
                            foreach (var obj in currentSession.importedObjects)
                            {
                                if (obj.animator == null) continue;

                                obj.animator.LateUpdateStep(Time.deltaTime);

                                if (currentSession.CameraSettings.mode == CameraMode.Game && obj.cameraProxy != null)
                                {
                                    if (nonRelativeCamera < 0 || ReferenceEquals(activeAnimatable, obj)) nonRelativeCamera = cameraProxies.Count;
                                    cameraProxies.Add(obj.cameraProxy);
                                }
                            }
                        }
                        break;
                    case PlayMode.Active_All:
                    case PlayMode.Active_Only:
                        if (activeAnimatable != null && activeAnimatable.animator != null)
                        {
                            activeAnimatable.animator.LateUpdateStep(Time.deltaTime);

                            if (currentSession.CameraSettings.mode == CameraMode.Game && activeAnimatable.cameraProxy != null)
                            {
                                nonRelativeCamera = 0;
                                cameraProxies.Add(activeAnimatable.cameraProxy);
                            }
                        }
                        break;
                }

                if (nonRelativeCamera >= 0)
                {
                    cameraProxies[nonRelativeCamera].SetTargetCamera(runtimeEditor.EditorCamera);
                    cameraProxies[nonRelativeCamera].ApplyChanges(false);
                    cameraProxies.RemoveAt(nonRelativeCamera);

                    foreach(var cam in cameraProxies)
                    {
                        cam.SetTargetCamera(runtimeEditor.EditorCamera);   
                        cam.ApplyChanges(true); 
                    }
                }
            }
        }
        public virtual void RefreshFlaggedReferencePoses(bool playbackModeBased = true)
        {
            if (currentSession != null)
            {
                bool nullify = false;
                if (playbackModeBased)
                {
                    if (currentSession.playbackMode == PlayMode.Active_Only || currentSession.playbackMode == PlayMode.Last_Edited) nullify = true;
                }
                 
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        try
                        {
                            if (obj == null) continue;
                            if (obj.setNextPoseAsReference)
                            {
                                if (nullify) obj.referencePose = null; else obj.SetCurrentPoseAsReference();  
                                obj.setNextPoseAsReference = false;
                            }
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e.Message);
                        }
                    }
                }
            }
        }
        public virtual void LateStep()
        {
            if (IsCompiling) return;

            if (IsPlayingPreview)
            {
                if (!isSnapshot)
                {
                    var state = ActivePreviewState;
                    if (playbackSpeed == 0 || state == null)
                    {
                        Pause();
                    }
                    else
                    {
                        if (!loop)
                        {
                            if (playbackSpeed >= 0)
                            {
                                var length = state.GetEstimatedDuration();
                                if (PlaybackPosition >= length)
                                {
                                    PlaybackPosition = 0;
                                    Stop();
                                }
                            }
                            else
                            {
                                if (PlaybackPosition <= 0)
                                {
                                    PlaybackPosition = state.GetEstimatedDuration();
                                    Stop();
                                }
                            }
                        }
                    }

                    PlayStepLate();  
                }
            } 

            //RefreshFlaggedReferencePoses(); Moved to RecordChanges, so it happens after IK
        }

        #region Actions

        [Serializable]
        public class ClickableOption
        {
            public GameObject gameObject;
            public bool requireActiveAnimatable = true;
            public bool requireActiveAnimationSource = true;
            public bool requireBoneSelection = false;
            public BoolParameterlessDelegate customRequirement;

            public void Refresh(AnimationEditor editor)
            {
                if (gameObject == null || editor == null)
                {
                    if (gameObject != null) gameObject.SetActive(false); 
                    return;
                }

                var animatable = editor.ActiveAnimatable;
                if (animatable == null && (requireActiveAnimatable || requireActiveAnimationSource))
                {
                    gameObject.SetActive(false);
                    return;
                }
                var source = animatable.CurrentSource;
                if (source == null && requireActiveAnimationSource)
                {
                    gameObject.SetActive(false);
                    return;
                }
                if (requireBoneSelection && editor.SelectedBoneCount <= 0)
                {
                    gameObject.SetActive(false);
                    return;
                }

                if (customRequirement != null && !customRequirement.Invoke())
                {
                    gameObject.SetActive(false);
                    return;
                }

                gameObject.SetActive(true);  
            }
        }

        [Header("Clickables")]
        public List<ClickableOption> clickableOptions = new List<ClickableOption>();

        public struct TransformState
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 localScale;

            public TransformState(Transform t, bool worldSpace)
            {
                if (worldSpace)
                {
                    t.GetPositionAndRotation(out pos, out rot);
                } 
                else
                {
                    t.GetLocalPositionAndRotation(out pos, out rot);
                }
                localScale = t.localScale; 
            }

            public void Apply(Transform t, bool worldSpace)
            {
                if (worldSpace)
                {
                    t.SetPositionAndRotation(pos, rot);
                }
                else
                {
                    t.SetLocalPositionAndRotation(pos, rot);
                }
                t.localScale = localScale;
            }

            public TransformState WorldToRootSpace(Transform root) => WorldToRootSpace(root.worldToLocalMatrix, root.rotation);
            public TransformState RootToWorldSpace(Transform root) => RootToWorldSpace(root.localToWorldMatrix, root.rotation);

            public TransformState WorldToRootSpace(Matrix4x4 worldToRoot, Quaternion rootRot)
            {
                var ts = this;

                ts.pos = worldToRoot.MultiplyPoint(pos);
                ts.rot = Quaternion.Inverse(rootRot) * rot;

                return ts;
            }
            public TransformState RootToWorldSpace(Matrix4x4 rootToWorld, Quaternion rootRot)
            {
                var ts = this;

                ts.pos = rootToWorld.MultiplyPoint(pos);
                ts.rot = rootRot * rot;

                return ts;
            }

        }

        private static readonly List<GameObject> _tempGameObjects = new List<GameObject>();
        private static readonly List<Transform> _tempTransforms = new List<Transform>();
        private static readonly List<Transform> _tempTransforms2 = new List<Transform>();
        private static readonly List<Transform> _tempTransforms3 = new List<Transform>(); 
        private static readonly List<PoseableBone> _tempPoseableBones = new List<PoseableBone>();
        private static readonly Dictionary<Transform, TransformState> _tempTransformStates = new Dictionary<Transform, TransformState>();

        protected void GetAllPoseableBones(List<Transform> list, List<PoseableBone> poseableList)
        {
            if (list == null && poseableList == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            foreach (var transform in animatable.animator.Bones.bones)
            {
                if (transform == null) continue;

                var bone = FindPoseableBone(transform);
                if (bone == null) continue;

                if (list != null) list.Add(bone.transform);
                if (poseableList != null) poseableList.Add(bone);
            }
        }
        protected void GetSelectedPoseableBones(List<Transform> list, List<PoseableBone> poseableList)
        {
            if (list == null && poseableList == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempGameObjects.Clear();
            runtimeEditor.QueryActiveSelection(_tempGameObjects);

            foreach (var go in _tempGameObjects)
            {
                if (go == null) continue;

                var bone = FindPoseableBone(go.transform);
                if (bone == null) continue;

                if (list != null) list.Add(bone.transform);
                if (poseableList != null) poseableList.Add(bone);
            }
        }
        protected void GetAllPoseableBonesWithKeyframes(AnimationSource source, List<Transform> list, List<PoseableBone> poseableList)
        {
            if ((list == null && poseableList == null) || source == null || source.rawAnimation == null || source.rawAnimation.transformAnimationCurves == null) return; 

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            foreach (var transform in animatable.animator.Bones.bones)
            {
                if (transform == null) continue;

                var bone = FindPoseableBone(transform);
                if (bone == null) continue;

                string boneName = animatable.animator.RemapBoneName(transform.name).AsID();

                bool hasKeyframes = false;
                foreach(var info in source.rawAnimation.transformAnimationCurves)
                {
                    if (info.infoMain.curveIndex < 0) continue;

                    ITransformCurve curve = info.infoMain.isLinear ? source.rawAnimation.transformLinearCurves[info.infoMain.curveIndex] : source.rawAnimation.transformCurves[info.infoMain.curveIndex]; 
                    if (curve == null || curve.TransformName.AsID() != boneName || !curve.HasKeyframes) continue;

                    hasKeyframes = true;
                    break;
                }
                if (!hasKeyframes) continue;

                if (list != null) list.Add(bone.transform);
                if (poseableList != null) poseableList.Add(bone);
            }
        }

        private readonly List<ProxyBone> tempProxyBones = new List<ProxyBone>();
        protected void AppendProxyBoneTargets(List<Transform> list)
        {
            if (list == null) return;

            tempProxyBones.Clear();
            foreach(var t in list)
            {
                if (t == null) continue;

                var proxyBone = t.GetComponent<ProxyBone>();
                if (proxyBone != null && !tempProxyBones.Contains(proxyBone)) tempProxyBones.Add(proxyBone);
            }

            foreach(var proxyBone in tempProxyBones)
            {
                if (proxyBone == null) continue;

                foreach(var binding in proxyBone.bindings)
                {
                    if (binding == null || binding.bone == null) continue;

                    if (!list.Contains(binding.bone)) list.Add(binding.bone); 
                }
            }
        }

        private void GetAllAnimatingBones(List<Transform> list)
        {
            GetAllPoseableBones(list, null);
            AppendProxyBoneTargets(list);
        }
        private void GetSelectedAnimatingBones(List<Transform> list)
        {
            GetSelectedPoseableBones(list, null);
            AppendProxyBoneTargets(list); 
        }

        #region Keyframes

        public void InsertKeyframes(AnimationSource source, List<Transform> targetTransforms, bool useReferencePose)
        {
            if (source == null || source.rawAnimation == null) return;

            bool isActiveSource = ReferenceEquals(source, CurrentSource);
            BeginNewAnimationEditRecord();
            RecordPose(source, targetTransforms, useReferencePose, useReferencePose && isActiveSource, isActiveSource, true, false, true);
            CommitAnimationEditRecord();
        }
        public void InsertKeyframesLocalSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            InsertKeyframes(CurrentSource, _tempTransforms2, true);
        }
        public void InsertKeyframesLocalAll()
        {
            _tempTransforms2.Clear();
            var source = CurrentSource;
            GetAllPoseableBonesWithKeyframes(source, _tempTransforms2, null);

            InsertKeyframes(source, _tempTransforms2, true);
        }
        public void InsertKeyframesGlobalSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            InsertKeyframes(CurrentSource, _tempTransforms2, false);  
        }
        public void InsertKeyframesGlobalAll()
        {
            _tempTransforms2.Clear(); 
            var source = CurrentSource;
            GetAllPoseableBonesWithKeyframes(source, _tempTransforms2, null); 

            InsertKeyframes(source, _tempTransforms2, false);
        }

        public void RebuildKeyframeAtTime(ImportedAnimatable animatable, AnimationSource source, float time, List<Transform> targetTransforms, bool onlyReAddChangedData = false, bool onlyTransformsWithKeyframesAtTime = true)
        {
            if (animatable == null || source == null || source.rawAnimation == null) return;

            bool HasTargetTransform(string name, out Transform transform)
            {
                transform = null;

                name = name.AsID();
                foreach(var t in targetTransforms) if (t.name.AsID() == name)
                    {
                        transform = t; 
                        return true;
                    }

                return false;
            }

            int frameIndex = timelineWindow.CalculateFrameAtTimelinePositionFloat(time);

            var returnPose = new AnimationUtils.Pose(animatable.animator);
            var animPose = animatable.RestPose.Duplicate().ApplyAnimation(source.rawAnimation, time, false, 1, null, WrapMode.Clamp, WrapMode.Clamp); // <- clamping is important here otherwise it could loop back and give an undesired pose
            
            animatable.RestPose.ApplyTo(animatable.animator);  
            animPose.ApplyTo(animatable.animator);

            var editRecord = BeginNewAnimationEditRecord();
            _tempCurves.Clear();
            _tempTransforms3.Clear();
            foreach (var info in source.rawAnimation.transformAnimationCurves)
            {
                if (info.infoMain.curveIndex < 0) continue;

                if (info.infoMain.isLinear)
                {
                    var curve = source.rawAnimation.transformLinearCurves[info.infoMain.curveIndex];
                    if (curve != null && HasTargetTransform(curve.TransformName, out var t))
                    {
                        if (onlyTransformsWithKeyframesAtTime)
                        {
                            bool hasKey = false;
                            if (curve.frames != null)
                            {
                                foreach(var frame in curve.frames) if (frame != null && frame.timelinePosition == frameIndex)
                                    {
                                        hasKey = true;
                                        break;
                                    }
                            }

                            if (!hasKey) continue;
                        }

                        editRecord.SetOriginalPoseTransformState(t);
                        editRecord.SetOriginalTransformLinearCurveState(curve, false);
                        _tempTransforms3.Add(t);
                        curve.DeleteFramesAt(frameIndex);
                    }
                } 
                else
                {
                    var curve = source.rawAnimation.transformCurves[info.infoMain.curveIndex]; 
                    if (curve != null && HasTargetTransform(curve.TransformName, out var t))
                    {
                        if (onlyTransformsWithKeyframesAtTime)
                        {
                            bool hasKey = false;
                            void CheckSubCurve(EditableAnimationCurve subCurve)
                            {
                                if (subCurve != null)
                                {
                                    if (subCurve.HasKeyAtFrame(frameIndex, timelineWindow.CalculateFrameAtTimelinePosition))
                                    {
                                        hasKey = true;
                                    }
                                    else
                                    {
                                        _tempCurves.Add(subCurve); // sub curve does not have a key at target time, so add it to a list where the newly added key will be removed. Necessary because transform curves are evaluated as a whole
                                    }
                                }
                            }

                            CheckSubCurve(curve.localPositionCurveX);
                            CheckSubCurve(curve.localPositionCurveY);
                            CheckSubCurve(curve.localPositionCurveZ);

                            CheckSubCurve(curve.localRotationCurveX);
                            CheckSubCurve(curve.localRotationCurveY);
                            CheckSubCurve(curve.localRotationCurveZ);
                            CheckSubCurve(curve.localRotationCurveW);

                            CheckSubCurve(curve.localScaleCurveX);
                            CheckSubCurve(curve.localScaleCurveY);
                            CheckSubCurve(curve.localScaleCurveZ);  

                            if (!hasKey) continue;                          
                        }

                        editRecord.SetOriginalPoseTransformState(t);
                        editRecord.SetOriginalTransformCurveState(curve, false);
                        _tempTransforms3.Add(t);
                        _tempTransformCurves.Clear();
                        _tempTransformCurves.Add(curve);
                        IterateTransformCurves(_tempTransformCurves, (EditableAnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<AnimationCurveEditor.KeyframeStateRaw> keyframesEdited) =>
                        {

                            keyframesEdited.RemoveAll(i => getFrameIndex((decimal)i.time) == frameIndex);  

                        }, timelineWindow.CalculateFrameAtTimelinePosition, editRecord);  
                        _tempTransformCurves.Clear(); 
                    }
                }
            }
            RecordPose(source, _tempTransforms3, false, false, false, true, onlyReAddChangedData, false);   

            animatable.RestPose.ApplyTo(animatable.animator);
            returnPose.ApplyTo(animatable.animator);

            foreach(var curve in _tempCurves)
            {
                if (curve == null) continue;
                curve.DeleteKeysAt(time, timelineWindow.CalculateFrameAtTimelinePosition); // sub curves in this list did not have a key at this time, so remove any that were added
            }

            foreach(var transform in _tempTransforms3)
            {
                if (source.rawAnimation.TryGetTransformCurves(transform.name, out _, out _, out var mainCurve, out var baseCurve))
                {
                    if (mainCurve is TransformCurve tc) editRecord.RecordTransformCurveEdit(tc, false);
                    if (mainCurve is TransformLinearCurve tlc) editRecord.RecordTransformLinearCurveEdit(tlc, false);
                    
                    if (baseCurve is TransformCurve btc) editRecord.RecordTransformCurveEdit(btc, true);
                    if (baseCurve is TransformLinearCurve btlc) editRecord.RecordTransformLinearCurveEdit(btlc, true); 
                }
            }

            _tempTransforms3.Clear();
            _tempCurves.Clear();

            CommitAnimationEditRecord();
        }
        public void ReevaluateKeyframeAtTime(ImportedAnimatable animatable, AnimationSource source, float time, List<Transform> targetTransforms) => RebuildKeyframeAtTime(animatable, source, time, targetTransforms, true);

        public void RebuildKeyframeSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            RebuildKeyframeAtTime(ActiveAnimatable, CurrentSource, PlaybackPosition, _tempTransforms2, false);
        }
        public void RebuildKeyframeAll()
        {
            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2, null);

            RebuildKeyframeAtTime(ActiveAnimatable, CurrentSource, PlaybackPosition, _tempTransforms2, false); 
        }

        public void ReevaluateKeyframeSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            ReevaluateKeyframeAtTime(ActiveAnimatable, CurrentSource, PlaybackPosition, _tempTransforms2);
        }
        public void ReevaluateKeyframeAll()
        {
            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2, null);

            ReevaluateKeyframeAtTime(ActiveAnimatable, CurrentSource, PlaybackPosition, _tempTransforms2);
        }

        #endregion

        #region Flip Pose
        /// <summary>
        /// Will append mirrored transform equivalents to the given list
        /// </summary>
        public static void FlipPose(UndoableEditAnimationSourceData editRecord, Transform root, List<Transform> transforms, bool includeLeft = true, bool includeRight = true, bool ignoreUnmirrorableBones = false, bool useWorldSpace = false, bool appendMirrorTransforms = true)
        {
            _tempTransforms.Clear();
            _tempTransformStates.Clear();

            Matrix4x4 rootL2W = Matrix4x4.identity;
            Matrix4x4 rootW2L = Matrix4x4.identity;
            Quaternion rootRot = Quaternion.identity;

            if (root != null)
            {
                rootL2W = root.localToWorldMatrix;
                rootW2L = root.worldToLocalMatrix;
                rootRot = root.rotation;
            }

            void AddTransformState(Transform transform)
            {
                var state = new TransformState();
                transform.GetPositionAndRotation(out state.pos, out state.rot);
                state.localScale = transform.localScale;
                
                if (root != null && transform != root) state = state.WorldToRootSpace(rootW2L, rootRot);
                
                _tempTransformStates[transform] = state;
            }

            foreach (var transform in transforms)
            {
                if (transform == null || _tempTransformStates.ContainsKey(transform)) continue;

                AddTransformState(transform);
                if (!useWorldSpace && transform.parent != null && transform.parent != root) AddTransformState(transform.parent);
                string mirroredName = Utils.GetMirroredName(transform.name, includeLeft, includeRight);
                if (mirroredName != transform.name)
                {
                    var mirrorTransform = root.FindDeepChild(mirroredName);
                    if (mirrorTransform != null) 
                    {
                        AddTransformState(mirrorTransform);
                        if (!useWorldSpace && mirrorTransform.parent != null && mirrorTransform.parent != root) AddTransformState(mirrorTransform.parent);  
                        if (appendMirrorTransforms && !transforms.Contains(mirrorTransform)) _tempTransforms.Add(mirrorTransform); 
                    }
                }
            }
            if (appendMirrorTransforms) foreach (var append in _tempTransforms) if (!transforms.Contains(append)) transforms.Add(append);

            bool TryGetMirrorState(string name, out TransformState mirrorState)
            {
                mirrorState = default;

                string mirroredName = Utils.GetMirroredName(name, includeLeft, includeRight); 
                if (mirroredName != name)
                {
                    var mirrorTransform = root.FindDeepChild(mirroredName);
                    if (mirrorTransform != null && _tempTransformStates.TryGetValue(mirrorTransform, out mirrorState)) return true;
                }

                return false;
            }

            foreach (var transform in transforms)
            {
                if (transform == null) continue;

                var state = _tempTransformStates[transform];
                if (TryGetMirrorState(transform.name, out var mirrorState))
                {
                    state = mirrorState;
                } 
                else if (ignoreUnmirrorableBones) continue;             

                TransformState parentState = default;  
                //bool parentHasMirror = false;
                bool hasParent = !useWorldSpace && transform.parent != null && transform.parent != root;
                if (hasParent)
                {
                    if (TryGetMirrorState(transform.parent.name, out var parentMirrorState))
                    {
                        parentState = parentMirrorState;
                        //parentHasMirror = true;
                    } 
                    else if (!_tempTransformStates.TryGetValue(transform.parent, out parentState)) 
                    {
                        hasParent = false;     
                    }
                }

                if (!hasParent)
                {
                    Maths.MirrorPositionAndRotationX(state.pos, state.rot, out state.pos, out state.rot);
                    if (root != null && transform != root) state = state.RootToWorldSpace(rootL2W, rootRot);

                    editRecord?.SetOriginalPoseTransformState(transform);

                    transform.SetPositionAndRotation(state.pos, state.rot); 
                    transform.localScale = state.localScale;

                    editRecord?.RecordPoseTransformState(transform);
                } 
                else
                {
                    /*if (parentHasMirror || !ignoreUnmirrorableBones)*/ Maths.MirrorPositionAndRotationX(parentState.pos, parentState.rot, out parentState.pos, out parentState.rot);
                    Maths.MirrorPositionAndRotationX(state.pos, state.rot, out state.pos, out state.rot);

                    editRecord?.SetOriginalPoseTransformState(transform);

                    Quaternion iParentRot = Quaternion.Inverse(parentState.rot);
                    state.pos = iParentRot * (state.pos - parentState.pos);
                    state.rot = iParentRot * state.rot;
                    transform.SetLocalPositionAndRotation(state.pos, state.rot);
                    transform.localScale = state.localScale;

                    editRecord?.RecordPoseTransformState(transform);
                }
            }

            _tempTransforms.Clear();
            _tempTransformStates.Clear();
        }
        public void FlipPose() => FlipPose(true, true);
        public void FlipPose(bool includeLeft, bool includeRight, bool ignoreUnmirrorableBones = false, bool useWorldSpace = false)
        {
            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempTransforms2.Clear();
            GetAllAnimatingBones(_tempTransforms2);
            
            var editRecord = BeginNewAnimationEditRecord();

            Transform root = animatable.instance.transform;
            if (animatable.animator != null) root = animatable.animator.RootBoneUnity;
            FlipPose(editRecord, root, _tempTransforms2, includeLeft, includeRight, ignoreUnmirrorableBones);

            if (CanRecord) RecordPose(_tempTransforms2, true, true);
            CommitAnimationEditRecord();
        }
        public void FlipPoseSelected() => FlipPoseSelected(true, true);
        public void FlipPoseSelected(bool includeLeft, bool includeRight, bool ignoreUnmirrorableBones = false, bool useWorldSpace = false)
        {
            if (runtimeEditor == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            Transform root = animatable.instance.transform;
            if (animatable.animator != null) root = animatable.animator.RootBoneUnity;
            
            FlipPose(editRecord, root, _tempTransforms2, includeLeft, includeRight, ignoreUnmirrorableBones);

            if (CanRecord) RecordPose(_tempTransforms2, true, true);
            CommitAnimationEditRecord();
        }
        #endregion

        #region Mirror Pose
        /// <summary>
        /// Will append mirrored transform equivalents to the given list
        /// </summary>
        public static void MirrorPoseLR(UndoableEditAnimationSourceData editRecord, Transform root, List<Transform> transforms) => FlipPose(editRecord, root, transforms, false, true, true, false);
        public void MirrorPoseLR() => FlipPose(false, true, true, false);
        public void MirrorPoseLRSelected() => FlipPoseSelected(false, true, true, false); 

        /// <summary>
        /// Will append mirrored transform equivalents to the given list
        /// </summary>
        public static void MirrorPoseRL(UndoableEditAnimationSourceData editRecord, Transform root, List<Transform> transforms) => FlipPose(editRecord, root, transforms, true, false, true, false);
        public void MirrorPoseRL() => FlipPose(true, false, true, false);
        public void MirrorPoseRLSelected() => FlipPoseSelected(true, false, true, false);    
        #endregion

        #region Copy/Paste Pose
        protected readonly Dictionary<Transform, TransformState> clipboard_pose = new Dictionary<Transform, TransformState>();
        private static void CopyPose(List<Transform> transforms, Dictionary<Transform, TransformState> clipboard)
        {
            clipboard.Clear();
            if (transforms == null) return;
            foreach (var t in transforms) if (t != null) clipboard[t] = new TransformState(t, false);
        }
        public void CopyPose()
        {
            _tempTransforms2.Clear();
            GetAllAnimatingBones(_tempTransforms2); 

            CopyPose(_tempTransforms2, clipboard_pose);
        }
        public void CopyPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            CopyPose(_tempTransforms2, clipboard_pose); 
        }
        private static void PastePose(UndoableEditAnimationSourceData editRecord, List<Transform> transforms, Dictionary<Transform, TransformState> clipboard)
        {
            if (transforms == null) return;
            foreach (var t in transforms) 
            {
                if (t != null && clipboard.TryGetValue(t, out var state)) 
                {
                    editRecord.SetOriginalPoseTransformState(t);
                    state.Apply(t, false);
                    editRecord.RecordPoseTransformState(t);
                }
            }
        }
        public void PasteGlobalPose()
        {
            _tempTransforms2.Clear();
            GetAllAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            PastePose(editRecord, _tempTransforms2, clipboard_pose);

            if (CanRecord) RecordPose(_tempTransforms2, false, true);
            CommitAnimationEditRecord();
        }
        public void PasteGlobalPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            PastePose(editRecord, _tempTransforms2, clipboard_pose);

            if (CanRecord) RecordPose(_tempTransforms2, false, true);
            CommitAnimationEditRecord();
        }
        public void PasteLocalPose()
        {
            _tempTransforms2.Clear();
            GetAllAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            PastePose(editRecord, _tempTransforms2, clipboard_pose);

            if (CanRecord) RecordPose(_tempTransforms2, true, true);
            CommitAnimationEditRecord();
        }
        public void PasteLocalPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            PastePose(editRecord, _tempTransforms2, clipboard_pose);

            if (CanRecord) RecordPose(_tempTransforms2, true, true);
            CommitAnimationEditRecord();
        }
        #endregion

        #region Force Linear Curves

        protected virtual void ForceLinearSelected(float time = -1)
        {
            var source = CurrentSource;
            if (source == null || source.rawAnimation == null) return;

            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            bool changed = false;
            if (source.rawAnimation.transformCurves != null)
            {
                foreach (var curve in source.rawAnimation.transformCurves)
                {
                    if (curve == null) continue;

                    string tName = curve.TransformName.AsID();

                    bool flag = true;
                    foreach(var t in _tempTransforms2)
                    {
                        if (t.name.AsID() == tName)
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) continue;

                    editRecord.SetOriginalRawTransformCurveState(curve);

                    changed = true;
                    if (time < 0) AnimationUtils.ForceLinear(curve, curveEditor); else AnimationUtils.ForceLinearAtFrame(timelineWindow.CalculateFrameAtTimelinePositionFloat(time), timelineWindow.CalculateFrameAtTimelinePositionFloat, curve, curveEditor);

                    editRecord.RecordRawTransformCurveEdit(curve);
                }
            }

            if (changed)
            {
                source.MarkAsDirty();
                RedrawAllCurves(); 
            }

            CommitAnimationEditRecord();
        }
        protected virtual void ForceLinearGlobal(float time = -1)
        {
            var source = CurrentSource;
            if (source == null || source.rawAnimation == null) return;

            var editRecord = BeginNewAnimationEditRecord();

            bool changed = false;
            if (source.rawAnimation.transformCurves != null)
            {
                foreach (var curve in source.rawAnimation.transformCurves)
                {
                    if (curve == null) continue;

                    editRecord.SetOriginalRawTransformCurveState(curve);

                    changed = true;
                    if (time < 0) AnimationUtils.ForceLinear(curve, curveEditor); else AnimationUtils.ForceLinearAtFrame(timelineWindow.CalculateFrameAtTimelinePositionFloat(time), timelineWindow.CalculateFrameAtTimelinePositionFloat, curve, curveEditor);

                    editRecord.RecordRawTransformCurveEdit(curve);
                }
            }

            if (changed)
            {
                source.MarkAsDirty();
                RedrawAllCurves();
            }

            CommitAnimationEditRecord();
        }

        public void ForceLinearCurvesSelected() => ForceLinearSelected(-1);
        public void ForceLinearCurvesGlobal() => ForceLinearGlobal(-1);

        public void ForceLinearKeysSelected() => ForceLinearSelected(PlaybackPosition);
        public void ForceLinearKeysGlobal() => ForceLinearGlobal(PlaybackPosition);

        #endregion

        #region Smooth Curves

        protected virtual void SmoothCurveKeysSelected(float time = -1)
        {
            var source = CurrentSource;
            if (source == null || source.rawAnimation == null) return;

            _tempTransforms2.Clear();
            GetSelectedAnimatingBones(_tempTransforms2);

            var editRecord = BeginNewAnimationEditRecord();

            bool changed = false;
            if (source.rawAnimation.transformCurves != null)
            {
                foreach (var curve in source.rawAnimation.transformCurves)
                {
                    if (curve == null) continue;

                    string tName = curve.TransformName.AsID();

                    bool flag = true;
                    foreach (var t in _tempTransforms2)
                    {
                        if (t.name.AsID() == tName)
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) continue;

                    editRecord.SetOriginalRawTransformCurveState(curve);

                    changed = true;
                    if (time < 0) AnimationUtils.ForceSmooth(curve, curveEditor); else AnimationUtils.ForceSmoothAtFrame(timelineWindow.CalculateFrameAtTimelinePositionFloat(time), timelineWindow.CalculateFrameAtTimelinePositionFloat, curve, curveEditor);

                    editRecord.RecordRawTransformCurveEdit(curve);
                }
            }

            if (changed)
            {
                source.MarkAsDirty();
                RedrawAllCurves();
            }

            CommitAnimationEditRecord();
        }
        protected virtual void SmoothCurveKeysGlobal(float time = -1)
        {
            var source = CurrentSource;
            if (source == null || source.rawAnimation == null) return;

            var editRecord = BeginNewAnimationEditRecord();

            bool changed = false;
            if (source.rawAnimation.transformCurves != null)
            {
                foreach (var curve in source.rawAnimation.transformCurves)
                {
                    if (curve == null) continue;

                    editRecord.SetOriginalRawTransformCurveState(curve);

                    changed = true;
                    if (time < 0) AnimationUtils.ForceSmooth(curve, curveEditor); else AnimationUtils.ForceSmoothAtFrame(timelineWindow.CalculateFrameAtTimelinePositionFloat(time), timelineWindow.CalculateFrameAtTimelinePositionFloat, curve, curveEditor);

                    editRecord.RecordRawTransformCurveEdit(curve);
                }
            }

            if (changed)
            {
                source.MarkAsDirty();
                RedrawAllCurves();
            }

            CommitAnimationEditRecord();
        }

        public void SmoothCurvesSelected() => SmoothCurveKeysSelected(-1);
        public void SmoothCurvesGlobal() => SmoothCurveKeysGlobal(-1);

        public void SmoothKeysSelected() => SmoothCurveKeysSelected(PlaybackPosition);
        public void SmoothKeysGlobal() => SmoothCurveKeysGlobal(PlaybackPosition);  

        #endregion

        #endregion

    }
}

#endif